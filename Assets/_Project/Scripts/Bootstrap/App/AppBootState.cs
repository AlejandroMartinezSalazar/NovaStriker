using NovaStriker.Contracts.Data;
using NovaStriker.Core.StateMachine;

namespace NovaStriker.Bootstrap.App
{
    /// <summary>
    /// Arranque de la app. En etapas posteriores aquí se cargará el progreso y se inicializarán los anuncios.
    /// Por ahora pasa directamente al menú principal.
    /// </summary>
    internal sealed class AppBootState : IState
    {
        private readonly IStateMachine<AppStateId> _machine;

        public AppBootState(IStateMachine<AppStateId> machine)
        {
            _machine = machine;
        }

        public void Enter()
        {
            // Se encola: la máquina la ejecuta en cuanto termine esta transición.
            _machine.ChangeState(AppStateId.MainMenu);
        }

        public void Exit()
        {
        }
    }
}
