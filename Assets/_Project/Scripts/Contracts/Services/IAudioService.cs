using NovaStriker.Contracts.Data;

namespace NovaStriker.Contracts.Services
{
    /// <summary>
    /// Reproduce música, ambiente y efectos con los volúmenes de IGameSettings.
    /// Los sonidos de juego se detienen en pausa; los de interfaz (PlayUi) no.
    /// </summary>
    public interface IAudioService
    {
        /// <param name="intensity">0..1: escala el volumen (p. ej. según la fuerza de un golpe).</param>
        void PlaySfx(SfxId id, float intensity = 1f);

        /// <summary>Sonidos de interfaz: suenan también con el juego en pausa.</summary>
        void PlayUi(SfxId id);

        /// <summary>Cambia de pista con un fundido corto. La misma pista no se reinicia.</summary>
        void PlayMusic(MusicId id);

        void StopMusic();

        void SetAmbience(bool enabled);

        /// <summary>Baja la música y el ambiente (pantallas de resultado).</summary>
        void SetDucked(bool ducked);

        /// <summary>Pausa los efectos de juego y el ambiente, y baja la música.</summary>
        void SetPaused(bool paused);

        /// <summary>Silencia todo el audio del juego mientras se muestra un anuncio a pantalla completa.</summary>
        void SetSuspended(bool suspended);
    }
}
