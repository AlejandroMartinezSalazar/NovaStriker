using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Core.Pooling;
using NovaStriker.Gameplay.Throwing;
using UnityEngine;

namespace NovaStriker.Gameplay.Ball
{
    /// <summary>
    /// Balón físico. Espera kinemático en el punto de saque hasta Launch; luego lo mueve PhysX.
    /// Lo crea BallFactory (que llama a Construct) y lo recicla BallPool.
    /// CCD: kinemático usa ContinuousSpeculative (el único que Unity admite en cuerpos kinemáticos);
    /// en vuelo usa ContinuousDynamic para no atravesar objetos finos a alta velocidad.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BallView : MonoBehaviour, IBall, IPoolable
    {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private BallCurveForce _curveForce;
        [SerializeField] private TrailRenderer _trail;

        private IEventBus _eventBus;
        private BallConfig _config;
        private BallSettleDetector _settleDetector;
        private bool _isLaunched;
        private Vector3 _preImpactVelocity;

        /// <summary>El balón terminó su recorrido (el pool lo usa para reciclar los que salen del escenario).</summary>
        public event Action<BallView, BallSettleReason> Settled;

        public Vector3 Position => _rigidbody.position;
        public bool IsLaunched => _isLaunched;

        /// <summary>Velocidad al inicio del último paso de física (antes de cualquier choque de ese paso).</summary>
        public Vector3 PreImpactVelocity => _preImpactVelocity;

        public void Construct(IEventBus eventBus, BallConfig config)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _settleDetector = new BallSettleDetector(config.SettleSpeed, config.SettleDuration, config.MaxLifetime);
        }

        public void Launch(in ThrowCommand command)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.linearVelocity = command.InitialVelocity;
            _rigidbody.angularVelocity = command.VisualSpin;
            _curveForce.Begin(command.ExtraAcceleration);
            _settleDetector.Begin();
            _preImpactVelocity = command.InitialVelocity;
            _isLaunched = true;
        }

        /// <summary>
        /// Devuelve al balón la velocidad que traía antes del choque de este paso.
        /// La usa el vidrio rompible para que el balón lo atraviese al romperlo.
        /// </summary>
        public void RestorePreImpactVelocity()
        {
            if (_isLaunched && !_rigidbody.isKinematic)
            {
                _rigidbody.linearVelocity = _preImpactVelocity;
            }
        }

        /// <summary>
        /// Cambia la velocidad del balón en vuelo (la usa la caja de vidrio para hacerlo rebotar al romperse).
        /// </summary>
        public void Deflect(Vector3 velocity)
        {
            if (_isLaunched && !_rigidbody.isKinematic)
            {
                _rigidbody.linearVelocity = velocity;
            }
        }

        /// <summary>
        /// Mueve el balón en vuelo a otro punto sin tocar su velocidad ni su giro (lo usan los portales).
        /// Rigidbody.position lo teletransporta sin pasar por los puntos intermedios; el rastro se borra para que
        /// no dibuje una línea entre los dos portales.
        /// </summary>
        public void TeleportTo(Vector3 position)
        {
            if (!_isLaunched || _rigidbody.isKinematic)
            {
                return;
            }

            transform.position = position;
            _rigidbody.position = position;
            if (_trail != null)
            {
                _trail.Clear();
            }
        }

        /// <summary>Lo llama la KillZone cuando el balón sale del escenario.</summary>
        public void NotifyOutOfBounds()
        {
            if (_isLaunched && _settleDetector.IsActive)
            {
                _settleDetector.Stop();
                Finish(BallSettleReason.OutOfBounds);
            }
        }

        public void OnSpawned()
        {
            // El pool ya colocó el transform antes de activarlo; el Rigidbody kinemático toma esa posición.
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigidbody.isKinematic = true;
            _isLaunched = false;
            if (_trail != null)
            {
                _trail.Clear();
            }
        }

        public void OnDespawned()
        {
            _curveForce.Stop();
            if (_isLaunched && _settleDetector != null && _settleDetector.IsActive)
            {
                // Lo recogen en pleno juego: se avisa para que nadie se quede esperando a este balón.
                _settleDetector.Stop();
                Finish(BallSettleReason.Recycled);
            }

            if (!_rigidbody.isKinematic)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigidbody.isKinematic = true;
            _isLaunched = false;
        }

        private void FixedUpdate()
        {
            if (!_isLaunched)
            {
                return;
            }

            // FixedUpdate corre antes del paso de física: esta es la velocidad previa a los choques del paso.
            _preImpactVelocity = _rigidbody.linearVelocity;

            if (!_settleDetector.IsActive)
            {
                return;
            }

            BallSettleReason reason = _settleDetector.Step(_rigidbody.linearVelocity.magnitude, Time.fixedDeltaTime);
            if (reason != BallSettleReason.None)
            {
                Finish(reason);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Tras el primer impacto manda la física normal: se apaga la curva.
            _curveForce.Stop();

            float impulse = collision.impulse.magnitude;
            if (impulse < _config.MinCollisionImpulse || collision.contactCount == 0)
            {
                return;
            }

            ContactPoint contact = collision.GetContact(0);
            _eventBus.Publish(new BallCollidedEvent(contact.point, contact.normal, impulse, collision.gameObject.layer));
        }

        private void Finish(BallSettleReason reason)
        {
            _curveForce.Stop();
            _eventBus.Publish(new BallSettledEvent(reason, _rigidbody.position));
            Settled?.Invoke(this, reason);
        }
    }
}
