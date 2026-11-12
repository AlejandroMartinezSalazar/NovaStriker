using UnityEngine;

namespace NovaStriker.Gameplay.Characters
{
    /// <summary>
    /// Huesos físicos del futbolista. Lo genera el setup de Etapa 4 a partir del rig Humanoid.
    /// - Animado: la animación mueve los huesos; sus Rigidbodies son kinemáticos y sus colliders están apagados.
    /// - Activo: se apaga el Animator y los huesos pasan a física.
    /// Índice 0 = cadera (Hips).
    /// </summary>
    public sealed class RagdollRig : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private Rigidbody[] _bodies;
        [SerializeField] private Collider[] _colliders;

        public bool IsActive { get; private set; }

        /// <summary>Posición de la cadera (centro aproximado del cuerpo).</summary>
        public Vector3 CenterPosition => _bodies != null && _bodies.Length > 0 ? _bodies[0].position : transform.position;

        public int BoneCount => _bodies?.Length ?? 0;

        private void Awake()
        {
            SetAnimated();
        }

        public void SetAnimated()
        {
            IsActive = false;
            for (int i = 0; i < _bodies.Length; i++)
            {
                _bodies[i].isKinematic = true;
                _bodies[i].detectCollisions = false;
            }
            for (int i = 0; i < _colliders.Length; i++)
            {
                _colliders[i].enabled = false;
            }
            _animator.enabled = true;
        }

        /// <summary>Pasa a ragdoll. Los huesos heredan la velocidad que llevaba el cuerpo.</summary>
        public void Activate(Vector3 inheritedVelocity)
        {
            if (IsActive)
            {
                return;
            }

            IsActive = true;
            _animator.enabled = false;

            for (int i = 0; i < _colliders.Length; i++)
            {
                _colliders[i].enabled = true;
            }
            for (int i = 0; i < _bodies.Length; i++)
            {
                Rigidbody body = _bodies[i];
                body.isKinematic = false;
                body.detectCollisions = true;
                body.linearVelocity = inheritedVelocity;
            }
        }

        /// <summary>Aplica un impulso al hueso más cercano al punto de impacto.</summary>
        public void ApplyImpulse(Vector3 point, Vector3 impulse)
        {
            if (!IsActive || impulse == Vector3.zero || _bodies.Length == 0)
            {
                return;
            }

            Rigidbody nearest = _bodies[0];
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < _bodies.Length; i++)
            {
                float distance = (_bodies[i].worldCenterOfMass - point).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = _bodies[i];
                }
            }

            nearest.AddForceAtPosition(impulse, point, ForceMode.Impulse);
        }
    }
}
