using System;
using UnityEngine;

namespace NovaStriker.Gameplay.Level.Rules
{
    /// <summary>Tiempos de la partida: cuánto se espera a que la física se calme tras cada tiro.</summary>
    [Serializable]
    public struct GameFlowSettings
    {
        [Tooltip("Segundos desde un tiro hasta que aparece el siguiente balón (deja que el anterior se aleje del punto de saque).")]
        [Min(0.05f)] public float ReloadDelay;

        [Tooltip("Espera mínima (s) tras detenerse el balón antes de decidir el resultado del tiro.")]
        [Min(0f)] public float MinResolveTime;

        [Tooltip("Segundos sin ningún derribo nuevo para dar la escena por calmada.")]
        [Min(0f)] public float QuietTime;

        [Tooltip("Espera máxima (s): pasado este tiempo se decide aunque algo se siga moviendo.")]
        [Min(0f)] public float MaxResolveTime;

        [Tooltip("Segundos que se muestra la pantalla de victoria antes de volver al menú principal.")]
        [Min(0.5f)] public float VictoryScreenTime;

        [Tooltip("Seguridad: si tras el último tiro los balones no avisan de que terminaron en este tiempo (s), se pasa a resolver.")]
        [Min(1f)] public float MaxBallFlightTime;

        public static GameFlowSettings Default => new GameFlowSettings
        {
            ReloadDelay = 0.25f,
            MinResolveTime = 0.75f,
            QuietTime = 1f,
            MaxResolveTime = 3f,
            MaxBallFlightTime = 12f,
            VictoryScreenTime = 3f,
        };
    }
}
