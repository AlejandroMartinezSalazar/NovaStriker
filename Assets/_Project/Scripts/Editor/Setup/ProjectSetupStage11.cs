using System;
using System.IO;
using System.Linq;
using System.Text;
using NovaStriker.Contracts.Data;
using NovaStriker.Infrastructure.Ads;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Etapa 11: publicación en Android. Aplica la identidad de la app (paquete ya publicado en Google Play), la keystore
    /// y el formato AAB, y revisa lo que solo el usuario puede completar: contraseñas y alias de la keystore,
    /// versionCode mayor que el publicado, IDs reales de AdMob y dependencias duplicadas.
    /// Nunca escribe contraseñas ni cambia la versión: eso lo decide el usuario en Player Settings.
    /// </summary>
    internal static class ProjectSetupStage11
    {
        internal const string ApplicationId = "com.oxac.globalsoccercup";
        internal const string ProductName = "NovaStriker";
        internal const string KeystorePath = "D:/SoccerStrikerCopSeg/SoccerStriker_2.1.10_9_Abril2026_6v/quantum_unity/user.keystore";

        private const string AdsConfigPath = ProjectSetupStage0.ProjectRoot + "/Settings/Ads/SO_AdsConfig.asset";
        private const string ManualEdmFolder = "Assets/ExternalDependencyManager";
        private const string UpmEdmPackage = "com.unity.external-dependency-manager";
        private const string PipelinePackage = "com.unity.pipeline";

        [MenuItem("NovaStriker/Setup/Etapa 11 - Publicación Android")]
        private static void Run()
        {
            var log = new StringBuilder("[NovaStriker Setup] Etapa 11\n");
            int pending = 0;

            ApplyIdentity(log);
            pending += ApplyKeystore(log);
            ApplyBuildFormat(log);

            log.AppendLine("\nPendiente de revisar:");
            pending += CheckVersion(log);
            pending += CheckBuildScenes(log);
            pending += CheckAdUnits(log);
            pending += CheckDependencies(log);

            AssetDatabase.SaveAssets();

            if (pending == 0)
            {
                log.AppendLine("- Nada. Listo para File > Build Profiles > Android > Build (genera el .aab).");
            }

            if (pending > 0)
            {
                Debug.LogWarning(log.ToString());
            }
            else
            {
                Debug.Log(log.ToString());
            }
        }

        // ---------- Se aplica ----------

        private static void ApplyIdentity(StringBuilder log)
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.productName = ProductName;
            log.AppendLine($"- Paquete: {ApplicationId}. Nombre visible: {ProductName}.");
            log.AppendLine($"- Company Name sin cambios: '{PlayerSettings.companyName}' (en Android no afecta a nada visible).");
        }

        private static int ApplyKeystore(StringBuilder log)
        {
            if (!File.Exists(KeystorePath))
            {
                log.AppendLine($"- ERROR: no existe la keystore en {KeystorePath}. No se activa la firma personalizada.");
                return 1;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = KeystorePath;
            log.AppendLine($"- Firma: keystore {KeystorePath}.");

            if (string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName))
            {
                log.AppendLine("- Firma pendiente: en Player Settings > Publishing Settings escribe la contraseña de la keystore, " +
                               "elige el alias (el mismo con el que se firmó la versión publicada) y su contraseña. " +
                               "Unity no guarda las contraseñas: se piden de nuevo al reabrir el proyecto.");
                return 1;
            }

            log.AppendLine($"- Alias: {PlayerSettings.Android.keyaliasName}. Recuerda escribir las contraseñas en cada sesión de Unity.");
            return 0;
        }

        private static void ApplyBuildFormat(StringBuilder log)
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;
            log.AppendLine("- Build: IL2CPP, ARM64, App Bundle (.aab), sin Development Build.");
        }

        // ---------- Se revisa (no se cambia) ----------

        private static int CheckVersion(StringBuilder log)
        {
            log.AppendLine($"- Versión actual: versionName '{PlayerSettings.bundleVersion}', versionCode {PlayerSettings.Android.bundleVersionCode}. " +
                           "Como el paquete ya está publicado, el versionCode debe ser MAYOR que el de la última versión en " +
                           "Google Play (Play Console > Versiones). Cámbialo en Player Settings > Other Settings.");
            return 1;
        }

        private static int CheckBuildScenes(StringBuilder log)
        {
            string[] expected =
            {
                ProjectSetupStage0.ScenePath(SceneNames.Boot),
                ProjectSetupStage0.ScenePath(SceneNames.MainMenu),
                ProjectSetupStage0.ScenePath(SceneNames.Game),
            };
            string[] enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();

            string[] missing = expected.Where(path => !enabled.Contains(path)).ToArray();
            if (missing.Length > 0)
            {
                log.AppendLine($"- ERROR: faltan escenas en el build: {string.Join(", ", missing)}.");
                return 1;
            }

            if (enabled.FirstOrDefault() != expected[0])
            {
                log.AppendLine($"- ERROR: la primera escena del build debe ser {expected[0]}.");
                return 1;
            }

            return 0;
        }

        private static int CheckAdUnits(StringBuilder log)
        {
            var config = AssetDatabase.LoadAssetAtPath<AdsConfig>(AdsConfigPath);
            if (config == null)
            {
                log.AppendLine($"- ERROR: no existe {AdsConfigPath}. Ejecuta antes la Etapa 10.");
                return 1;
            }

            if (config.UsesTestAdUnits)
            {
                log.AppendLine($"- Anuncios: {AdsConfigPath} aún tiene bloques de PRUEBA de Google. Pon tus IDs reales de " +
                               "banner, intersticial y rewarded antes de subir a producción (para pruebas internas puede quedarse así).");
                return 1;
            }

            return 0;
        }

        private static int CheckDependencies(StringBuilder log)
        {
            string[] packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Select(package => package.name).ToArray();
            int pending = 0;

            if (packages.Contains(UpmEdmPackage) && AssetDatabase.IsValidFolder(ManualEdmFolder))
            {
                log.AppendLine($"- External Dependency Manager está dos veces: el paquete {UpmEdmPackage} y la carpeta {ManualEdmFolder}. " +
                               $"Borra la carpeta {ManualEdmFolder} (la importamos a mano cuando no había conexión) y su .meta.");
                pending++;
            }

            if (packages.Contains(PipelinePackage))
            {
                log.AppendLine($"- Aviso: {PipelinePackage} (experimental, venía con la plantilla) sigue instalado. " +
                               "No lo usa el juego; quitarlo es opcional (Package Manager > Remove).");
            }

            return pending;
        }
    }
}
