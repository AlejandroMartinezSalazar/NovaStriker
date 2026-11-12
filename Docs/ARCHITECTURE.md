# NovaStriker — Arquitectura técnica (v2, actualizada en la Etapa 6)

> Documento de referencia para todas las etapas. Cualquier cambio de arquitectura se registra aquí.

## 0. Decisiones de producto confirmadas (2026-10-13)

| # | Decisión |
|---|---|
| D1 | Al superar el último nivel se muestra la pantalla **"Próximamente nuevos niveles"** con la opción **"Empezar desde el nivel 1"** (y volver al menú). Es el único caso en que el índice de nivel vuelve a 0, y solo por acción explícita del jugador. |
| D2 | Etapa 11: paquete **`com.oxac.globalsoccercup`** (ya publicado en Google Play; esta versión se sube como actualización firmada con la keystore existente), nombre visible **NovaStriker**. Company Name y `com.unity.pipeline` sin cambios (no afectan a Android; quitar el paquete es opcional). |
| D3 | **La estrategia de tiro la define cada nivel** (`LevelDefinition.ThrowStrategy`). El jugador no elige. |
| D4 | **El tiro se ejecuta al levantar el dedo**, y el balón debe **llegar al punto del mundo bajo el lugar donde se levantó el dedo**. |
| D5 | Al perder se ofrece **"+1 balón" a cambio de un rewarded**. |
| D6 | Se muestra un **intersticial al final de cada nivel superado**, al avanzar. Desde la Etapa 7: al pasar de la pantalla de victoria al menú principal. |
| D7 | **Todos los futbolistas del nivel son objetivos** (no hay neutrales por ahora). `CharacterDefinition.IsTarget` existe (true) para añadir neutrales en el futuro sin cambiar la arquitectura. |
| D8 | El ragdoll se activa por **golpe del balón** y también cuando el futbolista **se cae, se inclina o lo arrastran** (p. ej. si el balón golpea la caja sobre la que está), por un **impacto** fuerte o al salir del escenario. |
| D9 | Obstáculos: caja normal, caja inamovible, muro rebotador, vidrio irrompible, vidrio rompible (3 golpes; **el balón que lo rompe lo atraviesa**), muro móvil y plataforma móvil, editables por instancia en el Inspector. Añadida después: **caja de vidrio con cajas dentro** (`PF_GlassCrate`, `GlassCrate`): Rigidbody cinemático que recorre una ruta editable por instancia (`PathMover`, como muros y plataformas móviles); se agrieta y se rompe con `GlassDamage`; las 3 cajas de `Contents` (pirámide) viajan fijas dentro (cinemáticas) y al romperse se sueltan quietas y caen en vertical. Excepción a D9: el balón que rompe la caja de vidrio rebota hacia fuera (`GlassCrateBounce`) en lugar de atravesarla, para no empujar las cajas. Uso en niveles 10–12: futbolistas tras vidrio irrompible que solo caen si se rompe la caja cuando pasa por encima. |

## 1. Estado real del proyecto (inspeccionado 2026-10-13)

| Aspecto | Valor encontrado |
|---|---|
| Unity | **6000.6.5f1** (Unity 6.6) |
| Render pipeline | URP 17.6.0, dos assets: `Mobile_RPAsset` / `PC_RPAsset`. Android usa calidad "Mobile" |
| Input | `com.unity.inputsystem` 1.20.0, **activeInputHandler = 1 (solo Input System nuevo)**; `InputSystem_Actions.inputactions` asignado como project-wide actions |
| UI | `com.unity.ugui` 2.6.0 (TextMeshPro viene integrado en uGUI desde Unity 6) |
| Otros paquetes | ai.navigation 2.0.15, timeline 6.6.0, test-framework 1.8.0, visualscripting 1.9.12, collab-proxy 2.13.6, **com.unity.pipeline 0.8.0-exp.1 (experimental)** |
| Escenas | Solo `Assets/Scenes/SampleScene.unity` (Main Camera, Directional Light, Global Volume) |
| Código | Solo plantilla `TutorialInfo` (Readme). Sin asmdefs propios |
| Enter Play Mode | Activado, opción 3 = **sin Domain Reload y sin Scene Reload** |
| Físicas | Valores por defecto: gravedad -9.81, fixed timestep 0.02, solver 6/1, bounce threshold 2, matriz de colisión todo-con-todo, sin capas propias |
| Orientación | AutoRotation con landscape permitido (hay que fijar Portrait) |
| Android | IL2CPP, solo ARM64, minSdk 26, targetSdk automático, app id de plantilla `com.UnityTechnologies.com.unity.template.urpblank`, companyName `DefaultCompany` |
| AdMob | No instalado |
| Control de versiones | No hay `.git` ni `.gitignore` en la raíz |

## 2. Principios que se mantienen en todo el desarrollo

1. **Sin singletons ni estado estático mutable.** (Crítico porque Domain Reload está desactivado.)
2. **DI manual explícita**: solo las *composition roots* resuelven dependencias. El resto recibe dependencias por constructor (C# puro) o por `Construct(...)` (MonoBehaviour). Prohibido `FindObjectOfType`, `GameObject.Find`, `Resolve` fuera de las roots.
3. **Lógica en C# puro**, MonoBehaviours finos (vista/física). La lógica pura se testea en EditMode.
4. **Eventos entre sistemas, llamadas directas dentro de un sistema.** Gameplay, UI y Audio no se referencian entre sí.
5. **Datos en ScriptableObjects** (`*Definition`, `*Config`); ningún número mágico de diseño en código.
6. **Un nivel es un prefab + una definición**, no una escena. Reiniciar = destruir e instanciar de nuevo.
7. **Toda API de terceros detrás de un adapter** (AdMob, almacenamiento).
8. **El progreso nunca retrocede**: `CurrentLevelIndex` solo aumenta, salvo la acción explícita "Empezar desde el nivel 1" de la pantalla Próximamente (D1).

## 3. Ensamblados (asmdef) y dependencias

> Actualizado el 2026-11-12 con las referencias reales de cada `.asmdef` (la versión inicial preveía un asmdef aparte para AdMob; al final AdMob quedó en Infrastructure bajo `#if NOVA_ADMOB`, y PlayFab sí tiene asmdef propio).

```
NovaStriker.Bootstrap               ──► Core, Contracts, Gameplay, Infrastructure, Infrastructure.PlayFab, UI, Unity.InputSystem, UnityEngine.UI
NovaStriker.Gameplay                ──► Core, Contracts, Unity.InputSystem, UnityEngine.UI
NovaStriker.UI                      ──► Core, Contracts, UnityEngine.UI, Unity.TextMeshPro
NovaStriker.Infrastructure          ──► Core, Contracts   (+ SDK precompilados de AdMob y Firebase, bajo #if NOVA_ADMOB / NOVA_FIREBASE)
NovaStriker.Infrastructure.PlayFab  ──► Infrastructure, PlayFab   (defineConstraints: NOVA_PLAYFAB)
NovaStriker.Contracts               ──► Core
NovaStriker.Core                    ──► (nada propio)
NovaStriker.EditorTools             ──► Core, Contracts, Gameplay, Infrastructure, UI, Bootstrap   (solo Editor)
NovaStriker.Tests.Support           ──► Core   (solo con UNITY_INCLUDE_TESTS)
NovaStriker.Tests.EditMode          ──► Core, Contracts, Gameplay, Infrastructure, UI, Bootstrap, Tests.Support   (solo Editor)
```
Gameplay, UI e Infrastructure **no** se conocen: se comunican con eventos e interfaces de Contracts. Unity no compila una referencia que no esté declarada en el asmdef, así que estas reglas se cumplen por construcción. `Infrastructure.PlayFab` solo se compila con el símbolo `NOVA_PLAYFAB`, de modo que el proyecto compila sin el SDK.

## 4. Árbol de carpetas

```
Assets/
└─ _Project/
   ├─ Art/            Materials/ Models/ Textures/ Animations/
   ├─ Audio/          Music/ SFX/ Mixers/ (AM_Main)
   ├─ PhysicsMaterials/  (PM_Ball, PM_Grass, PM_Wood, PM_Glass, PM_Wall)
   ├─ Prefabs/
   │  ├─ Balls/       PF_Ball
   │  ├─ Characters/  PF_Footballer_Target, PF_Footballer_Neutral
   │  ├─ Obstacles/   PF_Box, PF_MovingWall, PF_Glass, PF_Glass_Fractured
   │  ├─ Levels/      PF_Level_001 ...
   │  ├─ UI/          PF_HUD, PF_PauseMenu, PF_VictoryScreen, PF_DefeatScreen, PF_ComingSoonScreen
   │  ├─ VFX/
   │  └─ Bootstrap/   PF_AppRoot
   ├─ Resources/      (solo PF_AppRoot para el arranque en Editor)
   ├─ Scenes/         SC_Boot, SC_MainMenu, SC_Game
   ├─ Settings/
   │  ├─ Config/      SO_GameConfig, SO_BallConfig, SO_ScoreConfig, SO_AdsConfig, SO_AudioLibrary
   │  ├─ Throws/      SO_Throw_Straight, SO_Throw_Curve, SO_Throw_Lob
   │  ├─ Characters/  SO_Character_Target, SO_Character_Neutral
   │  ├─ Obstacles/   SO_Obstacle_Box, SO_Obstacle_Glass, SO_Obstacle_MovingWall
   │  ├─ Levels/      SO_LevelCatalog, SO_Level_001 ...
   │  └─ Input/       IA_Gameplay.inputactions
   └─ Scripts/
      ├─ Core/                     NovaStriker.Core
      │  ├─ DI/                    IServiceContainer, ServiceContainer
      │  ├─ Events/                IGameEvent, IEventBus, EventBus
      │  ├─ StateMachine/          IState, IPayloadState<T>, ITickableState, ISuspendableState, IStateMachine<TId>, StateMachine<TId>
      │  ├─ Pooling/               IPoolable, IPool<T>, ComponentPool<T>
      │  └─ Utils/                 CompositeDisposable
      ├─ Contracts/                NovaStriker.Contracts
      │  ├─ Events/                AppEvents, LevelEvents, ShotEvents, TargetEvents, ScoreEvents, AdsEvents
      │  ├─ Services/              IProgressService, IAudioService, IAdsService, ISceneLoader, IGameCommands, ITimeService
      │  ├─ Persistence/           ISaveStorage, ISerializer, IProgressRepository
      │  ├─ Ads/                   IAdsProvider, AdResult
      │  └─ Data/                  GameLayers, SceneNames, PlayerProgress, PlayerSettings, SfxId, MusicId, KnockdownCause, SurfaceType
      ├─ Gameplay/                 NovaStriker.Gameplay
      │  ├─ States/                GameplayStateMachine, LevelSetupState, AimingState, BallInFlightState,
      │  │                         ShotResolveState, PausedState, VictoryState, DefeatState, ComingSoonState
      │  ├─ Input/                 ISwipeInput, SwipeInputReader, SwipeData
      │  ├─ Throwing/              IThrowStrategy, ThrowContext, ThrowCommand, BallisticSolver, AimTargetResolver, StraightThrowStrategy, CurveThrowStrategy,
      │  │                         LobThrowStrategy, ThrowStrategyDefinition, ThrowController, TrajectoryPreview
      │  ├─ Ball/                  BallView, BallCurveForce, BallSpinVisual, BallSettleDetector, BallPool, BallConfig
      │  ├─ Characters/            FootballerController, RagdollController, RagdollHitReceiver, CharacterDefinition
      │  ├─ Obstacles/             ObstacleView, BreakableGlass, MovingWall, ObstacleDefinition
      │  ├─ Factories/             ICharacterFactory, CharacterFactory, IObstacleFactory, ObstacleFactory, IBallFactory, BallFactory
      │  ├─ Level/                 LevelDefinition, LevelCatalog, LevelLayout, SpawnPoint, LevelBuilder, LevelRulesTracker, KillZone
      │  ├─ Scoring/               ScoreCalculator, ScoreTracker, ScoreConfig
      │  └─ Camera/                GameplayCameraRig
      ├─ Infrastructure/           NovaStriker.Infrastructure
      │  ├─ Persistence/           JsonFileStorageAdapter, PlayerPrefsStorageAdapter, JsonUtilitySerializer, ProgressRepository
      │  ├─ Progress/              ProgressService
      │  ├─ Ads/                   AdsService, AdFrequencyPolicy, NullAdsAdapter, EditorFakeAdsAdapter
      │  ├─ Audio/                 AudioService, AudioEventRouter, AudioLibrary
      │  ├─ Scenes/                SceneLoader
      │  └─ AdMob/                 NovaStriker.Infrastructure.AdMob → AdMobAdsAdapter (NOVA_ADMOB)
      ├─ UI/                       NovaStriker.UI
      │  ├─ Common/                SafeAreaFitter, UIScreen
      │  ├─ MainMenu/              MainMenuView, MainMenuPresenter
      │  ├─ HUD/                   HudView, HudPresenter
      │  ├─ Pause/                 PauseView, PausePresenter
      │  └─ Results/               VictoryView, VictoryPresenter, DefeatView, DefeatPresenter, ComingSoonView, ComingSoonPresenter
      ├─ Bootstrap/                NovaStriker.Bootstrap
      │  ├─ AppCompositionRoot, AppBootstrapper, ApplicationSettingsInitializer
      │  ├─ App/                   AppStateMachine, AppBootState, MainMenuState, GameplayAppState
      │  └─ Scenes/                MainMenuCompositionRoot, GameplayCompositionRoot, GameCommands
      ├─ Editor/                   NovaStriker.EditorTools (Setup/ProjectSetupStage0, validadores de niveles)
      └─ Tests/EditMode/           NovaStriker.Tests.EditMode
Docs/ARCHITECTURE.md   (este archivo, fuera de Assets)
```

## 5. Convenciones de nombres

- Namespace = `NovaStriker.<Ensamblado>[.<Carpeta>]`, p. ej. `NovaStriker.Gameplay.Throwing`.
- Sufijos de tipo: `I*` interfaces, `*Event` (readonly struct), `*State`, `*ThrowStrategy`, `*Factory`, `*Adapter`, `*Service`, `*View` (MonoBehaviour UI), `*Presenter` (C# puro), `*Definition`/`*Config` (ScriptableObject), `*Controller` (MonoBehaviour de gameplay), `*CompositionRoot`.
- Prefijos de assets: `SC_` escena, `PF_` prefab, `SO_` ScriptableObject, `MAT_` material, `PM_` physics material, `SFX_`/`MUS_` audio, `AM_` mixer, `IA_` input actions, `TEX_`, `MDL_`, `ANIM_`.
- Campos privados `_camelCase`; `[SerializeField] private` en lugar de campos públicos.
- Una clase por archivo; el archivo se llama como la clase.
- **Scripts de setup de Editor**: nunca reutilizar una referencia a un asset obtenida antes de `EditorSceneManager.OpenScene`. Abrir la escena descarga los assets no usados y la referencia se guarda como vacía. Hay que cargar por ruta (`AssetDatabase.LoadAssetAtPath`) después de abrir la escena y verificar al final que ninguna referencia quedó vacía.
- **Sin respaldos silenciosos**: si falta configuración obligatoria, se lanza un error claro (`Require`) en vez de usar un valor por defecto que oculta el problema.
- Los MonoBehaviours que usan los tests (probes, dobles de prueba) van en `Tests/Support` (asmdef `NovaStriker.Tests.Support`, runtime, `defineConstraints: UNITY_INCLUDE_TESTS`). Unity no permite añadir a un GameObject un MonoBehaviour de un ensamblado de Editor.

## 6. Patrones: dónde vive cada uno

| Patrón | Implementación | Responsabilidad |
|---|---|---|
| **State** | `StateMachine<TId>` genérica con pila (Core) usada por `AppStateMachine` (Boot → MainMenu → Gameplay) y `GameplayStateMachine` (estados del nivel) | Cada estado decide *cuándo* salir; la máquina solo ejecuta Enter/Exit/Tick |
| **Factory** | `CharacterFactory`, `ObstacleFactory`, `BallFactory` | Instanciar prefab + configurarlo desde su `*Definition` + inyectarle dependencias (event bus, configs). Única vía de entrada de DI a objetos creados en runtime |
| **Object Pool** | `ComponentPool<T> : IPool<T>` envuelve `UnityEngine.Pool.ObjectPool<T>`; `BallPool` lo usa; `BallFactory` es su `createFunc` | Reutilizar balones; `IPoolable.OnSpawned/OnDespawned` resetea velocidades, spin, trail |
| **Observer** | `IEventBus` (instancia inyectada, no estática) con eventos `readonly struct : IGameEvent`; `Subscribe` devuelve `IDisposable` | Comunicación entre sistemas (Gameplay → UI, Audio, Ads, Progress) |
| **Strategy** | `IThrowStrategy` → `StraightThrowStrategy`, `CurveThrowStrategy`, `LobThrowStrategy` | Dado el origen del balón y el **punto objetivo** (donde se levantó el dedo), calcular un `ThrowCommand` que llegue a ese punto. Cada nivel define su estrategia (D3) |
| **Adapter** | `IAdsProvider` ← `AdMobAdsAdapter` / `NullAdsAdapter` / `EditorFakeAdsAdapter`; `ISaveStorage` ← `JsonFileStorageAdapter` / `PlayerPrefsStorageAdapter` | Aislar SDKs y almacenamiento del juego |
| **DI** | `ServiceContainer` + `AppCompositionRoot` (global, persistente) + `*CompositionRoot` por escena (contenedor hijo) | Composición explícita del grafo de objetos |

## 7. Conexión de componentes

### Arranque
1. `SC_Boot` contiene `PF_AppRoot` → `AppCompositionRoot` (`DontDestroyOnLoad`, `[DefaultExecutionOrder(-1000)]`).
2. Crea servicios globales: `EventBus`, `ISaveStorage`→`ProgressRepository`→`ProgressService`, `AudioService`+`AudioEventRouter`, `AdsService`(+adapter elegido por plataforma/define), `SceneLoader`, `TimeService`. Registra todo en el contenedor raíz.
3. Arranca `AppStateMachine` → `AppBootState` (carga progreso, inicializa ads) → `MainMenuState` (carga `SC_MainMenu`).
4. **Siempre** (build y Editor): `AppBootstrapper` (`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`) instancia `Resources/PF_AppRoot`. El estado inicial depende de la escena activa (SC_Boot → Boot, SC_MainMenu → MainMenu, SC_Game → Gameplay), así se puede dar Play desde cualquier escena sin recargarla. `PF_AppRoot` **no** se coloca en ninguna escena.

### Escena de juego
`GameplayCompositionRoot` (en `SC_Game`):
1. Crea un contenedor hijo del raíz.
2. Construye: `BallFactory` → `BallPool`; `CharacterFactory`; `ObstacleFactory`; `LevelBuilder`; `LevelRulesTracker`; `ScoreTracker`; `ThrowController` (con la estrategia activa); `SwipeInputReader`; `GameplayStateMachine` con sus estados; `GameCommands` (implementa `IGameCommands`).
3. Llama `Construct(...)` en los MonoBehaviours de la escena (cámara, preview, vistas UI) y crea los Presenters.
4. Inicia la máquina en `LevelSetupState` con `ProgressService.CurrentLevelIndex`.
5. En `OnDestroy` hace `Dispose` del contenedor hijo (desuscribe eventos, vacía pool).

### Mecánica de tiro (D3, D4)
1. **Presionar**: empieza el apuntado (`AimStartedEvent`). Si el toque empieza sobre la UI (botón de pausa), se ignora.
2. **Arrastrar**: en cada frame, `AimTargetResolver` hace un raycast desde la cámara por la posición del dedo contra la máscara `AimSurface`: suelo, paredes, personajes, obstáculos y un **backstop invisible** al fondo del nivel, para que siempre haya un punto. Se muestran el marcador del objetivo y la trayectoria (`TrajectoryPreview`), calculada con la **misma** estrategia.
3. **Levantar**: el punto final es el objetivo y se dispara.
4. **Cálculo (común a todas las estrategias)**: con aceleración constante `a` (gravedad + aceleración lateral de curva) y un tiempo de vuelo `T` elegido por la estrategia, la velocidad inicial exacta es
   `v0 = (P_objetivo − P_origen − ½·a·T²) / T`.
   - `StraightThrowStrategy`: `T` corto, proporcional a la distancia (tiro tenso), sin curva.
   - `LobThrowStrategy`: `T` derivado de una altura de vértice configurada (tiro bombeado).
   - `CurveThrowStrategy`: añade una aceleración lateral constante. El lado de la curva sale de la desviación del arrastre, o del nivel si se prefiere.
   Al ser analítico, el balón pasa por el punto objetivo salvo que choque antes con algo; esos choques son parte del gameplay.
5. Requisitos para que el balón llegue exacto: `linearDamping = 0` durante el vuelo, la curva se aplica como fuerza constante en `FixedUpdate` (`BallCurveForce`) y el giro del balón es solo visual. Tras el primer impacto se desactiva la fuerza de curva y manda la física normal.

### Flujo de un disparo
`SwipeInputReader` → (`AimingState` lo escucha) → `AimTargetResolver` da el punto objetivo → `ThrowController` usa la `IThrowStrategy` del nivel → obtiene `BallView` del `BallPool` y aplica `ThrowCommand` → publica `ShotFiredEvent` → `BallInFlightState` espera `BallSettleDetector`/`KillZone` → `ShotResolveState` espera que los ragdolls se asienten → decide transición. `RagdollHitReceiver` → `FootballerController` → `CharacterKnockedDownEvent` → `LevelRulesTracker` y `ScoreTracker` actualizan y publican `TargetsChangedEvent` / `ScoreChangedEvent`.

### UI
Views (MonoBehaviour, uGUI) solo muestran y exponen clicks. Presenters (C# puro) se suscriben al bus para mostrar datos y llaman a `IGameCommands` (`Pause`, `Resume`, `Restart`, `NextLevel`, `ContinueWithExtraBall`, `RestartCampaign`, `GoToMainMenu`) para actuar.

### Anuncios (D5, D6)
- **Intersticial al avanzar**: Victoria → se guarda el progreso → pantalla de victoria → "Siguiente" → `IAdsService.TryShowInterstitial("level_complete", onClosed)` → se carga el siguiente nivel (o la pantalla Próximamente). Si el anuncio no está listo, falla o supera un timeout, se continúa sin bloquear.
- **Rewarded "+1 balón"**: la pantalla de derrota muestra el botón solo si `IsRewardedReady` y si no se ha usado en este intento (`maxContinuesPerAttempt = 1` en `SO_AdsConfig`, ajustable). Si se completa con recompensa: `ContinueWithExtraBall()` suma 1 balón y vuelve a Apuntar **sin reconstruir el nivel**, así los personajes derribados siguen derribados. Si se cancela o falla, la derrota se mantiene.
- Mientras se muestra un anuncio: tiempo y audio en pausa (`TimeService`). Los callbacks del SDK se reenvían al hilo principal dentro del adapter antes de publicar eventos.
- La política (cuándo y cuántas veces) vive en `AdsService` y `AdFrequencyPolicy`; el adapter solo habla con el SDK. La UI nunca toca estados directamente.

### Audio
`AudioEventRouter` se suscribe a eventos y traduce a `IAudioService.PlaySfx(SfxId, position)`. Gameplay nunca llama audio directamente. Mixer `AM_Main` con grupos Master/Music/SFX y parámetros expuestos para volumen.

## 8. Eventos principales (Contracts/Events)

| Evento | Datos | Emisor | Oyentes típicos |
|---|---|---|---|
| `AppStateChangedEvent` | from, to | AppStateMachine | Debug, Ads |
| `GameStateChangedEvent` ✅ | from, to (GameStateId) | GameplayFlow | HUD, panel de resultado, Audio |
| `LevelStartedEvent` ✅ | levelIndex, levelCount, displayName | LevelSession | HUD, Audio |
| `LevelProgressChangedEvent` ✅ | targetsRemaining/Total, ballsRemaining/Total | LevelRulesTracker | HUD (sustituye a `TargetsChangedEvent` y `BallsChangedEvent`) |
| `AimStartedEvent` / `AimCancelledEvent` | — | AimingState | Preview, Audio |
| `ShotFiredEvent` | ballsRemaining, strategyId, velocity | ThrowController | HUD, Audio, Camera |
| `BallCollidedEvent` | surface, impulse, point | BallView | Audio, VFX |
| `BallSettledEvent` | reason (Stopped/OutOfBounds/Timeout) | BallSettleDetector/KillZone | BallInFlightState |
| `CharacterKnockedDownEvent` | characterId, isTarget, cause, position | FootballerController | RulesTracker, Score, Audio |
| `ObstacleBrokenEvent` | obstacleType, position | BreakableGlass | Score, Audio |
| `TargetsChangedEvent` | remaining, total | LevelRulesTracker | HUD, estados |
| `BallsChangedEvent` | remaining, total | LevelRulesTracker | HUD |
| `ScoreChangedEvent` | total, delta | ScoreTracker | HUD |
| `PauseChangedEvent` | isPaused | PausedState | UI, Audio |
| `LevelCompletedEvent` ✅ | levelIndex, isLastLevel, ballsRemaining (puntuación: pendiente) | VictoryState | Ads, UI, Audio |
| `LevelFailedEvent` ✅ | levelIndex, reason (`DefeatReason`) | DefeatState | Ads, UI, Audio |
| `ProgressSavedEvent` | currentLevelIndex | ProgressService | Debug |
| `AimUpdatedEvent` | targetPoint, valid | AimingState | TrajectoryPreview, marcador |
| `ExtraBallGrantedEvent` | ballsRemaining | LevelRulesTracker | HUD, Audio |
| `CampaignCompletedEvent` ✅ | levelCount | ComingSoonState | UI (Próximamente) |
| `CampaignRestartedEvent` | — | ProgressService | Debug |
| `AdStartedEvent` / `AdFinishedEvent` | placement, type | AdsService | TimeService, Audio |
| `RewardedAdCompletedEvent` | placement, rewarded | AdsService | DefeatPresenter |

## 9. Máquinas de estado y transiciones

### Modelo de progreso
`PlayerProgress { saveVersion, currentLevelIndex, bestScores[], bestStars[], settings }`.
- `currentLevelIndex` = siguiente nivel por jugar. Si es `>= LevelCatalog.Count`, la campaña está completa.
- `CompleteLevel(i)` solo avanza si `i == currentLevelIndex`.
- `RestartCampaign()` pone `currentLevelIndex = 0` y **conserva las mejores puntuaciones**.
- Si una actualización agrega niveles, un jugador con la campaña completa (`index == Count antiguo`) continúa automáticamente en el primer nivel nuevo.

### AppStateMachine
`AppBootState → MainMenuState ⇄ GameplayAppState`.
Menú "Jugar" → Gameplay con `CurrentLevelIndex` (no hay selector de niveles anteriores). Si la campaña está completa, el juego entra directamente en `ComingSoonState`. Pausa → "Menú" → MainMenu.

### GameplayStateMachine
| Desde | Hacia | Condición |
|---|---|---|
| LevelSetup | Aiming | Nivel construido, contadores inicializados |
| Aiming | Aiming | Tiro con balones restantes: se descuenta y el siguiente balón aparece a los `ReloadDelay` (0,25 s) sin esperar al anterior |
| Aiming | BallInFlight | Se lanzó el último balón |
| BallInFlight | ShotResolve | Todos los balones en juego terminaron (detenidos, fuera o reciclados), tiempo máximo o todos los objetivos caídos |
| ShotResolve | Victory | `targetsRemaining == 0` (prioridad: si el último balón derriba al último objetivo, es victoria) |
| ShotResolve | Aiming | `targetsRemaining > 0` y `ballsRemaining > 0` |
| ShotResolve | Defeat | `targetsRemaining > 0` y `ballsRemaining == 0` |
| Aiming / BallInFlight / ShotResolve | Paused | `IGameCommands.Pause()` |
| Paused | (estado previo) | `Resume()` |
| Aiming / BallInFlight / ShotResolve / Paused / Defeat | LevelSetup (mismo índice) | `RestartLevel()` (desde Victory no: solo se avanza) |
| Aiming | ShotResolve | Un derribo tardío deja el nivel sin objetivos (o se entra sin balones) |
| Victory | (App) MainMenu | Automático a los `VictoryScreenTime` (3 s); progreso ya avanzado. Etapa 10: intersticial aquí |
| LevelSetup | ComingSoon | Se entra con la campaña completa |
| Defeat | Aiming | `ContinueWithExtraBall()` tras rewarded completado (+1 balón, nivel intacto) |
| ComingSoon | LevelSetup (índice 0) | `RestartCampaign()` ("Empezar desde el nivel 1") |
| cualquiera | (App) MainMenu | `GoToMainMenu()` |

Implementación: Paused se **apila** (`Push`) sobre Aiming/BallInFlight/ShotResolve, que reciben `OnSuspend`/`OnResume` en vez de Exit/Enter; `Resume()` = `Pop()`. Cualquier `ChangeState` sale de toda la pila, de arriba hacia abajo. Reglas (Etapa 6, `GameFlowSettings`): `ShotResolve` espera al menos 0,75 s y hasta 1 s sin derribos nuevos (cada derribo en cadena reinicia la cuenta), con un máximo de 3 s; sin objetivos basta la espera mínima. `BallInFlight` tiene una salida de seguridad a los 12 s. Pausa = `Time.timeScale = 0` (con `ITimeScale`; `AudioListener.pause` llegará con el audio en la Etapa 9); UI usa tiempo no escalado. Victory guarda progreso **antes** de mostrar la pantalla. Defeat nunca modifica `CurrentLevelIndex`. Defeat no destruye el nivel hasta que se elige Reintentar o Menú, para permitir el "+1 balón".

## 10. Interfaces esenciales

```csharp
// Core (implementado en Etapa 1)
public interface IGameEvent { }
public interface IEventBus {
    IDisposable Subscribe<T>(Action<T> handler) where T : struct, IGameEvent;
    void Publish<T>(T gameEvent) where T : struct, IGameEvent;
}
public interface IExitableState { void Exit(); }
public interface IState : IExitableState { void Enter(); }
public interface IPayloadState<in TPayload> : IExitableState { void Enter(TPayload payload); }
public interface ITickableState { void Tick(float deltaTime); }
public interface ISuspendableState { void OnSuspend(); void OnResume(); }
public interface IStateMachine<TId> where TId : struct, Enum {   // el valor 0 del enum = None (reservado)
    TId CurrentId { get; } bool HasState { get; } int Depth { get; } event Action<TId, TId> StateChanged;
    void Register(TId id, IExitableState state); bool Contains(TId id);
    void ChangeState(TId id); void ChangeState<TPayload>(TId id, TPayload payload);
    void Push(TId id); void Pop(); void Tick(float deltaTime); void Stop();
}
public interface IServiceContainer : IDisposable {
    void RegisterInstance<T>(T instance, bool disposeWithContainer = false) where T : class;
    void RegisterLazy<T>(Func<IServiceContainer, T> factory, bool disposeWithContainer = false) where T : class;
    bool IsRegistered<T>() where T : class;
    T Resolve<T>() where T : class;
    bool TryResolve<T>(out T instance) where T : class;
    IServiceContainer CreateChild();
}
public interface IPoolable { void OnSpawned(); void OnDespawned(); }
public interface IPool<T> where T : class { int CountActive { get; } int CountInactive { get; }
    T Get(); void Release(T item); void ReleaseAll(); void Prewarm(int count); void Clear(); }
// ComponentPool<T> además ofrece Get(Vector3 position, Quaternion rotation).

// Contracts
// Implementado en Etapa 6 (cada orden se ignora si no corresponde al estado actual):
public interface IGameCommands { void Pause(); void Resume(); void RestartLevel(); void NextLevel(); void RestartCampaign(); void ContinueWithExtraBall(); void GoToMainMenu(); }
// Etapa 6 (en memoria). La Etapa 8 añadirá ajustes y persistencia; puntuación/estrellas, si se añaden, también ahí.
public interface IProgressService { int CurrentLevelIndex { get; } void CompleteLevel(int levelIndex); void RestartCampaign(); }
public interface IProgressRepository { PlayerProgress Load(); void Save(PlayerProgress progress); }
public interface ISaveStorage { bool Exists(string key); string Read(string key); void Write(string key, string data); void Delete(string key); }
public interface ISerializer { string Serialize<T>(T data); T Deserialize<T>(string raw); }
public interface IAudioService { void PlaySfx(SfxId id, Vector3? position = null); void PlayMusic(MusicId id); void SetMusicVolume(float v); void SetSfxVolume(float v); }
public interface ISceneLoader { void Load(string sceneName, Action onLoaded = null); }
public interface IAdsService { bool IsRewardedReady { get; } void TryShowInterstitial(string placement, Action onFinished); void ShowRewarded(string placement, Action<bool> onCompleted); }
public interface IAdsProvider { void Initialize(Action onReady); bool IsInterstitialReady { get; } bool IsRewardedReady { get; }
    void LoadInterstitial(); void ShowInterstitial(Action onClosed); void LoadRewarded(); void ShowRewarded(Action<bool> onCompleted); }

// Gameplay
public interface ISwipeInput { event Action<Vector2> PressStarted; event Action<Vector2> Dragged; event Action<SwipeData> Released; void Enable(); void Disable(); }
public interface IAimTargetResolver { bool TryResolve(Vector2 screenPosition, out Vector3 worldPoint); }
// ThrowContext: origin, target, swipe (para el lado de la curva), gravity. ThrowCommand: initialVelocity, constantAcceleration (curva), flightTime, visualSpin
public interface IThrowStrategy { string Id { get; } bool TryCreateThrow(in ThrowContext ctx, out ThrowCommand command);
    int SampleTrajectory(in ThrowCommand command, Vector3 origin, Vector3[] buffer); } // usada por la preview
public interface ICharacterFactory { FootballerController Create(CharacterDefinition def, SpawnPoint point, Transform parent); }
public interface IObstacleFactory { ObstacleView Create(ObstacleDefinition def, SpawnPoint point, Transform parent); }
public interface IBallFactory { BallView Create(); }
```

`AdsService` (no el adapter) contiene la política (frecuencia de intersticiales, no mostrar en pausa); el adapter solo traduce llamadas al SDK.

## 11. Físicas

- Capas: `Ball`, `Ragdoll`, `Obstacle`, `Breakable`, `Environment`, `KillZone`, `Debris`, `AimBackstop` (solo raycasts de apuntado; no colisiona con nada). Matriz definida explícitamente (p. ej. `Debris` no colisiona con `Ball` ni con `Ragdoll`).
- Balón: `Rigidbody` con `CollisionDetectionMode.ContinuousDynamic`, interpolación, `PM_Ball` con rebote; curva como aceleración constante en `FixedUpdate` (`BallCurveForce`) hasta el primer impacto; el giro es visual (`BallSpinVisual`). Unity 6 usa `linearVelocity` / `PhysicsMaterial`.
- Ragdoll: huesos con `CharacterJoint`; cuerpos kinemáticos mientras el Animator controla; al impacto que supere el umbral (`Collision.impulse`), se desactiva el Animator y se vuelven dinámicos. Iteraciones de solver por rigidbody (`Rigidbody.solverIterations`), no globales.
- Derribo también por caída en `KillZone` o por golpe de otro ragdoll/obstáculo.
- Vidrio: se reemplaza por prefab prefracturado; fragmentos en capa `Debris` y se desactivan tras un tiempo.
- Paredes móviles: Rigidbody kinemático movido con `MovePosition` en `FixedUpdate`.

## 12. Riesgos y decisiones a mantener

1. **Domain Reload desactivado**: cualquier estático sobrevive entre Plays. Regla: nada estático mutable; si es inevitable, resetear con `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`. Toda suscripción se libera con `IDisposable`.
2. **Solo Input System nuevo**: `UnityEngine.Input` legacy lanza excepción. EventSystem con `InputSystemUIInputModule`. Usaremos un asset propio `IA_Gameplay` sin modificar `InputSystem_Actions`.
3. **`com.unity.pipeline` 0.8.0-exp.1** es experimental y no lo usaremos; se revisa en la etapa final (D2).
3b. **Precisión del tiro**: la integración discreta de PhysX puede desviar unos centímetros del punto calculado. Se mitiga con `Interpolate`, damping 0 en vuelo y una prueba automática en PlayMode que mida el error de llegada. Si se necesita más precisión, se corrige la velocidad en `FixedUpdate` mientras no haya impacto.
3c. **Toques accidentales**: como se dispara al levantar el dedo, un toque corto gasta un balón. `SO_GameConfig.minAimDuration` (0 por defecto) permite exigir una duración mínima si las pruebas lo piden.
4. **Visual Scripting** instalado y sin uso: se deja por ahora.
5. **Orientación**: hay que fijar Portrait (Etapa 0). CanvasScaler referencia 1080×1920.
6. **Application ID y Company Name de plantilla**: se definen en la etapa final (D2), antes del primer build a Play Store; el ID no se puede cambiar después de publicar.
7. **AdMob**: el plugin no está en el registro de Unity; se integra en etapa propia, aislado en asmdef con `NOVA_ADMOB`. Puede exigir cambios de Gradle/minSdk: se verificará al integrarlo.
8. **Estabilidad de ragdolls en móvil**: limitar ragdolls activos por nivel, CCD solo en el balón, `maxDepenetrationVelocity` controlada, presupuesto de rigidbodies por nivel.
9. **Reinicio fiable**: destruir e instanciar el layout del nivel en lugar de resetear objeto por objeto; los balones vuelven al pool.
10. **Sin modelos de personajes**: se usarán ragdolls de primitivas (placeholder) con la misma interfaz; el arte se cambia después.
11. **Guardado**: escritura atómica (archivo temporal + reemplazo) y campo `saveVersion` para migraciones.
12. **FPS**: `Application.targetFrameRate = 60` en Android (por defecto móvil es 30).
13. **Sin control de versiones** en la carpeta: recomendado inicializar Git (o Unity Version Control) antes de la Etapa 1.

## 13. Plan por etapas

| Etapa | Contenido | Resultado verificable |
|---|---|---|
| 0 ✅ | Carpetas, asmdefs, escenas `SC_Boot/SC_MainMenu/SC_Game`, Build Settings, Portrait, capas y matriz, targetFrameRate | Proyecto compila, escenas en build |
| 1 ✅ | Core: ServiceContainer, EventBus, StateMachine, pool base + tests EditMode | Tests en verde |
| 2 ✅ | Bootstrap: AppCompositionRoot, EditorBootstrapper, SceneLoader, estados de App, menú placeholder | Boot → Menú → Juego → Menú |
| 3 ✅ | Input (presionar/arrastrar/soltar), AimTargetResolver, 3 estrategias con solución analítica, balón, pool, preview de trayectoria, cámara | El balón llega al punto donde se soltó el dedo; prueba de precisión |
| 4 ✅ | Ragdoll footballer, CharacterFactory, detección de derribo | Jugadores caen al impacto |
| 5 ✅ | Obstáculos: cajas, paredes móviles, vidrio rompible, ObstacleFactory | Interacciones físicas |
| 6 ✅ | LevelDefinition/Catalog, LevelBuilder, RulesTracker, máquina de estados de la partida, HUD con contadores, paneles provisionales, 3 niveles de ejemplo (puntuación aplazada) | Ganar/perder/reintentar/avanzar |
| 7 ✅ | UI final: victoria animada → menú (3 s), menú con escena 3D y panel de nivel (grupos de 10), Próximamente aparte, HUD, pausa, derrota ("+1 balón" desactivado), SafeArea, fuente Lilita One | Flujo completo jugable |
| 8 ✅ | Persistencia: adapters, repositorio, progreso (incl. RestartCampaign) y ajustes | El progreso sobrevive al cierre |
| 9 ✅ | Audio: AudioService, AudioEventRouter, SO_SoundLibrary, ajustes de volumen | Música y SFX por eventos |
| 10 ✅ | Ads: banner superior fijo, intersticial (cada 4 victorias, desde la 16 cada 2), recompensado "+1 balón" (una vez por intento); adapters simulado/AdMob/ninguno | Anuncios en Editor y en Android |
| 11 🔶 | Android: paquete y nombre, keystore, AAB IL2CPP ARM64, revisión de versionCode, IDs reales de AdMob y dependencias (`ProjectSetupStage11`); pilas estables (`PhysicsSettler` duerme cajas y futbolistas 0,4 s después de construir el nivel; `PM_Footballer` con fricción 1); perfilado en dispositivo | AAB estable a 60 FPS |

## 14. Registro de etapas

### Etapa 0 (2026-10-13)
- Asmdefs creados (todos con `autoReferenced: false`): Core, Contracts, Gameplay, Infrastructure, UI, Bootstrap, EditorTools y Tests.EditMode. El asmdef de AdMob se crea en la Etapa 10.
- Referencias a paquetes verificadas contra Library/PackageCache: `Unity.InputSystem`, `UnityEngine.UI`, `Unity.TextMeshPro`.
- `GameLayers` (6 Ball, 7 Ragdoll, 8 Obstacle, 9 Breakable, 10 Environment, 11 KillZone, 12 Debris, 13 AimBackstop) y `SceneNames`.
- `ApplicationSettingsInitializer`: 60 FPS y pantalla siempre encendida, antes de cargar la primera escena.
- Menú **NovaStriker/Setup/Etapa 0 - Configurar proyecto** (idempotente): carpetas, capas (no sobrescribe capas ocupadas), matriz de colisión, escenas SC_Boot/SC_MainMenu/SC_Game, Build Settings y orientación Portrait.
- `SampleScene` queda fuera del build pero no se borra.
- `ProjectSetupTests` (EditMode) verifica capas, matriz, orden de escenas y orientación.

### Etapa 1 (2026-10-13)
- **DI** `ServiceContainer`: sin reflexión (seguro con IL2CPP). Registro por instancia o perezoso, un registro por tipo, contenedores hijos que buscan en el padre, detección de dependencias circulares. Al liberarse, libera primero los hijos y luego sus servicios propios (`disposeWithContainer`), en orden inverso de creación.
- **Eventos** `EventBus`: canales tipados, sin asignaciones de memoria al publicar structs. Una excepción en un handler se reporta y no corta a los demás. Suscribirse durante una publicación aplica desde la siguiente; desuscribirse aplica de inmediato. Admite publicaciones anidadas. `GetSubscriberCount<T>()` sirve para detectar fugas de suscripciones.
- **Estados** `StateMachine<TId>`: con pila (`Push`/`Pop` para overlays como Pausa). Las transiciones pedidas dentro de Enter/Exit se encolan. Admite payload tipado. Un error de tipo de estado se detecta antes de salir del estado actual.
- **Pool** `ComponentPool<T>`: `Get(pos, rot)` coloca el objeto antes de activarlo; avisa a todos los `IPoolable` de la jerarquía; detecta doble devolución; `ReleaseAll()` tolera objetos destruidos desde fuera. Se llama `IPool` para no chocar con `UnityEngine.Pool.IObjectPool`.
- `CompositeDisposable` para agrupar suscripciones.
- Tests EditMode: 56 (ServiceContainer 15, EventBus 13, StateMachine 17, ComponentPool 11). Verificados fuera de Unity con .NET 8 + C# 9; el pool, con un stub que replica la lógica pública de `UnityEngine.Pool.ObjectPool`. Corrección tras el primer run en Unity: 10 tests del pool fallaban porque `PoolableProbe` estaba en el ensamblado de tests (solo Editor) y Unity no permitió añadirlo con `AddComponent`. Se movió a `NovaStriker.Tests.Support`.

### Etapa 2 (2026-10-13)
- `AppBootstrapper` sustituye al "EditorBootstrapper" previsto: crea la raíz global en builds y en el Editor.
- `AppCompositionRoot`: registra `IEventBus`, `ISceneLoader` e `IAppNavigator`, dirige la `StateMachine<AppStateId>` y publica `AppStateChangedEvent`.
- Estados de la app: `AppBootState`, y `SceneAppState` (una sola clase para MainMenu y Gameplay, parametrizada por escena). Al entrar carga la escena y compone su raíz; al salir la libera. Si se sale del estado antes de que termine la carga, esa carga se ignora.
- Raíces de escena: `SceneCompositionRoot` (base: contenedor hijo + `CompositeDisposable`), `MainMenuCompositionRoot`, `GameplayCompositionRoot` (provisional). Se localizan con `ISceneRootProvider` entre los objetos raíz de la escena cargada: la única búsqueda en escena permitida, y solo en la capa de composición.
- **Regla**: los objetos de escena no usan servicios en Awake/Start; esperan a la composición.
- UI en MVP: interfaces de vista (`IMainMenuView`), vistas uGUI que solo emiten clicks, presenters C# puros (`IDisposable`). La UI navega con `IAppNavigator`, que ignora peticiones mientras se carga una escena.
- `GameplayPlaceholder*` es **provisional**: se eliminó en la Etapa 6.
- Menú **NovaStriker/Setup/Etapa 2**: crea `Resources/PF_AppRoot` y la UI provisional (Canvas 1080×1920 ajustado al ancho, EventSystem con `InputSystemUIInputModule`). Requiere haber importado TMP Essential Resources.
- Tests nuevos: 15 (AppFlow 11, Presenters 4). Total EditMode esperado: 87.

### Etapa 3 (2026-10-13)
- **Solver balístico** (`BallisticSolver`): v0 = (Δp − ½aT²)/T − ½·a·dt. El término −½·a·dt compensa el Euler semi-implícito de la física; T se redondea a múltiplos de `fixedDeltaTime`. Sin compensación el error sería de unos 10 cm por segundo de vuelo (hay un test que lo documenta).
- **Estrategias** (Strategy): `StraightThrowStrategy` (T = distancia/velocidad, acotado), `LobThrowStrategy` (T sale de la altura del vértice), `CurveThrowStrategy` (aceleración lateral constante; el trazo arqueado a la derecha abre la trayectoria a la derecha). Datos en ScriptableObjects `*ThrowDefinition` → `CreateStrategy()`.
- **Input**: `PointerInputReader` con acciones creadas en código (`<Pointer>/press`, `<Pointer>/position`: táctil y ratón), por sondeo en Tick. **Decisión**: no se crea `IA_Gameplay.inputactions`; no hace falta un asset. UI bloqueante: `EventSystemUiHitTester` (raycast de UI, fiable con táctil).
- **Apuntado** (`AimController`): presionar → arrastrar (preview y curvatura en vivo con `SwipeGestureTracker`) → soltar = tiro al punto bajo el dedo (`PhysicsAimTargetResolver`, máscara `GameLayers.AimMask`). Sin objetivo o con un toque más corto que `MinAimDuration` → `AimCancelledEvent` y el balón no se gasta.
- **Balón**: `BallView` (kinemático y con CCD especulativo en espera; dinámico y con CCD continuo en vuelo), `BallCurveForce` (`ForceMode.Acceleration` en FixedUpdate hasta el primer impacto), `BallSettleDetector` (C# puro: Stopped/Timeout), `KillZone` → OutOfBounds.
- **Pool**: `BallFactory` (Factory + inyección) → `BallPool` (`ComponentPool`): recicla solo al salir del escenario; los balones detenidos se quedan como objetos físicos hasta `ReleaseAll` (reinicio de nivel); si se supera `MaxActiveBalls` recicla el más antiguo. Tras `ReleaseAll` hay que llamar a `IThrowController.ClearReadyBall()`.
- **Loop**: `ITickable` e `IClock` en Core; `SceneCompositionRoot` actualiza sus tickables en Update.
- **Provisional**: `ShotSandbox` (balones infinitos, recarga a los 0,6 s) y botón "TIRO" para alternar estrategias. Desaparecen en la Etapa 6.
- Menú **NovaStriker/Setup/Etapa 3**: materiales URP, `PM_Ball` (rebote 0,65), `PF_Ball` (Ø 0,22 m, 0,43 kg, damping lineal 0), SOs de configuración y tiro, y `[Sandbox]` en SC_Game (suelo 20×50 m, backstop, KillZone, 4 cajas, punto de saque, preview).
- Tests: 50 nuevos, 6 de ellos de **precisión con PhysX real** (`Physics.Simulate`, tolerancia 5 cm; registran el error medido). Total EditMode esperado: 137.

#### Corrección Etapa 3 (2026-10-13)
- Bug: el setup de Etapa 3 dejó vacías en SC_Game las referencias a `PF_Ball`, `SO_BallConfig`, `SO_AimConfig` y los tres `SO_Throw_*`, porque las tomó antes de `OpenScene`. Por eso el botón TIRO solo mostraba "Recto" (respaldo silencioso del sandbox).
- Corregido: el setup carga los assets por ruta después de abrir la escena y verifica todas las referencias del `GameplayCompositionRoot`. El sandbox ya no tiene respaldo: si falta una estrategia, lanza un error.

### Etapa 4 (2026-10-13)
- **Modelo**: `Player_Tpose.fbx` (Humanoid, esqueleto `Bip001` asignado por el avatar) y `HipHopDacingAnimation.fbx` (Humanoid, avatar copiado, en bucle). Root motion desactivado: baila en el sitio.
- **Futbolista en pie** (`FootballerController`): Rigidbody raíz con cápsula, gravedad activa y rotación X/Z congelada; el modelo anima encima con los huesos kinemáticos y sus colliders apagados. Así, si el apoyo se mueve, el futbolista cae de verdad.
- **Detección** (`KnockdownDetector`, C# puro; umbrales en `KnockdownSettings` dentro de `CharacterDefinition`):
  - Caída: sin apoyo durante `UngroundedGrace`, o velocidad vertical por debajo de `-FallSpeed`.
  - Inclinación: la normal del apoyo supera `MaxSupportTiltDegrees` durante `TiltGrace`. El apoyo se mide con un raycast desde la cadera, que da la normal de la cara (no la del contacto con el borde).
  - Arrastre: velocidad horizontal mayor que `PushedSpeed`.
  - Choques (`CollisionClassifier` por capa): Ball ≥ `BallHitMinSpeed`; Obstacle/Breakable/Ragdoll/Debris ≥ `ImpactMinSpeed`. Los choques cuentan aunque no haya pasado `ArmingDelay`; caída, inclinación y arrastre esperan a que se asiente.
  - KillZone → OutOfBounds.
- **Ragdoll** (`RagdollRig`): 11 huesos generados desde el avatar Humanoid (`HumanoidRagdollBuilder`): cadera, torso, cabeza, muslos, piernas, brazos y antebrazos, con CharacterJoint (límites aproximados), projection activado y 12 iteraciones de solver por hueso. Al activarse: se apaga el Animator, los huesos pasan a dinámicos heredando la velocidad, y se aplica el impulso del golpe (`HitImpulseCalculator`, independiente del signo de `relativeVelocity`, con límite `MaxHitImpulse`) al hueso más cercano.
- **Evento** `CharacterKnockedDownEvent(Id, IsTarget, Cause, Position)`, una vez por futbolista.
- **Factory**: `CharacterFactory : ICharacterFactory` (instancia, id único por escena, `Construct`). Es la única forma de crear futbolistas en runtime.
- **Masas arcade**: raíz 12 kg y ragdoll 12 kg, para que el balón (0,43 kg) y las cajas (4 kg) los derriben con facilidad. Se ajustan en `ProjectSetupStage4` y se vuelve a ejecutar el menú.
- Menú **NovaStriker/Setup/Etapa 4**: crea `AC_Footballer.controller`, **regenera** `PF_Footballer` en cada ejecución (conserva el GUID), crea `SO_Character_Footballer` y añade 4 puntos de aparición al sandbox (3 sobre cajas, 1 en el suelo). Carga por ruta tras abrir la escena y verifica las referencias.
- **Provisional**: `KnockdownLogger` escribe cada derribo en consola ("Total: x/4").
- Limitación conocida: en pie, solo la cápsula recibe golpes. Un balón que roza solo un brazo extendido no derriba.
- Tests nuevos: 21. Total EditMode esperado: 158.

### Etapa 5 (2026-10-13)
- **Colocación**: los obstáculos son prefabs que se colocan directamente en el nivel y se ajustan por instancia (tamaño = escala). Tras instanciar el nivel, `ObstacleInstaller` (composición) inyecta `ObstacleServices` (bus de eventos y fragmentos) en todos los `IObstacle` bajo la raíz del nivel.
- **Tipos** (`ObstacleType`): `PF_Box` (dinámica, 4 kg, `Obstacle`), `PF_StaticBox` (estática, `Environment`, puede flotar), `PF_Wall` (estático, `PM_Bouncy` 0,85 con Maximum), `PF_Glass` (estático y transparente; bloquea balón y apuntado), `PF_GlassBreakable` (`Breakable`), `PF_MovingWall` y `PF_MovingPlatform` (kinemáticos con `PathMover`). Las cajas inamovibles, muros y vidrios van en `Environment`, así que nunca cuentan como "impacto" para un futbolista.
- **Rutas** (`PathMover` + `PathMotion`, C# puro): puntos de ruta como desplazamientos desde la posición inicial (ejes del mundo), velocidad, pausa en cada punto, PingPong/Loop, suavizado, desfase y retraso. La posición es función pura del tiempo, sin deriva. `PathMoverEditor` dibuja la ruta y permite arrastrar los puntos en la escena, con deshacer.
- **Plataforma móvil**: `PathMover` implementa `ISupportSurface`. El futbolista sondea su apoyo; si es una superficie móvil, acompaña su velocidad horizontal, y caída y arrastre se miden **respecto al apoyo**. Si el apoyo es una caja dinámica no se fuerza nada: si la caja se desliza, el futbolista sí cuenta como arrastrado.
- **Vidrio rompible** (`BreakableGlass` + `GlassDamage`, C# puro): golpes válidos de balón (≥ `MinBallSpeed`) o de cajas/futbolistas (≥ `MinImpactSpeed`). Cada golpe aumenta el daño visual (color/opacidad con MaterialPropertyBlock) y publica `GlassCrackedEvent`. Al llegar a `HitsToBreak` (3 por defecto): apaga el collider, devuelve al balón la velocidad previa al choque (`BallView.RestorePreImpactVelocity`, capturada en FixedUpdate antes del paso de física), genera fragmentos y publica `ObstacleBrokenEvent`.
- **Fragmentos**: `DebrisPool` (Factory + `ComponentPool`) con `DebrisShard` en la capa `Debris`, que no colisiona con balón ni futbolistas. Rejilla calculada por `ShardLayout` (C# puro); cada fragmento vuelve al pool tras ~3 s.
- Menú **NovaStriker/Setup/Etapa 5**: crea materiales (vidrio transparente URP), `PM_Box`/`PM_Bouncy`/`PM_Glass` y los 8 prefabs (solo si no existen, para conservar tus cambios), y añade al sandbox un ejemplo de cada obstáculo más 5 futbolistas (sobre la caja inamovible, detrás de cada vidrio, detrás del muro móvil y sobre la plataforma).
- Limitación: el agrietado es un cambio de color y opacidad, sin textura de grietas (no hay arte todavía).
- Tests nuevos: 9. Total EditMode esperado: 167.

### Etapa 6 (2026-10-14)
- **Reglas** (decisión del usuario): cada nivel tiene un número de futbolistas a derribar y de balones. Victoria = todos los objetivos derribados; derrota = sin balones y quedan objetivos. Si el último balón derriba al último objetivo, es victoria.
- **Niveles**: `LevelDefinition` (nombre, balones, estrategia de tiro D3, prefab de escenario) y `LevelCatalog` (orden de juego). El escenario es un prefab con `LevelLayout` (punto de saque y de cámara), suelo, `KillZone`, `AimBackstop`, obstáculos colocados directamente y `FootballerSpawnPoint` (con su `CharacterDefinition`). **El número de objetivos no se configura**: sale de los puntos de aparición.
- `LevelBuilder`: instancia el escenario, inyecta los obstáculos y crea los futbolistas con `CharacterFactory`. `LevelSession` (`ILevelSession`): reinicio = descargar (balones y fragmentos al pool, `ClearReadyBall`, nivel desactivado y destruido) y reconstruir; coloca el punto de saque (`IThrowController.SetSpawnPosition`, nuevo), la estrategia y la cámara (`ILevelCamera`, adapter sobre la cámara). Un nivel sin objetivos lanza un error claro.
- **Cuenta** (`LevelRulesTracker`): escucha `CharacterKnockedDownEvent` y solo cuenta los futbolistas del nivel actual, una vez cada uno (las caídas en cadena cuentan). Los balones se gastan al disparar. `AddBalls` prepara el "+1 balón".
- **Condiciones** (Strategy): `IVictoryCondition` (`AllTargetsDownCondition`) e `IDefeatCondition` (`OutOfBallsCondition`) evaluadas por `LevelOutcomeEvaluator`, con la victoria primero. Una condición nueva (p. ej. derribar a un neutral) es otra clase; `DefeatReason` ya está preparado.
- **Partida** (`GameplayFlow`, State): LevelSetup → Aiming → BallInFlight → ShotResolve → Aiming | Victory | Defeat; sin niveles → ComingSoon (D1); Paused apilado (congela el tiempo y bloquea el apuntado). La derrota solo se decide cuando el último tiro termina y la física se calma (`ShotResolveTimer`).
- **Órdenes** (`GameCommands : IGameCommands`): Pause/Resume, RestartLevel, NextLevel (punto del futuro intersticial, D6), RestartCampaign, ContinueWithExtraBall (lista para el rewarded de la Etapa 10, sin botón aún) y GoToMainMenu (quita la pausa antes de salir).
- **Progreso**: `InMemoryProgressService` en el contenedor global (sobrevive a los cambios de escena). Solo avanza; `RestartCampaign` vuelve al nivel 1. Persistencia en la Etapa 8.
- **UI (MVP)**: `HudPresenter`/`HudView` muestra "NIVEL n", "Por derribar: x/y" y "Balones: n", y el botón PAUSA solo mientras se juega. `ResultPanelPresenter`/`ResultPanelView` (provisional): Pausa (Continuar/Reiniciar), Victoria (Siguiente), Derrota (Reintentar), Próximamente (Empezar desde el nivel 1), y Menú siempre.
- **Retirado**: `ShotSandbox`, `KnockdownLogger`, el botón TIRO y `GameplayPlaceholder*` (sus archivos quedan vacíos para borrarlos a mano). Los setups de las Etapas 2–5 ya no tocan SC_Game; solo crean sus assets.
- Menú **NovaStriker/Setup/Etapa 6 - Niveles, reglas y HUD**: crea `PF_Level_01` (Cajas, Recto, 4 futbolistas, 5 balones), `PF_Level_02` (Vidrios y muros, Bombeado, 3 futbolistas, 7 balones) y `PF_Level_03` (En movimiento, Curva, 3 futbolistas, 5 balones), sus `SO_Level_0x` y `SO_LevelCatalog` (solo si faltan; en definiciones existentes rellena solo referencias vacías). En SC_Game retira `[Sandbox]` y el canvas provisional, crea `[Game]` (Level, Characters, Balls, Debris, AimPreview) y `Canvas_Gameplay`, asigna y verifica referencias.
- Puntuación: aplazada (no forma parte de las reglas confirmadas).
- Tests nuevos: 35 (LevelRules 11, GameplayFlow 17, presenters HUD/resultado 6, punto de saque 1); se retiran 3 del sandbox/placeholder. Total EditMode esperado: 199.

#### Ajuste Etapa 6 (2026-10-14): recarga casi instantánea
- Petición del usuario: el siguiente balón tardaba demasiado (se esperaba a que el anterior se detuviera y la escena se calmara).
- Ahora el siguiente balón aparece `ReloadDelay` = 0,25 s después de cada tiro (mínimo 0,1 s para no solaparse con el que sale), y se puede seguir disparando con los anteriores aún en juego. `BallInFlight` pasa a ser "último balón lanzado": espera a que **todos** los balones en juego terminen (`LevelRulesTracker.BallsInPlay`) y luego `ShotResolve` decide.
- Nuevo `BallSettleReason.Recycled`: un balón que el pool recoge en pleno juego avisa, para que no se quede esperando a un balón que ya no existe.
- Tests: +4 (flujo 20, reglas 12). Total EditMode esperado: 203.

### Etapa 7 (2026-10-14)
- **Decisiones del usuario** (con capturas de referencia de otro juego, usadas solo como inspiración de estilo):
  - La victoria se muestra unos 3 s con un mensaje animado ("¡VICTORIA!" + "NIVEL N COMPLETADO") y después se pasa **automáticamente** al menú principal. Desaparece la orden `NextLevel`.
  - Menú principal con la **escena 3D** detrás (campo, futbolista bailando y un balón), un minipanel "NIVEL N" con **cuadraditos agrupados de 10 en 10** (amarillo = superado, verde = actual y algo más alto, azul oscuro = sin jugar; no interactuables) y el botón JUGAR debajo.
  - "Próximamente nuevos niveles" es una **pantalla aparte** del menú; "Empezar desde el nivel 1" reinicia el progreso y vuelve al menú normal.
  - HUD, pausa y derrota con el mismo estilo; "+1 BALÓN" visible pero desactivado hasta la Etapa 10.
- **Partida**: `VictoryState` (tickable) avanza el progreso, publica `LevelCompletedEvent` y a los `GameFlowSettings.VictoryScreenTime` llama a `IAppNavigator.GoToMainMenu()` una sola vez. `ComingSoonState` se mantiene como respaldo si se entra a jugar sin niveles.
- **UI (MVP)**: `MainMenuPresenter` (progreso + nº de niveles del catálogo; `LevelProgressGroup` calcula los cuadraditos), `VictoryPresenter`/`VictoryView` (pop con `UiEasing.BackOut`, balones con `DampedBounce`, tiempo no escalado), `ResultPanelPresenter` (pausa y derrota; `ResultPanelContent` admite botón secundario desactivado y estilo Neutral/Negative). `SafeAreaFitter` + `SafeAreaMath` ajustan cada pantalla a `Screen.safeArea`.
- **Arte**: sprites originales generados (panel, botón con labio, botón redondo, cuadradito, patrón de hexágonos repetible, cartel, balón, icono de futbolista) en `Art/UI`; se tiñen con `Image.color`. Fuente **Lilita One** (Google Fonts, licencia OFL incluida en `Art/Fonts/LilitaOne-OFL.txt`) convertida a TMP con atlas dinámico + material con contorno y sombra.
- **Editor**: `UiKit` (importación de sprites, fuente, ayudas de UI), `GameplayUiBuilder` (lo usan las Etapas 6 y 7) y `MainMenuSceneBuilder`. Menú **NovaStriker/Setup/Etapa 7 - Interfaz final**: sustituye el `Canvas_Gameplay` provisional y el `Canvas_MainMenu` de la Etapa 2; lo ya creado en formato Etapa 7 no se toca (para rehacerlo, borrar el canvas y volver a ejecutar).
- El futbolista del menú es decorado: no pasa por la Factory, su Rigidbody es kinemático y, sin `Construct`, no evalúa derribos.
- Tests: +15 (menú 2, victoria 1, derrota 1, resultado oculto en victoria 1, flujo +1, lógica de UI 10). Total EditMode esperado: 218.

### Etapa 8 (2026-10-14)
- **Adapters**: `ISaveStorage` ← `FileSaveStorage` (carpeta `Application.persistentDataPath/Save`) e `ISerializer` ← `JsonUtilitySerializer`. Cambiar a PlayerPrefs o a la nube = otro adapter, sin tocar el juego.
- **Escritura atómica**: se escribe `clave.tmp`, la versión anterior pasa a `clave.bak` y el temporal ocupa su sitio. Si la app se cierra a mitad, al leer se usa el `.bak`. Las claves se validan (sin rutas).
- **Repositorio** `SaveSlot<T>`: sin archivo → valores por defecto; archivo dañado → aviso, copia `clave.corrupt` y valores por defecto; fallo al escribir → error en consola sin interrumpir el juego.
- **Progreso** `PersistentProgressService : IProgressService` (`progress.json`, `ProgressData` con `SaveVersion` para migraciones): carga al crearse y guarda en cada cambio (solo si cambia). Mantiene las reglas: solo avanza y `RestartCampaign` vuelve a 0. Si una actualización añade niveles, quien estaba en "Próximamente" sigue por el siguiente nivel nuevo.
- **Ajustes** `IGameSettings` (Contracts) ← `PersistentGameSettings` (`settings.json`): volumen de música (0,8) y efectos (1), limitados a [0, 1], con evento `Changed`. Los usará el audio en la Etapa 9.
- `AppCompositionRoot` registra `ISaveStorage`, `IProgressService` (persistente) e `IGameSettings`. `InMemoryProgressService` queda para tests.
- Menú del Editor **NovaStriker/Guardado**: abrir la carpeta de guardado y borrar progreso y ajustes (fuera de Play).
- Tests nuevos: 11 (archivos reales en carpeta temporal y almacenamiento en memoria). Total EditMode esperado: 229.

### Etapa 9 (2026-10-14)
- **Sonidos elegidos por el usuario**: música de menú y de partida, apuntar, patada, golpes del balón (suelo, caja, muro/caja inamovible, futbolista), quejido al derribar (3 variantes), vidrio agrietándose y rompiéndose, caja contra caja/suelo, victoria, derrota, click, "whoosh" de panel y ambiente de público. Descartados: "balón nuevo" y "cuerpo cayendo".
- **Archivos**: generados por síntesis (provisionales, `Audio/SFX/SFX_*.wav`, `Audio/Music/MUS_*.wav` y `AMB_Crowd.wav`). Se sustituyen reemplazando el archivo con el mismo nombre o arrastrando otro clip a su entrada de `SO_SoundLibrary` (admite variantes).
- **Contratos**: `SfxId`, `MusicId`, `IAudioService` (PlaySfx con intensidad, PlayUi que suena también en pausa, PlayMusic con fundido, ambiente, duck y pausa). `IGameSettings.Save()`: las barras aplican el volumen al momento y se guarda al cerrar el panel, al salir del menú o al pasar la app a segundo plano.
- **Adapter/servicio** `AudioService` (Infrastructure, global): 2 fuentes de música con fundido cruzado, ambiente, 10 voces de juego y 3 de interfaz; variación de tono y volumen, sin repetir variante (`VariantPicker`) y con intervalo mínimo por efecto (`SfxThrottle`). Sin AudioMixer: Unity no permite crear mixers por código, así que los volúmenes de Música y Efectos se aplican en las fuentes. El ambiente usa el volumen de Efectos.
- **Observer** `AudioEventRouter`: traduce eventos a sonidos. `BallCollidedEvent` ahora lleva la normal del contacto (suelo si apunta hacia arriba, si no muro). Nuevo `ObstacleImpactEvent`, que publica `ObstacleView` en las cajas dinámicas (sin contar el balón, con umbral y enfriamiento). Victoria y derrota bajan la música; la pausa detiene efectos y ambiente y baja la música.
- **UI**: `ButtonClickSounds` (composición) añade el click a todos los botones del canvas. `SettingsPresenter`/`SettingsView`: engranaje en el menú y panel con barras de Música y Efectos.
- Menú **NovaStriker/Setup/Etapa 9 - Audio**: importación (efectos en memoria ADPCM, música y ambiente en streaming Vorbis, mono), `Settings/Audio/SO_SoundLibrary`, asignación a `PF_AppRoot` y ajustes en el menú. Sin la ficha asignada, el juego avisa con un error claro.
- Tests nuevos: 15. Total EditMode esperado: 244.

### Etapa 10 (2026-10-14) — implementada, pendiente de verificar en Unity
- **Decisiones del usuario**: banner **arriba** y siempre visible (abajo tapaba la zona de tiro y provocaba toques accidentales); App ID real `ca-app-pub-6654333646555327~5481850987` con bloques de **prueba** de Google; intersticial cada 4 victorias hasta la 16 y después cada 2 (4, 8, 12, 16, 18, 20...); "+1 balón" con recompensado **una vez por intento** del nivel (se reinicia al reintentar).
- **Contratos**: `IAdsService` (alto del banner + evento de cambio, `ShowInterstitialIfDue(totalVictories, onFinished)`, `ShowRewarded`), `FullscreenAdChangedEvent`, `IProgressService.TotalVictories` (cuenta toda victoria; `ProgressData` v2), `IGameCommands.CanContinueWithExtraBall`, `IAudioService.SetSuspended`.
- **Adapters** `IAdsProvider`: `FakeAdsProvider` (Editor, IMGUI: banner gris "BANNER (prueba)", intersticial y recompensado con botones), `AdMobAdsProvider` (solo con el símbolo `NOVA_ADMOB`; banner adaptable anclado arriba, carga y precarga, callbacks en el hilo principal) y `NullAdsProvider` (build sin plugin: aviso en consola, sin anuncios). `AdsProviderFactory` elige uno.
- **Servicio** `AdsService`: política `InterstitialPolicy` (configurable en `SO_AdsConfig`), banner al iniciar, precarga y recarga tras cada uso; si toca intersticial pero no está cargado, el juego sigue sin esperar. Durante un anuncio a pantalla completa el audio se suspende (`AudioListener.pause`).
- **Flujo**: `VictoryState` pide el intersticial con el total de victorias y vuelve al menú cuando se cierra. Derrota: "+1 BALÓN" activo si hay recompensado cargado, desactivado si no y oculto si ya se usó; solo con el anuncio visto entero se concede el balón (`LevelRulesTracker.TryGrantExtraBall`).
- **Franja del banner**: `BannerLayoutBinder` (composición de cada escena) baja la interfaz (`SafeAreaFitter.SetTopInset`, banner + margen de 1,5 % del alto) y recorta el viewport de la cámara (`SafeAreaMath.CameraViewport`) para no dibujar detrás del anuncio; una cámara de fondo limpia esa franja en negro.
- Menú **NovaStriker/Setup/Etapa 10 - Anuncios**: crea `Settings/Ads/SO_AdsConfig`, lo asigna a `PF_AppRoot`, conecta la cámara del menú y, si detecta el plugin Google Mobile Ads, activa `NOVA_ADMOB` para Android y copia el App ID a la configuración del plugin.
- Limitación conocida: en el Editor los anuncios simulados (IMGUI) no bloquean los toques a la interfaz que tienen debajo.
- Tests nuevos: 16. Total EditMode esperado: 260.

## Tutorial del nivel 1 (indicación de tiro)

- `Gameplay/Tutorial/TutorialDirector` (Observer): en el nivel con índice 0, mientras el estado es Aiming y hasta el primer `ShotFiredEvent` de cada intento, publica `TutorialHintEvent` (visible + punto de saque + pecho del primer objetivo en pie, vía `ILevelHintSource` implementado por `LevelSession`). Se oculta con `AimStartedEvent` y vuelve con `AimCancelledEvent` si no se disparó.
- `UI/Tutorial` (MVP): `TutorialHintPresenter` → `TutorialHintView` (dedo `UI_Hand`, anillo de toque, 6 puntos de rastro y cartel), animación pura en `DragHintAnimation`. No bloquea toques (CanvasGroup sin raycasts).
- Setup: `NovaStriker/Setup/Tutorial - Indicación de tiro` crea `Canvas_Gameplay/TutorialHint` y lo asigna a `GameplayCompositionRoot._tutorialHintView` (opcional: si falta, aviso en consola).


## Idiomas (español, inglés, portugués)

- Contracts: `Language`, `TextKey` (claves; `TextKey.All` para las pruebas), `ILocalization`. `IGameSettings` guarda el idioma (`SettingsData` v2, 0 = sin elegir).
- Infrastructure: `LocalizationService` (sin elección → idioma del móvil o inglés; `SetLanguage` guarda al momento y avisa con `LanguageChanged`) y `StringTable` (todas las traducciones). Pruebas: ninguna clave sin traducir y mismos `{0}` en los tres idiomas.
- UI: los presentadores piden sus textos a `ILocalization`; los textos fijos de escena llevan `LocalizedText` (clave) y los inicializa `LocalizedTextInstaller` desde cada raíz de composición. `LanguagePickerPresenter` muestra la pantalla de idioma solo la primera vez; `SettingsPresenter` cambia el idioma desde Ajustes (`LanguageButtons`).
- Setup: `NovaStriker/Setup/Idiomas` (pantalla de idioma, fila en Ajustes y `LocalizedText` en los textos fijos de menú y juego). Ejecutarlo después de `Tutorial - Indicación de tiro`.


## 15. Registro de cambios posteriores (2026-10-20 → 2026-11-12)

Cambios hechos después de la Etapa 10, del más reciente al más antiguo. Las pruebas EditMode pasaron de 260 a **328** (ejecución del 2026-11-12: 328/328 correctas, evidencia en `Docs/evidencias/`).

| Fecha | Cambio | Detalle |
|---|---|---|
| 2026-11-12 | Repositorio y evidencias | Git con `.gitignore` y `.gitattributes` de Unity, `README.md` (qué es, cómo ejecutarlo, cómo probarlo, estructura), `Tools/run-tests.bat` (pruebas por consola, resultados en `TestResults/`) y resultados guardados en `Docs/evidencias/`. Cierra el riesgo 13 (sin control de versiones). |
| 2026-10-28 | Nivel 13 "Atajo" | Barrera de vidrio irrompible a lo ancho, portal azul delante y naranja detrás; dos futbolistas solo alcanzables por el portal y uno por encima del vidrio. 5 balones, tiro recto. Generador `NovaStriker/Niveles/Generar nivel 13 (portales)`; herramientas comunes extraídas a `LevelPrefabEditor`. |
| 2026-10-28 | Portales | `PortalPair` + dos `Portal` (Gameplay/Obstacles/Portals): el balón sale por el centro del otro portal con la misma velocidad y dirección. Lógica pura `TeleportBudget` (límite anti-bucle) y `PortalArrivalGate`. Nueva capa 14 `Portal` (solo detecta Ball), `ObstacleType.Portal`, `SfxId.Portal`, evento `BallTeleportedEvent`, `BallView.TeleportTo`. Menú `NovaStriker/Obstáculos/Crear portales`. 6 pruebas nuevas. |
| 2026-10-23 | Analíticas con Firebase | Observer `AnalyticsEventRouter` + Adapter `IAnalyticsBackend` (Firebase en Android bajo `NOVA_FIREBASE`, consola en el Editor, vacío sin SDK). Eventos: `language_selected`, `tutorial_begin/complete`, `level_start/end/retry/quit`, `leave_game` (dónde estaba el jugador al salir), `rewarded_offer/complete`, `interstitial_shown`; propiedad `highest_level`; `user_id` = id de PlayFab. `AdsService` publica eventos de anuncios. Menú `NovaStriker/Setup/Firebase - Analíticas`. 15 pruebas nuevas. |
| 2026-10-23 | Build de Android con Firebase | `GradleRepoPathFix` (`IPostGenerateGradleAndroidProject`): cambia los espacios de las rutas `file:///` de `settings.gradle` por `%20` (la carpeta del proyecto tiene un espacio). Se elimina la copia duplicada de `Assets/ExternalDependencyManager` y se usa el paquete `com.unity.external-dependency-manager` 2.1.0. |
| 2026-10-22 | Inicio de sesión con PlayFab | Anónimo y automático al abrir la app (`LoginWithCustomID` con el id del dispositivo), sin bloquear el juego: si falla, se juega sin cuenta y se reintenta a los 5, 20 y 60 s y al volver a la app. `IPlayerAccount` (Contracts), `PlayerAccountService`, `PlayerIdResolver`, `PlayFabConfig`; adaptador real en el asmdef `Infrastructure.PlayFab` (`NOVA_PLAYFAB`). Menú `NovaStriker/Setup/PlayFab - Inicio de sesión`. 10 pruebas nuevas. |
| 2026-10-22 | Recompensa de +2 balones | El anuncio recompensado tras perder da 2 balones (`LevelRulesTracker.RewardedExtraBalls = 2`), una vez por intento. Sustituye al "+1 balón" de D5. |
| 2026-10-22 | Idiomas y tutorial | Ver los apartados "Tutorial del nivel 1" e "Idiomas" de este documento. La pantalla de idioma aparece antes que el nivel 1. |
| 2026-10-21 | Caja de vidrio y niveles 10–12 | `PF_GlassCrate` (ver D9) y niveles 10 "Lluvia de cajas", 11 "Relevo" y 12 "Caos", generados con `NovaStriker/Niveles/Generar niveles 10, 11 y 12`. |
| 2026-10-21 | Herramientas de niveles | `NovaStriker/Niveles/Integrar niveles nuevos` (crea `SO_Level_NN` y los añade al final del catálogo sin reordenar) y `NovaStriker/Desarrollo/Probar nivel...`. Niveles 7–11 integrados. |
| 2026-10-20 | Etapa 11 y estabilidad | Configuración de publicación Android (`ProjectSetupStage11`, ver D2), conversión del estadio a URP, `PhysicsSettler` (duerme cajas y futbolistas 0,4 s tras construir el nivel) y `PM_Footballer` (fricción 1) para que las pirámides no resbalen. |
