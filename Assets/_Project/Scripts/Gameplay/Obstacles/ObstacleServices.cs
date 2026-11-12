using System;
using NovaStriker.Core.Events;
using NovaStriker.Gameplay.Debris;

namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>Dependencias que se inyectan en los obstáculos colocados en un nivel.</summary>
    public sealed class ObstacleServices
    {
        public ObstacleServices(IEventBus eventBus, IDebrisSpawner debris)
        {
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            Debris = debris ?? throw new ArgumentNullException(nameof(debris));
        }

        public IEventBus EventBus { get; }
        public IDebrisSpawner Debris { get; }
    }
}
