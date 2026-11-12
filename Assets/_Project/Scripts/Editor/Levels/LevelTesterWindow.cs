using System.Collections.Generic;
using NovaStriker.Bootstrap;
using NovaStriker.Contracts.Data;
using NovaStriker.EditorTools.Setup;
using NovaStriker.Gameplay.Level;
using NovaStriker.Infrastructure.Progress;
using NovaStriker.Infrastructure.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NovaStriker.EditorTools.Levels
{
    /// <summary>
    /// Herramienta de desarrollo: lista los niveles del catálogo y arranca el juego en el elegido.
    /// Escribe ese nivel en el progreso guardado del Editor (conserva las victorias) y entra en Play desde SC_Boot;
    /// en el menú, JUGAR lleva directamente a ese nivel. Solo existe en el Editor: no va en el build.
    /// </summary>
    internal sealed class LevelTesterWindow : EditorWindow
    {
        private const string CatalogPath = "Assets/_Project/Settings/Levels/SO_LevelCatalog.asset";

        private readonly List<FootballerSpawnPoint> _spawnBuffer = new List<FootballerSpawnPoint>();
        private Vector2 _scroll;

        [MenuItem("NovaStriker/Desarrollo/Probar nivel...")]
        private static void Open()
        {
            GetWindow<LevelTesterWindow>("Probar nivel").Show();
        }

        private void OnGUI()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            if (catalog == null)
            {
                EditorGUILayout.HelpBox($"No existe {CatalogPath}.", MessageType.Error);
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Sal de Play para elegir otro nivel.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox("Jugar: guarda ese nivel como el actual y arranca el juego. En el menú, pulsa JUGAR.",
                MessageType.None);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < catalog.Count; i++)
            {
                DrawLevel(catalog.Get(i), i);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("Ver 'Próximamente' (campaña completa)"))
            {
                PlayFrom(catalog.Count);
            }
        }

        private void DrawLevel(LevelDefinition level, int index)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (level == null)
                {
                    EditorGUILayout.LabelField($"{index + 1}. (vacío en el catálogo)");
                    return;
                }

                int footballers = 0;
                if (level.Layout != null)
                {
                    level.Layout.CollectSpawnPoints(_spawnBuffer);
                    footballers = _spawnBuffer.Count;
                    _spawnBuffer.Clear();
                }

                EditorGUILayout.LabelField($"{index + 1}. {level.name}", GUILayout.MinWidth(120f));
                EditorGUILayout.LabelField($"{footballers} futbolistas · {level.BallCount} balones", GUILayout.MinWidth(150f));

                using (new EditorGUI.DisabledScope(level.Layout == null))
                {
                    if (GUILayout.Button("Prefab", GUILayout.Width(60f)))
                    {
                        AssetDatabase.OpenAsset(level.Layout.gameObject);
                    }
                }

                if (GUILayout.Button("Jugar", GUILayout.Width(60f)))
                {
                    PlayFrom(index);
                }
            }
        }

        private static void PlayFrom(int levelIndex)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var slot = new SaveSlot<ProgressData>(
                new FileSaveStorage(AppCompositionRoot.SaveDirectory),
                new JsonUtilitySerializer(),
                PersistentProgressService.SaveKeyName,
                () => new ProgressData());

            ProgressData data = slot.Load();
            data.CurrentLevelIndex = levelIndex;
            if (!slot.Save(data))
            {
                Debug.LogError("[NovaStriker Desarrollo] No se pudo escribir el progreso. Revisa la consola.");
                return;
            }

            Debug.Log($"[NovaStriker Desarrollo] Progreso del Editor en el nivel {levelIndex + 1}. Arrancando el juego...");
            EditorSceneManager.OpenScene(ProjectSetupStage0.ScenePath(SceneNames.Boot));
            EditorApplication.EnterPlaymode();
        }
    }
}
