using NovaStriker.Contracts.Data;
using UnityEngine;

namespace NovaStriker.Gameplay.Characters
{
    /// <summary>
    /// Decide si un futbolista en pie debe pasar a ragdoll. C# puro: el controlador le pasa
    /// cada paso de física el estado de apoyo y velocidad, y le informa de los choques.
    /// </summary>
    public sealed class KnockdownDetector
    {
        private readonly KnockdownSettings _settings;
        private float _elapsed;
        private float _ungroundedTime;
        private float _tiltTime;

        public KnockdownDetector(KnockdownSettings settings)
        {
            _settings = settings;
        }

        /// <summary>True cuando ya pasó el tiempo de asentamiento y se vigilan caída, inclinación y arrastre.</summary>
        public bool IsArmed => _elapsed >= _settings.ArmingDelay;

        /// <param name="grounded">Hay superficie de apoyo bajo los pies.</param>
        /// <param name="supportNormal">Normal de esa superficie (ignorada si no hay apoyo).</param>
        /// <param name="velocity">Velocidad actual del cuerpo del futbolista.</param>
        public KnockdownCause Step(float deltaTime, bool grounded, Vector3 supportNormal, Vector3 velocity)
        {
            _elapsed += deltaTime;
            if (!IsArmed)
            {
                return KnockdownCause.None;
            }

            if (velocity.y < -_settings.FallSpeed)
            {
                return KnockdownCause.Fall;
            }

            if (!grounded)
            {
                _ungroundedTime += deltaTime;
                _tiltTime = 0f;
                return _ungroundedTime >= _settings.UngroundedGrace ? KnockdownCause.Fall : KnockdownCause.None;
            }

            _ungroundedTime = 0f;

            if (Vector3.Angle(supportNormal, Vector3.up) > _settings.MaxSupportTiltDegrees)
            {
                _tiltTime += deltaTime;
                if (_tiltTime >= _settings.TiltGrace)
                {
                    return KnockdownCause.Tilt;
                }
            }
            else
            {
                _tiltTime = 0f;
            }

            var horizontal = new Vector2(velocity.x, velocity.z);
            if (horizontal.magnitude > _settings.PushedSpeed)
            {
                return KnockdownCause.Pushed;
            }

            return KnockdownCause.None;
        }

        /// <summary>Evalúa un choque. Los choques derriban aunque el detector aún no esté armado.</summary>
        public KnockdownCause EvaluateCollision(CollisionKind kind, float relativeSpeed)
        {
            switch (kind)
            {
                case CollisionKind.Ball:
                    return relativeSpeed >= _settings.BallHitMinSpeed ? KnockdownCause.BallHit : KnockdownCause.None;
                case CollisionKind.Impact:
                    return relativeSpeed >= _settings.ImpactMinSpeed ? KnockdownCause.Impact : KnockdownCause.None;
                default:
                    return KnockdownCause.None;
            }
        }
    }
}
