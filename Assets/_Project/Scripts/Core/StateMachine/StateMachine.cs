using System;
using System.Collections.Generic;

namespace NovaStriker.Core.StateMachine
{
    /// <summary>
    /// Implementación de IStateMachine.
    /// Si un estado pide una transición dentro de su Enter/Exit/OnSuspend/OnResume, la transición se encola
    /// y se ejecuta cuando termina la actual. Así el orden de llamadas siempre es consistente.
    /// </summary>
    public sealed class StateMachine<TId> : IStateMachine<TId> where TId : struct, Enum
    {
        private readonly struct StackEntry
        {
            public readonly TId Id;
            public readonly IExitableState State;

            public StackEntry(TId id, IExitableState state)
            {
                Id = id;
                State = state;
            }
        }

        private static readonly EqualityComparer<TId> IdComparer = EqualityComparer<TId>.Default;

        private readonly Dictionary<TId, IExitableState> _states = new Dictionary<TId, IExitableState>();
        private readonly List<StackEntry> _stack = new List<StackEntry>();
        private readonly Queue<Action> _pendingTransitions = new Queue<Action>();
        private bool _isTransitioning;

        public event Action<TId, TId> StateChanged;

        public TId CurrentId => _stack.Count > 0 ? _stack[_stack.Count - 1].Id : default;
        public bool HasState => _stack.Count > 0;
        public int Depth => _stack.Count;

        public void Register(TId id, IExitableState state)
        {
            if (IdComparer.Equals(id, default))
            {
                throw new ArgumentException($"El valor 0 de {typeof(TId).Name} está reservado como None.", nameof(id));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (_states.ContainsKey(id))
            {
                throw new InvalidOperationException($"El estado {id} ya está registrado.");
            }

            _states.Add(id, state);
        }

        public bool Contains(TId id)
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                if (IdComparer.Equals(_stack[i].Id, id))
                {
                    return true;
                }
            }
            return false;
        }

        public void ChangeState(TId id)
        {
            IState state = GetState<IState>(id);
            Run(() => ReplaceStack(id, state, () => state.Enter()));
        }

        public void ChangeState<TPayload>(TId id, TPayload payload)
        {
            IPayloadState<TPayload> state = GetState<IPayloadState<TPayload>>(id);
            Run(() => ReplaceStack(id, state, () => state.Enter(payload)));
        }

        public void Push(TId id)
        {
            IState state = GetState<IState>(id);
            Run(() =>
            {
                if (_stack.Count == 0)
                {
                    throw new InvalidOperationException($"No se puede apilar {id}: la máquina no tiene estado base.");
                }

                if (Contains(id))
                {
                    throw new InvalidOperationException($"El estado {id} ya está en la pila.");
                }

                TId from = CurrentId;
                (_stack[_stack.Count - 1].State as ISuspendableState)?.OnSuspend();
                _stack.Add(new StackEntry(id, state));
                state.Enter();
                StateChanged?.Invoke(from, id);
            });
        }

        public void Pop()
        {
            Run(() =>
            {
                if (_stack.Count < 2)
                {
                    throw new InvalidOperationException("No hay un estado apilado que quitar.");
                }

                StackEntry top = _stack[_stack.Count - 1];
                top.State.Exit();
                _stack.RemoveAt(_stack.Count - 1);

                StackEntry resumed = _stack[_stack.Count - 1];
                (resumed.State as ISuspendableState)?.OnResume();
                StateChanged?.Invoke(top.Id, resumed.Id);
            });
        }

        public void Tick(float deltaTime)
        {
            if (_isTransitioning || _stack.Count == 0)
            {
                return;
            }

            (_stack[_stack.Count - 1].State as ITickableState)?.Tick(deltaTime);
        }

        public void Stop()
        {
            Run(() =>
            {
                if (_stack.Count == 0)
                {
                    return;
                }

                TId from = CurrentId;
                ExitAll();
                StateChanged?.Invoke(from, default);
            });
        }

        private T GetState<T>(TId id) where T : class
        {
            if (!_states.TryGetValue(id, out IExitableState registered))
            {
                throw new InvalidOperationException($"El estado {id} no está registrado.");
            }

            if (!(registered is T typed))
            {
                throw new InvalidOperationException(
                    $"El estado {id} ({registered.GetType().Name}) no implementa {typeof(T).Name}.");
            }

            return typed;
        }

        private void ReplaceStack(TId id, IExitableState state, Action enter)
        {
            TId from = CurrentId;
            ExitAll();
            _stack.Add(new StackEntry(id, state));
            enter();
            StateChanged?.Invoke(from, id);
        }

        private void ExitAll()
        {
            // De arriba hacia abajo: el overlay sale antes que el estado que tapaba.
            while (_stack.Count > 0)
            {
                int last = _stack.Count - 1;
                IExitableState state = _stack[last].State;
                _stack.RemoveAt(last);
                state.Exit();
            }
        }

        private void Run(Action transition)
        {
            if (_isTransitioning)
            {
                _pendingTransitions.Enqueue(transition);
                return;
            }

            _isTransitioning = true;
            try
            {
                transition();
                while (_pendingTransitions.Count > 0)
                {
                    _pendingTransitions.Dequeue()();
                }
            }
            catch
            {
                _pendingTransitions.Clear();
                throw;
            }
            finally
            {
                _isTransitioning = false;
            }
        }
    }
}
