using UnityEngine;

namespace NovaStriker.Gameplay.Obstacles.Portals
{
    /// <summary>Efecto visual: el interior del portal "respira" (su tamaño sube y baja en el plano del disco).</summary>
    public sealed class PortalPulse : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.3f)] private float _amplitude = 0.06f;
        [SerializeField, Min(0f)] private float _speed = 3f;

        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            float factor = 1f + _amplitude * Mathf.Sin(Time.time * _speed);
            transform.localScale = new Vector3(_baseScale.x * factor, _baseScale.y, _baseScale.z * factor);
        }
    }
}
