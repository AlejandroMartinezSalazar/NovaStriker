using System;
using NovaStriker.Core.Loop;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NovaStriker.Gameplay.Input
{
    /// <summary>
    /// Lee el puntero con el Input System. El binding &lt;Pointer&gt; cubre el táctil (dedo principal) y el ratón.
    /// Las acciones se crean en código: no depende del asset InputSystem_Actions del proyecto.
    /// Se consulta en Tick (sondeo), no en callbacks, para que la detección de UI sea fiable.
    /// </summary>
    public sealed class PointerInputReader : IPointerInput, ITickable, IDisposable
    {
        private readonly InputAction _pressAction;
        private readonly InputAction _positionAction;
        private Vector2 _lastPosition;
        private bool _isDisposed;

        public PointerInputReader()
        {
            _pressAction = new InputAction("NovaStriker.PointerPress", InputActionType.Button, "<Pointer>/press");
            _positionAction = new InputAction("NovaStriker.PointerPosition", InputActionType.Value, "<Pointer>/position");
            _pressAction.Enable();
            _positionAction.Enable();
        }

        public event Action<Vector2> Pressed;
        public event Action<Vector2> Dragged;
        public event Action<Vector2> Released;

        public void Tick(float deltaTime)
        {
            if (_isDisposed)
            {
                return;
            }

            Vector2 position = _positionAction.ReadValue<Vector2>();

            if (_pressAction.WasPressedThisFrame())
            {
                _lastPosition = position;
                Pressed?.Invoke(position);
            }
            else if (_pressAction.IsPressed() && position != _lastPosition)
            {
                _lastPosition = position;
                Dragged?.Invoke(position);
            }

            // Un toque muy rápido puede empezar y terminar en el mismo frame: se emiten ambos, en orden.
            if (_pressAction.WasReleasedThisFrame())
            {
                Released?.Invoke(position);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _pressAction.Disable();
            _positionAction.Disable();
            _pressAction.Dispose();
            _positionAction.Dispose();
        }
    }
}
