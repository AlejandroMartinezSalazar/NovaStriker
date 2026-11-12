using NovaStriker.Core.Pooling;
using UnityEngine;

namespace NovaStriker.Tests.Support
{
    /// <summary>
    /// Componente de prueba que cuenta las llamadas del pool.
    /// Vive en un ensamblado runtime (no Editor) porque Unity no permite añadir
    /// MonoBehaviours de ensamblados de Editor a un GameObject.
    /// Solo compila con UNITY_INCLUDE_TESTS, así que nunca entra en el build del juego.
    /// </summary>
    public sealed class PoolableProbe : MonoBehaviour, IPoolable
    {
        public int SpawnCount { get; private set; }
        public int DespawnCount { get; private set; }
        public bool WasActiveOnSpawn { get; private set; }
        public Vector3 PositionOnSpawn { get; private set; }

        public void OnSpawned()
        {
            SpawnCount++;
            WasActiveOnSpawn = gameObject.activeSelf;
            PositionOnSpawn = transform.position;
        }

        public void OnDespawned() => DespawnCount++;
    }
}
