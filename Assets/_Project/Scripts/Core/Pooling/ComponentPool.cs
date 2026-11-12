using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace NovaStriker.Core.Pooling
{
    /// <summary>
    /// Pool de componentes sobre UnityEngine.Pool.ObjectPool.
    /// - La creación se delega (normalmente a una Factory, que inyecta dependencias).
    /// - Los objetos inactivos viven desactivados bajo <c>container</c>.
    /// - Get(position, rotation) coloca el objeto antes de activarlo, para que trails e interpolación no salten.
    /// - Avisa a todos los IPoolable del objeto (incluidos hijos) al sacar y devolver.
    /// </summary>
    public sealed class ComponentPool<T> : IPool<T>, IDisposable where T : Component
    {
        private readonly Func<T> _createFunc;
        private readonly Transform _container;
        private readonly ObjectPool<T> _pool;
        private readonly HashSet<T> _active = new HashSet<T>();
        private readonly Dictionary<T, IPoolable[]> _poolables = new Dictionary<T, IPoolable[]>();
        private readonly List<T> _buffer = new List<T>();

        private bool _hasPendingPose;
        private Vector3 _pendingPosition;
        private Quaternion _pendingRotation;
        private bool _isDisposed;

        /// <param name="createFunc">Crea una instancia nueva (p. ej. BallFactory.Create).</param>
        /// <param name="container">Padre de los objetos inactivos. Puede ser null.</param>
        /// <param name="defaultCapacity">Capacidad inicial de la lista interna.</param>
        /// <param name="maxSize">Máximo de objetos inactivos guardados; el exceso se destruye al devolverse.</param>
        public ComponentPool(Func<T> createFunc, Transform container, int defaultCapacity = 8, int maxSize = 64)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _container = container;
            _pool = new ObjectPool<T>(
                CreateItem,
                OnGetItem,
                OnReleaseItem,
                OnDestroyItem,
                collectionCheck: false, // La doble devolución se valida en Release con un mensaje claro.
                defaultCapacity,
                maxSize);
        }

        public int CountActive => _active.Count;
        public int CountInactive => _pool.CountInactive;

        public T Get()
        {
            ThrowIfDisposed();
            _hasPendingPose = false;
            return _pool.Get();
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            ThrowIfDisposed();
            _hasPendingPose = true;
            _pendingPosition = position;
            _pendingRotation = rotation;
            try
            {
                return _pool.Get();
            }
            finally
            {
                _hasPendingPose = false;
            }
        }

        public void Release(T item)
        {
            ThrowIfDisposed();
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!_active.Contains(item))
            {
                throw new InvalidOperationException(
                    $"[ComponentPool<{typeof(T).Name}>] Se intentó devolver '{item.name}', que no está activo en este pool.");
            }

            _pool.Release(item);
        }

        public void ReleaseAll()
        {
            ThrowIfDisposed();
            _buffer.Clear();
            _buffer.AddRange(_active);
            for (int i = 0; i < _buffer.Count; i++)
            {
                T item = _buffer[i];
                if (item == null)
                {
                    // Destruido desde fuera del pool (p. ej. se descargó su escena): solo se olvida.
                    _active.Remove(item);
                    _poolables.Remove(item);
                    continue;
                }
                _pool.Release(item);
            }
            _buffer.Clear();
        }

        public void Prewarm(int count)
        {
            ThrowIfDisposed();
            int missing = count - _pool.CountInactive;
            if (missing <= 0)
            {
                return;
            }

            _buffer.Clear();
            for (int i = 0; i < missing; i++)
            {
                _buffer.Add(_pool.Get());
            }
            for (int i = 0; i < _buffer.Count; i++)
            {
                _pool.Release(_buffer[i]);
            }
            _buffer.Clear();
        }

        public void Clear()
        {
            ThrowIfDisposed();
            ReleaseAll();
            _pool.Clear();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            Clear();
            _pool.Dispose();
            _isDisposed = true;
        }

        private T CreateItem()
        {
            T item = _createFunc();
            if (item == null)
            {
                throw new InvalidOperationException($"[ComponentPool<{typeof(T).Name}>] createFunc devolvió null.");
            }

            item.gameObject.SetActive(false);
            item.transform.SetParent(_container, false);
            _poolables[item] = item.GetComponentsInChildren<IPoolable>(true);
            return item;
        }

        private void OnGetItem(T item)
        {
            Transform itemTransform = item.transform;
            if (_hasPendingPose)
            {
                itemTransform.SetPositionAndRotation(_pendingPosition, _pendingRotation);
            }

            item.gameObject.SetActive(true);
            _active.Add(item);

            IPoolable[] poolables = _poolables[item];
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnSpawned();
            }
        }

        private void OnReleaseItem(T item)
        {
            _active.Remove(item);

            IPoolable[] poolables = _poolables[item];
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnDespawned();
            }

            item.gameObject.SetActive(false);
            item.transform.SetParent(_container, false);
        }

        private void OnDestroyItem(T item)
        {
            _active.Remove(item);
            _poolables.Remove(item);

            if (item == null)
            {
                return; // Ya destruido por Unity (p. ej. al descargar la escena).
            }

            if (Application.isPlaying)
            {
                Object.Destroy(item.gameObject);
            }
            else
            {
                Object.DestroyImmediate(item.gameObject);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException($"ComponentPool<{typeof(T).Name}>");
            }
        }
    }
}
