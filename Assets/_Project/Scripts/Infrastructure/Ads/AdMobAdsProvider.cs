#if NOVA_ADMOB
using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace NovaStriker.Infrastructure.Ads
{
    /// <summary>
    /// Adapter de Google Mobile Ads (AdMob). Solo se compila con el símbolo NOVA_ADMOB, que el setup de la
    /// Etapa 10 añade cuando detecta el plugin instalado. Banner adaptable anclado arriba.
    /// Los callbacks del SDK se reciben en el hilo principal (RaiseAdEventsOnUnityMainThread).
    /// </summary>
    public sealed class AdMobAdsProvider : IAdsProvider, IDisposable
    {
        private readonly AdsConfig _config;
        private BannerView _banner;
        private InterstitialAd _interstitial;
        private RewardedAd _rewarded;
        private bool _loadingInterstitial;
        private bool _loadingRewarded;

        public AdMobAdsProvider(AdsConfig config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        public event Action BannerChanged;

        public float BannerHeightPixels { get; private set; }
        public bool IsInterstitialReady => _interstitial != null && _interstitial.CanShowAd();
        public bool IsRewardedReady => _rewarded != null && _rewarded.CanShowAd();

        public void Initialize(Action onReady)
        {
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            MobileAds.Initialize(_ => onReady?.Invoke());
        }

        public void ShowBanner()
        {
            if (_banner != null)
            {
                _banner.Show();
                return;
            }

            AdSize size = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            _banner = new BannerView(_config.BannerId, size, AdPosition.Top);
            _banner.OnBannerAdLoaded += () =>
            {
                BannerHeightPixels = _banner.GetHeightInPixels();
                BannerChanged?.Invoke();
            };
            _banner.OnBannerAdLoadFailed += error =>
                Debug.LogWarning($"[NovaStriker] El banner no cargó: {error.GetMessage()}");
            _banner.LoadAd(new AdRequest());
        }

        public void LoadInterstitial()
        {
            if (_loadingInterstitial || IsInterstitialReady)
            {
                return;
            }

            _loadingInterstitial = true;
            InterstitialAd.Load(_config.InterstitialId, new AdRequest(), (ad, error) =>
            {
                _loadingInterstitial = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[NovaStriker] El intersticial no cargó: {error?.GetMessage()}");
                    return;
                }

                _interstitial = ad;
            });
        }

        public void ShowInterstitial(Action onClosed)
        {
            InterstitialAd ad = _interstitial;
            _interstitial = null;
            if (ad == null || !ad.CanShowAd())
            {
                onClosed?.Invoke();
                return;
            }

            bool finished = false;
            void Finish()
            {
                if (finished)
                {
                    return;
                }

                finished = true;
                ad.Destroy();
                onClosed?.Invoke();
            }

            ad.OnAdFullScreenContentClosed += Finish;
            ad.OnAdFullScreenContentFailed += _ => Finish();
            ad.Show();
        }

        public void LoadRewarded()
        {
            if (_loadingRewarded || IsRewardedReady)
            {
                return;
            }

            _loadingRewarded = true;
            RewardedAd.Load(_config.RewardedId, new AdRequest(), (ad, error) =>
            {
                _loadingRewarded = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[NovaStriker] El recompensado no cargó: {error?.GetMessage()}");
                    return;
                }

                _rewarded = ad;
            });
        }

        public void ShowRewarded(Action<bool> onClosed)
        {
            RewardedAd ad = _rewarded;
            _rewarded = null;
            if (ad == null || !ad.CanShowAd())
            {
                onClosed?.Invoke(false);
                return;
            }

            bool earned = false;
            bool finished = false;
            void Finish()
            {
                if (finished)
                {
                    return;
                }

                finished = true;
                ad.Destroy();
                onClosed?.Invoke(earned);
            }

            ad.OnAdFullScreenContentClosed += Finish;
            ad.OnAdFullScreenContentFailed += _ => Finish();
            ad.Show(_ => earned = true);
        }

        public void Dispose()
        {
            _banner?.Destroy();
            _interstitial?.Destroy();
            _rewarded?.Destroy();
        }
    }
}
#endif
