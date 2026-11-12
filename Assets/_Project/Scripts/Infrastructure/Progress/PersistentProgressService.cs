using System;
using NovaStriker.Contracts.Services;
using NovaStriker.Infrastructure.Save;

namespace NovaStriker.Infrastructure.Progress
{
    /// <summary>
    /// IProgressService que sobrevive al cierre de la app: carga al crearse y guarda en cada cambio.
    /// Si en una actualización se añaden niveles, quien estaba en "Próximamente" sigue directamente por el siguiente.
    /// </summary>
    public sealed class PersistentProgressService : IProgressService
    {
        public const string SaveKeyName = "progress.json";

        private readonly SaveSlot<ProgressData> _slot;
        private readonly ProgressData _data;

        public PersistentProgressService(ISaveStorage storage, ISerializer serializer)
        {
            _slot = new SaveSlot<ProgressData>(storage, serializer, SaveKeyName, () => new ProgressData());
            _data = Sanitize(_slot.Load());
        }

        public int CurrentLevelIndex => _data.CurrentLevelIndex;
        public int TotalVictories => _data.TotalVictories;

        public void CompleteLevel(int levelIndex)
        {
            if (levelIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(levelIndex));
            }

            _data.TotalVictories++;

            // Solo avanza: superar de nuevo un nivel anterior no hace retroceder.
            if (levelIndex + 1 > _data.CurrentLevelIndex)
            {
                _data.CurrentLevelIndex = levelIndex + 1;
            }

            _slot.Save(_data);
        }

        public void RestartCampaign()
        {
            _data.CurrentLevelIndex = 0;
            _slot.Save(_data);
        }

        private static ProgressData Sanitize(ProgressData data)
        {
            if (data.CurrentLevelIndex < 0)
            {
                data.CurrentLevelIndex = 0;
            }

            // v1 → v2: TotalVictories no existía; JsonUtility lo deja en 0, que es correcto.
            if (data.TotalVictories < 0)
            {
                data.TotalVictories = 0;
            }

            data.SaveVersion = ProgressData.CurrentVersion;
            return data;
        }
    }
}
