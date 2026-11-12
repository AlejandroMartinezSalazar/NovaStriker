using UnityEngine;

namespace NovaStriker.Gameplay.Level
{
    /// <summary>Puntos del nivel cargado que usa la indicación de tiro del tutorial.</summary>
    public interface ILevelHintSource
    {
        /// <summary>
        /// Punto de saque del balón y pecho del primer futbolista objetivo aún en pie.
        /// Devuelve false si no hay nivel o no queda ningún objetivo en pie.
        /// </summary>
        bool TryGetAimHint(out Vector3 from, out Vector3 to);
    }
}
