using NovaStriker.Core.Events;
using UnityEngine;

namespace NovaStriker.Contracts.Events
{
    /// <summary>
    /// Mostrar u ocultar la indicación de tiro del tutorial: un dedo que arrastra desde <see cref="From"/>
    /// (punto de saque) hasta <see cref="To"/> (el futbolista) y suelta. Posiciones en el mundo.
    /// </summary>
    public readonly struct TutorialHintEvent : IGameEvent
    {
        public readonly bool Visible;
        public readonly Vector3 From;
        public readonly Vector3 To;

        public TutorialHintEvent(bool visible, Vector3 from, Vector3 to)
        {
            Visible = visible;
            From = from;
            To = to;
        }

        public static TutorialHintEvent Hidden => new TutorialHintEvent(false, Vector3.zero, Vector3.zero);
    }
}
