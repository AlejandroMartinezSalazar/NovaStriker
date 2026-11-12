using System;

namespace NovaStriker.Core.StateMachine
{
    /// <summary>
    /// Máquina de estados con pila (pushdown automaton).
    /// Convención: el valor 0 del enum de identificadores se reserva como "None" y no se puede registrar.
    /// </summary>
    public interface IStateMachine<TId> where TId : struct, Enum
    {
        /// <summary>Estado superior de la pila, o el valor 0 (None) si la máquina está detenida.</summary>
        TId CurrentId { get; }

        bool HasState { get; }

        /// <summary>Número de estados apilados (1 en uso normal, 2 o más con overlays como Paused).</summary>
        int Depth { get; }

        /// <summary>Se dispara tras cada transición con (desde, hacia). None indica que no había o no queda estado.</summary>
        event Action<TId, TId> StateChanged;

        void Register(TId id, IExitableState state);

        bool Contains(TId id);

        /// <summary>Sale de todos los estados apilados y entra en <paramref name="id"/>. Volver a entrar en el estado actual está permitido.</summary>
        void ChangeState(TId id);

        void ChangeState<TPayload>(TId id, TPayload payload);

        /// <summary>Apila un estado encima del actual sin hacerle Exit (overlay).</summary>
        void Push(TId id);

        /// <summary>Sale del estado superior y reanuda el de abajo. No se puede quitar el estado base.</summary>
        void Pop();

        void Tick(float deltaTime);

        /// <summary>Sale de todos los estados. La máquina queda vacía y reutilizable.</summary>
        void Stop();
    }
}
