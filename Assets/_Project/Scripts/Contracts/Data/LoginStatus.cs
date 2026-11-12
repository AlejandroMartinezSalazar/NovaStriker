namespace NovaStriker.Contracts.Data
{
    /// <summary>Estado del inicio de sesión automático. El juego funciona igual en todos.</summary>
    public enum LoginStatus
    {
        NotStarted = 0,
        InProgress = 1,
        LoggedIn = 2,

        /// <summary>Falló (sin red, servidor caído...). Se reintenta solo; mientras tanto se juega sin cuenta.</summary>
        Offline = 3,

        /// <summary>No se intenta: SDK sin instalar, sin Title ID o desactivado en la configuración.</summary>
        Disabled = 4,
    }
}
