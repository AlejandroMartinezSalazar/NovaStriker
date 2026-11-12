using UnityEngine;

namespace NovaStriker.Gameplay.Ball
{
    /// <summary>
    /// Aplica la aceleración lateral constante de la curva en cada paso de física, hasta el primer impacto.
    /// Usa ForceMode.Acceleration: no depende de la masa, igual que el cálculo de la estrategia.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BallCurveForce : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rigidbody;

        private Vector3 _acceleration;
        private bool _isActive;

        public bool IsActive => _isActive;

        public void Begin(Vector3 acceleration)
        {
            _acceleration = acceleration;
            _isActive = acceleration.sqrMagnitude > 0f;
        }

        public void Stop()
        {
            _isActive = false;
            _acceleration = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (_isActive)
            {
                _rigidbody.AddForce(_acceleration, ForceMode.Acceleration);
            }
        }
    }
}
