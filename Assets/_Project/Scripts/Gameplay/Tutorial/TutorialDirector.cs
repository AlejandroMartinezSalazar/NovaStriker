using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Core.Utils;
using NovaStriker.Gameplay.Level;
using UnityEngine;

namespace NovaStriker.Gameplay.Tutorial
{
    /// <summary>
    /// Decide cuándo se ve la indicación "arrastra el dedo hasta el futbolista y suelta" (Observer sobre el bus).
    /// Solo en el nivel del tutorial, mientras se puede apuntar y hasta el primer tiro de cada intento:
    /// se oculta en cuanto el jugador pone el dedo y vuelve si lo levanta sin disparar.
    /// No sabe nada de la UI: publica TutorialHintEvent y la vista se encarga del resto.
    /// </summary>
    public sealed class TutorialDirector : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly ILevelHintSource _hints;
        private readonly int _tutorialLevelIndex;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

        private bool _isTutorialLevel;
        private int _shotsFired;
        private bool _aiming;
        private GameStateId _state = GameStateId.None;
        private bool _visible;

        public TutorialDirector(IEventBus eventBus, ILevelHintSource hints, int tutorialLevelIndex = 0)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _hints = hints ?? throw new ArgumentNullException(nameof(hints));
            _tutorialLevelIndex = tutorialLevelIndex;

            _subscriptions.Add(eventBus.Subscribe<LevelStartedEvent>(OnLevelStarted));
            _subscriptions.Add(eventBus.Subscribe<GameStateChangedEvent>(OnStateChanged));
            _subscriptions.Add(eventBus.Subscribe<AimStartedEvent>(_ => SetAiming(true)));
            _subscriptions.Add(eventBus.Subscribe<AimCancelledEvent>(_ => SetAiming(false)));
            _subscriptions.Add(eventBus.Subscribe<ShotFiredEvent>(OnShotFired));
        }

        public bool IsVisible => _visible;

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private void OnLevelStarted(LevelStartedEvent e)
        {
            _isTutorialLevel = e.LevelIndex == _tutorialLevelIndex;
            _shotsFired = 0;
            _aiming = false;
            Refresh();
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            _state = e.To;
            Refresh();
        }

        private void SetAiming(bool aiming)
        {
            _aiming = aiming;
            Refresh();
        }

        private void OnShotFired(ShotFiredEvent e)
        {
            _shotsFired++;
            _aiming = false;
            Refresh();
        }

        private void Refresh()
        {
            Vector3 from = default;
            Vector3 to = default;
            bool show = _isTutorialLevel
                        && _shotsFired == 0
                        && !_aiming
                        && _state == GameStateId.Aiming
                        && _hints.TryGetAimHint(out from, out to);

            if (show == _visible)
            {
                return;
            }

            _visible = show;
            _eventBus.Publish(show ? new TutorialHintEvent(true, from, to) : TutorialHintEvent.Hidden);
        }
    }
}
