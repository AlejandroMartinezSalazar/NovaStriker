using NovaStriker.Gameplay.Ball;
using NovaStriker.Gameplay.Characters;
using UnityEngine;

namespace NovaStriker.Gameplay.Level
{
    /// <summary>Trigger de fuera de límites: avisa a balones y futbolistas que entren.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class KillZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null)
            {
                return;
            }

            if (body.TryGetComponent(out BallView ball))
            {
                ball.NotifyOutOfBounds();
                return;
            }

            // En pie es el Rigidbody raíz; en ragdoll es un hueso: se busca el controlador hacia arriba.
            FootballerController footballer = body.GetComponentInParent<FootballerController>();
            if (footballer != null)
            {
                footballer.NotifyOutOfBounds();
            }
        }
    }
}
