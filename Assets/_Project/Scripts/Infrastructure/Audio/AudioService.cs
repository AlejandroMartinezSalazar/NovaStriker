using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Loop;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace NovaStriker.Infrastructure.Audio
{
    /// <summary>
    /// IAudioService sobre AudioSources creadas bajo la raíz global (sobrevive a los cambios de escena):
    /// 2 fuentes de música (fundido cruzado), 1 de ambiente, un grupo de fuentes para efectos de juego
    /// y otro para la interfaz (este no se pausa). Sonido 2D (juego vertical con cámara fija).
    /// Los volúmenes salen de IGameSettings y se actualizan al cambiar los ajustes.
    /// Tick con tiempo no escalado (los fundidos siguen en pausa).
    /// </summary>
    public sealed class AudioService : IAudioService, ITickable, IDisposable
    {
        private const int GameplayVoices = 10;
        private const int UiVoices = 3;

        private readonly SoundLibrary _library;
        private readonly IGameSettings _settings;
        private readonly Transform _root;
        private readonly AudioSource[] _musicSources = new AudioSource[2];
        private readonly AudioSource _ambienceSource;
        private readonly AudioSource[] _sfxSources;
        private readonly AudioSource[] _uiSources;
        private readonly SfxThrottle _throttle = new SfxThrottle();
        private readonly int[] _lastVariant;

        private int _activeMusic;
        private MusicEntry _currentMusic;
        private float _musicBlend = 1f; // 0 = sonando la anterior, 1 = sonando la actual.
        private bool _ambienceEnabled;
        private bool _ducked;
        private bool _paused;
        private float _duckFactor = 1f;
        private float _time;
        private int _nextSfx;
        private int _nextUi;

        public AudioService(SoundLibrary library, IGameSettings settings, Transform parent)
        {
            _library = library != null ? library : throw new ArgumentNullException(nameof(library));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _root = new GameObject("[Audio]").transform;
            _root.SetParent(parent, false);

            _musicSources[0] = CreateSource("Music_A", loop: true);
            _musicSources[1] = CreateSource("Music_B", loop: true);
            _ambienceSource = CreateSource("Ambience", loop: true);
            _sfxSources = CreateVoices("Sfx", GameplayVoices);
            _uiSources = CreateVoices("Ui", UiVoices);
            _lastVariant = new int[Enum.GetValues(typeof(SfxId)).Length + 1];
            for (int i = 0; i < _lastVariant.Length; i++)
            {
                _lastVariant[i] = -1;
            }

            _settings.Changed += ApplyVolumes;
        }

        public void PlaySfx(SfxId id, float intensity = 1f)
        {
            if (!_paused)
            {
                Play(id, intensity, _sfxSources, ref _nextSfx);
            }
        }

        public void PlayUi(SfxId id)
        {
            Play(id, 1f, _uiSources, ref _nextUi);
        }

        public void PlayMusic(MusicId id)
        {
            MusicEntry entry = _library.GetMusic(id);
            if (entry == null || entry.Clip == null)
            {
                Debug.LogWarning($"[NovaStriker] SO_SoundLibrary no tiene música para {id}.");
                return;
            }

            if (_currentMusic == entry && _musicSources[_activeMusic].isPlaying)
            {
                return;
            }

            _activeMusic = 1 - _activeMusic;
            AudioSource source = _musicSources[_activeMusic];
            source.clip = entry.Clip;
            source.volume = 0f;
            source.Play();
            _currentMusic = entry;
            _musicBlend = _library.MusicFade > 0f ? 0f : 1f;
            ApplyVolumes();
        }

        public void StopMusic()
        {
            _currentMusic = null;
            foreach (AudioSource source in _musicSources)
            {
                source.Stop();
            }
        }

        public void SetAmbience(bool enabled)
        {
            if (_ambienceEnabled == enabled)
            {
                return;
            }

            _ambienceEnabled = enabled;
            if (enabled && _library.Ambience != null)
            {
                _ambienceSource.clip = _library.Ambience;
                _ambienceSource.Play();
                if (_paused)
                {
                    _ambienceSource.Pause();
                }
            }
            else
            {
                _ambienceSource.Stop();
            }

            ApplyVolumes();
        }

        public void SetDucked(bool ducked)
        {
            _ducked = ducked;
        }

        public void SetPaused(bool paused)
        {
            if (_paused == paused)
            {
                return;
            }

            _paused = paused;
            foreach (AudioSource source in _sfxSources)
            {
                if (paused)
                {
                    source.Pause();
                }
                else
                {
                    source.UnPause();
                }
            }

            if (_ambienceEnabled)
            {
                if (paused)
                {
                    _ambienceSource.Pause();
                }
                else
                {
                    _ambienceSource.UnPause();
                }
            }
        }

        public void SetSuspended(bool suspended)
        {
            AudioListener.pause = suspended;
        }

        public void Tick(float deltaTime)
        {
            _time += deltaTime;

            float targetDuck = _ducked || _paused ? _library.DuckedVolume : 1f;
            _duckFactor = Mathf.MoveTowards(_duckFactor, targetDuck, deltaTime * 2f);

            if (_musicBlend < 1f)
            {
                _musicBlend = Mathf.MoveTowards(_musicBlend, 1f, deltaTime / Mathf.Max(0.01f, _library.MusicFade));
                if (_musicBlend >= 1f)
                {
                    _musicSources[1 - _activeMusic].Stop();
                }
            }

            ApplyVolumes();
        }

        public void Dispose()
        {
            _settings.Changed -= ApplyVolumes;
            AudioListener.pause = false;
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }
        }

        private void Play(SfxId id, float intensity, AudioSource[] voices, ref int next)
        {
            float sfxVolume = _settings.SfxVolume;
            if (sfxVolume <= 0f || intensity <= 0f)
            {
                return;
            }

            SfxEntry entry = _library.GetSfx(id);
            if (entry == null || entry.Clips == null || entry.Clips.Length == 0)
            {
                return;
            }

            if (!_throttle.TryAcquire(id, _time, entry.MinInterval))
            {
                return;
            }

            int variant = VariantPicker.Next(entry.Clips.Length, _lastVariant[(int)id], Random.value);
            _lastVariant[(int)id] = variant;
            AudioClip clip = entry.Clips[variant];
            if (clip == null)
            {
                return;
            }

            AudioSource source = PickVoice(voices, ref next);
            source.clip = clip;
            source.pitch = 1f + Random.Range(-entry.PitchJitter, entry.PitchJitter);
            source.volume = entry.Volume * Mathf.Clamp01(intensity) * (1f - Random.Range(0f, entry.VolumeJitter)) * sfxVolume;
            source.Play();
        }

        private static AudioSource PickVoice(AudioSource[] voices, ref int next)
        {
            for (int i = 0; i < voices.Length; i++)
            {
                int index = (next + i) % voices.Length;
                if (!voices[index].isPlaying)
                {
                    next = (index + 1) % voices.Length;
                    return voices[index];
                }
            }

            // Todas ocupadas: se reutiliza la más antigua (rotación).
            AudioSource oldest = voices[next];
            next = (next + 1) % voices.Length;
            return oldest;
        }

        private void ApplyVolumes()
        {
            float music = (_currentMusic != null ? _currentMusic.Volume : 0f) * _settings.MusicVolume * _duckFactor;
            _musicSources[_activeMusic].volume = music * _musicBlend;
            _musicSources[1 - _activeMusic].volume = _musicSources[1 - _activeMusic].isPlaying ? music * (1f - _musicBlend) : 0f;
            _ambienceSource.volume = _library.AmbienceVolume * _settings.SfxVolume * _duckFactor;
        }

        private AudioSource CreateSource(string name, bool loop)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(_root, false);
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private AudioSource[] CreateVoices(string prefix, int count)
        {
            var sources = new AudioSource[count];
            for (int i = 0; i < count; i++)
            {
                sources[i] = CreateSource($"{prefix}_{i}", loop: false);
            }

            return sources;
        }
    }
}
