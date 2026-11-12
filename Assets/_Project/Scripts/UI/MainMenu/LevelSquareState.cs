namespace NovaStriker.UI.MainMenu
{
    /// <summary>Cómo se pinta cada cuadradito del panel de nivel del menú.</summary>
    public enum LevelSquareState
    {
        /// <summary>Superado (amarillo).</summary>
        Completed = 0,
        /// <summary>El que toca jugar (verde).</summary>
        Current = 1,
        /// <summary>Sin jugar (color del panel).</summary>
        Locked = 2,
    }
}
