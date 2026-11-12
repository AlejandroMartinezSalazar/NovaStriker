using System.Collections.Generic;
using UnityEngine;

namespace NovaStriker.Gameplay.Level
{
    /// <summary>
    /// Raíz del prefab de un nivel. Contiene el escenario (suelo, KillZone, AimBackstop), los obstáculos
    /// colocados directamente y los FootballerSpawnPoint. Indica dónde va el balón y dónde la cámara.
    /// </summary>
    public sealed class LevelLayout : MonoBehaviour
    {
        [SerializeField] private Transform _ballSpawnPoint;
        [SerializeField] private Transform _cameraPoint;

        public Transform BallSpawnPoint => _ballSpawnPoint;
        public Transform CameraPoint => _cameraPoint;

        /// <summary>Puntos de aparición de futbolistas bajo este nivel (solo los activos).</summary>
        public void CollectSpawnPoints(List<FootballerSpawnPoint> results)
        {
            GetComponentsInChildren(false, results);
        }

        private void OnDrawGizmos()
        {
            if (_ballSpawnPoint != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(_ballSpawnPoint.position, 0.15f);
            }

            if (_cameraPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_cameraPoint.position, 0.25f);
                Gizmos.DrawLine(_cameraPoint.position, _cameraPoint.position + _cameraPoint.forward * 2f);
            }
        }
    }
}
