using System;
using System.Text;
using NovaStriker.EditorTools.Setup;
using NovaStriker.Gameplay.Characters;
using NovaStriker.Gameplay.Level;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NovaStriker.EditorTools.Levels
{
    /// <summary>
    /// Herramientas para generar niveles por código sobre un prefab de nivel existente: conserva el escenario
    /// (suelo, cámara, punto del balón) y solo vacía y rellena "Obstacles" y "FootballerSpawns".
    /// </summary>
    internal sealed class LevelPrefabEditor
    {
        internal const string LevelsFolder = ProjectSetupStage0.ProjectRoot + "/Prefabs/Levels/";
        internal const string SettingsFolder = ProjectSetupStage0.ProjectRoot + "/Settings/Levels/";
        private const string ObstaclesName = "Obstacles";
        private const string SpawnsName = "FootballerSpawns";

        /// <summary>Margen para que un futbolista no nazca hundido en su apoyo.</summary>
        internal const float OnTop = 0.02f;

        private readonly Transform _obstacles;
        private readonly Transform _spawns;
        private readonly CharacterDefinition _character;
        private readonly Quaternion _spawnRotation;

        private LevelPrefabEditor(Transform obstacles, Transform spawns, CharacterDefinition character, Quaternion spawnRotation)
        {
            _obstacles = obstacles;
            _spawns = spawns;
            _character = character;
            _spawnRotation = spawnRotation;
        }

        public int ObstacleCount => _obstacles.childCount;
        public int SpawnCount => _spawns.childCount;

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

        /// <summary>Abre el prefab, vacía obstáculos y futbolistas, aplica el diseño y guarda.</summary>
        internal static bool Rebuild(string path, Action<LevelPrefabEditor> design, StringBuilder log)
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

                var editor = new LevelPrefabEditor(obstacles, spawns, template.Character, template.transform.localRotation);
                ClearChildren(obstacles);
                ClearChildren(spawns);
                design(editor);

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                log.AppendLine($"- {path}: {editor.ObstacleCount} obstáculos, {editor.SpawnCount} futbolistas.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>Pone nombre y balones a SO_Level_NN.</summary>
        internal static void SetDefinition(string assetName, string displayName, int balls, StringBuilder log)
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

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }
    }
}
