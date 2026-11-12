using NovaStriker.Contracts.Data;

namespace NovaStriker.Gameplay.Level.Rules
{
    /// <summary>Derrota: no quedan balones (se evalúa cuando el último tiro ya terminó).</summary>
    public sealed class OutOfBallsCondition : IDefeatCondition
    {
        public DefeatReason Check(in LevelStatus status) =>
            status.BallsRemaining <= 0 ? DefeatReason.OutOfBalls : DefeatReason.None;
    }
}
