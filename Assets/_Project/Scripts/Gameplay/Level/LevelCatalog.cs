using System;
using UnityEngine;

namespace NovaStriker.Gameplay.Level
{
    /// <summary>Lista ordenada de niveles de la campaña. El orden del array es el orden de juego.</summary>
    [CreateAssetMenu(menuName = "NovaStriker/Levels/Level Catalog", fileName = "SO_LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private LevelDefinition[] _levels = Array.Empty<LevelDefinition>();

        public int Count => _levels != null ? _levels.Length : 0;

        public LevelDefinition Get(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"El catálogo tiene {Count} niveles.");
            }

            LevelDefinition level = _levels[index];
            if (level == null)
            {
                throw new InvalidOperationException($"{name}: el nivel {index + 1} está vacío en el catálogo.");
            }

            return level;
        }
    }
}
