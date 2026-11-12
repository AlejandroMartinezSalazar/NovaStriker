namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>Cómo recorre un objeto móvil sus puntos de ruta.</summary>
    public enum PathMode
    {
        /// <summary>Ida y vuelta: 0 → 1 → … → último → … → 1 → 0.</summary>
        PingPong = 0,
        /// <summary>Circuito cerrado: 0 → 1 → … → último → 0.</summary>
        Loop = 1,
    }
}
