using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Services;

namespace NovaStriker.UI.MainMenu
{
    /// <summary>
    /// Menú principal: muestra el nivel actual y su grupo de 10 niveles, y Jugar lleva a la partida.
    /// Si ya se superaron todos los niveles muestra "Próximamente"; "Empezar desde el nivel 1" reinicia
    /// el progreso y vuelve al menú normal (D1).
    /// </summary>
    public sealed class MainMenuPresenter : IDisposable
    {
        private readonly IMainMenuView _view;
        private readonly IAppNavigator _navigator;
        private readonly IProgressService _progress;
        private readonly ILocalization _localization;
        private readonly int _levelCount;
        private readonly int _groupSize;
        private readonly List<LevelSquareState> _squares = new List<LevelSquareState>();

        public MainMenuPresenter(IMainMenuView view, IAppNavigator navigator, IProgressService progress, int levelCount,
            ILocalization localization, int groupSize = LevelProgressGroup.DefaultGroupSize)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            if (levelCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(levelCount), "El juego necesita al menos un nivel.");
            }

            _levelCount = levelCount;
            _groupSize = groupSize;

            _view.PlayClicked += OnPlayClicked;
            _view.RestartCampaignClicked += OnRestartCampaignClicked;
            _localization.LanguageChanged += Refresh;
            Refresh();
        }

        public bool IsCampaignCompleted => _progress.CurrentLevelIndex >= _levelCount;

        public string FormatLevel(int levelNumber) => _localization.Format(TextKey.MenuLevel, levelNumber);

        public void Dispose()
        {
            _view.PlayClicked -= OnPlayClicked;
            _view.RestartCampaignClicked -= OnRestartCampaignClicked;
            _localization.LanguageChanged -= Refresh;
        }

        private void Refresh()
        {
            if (IsCampaignCompleted)
            {
                _view.ShowComingSoon();
                return;
            }

            int current = _progress.CurrentLevelIndex;
            LevelProgressGroup.Build(current, _levelCount, _groupSize, _squares);
            _view.ShowLevel(FormatLevel(current + 1), _squares);
        }

        private void OnPlayClicked()
        {
            if (!IsCampaignCompleted)
            {
                _navigator.GoToGameplay();
            }
        }

        private void OnRestartCampaignClicked()
        {
            _progress.RestartCampaign();
            Refresh();
        }
    }
}
