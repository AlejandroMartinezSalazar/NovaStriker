using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using UnityEngine;

namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>
    /// Identifica un obstáculo simple (caja, caja inamovible, muro, vidrio, muro o plataforma móvil).
    /// Su comportamiento lo dan la física (Rigidbody, collider, material) y, si se mueve, PathMover.
    /// Las cajas dinámicas publican ObstacleImpactEvent al chocar con fuerza (para el audio); los choques
    /// con el balón no, porque ya suenan como golpe del balón.
    /// </summary>
    public sealed class ObstacleView : MonoBehaviour, IObstacle
    {
        [SerializeField] private ObstacleType _type = ObstacleType.Box;

        [Header("Impactos (solo cajas)")]
        [Tooltip("Impulso mínimo (N·s) para avisar de un impacto.")]
        [SerializeField, Min(0f)] private float _minImpactImpulse = 1.5f;

        [Tooltip("Tiempo mínimo (s) entre dos avisos de la misma caja.")]
        [SerializeField, Min(0f)] private float _impactCooldown = 0.12f;

        private ObstacleServices _services;
        private float _lastImpactTime = float.NegativeInfinity;

        public ObstacleType Type => _type;

        public void Construct(ObstacleServices services)
        {
            _services = services;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_type != ObstacleType.Box || _services == null || collision.gameObject.layer == GameLayers.Ball)
            {
                return;
            }

            float impulse = collision.impulse.magnitude;
            if (impulse < _minImpactImpulse || Time.time - _lastImpactTime < _impactCooldown)
            {
                return;
            }

            _lastImpactTime = Time.time;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            _services.EventBus.Publish(new ObstacleImpactEvent(_type, point, impulse));
        }
    }
}
