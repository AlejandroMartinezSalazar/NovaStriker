using UnityEngine;

namespace NovaStriker.UI.Tutorial
{
    /// <summary>Vista de la indicación de tiro del tutorial.</summary>
    public interface ITutorialHintView
    {
        /// <summary>Muestra el dedo arrastrando desde <paramref name="fromWorld"/> hasta <paramref name="toWorld"/> (en bucle).</summary>
        void Show(Vector3 fromWorld, Vector3 toWorld);

        void Hide();
    }
}
