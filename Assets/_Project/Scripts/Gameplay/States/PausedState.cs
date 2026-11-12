using System;
using NovaStriker.Core.Loop;
using NovaStriker.Core.StateMachine;

namespace NovaStriker.Gameplay.States
{
    /// <summary>
    /// Se apila encima de Aiming, BallInFlight o ShotResolve. Congela el tiempo (física incluida)
    /// y lo restaura al salir, también si se sale de la escena en pausa.
    /// </summary>
    public sealed class PausedState : IState
    {
        private readonly ITimeScale _timeScale;
        private float _previousScale = 1f;

        public PausedState(ITimeScale timeScale)
        {
            _timeScale = timeScale ?? throw new ArgumentNullException(nameof(timeScale));
        }

        public void Enter()
        {
            _previousScale = _timeScale.Scale > 0f ? _timeScale.Scale : 1f;
            _timeScale.Scale = 0f;
        }

        public void Exit()
        {
            _timeScale.Scale = _previousScale;
        }
    }
}
