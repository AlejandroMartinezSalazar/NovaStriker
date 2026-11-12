namespace NovaStriker.Contracts.Data
{
    /// <summary>Resultado de evaluar las reglas del nivel tras resolver un tiro.</summary>
    public enum LevelOutcome
    {
        /// <summary>El nivel sigue: se prepara otro balón.</summary>
        None = 0,
        Victory = 1,
        Defeat = 2,
    }
}
