using System;

namespace NovaStriker.Infrastructure.Progress
{
    /// <summary>
    /// Datos guardados del progreso. Campos públicos para JsonUtility.
    /// SaveVersion permite migrar el formato en el futuro.
    /// </summary>
    [Serializable]
    public sealed class ProgressData
    {
        /// <summary>v2 (Etapa 10): TotalVictories.</summary>
        public const int CurrentVersion = 2;

        public int SaveVersion = CurrentVersion;

        /// <summary>Nivel que toca jugar. Igual al número de niveles = campaña completa (Próximamente).</summary>
        public int CurrentLevelIndex;

        /// <summary>Victorias totales (para la frecuencia de intersticiales).</summary>
        public int TotalVictories;
    }
}
