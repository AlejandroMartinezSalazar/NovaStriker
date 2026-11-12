using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Core.StateMachine;
using NovaStriker.Gameplay.Level.Rules;

namespace NovaStriker.Gameplay.States
{
    /// <summary>
    /// Ya no quedan balones: se espera a que terminen todos los que siguen en juego (se detienen o salen del
    /// escenario), a que no queden objetivos, o por seguridad a MaxBallFlightTime. Después se resuelve.
    /// </summary>
    public sealed class BallInFlightState : IState, ITickableState
    {
        private readonly IStateMachine<GameStateId> _machine;
        private readonly LevelRulesTracker _rules;
        private readonly float _maxFlightTime;

        private float _elapsed;
        private bool _isActive;

        public BallInFlightState(IStateMachine<GameStateId> machine, LevelRulesTracker rules, float maxFlightTime)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _maxFlightTime = maxFlightTime;
        }

        public void Enter()
        {
            _elapsed = 0f;
            _isActive = true;
            _rules.Changed += OnRulesChanged;
        }

        public void Exit()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            _rules.Changed -= OnRulesChanged;
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            if (_rules.BallsInPlay == 0 || _elapsed >= _maxFlightTime)
            {
                Finish();
            }
        }

        private void OnRulesChanged()
        {
            if (_rules.Status.AllTargetsDown)
            {
                Finish();
            }
        }

        private void Finish()
        {
            if (_isActive)
            {
                _machine.ChangeState(GameStateId.ShotResolve);
            }
        }
    }
}
