using System;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;

namespace NovaStriker.UI.Tutorial
{
    /// <summary>Conecta TutorialHintEvent con la vista (MVP + Observer). La decisión de cuándo mostrarla es de Gameplay.</summary>
    public sealed class TutorialHintPresenter : IDisposable
    {
        private readonly ITutorialHintView _view;
        private readonly IDisposable _subscription;

        public TutorialHintPresenter(ITutorialHintView view, IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            if (eventBus == null)
            {
                throw new ArgumentNullException(nameof(eventBus));
            }

            _view.Hide();
            _subscription = eventBus.Subscribe<TutorialHintEvent>(OnHint);
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }

        private void OnHint(TutorialHintEvent e)
        {
            if (e.Visible)
            {
                _view.Show(e.From, e.To);
            }
            else
            {
                _view.Hide();
            }
        }
    }
}
