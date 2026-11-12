using System;
using System.Collections.Generic;
using NovaStriker.Core.Pooling;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace NovaStriker.Gameplay.Debris
{
    /// <summary>
    /// Fragmentos de vidrio: Factory (instancia el prefab y le da su callback de devolución) + Object Pool.
    /// Los fragmentos salen disparados en la dirección del impacto, con algo de dispersión.
    /// </summary>
    public sealed class DebrisPool : IDebrisSpawner, IDisposable
    {
        private readonly DebrisShard _prefab;
        private readonly Transform _container;
        private readonly ComponentPool<DebrisShard> _pool;
        private readonly float _lifetime;
        private readonly List<ShardLayout.Shard> _layout = new List<ShardLayout.Shard>(32);

        /// <param name="lifetime">Segundos que vive cada fragmento antes de volver al pool.</param>
        /// <param name="maxPooled">Máximo de fragmentos inactivos guardados.</param>
        public DebrisPool(DebrisShard prefab, Transform container, float lifetime, int maxPooled)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _container = container;
            _lifetime = lifetime;
            _pool = new ComponentPool<DebrisShard>(CreateShard, container, 16, Mathf.Max(1, maxPooled));
        }

        public int CountActive => _pool.CountActive;

        public void SpawnShards(Transform plate, int columns, int rows, Vector3 impactVelocity)
        {
            if (plate == null)
            {
                return;
            }

            ShardLayout.Compute(plate.lossyScale, columns, rows, _layout);
            Quaternion rotation = plate.rotation;
            float impactSpeed = impactVelocity.magnitude;
            Vector3 impactDirection = impactSpeed > 1e-3f ? impactVelocity / impactSpeed : plate.forward;

            foreach (ShardLayout.Shard shard in _layout)
            {
                // El centro local se calcula sin escala: se transforma con posición y rotación de la placa.
                Vector3 position = plate.position + rotation * shard.LocalCenter;
                DebrisShard instance = _pool.Get(position, rotation);

                Vector3 velocity = impactDirection * (impactSpeed * Random.Range(0.25f, 0.6f))
                                   + Random.insideUnitSphere * 1.5f;
                instance.Launch(shard.Size, velocity, Random.insideUnitSphere * 8f, _lifetime * Random.Range(0.8f, 1.2f));
            }
        }

        public void ReleaseAll() => _pool.ReleaseAll();

        public void Dispose() => _pool.Dispose();

        private DebrisShard CreateShard()
        {
            DebrisShard shard = Object.Instantiate(_prefab, _container);
            shard.Construct(Release);
            return shard;
        }

        private void Release(DebrisShard shard)
        {
            if (shard != null && shard.gameObject.activeSelf)
            {
                _pool.Release(shard);
            }
        }
    }
}
