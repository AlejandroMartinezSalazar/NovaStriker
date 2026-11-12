using System;
using System.Text;
using NovaStriker.Bootstrap.Scenes;
using NovaStriker.Contracts.Data;
using NovaStriker.Gameplay.Aim;
using NovaStriker.Gameplay.Ball;
using NovaStriker.Gameplay.Characters;
using NovaStriker.Gameplay.Debris;
using NovaStriker.Gameplay.Level;
using NovaStriker.Gameplay.Throwing;
using NovaStriker.EditorTools.Setup.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Etapa 6: prefabs de los 3 niveles de ejemplo, sus definiciones, el catálogo y SC_Game
    /// (retira el sandbox y la UI provisional; crea contenedores y preview de apuntado).
    /// Desde la Etapa 7 la interfaz de SC_Game la monta GameplayUiBuilder (la misma que usa la Etapa 7).
    /// Idempotente: prefabs, definiciones y catálogo solo se crean si faltan (se conservan tus cambios);
    /// en una definición existente solo se rellenan las referencias vacías.
    /// </summary>
    internal static class ProjectSetupStage6
    {
        private const string Root = ProjectSetupStage0.ProjectRoot;
        private const string LevelPrefabsFolder = Root + "/Prefabs/Levels/";
        private const string LevelSettingsFolder = Root + "/Settings/Levels/";
        internal const string CatalogPath = LevelSettingsFolder + "SO_LevelCatalog.asset";

        private const string GameRootName = "[Game]";
        private const string OldSandboxName = "[Sandbox]";
        private const string OldCanvasName = "Canvas_GameplayPlaceholder";

        private static readonly Vector3 CameraPosition = new Vector3(0f, 2.4f, -4.5f);
        private static readonly Vector3 CameraLookAt = new Vector3(0f, 0.8f, 12f);
        private static readonly Quaternion FacingCamera = Quaternion.Euler(0f, 180f, 0f);

        private sealed class LevelSpec
        {
            public string Id;
            public string DisplayName;
            public int Balls;
            public string StrategyPath;
            public Action<LayoutBuilder> Build;

            public string PrefabPath => LevelPrefabsFolder + $"PF_Level_{Id}.prefab";
            public string DefinitionPath => LevelSettingsFolder + $"SO_Level_{Id}.asset";
        }

        private static readonly LevelSpec[] Levels =
        {
            new LevelSpec
            {
                Id = "01", DisplayName = "Cajas", Balls = 5, StrategyPath = ProjectSetupStage3.StraightPath,
                Build = b =>
                {
                    b.Obstacle(ProjectSetupStage5.BoxPath, new Vector3(-2f, 0.5f, 12f));
                    b.Obstacle(ProjectSetupStage5.BoxPath, new Vector3(2f, 0.5f, 12f));
                    b.Obstacle(ProjectSetupStage5.BoxPath, new Vector3(0f, 0.5f, 15f));
                    b.Obstacle(ProjectSetupStage5.BoxPath, new Vector3(0f, 1.5f, 15f));
                    b.Spawn(new Vector3(-2f, 1.02f, 12f));
                    b.Spawn(new Vector3(2f, 1.02f, 12f));
                    b.Spawn(new Vector3(0f, 2.02f, 15f));
                    b.Spawn(new Vector3(3.5f, 0.02f, 17f));
                },
            },
            new LevelSpec
            {
                Id = "02", DisplayName = "Vidrios y muros", Balls = 7, StrategyPath = ProjectSetupStage3.LobPath,
                Build = b =>
                {
                    b.Obstacle(ProjectSetupStage5.StaticBoxPath, new Vector3(-3.5f, 2.5f, 17f));
                    b.Obstacle(ProjectSetupStage5.WallPath, new Vector3(5f, 1.5f, 16f), -35f);
                    b.Obstacle(ProjectSetupStage5.BreakableGlassPath, new Vector3(1.5f, 1.25f, 19f));
                    b.Obstacle(ProjectSetupStage5.GlassPath, new Vector3(-1.5f, 1.25f, 24f));
                    b.Spawn(new Vector3(-3.5f, 3.02f, 17f));
                    b.Spawn(new Vector3(1.5f, 0.02f, 21f));
                    b.Spawn(new Vector3(3.5f, 0.02f, 13.5f));
                },
            },
            new LevelSpec
            {
                Id = "03", DisplayName = "En movimiento", Balls = 5, StrategyPath = ProjectSetupStage3.CurvePath,
                Build = b =>
                {
                    b.Obstacle(ProjectSetupStage5.MovingWallPath, new Vector3(-1.5f, 1.25f, 20f));
                    b.Obstacle(ProjectSetupStage5.MovingPlatformPath, new Vector3(3f, 0.6f, 18f));
                    b.Spawn(new Vector3(0.5f, 0.02f, 22.5f));
                    b.Spawn(new Vector3(3f, 0.77f, 18f));
                    b.Spawn(new Vector3(-3.5f, 0.02f, 16f));
                },
            },
        };

        [MenuItem("NovaStriker/Setup/Etapa 6 - Niveles, reglas y HUD")]
        private static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[NovaStriker Setup] Cancelado: hay escenas sin guardar.");
                return;
            }

            var log = new StringBuilder("[NovaStriker Setup] Etapa 6\n");
            bool ok = CheckPrerequisites(log) && UiKit.PrepareAssets(log);
            if (ok)
            {
                CreateLevelPrefabs(log);
                AssetDatabase.SaveAssets();
                ok = CreateDefinitionsAndCatalog(log);
                AssetDatabase.SaveAssets();
            }

            ok = ok && SetupGameScene(log);

            if (ok)
            {
                Debug.Log(log.ToString());
            }
            else
            {
                Debug.LogError(log.ToString());
            }
        }

        // ---------- Requisitos ----------

        private static bool CheckPrerequisites(StringBuilder log)
        {
            string[] required =
            {
                ProjectSetupStage3.BallPrefabPath, ProjectSetupStage3.BallConfigPath, ProjectSetupStage3.AimConfigPath,
                ProjectSetupStage3.StraightPath, ProjectSetupStage3.LobPath, ProjectSetupStage3.CurvePath,
                ProjectSetupStage4.DefinitionPath, ProjectSetupStage5.ShardPrefabPath,
                ProjectSetupStage5.BoxPath, ProjectSetupStage5.StaticBoxPath, ProjectSetupStage5.WallPath,
                ProjectSetupStage5.GlassPath, ProjectSetupStage5.BreakableGlassPath,
                ProjectSetupStage5.MovingWallPath, ProjectSetupStage5.MovingPlatformPath,
            };

            bool ok = true;
            foreach (string path in required)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(path) == null)
                {
                    log.AppendLine($"- ERROR: falta {path}. Ejecuta antes las Etapas 3, 4 y 5.");
                    ok = false;
                }
            }

            return ok;
        }

        // ---------- Prefabs de nivel ----------

        private static void CreateLevelPrefabs(StringBuilder log)
        {
            var character = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(ProjectSetupStage4.DefinitionPath);
            Material groundMaterial = ProjectSetupStage3.LoadMaterial(ProjectSetupStage3.GroundMaterialName);

            foreach (LevelSpec spec in Levels)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath) != null)
                {
                    log.AppendLine($"- {spec.PrefabPath} ya existía, no se modifica.");
                    continue;
                }

                var builder = new LayoutBuilder($"PF_Level_{spec.Id}", groundMaterial, character);
                try
                {
                    spec.Build(builder);
                    PrefabUtility.SaveAsPrefabAsset(builder.Root, spec.PrefabPath);
                    log.AppendLine($"- Prefab de nivel: {spec.PrefabPath} ({builder.SpawnCount} futbolistas).");
                }
                finally
                {
                    Object.DestroyImmediate(builder.Root);
                }
            }
        }

        /// <summary>Monta la raíz de un nivel: escenario común + obstáculos y puntos de aparición.</summary>
        private sealed class LayoutBuilder
        {
            private readonly CharacterDefinition _character;
            private readonly Transform _obstacles;
            private readonly Transform _spawns;

            public LayoutBuilder(string name, Material groundMaterial, CharacterDefinition character)
            {
                _character = character;
                Root = new GameObject(name);
                var layout = Root.AddComponent<LevelLayout>();

                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.layer = GameLayers.Environment;
                ground.transform.SetParent(Root.transform, false);
                ground.transform.localPosition = new Vector3(0f, 0f, 20f);
                ground.transform.localScale = new Vector3(2f, 1f, 5f); // 20 m × 50 m.
                ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;

                var backstop = new GameObject("AimBackstop", typeof(BoxCollider));
                backstop.layer = GameLayers.AimBackstop;
                backstop.transform.SetParent(Root.transform, false);
                backstop.transform.localPosition = new Vector3(0f, 15f, 45f);
                backstop.GetComponent<BoxCollider>().size = new Vector3(80f, 40f, 1f);

                var killZone = new GameObject("KillZone", typeof(BoxCollider), typeof(KillZone));
                killZone.layer = GameLayers.KillZone;
                killZone.transform.SetParent(Root.transform, false);
                killZone.transform.localPosition = new Vector3(0f, -6f, 20f);
                var killCollider = killZone.GetComponent<BoxCollider>();
                killCollider.isTrigger = true;
                killCollider.size = new Vector3(200f, 2f, 200f);

                Transform ballSpawn = CreateChild(Root.transform, "BallSpawnPoint");
                ballSpawn.localPosition = new Vector3(0f, ProjectSetupStage3.BallDiameter * 0.5f + 0.01f, 0f);

                Transform cameraPoint = CreateChild(Root.transform, "CameraPoint");
                cameraPoint.localPosition = CameraPosition;
                cameraPoint.localRotation = Quaternion.LookRotation(CameraLookAt - CameraPosition);

                _obstacles = CreateChild(Root.transform, "Obstacles");
                _spawns = CreateChild(Root.transform, "FootballerSpawns");

                ProjectSetupStage2.AssignReference(layout, "_ballSpawnPoint", ballSpawn);
                ProjectSetupStage2.AssignReference(layout, "_cameraPoint", cameraPoint);
            }

            public GameObject Root { get; }
            public int SpawnCount => _spawns.childCount;

            public void Obstacle(string prefabPath, Vector3 position, float yaw = 0f)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _obstacles);
                instance.transform.localPosition = position;
                instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }

            public void Spawn(Vector3 position)
            {
                Transform spawn = CreateChild(_spawns, $"Spawn_{_spawns.childCount + 1}");
                spawn.localPosition = position;
                spawn.localRotation = FacingCamera;
                var point = spawn.gameObject.AddComponent<FootballerSpawnPoint>();
                ProjectSetupStage2.AssignReference(point, "_character", _character);
            }
        }

        // ---------- Definiciones y catálogo ----------

        private static bool CreateDefinitionsAndCatalog(StringBuilder log)
        {
            var definitions = new Object[Levels.Length];
            for (int i = 0; i < Levels.Length; i++)
            {
                LevelSpec spec = Levels[i];
                bool existed = AssetDatabase.LoadAssetAtPath<LevelDefinition>(spec.DefinitionPath) != null;
                var definition = ProjectSetupStage3.GetOrCreateAsset<LevelDefinition>(spec.DefinitionPath, log);
                var serialized = new SerializedObject(definition);

                if (!existed)
                {
                    serialized.FindProperty("_displayName").stringValue = spec.DisplayName;
                    serialized.FindProperty("_ballCount").intValue = spec.Balls;
                }

                FillIfEmpty(serialized, "_throwStrategy", AssetDatabase.LoadAssetAtPath<ThrowStrategyDefinition>(spec.StrategyPath));
                FillIfEmpty(serialized, "_layout", AssetDatabase.LoadAssetAtPath<LevelLayout>(spec.PrefabPath));
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);

                if (!ProjectSetupStage3.VerifyNoMissingReferences(definition, log))
                {
                    return false;
                }

                definitions[i] = definition;
            }

            var catalog = ProjectSetupStage3.GetOrCreateAsset<LevelCatalog>(CatalogPath, log);
            var catalogSerialized = new SerializedObject(catalog);
            if (catalogSerialized.FindProperty("_levels").arraySize == 0)
            {
                ProjectSetupStage3.AssignArray(catalog, "_levels", definitions);
                log.AppendLine($"- Catálogo: {Levels.Length} niveles en orden.");
            }
            else
            {
                log.AppendLine("- Catálogo ya tenía niveles, no se modifica.");
            }

            EditorUtility.SetDirty(catalog);
            return ProjectSetupStage3.VerifyNoMissingReferences(catalog, log);
        }

        private static void FillIfEmpty(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property.objectReferenceValue == null)
            {
                property.objectReferenceValue = value;
            }
        }

        // ---------- Escena ----------

        private static bool SetupGameScene(StringBuilder log)
        {
            Scene scene = EditorSceneManager.OpenScene(ProjectSetupStage0.ScenePath(SceneNames.Game), OpenSceneMode.Single);

            // Regla del proyecto: cargar los assets por ruta DESPUÉS de abrir la escena.
            var ballPrefab = AssetDatabase.LoadAssetAtPath<BallView>(ProjectSetupStage3.BallPrefabPath);
            var ballConfig = AssetDatabase.LoadAssetAtPath<BallConfig>(ProjectSetupStage3.BallConfigPath);
            var aimConfig = AssetDatabase.LoadAssetAtPath<AimConfig>(ProjectSetupStage3.AimConfigPath);
            var shardPrefab = AssetDatabase.LoadAssetAtPath<DebrisShard>(ProjectSetupStage5.ShardPrefabPath);
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);

            var root = ProjectSetupStage3.FindRootComponent<GameplayCompositionRoot>(scene);
            Camera camera = ProjectSetupStage3.FindInScene<Camera>(scene);
            if (root == null || camera == null)
            {
                log.AppendLine("- ERROR: SC_Game no tiene GameplayCompositionRoot o cámara. Ejecuta primero la Etapa 2.");
                return false;
            }

            RemoveRoot(scene, OldSandboxName, log);
            RemoveRoot(scene, OldCanvasName, log);
            ProjectSetupStage2.EnsureEventSystem(scene);

            camera.transform.SetPositionAndRotation(CameraPosition, Quaternion.LookRotation(CameraLookAt - CameraPosition));
            camera.fieldOfView = 60f;

            GameObject game = ProjectSetupStage3.FindRoot(scene, GameRootName);
            if (game == null)
            {
                game = new GameObject(GameRootName);
                log.AppendLine($"- {GameRootName}: contenedores de nivel, personajes, balones y fragmentos, y preview de apuntado.");
            }

            Transform level = GetOrCreateChild(game.transform, "Level");
            Transform characters = GetOrCreateChild(game.transform, "Characters");
            Transform balls = GetOrCreateChild(game.transform, "Balls");
            Transform debris = GetOrCreateChild(game.transform, "Debris");
            AimPreviewView aimPreview = game.GetComponentInChildren<AimPreviewView>(true);
            if (aimPreview == null)
            {
                aimPreview = ProjectSetupStage3.BuildAimPreview(game.transform,
                    ProjectSetupStage3.LoadMaterial(ProjectSetupStage3.LineMaterialName),
                    ProjectSetupStage3.LoadMaterial(ProjectSetupStage3.MarkerMaterialName));
            }

            ProjectSetupStage2.AssignReference(root, "_camera", camera);
            ProjectSetupStage2.AssignReference(root, "_ballContainer", balls);
            ProjectSetupStage2.AssignReference(root, "_aimPreview", aimPreview);
            ProjectSetupStage2.AssignReference(root, "_ballPrefab", ballPrefab);
            ProjectSetupStage2.AssignReference(root, "_ballConfig", ballConfig);
            ProjectSetupStage2.AssignReference(root, "_aimConfig", aimConfig);
            ProjectSetupStage2.AssignReference(root, "_levelCatalog", catalog);
            ProjectSetupStage2.AssignReference(root, "_levelContainer", level);
            ProjectSetupStage2.AssignReference(root, "_characterContainer", characters);
            ProjectSetupStage2.AssignReference(root, "_glassShardPrefab", shardPrefab);
            ProjectSetupStage2.AssignReference(root, "_debrisContainer", debris);

            bool uiOk = GameplayUiBuilder.Ensure(scene, root, log);
            ProjectSetupStage2.SaveScene(scene);

            bool ok = uiOk && ProjectSetupStage3.VerifyNoMissingReferences(root, log);
            if (ok)
            {
                log.AppendLine("- GameplayCompositionRoot e interfaz: referencias asignadas y verificadas.");
            }

            return ok;
        }

        private static void RemoveRoot(Scene scene, string name, StringBuilder log)
        {
            GameObject old = ProjectSetupStage3.FindRoot(scene, name);
            if (old != null)
            {
                Object.DestroyImmediate(old);
                log.AppendLine($"- Retirado {name} (sustituido por los niveles y la UI de la Etapa 6).");
            }
        }

        private static Transform GetOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            return child != null ? child : CreateChild(parent, name);
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject.transform;
        }
    }
}
