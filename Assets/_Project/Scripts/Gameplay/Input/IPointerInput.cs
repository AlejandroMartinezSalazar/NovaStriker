using System;
using UnityEngine;

namespace NovaStriker.Gameplay.Input
{
    /// <summary>Puntero único (primer dedo o ratón) en coordenadas de pantalla.</summary>
    public interface IPointerInput
    {
        event Action<Vector2> Pressed;
        event Action<Vector2> Dragged;
        event Action<Vector2> Released;
    }
}
