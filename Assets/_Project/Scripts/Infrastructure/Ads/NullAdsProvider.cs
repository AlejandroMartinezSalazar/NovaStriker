using System;

namespace NovaStriker.Infrastructure.Ads
{
    /// <summary>Sin red de anuncios (build sin AdMob instalado): no hay banner ni anuncios; "+1 balón" queda desactivado.</summary>
    public sealed class NullAdsProvider : IAdsProvider
    {
        public float BannerHeightPixels => 0f;
        public bool IsInterstitialReady => false;
        public bool IsRewardedReady => false;

        public event Action BannerChanged
        {
            add { }
            remove { }
        }

        public void Initialize(Action onReady) => onReady?.Invoke();
        public void ShowBanner() { }
        public void LoadInterstitial() { }
        public void ShowInterstitial(Action onClosed) => onClosed?.Invoke();
        public void LoadRewarded() { }
        public void ShowRewarded(Action<bool> onClosed) => onClosed?.Invoke(false);
    }
}
