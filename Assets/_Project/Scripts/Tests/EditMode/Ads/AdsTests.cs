using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Infrastructure.Ads;
using NovaStriker.UI.Common;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Ads
{
    public class AdsTests
    {
        /// <summary>Red de anuncios controlable desde el test.</summary>
        private sealed class ScriptedProvider : IAdsProvider
        {
            public readonly List<string> Calls = new List<string>();
            public bool InterstitialLoaded = true;
            public bool RewardedLoaded = true;
            public Action PendingInterstitialClose;
            public Action<bool> PendingRewardedClose;

            public event Action BannerChanged;
            public float BannerHeightPixels { get; private set; }
            public bool IsInterstitialReady => InterstitialLoaded;
            public bool IsRewardedReady => RewardedLoaded;

            public void Initialize(Action onReady) { Calls.Add("Initialize"); onReady(); }
            public void ShowBanner() { Calls.Add("ShowBanner"); BannerHeightPixels = 150f; BannerChanged?.Invoke(); }
            public void LoadInterstitial() { Calls.Add("LoadInterstitial"); }
            public void ShowInterstitial(Action onClosed) { Calls.Add("ShowInterstitial"); InterstitialLoaded = false; PendingInterstitialClose = onClosed; }
            public void LoadRewarded() { Calls.Add("LoadRewarded"); }
            public void ShowRewarded(Action<bool> onClosed) { Calls.Add("ShowRewarded"); RewardedLoaded = false; PendingRewardedClose = onClosed; }
        }

        // ---------- Política ----------

        [Test]
        public void Policy_Every4UntilVictory16_ThenEvery2()
        {
            InterstitialPolicy policy = InterstitialPolicy.Default;
            var due = new List<int>();
            for (int victory = 0; victory <= 26; victory++)
            {
                if (policy.IsDue(victory))
                {
                    due.Add(victory);
                }
            }

            CollectionAssert.AreEqual(new[] { 4, 8, 12, 16, 18, 20, 22, 24, 26 }, due);
        }

        [Test]
        public void Policy_RejectsInvalidIntervals()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new InterstitialPolicy(0, 16, 2));
        }

        // ---------- Servicio ----------

        private ScriptedProvider _provider;
        private EventBus _bus;
        private AdsService _ads;
        private List<bool> _fullscreen;

        [SetUp]
        public void SetUp()
        {
            _provider = new ScriptedProvider();
            _bus = new EventBus();
            _ads = new AdsService(_provider, InterstitialPolicy.Default, _bus);
            _fullscreen = new List<bool>();
            _bus.Subscribe<FullscreenAdChangedEvent>(e => _fullscreen.Add(e.IsShowing));
        }

        [TearDown]
        public void TearDown()
        {
            _ads.Dispose();
            _bus.Dispose();
        }

        [Test]
        public void Initialize_ShowsBanner_AndPreloadsAds()
        {
            int layoutChanges = 0;
            _ads.BannerLayoutChanged += () => layoutChanges++;

            _ads.Initialize();

            CollectionAssert.AreEqual(new[] { "Initialize", "ShowBanner", "LoadInterstitial", "LoadRewarded" }, _provider.Calls);
            Assert.AreEqual(150f, _ads.BannerHeightPixels, 1e-4f);
            Assert.AreEqual(1, layoutChanges);
        }

        [Test]
        public void Interstitial_NotDue_FinishesImmediately()
        {
            int finished = 0;
            _ads.ShowInterstitialIfDue(3, () => finished++);

            Assert.AreEqual(1, finished);
            CollectionAssert.AreEqual(new string[0], _provider.Calls);
        }

        [Test]
        public void Interstitial_Due_ShowsWaitsAndReloads()
        {
            int finished = 0;
            _ads.ShowInterstitialIfDue(4, () => finished++);
            Assert.AreEqual(0, finished, "Espera a que se cierre.");
            CollectionAssert.AreEqual(new[] { true }, _fullscreen);

            _provider.PendingInterstitialClose();

            Assert.AreEqual(1, finished);
            CollectionAssert.AreEqual(new[] { true, false }, _fullscreen);
            CollectionAssert.AreEqual(new[] { "ShowInterstitial", "LoadInterstitial" }, _provider.Calls);
        }

        [Test]
        public void Interstitial_DueButNotLoaded_DoesNotBlockTheGame()
        {
            _provider.InterstitialLoaded = false;
            int finished = 0;

            _ads.ShowInterstitialIfDue(8, () => finished++);

            Assert.AreEqual(1, finished);
            CollectionAssert.AreEqual(new[] { "LoadInterstitial" }, _provider.Calls);
        }

        [Test]
        public void Rewarded_ReportsResult_AndReloads()
        {
            bool? result = null;
            _ads.ShowRewarded(r => result = r);
            Assert.IsFalse(_ads.IsRewardedReady, "No se puede pedir otro mientras se muestra.");

            _provider.PendingRewardedClose(true);

            Assert.AreEqual(true, result);
            CollectionAssert.AreEqual(new[] { "ShowRewarded", "LoadRewarded" }, _provider.Calls);
            CollectionAssert.AreEqual(new[] { true, false }, _fullscreen);
        }

        [Test]
        public void Rewarded_NotLoaded_ReturnsFalse()
        {
            _provider.RewardedLoaded = false;
            bool? result = null;

            _ads.ShowRewarded(r => result = r);

            Assert.AreEqual(false, result);
        }

        [Test]
        public void NullProvider_NeverShowsAnything()
        {
            var ads = new AdsService(new NullAdsProvider(), InterstitialPolicy.Default, _bus);
            int finished = 0;
            bool? rewarded = null;

            ads.Initialize();
            ads.ShowInterstitialIfDue(4, () => finished++);
            ads.ShowRewarded(r => rewarded = r);

            Assert.AreEqual(0f, ads.BannerHeightPixels);
            Assert.AreEqual(1, finished);
            Assert.AreEqual(false, rewarded);
            Assert.IsFalse(ads.IsRewardedReady);
        }

        // ---------- Franja del banner ----------

        [Test]
        public void SafeArea_TopInset_LowersTheUiUnderTheBanner()
        {
            // Pantalla 1000×2000, muesca de 100 px arriba, banner de 150 px.
            SafeAreaMath.ToAnchors(new Rect(0f, 0f, 1000f, 1900f), new Vector2(1000f, 2000f), out Vector2 min, out Vector2 max, 150f);

            Assert.AreEqual(0f, min.y, 1e-5f);
            Assert.AreEqual(0.875f, max.y, 1e-5f);
        }

        [Test]
        public void CameraViewport_LeavesTheNotchAndBannerStripFree()
        {
            Rect viewport = SafeAreaMath.CameraViewport(new Rect(0f, 0f, 1000f, 1900f), new Vector2(1000f, 2000f), 150f);

            Assert.AreEqual(0f, viewport.y, 1e-5f);
            Assert.AreEqual(0.875f, viewport.height, 1e-5f);
            Assert.AreEqual(1f, SafeAreaMath.CameraViewport(new Rect(0f, 0f, 1000f, 1900f), new Vector2(1000f, 2000f), 0f).height, 1e-5f,
                "Sin banner se usa toda la pantalla.");
        }
    }
}
