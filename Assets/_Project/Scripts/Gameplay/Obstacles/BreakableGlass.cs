using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Gameplay.Ball;
using NovaStriker.Gameplay.Characters;
using NovaStriker.Gameplay.Debris;
using UnityEngine;

namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>
    /// Vidrio que se agrieta en cada golpe válido (balón, caja o futbolista) y se rompe al llegar a HitsToBreak.
    /// Si lo rompe el balón, este lo atraviesa conservando la velocidad que traía (decisión de diseño).
    /// La grieta se muestra cambiando el color y la opacidad del material según el daño.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class BreakableGlass : MonoBehaviour, IObstacle
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Collider _collider;
        [SerializeField] private Renderer _renderer;

        [Header("Resistencia")]
        [Tooltip("Golpes válidos necesarios para romperlo.")]
        [SerializeField, Min(1)] private int _hitsToBreak = 3;

        [Tooltip("Velocidad relativa mínima (m/s) del balón para que cuente como golpe.")]
        [SerializeField, Min(0f)] private float _minBallSpeed = 3f;

        [Tooltip("Velocidad relativa mínima (m/s) de cajas o futbolistas para que cuente como golpe.")]
        [SerializeField, Min(0f)] private float _minImpactSpeed = 3f;

        [Header("Aspecto")]
        [SerializeField] private Color _intactColor = new Color(0.75f, 0.9f, 1f, 0.25f);
        [SerializeField] private Color _crackedColor = new Color(0.95f, 0.97f, 1f, 0.75f);

        [Header("Fragmentos")]
        [SerializeField, Min(1)] private int _shardColumns = 4;
        [SerializeField, Min(1)] private int _shardRows = 4;

        private ObstacleServices _services;
        private GlassDamage _damage;
        private MaterialPropertyBlock _propertyBlock;

        public ObstacleType Type => ObstacleType.BreakableGlass;
        public int Hits => _damage?.Hits ?? 0;
        public bool IsBroken => _damage != null && _damage.IsBroken;

        public void Construct(ObstacleServices services)
        {
            _services = services;
            _damage = new GlassDamage(_hitsToBreak, _minBallSpeed, _minImpactSpeed);
            ApplyDamageVisual();
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
            _collider.enabled = false;

            Vector3 impactVelocity = collision.relativeVelocity;
            Rigidbody hitter = collision.rigidbody;
            if (hitter != null && hitter.TryGetComponent(out BallView ball))
            {
                // El contacto ya hizo rebotar el balón en este paso: se le devuelve la velocidad previa.
                ball.RestorePreImpactVelocity();
                impactVelocity = ball.PreImpactVelocity;
            }

            _services.Debris.SpawnShards(transform, _shardColumns, _shardRows, impactVelocity);
            _services.EventBus.Publish(new ObstacleBrokenEvent(ObstacleType.BreakableGlass, transform.position));
            gameObject.SetActive(false);
        }

        private void ApplyDamageVisual()
        {
            if (_renderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_propertyBlock);
            float damage = _damage != null ? _damage.Damage01 : 0f;
            _propertyBlock.SetColor(BaseColorId, Color.Lerp(_intactColor, _crackedColor, damage));
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
