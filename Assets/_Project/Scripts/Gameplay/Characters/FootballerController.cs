using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Gameplay.Obstacles;
using UnityEngine;

namespace NovaStriker.Gameplay.Characters
{
    /// <summary>
    /// Futbolista. En pie: un Rigidbody con cápsula (gravedad activa, sin volcar) y el modelo animado encima.
    /// Así, si la caja de debajo se mueve, el personaje cae de verdad y el detector lo nota.
    /// Pasa a ragdoll (una sola vez) por golpe, caída, inclinación, arrastre, impacto o KillZone.
    /// Sobre una plataforma móvil (ISupportSurface) se mueve con ella, y caída y arrastre se miden
    /// respecto a la velocidad del apoyo: viajar en la plataforma no cuenta como "arrastrado".
    /// Lo crea CharacterFactory, que llama a Construct.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FootballerController : MonoBehaviour
    {
        /// <summary>Altura (m) sobre los pies desde la que se lanza el sondeo de suelo.</summary>
        private const float ProbeStartHeight = 0.5f;

        [SerializeField] private Rigidbody _body;
        [SerializeField] private CapsuleCollider _capsule;
        [SerializeField] private RagdollRig _ragdoll;
        [SerializeField] private Animator _animator;
        [SerializeField] private string _idleStateName;

        [Tooltip("Distancia (m) bajo los pies en la que aún se considera que hay apoyo.")]
        [SerializeField, Min(0.01f)] private float _groundTolerance = 0.15f;

        private IEventBus _eventBus;
        private CharacterDefinition _definition;
        private KnockdownDetector _detector;
        private Collider _supportCollider;
        private ISupportSurface _supportSurface;

        public int Id { get; private set; }
        public bool IsTarget => _definition != null && _definition.IsTarget;
        public bool IsKnockedDown { get; private set; }
        public KnockdownCause KnockdownCause { get; private set; }

        /// <summary>Se dispara una vez, al pasar a ragdoll.</summary>
        public event Action<FootballerController> KnockedDown;

        public void Construct(IEventBus eventBus, CharacterDefinition definition, int id)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            _detector = new KnockdownDetector(definition.Knockdown);
            Id = id;

            // Cada futbolista empieza el baile en un punto distinto para que no se muevan sincronizados.
            if (!string.IsNullOrEmpty(_idleStateName) && _animator != null)
            {
                _animator.Play(Animator.StringToHash(_idleStateName), 0, UnityEngine.Random.value);
            }
        }

        /// <summary>Lo llama la KillZone.</summary>
        public void NotifyOutOfBounds()
        {
            KnockDown(KnockdownCause.OutOfBounds, Vector3.zero, Vector3.zero);
        }

        private void FixedUpdate()
        {
            if (IsKnockedDown || _detector == null)
            {
                return;
            }

            bool grounded = ProbeGround(out Vector3 supportNormal, out Vector3 supportVelocity);
            Vector3 relativeVelocity = _body.linearVelocity - supportVelocity;

            KnockdownCause cause = _detector.Step(Time.fixedDeltaTime, grounded, supportNormal, relativeVelocity);
            if (cause != KnockdownCause.None)
            {
                KnockDown(cause, Vector3.zero, Vector3.zero);
                return;
            }

            // Sobre una plataforma móvil: acompañar su movimiento horizontal (la fricción sola no basta).
            if (grounded && _supportSurface != null)
            {
                Vector3 velocity = _body.linearVelocity;
                _body.linearVelocity = new Vector3(supportVelocity.x, velocity.y, supportVelocity.z);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsKnockedDown || _detector == null)
            {
                return;
            }

            CollisionKind kind = CollisionClassifier.FromLayer(collision.gameObject.layer);
            Vector3 relativeVelocity = collision.relativeVelocity;
            KnockdownCause cause = _detector.EvaluateCollision(kind, relativeVelocity.magnitude);
            if (cause == KnockdownCause.None)
            {
                return;
            }

            Vector3 hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : _body.worldCenterOfMass;
            Rigidbody hitter = collision.rigidbody;
            Vector3 hitterPosition = hitter != null ? hitter.position : hitPoint;
            float hitterMass = hitter != null ? hitter.mass : 1f;

            Vector3 impulse = HitImpulseCalculator.Compute(
                relativeVelocity,
                _body.worldCenterOfMass - hitterPosition,
                hitterMass,
                _definition.HitImpulseMultiplier,
                _definition.MaxHitImpulse);

            KnockDown(cause, hitPoint, impulse);
        }

        private bool ProbeGround(out Vector3 supportNormal, out Vector3 supportVelocity)
        {
            Vector3 origin = _body.position + Vector3.up * ProbeStartHeight;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, ProbeStartHeight + _groundTolerance,
                    GameLayers.CharacterSupportMask, QueryTriggerInteraction.Ignore))
            {
                supportNormal = hit.normal;
                ISupportSurface surface = ResolveSupportSurface(hit.collider);
                supportVelocity = surface != null ? surface.Velocity : Vector3.zero;
                return true;
            }

            _supportCollider = null;
            _supportSurface = null;
            supportNormal = Vector3.up;
            supportVelocity = Vector3.zero;
            return false;
        }

        /// <summary>Busca (y cachea por collider) si el apoyo es una superficie móvil.</summary>
        private ISupportSurface ResolveSupportSurface(Collider support)
        {
            if (support != _supportCollider)
            {
                _supportCollider = support;
                _supportSurface = support.GetComponentInParent<ISupportSurface>();
            }

            return _supportSurface;
        }

        private void KnockDown(KnockdownCause cause, Vector3 hitPoint, Vector3 impulse)
        {
            if (IsKnockedDown)
            {
                return;
            }

            IsKnockedDown = true;
            KnockdownCause = cause;

            Vector3 inheritedVelocity = _body.linearVelocity;
            _capsule.enabled = false;
            _body.isKinematic = true;
            _ragdoll.Activate(inheritedVelocity);
            _ragdoll.ApplyImpulse(hitPoint, impulse);

            _eventBus?.Publish(new CharacterKnockedDownEvent(Id, IsTarget, cause, _ragdoll.CenterPosition));
            KnockedDown?.Invoke(this);
        }
    }
}
