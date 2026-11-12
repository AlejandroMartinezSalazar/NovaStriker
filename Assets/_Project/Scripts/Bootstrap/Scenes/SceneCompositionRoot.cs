using System;
using System.Collections.Generic;
using NovaStriker.Core.DI;
using NovaStriker.Core.Loop;
using NovaStriker.Core.Utils;
using UnityEngine;

namespace NovaStriker.Bootstrap.Scenes
{
    /// <summary>
    /// Base de las raíces de composición de escena.
    /// Crea un contenedor hijo del de la app y un grupo de IDisposable; ambos se liberan juntos.
    /// Regla: los objetos de la escena no hacen trabajo en Awake/Start que dependa de servicios;
    /// esperan a que la raíz los construya.
    /// </summary>
    public abstract class SceneCompositionRoot : MonoBehaviour, ISceneCompositionRoot
    {
        private IServiceContainer _sceneContainer;
        private CompositeDisposable _disposables;
        private readonly List<ITickable> _tickables = new List<ITickable>();
        private bool _wasComposed;
        private bool _isDisposed;

        public bool CanCompose => !_wasComposed;

        public void Compose(IServiceContainer appContainer)
        {
            if (appContainer == null)
            {
                throw new ArgumentNullException(nameof(appContainer));
            }

            if (_wasComposed)
            {
                throw new InvalidOperationException($"{GetType().Name} ya fue compuesta.");
            }

            _wasComposed = true;
            _sceneContainer = appContainer.CreateChild();
            _disposables = new CompositeDisposable();
            OnCompose(_sceneContainer, _disposables);
        }

        /// <summary>Crea los objetos de la escena. Todo lo que deba liberarse se añade a <paramref name="disposables"/>.</summary>
        protected abstract void OnCompose(IServiceContainer sceneContainer, CompositeDisposable disposables);

        public void Dispose()
        {
            if (!_wasComposed || _isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _tickables.Clear();
            _disposables.Dispose();
            _sceneContainer.Dispose();
        }

        /// <summary>Registra un objeto C# que se actualizará cada frame mientras la escena esté compuesta.</summary>
        protected void AddTickable(ITickable tickable)
        {
            _tickables.Add(tickable ?? throw new ArgumentNullException(nameof(tickable)));
        }

        protected virtual void Update()
        {
            if (!_wasComposed || _isDisposed)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _tickables.Count; i++)
            {
                _tickables[i].Tick(deltaTime);
            }
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }

        /// <summary>Falla con un mensaje claro si falta una referencia asignada en el Inspector.</summary>
        protected T Require<T>(T reference, string fieldName) where T : UnityEngine.Object
        {
            if (reference == null)
            {
                throw new InvalidOperationException($"{GetType().Name}: falta asignar '{fieldName}' en el Inspector.");
            }

            return reference;
        }
    }
}
