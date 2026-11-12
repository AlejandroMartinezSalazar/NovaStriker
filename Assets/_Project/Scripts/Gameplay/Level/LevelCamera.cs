using System;
using UnityEngine;

namespace NovaStriker.Gameplay.Level
{
    /// <summary>Adapter: ILevelCamera sobre el transform de la cámara de la escena.</summary>
    public sealed class LevelCamera : ILevelCamera
    {
        private readonly Transform _cameraTransform;

        public LevelCamera(Camera camera)
        {
            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera));
            }

            _cameraTransform = camera.transform;
        }

        public void MoveTo(Vector3 position, Quaternion rotation)
        {
            _cameraTransform.SetPositionAndRotation(position, rotation);
        }
    }
}
