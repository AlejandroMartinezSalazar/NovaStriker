using UnityEngine;

namespace NovaStriker.Core.Loop
{
    /// <summary>ITimeScale sobre Time.timeScale. Con 0 se detienen la física y el tiempo escalado.</summary>
    public sealed class UnityTimeScale : ITimeScale
    {
        public float Scale
        {
            get => Time.timeScale;
            set => Time.timeScale = value;
        }
    }
}
