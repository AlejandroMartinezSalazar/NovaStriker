namespace NovaStriker.Core.StateMachine
{
    /// <summary>Base común de todos los estados: todos saben salir.</summary>
    public interface IExitableState
    {
        void Exit();
    }

    /// <summary>Estado que entra sin datos.</summary>
    public interface IState : IExitableState
    {
        void Enter();
    }

    /// <summary>Estado que necesita datos para entrar (p. ej. LevelSetup recibe el índice de nivel).</summary>
    public interface IPayloadState<in TPayload> : IExitableState
    {
        void Enter(TPayload payload);
    }

    /// <summary>Estado que necesita actualizarse cada frame. Solo se actualiza el estado superior de la pila.</summary>
    public interface ITickableState
    {
        void Tick(float deltaTime);
    }

    /// <summary>
    /// Estado que puede quedar debajo de otro apilado (p. ej. Aiming debajo de Paused).
    /// Recibe OnSuspend al apilarse otro encima y OnResume cuando vuelve a ser el superior.
    /// No recibe Exit/Enter en esos casos.
    /// </summary>
    public interface ISuspendableState
    {
        void OnSuspend();
        void OnResume();
    }
}
