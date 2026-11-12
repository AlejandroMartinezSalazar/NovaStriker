using UnityEngine;

namespace NovaStriker.UI.Common
{
    /// <summary>Curvas de animación para la UI (funciones puras de t en [0, 1]).</summary>
    public static class UiEasing
    {
        /// <summary>Crece pasándose un poco del final y vuelve (efecto "pop").</summary>
        public static float BackOut(float t, float overshoot = 1.70158f)
        {
            t = Mathf.Clamp01(t) - 1f;
            return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
        }

        /// <summary>Saltos que se van apagando: 0 en reposo, hasta 1 en el punto más alto del primer salto.</summary>
        public static float DampedBounce(float time, float frequency, float damping)
        {
            if (time <= 0f)
            {
                return 0f;
            }

            return Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI)) * Mathf.Exp(-damping * time);
        }
    }
}
