using System;
using NovaStriker.Contracts.Data;
using UnityEngine;

namespace NovaStriker.Infrastructure.Audio
{
    /// <summary>
    /// Ficha de todos los sonidos del juego. Para cambiar un sonido basta con sustituir su archivo
    /// (mismo nombre) o arrastrar otro clip a su entrada.
    /// </summary>
    [CreateAssetMenu(menuName = "NovaStriker/Audio/Sound Library", fileName = "SO_SoundLibrary")]
    public sealed class SoundLibrary : ScriptableObject
    {
        [SerializeField] private SfxEntry[] _effects = Array.Empty<SfxEntry>();
        [SerializeField] private MusicEntry[] _music = Array.Empty<MusicEntry>();

        [Header("Ambiente")]
        [SerializeField] private AudioClip _ambience;
        [SerializeField, Range(0f, 1f)] private float _ambienceVolume = 0.35f;

        [Header("Mezcla")]
        [Tooltip("Volumen relativo de música y ambiente en pausa o en pantallas de resultado.")]
        [SerializeField, Range(0f, 1f)] private float _duckedVolume = 0.35f;

        [Tooltip("Duración (s) del fundido entre pistas de música.")]
        [SerializeField, Min(0f)] private float _musicFade = 0.6f;

        public AudioClip Ambience => _ambience;
        public float AmbienceVolume => _ambienceVolume;
        public float DuckedVolume => _duckedVolume;
        public float MusicFade => _musicFade;

        /// <summary>Solo para el setup del Editor: rellena la ficha completa.</summary>
        internal void SetContent(SfxEntry[] effects, MusicEntry[] music, AudioClip ambience)
        {
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _music = music ?? throw new ArgumentNullException(nameof(music));
            _ambience = ambience;
        }

        public SfxEntry GetSfx(SfxId id)
        {
            for (int i = 0; i < _effects.Length; i++)
            {
                if (_effects[i] != null && _effects[i].Id == id)
                {
                    return _effects[i];
                }
            }

            return null;
        }

        public MusicEntry GetMusic(MusicId id)
        {
            for (int i = 0; i < _music.Length; i++)
            {
                if (_music[i] != null && _music[i].Id == id)
                {
                    return _music[i];
                }
            }

            return null;
        }
    }
}
