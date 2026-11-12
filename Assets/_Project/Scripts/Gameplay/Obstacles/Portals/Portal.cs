using NovaStriker.Contracts.Data;
using NovaStriker.Gameplay.Ball;
using UnityEngine;

namespace NovaStriker.Gameplay.Obstacles.Portals
{
    /// <summary>
    /// Una boca de portal: trigger en la capa Portal (solo detecta el balón). Cuando el balón en vuelo entra,
    /// pide a su PortalPair que lo lleve a la otra boca. Para moverla, añade un PathMover a este objeto.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class Portal : MonoBehaviour
    {
        private readonly PortalArrivalGate _arrival = new PortalArrivalGate();
        private PortalPair _pair;

        /// <summary>Punto por el que sale el balón.</summary>
        public Vector3 Center => transform.position;

        internal void Bind(PortalPair pair) => _pair = pair;

        internal void ExpectArrival(Rigidbody body) => _arrival.Expect(body, Time.time);

        private void OnTriggerEnter(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null || _pair == null || other.gameObject.layer != GameLayers.Ball)
            {
                return;
            }

            if (_arrival.ShouldIgnoreEnter(body, Time.time))
            {
                return;
            }

            if (body.TryGetComponent(out BallView ball) && ball.IsLaunched)
            {
                _pair.Teleport(this, ball, body);
            }
        }
    }
}
