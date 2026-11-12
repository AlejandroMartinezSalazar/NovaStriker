using NovaStriker.Gameplay.Throwing;
using UnityEngine;

namespace NovaStriker.Gameplay.Level
{
    /// <summary>
    /// Datos de un nivel: escenario, balones disponibles y estrategia de tiro (D3).
    /// El número de futbolistas a derribar sale de los FootballerSpawnPoint del escenario.
    /// </summary>
    [CreateAssetMenu(menuName = "NovaStriker/Levels/Level Definition", fileName = "SO_Level_00")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Nivel";

        [Tooltip("Balones disponibles para derribar a todos los futbolistas.")]
        [SerializeField, Min(1)] private int _ballCount = 3;

        [Tooltip("Cómo vuela el balón en este nivel (Recto, Bombeado, Curva...).")]
        [SerializeField] private ThrowStrategyDefinition _throwStrategy;

        [Tooltip("Prefab del escenario con sus obstáculos y puntos de aparición.")]
        [SerializeField] private LevelLayout _layout;

        public string DisplayName => _displayName;
        public int BallCount => _ballCount;
        public ThrowStrategyDefinition ThrowStrategy => _throwStrategy;
        public LevelLayout Layout => _layout;
    }
}
