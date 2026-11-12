using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Services;
using NovaStriker.Infrastructure.Save;

namespace NovaStriker.Infrastructure.Settings
{
    /// <summary>IGameSettings guardado en disco. Los volúmenes se limitan a [0, 1]; Save solo escribe si hubo cambios.</summary>
    public sealed class PersistentGameSettings : IGameSettings
    {
        public const string SaveKeyName = "settings.json";

        private readonly SaveSlot<SettingsData> _slot;
        private readonly SettingsData _data;
        private bool _isDirty;

        public PersistentGameSettings(ISaveStorage storage, ISerializer serializer)
        {
            _slot = new SaveSlot<SettingsData>(storage, serializer, SaveKeyName, () => new SettingsData());
            _data = _slot.Load();
            _data.MusicVolume = Clamp01(_data.MusicVolume);
            _data.SfxVolume = Clamp01(_data.SfxVolume);
            if (!Enum.IsDefined(typeof(Language), _data.Language))
            {
                _data.Language = (int)Language.None;
            }
            _data.SaveVersion = SettingsData.CurrentVersion;
        }

        public event Action Changed;

        public float MusicVolume => _data.MusicVolume;
        public float SfxVolume => _data.SfxVolume;
        public Language Language => (Language)_data.Language;

        public void SetMusicVolume(float volume) => Apply(ref _data.MusicVolume, volume);

        public void SetSfxVolume(float volume) => Apply(ref _data.SfxVolume, volume);

        public void SetLanguage(Language language)
        {
            if (!Enum.IsDefined(typeof(Language), language) || _data.Language == (int)language)
            {
                return;
            }

            _data.Language = (int)language;
            _isDirty = true;
            Changed?.Invoke();
        }

        public void Save()
        {
            if (_isDirty && _slot.Save(_data))
            {
                _isDirty = false;
            }
        }

        private void Apply(ref float field, float value)
        {
            value = Clamp01(value);
            if (field == value)
            {
                return;
            }

            field = value;
            _isDirty = true;
            Changed?.Invoke();
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value))
            {
                return 0f;
            }

            return value < 0f ? 0f : value > 1f ? 1f : value;
        }
    }
}
