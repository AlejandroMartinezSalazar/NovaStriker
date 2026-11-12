using System;
using NovaStriker.Contracts.Data;
using UnityEngine;

namespace NovaStriker.Infrastructure.Audio
{
    /// <summary>Configuración de un efecto: uno o varios clips (variantes) y cómo se reproducen.</summary>
    [Serializable]
    public sealed class SfxEntry
    {
        public SfxId Id;

        [Tooltip("Variantes: se elige una al azar sin repetir la anterior.")]
        public AudioClip[] Clips = Array.Empty<AudioClip>();

        [Range(0f, 1f)] public float Volume = 1f;

        [Tooltip("Variación aleatoria de tono (± fracción). 0.05 = ±5 %.")]
        [Range(0f, 0.5f)] public float PitchJitter = 0.05f;

        [Tooltip("Variación aleatoria de volumen (− fracción).")]
        [Range(0f, 0.5f)] public float VolumeJitter = 0.1f;

        [Tooltip("Tiempo mínimo (s) entre dos reproducciones de este efecto (evita saturar en caídas en cadena).")]
        [Min(0f)] public float MinInterval = 0.05f;
    }
}
