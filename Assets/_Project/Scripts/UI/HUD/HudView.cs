using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NovaStriker.UI.HUD
{
    /// <summary>Vista del HUD: solo pinta textos y avisa del click en pausa. El aspecto final llega en la Etapa 7.</summary>
    public sealed class HudView : MonoBehaviour, IHudView
    {
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private TMP_Text _targetsLabel;
        [SerializeField] private TMP_Text _ballsLabel;
        [SerializeField] private Button _pauseButton;

        public event Action PauseClicked;

        private void Awake()
        {
            _pauseButton.onClick.AddListener(HandlePauseClicked);
        }

        private void OnDestroy()
        {
            _pauseButton.onClick.RemoveListener(HandlePauseClicked);
        }

        public void SetLevelText(string text) => _levelLabel.text = text;

        public void SetTargetsText(string text) => _targetsLabel.text = text;

        public void SetBallsText(string text) => _ballsLabel.text = text;

        public void SetPauseVisible(bool visible) => _pauseButton.gameObject.SetActive(visible);

        private void HandlePauseClicked() => PauseClicked?.Invoke();
    }
}
