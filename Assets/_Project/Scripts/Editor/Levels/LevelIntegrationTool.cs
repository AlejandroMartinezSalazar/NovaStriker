using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NovaStriker.Gameplay.Level;
using UnityEditor;
using UnityEngine;

namespace NovaStriker.EditorTools.Levels
{
    /// <summary>
    /// Integra en el juego los prefabs de nivel nuevos (PF_Level_NN):
    /// crea su definición SO_Level_NN si falta (copiando estrategia de tiro y balones del nivel anterior),
    /// la añade AL FINAL del catálogo y revisa cada nivel (punto del balón, cámara, futbolistas).
    /// Nunca reordena ni quita niveles del catálogo: el progreso guardado depende de su posición.
    /// Se puede ejecutar todas las veces que haga falta.
    /// </summary>
    internal static class LevelIntegrationTool
    {
        private const string PrefabsFolder = "Assets/_Project/Prefabs/Levels";
        private const string SettingsFolder = "Assets/_Project/Settings/Levels";
        private const string CatalogPath = SettingsFolder + "/SO_LevelCatalog.asset";
        private static readonly Regex LevelName = new Regex(@"^PF_Level_(\d+)$");

        private sealed class LevelEntry
        {
            public int Number;
            public string Suffix;
            public LevelLayout Layout;
            public LevelDefinition Definition;
            public string DefinitionPath => $"{SettingsFolder}/SO_Level_{Suffix}.asset";
        }

        [MenuItem("NovaStriker/Niveles/Integrar niveles nuevos")]
        private static void Run()
        {
            var log = new StringBuilder("[NovaStriker Niveles] Integrar niveles nuevos\n");
            int problems = Integrate(log);

            if (problems > 0)
            {
                Debug.LogWarning(log.ToString());
            }
            else if (problems == 0)
            {
                log.AppendLine("Todo correcto.");
                Debug.Log(log.ToString());
            }
            else
            {
                Debug.LogError(log.ToString());
            }
        }

        /// <summary>Hace la integración y devuelve los problemas encontrados (-1 si falta el catálogo).</summary>
        internal static int Integrate(StringBuilder log)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            if (catalog == null)
            {
                log.AppendLine($"- ERROR: no existe {CatalogPath}. Ejecuta antes la Etapa 6.");
                return -1;
            }

            List<LevelEntry> levels = FindLevelPrefabs(log);
            int problems = 0;
            LevelDefinition previous = null;

            foreach (LevelEntry level in levels)
            {
                level.Definition = GetOrCreateDefinition(level, previous, log);
                problems += Validate(level, log);
                previous = level.Definition;
            }

            problems += AppendToCatalog(catalog, levels, log);
            AssetDatabase.SaveAssets();
            return problems;
        }

        private static List<LevelEntry> FindLevelPrefabs(StringBuilder log)
        {
            var levels = new List<LevelEntry>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Match match = LevelName.Match(System.IO.Path.GetFileNameWithoutExtension(path));
                if (!match.Success)
                {
                    continue;
                }

                var layout = AssetDatabase.LoadAssetAtPath<LevelLayout>(path);
                if (layout == null)
                {
                    log.AppendLine($"- AVISO: {path} no tiene el componente LevelLayout en la raíz; se ignora.");
                    continue;
                }

                levels.Add(new LevelEntry
                {
                    Number = int.Parse(match.Groups[1].Value),
                    Suffix = match.Groups[1].Value,
                    Layout = layout,
                });
            }

            return levels.OrderBy(level => level.Number).ToList();
        }

        private static LevelDefinition GetOrCreateDefinition(LevelEntry level, LevelDefinition previous, StringBuilder log)
        {
            var definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(level.DefinitionPath);
            if (definition != null)
            {
                var existing = new SerializedObject(definition);
                SerializedProperty layout = existing.FindProperty("_layout");
                if (layout.objectReferenceValue == null)
                {
                    layout.objectReferenceValue = level.Layout;
                    existing.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(definition);
                    log.AppendLine($"- {level.DefinitionPath}: escenario asignado.");
                }

                return definition;
            }

            definition = ScriptableObject.CreateInstance<LevelDefinition>();
            AssetDatabase.CreateAsset(definition, level.DefinitionPath);

            int footballers = CountFootballers(level.Layout);
            int balls = Mathf.Max(previous != null ? previous.BallCount : 3, footballers + 1);

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_displayName").stringValue = $"Nivel {level.Number}";
            serialized.FindProperty("_ballCount").intValue = balls;
            serialized.FindProperty("_throwStrategy").objectReferenceValue = previous != null ? previous.ThrowStrategy : null;
            serialized.FindProperty("_layout").objectReferenceValue = level.Layout;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);

            string strategy = previous != null && previous.ThrowStrategy != null ? previous.ThrowStrategy.name : "ninguna";
            log.AppendLine($"- Creado {level.DefinitionPath}: {balls} balones, tiro {strategy} (copiados del nivel anterior; ajústalos si quieres).");
            return definition;
        }

        private static int Validate(LevelEntry level, StringBuilder log)
        {
            string name = level.Layout.name;
            int problems = 0;

            if (level.Layout.BallSpawnPoint == null || level.Layout.CameraPoint == null)
            {
                log.AppendLine($"- ERROR: {name} no tiene asignado BallSpawnPoint o CameraPoint (componente LevelLayout de la raíz).");
                problems++;
            }

            var spawns = new List<FootballerSpawnPoint>();
            level.Layout.CollectSpawnPoints(spawns);
            if (spawns.Count == 0)
            {
                log.AppendLine($"- ERROR: {name} no tiene futbolistas (FootballerSpawnPoint).");
                problems++;
            }

            foreach (FootballerSpawnPoint spawn in spawns.Where(spawn => spawn.Character == null))
            {
                log.AppendLine($"- ERROR: {name}/{spawn.name} no tiene futbolista (Character) asignado.");
                problems++;
            }

            if (level.Definition.ThrowStrategy == null)
            {
                log.AppendLine($"- ERROR: {level.DefinitionPath} no tiene estrategia de tiro.");
                problems++;
            }

            if (level.Definition.Layout != level.Layout)
            {
                string assigned = level.Definition.Layout != null ? level.Definition.Layout.name : "ninguno";
                log.AppendLine($"- AVISO: {level.DefinitionPath} usa el escenario {assigned}, no {name}. ¿Es intencionado?");
                problems++;
            }

            if (problems == 0)
            {
                log.AppendLine($"  · {name}: {spawns.Count} futbolistas, {level.Definition.BallCount} balones, " +
                               $"tiro {level.Definition.ThrowStrategy.name}.");
            }

            return problems;
        }

        private static int AppendToCatalog(LevelCatalog catalog, List<LevelEntry> levels, StringBuilder log)
        {
            var serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty("_levels");

            var inCatalog = new HashSet<Object>();
            int lastNumber = -1;
            for (int i = 0; i < list.arraySize; i++)
            {
                Object entry = list.GetArrayElementAtIndex(i).objectReferenceValue;
                inCatalog.Add(entry);
                LevelEntry known = levels.FirstOrDefault(level => level.Definition == entry);
                if (known != null)
                {
                    lastNumber = Mathf.Max(lastNumber, known.Number);
                }
            }

            int problems = 0;
            int added = 0;
            foreach (LevelEntry level in levels.Where(level => !inCatalog.Contains(level.Definition)))
            {
                if (level.Number < lastNumber)
                {
                    log.AppendLine($"- AVISO: {level.DefinitionPath} no estaba en el catálogo y se añade al final, " +
                                   "después de niveles con número mayor. Revisa el orden.");
                    problems++;
                }

                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                list.GetArrayElementAtIndex(index).objectReferenceValue = level.Definition;
                lastNumber = Mathf.Max(lastNumber, level.Number);
                added++;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            log.AppendLine($"- Catálogo: {added} niveles añadidos al final. Total: {list.arraySize}.");
            return problems;
        }

        private static int CountFootballers(LevelLayout layout)
        {
            var spawns = new List<FootballerSpawnPoint>();
            layout.CollectSpawnPoints(spawns);
            return spawns.Count;
        }
    }
}
