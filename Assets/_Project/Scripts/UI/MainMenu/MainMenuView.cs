using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NovaStriker.UI.MainMenu
{
    /// <summary>
    /// Vista del menú principal. Pinta el panel de nivel (texto + cuadraditos no interactuables) y alterna
    /// entre el menú normal y la pantalla "Próximamente". Solo emite clicks.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour, IMainMenuView
    {
        [Header("Menú")]
        [SerializeField] private GameObject _menuScreen;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private Image[] _squares;
        [SerializeField] private Button _playButton;

        [Header("Próximamente")]
        [SerializeField] private GameObject _comingSoonScreen;
        [SerializeField] private Button _restartCampaignButton;

        [Header("Colores de los cuadraditos")]
        [SerializeField] private Color _completedColor = new Color(0.97f, 0.72f, 0.19f);
        [SerializeField] private Color _currentColor = new Color(0.3f, 0.85f, 0.39f);
        [SerializeField] private Color _lockedColor = new Color(0.08f, 0.27f, 0.37f);
        [Tooltip("Escala vertical del cuadradito del nivel actual (resalta un poco más).")]
        [SerializeField, Min(1f)] private float _currentHeightScale = 1.3f;

        public event Action PlayClicked;
        public event Action RestartCampaignClicked;

        private void Awake()
        {
            _playButton.onClick.AddListener(HandlePlayClicked);
            _restartCampaignButton.onClick.AddListener(HandleRestartClicked);
        }

        private void OnDestroy()
        {
            _playButton.onClick.RemoveListener(HandlePlayClicked);
            _restartCampaignButton.onClick.RemoveListener(HandleRestartClicked);
        }

        public void ShowLevel(string levelText, IReadOnlyList<LevelSquareState> squares)
        {
            _comingSoonScreen.SetActive(false);
            _menuScreen.SetActive(true);
            _levelLabel.text = levelText;

            for (int i = 0; i < _squares.Length; i++)
            {
                Image square = _squares[i];
                bool visible = i < squares.Count;
                square.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                LevelSquareState state = squares[i];
                square.color = state == LevelSquareState.Completed ? _completedColor
                    : state == LevelSquareState.Current ? _currentColor
                    : _lockedColor;
                square.rectTransform.localScale = new Vector3(1f, state == LevelSquareState.Current ? _currentHeightScale : 1f, 1f);
            }
        }

        public void ShowComingSoon()
        {
            _menuScreen.SetActive(false);
            _comingSoonScreen.SetActive(true);
        }

        private void HandlePlayClicked() => PlayClicked?.Invoke();

        private void HandleRestartClicked() => RestartCampaignClicked?.Invoke();
    }
}
