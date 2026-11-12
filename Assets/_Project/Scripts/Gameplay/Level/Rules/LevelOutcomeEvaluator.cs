using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Data;

namespace NovaStriker.Gameplay.Level.Rules
{
    /// <summary>
    /// Decide el resultado con las condiciones configuradas. La victoria tiene prioridad:
    /// si el último balón derriba al último objetivo, se gana.
    /// Para añadir una condición (p. ej. "derribaste a un neutral") basta con otra IDefeatCondition.
    /// </summary>
    public sealed class LevelOutcomeEvaluator
    {
        private readonly IReadOnlyList<IVictoryCondition> _victoryConditions;
        private readonly IReadOnlyList<IDefeatCondition> _defeatConditions;

        public LevelOutcomeEvaluator(IReadOnlyList<IVictoryCondition> victoryConditions, IReadOnlyList<IDefeatCondition> defeatConditions)
        {
            _victoryConditions = victoryConditions ?? throw new ArgumentNullException(nameof(victoryConditions));
            _defeatConditions = defeatConditions ?? throw new ArgumentNullException(nameof(defeatConditions));
            if (_victoryConditions.Count == 0)
            {
                throw new ArgumentException("Hace falta al menos una condición de victoria.", nameof(victoryConditions));
            }
        }

        /// <summary>Reglas actuales del juego: ganar derribando a todos, perder sin balones.</summary>
        public static LevelOutcomeEvaluator CreateDefault() => new LevelOutcomeEvaluator(
            new IVictoryCondition[] { new AllTargetsDownCondition() },
            new IDefeatCondition[] { new OutOfBallsCondition() });

        public LevelOutcome Evaluate(in LevelStatus status, out DefeatReason defeatReason)
        {
            defeatReason = DefeatReason.None;
            for (int i = 0; i < _victoryConditions.Count; i++)
            {
                if (_victoryConditions[i].IsMet(status))
                {
                    return LevelOutcome.Victory;
                }
            }

            for (int i = 0; i < _defeatConditions.Count; i++)
            {
                DefeatReason reason = _defeatConditions[i].Check(status);
                if (reason != DefeatReason.None)
                {
                    defeatReason = reason;
                    return LevelOutcome.Defeat;
                }
            }

            return LevelOutcome.None;
        }
    }
}
