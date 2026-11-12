using System;
using System.Text;
using NovaStriker.EditorTools.Setup;
using NovaStriker.Gameplay.Characters;
using NovaStriker.Gameplay.Level;
using NovaStriker.Gameplay.Obstacles;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NovaStriker.EditorTools.Levels
{
    /// <summary>
    /// Genera los niveles 10, 11 y 12 alrededor de la caja de vidrio móvil.
    /// Idea: algunos futbolistas están protegidos detrás de un vidrio irrompible y solo caen si el jugador rompe una
    /// caja de vidrio justo cuando pasa POR ENCIMA de ellos: las cajas de dentro les caen encima.
    /// Medidas clave: la caja de vidrio vuela con su base a 2,8 m (el vidrio protector mide 2,5 m y la cabeza del
    /// futbolista ~1,8 m), así que las cajas caen ~1 m y llegan a ~4,5 m/s, por encima de los 2,5 m/s que derriban.
    /// Reutiliza el escenario de cada prefab (suelo, cámara, balón): solo vacía y rellena "Obstacles" y
    /// "FootballerSpawns". Los prefabs existentes se editan en su sitio (conservan GUID y su SO_Level).
    /// </summary>
    internal static class GlassCrateLevelsBuilder
    {
        private const string LevelsFolder = ProjectSetupStage0.ProjectRoot + "/Prefabs/Levels/";
        private const string SettingsFolder = ProjectSetupStage0.ProjectRoot + "/Settings/Levels/";
        private const string Level10 = LevelsFolder + "PF_Level_10.prefab";
        private const string Level11 = LevelsFolder + "PF_Level_11.prefab";
        private const string Level12 = LevelsFolder + "PF_Level_12.prefab";
        private const string ObstaclesName = "Obstacles";
        private const string SpawnsName = "FootballerSpawns";

        private const float OnTop = 0.02f;
        /// <summary>Altura de la base de las cajas de vidrio que vuelan sobre los futbolistas.</summary>
        private const float FlyingCrateHeight = 2.8f;
        /// <summary>El vidrio protector (PF_Glass, 2,5 m de alto) va 1,5 m por delante del futbolista.</summary>
        private const float ShieldDistance = 1.5f;

        [MenuItem("NovaStriker/Niveles/Generar niveles 10, 11 y 12 (cajas de vidrio)")]
        private static void Run()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Level10) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(Level11) == null)
            {
                Debug.LogError($"[NovaStriker Niveles] Faltan {Level10} o {Level11}: se usan como base del escenario.");
                return;
            }

            bool has12 = AssetDatabase.LoadAssetAtPath<GameObject>(Level12) != null;
            string message = "Se reemplazarán los obstáculos y futbolistas de PF_Level_10, PF_Level_11" +
                             (has12 ? " y PF_Level_12." : " y se creará PF_Level_12.") +
                             "\nEl suelo, la cámara y el punto del balón se mantienen.";
            if (!EditorUtility.DisplayDialog("Generar niveles 10, 11 y 12", message, "Generar", "Cancelar"))
            {
                return;
            }

            var log = new StringBuilder("[NovaStriker Niveles] Niveles 10, 11 y 12 con cajas de vidrio\n");
            if (!GlassCrateBuilder.EnsurePrefab(log))
            {
                Debug.LogError(log.ToString());
                return;
            }

            if (!has12 && !AssetDatabase.CopyAsset(Level11, Level12))
            {
                Debug.LogError($"{log}- ERROR: no se pudo copiar {Level11} a {Level12}.");
                return;
            }

            bool ok = Rebuild(Level10, BuildLluvia, log)
                      && Rebuild(Level11, BuildRelevo, log)
                      && Rebuild(Level12, BuildCaos, log);
            if (!ok)
            {
                Debug.LogError(log.ToString());
                return;
            }

            int problems = LevelIntegrationTool.Integrate(log);
            SetDefinition("SO_Level_10", "Lluvia de cajas", 6, log);
            SetDefinition("SO_Level_11", "Relevo", 7, log);
            SetDefinition("SO_Level_12", "Caos", 9, log);
            AssetDatabase.SaveAssets();

            if (problems != 0)
            {
                Debug.LogWarning(log.ToString());
            }
            else
            {
                Debug.Log(log.ToString());
            }
        }

        // ---------- Diseños ----------
        // Cada caja de vidrio necesita 2 golpes: el primero la agrieta, el segundo la rompe (ese es el que hay que cronometrar).

        /// <summary>
        /// Nivel 10 "Lluvia de cajas" (introducción): una caja lenta cruza sobre un futbolista protegido;
        /// otros dos futbolistas se derriban con tiro directo.
        /// </summary>
        private static void BuildLluvia(LevelEditor level)
        {
            level.ProtectedFootballer(new Vector3(0f, 0f, 16f));
            level.FlyingCrate(new Vector3(-3f, FlyingCrateHeight, 16f), new Vector3(6f, 0f, 0f), speed: 1.2f, wait: 0.8f);

            level.Spawn(new Vector3(-4f, OnTop, 20f));

            level.Obstacle(ProjectSetupStage5.StaticBoxPath, new Vector3(4f, 0.5f, 20f));
            level.Spawn(new Vector3(4f, 1f + OnTop, 20f));
        }

        /// <summary>
        /// Nivel 11 "Relevo": dos cajas en carriles distintos, cada una sobre su futbolista protegido, que van en
        /// sentidos contrarios y desfasadas; un tercer futbolista en una torre de cajas.
        /// </summary>
        private static void BuildRelevo(LevelEditor level)
        {
            level.ProtectedFootballer(new Vector3(-2f, 0f, 15f));
            level.FlyingCrate(new Vector3(-4f, FlyingCrateHeight, 15f), new Vector3(4f, 0f, 0f), speed: 1.5f, wait: 0.5f);

            level.ProtectedFootballer(new Vector3(2f, 0f, 20f));
            level.FlyingCrate(new Vector3(4f, FlyingCrateHeight, 20f), new Vector3(-4f, 0f, 0f), speed: 1.8f, wait: 0.5f,
                startOffset: 0.5f);

            level.Obstacle(ProjectSetupStage5.BoxPath, new Vector3(-5f, 0.5f, 22f));
            level.Obstacle(ProjectSetupStage5.BoxPath, new Vector3(-5f, 1.5f, 22f));
            level.Spawn(new Vector3(-5f, 2f + OnTop, 22f));
        }

        /// <summary>
        /// Nivel 12 "Caos": tres futbolistas protegidos. Dos cajas rápidas cruzan de lado a lado en carriles
        /// distintos y una tercera avanza y retrocede en profundidad (más difícil de cronometrar).
        /// Un cuarto futbolista viaja en una plataforma móvil.
        /// </summary>
        private static void BuildCaos(LevelEditor level)
        {
            level.ProtectedFootballer(new Vector3(-2.5f, 0f, 14f));
            level.FlyingCrate(new Vector3(-5f, FlyingCrateHeight, 14f), new Vector3(5f, 0f, 0f), speed: 2.2f, wait: 0.3f);

            level.ProtectedFootballer(new Vector3(2.5f, 0f, 18f));
            level.FlyingCrate(new Vector3(5f, FlyingCrateHeight, 18f), new Vector3(-5f, 0f, 0f), speed: 2.2f, wait: 0.3f,
                startOffset: 0.5f);

            // La caja se mueve en profundidad (z 21 → 26) sobre un futbolista en z 23,5. Su vidrio protector
            // queda en z 22 y la caja nunca baja de 2,8 m, así que pasa por encima de él.
            level.ProtectedFootballer(new Vector3(0f, 0f, 23.5f));
            level.FlyingCrate(new Vector3(0f, FlyingCrateHeight, 21f), new Vector3(0f, 0f, 5f), speed: 1.5f, wait: 0.4f);

            level.Obstacle(ProjectSetupStage5.MovingPlatformPath, new Vector3(-5f, 0.6f, 24f));
            level.Spawn(new Vector3(-5f, 0.75f + OnTop, 24f));
        }

        // ---------- Edición del prefab ----------

        private static bool Rebuild(string path, Action<LevelEditor> design, StringBuilder log)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform obstacles = contents.transform.Find(ObstaclesName);
                Transform spawns = contents.transform.Find(SpawnsName);
                if (contents.GetComponent<LevelLayout>() == null || obstacles == null || spawns == null)
                {
                    log.AppendLine($"- ERROR: {path} no tiene LevelLayout, '{ObstaclesName}' o '{SpawnsName}'.");
                    return false;
                }

                // El futbolista y su orientación se toman del nivel actual antes de vaciarlo.
                FootballerSpawnPoint template = spawns.GetComponentInChildren<FootballerSpawnPoint>(true);
                if (template == null || template.Character == null)
                {
                    log.AppendLine($"- ERROR: {path} no tiene ningún futbolista del que copiar el personaje.");
                    return false;
                }

                var editor = new LevelEditor(obstacles, spawns, template.Character, template.transform.localRotation);
                ClearChildren(obstacles);
                ClearChildren(spawns);
                design(editor);

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                log.AppendLine($"- {path}: {editor.ObstacleCount} obstáculos, {editor.SpawnCount} futbolistas " +
                               $"({editor.ProtectedCount} solo alcanzables con una caja de vidrio).");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static void SetDefinition(string assetName, string displayName, int balls, StringBuilder log)
        {
            string path = SettingsFolder + assetName + ".asset";
            var definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (definition == null)
            {
                log.AppendLine($"- AVISO: no encontré {path}; ajusta nombre y balones a mano.");
                return;
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_ballCount").intValue = balls;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            log.AppendLine($"- {path}: \"{displayName}\", {balls} balones.");
        }

        /// <summary>Coloca obstáculos y futbolistas dentro de un prefab de nivel abierto.</summary>
        private sealed class LevelEditor
        {
            private readonly Transform _obstacles;
            private readonly Transform _spawns;
            private readonly CharacterDefinition _character;
            private readonly Quaternion _spawnRotation;

            public LevelEditor(Transform obstacles, Transform spawns, CharacterDefinition character, Quaternion spawnRotation)
            {
                _obstacles = obstacles;
                _spawns = spawns;
                _character = character;
                _spawnRotation = spawnRotation;
            }

            public int ObstacleCount => _obstacles.childCount;
            public int SpawnCount => _spawns.childCount;
            public int ProtectedCount { get; private set; }

            public GameObject Obstacle(string prefabPath, Vector3 position, float yaw = 0f)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"No existe {prefabPath}.");
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _obstacles);
                instance.transform.localPosition = position;
                instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                return instance;
            }

            public void Spawn(Vector3 position)
            {
                var spawn = new GameObject($"Spawn_{_spawns.childCount + 1}");
                spawn.transform.SetParent(_spawns, false);
                spawn.transform.localPosition = position;
                spawn.transform.localRotation = _spawnRotation;
                var point = spawn.AddComponent<FootballerSpawnPoint>();
                ProjectSetupStage2.AssignReference(point, "_character", _character);
            }

            /// <summary>
            /// Futbolista en el suelo con un vidrio irrompible delante (más cerca de la cámara): el tiro directo
            /// rebota en el vidrio y solo cae si le llueven las cajas de una caja de vidrio.
            /// </summary>
            public void ProtectedFootballer(Vector3 groundPosition)
            {
                Spawn(groundPosition + new Vector3(0f, OnTop, 0f));
                Obstacle(ProjectSetupStage5.GlassPath, new Vector3(groundPosition.x, 1.25f, groundPosition.z - ShieldDistance));
                ProtectedCount++;
            }

            /// <summary>Caja de vidrio con su base en <paramref name="basePosition"/> que recorre <paramref name="path"/> y vuelve.</summary>
            public void FlyingCrate(Vector3 basePosition, Vector3 path, float speed, float wait, float startOffset = 0f)
            {
                GameObject instance = Obstacle(GlassCrateBuilder.PrefabPath, basePosition);
                Transform crate = instance.transform.Find(GlassCrateNames.Crate);
                PathMover mover = crate != null ? crate.GetComponent<PathMover>() : null;
                if (mover == null)
                {
                    throw new InvalidOperationException("PF_GlassCrate no tiene PathMover en su objeto Crate.");
                }

                var serialized = new SerializedObject(mover);
                SerializedProperty waypoints = serialized.FindProperty("_waypoints");
                waypoints.arraySize = 2;
                waypoints.GetArrayElementAtIndex(0).vector3Value = Vector3.zero;
                waypoints.GetArrayElementAtIndex(1).vector3Value = path;
                serialized.FindProperty("_speed").floatValue = speed;
                serialized.FindProperty("_waitAtWaypoint").floatValue = wait;
                serialized.FindProperty("_startOffset").floatValue = startOffset;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
