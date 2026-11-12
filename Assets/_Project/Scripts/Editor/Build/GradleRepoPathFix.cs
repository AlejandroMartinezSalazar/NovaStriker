#if UNITY_ANDROID
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEngine;

namespace NovaStriker.EditorTools.Build
{
    /// <summary>
    /// Arregla el build de Android cuando la ruta del proyecto tiene espacios ("D:/unity projects/...").
    /// El External Dependency Manager escribe en settings.gradle el repositorio local de Firebase como
    /// "file:///D:/unity projects/.../GeneratedLocalRepo/...", y Gradle no acepta espacios en una URI
    /// ("Cannot convert URI ... to a file"). Tras generar el proyecto Gradle, y antes de compilarlo, se cambian
    /// los espacios de esas URI por %20 (la forma válida). No toca las plantillas de Assets/Plugins/Android,
    /// que el resolver reescribe a menudo.
    /// </summary>
    internal sealed class GradleRepoPathFix : IPostGenerateGradleAndroidProject
    {
        private const string SettingsFile = "settings.gradle";

        // Una URI file:/// hasta el final de la cadena: comilla, '$' (cadenas $/.../$ de Groovy) o fin de línea.
        private static readonly Regex FileUri = new Regex(@"file:///[^""'$\r\n]*", RegexOptions.Compiled);

        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // 'path' es el módulo unityLibrary; settings.gradle está en la raíz del proyecto Gradle.
            DirectoryInfo root = Directory.GetParent(path);
            string settingsPath = root != null ? Path.Combine(root.FullName, SettingsFile) : null;
            if (settingsPath == null || !File.Exists(settingsPath))
            {
                Debug.LogWarning($"[NovaStriker Build] No se encontró {SettingsFile} junto a {path}; no se corrigen rutas con espacios.");
                return;
            }

            string original = File.ReadAllText(settingsPath);
            string fixedText = EscapeSpacesInFileUris(original);
            if (fixedText == original)
            {
                return;
            }

            File.WriteAllText(settingsPath, fixedText);
            Debug.Log("[NovaStriker Build] settings.gradle: espacios de las rutas file:/// cambiados por %20 (repositorio local de Firebase).");
        }

        internal static string EscapeSpacesInFileUris(string text)
        {
            return FileUri.Replace(text, match => match.Value.Replace(" ", "%20"));
        }
    }
}
#endif
