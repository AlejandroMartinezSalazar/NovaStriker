using UnityEngine;

namespace NovaStriker.UI.Common
{
    /// <summary>
    /// Ajusta este RectTransform a Screen.safeArea (muescas, cámara frontal, barra de gestos).
    /// Debe ser hijo de algo que ocupe toda la pantalla (el Canvas o un panel a pantalla completa).
    /// Además puede reservar una franja superior para el banner de anuncios (SetTopInset).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _appliedArea;
        private Vector2Int _appliedScreen;
        private float _topInsetPixels;
        private float _appliedTopInset = -1f;

        private void OnEnable()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _appliedArea || Screen.width != _appliedScreen.x || Screen.height != _appliedScreen.y
                || _topInsetPixels != _appliedTopInset)
            {
                Apply();
            }
        }

        /// <summary>Reserva una franja arriba, bajo la zona segura (el banner de anuncios). La pone la composición.</summary>
        public void SetTopInset(float pixels)
        {
            _topInsetPixels = Mathf.Max(0f, pixels);
            if (isActiveAndEnabled && _rect != null)
            {
                Apply();
            }
        }

        private void Apply()
        {
            _appliedArea = Screen.safeArea;
            _appliedScreen = new Vector2Int(Screen.width, Screen.height);
            _appliedTopInset = _topInsetPixels;
            SafeAreaMath.ToAnchors(_appliedArea, new Vector2(Screen.width, Screen.height), out Vector2 min, out Vector2 max, _topInsetPixels);
            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
