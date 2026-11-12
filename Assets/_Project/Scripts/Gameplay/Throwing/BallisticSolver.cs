using UnityEngine;

namespace NovaStriker.Gameplay.Throwing
{
    /// <summary>
    /// Matemática del tiro con aceleración constante <c>a</c> (gravedad + curva).
    ///
    /// Modelo continuo:  p(t) = p0 + v0·t + ½·a·t²   ⇒   v0 = (Δp − ½·a·T²) / T
    ///
    /// La física avanza en pasos fijos dt con Euler semi-implícito (v += a·dt; p += v·dt), que tras n pasos da
    ///   p_n = p0 + v0·t + ½·a·t² + ½·a·dt·t     (t = n·dt)
    /// El término extra ½·a·dt·t desviaría el balón ~10 cm por segundo de vuelo. Se compensa restando ½·a·dt a v0,
    /// y se redondea T a un múltiplo de dt para que el balón pase exactamente por el objetivo en un paso.
    ///
    /// Funciones puras y sin estado.
    /// </summary>
    public static class BallisticSolver
    {
        /// <summary>Redondea el tiempo de vuelo hacia arriba al múltiplo más cercano del paso fijo (mínimo un paso).</summary>
        public static float QuantizeFlightTime(float flightTime, float fixedDeltaTime)
        {
            if (fixedDeltaTime <= 0f)
            {
                return flightTime;
            }

            int steps = Mathf.Max(1, Mathf.CeilToInt(flightTime / fixedDeltaTime - 1e-4f));
            return steps * fixedDeltaTime;
        }

        /// <summary>Velocidad inicial que lleva de <paramref name="origin"/> a <paramref name="target"/> en <paramref name="flightTime"/>.</summary>
        /// <param name="totalAcceleration">Gravedad + aceleración extra constante.</param>
        /// <param name="fixedDeltaTime">Paso de la física; 0 para el modelo continuo exacto.</param>
        public static Vector3 SolveInitialVelocity(
            Vector3 origin, Vector3 target, Vector3 totalAcceleration, float flightTime, float fixedDeltaTime)
        {
            Vector3 continuous = (target - origin - 0.5f * flightTime * flightTime * totalAcceleration) / flightTime;
            return continuous - 0.5f * fixedDeltaTime * totalAcceleration;
        }

        /// <summary>Posición en el instante <paramref name="time"/> según el mismo modelo discreto que usa la física.</summary>
        public static Vector3 PositionAt(
            Vector3 origin, Vector3 initialVelocity, Vector3 totalAcceleration, float time, float fixedDeltaTime)
        {
            return origin
                   + initialVelocity * time
                   + 0.5f * time * time * totalAcceleration
                   + 0.5f * fixedDeltaTime * time * totalAcceleration;
        }

        /// <summary>
        /// Llena <paramref name="buffer"/> con puntos equiespaciados en el tiempo desde el origen hasta el objetivo.
        /// Devuelve cuántos puntos escribió.
        /// </summary>
        public static int SampleTrajectory(
            Vector3 origin, in ThrowCommand command, Vector3 gravity, float fixedDeltaTime, Vector3[] buffer)
        {
            if (buffer == null || buffer.Length < 2)
            {
                return 0;
            }

            Vector3 totalAcceleration = gravity + command.ExtraAcceleration;
            int count = buffer.Length;
            for (int i = 0; i < count; i++)
            {
                float time = command.FlightTime * i / (count - 1);
                buffer[i] = PositionAt(origin, command.InitialVelocity, totalAcceleration, time, fixedDeltaTime);
            }

            return count;
        }

        /// <summary>Dirección horizontal (XZ) normalizada de origen a objetivo; Vector3.forward si coinciden.</summary>
        public static Vector3 HorizontalDirection(Vector3 origin, Vector3 target)
        {
            Vector3 delta = target - origin;
            delta.y = 0f;
            float sqrMagnitude = delta.sqrMagnitude;
            return sqrMagnitude > 1e-8f ? delta / Mathf.Sqrt(sqrMagnitude) : Vector3.forward;
        }

        /// <summary>Giro visual típico: el balón rueda hacia delante en la dirección del tiro.</summary>
        public static Vector3 ForwardSpin(Vector3 horizontalDirection, float spinRate)
        {
            return Vector3.Cross(Vector3.up, horizontalDirection) * spinRate;
        }
    }
}
