using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Analíticas con Firebase. Si el SDK (FirebaseAnalytics.unitypackage) está importado, activa el símbolo
    /// NOVA_FIREBASE para Android (se compila el adapter); si no, lo quita para que el proyecto siga compilando.
    /// Comprueba que google-services.json exista y sea del paquete de la app, y lista los parámetros que hay que
    /// registrar en la consola de Firebase para verlos en los informes.
    /// </summary>
    internal static class FirebaseSetup
    {
        private const string Define = "NOVA_FIREBASE";
        private const string AnalyticsTypeName = "Firebase.Analytics.FirebaseAnalytics, Firebase.Analytics";

        // Parámetros propios (los de Firebase como level, level_name y success no hace falta registrarlos aparte
        // si ya aparecen; registrarlos no hace daño).
        private static readonly string[] NumberParameters = { "level", "attempt", "balls", "seconds", "balls_used", "balls_left", "success", "ad_ready", "first_time" };
        private static readonly string[] TextParameters = { "level_name", "context", "from", "result", "language" };

        [MenuItem("NovaStriker/Setup/Firebase - Analíticas")]
        private static void Run()
        {
            var log = new StringBuilder("[NovaStriker Setup] Firebase - Analíticas\n");
            bool sdkInstalled = Type.GetType(AnalyticsTypeName, false) != null;
            bool warnings = !sdkInstalled;

            SetDefine(NamedBuildTarget.Android, sdkInstalled, log);
            if (!sdkInstalled)
            {
                log.AppendLine("- AVISO: el SDK de Firebase Analytics no está importado (FirebaseAnalytics.unitypackage del Firebase Unity SDK).");
            }

            warnings |= !CheckGoogleServices(log);

            log.AppendLine("- En la consola de Firebase > Analytics > Custom definitions, crea estas definiciones personalizadas (ámbito Evento):");
            log.AppendLine($"    Métricas (número): {string.Join(", ", NumberParameters)}");
            log.AppendLine($"    Dimensiones (texto): {string.Join(", ", TextParameters)}");
            log.AppendLine("  Y una dimensión de ámbito Usuario: highest_level.");

            if (warnings)
            {
                Debug.LogWarning(log.ToString());
            }
            else
            {
                log.AppendLine("- Listo. En el Editor los eventos salen en consola con el prefijo [Analytics]; en Android van a Firebase.");
                Debug.Log(log.ToString());
            }
        }

        private static bool CheckGoogleServices(StringBuilder log)
        {
            string[] paths = AssetDatabase.FindAssets("google-services")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileName(p) == "google-services.json")
                .ToArray();

            if (paths.Length == 0)
            {
                log.AppendLine("- AVISO: falta google-services.json (consola de Firebase > Configuración del proyecto > tu app Android).");
                return false;
            }

            if (paths.Length > 1)
            {
                log.AppendLine($"- AVISO: hay {paths.Length} google-services.json; deja solo uno: {string.Join(", ", paths)}");
                return false;
            }

            string applicationId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            string json = File.ReadAllText(paths[0]);
            if (!json.Contains($"\"{applicationId}\""))
            {
                log.AppendLine($"- AVISO: {paths[0]} no incluye el paquete {applicationId}. Descárgalo de la app Android correcta en Firebase.");
                return false;
            }

            log.AppendLine($"- {paths[0]}: paquete {applicationId} correcto.");
            return true;
        }

        private static void SetDefine(NamedBuildTarget target, bool enabled, StringBuilder log)
        {
            string[] symbols = PlayerSettings.GetScriptingDefineSymbols(target)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            bool present = symbols.Contains(Define);
            if (present == enabled)
            {
                log.AppendLine($"- Símbolo {Define} {(enabled ? "ya activo" : "inactivo")} para Android.");
                return;
            }

            string[] updated = enabled ? symbols.Append(Define).ToArray() : symbols.Where(s => s != Define).ToArray();
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", updated));
            log.AppendLine(enabled
                ? $"- Símbolo {Define} activado para Android (se compila el adapter de Firebase; Unity recompila ahora)."
                : $"- Símbolo {Define} quitado (sin SDK no se puede compilar el adapter).");
        }
    }
}
