using System;
using NovaStriker.Contracts.Data;
using NovaStriker.UI.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace NovaStriker.UI.Settings
{
    /// <summary>Vista de ajustes: engranaje, panel, dos barras (Música, Efectos) e idioma. Solo emite eventos.</summary>
    public sealed class SettingsView : MonoBehaviour, ISettingsView
    {
        [SerializeField] private Button _openButton;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Button _closeButton;
        [Tooltip("Fila de idiomas (la añade NovaStriker/Setup/Idiomas).")]
        [SerializeField] private LanguageButtons _languageButtons;

        public event Action OpenClicked;
        public event Action CloseClicked;
        public event Action<float> MusicVolumeChanged;
        public event Action<float> SfxVolumeChanged;
        public event Action<Language> LanguageSelected;

        private void Awake()
        {
            _openButton.onClick.AddListener(HandleOpen);
            _closeButton.onClick.AddListener(HandleClose);
            _musicSlider.onValueChanged.AddListener(HandleMusic);
            _sfxSlider.onValueChanged.AddListener(HandleSfx);
            if (_languageButtons != null)
            {
                _languageButtons.Selected += HandleLanguage;
            }
        }

        private void OnDestroy()
        {
            _openButton.onClick.RemoveListener(HandleOpen);
            _closeButton.onClick.RemoveListener(HandleClose);
            _musicSlider.onValueChanged.RemoveListener(HandleMusic);
            _sfxSlider.onValueChanged.RemoveListener(HandleSfx);
            if (_languageButtons != null)
            {
                _languageButtons.Selected -= HandleLanguage;
            }
        }

        public void ShowPanel(float musicVolume, float sfxVolume)
        {
            _musicSlider.SetValueWithoutNotify(musicVolume);
            _sfxSlider.SetValueWithoutNotify(sfxVolume);
            _panel.SetActive(true);
        }

        public void HidePanel() => _panel.SetActive(false);

        public void SetSelectedLanguage(Language language)
        {
            if (_languageButtons != null)
            {
                _languageButtons.SetSelected(language);
            }
        }

        private void HandleLanguage(Language language) => LanguageSelected?.Invoke(language);

        private void HandleOpen() => OpenClicked?.Invoke();

        private void HandleClose() => CloseClicked?.Invoke();

        private void HandleMusic(float value) => MusicVolumeChanged?.Invoke(value);

        private void HandleSfx(float value) => SfxVolumeChanged?.Invoke(value);
    }
}
