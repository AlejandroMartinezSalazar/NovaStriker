using System;
using NovaStriker.Contracts.Data;
using UnityEngine;

namespace NovaStriker.UI.Localization
{
    /// <summary>Vista de la pantalla de idioma: un panel a pantalla completa con la fila de botones de idioma.</summary>
    public sealed class LanguagePickerView : MonoBehaviour, ILanguagePickerView
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private LanguageButtons _buttons;

        public event Action<Language> LanguageSelected;

        private void Awake() => _buttons.Selected += HandleSelected;

        private void OnDestroy() => _buttons.Selected -= HandleSelected;

        public void Show(Language preselected)
        {
            _buttons.SetSelected(preselected);
            _panel.SetActive(true);
        }

        public void Hide() => _panel.SetActive(false);

        private void HandleSelected(Language language) => LanguageSelected?.Invoke(language);
    }
}
