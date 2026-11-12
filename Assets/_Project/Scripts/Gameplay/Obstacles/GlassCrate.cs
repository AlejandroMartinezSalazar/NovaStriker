using System.Collections.Generic;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Gameplay.Ball;
using NovaStriker.Gameplay.Characters;
using UnityEngine;

namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>
    /// Caja de vidrio rompible que se desplaza por una ruta editable (PathMover, igual que muros y plataformas
    /// móviles): un Rigidbody cinemático con seis paneles (collider compuesto).
    /// Se agrieta con cada golpe válido (misma regla que BreakableGlass, vía GlassDamage) y al romperse lanza los
    /// fragmentos de cada panel y desaparece. Las cajas de "Contents" viajan fijas dentro (cinemáticas, hijas del
    /// vidrio, sin traqueteo); al romperse se sueltan quietas y caen en vertical, sobre lo que haya debajo.
    /// Excepción a D9: si la rompe el balón NO la atraviesa, rebota hacia fuera (GlassCrateBounce) para no
    /// empujar las cajas que caen.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class GlassCrate : MonoBehaviour, IObstacle
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Rigidbody _body;
        [SerializeField] private Collider[] _panels = new Collider[0];
        [SerializeField] private Renderer[] _renderers = new Renderer[0];

        [Tooltip("Contenedor de las cajas que van dentro (hijo de este objeto). Puedes añadir o quitar cajas en él.")]
        [SerializeField] private Transform _contents;

        [Header("Resistencia")]
        [Tooltip("Golpes válidos necesarios para romperla.")]
        [SerializeField, Min(1)] private int _hitsToBreak = 2;

        [Tooltip("Velocidad relativa mínima (m/s) del balón para que cuente como golpe.")]
        [SerializeField, Min(0f)] private float _minBallSpeed = 3f;

        [Tooltip("Velocidad relativa mínima (m/s) de cajas o futbolistas para que cuente como golpe. " +
                 "Alto para que las cajas de dentro no la rompan al moverse.")]
        [SerializeField, Min(0f)] private float _minImpactSpeed = 4f;

        [Tooltip("Al romperla, el balón rebota hacia fuera con esta fracción de su velocidad.")]
        [SerializeField, Range(0f, 1f)] private float _ballBounce = 0.4f;

        [Tooltip("Segundos en que el balón que la rompió no choca con las cajas que caen (no las empuja aunque las roce).")]
        [SerializeField, Min(0f)] private float _ballIgnoreTime = 0.6f;

        [Header("Aspecto")]
        [SerializeField] private Color _intactColor = new Color(0.75f, 0.9f, 1f, 0.25f);
        [SerializeField] private Color _crackedColor = new Color(0.95f, 0.97f, 1f, 0.75f);

        [Header("Fragmentos (por panel)")]
        [SerializeField, Min(1)] private int _shardColumns = 3;
        [SerializeField, Min(1)] private int _shardRows = 3;

        [Tooltip("Velocidad con la que los fragmentos salen hacia FUERA de cada panel (m/s, antes de la dispersión). " +
                 "Hacia fuera para que no golpeen las cajas de dentro.")]
        [SerializeField, Min(0f)] private float _shardOutwardSpeed = 8f;

        private ObstacleServices _services;
        private GlassDamage _damage;
        private MaterialPropertyBlock _propertyBlock;
        private ISupportSurface _carrier;
        private readonly List<Rigidbody> _carried = new List<Rigidbody>();
        private readonly List<RigidbodyInterpolation> _carriedInterpolation = new List<RigidbodyInterpolation>();

        public ObstacleType Type => ObstacleType.GlassCrate;
        public int Hits => _damage?.Hits ?? 0;
        public bool IsBroken => _damage != null && _damage.IsBroken;

        public void Construct(ObstacleServices services)
        {
            _services = services;
            _damage = new GlassDamage(_hitsToBreak, _minBallSpeed, _minImpactSpeed);
            _carrier = GetComponent<ISupportSurface>();
            HoldContents();
            ApplyDamageVisual();
        }

        /// <summary>Las cajas de dentro viajan fijas con el vidrio hasta que se rompe.</summary>
        private void HoldContents()
        {
            _carried.Clear();
            _carriedInterpolation.Clear();
            if (_contents == null)
            {
                return;
            }

            foreach (Rigidbody box in _contents.GetComponentsInChildren<Rigidbody>(true))
            {
                _carried.Add(box);
                _carriedInterpolation.Add(box.interpolation);
                box.isKinematic = true;
                box.interpolation = RigidbodyInterpolation.None; // Las mueve la jerarquía, no la física.
            }
        }

        private Vector3 CarrierVelocity =>
            _carrier != null ? _carrier.Velocity : _body.isKinematic ? Vector3.zero : _body.linearVelocity;

        private void IgnoreBallAgainstContents(Collider ballCollider)
        {
            if (ballCollider == null)
            {
                return;
            }

            foreach (Rigidbody box in _carried)
            {
                if (box == null)
                {
                    continue;
                }

                foreach (Collider boxCollider in box.GetComponentsInChildren<Collider>())
                {
                    TemporaryCollisionIgnore.Apply(ballCollider, boxCollider, _ballIgnoreTime);
                }
            }
        }

        /// <summary>Saca las cajas del vidrio (antes de desactivarlo) y las suelta con <paramref name="velocity"/>.</summary>
        private void ReleaseContents(Vector3 velocity)
        {
            if (_contents == null)
            {
                return;
            }

            _contents.SetParent(transform.parent, true);
            for (int i = 0; i < _carried.Count; i++)
            {
                Rigidbody box = _carried[i];
                if (box == null)
                {
                    continue;
                }

                box.isKinematic = false;
                box.interpolation = _carriedInterpolation[i];
                box.linearVelocity = velocity;
                box.WakeUp();
            }

            _carried.Clear();
            _carriedInterpolation.Clear();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_damage == null || _damage.IsBroken)
            {
                return;
            }

            CollisionKind kind = CollisionClassifier.FromLayer(collision.gameObject.layer);
            GlassHitResult result = _damage.RegisterHit(kind, collision.relativeVelocity.magnitude);

            switch (result)
            {
                case GlassHitResult.Cracked:
                    ApplyDamageVisual();
                    _services.EventBus.Publish(new GlassCrackedEvent(_damage.Hits, _damage.HitsToBreak, transform.position));
                    break;

                case GlassHitResult.Broken:
                    Break(collision);
                    break;
            }
        }

        private void Break(Collision collision)
        {
            foreach (Collider panel in _panels)
            {
                if (panel != null)
                {
                    panel.enabled = false;
                }
            }

            Rigidbody hitter = collision.rigidbody;
            BallView ball = null;
            if (hitter != null && hitter.TryGetComponent(out ball))
            {
                // El balón rebota hacia fuera de la caja: así no arrastra las cajas que caen.
                ball.Deflect(GlassCrateBounce.Compute(ball.Position, transform.position, ball.PreImpactVelocity, _ballBounce));
            }

            // Cada panel lanza sus fragmentos hacia fuera (nunca hacia las cajas de dentro), más el movimiento de la caja.
            Vector3 carrierVelocity = CarrierVelocity;
            foreach (Collider panel in _panels)
            {
                if (panel == null)
                {
                    continue;
                }

                Vector3 outward = panel.transform.position - transform.position;
                outward = outward.sqrMagnitude > 1e-6f ? outward.normalized : Vector3.up;
                _services.Debris.SpawnShards(panel.transform, _shardColumns, _shardRows,
                    outward * _shardOutwardSpeed + carrierVelocity);
            }

            // Las cajas de dentro caen en vertical. El balón que la rompió puede estar ya rozándolas en este paso
            // (viaja ~0,4 m por paso): durante un instante no choca con ellas, para no empujarlas.
            if (ball != null)
            {
                IgnoreBallAgainstContents(collision.collider);
            }

            ReleaseContents(Vector3.zero);

            _services.EventBus.Publish(new ObstacleBrokenEvent(ObstacleType.GlassCrate, transform.position));
            gameObject.SetActive(false);
        }

        private void ApplyDamageVisual()
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            float damage = _damage != null ? _damage.Damage01 : 0f;
            Color color = Color.Lerp(_intactColor, _crackedColor, damage);

            foreach (Renderer panel in _renderers)
            {
                if (panel == null)
                {
                    continue;
                }

                panel.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, color);
                panel.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
