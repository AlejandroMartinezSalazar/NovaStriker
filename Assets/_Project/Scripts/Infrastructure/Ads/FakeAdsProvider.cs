using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NovaStriker.Infrastructure.Ads
{
    /// <summary>
    /// Anuncios simulados para el Editor: banner gris arriba, intersticial y recompensado a pantalla completa
    /// con botones. Permite probar todo el flujo sin SDK, sin red y sin móvil.
    /// </summary>
    public sealed class FakeAdsProvider : IAdsProvider, IDisposable
    {
        /// <summary>Alto típico de un banner adaptable en vertical (dp).</summary>
        private const float BannerHeightDp = 56f;

        private readonly FakeAdsOverlay _overlay;
        private bool _interstitialLoaded;
        private bool _rewardedLoaded;

        public FakeAdsProvider(Transform host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var gameObject = new GameObject("[FakeAds]");
            gameObject.transform.SetParent(host, false);
            _overlay = gameObject.AddComponent<FakeAdsOverlay>();
        }

        public event Action BannerChanged;

        public float BannerHeightPixels { get; private set; }
        public bool IsInterstitialReady => _interstitialLoaded;
        public bool IsRewardedReady => _rewardedLoaded;

        public void Initialize(Action onReady) => onReady?.Invoke();

        public void ShowBanner()
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f * Screen.height / 800f;
            BannerHeightPixels = Mathf.Round(BannerHeightDp * dpi / 160f);
            _overlay.BannerHeight = BannerHeightPixels;
            BannerChanged?.Invoke();
        }

        public void LoadInterstitial() => _interstitialLoaded = true;

        public void ShowInterstitial(Action onClosed)
        {
            _interstitialLoaded = false;
            _overlay.ShowFullscreen("INTERSTICIAL (prueba)", null, _ => onClosed?.Invoke());
        }

        public void LoadRewarded() => _rewardedLoaded = true;

        public void ShowRewarded(Action<bool> onClosed)
        {
            _rewardedLoaded = false;
            _overlay.ShowFullscreen("RECOMPENSADO (prueba)", "Ver completo (+1 balón)", onClosed);
        }

        public void Dispose()
        {
            if (_overlay != null)
            {
                Object.Destroy(_overlay.gameObject);
            }
        }
    }
}
