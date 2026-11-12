using UnityEngine;

namespace NovaStriker.Gameplay.Input
{
    /// <summary>Resumen de un gesto de arrastre completo (en píxeles de pantalla).</summary>
    public readonly struct SwipeData
    {
        public readonly Vector2 StartPosition;
        public readonly Vector2 EndPosition;
        public readonly float Duration;

        /// <summary>Curvatura del trazo en [-1, 1]. Positivo: arqueado hacia la derecha respecto a la línea inicio→fin.</summary>
        public readonly float CurveBias;

        public SwipeData(Vector2 startPosition, Vector2 endPosition, float duration, float curveBias)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            Duration = duration;
            CurveBias = curveBias;
        }
    }
}
