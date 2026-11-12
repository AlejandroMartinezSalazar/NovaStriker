namespace NovaStriker.Contracts.Data
{
    /// <summary>Motivo por el que un balón lanzado se da por terminado.</summary>
    public enum BallSettleReason
    {
        None = 0,
        /// <summary>Quedó casi quieto durante el tiempo configurado.</summary>
        Stopped = 1,
        /// <summary>Entró en una KillZone (salió del escenario).</summary>
        OutOfBounds = 2,
        /// <summary>Superó el tiempo máximo de vida tras el lanzamiento.</summary>
        Timeout = 3,
        /// <summary>El pool lo recogió estando aún en juego (reinicio de nivel o límite de balones en escena).</summary>
        Recycled = 4,
    }
}
