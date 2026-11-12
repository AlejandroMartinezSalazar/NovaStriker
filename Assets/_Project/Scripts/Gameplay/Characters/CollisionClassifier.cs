using NovaStriker.Contracts.Data;

namespace NovaStriker.Gameplay.Characters
{
    /// <summary>Clasifica un choque según la capa física del otro objeto.</summary>
    public static class CollisionClassifier
    {
        public static CollisionKind FromLayer(int layer)
        {
            switch (layer)
            {
                case GameLayers.Ball:
                    return CollisionKind.Ball;
                case GameLayers.Obstacle:
                case GameLayers.Breakable:
                case GameLayers.Ragdoll:
                case GameLayers.Debris:
                    return CollisionKind.Impact;
                default:
                    return CollisionKind.Ignore;
            }
        }
    }
}
