using System;
using NovaStriker.Core.Events;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NovaStriker.Gameplay.Ball
{
    /// <summary>Factory: instancia el prefab del balón y le inyecta sus dependencias.</summary>
    public sealed class BallFactory : IBallFactory
    {
        private readonly BallView _prefab;
        private readonly IEventBus _eventBus;
        private readonly BallConfig _config;
        private readonly Transform _parent;
        private int _createdCount;

        public BallFactory(BallView prefab, IEventBus eventBus, BallConfig config, Transform parent)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _parent = parent;
        }

        public BallView Create()
        {
            BallView ball = Object.Instantiate(_prefab, _parent);
            ball.name = $"{_prefab.name}_{++_createdCount:00}";
            ball.Construct(_eventBus, _config);
            return ball;
        }
    }
}
