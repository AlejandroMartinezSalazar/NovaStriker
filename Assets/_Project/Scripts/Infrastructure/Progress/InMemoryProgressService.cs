using System;
using NovaStriker.Contracts.Services;

namespace NovaStriker.Infrastructure.Progress
{
    /// <summary>
    /// Progreso en memoria: dura mientras la app esté abierta (vive en el contenedor global).
    /// La Etapa 8 lo sustituye por una versión que guarda en disco con el mismo contrato.
    /// </summary>
    public sealed class InMemoryProgressService : IProgressService
    {
        public InMemoryProgressService(int initialLevelIndex = 0)
        {
            if (initialLevelIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialLevelIndex));
            }

            CurrentLevelIndex = initialLevelIndex;
        }

        public int CurrentLevelIndex { get; private set; }
        public int TotalVictories { get; private set; }

        public void CompleteLevel(int levelIndex)
        {
            if (levelIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(levelIndex));
            }

            TotalVictories++;

            // Solo avanza: superar de nuevo un nivel anterior no hace retroceder.
            if (levelIndex + 1 > CurrentLevelIndex)
            {
                CurrentLevelIndex = levelIndex + 1;
            }
        }

        public void RestartCampaign()
        {
            CurrentLevelIndex = 0;
        }
    }
}
