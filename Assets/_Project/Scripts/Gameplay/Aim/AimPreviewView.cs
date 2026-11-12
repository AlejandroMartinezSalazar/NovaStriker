using UnityEngine;

namespace NovaStriker.Gameplay.Aim
{
    /// <summary>Dibuja la trayectoria con un LineRenderer y coloca un marcador en el punto objetivo.</summary>
    public sealed class AimPreviewView : MonoBehaviour, IAimPreview
    {
        private const float MarkerLift = 0.02f;

        [SerializeField] private LineRenderer _line;
        [SerializeField] private Transform _marker;

        private void Awake()
        {
            Hide();
        }

        public void Show(Vector3[] points, int count, Vector3 target)
        {
            _line.enabled = true;
            _line.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                _line.SetPosition(i, points[i]);
            }

            _marker.gameObject.SetActive(true);
            _marker.position = target + Vector3.up * MarkerLift;
        }

        public void Hide()
        {
            _line.enabled = false;
            _line.positionCount = 0;
            _marker.gameObject.SetActive(false);
        }
    }
}
