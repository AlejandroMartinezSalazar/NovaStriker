namespace NovaStriker.Gameplay.Ball
{
    /// <summary>Crea balones nuevos ya construidos (con dependencias inyectadas).</summary>
    public interface IBallFactory
    {
        BallView Create();
    }
}
