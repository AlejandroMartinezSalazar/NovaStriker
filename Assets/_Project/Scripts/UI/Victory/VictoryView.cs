using NovaStriker.UI.Common;
using TMPro;
using UnityEngine;

namespace NovaStriker.UI.Victory
{
    /// <summary>
    /// Pantalla de victoria: el cartel aparece con un "pop", los balones de encima saltan y el subtítulo
    /// aparece justo después; luego el cartel "respira" suavemente. Usa tiempo no escalado.
    /// </summary>
    public sealed class VictoryView : MonoBehaviour, IVictoryView
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _banner;
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private RectTransform[] _balls;

        [Header("Animación")]
        [SerializeField, Min(0.05f)] private float _popDuration = 0.45f;
        [SerializeField, Min(0f)] private float _subtitleDelay = 0.3f;
        [SerializeField, Min(0f)] private float _ballJumpHeight = 40f;

        private Vector2[] _ballRestPositions;
        private float _time;
        private bool _isPlaying;

        private void Awake() => CacheBallPositions();

        /// <summary>
        /// Guarda la posición de reposo de cada balón. Se rehace si cambia el número de balones
        /// (p. ej. al editar la vista con Play Mode sin recargar escena) para no salirse del array.
        /// </summary>
        private void CacheBallPositions()
        {
            _balls ??= new RectTransform[0];
            if (_ballRestPositions != null && _ballRestPositions.Length == _balls.Length)
            {
                return;
            }

            _ballRestPositions = new Vector2[_balls.Length];
            for (int i = 0; i < _balls.Length; i++)
            {
                _ballRestPositions[i] = _balls[i] != null ? _balls[i].anchoredPosition : Vector2.zero;
            }
        }

        public void Show(string subtitle)
        {
            CacheBallPositions();
            _subtitle.text = subtitle;
            _time = 0f;
            _isPlaying = true;
            _root.SetActive(true);
            Apply();
        }

        public void Hide()
        {
            _isPlaying = false;
            _root.SetActive(false);
        }

        private void Update()
        {
            if (!_isPlaying)
            {
                return;
            }

            _time += Time.unscaledDeltaTime;
            Apply();
        }

        private void Apply()
        {
            float pop = UiEasing.BackOut(_time / _popDuration);
            float breathe = _time > _popDuration ? 1f + 0.025f * Mathf.Sin((_time - _popDuration) * 4f) : 1f;
            _banner.localScale = Vector3.one * (pop * breathe);

            CacheBallPositions();
            for (int i = 0; i < _balls.Length; i++)
            {
                if (_balls[i] == null)
                {
                    continue;
                }

                float jump = UiEasing.DampedBounce(_time - _popDuration * 0.5f - i * 0.08f, 2.2f, 1.2f);
                _balls[i].anchoredPosition = _ballRestPositions[i] + new Vector2(0f, jump * _ballJumpHeight);
            }

            Color color = _subtitle.color;
            color.a = Mathf.Clamp01((_time - _subtitleDelay) / 0.25f);
            _subtitle.color = color;
        }
    }
}
