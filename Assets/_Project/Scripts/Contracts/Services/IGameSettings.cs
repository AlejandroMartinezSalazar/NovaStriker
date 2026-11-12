using System;
using NovaStriker.Contracts.Data;

namespace NovaStriker.Contracts.Services
{
    /// <summary>
    /// Ajustes del jugador que se guardan entre sesiones. Etapa 8: volúmenes (los usará el audio en la Etapa 9).
    /// Los valores se limitan a [0, 1]. Los cambios se aplican al momento (Changed) y se guardan con Save
    /// (p. ej. al cerrar el panel de ajustes), así arrastrar una barra no escribe en disco en cada frame.
    /// </summary>
    public interface IGameSettings
    {
        float MusicVolume { get; }
        float SfxVolume { get; }

        /// <summary>Idioma elegido por el jugador; None si aún no ha elegido.</summary>
        Language Language { get; }

        /// <summary>Se dispara tras cada cambio.</summary>
        event Action Changed;

        void SetMusicVolume(float volume);
        void SetSfxVolume(float volume);
        void SetLanguage(Language language);

        /// <summary>Guarda en disco si hubo cambios desde el último guardado.</summary>
        void Save();
    }
}
