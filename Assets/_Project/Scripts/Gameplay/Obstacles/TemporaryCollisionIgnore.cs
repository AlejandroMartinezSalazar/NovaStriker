using NovaStriker.Gameplay.Level;
using UnityEngine;

namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>
    /// Desactiva el choque entre dos colliders durante un tiempo y luego lo restaura.
    /// Vive en el objeto del segundo collider (el que permanece en el nivel) y se destruye al terminar.
    /// </summary>
    public sealed class TemporaryCollisionIgnore : MonoBehaviour
    {
        private Collider _a;
        private Collider _b;
        private SettleCountdown _countdown;

        public static void Apply(Collider a, Collider b, float seconds)
        {
            if (a == null || b == null)
            {
                return;
            }

            Physics.IgnoreCollision(a, b, true);
            var timer = b.gameObject.AddComponent<TemporaryCollisionIgnore>();
            timer._a = a;
            timer._b = b;
            timer._countdown = new SettleCountdown(Mathf.Max(0f, seconds));
        }

        private void FixedUpdate()
        {
            if (_countdown != null && !_countdown.Step(Time.fixedDeltaTime))
            {
                return;
            }

            Restore();
            Destroy(this);
        }

        private void OnDestroy() => Restore();

        private void Restore()
        {
            if (_a != null && _b != null)
            {
                Physics.IgnoreCollision(_a, _b, false);
            }

            _a = null;
            _b = null;
        }
    }
}
