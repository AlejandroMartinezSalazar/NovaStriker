using NovaStriker.Contracts.Data;

namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>
    /// Obstáculo colocado por el diseñador en el nivel. Tras instanciar el nivel, ObstacleInstaller
    /// le inyecta sus dependencias con Construct. Los que no necesitan nada simplemente las ignoran.
    /// </summary>
    public interface IObstacle
    {
        ObstacleType Type { get; }

        void Construct(ObstacleServices services);
    }
}
