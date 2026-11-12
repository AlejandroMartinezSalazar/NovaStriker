using UnityEngine;

namespace NovaStriker.Gameplay.Throwing
{
    /// <summary>
    /// Asset de datos de una estrategia de tiro. Cada nivel referencia uno (decisión D3).
    /// La lógica vive en clases C# puras; el asset solo guarda parámetros y crea la estrategia.
    /// </summary>
    public abstract class ThrowStrategyDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Tiro";

        public string DisplayName => _displayName;

        public abstract IThrowStrategy CreateStrategy();
    }
}
