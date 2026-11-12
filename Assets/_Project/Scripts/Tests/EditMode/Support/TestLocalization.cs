using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Services;
using NovaStriker.Infrastructure.Localization;

namespace NovaStriker.Tests.EditMode
{
    /// <summary>Ajustes en memoria y un servicio de idioma real para las pruebas de presentadores.</summary>
    internal sealed class MemorySettings : IGameSettings
    {
        public float MusicVolume { get; private set; } = 0.8f;
        public float SfxVolume { get; private set; } = 1f;
        public Language Language { get; private set; }
        public int Saves;
        public event Action Changed;
        public void SetMusicVolume(float volume) { MusicVolume = volume; Changed?.Invoke(); }
        public void SetSfxVolume(float volume) { SfxVolume = volume; Changed?.Invoke(); }
        public void SetLanguage(Language language) { Language = language; Changed?.Invoke(); }
        public void Save() => Saves++;
    }

    internal static class TestLocalization
    {
        /// <summary>Español ya elegido: los textos de las pruebas existentes no cambian.</summary>
        public static ILocalization Spanish()
        {
            var settings = new MemorySettings();
            settings.SetLanguage(Language.Spanish);
            return new LocalizationService(settings, Language.Spanish);
        }
    }
}
