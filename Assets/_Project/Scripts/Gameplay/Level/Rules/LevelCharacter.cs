namespace NovaStriker.Gameplay.Level.Rules
{
    /// <summary>Un futbolista del nivel tal como lo ven las reglas: su id y si cuenta como objetivo.</summary>
    public readonly struct LevelCharacter
    {
        public readonly int Id;
        public readonly bool IsTarget;

        public LevelCharacter(int id, bool isTarget)
        {
            Id = id;
            IsTarget = isTarget;
        }
    }
}
