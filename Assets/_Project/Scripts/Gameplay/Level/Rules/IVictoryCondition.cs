namespace NovaStriker.Gameplay.Level.Rules
{
    /// <summary>Strategy: una forma de ganar el nivel.</summary>
    public interface IVictoryCondition
    {
        bool IsMet(in LevelStatus status);
    }
}
