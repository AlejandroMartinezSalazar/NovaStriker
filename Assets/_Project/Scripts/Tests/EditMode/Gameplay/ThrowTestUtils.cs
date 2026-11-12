using NovaStriker.Gameplay.Throwing;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Gameplay
{
    /// <summary>Integrador de referencia: Euler semi-implícito, el mismo esquema que usa la física por pasos.</summary>
    internal static class ThrowTestUtils
    {
        public static readonly Vector3 Gravity = new Vector3(0f, -9.81f, 0f);
        public const float FixedDeltaTime = 0.02f;

        /// <param name="maxLateralX">Mayor desviación en X (con signo) respecto al origen. Los tests tiran hacia +Z, así que +X es la derecha.</param>
        public static Vector3 Simulate(Vector3 origin, in ThrowCommand command, Vector3 gravity, float dt, out float maxY, out float maxLateralX)
        {
            Vector3 position = origin;
            Vector3 velocity = command.InitialVelocity;
            Vector3 acceleration = gravity + command.ExtraAcceleration;
            int steps = Mathf.RoundToInt(command.FlightTime / dt);

            maxY = origin.y;
            maxLateralX = 0f;

            for (int i = 0; i < steps; i++)
            {
                velocity += acceleration * dt;
                position += velocity * dt;
                maxY = Mathf.Max(maxY, position.y);
                float lateral = position.x - origin.x;
                if (Mathf.Abs(lateral) > Mathf.Abs(maxLateralX))
                {
                    maxLateralX = lateral;
                }
            }

            return position;
        }

        public static Vector3 Simulate(Vector3 origin, in ThrowCommand command)
        {
            return Simulate(origin, command, Gravity, FixedDeltaTime, out _, out _);
        }

        public static ThrowContext Context(Vector3 origin, Vector3 target, float curveBias = 0f)
        {
            return new ThrowContext(origin, target, Gravity, FixedDeltaTime, curveBias);
        }
    }
}
