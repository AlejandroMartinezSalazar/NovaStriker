using System;
using NovaStriker.Core.Events;
using NovaStriker.Gameplay.Characters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NovaStriker.Gameplay.Factories
{
    /// <summary>
    /// Factory: instancia el prefab de la definición, le asigna un id único dentro de la escena
    /// y le inyecta sus dependencias. Es la única forma de crear futbolistas en runtime.
    /// </summary>
    public sealed class CharacterFactory : ICharacterFactory
    {
        private readonly IEventBus _eventBus;
        private readonly Transform _parent;
        private int _nextId = 1;

        public CharacterFactory(IEventBus eventBus, Transform parent)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _parent = parent;
        }

        public FootballerController Create(CharacterDefinition definition, Vector3 position, Quaternion rotation)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (definition.Prefab == null)
            {
                throw new InvalidOperationException($"La definición '{definition.name}' no tiene prefab asignado.");
            }

            FootballerController footballer = Object.Instantiate(definition.Prefab, position, rotation, _parent);
            int id = _nextId++;
            footballer.name = $"{definition.Prefab.name}_{id:00}";
            footballer.Construct(_eventBus, definition, id);
            return footballer;
        }
    }
}
