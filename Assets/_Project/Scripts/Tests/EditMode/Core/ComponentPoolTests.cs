using System;
using NovaStriker.Core.Pooling;
using NovaStriker.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NovaStriker.Tests.EditMode.Core
{
    public class ComponentPoolTests
    {
        private GameObject _container;
        private ComponentPool<PoolableProbe> _pool;
        private int _created;

        [SetUp]
        public void SetUp()
        {
            _created = 0;
            _container = new GameObject("PoolContainer");
            _pool = new ComponentPool<PoolableProbe>(Create, _container.transform, defaultCapacity: 4, maxSize: 3);
        }

        [TearDown]
        public void TearDown()
        {
            _pool.Dispose();
            Object.DestroyImmediate(_container);
        }

        private PoolableProbe Create()
        {
            _created++;
            return new GameObject($"Probe_{_created}").AddComponent<PoolableProbe>();
        }

        [Test]
        public void Get_ReturnsActiveObject_AndCallsOnSpawnedWhileActive()
        {
            PoolableProbe probe = _pool.Get();

            Assert.IsTrue(probe.gameObject.activeSelf);
            Assert.AreEqual(1, probe.SpawnCount);
            Assert.IsTrue(probe.WasActiveOnSpawn);
            Assert.AreEqual(1, _pool.CountActive);
        }

        [Test]
        public void GetWithPose_PositionsBeforeOnSpawned()
        {
            var position = new Vector3(1f, 2f, 3f);
            PoolableProbe probe = _pool.Get(position, Quaternion.identity);

            Assert.AreEqual(position, probe.PositionOnSpawn);
        }

        [Test]
        public void Release_DeactivatesAndReparents_AndReuses()
        {
            PoolableProbe first = _pool.Get();
            first.transform.SetParent(null);

            _pool.Release(first);

            Assert.IsFalse(first.gameObject.activeSelf);
            Assert.AreSame(_container.transform, first.transform.parent);
            Assert.AreEqual(1, first.DespawnCount);

            PoolableProbe second = _pool.Get();
            Assert.AreSame(first, second);
            Assert.AreEqual(1, _created);
            Assert.AreEqual(2, second.SpawnCount);
        }

        [Test]
        public void Release_Twice_Throws()
        {
            PoolableProbe probe = _pool.Get();
            _pool.Release(probe);

            Assert.Throws<InvalidOperationException>(() => _pool.Release(probe));
        }

        [Test]
        public void Release_ObjectFromOutside_Throws()
        {
            var stranger = new GameObject("Stranger").AddComponent<PoolableProbe>();
            try
            {
                Assert.Throws<InvalidOperationException>(() => _pool.Release(stranger));
            }
            finally
            {
                Object.DestroyImmediate(stranger.gameObject);
            }
        }

        [Test]
        public void ReleaseAll_ReturnsEveryActiveObject()
        {
            _pool.Get();
            _pool.Get();

            _pool.ReleaseAll();

            Assert.AreEqual(0, _pool.CountActive);
            Assert.AreEqual(2, _pool.CountInactive);
        }

        [Test]
        public void ReleaseAll_IgnoresObjectsDestroyedOutsideThePool()
        {
            PoolableProbe destroyed = _pool.Get();
            _pool.Get();
            Object.DestroyImmediate(destroyed.gameObject);

            Assert.DoesNotThrow(() => _pool.ReleaseAll());
            Assert.AreEqual(0, _pool.CountActive);
            Assert.AreEqual(1, _pool.CountInactive);
        }

        [Test]
        public void Prewarm_CreatesInactiveObjects()
        {
            _pool.Prewarm(3);

            Assert.AreEqual(3, _created);
            Assert.AreEqual(3, _pool.CountInactive);
            Assert.AreEqual(0, _pool.CountActive);
        }

        [Test]
        public void Release_BeyondMaxSize_DestroysExtraObjects()
        {
            PoolableProbe[] probes = { _pool.Get(), _pool.Get(), _pool.Get(), _pool.Get() };

            foreach (PoolableProbe probe in probes)
            {
                _pool.Release(probe);
            }

            Assert.AreEqual(3, _pool.CountInactive);
            Assert.IsTrue(probes[3] == null, "El objeto que excede maxSize debe destruirse.");
        }

        [Test]
        public void Clear_DestroysAllObjects()
        {
            PoolableProbe active = _pool.Get();
            PoolableProbe inactive = _pool.Get();
            _pool.Release(inactive);

            _pool.Clear();

            Assert.AreEqual(0, _pool.CountActive);
            Assert.AreEqual(0, _pool.CountInactive);
            Assert.IsTrue(active == null);
            Assert.IsTrue(inactive == null);
        }

        [Test]
        public void Dispose_RejectsFurtherUse()
        {
            _pool.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _pool.Get());
        }
    }
}
