namespace NovaStriker.Gameplay.Level.Rules
{
    /// <summary>Victoria: todos los objetivos derribados.</summary>
    public sealed class AllTargetsDownCondition : IVictoryCondition
    {
        public bool IsMet(in LevelStatus status) => status.AllTargetsDown;
    }
}
