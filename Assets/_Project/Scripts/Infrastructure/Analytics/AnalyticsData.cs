using System;

namespace NovaStriker.Infrastructure.Analytics
{
    /// <summary>Lo que las analíticas recuerdan entre sesiones (Save/analytics.json).</summary>
    [Serializable]
    public sealed class AnalyticsData
    {
        public const int CurrentVersion = 1;

        public int SaveVersion = CurrentVersion;

        /// <summary>Intentos por nivel (índice = nivel - 1), sumando todas las sesiones.</summary>
        public int[] Attempts = Array.Empty<int>();

        /// <summary>Nivel más alto empezado (1 = primero; 0 = ninguno).</summary>
        public int HighestLevel;

        public bool TutorialDone;
    }
}
