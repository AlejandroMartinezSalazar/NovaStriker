namespace NovaStriker.Contracts.Services
{
    /// <summary>
    /// Progreso de la campaña. Los niveles solo avanzan: completar un nivel anterior no hace retroceder.
    /// Etapa 6: en memoria. Etapa 8: persistente (mismo contrato).
    /// </summary>
    public interface IProgressService
    {
        /// <summary>Nivel que toca jugar. Si es igual al número de niveles, la campaña está completa.</summary>
        int CurrentLevelIndex { get; }

        /// <summary>Victorias totales del jugador (no baja nunca; decide cuándo sale un intersticial).</summary>
        int TotalVictories { get; }

        /// <summary>
        /// Registra una victoria: suma a TotalVictories y avanza CurrentLevelIndex si era el nivel actual o posterior.
        /// </summary>
        void CompleteLevel(int levelIndex);

        /// <summary>Vuelve al nivel 1 (opción "Empezar desde el nivel 1" de la pantalla Próximamente).</summary>
        void RestartCampaign();
    }
}
