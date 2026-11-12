using NovaStriker.Bootstrap.Scenes;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.DI;
using NovaStriker.Core.StateMachine;

namespace NovaStriker.Bootstrap.App
{
    /// <summary>
    /// Construye la máquina de estados de la app con todos sus estados.
    /// La usan AppCompositionRoot y los tests, así ambos prueban el mismo cableado.
    /// </summary>
    internal sealed class AppStateMachineFactory
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly ISceneRootProvider _rootProvider;
        private readonly IServiceContainer _appContainer;

        public AppStateMachineFactory(ISceneLoader sceneLoader, ISceneRootProvider rootProvider, IServiceContainer appContainer)
        {
            _sceneLoader = sceneLoader;
            _rootProvider = rootProvider;
            _appContainer = appContainer;
        }

        public StateMachine<AppStateId> Create()
        {
            var machine = new StateMachine<AppStateId>();
            machine.Register(AppStateId.Boot, new AppBootState(machine));
            machine.Register(AppStateId.MainMenu, CreateSceneState(SceneNames.MainMenu));
            machine.Register(AppStateId.Gameplay, CreateSceneState(SceneNames.Game));
            return machine;
        }

        /// <summary>
        /// Estado inicial según la escena activa y si ya se eligió idioma. Sin idioma elegido nunca se empieza
        /// jugando: se va al menú, que muestra antes que nada la pantalla de idioma.
        /// </summary>
        public static AppStateId ResolveInitialState(string activeSceneName, bool languageChosen)
        {
            AppStateId state = ResolveInitialState(activeSceneName);
            return state == AppStateId.Gameplay && !languageChosen ? AppStateId.MainMenu : state;
        }

        /// <summary>Estado inicial según la escena activa: permite dar Play desde cualquier escena en el Editor.</summary>
        public static AppStateId ResolveInitialState(string activeSceneName)
        {
            switch (activeSceneName)
            {
                case SceneNames.MainMenu:
                    return AppStateId.MainMenu;
                case SceneNames.Game:
                    return AppStateId.Gameplay;
                default:
                    return AppStateId.Boot;
            }
        }

        private SceneAppState CreateSceneState(string sceneName)
        {
            return new SceneAppState(sceneName, _sceneLoader, _rootProvider, _appContainer);
        }
    }
}
