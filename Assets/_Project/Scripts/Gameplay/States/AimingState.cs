using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Core.StateMachine;
using NovaStriker.Gameplay.Aim;
using NovaStriker.Gameplay.Level.Rules;
using NovaStriker.Gameplay.Throwing;
using UnityEngine;

namespace NovaStriker.Gameplay.States
{
    /// <summary>
    /// El jugador apunta y dispara. Tras cada tiro gasta un balón y, si quedan, prepara el siguiente al cabo de
    /// ReloadDelay sin esperar a que el anterior termine (los tiros anteriores siguen en juego).
    /// Con el último balón lanzado pasa a BallInFlight. Si ya no quedan objetivos, pasa a resolver.
    /// En pausa se bloquea el apuntado (OnSuspend) y la recarga queda congelada.
    /// </summary>
    public sealed class AimingState : IState, ITickableState, ISuspendableState
    {
        /// <summary>Mínimo físico: con menos, el balón nuevo aparecería encima del que acaba de salir.</summary>
        public const float MinReloadDelay = 0.1f;

        private readonly IStateMachine<GameStateId> _machine;
        private readonly IThrowController _throwController;
        private readonly IAimController _aim;
        private readonly LevelRulesTracker _rules;
        private readonly IEventBus _eventBus;
        private readonly float _reloadDelay;

        private IDisposable _shotSubscription;
        private bool _isActive;
        private float _reloadIn = -1f;

        public AimingState(
            IStateMachine<GameStateId> machine,
            IThrowController throwController,
            IAimController aim,
            LevelRulesTracker rules,
            IEventBus eventBus,
            float reloadDelay)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _throwController = throwController ?? throw new ArgumentNullException(nameof(throwController));
            _aim = aim ?? throw new ArgumentNullException(nameof(aim));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _reloadDelay = Mathf.Max(MinReloadDelay, reloadDelay);
        }

        /// <summary>Hay una recarga pendiente (el siguiente balón aún no ha aparecido).</summary>
        public bool IsReloading => _reloadIn >= 0f;

        public void Enter()
        {
            LevelStatus status = _rules.Status;
            if (status.AllTargetsDown)
            {
                _machine.ChangeState(GameStateId.ShotResolve);
                return;
            }

            if (status.BallsRemaining <= 0)
            {
                _machine.ChangeState(GameStateId.BallInFlight);
                return;
            }

            _isActive = true;
            _reloadIn = -1f;
            PrepareBall();
            _shotSubscription = _eventBus.Subscribe<ShotFiredEvent>(OnShotFired);
            _rules.Changed += OnRulesChanged;
            _aim.Enable();
        }

        public void Exit()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            _reloadIn = -1f;
            _aim.Disable();
            _rules.Changed -= OnRulesChanged;
            _shotSubscription.Dispose();
            _shotSubscription = null;
        }

        public void Tick(float deltaTime)
        {
            if (_reloadIn < 0f)
            {
                return;
            }

            _reloadIn -= deltaTime;
            if (_reloadIn <= 0f)
            {
                _reloadIn = -1f;
                PrepareBall();
            }
        }

        public void OnSuspend() => _aim.Disable();

        public void OnResume() => _aim.Enable();

        private void OnShotFired(ShotFiredEvent e)
        {
            _rules.RegisterShot();
            if (!_isActive || _rules.Status.AllTargetsDown)
            {
                return; // Ya salió del estado o lo gestiona OnRulesChanged.
            }

            if (_rules.Status.BallsRemaining <= 0)
            {
                _machine.ChangeState(GameStateId.BallInFlight);
                return;
            }

            // El balón nuevo aparece un instante después para no solaparse con el que acaba de salir.
            _reloadIn = _reloadDelay;
        }

        private void OnRulesChanged()
        {
            if (_rules.Status.AllTargetsDown)
            {
                _machine.ChangeState(GameStateId.ShotResolve);
            }
        }

        private void PrepareBall()
        {
            if (!_throwController.PrepareBall())
            {
                Debug.LogError("[NovaStriker] No se pudo preparar el balón en el punto de saque.");
            }
        }
    }
}
