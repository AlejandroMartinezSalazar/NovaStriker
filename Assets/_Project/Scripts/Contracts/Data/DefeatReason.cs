namespace NovaStriker.Contracts.Data
{
    /// <summary>
    /// Motivo de la derrota. Hoy solo existe OutOfBalls; aquí se añadirán los futuros
    /// (p. ej. derribar a un futbolista que no se debe).
    /// </summary>
    public enum DefeatReason
    {
        None = 0,
        OutOfBalls = 1,
    }
}
