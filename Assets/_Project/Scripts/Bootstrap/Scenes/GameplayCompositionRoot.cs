using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.DI;
using NovaStriker.Core.Events;
using NovaStriker.Core.Loop;
using NovaStriker.Core.Utils;
using NovaStriker.Gameplay.Aim;
using NovaStriker.Gameplay.Ball;
using NovaStriker.Gameplay.Debris;
using NovaStriker.Gameplay.Factories;
using NovaStriker.Gameplay.Input;
using NovaStriker.Gameplay.Level;
using NovaStriker.Gameplay.Level.Rules;
using NovaStriker.Gameplay.Obstacles;
using NovaStriker.Gameplay.States;
using NovaStriker.Gameplay.Throwing;
using NovaStriker.Gameplay.Tutorial;
using NovaStriker.UI.Common;
using NovaStriker.UI.HUD;
using NovaStriker.UI.Localization;
using NovaStriker.UI.Results;
using NovaStriker.UI.Tutorial;
using NovaStriker.UI.Victory;
using UnityEngine;

namespace NovaStriker.Bootstrap.Scenes
{
    /// <summary>
    /// Raíz de composición de SC_Game (DI manual).
    /// Crea balones, tiro, apuntado, fragmentos y personajes; el nivel lo construye LevelSession a partir
    /// del catálogo, y GameplayFlow dirige la partida. La UI recibe IGameCommands y escucha el bus.
    /// </summary>
    public sealed class GameplayCompositionRoot : SceneCompositionRoot
    {
        [Header("Escena")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _ballContainer;
        [SerializeField] private AimPreviewView _aimPreview;

        [Header("Datos")]
        [SerializeField] private BallView _ballPrefab;
        [SerializeField] private BallConfig _ballConfig;
        [SerializeField] private AimConfig _aimConfig;

        [Header("Niveles")]
        [SerializeField] private LevelCatalog _levelCatalog;
        [Tooltip("Padre bajo el que se instancia el escenario del nivel.")]
        [SerializeField] private Transform _levelContainer;
        [SerializeField] private Transform _characterContainer;
        [SerializeField] private GameFlowSettings _flowSettings = GameFlowSettings.Default;

        [Header("Fragmentos")]
        [SerializeField] private DebrisShard _glassShardPrefab;
        [SerializeField] private Transform _debrisContainer;
        [SerializeField, Min(0.5f)] private float _debrisLifetime = 3f;
        [SerializeField, Min(1)] private int _maxPooledDebris = 64;

        [Header("UI")]
        [SerializeField] private HudView _hudView;
        [SerializeField] private ResultPanelView _resultPanelView;
        [SerializeField] private VictoryView _victoryView;
        [Tooltip("Indicación de tiro del nivel 1 (NovaStriker/Setup/Tutorial - Indicación de tiro).")]
        [SerializeField] private TutorialHintView _tutorialHintView;

        protected override void OnCompose(IServiceContainer sceneContainer, CompositeDisposable disposables)
        {
            var eventBus = sceneContainer.Resolve<IEventBus>();
            var navigator = sceneContainer.Resolve<IAppNavigator>();
            var progress = sceneContainer.Resolve<IProgressService>();
            var ads = sceneContainer.Resolve<IAdsService>();
            var physics = new UnityPhysicsSettings();
            Camera gameCamera = Require(_camera, nameof(_camera));

            // Balones: Factory + Object Pool.
            BallConfig ballConfig = Require(_ballConfig, nameof(_ballConfig));
            var ballFactory = new BallFactory(Require(_ballPrefab, nameof(_ballPrefab)), eventBus, ballConfig, _ballContainer);
            var ballPool = new BallPool(ballFactory, _ballContainer, ballConfig.MaxActiveBalls);
            disposables.Add(ballPool);
            sceneContainer.RegisterInstance<IBallProvider>(ballPool);

            // Tiro: el punto de saque y la estrategia los pone cada nivel.
            var throwController = new ThrowController(ballPool, Vector3.zero, physics, eventBus);
            sceneContainer.RegisterInstance<IThrowController>(throwController);

            // Input y apuntado (lo activa AimingState).
            var pointerInput = new PointerInputReader();
            disposables.Add(pointerInput);
            AddTickable(pointerInput);

            AimConfig aimConfig = Require(_aimConfig, nameof(_aimConfig));
            var aimController = new AimController(
                pointerInput,
                new EventSystemUiHitTester(),
                new PhysicsAimTargetResolver(gameCamera, GameLayers.AimMask, aimConfig.RaycastMaxDistance),
                throwController,
                Require(_aimPreview, nameof(_aimPreview)),
                eventBus,
                new UnityClock(),
                physics,
                AimSettings.From(aimConfig));
            disposables.Add(aimController);

            // Fragmentos de vidrio: Factory + Object Pool.
            var debrisPool = new DebrisPool(Require(_glassShardPrefab, nameof(_glassShardPrefab)), _debrisContainer,
                _debrisLifetime, _maxPooledDebris);
            disposables.Add(debrisPool);
            sceneContainer.RegisterInstance<IDebrisSpawner>(debrisPool);

            // Nivel: Factory de personajes + constructor del escenario + reglas.
            var characterFactory = new CharacterFactory(eventBus, Require(_characterContainer, nameof(_characterContainer)));
            sceneContainer.RegisterInstance<ICharacterFactory>(characterFactory);
            var levelBuilder = new LevelBuilder(
                characterFactory,
                new ObstacleInstaller(new ObstacleServices(eventBus, debrisPool)),
                Require(_levelContainer, nameof(_levelContainer)));

            var rules = new LevelRulesTracker(eventBus);
            disposables.Add(rules);

            var session = new LevelSession(Require(_levelCatalog, nameof(_levelCatalog)), levelBuilder, throwController,
                ballPool, debrisPool, new LevelCamera(gameCamera), rules, eventBus);
            disposables.Add(session);

            // Partida (State).
            var flow = new GameplayFlow(eventBus, session, rules, throwController, aimController, progress, navigator, ads,
                new UnityTimeScale(), LevelOutcomeEvaluator.CreateDefault(), _flowSettings);
            disposables.Add(flow);
            AddTickable(flow);

            var commands = new GameCommands(flow.Machine, session, progress, rules, navigator);
            sceneContainer.RegisterInstance<IGameCommands>(commands);

            // UI (MVP). Se añaden después del flujo para liberarse antes que él (orden inverso).
            var localization = sceneContainer.Resolve<ILocalization>();
            disposables.Add(LocalizedTextInstaller.InstallAll(Require(_hudView, nameof(_hudView)).transform.root, localization));
            disposables.Add(new HudPresenter(_hudView, eventBus, commands, localization));
            disposables.Add(new ResultPanelPresenter(Require(_resultPanelView, nameof(_resultPanelView)), eventBus, commands, ads,
                localization));
            disposables.Add(new VictoryPresenter(Require(_victoryView, nameof(_victoryView)), eventBus, localization));
            if (_tutorialHintView != null)
            {
                disposables.Add(new TutorialHintPresenter(_tutorialHintView, eventBus));
            }
            else
            {
                Debug.LogWarning("[NovaStriker] Falta la indicación de tiro del tutorial. " +
                                 "Ejecuta NovaStriker/Setup/Tutorial - Indicación de tiro.");
            }

            // Tutorial: decide cuándo se ve la indicación (nivel 1, hasta el primer tiro de cada intento).
            disposables.Add(new TutorialDirector(eventBus, session));
            disposables.Add(new ButtonClickSounds(_hudView.transform.root, sceneContainer.Resolve<IAudioService>()));
            disposables.Add(new BannerLayoutBinder(ads, _hudView.transform.root, gameCamera));

            flow.Start(progress.CurrentLevelIndex);
        }
    }
}
