using UnityEngine;

namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>
    /// Rebote del balón al romper una caja de vidrio: sale hacia fuera de la caja (en horizontal, desde su centro
    /// hacia el balón) y un poco hacia arriba, con una fracción de la velocidad que traía. C# puro.
    /// </summary>
    public static class GlassCrateBounce
    {
        /// <summary>Componente vertical (relativa) que se añade para que el rebote se vea.</summary>
        public const float Lift = 0.35f;

        public static Vector3 Compute(Vector3 ballPosition, Vector3 crateCenter, Vector3 incoming, float factor)
        {
            Vector3 away = ballPosition - crateCenter;
            away.y = 0f;
            if (away.sqrMagnitude < 1e-6f)
            {
                // Balón justo en la vertical del centro: se devuelve por donde vino.
                away = new Vector3(-incoming.x, 0f, -incoming.z);
            }

            if (away.sqrMagnitude < 1e-6f)
            {
                return Vector3.zero;
            }

            Vector3 direction = (away.normalized + Vector3.up * Lift).normalized;
            return direction * (incoming.magnitude * Mathf.Clamp01(factor));
        }
    }
}
