using UnityEngine;
using UnityEngine.UI;

namespace NovaStriker.UI.Tutorial
{
    /// <summary>
    /// Dedo animado que enseña a tirar: se apoya junto al balón, se arrastra hasta el futbolista dejando un rastro
    /// de puntos y suelta, con un anillo de toque al presionar y al soltar. Encima, un cartel con el texto.
    /// Las posiciones vienen del mundo y se convierten a la pantalla cada frame (la cámara o el banner pueden
    /// cambiar). Usa tiempo sin escalar. Nada de esto recibe toques: el jugador puede apuntar a través de él.
    /// </summary>
    public sealed class TutorialHintView : MonoBehaviour, ITutorialHintView
    {
        [SerializeField] private Camera _camera;
        [Tooltip("Zona (a pantalla completa) en la que se colocan el dedo, el anillo y los puntos.")]
        [SerializeField] private RectTransform _area;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _hand;
        [SerializeField] private Image _handImage;
        [SerializeField] private RectTransform _ring;
        [SerializeField] private Image _ringImage;
        [SerializeField] private Image[] _trailDots = new Image[0];

        [Tooltip("Separación (px de la referencia 1080×1920) entre el balón y donde se apoya el dedo, hacia arriba.")]
        [SerializeField] private float _startOffset = 140f;

        private Vector3 _fromWorld;
        private Vector3 _toWorld;
        private float _time;
        private bool _visible;

        public void Show(Vector3 fromWorld, Vector3 toWorld)
        {
            _fromWorld = fromWorld;
            _toWorld = toWorld;
            _time = 0f;
            _visible = true;
            gameObject.SetActive(true);
            _group.alpha = 1f;
            Apply();
        }

        public void Hide()
        {
            _visible = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_visible)
            {
                return;
            }

            _time += Time.unscaledDeltaTime;
            Apply();
        }

        private void Apply()
        {
            if (!ToArea(_fromWorld, out Vector2 start) || !ToArea(_toWorld, out Vector2 end))
            {
                _group.alpha = 0f;
                return;
            }

            _group.alpha = 1f;
            start += Vector2.up * _startOffset;
            DragHintAnimation.Frame frame = DragHintAnimation.Evaluate(_time);

            Vector2 finger = Vector2.Lerp(start, end, frame.Progress);
            _hand.anchoredPosition = finger;
            _hand.localScale = Vector3.one * frame.HandScale;
            SetAlpha(_handImage, frame.HandAlpha);

            _ring.anchoredPosition = frame.RingAtEnd ? end : start;
            _ring.localScale = Vector3.one * frame.RingScale;
            SetAlpha(_ringImage, frame.RingAlpha);

            // Rastro: cada punto aparece cuando el dedo lo pasa y se apaga con el dedo.
            for (int i = 0; i < _trailDots.Length; i++)
            {
                Image dot = _trailDots[i];
                if (dot == null)
                {
                    continue;
                }

                float at = (i + 1f) / (_trailDots.Length + 1f);
                dot.rectTransform.anchoredPosition = Vector2.Lerp(start, end, at);
                SetAlpha(dot, frame.Progress >= at ? 0.85f * frame.HandAlpha : 0f);
            }
        }

        /// <summary>Mundo → píxeles de pantalla → coordenadas locales de <see cref="_area"/>.</summary>
        private bool ToArea(Vector3 world, out Vector2 local)
        {
            local = default;
            Vector3 screen = _camera.WorldToScreenPoint(world);
            if (screen.z <= 0f)
            {
                return false; // Detrás de la cámara.
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, screen, null, out local);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
