using UnityEngine;

namespace NovaStriker.Gameplay.Aim
{
    /// <summary>Visualización del apuntado: trayectoria y marcador en el objetivo.</summary>
    public interface IAimPreview
    {
        void Show(Vector3[] points, int count, Vector3 target);
        void Hide();
    }
}
