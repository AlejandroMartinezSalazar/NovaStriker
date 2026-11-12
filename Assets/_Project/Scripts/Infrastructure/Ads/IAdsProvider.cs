using System;

namespace NovaStriker.Infrastructure.Ads
{
    /// <summary>
    /// Adapter de la red de anuncios (AdMob, simulada en el Editor o ninguna). Solo traduce llamadas;
    /// la política (cuándo mostrar) está en AdsService.
    /// </summary>
    public interface IAdsProvider
    {
        void Initialize(Action onReady);

        void ShowBanner();

        /// <summary>Alto real del banner en píxeles de pantalla (0 si no hay).</summary>
        float BannerHeightPixels { get; }

        event Action BannerChanged;

        bool IsInterstitialReady { get; }
        void LoadInterstitial();
        void ShowInterstitial(Action onClosed);

        bool IsRewardedReady { get; }
        void LoadRewarded();

        /// <summary>onClosed(true) si el jugador ganó la recompensa.</summary>
        void ShowRewarded(Action<bool> onClosed);
    }
}
