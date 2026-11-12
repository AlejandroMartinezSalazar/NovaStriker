namespace NovaStriker.Contracts.Data
{
    /// <summary>Estados de la partida dentro de SC_Game. El 0 se reserva como None (regla de StateMachine).</summary>
    public enum GameStateId
    {
        None = 0,
        LevelSetup = 1,
        Aiming = 2,
        BallInFlight = 3,
        ShotResolve = 4,
        Victory = 5,
        Defeat = 6,
        ComingSoon = 7,
        Paused = 8,
    }
}
