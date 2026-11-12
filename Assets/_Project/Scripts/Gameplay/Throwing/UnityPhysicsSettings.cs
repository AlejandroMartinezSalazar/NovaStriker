using UnityEngine;

namespace NovaStriker.Gameplay.Throwing
{
    /// <summary>Lee los valores actuales de Unity (Physics.gravity, Time.fixedDeltaTime).</summary>
    public sealed class UnityPhysicsSettings : IPhysicsSettings
    {
        public Vector3 Gravity => Physics.gravity;
        public float FixedDeltaTime => Time.fixedDeltaTime;
    }
}
