namespace NovaStriker.Gameplay.Characters
{
    /// <summary>Cómo cuenta un choque contra un futbolista en pie.</summary>
    public enum CollisionKind
    {
        /// <summary>No derriba (suelo, paredes estáticas, triggers...).</summary>
        Ignore = 0,
        /// <summary>El balón.</summary>
        Ball = 1,
        /// <summary>Cajas, vidrios, restos u otros futbolistas.</summary>
        Impact = 2,
    }
}
