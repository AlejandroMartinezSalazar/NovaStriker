using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Events;
using NovaStriker.Core.Utils;

namespace NovaStriker.Infrastructure.Audio
{
    /// <summary>
    /// Observer: traduce los eventos del juego a sonidos. Ningún sistema de juego conoce el audio.
    /// La intensidad de los golpes sale del impulso del choque.
    /// </summary>
    public sealed class AudioEventRouter : IDisposable
    {
        /// <summary>Impulso (N·s) del balón que suena a volumen completo.</summary>
        public const float BallFullImpulse = 3f;

        /// <summary>Impulso (N·s) de una caja que suena a volumen completo.</summary>
        public const float BoxFullImpulse = 10f;

        /// <summary>Componente vertical mínima de la normal para considerar el choque "con el suelo".</summary>
        public const float GroundNormalY = 0.6f;

        private readonly IAudioService _audio;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

        public AudioEventRouter(IEventBus eventBus, IAudioService audio)
        {
            if (eventBus == null)
            {
                throw new ArgumentNullException(nameof(eventBus));
            }

            _audio = audio ?? throw new ArgumentNullException(nameof(audio));

            _subscriptions.Add(eventBus.Subscribe<AppStateChangedEvent>(OnAppStateChanged));
            _subscriptions.Add(eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged));
            _subscriptions.Add(eventBus.Subscribe<AimStartedEvent>(_ => _audio.PlaySfx(SfxId.Aim)));
            _subscriptions.Add(eventBus.Subscribe<ShotFiredEvent>(_ => _audio.PlaySfx(SfxId.Kick)));
            _subscriptions.Add(eventBus.Subscribe<BallCollidedEvent>(OnBallCollided));
            _subscriptions.Add(eventBus.Subscribe<CharacterKnockedDownEvent>(_ => _audio.PlaySfx(SfxId.Knockdown)));
            _subscriptions.Add(eventBus.Subscribe<GlassCrackedEvent>(_ => _audio.PlaySfx(SfxId.GlassCrack)));
            _subscriptions.Add(eventBus.Subscribe<ObstacleBrokenEvent>(OnObstacleBroken));
            _subscriptions.Add(eventBus.Subscribe<FullscreenAdChangedEvent>(e => _audio.SetSuspended(e.IsShowing)));
            _subscriptions.Add(eventBus.Subscribe<BallTeleportedEvent>(_ => _audio.PlaySfx(SfxId.Portal)));
            _subscriptions.Add(eventBus.Subscribe<ObstacleImpactEvent>(e => _audio.PlaySfx(SfxId.BoxImpact, Intensity(e.Impulse, BoxFullImpulse))));
        }

        public void Dispose() => _subscriptions.Dispose();

        /// <summary>Qué efecto suena cuando el balón golpea algo de esta capa (None = ninguno).</summary>
        public static SfxId BallHitSound(int layer, float normalY)
        {
            switch (layer)
            {
                case GameLayers.Ragdoll:
                    return SfxId.BallBody;
                case GameLayers.Obstacle:
                    return SfxId.BallBox;
                case GameLayers.Breakable:
                    return SfxId.BallWall;
                case GameLayers.Environment:
                    return normalY >= GroundNormalY ? SfxId.BallGround : SfxId.BallWall;
                default:
                    return SfxId.None;
            }
        }

        public static float Intensity(float impulse, float fullImpulse)
        {
            if (fullImpulse <= 0f || impulse <= 0f)
            {
                return 0f;
            }

            float value = impulse / fullImpulse;
            return value > 1f ? 1f : value;
        }

        private void OnAppStateChanged(AppStateChangedEvent e)
        {
            switch (e.To)
            {
                case AppStateId.MainMenu:
                    _audio.SetPaused(false);
                    _audio.SetDucked(false);
                    _audio.SetAmbience(false);
                    _audio.PlayMusic(MusicId.Menu);
                    break;
                case AppStateId.Gameplay:
                    _audio.SetPaused(false);
                    _audio.SetDucked(false);
                    _audio.SetAmbience(true);
                    _audio.PlayMusic(MusicId.Gameplay);
                    break;
            }
        }

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            if (e.From == GameStateId.Paused && e.To != GameStateId.Paused)
            {
                _audio.SetPaused(false);
            }

            switch (e.To)
            {
                case GameStateId.Paused:
                    _audio.PlayUi(SfxId.Whoosh);
                    _audio.SetPaused(true);
                    break;
                case GameStateId.Victory:
                    _audio.SetDucked(true);
                    _audio.PlaySfx(SfxId.Victory);
                    break;
                case GameStateId.Defeat:
                    _audio.SetDucked(true);
                    _audio.PlaySfx(SfxId.Defeat);
                    _audio.PlayUi(SfxId.Whoosh);
                    break;
                case GameStateId.LevelSetup:
                case GameStateId.Aiming:
                    _audio.SetDucked(false);
                    break;
            }
        }

        private void OnBallCollided(BallCollidedEvent e)
        {
            SfxId sound = BallHitSound(e.OtherLayer, e.Normal.y);
            if (sound != SfxId.None)
            {
                _audio.PlaySfx(sound, Intensity(e.Impulse, BallFullImpulse));
            }
        }

        private void OnObstacleBroken(ObstacleBrokenEvent e)
        {
            if (e.Type == ObstacleType.BreakableGlass || e.Type == ObstacleType.GlassCrate)
            {
                _audio.PlaySfx(SfxId.GlassBreak);
            }
        }
    }
}
