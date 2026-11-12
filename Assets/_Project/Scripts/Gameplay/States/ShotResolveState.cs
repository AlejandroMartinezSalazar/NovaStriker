using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Core.StateMachine;
using NovaStriker.Gameplay.Level.Rules;

namespace NovaStriker.Gameplay.States
{
    /// <summary>
    /// Espera a que la física se calme (caídas en cadena incluidas) y decide: victoria, derrota o siguiente balón.
    /// La derrota nunca se declara al lanzar el último balón: solo aquí, cuando ese tiro ya terminó.
    /// </summary>
    public sealed class ShotResolveState : IState, ITickableState
    {
        private readonly IStateMachine<GameStateId> _machine;
        private readonly LevelRulesTracker _rules;
        private readonly LevelOutcomeEvaluator _evaluator;
        private readonly ShotResolveTimer _timer;
        private bool _isActive;

        public ShotResolveState(IStateMachine<GameStateId> machine, LevelRulesTracker rules, LevelOutcomeEvaluator evaluator, ShotResolveTimer timer)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _timer = timer ?? throw new ArgumentNullException(nameof(timer));
        }

        public void Enter()
        {
            _isActive = true;
            _timer.Begin();
            _rules.Changed += _timer.NotifyActivity;
        }

        public void Exit()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            _rules.Changed -= _timer.NotifyActivity;
        }

        public void Tick(float deltaTime)
        {
            LevelStatus status = _rules.Status;
            if (!_timer.Step(deltaTime, status.AllTargetsDown))
            {
                return;
            }

            switch (_evaluator.Evaluate(status, out DefeatReason reason))
            {
                case LevelOutcome.Victory:
                    _machine.ChangeState(GameStateId.Victory);
                    break;
                case LevelOutcome.Defeat:
                    _machine.ChangeState(GameStateId.Defeat, reason);
                    break;
                default:
                    _machine.ChangeState(GameStateId.Aiming);
                    break;
            }
        }
    }
}
