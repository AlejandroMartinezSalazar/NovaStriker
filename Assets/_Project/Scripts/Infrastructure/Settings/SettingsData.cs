using System;

namespace NovaStriker.Infrastructure.Settings
{
    /// <summary>Ajustes guardados. Campos públicos para JsonUtility.</summary>
    [Serializable]
    public sealed class SettingsData
    {
        /// <summary>v2: idioma (0 = sin elegir; los guardados v1 lo leen como 0 y muestran la pantalla de idioma).</summary>
        public const int CurrentVersion = 2;
        public const float DefaultMusicVolume = 0.8f;
        public const float DefaultSfxVolume = 1f;

        public int SaveVersion = CurrentVersion;
        public float MusicVolume = DefaultMusicVolume;
        public float SfxVolume = DefaultSfxVolume;
        public int Language;
    }
}
