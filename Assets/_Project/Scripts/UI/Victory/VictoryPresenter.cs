using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Events;
using NovaStriker.Core.Utils;

namespace NovaStriker.UI.Victory
{
    /// <summary>Muestra la victoria al entrar en ese estado ("NIVEL N COMPLETADO") y la oculta en cualquier otro.</summary>
    public sealed class VictoryPresenter : IDisposable
    {
        private readonly IVictoryView _view;
        private readonly ILocalization _localization;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private int _levelNumber;

        public VictoryPresenter(IVictoryView view, IEventBus eventBus, ILocalization localization)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            if (eventBus == null)
            {
                throw new ArgumentNullException(nameof(eventBus));
            }

            _subscriptions.Add(eventBus.Subscribe<LevelStartedEvent>(e => _levelNumber = e.LevelNumber));
            _subscriptions.Add(eventBus.Subscribe<GameStateChangedEvent>(OnStateChanged));
            _view.Hide();
        }

        public string FormatSubtitle(int levelNumber) => _localization.Format(TextKey.VictorySubtitle, levelNumber);

        public void Dispose() => _subscriptions.Dispose();

        private void OnStateChanged(GameStateChangedEvent e)
        {
            if (e.To == GameStateId.Victory)
            {
                _view.Show(FormatSubtitle(_levelNumber));
            }
            else
            {
                _view.Hide();
            }
        }
    }
}
