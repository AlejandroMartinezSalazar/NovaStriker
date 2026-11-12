using System;
using System.Linq;
using System.Text;
using NovaStriker.Bootstrap;
using NovaStriker.EditorTools.Setup.Ui;
using NovaStriker.Infrastructure.Ads;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Etapa 10: anuncios. Crea SO_AdsConfig (App ID del usuario + bloques de PRUEBA de Google), lo asigna a PF_AppRoot,
    /// conecta la cámara del menú (franja del banner) y, si el plugin Google Mobile Ads está importado, activa el
    /// símbolo NOVA_ADMOB para Android y copia el App ID a la configuración del plugin.
    /// </summary>
    internal static class ProjectSetupStage10
    {
        private const string Root = ProjectSetupStage0.ProjectRoot;
        private const string AdsFolder = Root + "/Settings/Ads";
        private const string ConfigPath = AdsFolder + "/SO_AdsConfig.asset";
        private const string AppRootPrefabPath = Root + "/Resources/" + AppBootstrapper.AppRootResourcePath + ".prefab";
        private const string AdMobDefine = "NOVA_ADMOB";
        private const string PluginFolder = "Assets/GoogleMobileAds";

        // El plugin reparte sus tipos en varias DLL según la versión; se prueba en las conocidas.
        private static readonly string[] MobileAdsTypeNames =
        {
            "GoogleMobileAds.Api.MobileAds, GoogleMobileAds",
            "GoogleMobileAds.Api.MobileAds, GoogleMobileAds.Core",
        };

        private static readonly string[] EdmPackages = { "com.google.external-dependency-manager", "com.unity.external-dependency-manager" };

        [MenuItem("NovaStriker/Setup/Etapa 10 - Anuncios")]
        private static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[NovaStriker Setup] Cancelado: hay escenas sin guardar.");
                return;
            }

            var log = new StringBuilder("[NovaStriker Setup] Etapa 10\n");
            bool ok = CreateConfig(log)
                      && AssignConfigToAppRoot(log)
                      && UiKit.PrepareAssets(log)
                      && MainMenuSceneBuilder.Setup(log);

            if (ok)
            {
                ConfigureAdMobPlugin(log);
                Debug.Log(log.ToString());
            }
            else
            {
                Debug.LogError(log.ToString());
            }
        }

        private static bool CreateConfig(StringBuilder log)
        {
            if (!AssetDatabase.IsValidFolder(AdsFolder))
            {
                AssetDatabase.CreateFolder(Root + "/Settings", "Ads");
            }

            ProjectSetupStage3.GetOrCreateAsset<AdsConfig>(ConfigPath, log);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static bool AssignConfigToAppRoot(StringBuilder log)
        {
            var config = AssetDatabase.LoadAssetAtPath<AdsConfig>(ConfigPath);
            GameObject contents = PrefabUtility.LoadPrefabContents(AppRootPrefabPath);
            try
            {
                var app = contents.GetComponent<AppCompositionRoot>();
                if (app == null)
                {
                    log.AppendLine($"- ERROR: {AppRootPrefabPath} no tiene AppCompositionRoot.");
                    return false;
                }

                ProjectSetupStage2.AssignReference(app, "_adsConfig", config);
                PrefabUtility.SaveAsPrefabAsset(contents, AppRootPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            bool ok = ProjectSetupStage3.VerifyNoMissingReferences(AssetDatabase.LoadAssetAtPath<AppCompositionRoot>(AppRootPrefabPath), log);
            if (ok)
            {
                log.AppendLine("- PF_AppRoot: SO_AdsConfig asignado (bloques de anuncio de PRUEBA de Google).");
            }

            return ok;
        }

        // ---------- Plugin Google Mobile Ads ----------

        private static void ConfigureAdMobPlugin(StringBuilder log)
        {
            bool pluginInstalled = MobileAdsTypeNames.Any(name => Type.GetType(name, false) != null);

            if (!pluginInstalled)
            {
                log.AppendLine("- AVISO: el plugin Google Mobile Ads no está importado. En el Editor se usan anuncios simulados; " +
                               "un build de Android no tendrá anuncios hasta importarlo y volver a ejecutar este menú.");
                return;
            }

            UpgradePluginAssets(log);
            CheckDependencyManager(log);
            SetDefine(NamedBuildTarget.Android, log);

            var config = AssetDatabase.LoadAssetAtPath<AdsConfig>(ConfigPath);
            if (!SetPluginAppId(config.AndroidAppId))
            {
                log.AppendLine($"- AVISO: no encontré la configuración del plugin. Pon el App ID a mano en " +
                               $"Assets > Google Mobile Ads > Settings > Android: {config.AndroidAppId}");
                return;
            }

            log.AppendLine($"- Google Mobile Ads: App ID de Android = {config.AndroidAppId}.");
        }

        /// <summary>
        /// El paquete trae .meta y prefabs en un formato antiguo ("PluginImporter object at version 1, below the
        /// supported minimum"): se reescriben en el formato de esta versión de Unity. No cambia su contenido.
        /// </summary>
        private static void UpgradePluginAssets(StringBuilder log)
        {
            string[] paths = AssetDatabase.FindAssets(string.Empty, new[] { PluginFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !AssetDatabase.IsValidFolder(path))
                .ToArray();
            if (paths.Length == 0)
            {
                return;
            }

            AssetDatabase.ForceReserializeAssets(paths, ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
            log.AppendLine($"- Google Mobile Ads: {paths.Length} archivos actualizados al formato de esta versión de Unity.");
        }

        /// <summary>Sin External Dependency Manager, el build de Android no incluye las librerías de Google.</summary>
        private static void CheckDependencyManager(StringBuilder log)
        {
            bool installed = EdmPackages.Any(name => UnityEditor.PackageManager.PackageInfo.FindForPackageName(name) != null)
                             || AssetDatabase.IsValidFolder("Assets/ExternalDependencyManager");
            if (installed)
            {
                log.AppendLine("- External Dependency Manager instalado. Antes de compilar: Assets > External Dependency Manager > " +
                               "Android Resolver > Force Resolve.");
                return;
            }

            log.AppendLine("- AVISO: falta External Dependency Manager (el plugin no pudo descargarlo). Con internet, reinicia Unity " +
                           "para que lo instale, o importa external-dependency-manager-*.unitypackage desde " +
                           "github.com/googlesamples/unity-jar-resolver (Releases).");
        }

        private static void SetDefine(NamedBuildTarget target, StringBuilder log)
        {
            string current = PlayerSettings.GetScriptingDefineSymbols(target);
            string[] symbols = current.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (symbols.Contains(AdMobDefine))
            {
                return;
            }

            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols.Append(AdMobDefine)));
            log.AppendLine($"- Símbolo {AdMobDefine} activado para Android (se compila el adapter de AdMob).");
        }

        /// <summary>Busca el asset de ajustes del plugin y escribe el App ID de Android (campo serializado del plugin).</summary>
        private static bool SetPluginAppId(string appId)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject GoogleMobileAdsSettings"))
            {
                Object settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (settings == null || settings.GetType().Name != "GoogleMobileAdsSettings")
                {
                    continue;
                }

                var serialized = new SerializedObject(settings);
                SerializedProperty property = serialized.FindProperty("adMobAndroidAppId");
                if (property == null)
                {
                    return false;
                }

                property.stringValue = appId;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                return true;
            }

            return false;
        }
    }
}
