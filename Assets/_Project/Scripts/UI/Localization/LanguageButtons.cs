using System;
using NovaStriker.Contracts.Data;
using UnityEngine;
using UnityEngine.UI;

namespace NovaStriker.UI.Localization
{
    /// <summary>
    /// Fila de tres botones (Español, English, Português) con el idioma actual resaltado.
    /// Los nombres van siempre en su propio idioma, para que cada jugador reconozca el suyo.
    /// La usan la pantalla de elegir idioma y el panel de ajustes.
    /// </summary>
    public sealed class LanguageButtons : MonoBehaviour
    {
        [SerializeField] private Button _spanish;
        [SerializeField] private Button _english;
        [SerializeField] private Button _portuguese;
        [SerializeField] private Color _selectedColor = new Color(0.24f, 0.76f, 0.24f);
        [SerializeField] private Color _normalColor = new Color(0.12f, 0.43f, 0.55f);

        public event Action<Language> Selected;

        private void Awake()
        {
            _spanish.onClick.AddListener(SelectSpanish);
            _english.onClick.AddListener(SelectEnglish);
            _portuguese.onClick.AddListener(SelectPortuguese);
        }

        private void OnDestroy()
        {
            _spanish.onClick.RemoveListener(SelectSpanish);
            _english.onClick.RemoveListener(SelectEnglish);
            _portuguese.onClick.RemoveListener(SelectPortuguese);
        }

        public void SetSelected(Language language)
        {
            Paint(_spanish, language == Language.Spanish);
            Paint(_english, language == Language.English);
            Paint(_portuguese, language == Language.Portuguese);
        }

        private void Paint(Button button, bool selected)
        {
            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = selected ? _selectedColor : _normalColor;
            }
        }

        private void SelectSpanish() => Selected?.Invoke(Language.Spanish);

        private void SelectEnglish() => Selected?.Invoke(Language.English);

        private void SelectPortuguese() => Selected?.Invoke(Language.Portuguese);
    }
}
