using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Events;
using NovaStriker.Infrastructure.Audio;
using NovaStriker.Infrastructure.Localization;
using NovaStriker.UI.Common;
using NovaStriker.UI.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NovaStriker.Tests.EditMode.Audio
{
    public class AudioTests
    {
        private sealed class FakeAudio : IAudioService
        {
            public readonly List<string> Calls = new List<string>();
            public readonly List<float> Intensities = new List<float>();
            public void PlaySfx(SfxId id, float intensity = 1f) { Calls.Add($"Sfx:{id}"); Intensities.Add(intensity); }
            public void PlayUi(SfxId id) => Calls.Add($"Ui:{id}");
            public void PlayMusic(MusicId id) => Calls.Add($"Music:{id}");
            public void StopMusic() => Calls.Add("StopMusic");
            public void SetAmbience(bool enabled) => Calls.Add($"Ambience:{enabled}");
            public void SetDucked(bool ducked) => Calls.Add($"Ducked:{ducked}");
            public void SetPaused(bool paused) => Calls.Add($"Paused:{paused}");
            public void SetSuspended(bool suspended) => Calls.Add($"Suspended:{suspended}");
        }

        private sealed class FakeSettings : IGameSettings
        {
            public float MusicVolume { get; private set; } = 0.8f;
            public float SfxVolume { get; private set; } = 1f;
            public Language Language { get; private set; }
            public int Saves;
            public event Action Changed;
            public void SetMusicVolume(float volume) { MusicVolume = volume; Changed?.Invoke(); }
            public void SetSfxVolume(float volume) { SfxVolume = volume; Changed?.Invoke(); }
            public void SetLanguage(Language language) { Language = language; Changed?.Invoke(); }
            public void Save() => Saves++;
        }

        private sealed class FakeSettingsView : ISettingsView
        {
            public event Action OpenClicked;
            public event Action CloseClicked;
            public event Action<float> MusicVolumeChanged;
            public event Action<float> SfxVolumeChanged;
            public event Action<Language> LanguageSelected;
            public Language SelectedLanguage;
            public bool IsOpen = true;
            public float ShownMusic, ShownSfx;
            public void ShowPanel(float musicVolume, float sfxVolume) { IsOpen = true; ShownMusic = musicVolume; ShownSfx = sfxVolume; }
            public void HidePanel() => IsOpen = false;
            public void SetSelectedLanguage(Language language) => SelectedLanguage = language;
            public void PickLanguage(Language language) => LanguageSelected?.Invoke(language);
            public void Open() => OpenClicked?.Invoke();
            public void Close() => CloseClicked?.Invoke();
            public void DragMusic(float v) => MusicVolumeChanged?.Invoke(v);
            public void DragSfx(float v) => SfxVolumeChanged?.Invoke(v);
            public bool HasListeners => OpenClicked != null || CloseClicked != null || MusicVolumeChanged != null ||
                                        SfxVolumeChanged != null || LanguageSelected != null;
        }

        private EventBus _bus;
        private FakeAudio _audio;
        private AudioEventRouter _router;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _audio = new FakeAudio();
            _router = new AudioEventRouter(_bus, _audio);
        }

        [TearDown]
        public void TearDown()
        {
            _router.Dispose();
            _bus.Dispose();
        }

        // ---------- Router ----------

        [Test]
        public void Router_MusicAndAmbience_FollowTheAppState()
        {
            _bus.Publish(new AppStateChangedEvent(AppStateId.Boot, AppStateId.MainMenu));
            CollectionAssert.AreEqual(new[] { "Paused:False", "Ducked:False", "Ambience:False", "Music:Menu" }, _audio.Calls);

            _audio.Calls.Clear();
            _bus.Publish(new AppStateChangedEvent(AppStateId.MainMenu, AppStateId.Gameplay));
            CollectionAssert.AreEqual(new[] { "Paused:False", "Ducked:False", "Ambience:True", "Music:Gameplay" }, _audio.Calls);
        }

        [Test]
        public void Router_ShotAimKnockdownAndGlass()
        {
            _bus.Publish(new AimStartedEvent());
            _bus.Publish(new ShotFiredEvent("s", Vector3.zero, Vector3.forward, Vector3.forward));
            _bus.Publish(new CharacterKnockedDownEvent(1, true, KnockdownCause.BallHit, Vector3.zero));
            _bus.Publish(new GlassCrackedEvent(1, 3, Vector3.zero));
            _bus.Publish(new ObstacleBrokenEvent(ObstacleType.BreakableGlass, Vector3.zero));

            CollectionAssert.AreEqual(new[] { "Sfx:Aim", "Sfx:Kick", "Sfx:Knockdown", "Sfx:GlassCrack", "Sfx:GlassBreak" }, _audio.Calls);
        }

        [Test]
        public void Router_GlassCrateBreak_SoundsLikeGlass()
        {
            _bus.Publish(new ObstacleBrokenEvent(ObstacleType.GlassCrate, Vector3.zero));

            CollectionAssert.AreEqual(new[] { "Sfx:GlassBreak" }, _audio.Calls);
        }

        [TestCase(GameLayers.Ragdoll, 0f, SfxId.BallBody)]
        [TestCase(GameLayers.Obstacle, 0f, SfxId.BallBox)]
        [TestCase(GameLayers.Environment, 0.95f, SfxId.BallGround)]
        [TestCase(GameLayers.Environment, 0.1f, SfxId.BallWall)]
        [TestCase(GameLayers.Breakable, 0f, SfxId.BallWall)]
        [TestCase(GameLayers.Debris, 1f, SfxId.None)]
        public void Router_BallHitSound_DependsOnSurface(int layer, float normalY, SfxId expected)
        {
            Assert.AreEqual(expected, AudioEventRouter.BallHitSound(layer, normalY));
        }

        [Test]
        public void Router_HitVolume_ScalesWithImpulse()
        {
            _bus.Publish(new BallCollidedEvent(Vector3.zero, Vector3.up, AudioEventRouter.BallFullImpulse * 0.5f, GameLayers.Environment));
            _bus.Publish(new ObstacleImpactEvent(ObstacleType.Box, Vector3.zero, AudioEventRouter.BoxFullImpulse * 4f));

            CollectionAssert.AreEqual(new[] { "Sfx:BallGround", "Sfx:BoxImpact" }, _audio.Calls);
            Assert.AreEqual(0.5f, _audio.Intensities[0], 1e-4f);
            Assert.AreEqual(1f, _audio.Intensities[1], 1e-4f, "Se limita a 1.");
        }

        [Test]
        public void Router_PauseVictoryDefeat()
        {
            _bus.Publish(new GameStateChangedEvent(GameStateId.Aiming, GameStateId.Paused));
            _bus.Publish(new GameStateChangedEvent(GameStateId.Paused, GameStateId.Aiming));
            _bus.Publish(new GameStateChangedEvent(GameStateId.ShotResolve, GameStateId.Victory));
            _bus.Publish(new GameStateChangedEvent(GameStateId.ShotResolve, GameStateId.Defeat));

            CollectionAssert.AreEqual(new[]
            {
                "Ui:Whoosh", "Paused:True",
                "Paused:False", "Ducked:False",
                "Ducked:True", "Sfx:Victory",
                "Ducked:True", "Sfx:Defeat", "Ui:Whoosh",
            }, _audio.Calls);
        }

        [Test]
        public void Router_FullscreenAds_SuspendAudio()
        {
            _bus.Publish(new FullscreenAdChangedEvent(true));
            _bus.Publish(new FullscreenAdChangedEvent(false));

            CollectionAssert.AreEqual(new[] { "Suspended:True", "Suspended:False" }, _audio.Calls);
        }

        [Test]
        public void Router_Dispose_Unsubscribes()
        {
            _router.Dispose();
            _bus.Publish(new ShotFiredEvent("s", Vector3.zero, Vector3.forward, Vector3.forward));

            Assert.AreEqual(0, _audio.Calls.Count);
            Assert.AreEqual(0, _bus.GetSubscriberCount<BallCollidedEvent>());
        }

        // ---------- Lógica pura ----------

        [Test]
        public void Throttle_BlocksRepeatsWithinTheInterval_PerSound()
        {
            var throttle = new SfxThrottle();
            Assert.IsTrue(throttle.TryAcquire(SfxId.BallBox, 1f, 0.1f));
            Assert.IsFalse(throttle.TryAcquire(SfxId.BallBox, 1.05f, 0.1f));
            Assert.IsTrue(throttle.TryAcquire(SfxId.BallWall, 1.05f, 0.1f), "Otro efecto no se bloquea.");
            Assert.IsTrue(throttle.TryAcquire(SfxId.BallBox, 1.1f, 0.1f));
        }

        [Test]
        public void VariantPicker_NeverRepeatsTheLastVariant()
        {
            for (int last = 0; last < 3; last++)
            {
                for (float r = 0f; r < 1f; r += 0.05f)
                {
                    int next = VariantPicker.Next(3, last, r);
                    Assert.IsTrue(next >= 0 && next < 3);
                    Assert.IsFalse(next == last, $"Repitió la variante {last} con r={r}.");
                }
            }

            Assert.AreEqual(0, VariantPicker.Next(1, 0, 0.7f));
            Assert.AreEqual(2, VariantPicker.Next(3, -1, 0.99f));
        }

        // ---------- Ajustes ----------

        [Test]
        public void Settings_OpenShowsValues_DragApplies_CloseSaves()
        {
            var view = new FakeSettingsView();
            var settings = new FakeSettings();
            var presenter = new SettingsPresenter(view, settings, TestLocalization.Spanish());
            Assert.IsFalse(view.IsOpen, "Empieza cerrado.");

            view.Open();
            Assert.AreEqual(0.8f, view.ShownMusic, 1e-5f);
            view.DragMusic(0.3f);
            view.DragSfx(0.6f);
            Assert.AreEqual(0.3f, settings.MusicVolume, 1e-5f);
            Assert.AreEqual(0.6f, settings.SfxVolume, 1e-5f);
            Assert.AreEqual(0, settings.Saves);

            view.Close();
            Assert.IsFalse(view.IsOpen);
            Assert.AreEqual(1, settings.Saves);

            presenter.Dispose();
            Assert.IsFalse(view.HasListeners);
            Assert.AreEqual(2, settings.Saves, "Al salir del menú también se guarda.");
        }

        [Test]
        public void Settings_LanguageButtons_ApplyAndHighlight()
        {
            var view = new FakeSettingsView();
            var settings = new FakeSettings();
            var localization = new LocalizationService(settings, Language.Spanish);
            using (new SettingsPresenter(view, settings, localization))
            {
                view.Open();
                Assert.AreEqual(Language.Spanish, view.SelectedLanguage, "Al abrir resalta el idioma actual.");

                view.PickLanguage(Language.Portuguese);
                Assert.AreEqual(Language.Portuguese, localization.Current);
                Assert.AreEqual(Language.Portuguese, settings.Language, "Queda guardado como elegido.");
                Assert.AreEqual(Language.Portuguese, view.SelectedLanguage);
                Assert.AreEqual("AJUSTES", localization.Get(TextKey.SettingsTitle));
                Assert.AreEqual("FECHAR", localization.Get(TextKey.SettingsClose));
            }
        }

        [Test]
        public void ButtonClickSounds_PlayClick_UntilDisposed()
        {
            var root = new GameObject("Canvas");
            var child = new GameObject("Button");
            child.transform.SetParent(root.transform);
            var button = child.AddComponent<Button>();
            var audio = new FakeAudio();

            var clicks = new ButtonClickSounds(root.transform, audio);
            button.onClick.Invoke();
            clicks.Dispose();
            button.onClick.Invoke();

            Assert.AreEqual(0, clicks.Count, "Dispose suelta los botones.");
            CollectionAssert.AreEqual(new[] { "Ui:Click" }, audio.Calls);
            Object.DestroyImmediate(root);
        }
    }
}
