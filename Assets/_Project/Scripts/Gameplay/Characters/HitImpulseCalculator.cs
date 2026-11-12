using UnityEngine;

namespace NovaStriker.Gameplay.Characters
{
    /// <summary>
    /// Calcula el impulso que recibe el ragdoll en un choque.
    /// No depende del convenio de signo de Collision.relativeVelocity: orienta la dirección
    /// para que siempre empuje desde el objeto que golpea hacia el futbolista.
    /// </summary>
    public static class HitImpulseCalculator
    {
        /// <param name="relativeVelocity">Collision.relativeVelocity (se usa su módulo y su eje).</param>
        /// <param name="hitterToTarget">Vector desde el objeto que golpea hacia el centro del futbolista.</param>
        /// <param name="hitterMass">Masa del objeto que golpea (1 si no tiene Rigidbody).</param>
        /// <param name="multiplier">Exageración arcade del golpe.</param>
        /// <param name="maxImpulse">Límite para evitar explosiones del ragdoll.</param>
        public static Vector3 Compute(Vector3 relativeVelocity, Vector3 hitterToTarget, float hitterMass, float multiplier, float maxImpulse)
        {
            float speed = relativeVelocity.magnitude;
            if (speed < 1e-4f)
            {
                return Vector3.zero;
            }

            Vector3 direction = relativeVelocity / speed;
            if (Vector3.Dot(direction, hitterToTarget) < 0f)
            {
                direction = -direction;
            }

            float magnitude = Mathf.Min(speed * Mathf.Max(hitterMass, 0f) * multiplier, maxImpulse);
            return direction * magnitude;
        }
    }
}
