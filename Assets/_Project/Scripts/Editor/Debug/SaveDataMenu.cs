using System.IO;
using NovaStriker.Bootstrap;
using UnityEditor;
using UnityEngine;

namespace NovaStriker.EditorTools.Debugging
{
    /// <summary>Herramientas del Editor para el guardado (Etapa 8): ver la carpeta y borrar el progreso para probar.</summary>
    internal static class SaveDataMenu
    {
        [MenuItem("NovaStriker/Guardado/Abrir carpeta de guardado")]
        private static void OpenFolder()
        {
            string directory = AppCompositionRoot.SaveDirectory;
            Directory.CreateDirectory(directory);
            EditorUtility.RevealInFinder(directory);
        }

        [MenuItem("NovaStriker/Guardado/Borrar progreso y ajustes")]
        private static void DeleteAll()
        {
            string directory = AppCompositionRoot.SaveDirectory;
            if (!Directory.Exists(directory))
            {
                Debug.Log("[NovaStriker] No hay datos guardados.");
                return;
            }

            if (!EditorUtility.DisplayDialog("Borrar guardado",
                    $"Se borrarán el progreso y los ajustes guardados en:\n{directory}", "Borrar", "Cancelar"))
            {
                return;
            }

            Directory.Delete(directory, true);
            Debug.Log("[NovaStriker] Progreso y ajustes borrados. La próxima partida empieza en el nivel 1.");
        }

        [MenuItem("NovaStriker/Guardado/Borrar progreso y ajustes", true)]
        private static bool CanDeleteAll() => !EditorApplication.isPlaying;
    }
}
