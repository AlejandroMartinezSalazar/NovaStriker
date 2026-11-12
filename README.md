# NovaStriker

Juego móvil de fútbol con física para Android (vertical). El jugador arrastra el dedo hacia un futbolista y suelta para lanzar el balón; debe derribar a todos los futbolistas del nivel (muñecos con física *ragdoll*) antes de quedarse sin balones. Los niveles combinan cajas, muros, vidrio rompible, plataformas móviles, cajas de vidrio voladoras y portales.

- **Motor:** Unity **6000.6.5f1** con Universal Render Pipeline (URP).
- **Plataforma:** Android (también se juega en el Editor de Unity).
- **Idiomas:** español, inglés y portugués.
- **Servicios externos (opcionales):** anuncios con Google AdMob, inicio de sesión anónimo con PlayFab y analíticas con Firebase. **Ninguno es necesario para jugar ni para correr las pruebas**: en el Editor los anuncios son simulados y las analíticas solo se escriben en la consola. Si no hay internet o falla un servicio, el juego sigue funcionando.

---

## 1. Requisitos

| Necesitas | Versión | Para qué |
|---|---|---|
| Unity Hub + Unity | **6000.6.5f1** (exacta) | Abrir, jugar y probar el proyecto |
| Módulo *Android Build Support* (con OpenJDK y SDK/NDK) | el de esa versión | Solo para generar el APK/AAB |
| Git | cualquiera reciente | Clonar el repositorio |
| Windows 10/11 | — | El script de pruebas es `.bat` (en macOS/Linux usa el comando del apartado 4.2) |

Los paquetes de Unity se descargan solos al abrir el proyecto (están en `Packages/manifest.json`). Los SDK de terceros (AdMob, PlayFab, Firebase) ya están dentro de `Assets/`.

## 2. Abrir y jugar (local)

1. Clona el repositorio:
   ```
   git clone <url-del-repositorio>
   ```
2. En Unity Hub: **Add > Add project from disk** y elige la carpeta clonada. Ábrelo con **6000.6.5f1**. La primera vez tarda varios minutos (Unity crea la carpeta `Library/`).
3. Abre la escena **`Assets/_Project/Scenes/SC_Boot.unity`** y pulsa **Play**.
   - La primera vez aparece la pantalla de idioma; después, el menú principal y el nivel 1 (tutorial).
   - En la vista *Game* elige una resolución vertical (por ejemplo 1080×1920).
   - Con el ratón: mantén pulsado sobre la pantalla, arrastra hacia un futbolista y suelta.
4. Para saltar directamente a un nivel: menú **NovaStriker > Desarrollo > Probar nivel...**

> Si al abrir el proyecto la consola muestra avisos del *External Dependency Manager* o de Firebase sobre Android, no afectan a jugar en el Editor ni a las pruebas.

## 3. Generar el APK/AAB (opcional)

1. **File > Build Profiles > Android > Switch Platform**.
2. La firma de publicación usa una *keystore* que **no está en el repositorio** (es privada). Para un APK de prueba sin ella: **Edit > Project Settings > Player > Android > Publishing Settings** y desmarca **Custom Keystore**; Unity firmará con la clave de depuración. Después, **Build**.
3. Los identificadores de anuncios, PlayFab y Firebase están en `Assets/_Project/Settings/Ads`, `Assets/_Project/Settings/Online` y `Assets/FirebaseFile`.

## 4. Correr las pruebas

Todas las pruebas son **EditMode** (se ejecutan en el Editor sin pulsar Play) y están en `Assets/_Project/Scripts/Tests/EditMode`.

### 4.1 Desde el Editor

1. **Window > General > Test Runner**.
2. Pestaña **EditMode** > **Run All**.
3. Todas deben quedar en verde.

### 4.2 Desde la consola (sin abrir el Editor)

Cierra Unity y, desde la carpeta del proyecto:

```
Tools\run-tests.bat
```

El script lanza Unity en modo *batch*, ejecuta todas las pruebas y muestra el resumen (total, correctas, fallidas, duración). Deja el detalle en:

- `TestResults/editmode-results.xml`: resultados en formato NUnit, uno por prueba.
- `TestResults/editmode.log`: registro completo de Unity.

Si Unity no está en la ruta por defecto de Unity Hub (`C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe`), indícala antes:

```
set UNITY_PATH=D:\ruta\a\Unity.exe
Tools\run-tests.bat
```

Comando equivalente en cualquier sistema:

```
<Unity> -batchmode -nographics -projectPath <carpeta-del-proyecto> -runTests -testPlatform EditMode -testResults TestResults/editmode-results.xml -logFile TestResults/editmode.log
```

Código de salida: `0` = todo en verde, `2` = alguna prueba falló, otro valor = error (ver el `.log`).

### 4.3 Qué cubren

| Carpeta de pruebas | Qué verifica |
|---|---|
| `Core` | Bus de eventos, contenedor de dependencias, máquina de estados, *pool* de objetos |
| `Gameplay` | Cálculo de tiros, estrategias de tiro, reglas del nivel (victoria/derrota, balones), derribo, obstáculos, portales, caja de vidrio, tutorial, flujo completo de una partida |
| `Infrastructure` | Guardado (incluidos archivo dañado, fallo de escritura y recuperación tras un cierre a medias), idiomas, inicio de sesión sin conexión, analíticas |
| `Ads` | Anuncios: cuándo toca un intersticial, recompensas, y que un anuncio sin cargar no bloquee el juego |
| `Audio` | Qué sonido corresponde a cada evento del juego |
| `UI` | Presentadores (MVP) de HUD, menús, ajustes, idioma y resultados |
| `Bootstrap` | Arranque de la app, pantalla inicial según el idioma y paso entre menú y juego |

## 5. Estructura del repositorio

```
NovaStriker/
├── Assets/
│   ├── _Project/                  ← TODO el código y contenido propio
│   │   ├── Scenes/                SC_Boot, SC_MainMenu, SC_Game
│   │   ├── Scripts/               código, separado en ensamblados (ver 6)
│   │   │   ├── Core/              utilidades genéricas (eventos, estados, pools, DI)
│   │   │   ├── Contracts/         interfaces, eventos y datos compartidos
│   │   │   ├── Gameplay/          reglas del juego, balón, tiros, obstáculos, niveles
│   │   │   ├── Infrastructure/    guardado, audio, anuncios, idiomas, cuentas, analíticas
│   │   │   ├── UI/                vistas y presentadores (MVP)
│   │   │   ├── Bootstrap/         raíces de composición: aquí se conecta todo
│   │   │   ├── Editor/            menús NovaStriker/... para generar prefabs, niveles y ajustes (solo Editor)
│   │   │   └── Tests/             pruebas automáticas
│   │   ├── Prefabs/               niveles (PF_Level_NN), obstáculos, personajes, UI
│   │   ├── Settings/              ScriptableObjects de configuración (niveles, tiros, audio, anuncios…)
│   │   ├── Art/ Audio/ PhysicsMaterials/ Resources/
│   ├── Firebase/ GoogleMobileAds/ PlayFabSDK/   SDK de terceros
│   ├── Plugins/Android/           plantillas de Gradle y librerías Android
│   └── SoccerStadiumPack/ …       arte de terceros (Asset Store)
├── Docs/                          documentación (ver 7)
├── Packages/                      manifiesto de paquetes de Unity
├── ProjectSettings/               configuración del proyecto Unity
└── Tools/run-tests.bat            ejecuta las pruebas por consola
```

Lo que no se sube (ver `.gitignore`): `Library/`, `Temp/`, `Logs/`, `UserSettings/`, compilaciones, archivos de Visual Studio generados por Unity y la *keystore*.

## 6. Arquitectura en breve

El código está dividido en **ensamblados** de Unity (*asmdef*). Cada uno solo puede usar a los que aparecen debajo de él; Unity no compila una dependencia que no esté declarada, así que estas reglas se cumplen por construcción:

```
                Bootstrap  (crea y conecta todo)
           ┌────────┬───────┴────────┬──────────┐
           ▼        ▼                ▼          ▼
       Gameplay    UI         Infrastructure  Infrastructure.PlayFab*
           │        │                │          │
           └────────┴──────┬─────────┘          │
                           ▼                    │
                       Contracts  ◄─────────────┘ (vía Infrastructure)
                           │
                           ▼
                         Core   (sin dependencias propias)
```

Además, `EditorTools` (solo en el Editor) y los ensamblados de pruebas pueden usar a todos, pero ninguno de juego depende de ellos.

\* `Infrastructure.PlayFab` solo se compila cuando el SDK de PlayFab está instalado (símbolo `NOVA_PLAYFAB`).

- **Gameplay, UI e Infrastructure no se conocen entre sí**: se comunican con eventos (`Contracts/Events`) e interfaces (`Contracts/Services`).
- Los SDK externos (AdMob, PlayFab, Firebase) quedan aislados detrás de **adaptadores** en Infrastructure; si faltan, se usa una versión vacía o simulada.

Patrones aplicados: **State** (máquinas de estado de la app y de la partida), **Factory** (personajes, estrategias, servicios), **Object Pool** (balones, fragmentos, efectos), **Observer** (bus de eventos), **Strategy** (tipos de tiro), **Adapter** (anuncios, cuentas, analíticas) e **inyección de dependencias manual** (raíces de composición en Bootstrap). El detalle de dónde vive cada uno está en `Docs/ARCHITECTURE.md`, apartado 6.

## 7. Documentación

| Documento | Contenido |
|---|---|
| [`Docs/ARCHITECTURE.md`](Docs/ARCHITECTURE.md) | Arquitectura completa: decisiones, ensamblados, carpetas, convenciones, patrones, eventos, máquinas de estado, riesgos y **registro de cambios por etapa** (apartado 14) |
| Manual de buenas prácticas | *(pendiente de enlazar)* |
| Informe de pruebas | *(pendiente de enlazar)* |

## 8. Datos

El juego no pide datos personales. Guarda el progreso solo en el dispositivo (`Application.persistentDataPath`). El inicio de sesión de PlayFab es anónimo (un identificador del dispositivo, sin nombre, correo ni contraseña).
