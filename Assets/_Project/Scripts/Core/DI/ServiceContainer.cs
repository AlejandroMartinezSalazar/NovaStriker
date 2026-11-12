using System;
using System.Collections.Generic;

namespace NovaStriker.Core.DI
{
    /// <summary>
    /// Contenedor DI mínimo, sin reflexión (seguro con IL2CPP y stripping).
    /// - Un registro por tipo; registrar dos veces el mismo tipo es un error.
    /// - Los hijos resuelven en el padre si no tienen el tipo; el padre nunca ve los registros del hijo.
    /// - Al liberarse, libera primero sus hijos vivos y después sus servicios propios, en orden inverso de creación.
    /// </summary>
    public sealed class ServiceContainer : IServiceContainer
    {
        private sealed class Registration
        {
            public object Instance;
            public Func<IServiceContainer, object> Factory;
            public bool DisposeWithContainer;
            public bool IsResolving;
        }

        private readonly ServiceContainer _parent;
        private readonly Dictionary<Type, Registration> _registrations = new Dictionary<Type, Registration>();
        private readonly List<IDisposable> _ownedDisposables = new List<IDisposable>();
        private readonly List<ServiceContainer> _children = new List<ServiceContainer>();
        private bool _isDisposed;

        public ServiceContainer() : this(null)
        {
        }

        private ServiceContainer(ServiceContainer parent)
        {
            _parent = parent;
        }

        public bool IsDisposed => _isDisposed;

        public void RegisterInstance<T>(T instance, bool disposeWithContainer = false) where T : class
        {
            ThrowIfDisposed();
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance), $"No se puede registrar null para {typeof(T).Name}.");
            }

            AddRegistration(typeof(T), new Registration
            {
                Instance = instance,
                DisposeWithContainer = disposeWithContainer,
            });

            if (disposeWithContainer && instance is IDisposable disposable)
            {
                _ownedDisposables.Add(disposable);
            }
        }

        public void RegisterLazy<T>(Func<IServiceContainer, T> factory, bool disposeWithContainer = false) where T : class
        {
            ThrowIfDisposed();
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            AddRegistration(typeof(T), new Registration
            {
                Factory = container => factory(container),
                DisposeWithContainer = disposeWithContainer,
            });
        }

        public bool IsRegistered<T>() where T : class
        {
            ThrowIfDisposed();
            return FindRegistration(typeof(T), out _, out _);
        }

        public T Resolve<T>() where T : class
        {
            if (TryResolve(out T instance))
            {
                return instance;
            }

            throw new InvalidOperationException(
                $"[ServiceContainer] No hay registro para {typeof(T).FullName}. Regístralo en la composition root.");
        }

        public bool TryResolve<T>(out T instance) where T : class
        {
            ThrowIfDisposed();
            instance = null;

            if (!FindRegistration(typeof(T), out Registration registration, out ServiceContainer owner))
            {
                return false;
            }

            instance = (T)owner.GetOrCreate(typeof(T), registration);
            return true;
        }

        public IServiceContainer CreateChild()
        {
            ThrowIfDisposed();
            var child = new ServiceContainer(this);
            _children.Add(child);
            return child;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            // Primero los hijos: pueden depender de servicios de este contenedor.
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                _children[i].Dispose();
            }
            _children.Clear();

            _isDisposed = true;

            for (int i = _ownedDisposables.Count - 1; i >= 0; i--)
            {
                _ownedDisposables[i].Dispose();
            }
            _ownedDisposables.Clear();
            _registrations.Clear();

            _parent?._children.Remove(this);
        }

        private void AddRegistration(Type type, Registration registration)
        {
            if (_registrations.ContainsKey(type))
            {
                throw new InvalidOperationException(
                    $"[ServiceContainer] {type.FullName} ya está registrado en este contenedor.");
            }

            _registrations.Add(type, registration);
        }

        private bool FindRegistration(Type type, out Registration registration, out ServiceContainer owner)
        {
            ServiceContainer current = this;
            while (current != null)
            {
                if (current._registrations.TryGetValue(type, out registration))
                {
                    owner = current;
                    return true;
                }
                current = current._parent;
            }

            registration = null;
            owner = null;
            return false;
        }

        private object GetOrCreate(Type type, Registration registration)
        {
            if (registration.Instance != null)
            {
                return registration.Instance;
            }

            if (registration.IsResolving)
            {
                throw new InvalidOperationException(
                    $"[ServiceContainer] Dependencia circular al crear {type.FullName}.");
            }

            registration.IsResolving = true;
            try
            {
                // La fábrica recibe el contenedor dueño del registro: un servicio global nunca depende de uno de escena.
                object created = registration.Factory(this);
                if (created == null)
                {
                    throw new InvalidOperationException(
                        $"[ServiceContainer] La fábrica de {type.FullName} devolvió null.");
                }

                registration.Instance = created;
                registration.Factory = null;

                if (registration.DisposeWithContainer && created is IDisposable disposable)
                {
                    _ownedDisposables.Add(disposable);
                }

                return created;
            }
            finally
            {
                registration.IsResolving = false;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ServiceContainer));
            }
        }
    }
}
