using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.StateMachine;

namespace NovaStriker.Bootstrap.App
{
    /// <summary>Traduce peticiones de navegación a transiciones de la máquina de la app.</summary>
    internal sealed class AppNavigator : IAppNavigator
    {
        private readonly IStateMachine<AppStateId> _machine;
        private readonly ISceneLoader _sceneLoader;

        public AppNavigator(IStateMachine<AppStateId> machine, ISceneLoader sceneLoader)
        {
            _machine = machine;
            _sceneLoader = sceneLoader;
        }

        public void GoToMainMenu() => GoTo(AppStateId.MainMenu);

        public void GoToGameplay() => GoTo(AppStateId.Gameplay);

        private void GoTo(AppStateId target)
        {
            if (_sceneLoader.IsLoading)
            {
                return;
            }

            _machine.ChangeState(target);
        }
    }
}
