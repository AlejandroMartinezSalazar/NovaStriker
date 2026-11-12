using System.Collections.Generic;
using System.Text;
using NovaStriker.Contracts.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Configuración inicial del proyecto (Etapa 0). Idempotente: se puede ejecutar varias veces.
    /// No toca Application ID, Company Name ni paquetes (decisión D2).
    /// </summary>
    internal static class ProjectSetupStage0
    {
        public const string ProjectRoot = "Assets/_Project";
        public const string ScenesFolder = ProjectRoot + "/Scenes";

        public static string ScenePath(string sceneName) => $"{ScenesFolder}/{sceneName}.unity";

        private static readonly string[] Folders =
        {
            "Art/Materials", "Art/Models", "Art/Textures", "Art/Animations",
            "Audio/Music", "Audio/SFX", "Audio/Mixers",
            "PhysicsMaterials",
            "Prefabs/Balls", "Prefabs/Characters", "Prefabs/Obstacles", "Prefabs/Levels",
            "Prefabs/UI", "Prefabs/VFX", "Prefabs/Bootstrap",
            "Resources",
            "Scenes",
            "Settings/Config", "Settings/Throws", "Settings/Characters", "Settings/Obstacles",
            "Settings/Levels", "Settings/Input",
        };

        private static readonly (int Index, string Name)[] Layers =
        {
            (GameLayers.Ball, GameLayers.BallName),
            (GameLayers.Ragdoll, GameLayers.RagdollName),
            (GameLayers.Obstacle, GameLayers.ObstacleName),
            (GameLayers.Breakable, GameLayers.BreakableName),
            (GameLayers.Environment, GameLayers.EnvironmentName),
            (GameLayers.KillZone, GameLayers.KillZoneName),
            (GameLayers.Debris, GameLayers.DebrisName),
            (GameLayers.AimBackstop, GameLayers.AimBackstopName),
        };

        /// <summary>Pares de capas del juego que SÍ colisionan (simétrico). El resto entre capas del juego se ignora.</summary>
        public static readonly (int A, int B)[] CollidingPairs =
        {
            (GameLayers.Ball, GameLayers.Ball),
            (GameLayers.Ball, GameLayers.Ragdoll),
            (GameLayers.Ball, GameLayers.Obstacle),
            (GameLayers.Ball, GameLayers.Breakable),
            (GameLayers.Ball, GameLayers.Environment),
            (GameLayers.Ball, GameLayers.KillZone),

            (GameLayers.Ragdoll, GameLayers.Ragdoll),
            (GameLayers.Ragdoll, GameLayers.Obstacle),
            (GameLayers.Ragdoll, GameLayers.Breakable),
            (GameLayers.Ragdoll, GameLayers.Environment),
            (GameLayers.Ragdoll, GameLayers.KillZone),

            (GameLayers.Obstacle, GameLayers.Obstacle),
            (GameLayers.Obstacle, GameLayers.Breakable),
            (GameLayers.Obstacle, GameLayers.Environment),
            (GameLayers.Obstacle, GameLayers.KillZone),
            (GameLayers.Obstacle, GameLayers.Debris),

            (GameLayers.Environment, GameLayers.Debris),
            (GameLayers.KillZone, GameLayers.Debris),
        };

        [MenuItem("NovaStriker/Setup/Etapa 0 - Configurar proyecto")]
        private static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[NovaStriker Setup] Cancelado: hay escenas sin guardar.");
                return;
            }

            var log = new StringBuilder("[NovaStriker Setup] Etapa 0\n");

            CreateFolders(log);
            bool layersOk = ConfigureLayers(log);
            if (layersOk)
            {
                ConfigureCollisionMatrix(log);
            }
            CreateScenes(log);
            ConfigureBuildScenes(log);
            ConfigurePlayerSettings(log);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(ScenePath(SceneNames.Boot));

            if (layersOk)
            {
                Debug.Log(log.ToString());
            }
            else
            {
                Debug.LogError(log.ToString());
            }
        }

        private static void CreateFolders(StringBuilder log)
        {
            foreach (string relative in Folders)
            {
                string current = ProjectRoot;
                EnsureFolder("Assets", "_Project");
                foreach (string part in relative.Split('/'))
                {
                    EnsureFolder(current, part);
                    current = $"{current}/{part}";
                }
            }
            log.AppendLine("- Carpetas verificadas.");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static bool ConfigureLayers(StringBuilder log)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                log.AppendLine("- ERROR: no se pudo cargar TagManager.asset.");
                return false;
            }

            var tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            bool ok = true;

            foreach ((int index, string name) in Layers)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(index);
                if (slot.stringValue == name)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(slot.stringValue))
                {
                    log.AppendLine($"- ERROR: la capa {index} ya se llama '{slot.stringValue}', se esperaba '{name}'. No se sobrescribe.");
                    ok = false;
                    continue;
                }

                slot.stringValue = name;
                log.AppendLine($"- Capa {index} = {name}");
            }

            tagManager.ApplyModifiedPropertiesWithoutUndo();
            return ok;
        }

        private static void ConfigureCollisionMatrix(StringBuilder log)
        {
            var colliding = new HashSet<(int, int)>();
            foreach ((int a, int b) in CollidingPairs)
            {
                colliding.Add((a, b));
                colliding.Add((b, a));
            }

            // Entre capas del juego: solo colisionan los pares declarados.
            foreach (var layerA in Layers)
            {
                foreach (var layerB in Layers)
                {
                    bool collides = colliding.Contains((layerA.Index, layerB.Index));
                    Physics.IgnoreLayerCollision(layerA.Index, layerB.Index, !collides);
                }
            }

            // AimBackstop solo existe para los raycasts de apuntado: no colisiona con ninguna capa.
            for (int layer = 0; layer < 32; layer++)
            {
                Physics.IgnoreLayerCollision(GameLayers.AimBackstop, layer, true);
            }

            Object[] dynamics = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset");
            if (dynamics != null && dynamics.Length > 0)
            {
                EditorUtility.SetDirty(dynamics[0]);
            }

            log.AppendLine("- Matriz de colisión configurada.");
        }

        private static void CreateScenes(StringBuilder log)
        {
            CreateSceneIfMissing(SceneNames.Boot, NewSceneSetup.EmptyScene, log);
            CreateSceneIfMissing(SceneNames.MainMenu, NewSceneSetup.DefaultGameObjects, log);
            CreateSceneIfMissing(SceneNames.Game, NewSceneSetup.DefaultGameObjects, log);
        }

        private static void CreateSceneIfMissing(string sceneName, NewSceneSetup setup, StringBuilder log)
        {
            string path = ScenePath(sceneName);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
            {
                log.AppendLine($"- Escena ya existe: {path}");
                return;
            }

            var scene = EditorSceneManager.NewScene(setup, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
            log.AppendLine($"- Escena creada: {path}");
        }

        private static void ConfigureBuildScenes(StringBuilder log)
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath(SceneNames.Boot), true),
                new EditorBuildSettingsScene(ScenePath(SceneNames.MainMenu), true),
                new EditorBuildSettingsScene(ScenePath(SceneNames.Game), true),
            };
            log.AppendLine("- Build Settings: SC_Boot, SC_MainMenu, SC_Game (SampleScene queda fuera del build, no se borra).");
        }

        private static void ConfigurePlayerSettings(StringBuilder log)
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            log.AppendLine("- Orientación: Portrait fija.");
        }
    }
}
