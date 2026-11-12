using UnityEngine;

namespace NovaStriker.Gameplay.Aim
{
    /// <summary>
    /// Raycast desde la cámara contra las capas de apuntado (GameLayers.AimMask).
    /// El backstop invisible del nivel garantiza que casi siempre haya un punto.
    /// </summary>
    public sealed class PhysicsAimTargetResolver : IAimTargetResolver
    {
        private readonly Camera _camera;
        private readonly int _layerMask;
        private readonly float _maxDistance;

        public PhysicsAimTargetResolver(Camera camera, int layerMask, float maxDistance)
        {
            _camera = camera;
            _layerMask = layerMask;
            _maxDistance = maxDistance;
        }

        public bool TryResolve(Vector2 screenPosition, out Vector3 worldPoint)
        {
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _layerMask, QueryTriggerInteraction.Ignore))
            {
                worldPoint = hit.point;
                return true;
            }

            worldPoint = default;
            return false;
        }
    }
}
