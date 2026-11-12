using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NovaStriker.UI.Results
{
    /// <summary>
    /// Panel superpuesto de pausa y derrota: cartel con título, mensaje y hasta tres botones.
    /// La vista vive en un objeto siempre activo y enciende/apaga _panelRoot (así Awake siempre se ejecuta).
    /// </summary>
    public sealed class ResultPanelView : MonoBehaviour, IResultPanelView
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Image _banner;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _messageLabel;
        [SerializeField] private Button _primaryButton;
        [SerializeField] private TMP_Text _primaryLabel;
        [SerializeField] private Button _secondaryButton;
        [SerializeField] private TMP_Text _secondaryLabel;
        [SerializeField] private Button _menuButton;

        [Header("Colores del cartel")]
        [SerializeField] private Color _neutralColor = new Color(0.12f, 0.43f, 0.55f);
        [SerializeField] private Color _negativeColor = new Color(0.78f, 0.24f, 0.2f);

        public event Action PrimaryClicked;
        public event Action SecondaryClicked;
        public event Action MenuClicked;

        private void Awake()
        {
            _primaryButton.onClick.AddListener(HandlePrimary);
            _secondaryButton.onClick.AddListener(HandleSecondary);
            _menuButton.onClick.AddListener(HandleMenu);
        }

        private void OnDestroy()
        {
            _primaryButton.onClick.RemoveListener(HandlePrimary);
            _secondaryButton.onClick.RemoveListener(HandleSecondary);
            _menuButton.onClick.RemoveListener(HandleMenu);
        }

        public void Show(in ResultPanelContent content)
        {
            _titleLabel.text = content.Title;
            _messageLabel.text = content.Message ?? string.Empty;
            _banner.color = content.Style == ResultPanelStyle.Negative ? _negativeColor : _neutralColor;
            SetButton(_primaryButton, _primaryLabel, content.PrimaryLabel, true);
            SetButton(_secondaryButton, _secondaryLabel, content.SecondaryLabel, content.SecondaryEnabled);
            _panelRoot.SetActive(true);
        }

        public void Hide() => _panelRoot.SetActive(false);

        private static void SetButton(Button button, TMP_Text label, string text, bool interactable)
        {
            bool visible = !string.IsNullOrEmpty(text);
            button.gameObject.SetActive(visible);
            if (visible)
            {
                label.text = text;
                button.interactable = interactable;
            }
        }

        private void HandlePrimary() => PrimaryClicked?.Invoke();

        private void HandleSecondary() => SecondaryClicked?.Invoke();

        private void HandleMenu() => MenuClicked?.Invoke();
    }
}
