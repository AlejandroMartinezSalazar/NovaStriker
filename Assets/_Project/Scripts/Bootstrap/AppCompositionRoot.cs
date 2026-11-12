using NovaStriker.Bootstrap.App;
using NovaStriker.Bootstrap.Online;
using NovaStriker.Bootstrap.Scenes;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.DI;
using NovaStriker.Core.Events;
using NovaStriker.Core.StateMachine;
using NovaStriker.Infrastructure.Ads;
using NovaStriker.Infrastructure.Analytics;
using NovaStriker.Infrastructure.Audio;
using NovaStriker.Infrastructure.Online;
using NovaStriker.Infrastructure.Progress;
using NovaStriker.Infrastructure.Save;
using NovaStriker.Infrastructure.Settings;
using NovaStriker.Infrastructure.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using NovaStriker.Infrastructure.Localization;

namespace NovaStriker.Bootstrap
{
    /// <summary>
    /// Raíz de composición global. Vive toda la sesión (DontDestroyOnLoad) y la crea AppBootstrapper.
    /// Registra los servicios globales y dirige la máquina de estados de la app.
    /// No colocar este componente en escenas: lo instancia AppBootstrapper desde Resources/PF_AppRoot.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class AppCompositionRoot : MonoBehaviour
    {
        /// <summary>Carpeta de guardado: Application.persistentDataPath/Save.</summary>
        public static string SaveDirectory => System.IO.Path.Combine(Application.persistentDataPath, "Save");

        [Tooltip("Ficha de sonidos (Settings/Audio/SO_SoundLibrary). La asigna el setup de la Etapa 9.")]
        [SerializeField] private SoundLibrary _soundLibrary;

        [Tooltip("IDs de AdMob y política de intersticiales (Settings/Ads/SO_AdsConfig). La asigna el setup de la Etapa 10.")]
        [SerializeField] private AdsConfig _adsConfig;

        [Tooltip("Title ID de PlayFab (Settings/Online/SO_PlayFabConfig). Lo asigna NovaStriker/Setup/PlayFab. Opcional: sin él se juega sin cuenta.")]
        [SerializeField] private PlayFabConfig _playFabConfig;

        private ServiceContainer _container;
        private StateMachine<AppStateId> _machine;
        private IEventBus _eventBus;
        private IGameSettings _settings;
        private ILocalization _localization;
        private AudioService _audio;
        private AudioEventRouter _audioRouter;
        private PlayerAccountService _account;
        private AnalyticsEventRouter _analytics;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Compose();
        }

        private void Start()
        {
            AppStateId initial = AppStateMachineFactory.ResolveInitialState(SceneManager.GetActiveScene().name,
                _localization.HasChosenLanguage);
            _machine.ChangeState(initial);
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            _machine.Tick(deltaTime);
            _audio.Tick(deltaTime);
            _account.Tick(deltaTime);
        }

        // En móvil la app puede cerrarse sin aviso después de pasar a segundo plano: se guardan los ajustes.
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _settings?.Save();
                _analytics?.OnAppPaused();
            }
            else
            {
                _account?.OnAppResumed();
            }
        }

        private void OnApplicationQuit()
        {
            _settings?.Save();
        }

        private void OnDestroy()
        {
            _audioRouter?.Dispose();
            _analytics?.Dispose();

            if (_machine != null)
            {
                _machine.StateChanged -= OnAppStateChanged;
                _machine.Stop();
            }

            _container?.Dispose();
        }

        private void Compose()
        {
            _container = new ServiceContainer();

            var eventBus = new EventBus();
            _eventBus = eventBus;
            _container.RegisterInstance<IEventBus>(eventBus, disposeWithContainer: true);

            var storage = new FileSaveStorage(SaveDirectory);

            // Lo primero: inicio de sesión automático sin contraseña (PlayFab). Va en segundo plano y nunca bloquea:
            // si falla, se juega igual y se reintenta solo.
            _account = new PlayerAccountService(
                LoginProviderFactory.Create(_playFabConfig),
                PlayerIdResolver.Resolve(SystemInfo.deviceUniqueIdentifier, SystemInfo.unsupportedIdentifier, storage,
                    () => System.Guid.NewGuid().ToString("N")),
                eventBus,
                _playFabConfig != null ? _playFabConfig.TimeoutSeconds : PlayerAccountService.DefaultTimeoutSeconds);
            _container.RegisterInstance<IPlayerAccount>(_account);
            _account.Start();

            var sceneLoader = new SceneLoader();
            _container.RegisterInstance<ISceneLoader>(sceneLoader);

            // Etapa 8: progreso y ajustes guardados en disco (Adapter: almacenamiento y serializador intercambiables).
            var serializer = new JsonUtilitySerializer();
            _container.RegisterInstance<ISaveStorage>(storage);
            _container.RegisterInstance<IProgressService>(new PersistentProgressService(storage, serializer));
            var settings = new PersistentGameSettings(storage, serializer);
            _settings = settings;
            _container.RegisterInstance<IGameSettings>(settings);

            // Idiomas: el elegido se guarda en los ajustes; sin elegir, el del móvil (o inglés).
            var localization = new LocalizationService(settings,
                LocalizationService.FromSystemLanguage(Application.systemLanguage));
            _container.RegisterInstance<ILocalization>(localization);
            _localization = localization;

            // Etapa 9: audio (servicio global; el router traduce eventos del juego a sonidos).
            if (_soundLibrary == null)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(AppCompositionRoot)}: falta SO_SoundLibrary en PF_AppRoot. Ejecuta NovaStriker/Setup/Etapa 9.");
            }

            _audio = new AudioService(_soundLibrary, settings, transform);
            _container.RegisterInstance<IAudioService>(_audio, disposeWithContainer: true);
            _audioRouter = new AudioEventRouter(eventBus, _audio);

            // Etapa 10: anuncios (Adapter: AdMob, simulados en el Editor o ninguno).
            if (_adsConfig == null)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(AppCompositionRoot)}: falta SO_AdsConfig en PF_AppRoot. Ejecuta NovaStriker/Setup/Etapa 10.");
            }

            var ads = new AdsService(AdsProviderFactory.Create(_adsConfig, transform), _adsConfig.CreatePolicy(), eventBus);
            _container.RegisterInstance<IAdsService>(ads, disposeWithContainer: true);
            ads.Initialize();

            // Analíticas (Observer + Adapter): escuchan el bus y envían a Firebase; en el Editor, solo consola.
            IAnalyticsBackend analyticsBackend = AnalyticsBackendFactory.Create();
            analyticsBackend.Initialize();
            _analytics = new AnalyticsEventRouter(analyticsBackend, eventBus, settings, ads,
                new SaveSlot<AnalyticsData>(storage, serializer, AnalyticsEventRouter.SaveKeyName, () => new AnalyticsData()),
                () => Time.realtimeSinceStartup);

            _machine = new AppStateMachineFactory(sceneLoader, new SceneRootProvider(), _container).Create();
            _machine.StateChanged += OnAppStateChanged;

            _container.RegisterInstance<IAppNavigator>(new AppNavigator(_machine, sceneLoader));
        }

        private void OnAppStateChanged(AppStateId from, AppStateId to)
        {
            _eventBus.Publish(new AppStateChangedEvent(from, to));
        }
    }
}
