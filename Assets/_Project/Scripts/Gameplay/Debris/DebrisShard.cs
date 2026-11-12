using System;
using NovaStriker.Core.Pooling;
using UnityEngine;

namespace NovaStriker.Gameplay.Debris
{
    /// <summary>Fragmento físico temporal. Vuelve al pool al acabar su tiempo de vida.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DebrisShard : MonoBehaviour, IPoolable
    {
        [SerializeField] private Rigidbody _body;

        private Action<DebrisShard> _release;
        private float _despawnAt = float.MaxValue;

        public void Construct(Action<DebrisShard> release)
        {
            _release = release ?? throw new ArgumentNullException(nameof(release));
        }

        /// <summary>Configura tamaño, velocidad y tiempo de vida. Se llama justo después de sacarlo del pool.</summary>
        public void Launch(Vector3 size, Vector3 velocity, Vector3 angularVelocity, float lifetime)
        {
            transform.localScale = size;
            _body.isKinematic = false;
            _body.linearVelocity = velocity;
            _body.angularVelocity = angularVelocity;
            _despawnAt = Time.time + lifetime;
        }

        public void OnSpawned()
        {
        }

        public void OnDespawned()
        {
            if (!_body.isKinematic)
            {
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
            }
            _body.isKinematic = true;
            _despawnAt = float.MaxValue;
        }

        private void Update()
        {
            if (Time.time >= _despawnAt)
            {
                _despawnAt = float.MaxValue;
                _release?.Invoke(this);
            }
        }
    }
}
