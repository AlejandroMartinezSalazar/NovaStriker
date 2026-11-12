namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>Resultado de un golpe sobre un vidrio rompible.</summary>
    public enum GlassHitResult
    {
        /// <summary>No cuenta (demasiado flojo, o el vidrio ya está roto).</summary>
        Ignored = 0,
        /// <summary>Cuenta como golpe y se agrieta más.</summary>
        Cracked = 1,
        /// <summary>Último golpe: se rompe.</summary>
        Broken = 2,
    }
}
