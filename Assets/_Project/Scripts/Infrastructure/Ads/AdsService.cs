using System;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Events;
using UnityEngine;

namespace NovaStriker.Infrastructure.Ads
{
    /// <summary>
    /// IAdsService: política de anuncios sobre un IAdsProvider. Muestra el banner al iniciar, mantiene
    /// intersticial y recompensado precargados (recarga tras cada uso) y avisa por el bus cuando hay un
    /// anuncio a pantalla completa (el audio se silencia) y de cada anuncio mostrado (analíticas).
    /// </summary>
    public sealed class AdsService : IAdsService, IDisposable
    {
        private readonly IAdsProvider _provider;
        private readonly InterstitialPolicy _policy;
        private readonly IEventBus _eventBus;
        private bool _isShowingFullscreen;

        public AdsService(IAdsProvider provider, InterstitialPolicy policy, IEventBus eventBus)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _provider.BannerChanged += OnBannerChanged;
        }

        public event Action BannerLayoutChanged;

        public float BannerHeightPixels => _provider.BannerHeightPixels;
        public bool IsRewardedReady => !_isShowingFullscreen && _provider.IsRewardedReady;

        /// <summary>Inicializa el SDK; al terminar muestra el banner y precarga los anuncios.</summary>
        public void Initialize()
        {
            _provider.Initialize(() =>
            {
                _provider.ShowBanner();
                _provider.LoadInterstitial();
                _provider.LoadRewarded();
            });
        }

        public void ShowInterstitialIfDue(int totalVictories, Action onFinished)
        {
            if (onFinished == null)
            {
                throw new ArgumentNullException(nameof(onFinished));
            }

            if (_isShowingFullscreen || !_policy.IsDue(totalVictories))
            {
                onFinished();
                return;
            }

            if (!_provider.IsInterstitialReady)
            {
                Debug.Log($"[NovaStriker] Tocaba intersticial (victoria {totalVictories}) pero no estaba cargado.");
                _provider.LoadInterstitial();
                onFinished();
                return;
            }

            BeginFullscreen();
            _eventBus.Publish(new InterstitialShownEvent(totalVictories));
            _provider.ShowInterstitial(() =>
            {
                EndFullscreen();
                _provider.LoadInterstitial();
                onFinished();
            });
        }

        public void ShowRewarded(Action<bool> onCompleted)
        {
            if (onCompleted == null)
            {
                throw new ArgumentNullException(nameof(onCompleted));
            }

            if (!IsRewardedReady)
            {
                _provider.LoadRewarded();
                onCompleted(false);
                return;
            }

            BeginFullscreen();
            _eventBus.Publish(new RewardedAdRequestedEvent());
            _provider.ShowRewarded(rewarded =>
            {
                EndFullscreen();
                _provider.LoadRewarded();
                _eventBus.Publish(new RewardedAdClosedEvent(rewarded));
                onCompleted(rewarded);
            });
        }

        public void Dispose()
        {
            _provider.BannerChanged -= OnBannerChanged;
            (_provider as IDisposable)?.Dispose();
        }

        private void BeginFullscreen()
        {
            _isShowingFullscreen = true;
            _eventBus.Publish(new FullscreenAdChangedEvent(true));
        }

        private void EndFullscreen()
        {
            _isShowingFullscreen = false;
            _eventBus.Publish(new FullscreenAdChangedEvent(false));
        }

        private void OnBannerChanged() => BannerLayoutChanged?.Invoke();
    }
}
