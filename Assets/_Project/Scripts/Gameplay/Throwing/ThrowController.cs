using System;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Gameplay.Ball;
using UnityEngine;

namespace NovaStriker.Gameplay.Throwing
{
    /// <summary>Une estrategia, balón y física. No sabe nada de input ni de reglas del nivel.</summary>
    public sealed class ThrowController : IThrowController
    {
        private readonly IBallProvider _ballProvider;
        private Vector3 _spawnPosition;
        private readonly IPhysicsSettings _physics;
        private readonly IEventBus _eventBus;

        private IThrowStrategy _strategy;
        private IBall _readyBall;

        public ThrowController(IBallProvider ballProvider, Vector3 spawnPosition, IPhysicsSettings physics, IEventBus eventBus)
        {
            _ballProvider = ballProvider ?? throw new ArgumentNullException(nameof(ballProvider));
            _spawnPosition = spawnPosition;
            _physics = physics ?? throw new ArgumentNullException(nameof(physics));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public bool HasBallReady => _readyBall != null;

        public string StrategyId => _strategy?.Id;

        public void SetStrategy(IThrowStrategy strategy)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        public void SetSpawnPosition(Vector3 position)
        {
            _spawnPosition = position;
        }

        public bool PrepareBall()
        {
            if (_readyBall == null)
            {
                _readyBall = _ballProvider.Spawn(_spawnPosition);
            }

            return _readyBall != null;
        }

        public void ClearReadyBall()
        {
            _readyBall = null;
        }

        public bool TryPreview(Vector3 target, float curveBias, out Vector3 origin, out ThrowCommand command)
        {
            origin = _readyBall != null ? _readyBall.Position : _spawnPosition;
            return TryCreateCommand(origin, target, curveBias, out command);
        }

        public bool TryThrow(Vector3 target, float curveBias)
        {
            if (_readyBall == null)
            {
                return false;
            }

            Vector3 origin = _readyBall.Position;
            if (!TryCreateCommand(origin, target, curveBias, out ThrowCommand command))
            {
                return false;
            }

            IBall ball = _readyBall;
            _readyBall = null;
            ball.Launch(command);
            _eventBus.Publish(new ShotFiredEvent(_strategy.Id, origin, target, command.InitialVelocity));
            return true;
        }

        private bool TryCreateCommand(Vector3 origin, Vector3 target, float curveBias, out ThrowCommand command)
        {
            if (_strategy == null)
            {
                throw new InvalidOperationException("ThrowController: falta asignar la estrategia de tiro del nivel.");
            }

            var context = new ThrowContext(origin, target, _physics.Gravity, _physics.FixedDeltaTime, curveBias);
            return _strategy.TryCreateThrow(context, out command);
        }
    }
}
