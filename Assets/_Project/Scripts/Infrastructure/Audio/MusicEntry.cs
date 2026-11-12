using System;
using NovaStriker.Contracts.Data;
using UnityEngine;

namespace NovaStriker.Infrastructure.Audio
{
    /// <summary>Una pista de música en bucle.</summary>
    [Serializable]
    public sealed class MusicEntry
    {
        public MusicId Id;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 0.6f;
    }
}
