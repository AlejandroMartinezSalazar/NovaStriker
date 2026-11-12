using NovaStriker.Bootstrap.Scenes;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.DI;
using NovaStriker.Core.StateMachine;
using UnityEngine;

namespace NovaStriker.Bootstrap.App
{
    /// <summary>
    /// Estado de la app ligado a una escena (MainMenu, Gameplay).
    /// Enter: carga la escena y compone su raíz. Exit: libera la raíz.
    /// Si la escena ya está activa y su raíz sin componer (Play directo desde esa escena en el Editor),
    /// la compone sin recargar.
    /// </summary>
    internal sealed class SceneAppState : IState
    {
        private readonly string _sceneName;
        private readonly ISceneLoader _sceneLoader;
        private readonly ISceneRootProvider _rootProvider;
        private readonly IServiceContainer _appContainer;

        private ISceneCompositionRoot _root;
        private int _visit;

        public SceneAppState(
            string sceneName,
            ISceneLoader sceneLoader,
            ISceneRootProvider rootProvider,
            IServiceContainer appContainer)
        {
            _sceneName = sceneName;
            _sceneLoader = sceneLoader;
            _rootProvider = rootProvider;
            _appContainer = appContainer;
        }

        public void Enter()
        {
            int visit = ++_visit;

            if (_sceneLoader.ActiveSceneName == _sceneName
                && _rootProvider.TryGetRoot(_sceneName, out ISceneCompositionRoot existingRoot)
                && existingRoot.CanCompose)
            {
                ComposeRoot(existingRoot);
                return;
            }

            _sceneLoader.LoadScene(_sceneName, () => OnSceneLoaded(visit));
        }

        public void Exit()
        {
            _visit++; // Invalida cualquier carga pendiente de esta visita.
            _root?.Dispose();
            _root = null;
        }

        private void OnSceneLoaded(int visit)
        {
            if (visit != _visit)
            {
                return; // Se salió del estado antes de que terminara la carga.
            }

            if (!_rootProvider.TryGetRoot(_sceneName, out ISceneCompositionRoot root))
            {
                Debug.LogError($"[SceneAppState] La escena '{_sceneName}' no tiene un objeto raíz con ISceneCompositionRoot.");
                return;
            }

            ComposeRoot(root);
        }

        private void ComposeRoot(ISceneCompositionRoot root)
        {
            _root = root;
            root.Compose(_appContainer);
        }
    }
}
