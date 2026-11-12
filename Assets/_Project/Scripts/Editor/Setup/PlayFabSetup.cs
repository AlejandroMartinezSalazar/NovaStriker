using System;
using System.Linq;
using System.Text;
using NovaStriker.Bootstrap;
using NovaStriker.Infrastructure.Online;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Inicio de sesión automático con PlayFab. Crea Settings/Online/SO_PlayFabConfig, lo asigna a PF_AppRoot y,
    /// si el SDK de PlayFab está importado, activa el símbolo NOVA_PLAYFAB para Android (se compila el adapter).
    /// Si el SDK no está, quita el símbolo para que el proyecto siga compilando. Se puede ejecutar varias veces.
    /// </summary>
    internal static class PlayFabSetup
    {
        private const string Root = ProjectSetupStage0.ProjectRoot;
        private const string OnlineFolder = Root + "/Settings/Online";
        private const string ConfigPath = OnlineFolder + "/SO_PlayFabConfig.asset";
        private const string AppRootPrefabPath = Root + "/Resources/" + AppBootstrapper.AppRootResourcePath + ".prefab";
        private const string Define = "NOVA_PLAYFAB";

        // Tipo público del SDK oficial, en su ensamblado "PlayFab" (PlayFabSDK/PlayFab.asmdef).
        private const string ClientApiTypeName = "PlayFab.PlayFabClientAPI, PlayFab";

        [MenuItem("NovaStriker/Setup/PlayFab - Inicio de sesión")]
        private static void Run()
        {
            var log = new StringBuilder("[NovaStriker Setup] PlayFab - Inicio de sesión\n");
            PlayFabConfig config = CreateConfig(log);
            if (!AssignConfigToAppRoot(config, log))
            {
                Debug.LogError(log.ToString());
                return;
            }

            bool sdkInstalled = Type.GetType(ClientApiTypeName, false) != null;
            bool warnings = false;
            if (sdkInstalled)
            {
                SetDefine(NamedBuildTarget.Android, true, log);
            }
            else
            {
                SetDefine(NamedBuildTarget.Android, false, log);
                log.AppendLine("- AVISO: el SDK de PlayFab no está importado. Assets > Import Package > Custom Package... > " +
                               "UnitySDK.unitypackage (carpeta ThirdParty/PlayFab del proyecto) y vuelve a ejecutar este menú.");
                warnings = true;
            }

            if (string.IsNullOrEmpty(config.TitleId))
            {
                log.AppendLine($"- AVISO: escribe tu Title ID en {ConfigPath} (campo Title Id). Sin él se juega sin cuenta.");
                warnings = true;
            }
            else
            {
                log.AppendLine($"- Title ID: {config.TitleId}");
            }

            if (warnings)
            {
                Debug.LogWarning(log.ToString());
            }
            else
            {
                log.AppendLine("- Listo: al abrir la app se inicia sesión sola (sin contraseña). Mira la consola al pulsar Play.");
                Debug.Log(log.ToString());
            }

            Selection.activeObject = config;
        }

        private static PlayFabConfig CreateConfig(StringBuilder log)
        {
            if (!AssetDatabase.IsValidFolder(OnlineFolder))
            {
                AssetDatabase.CreateFolder(Root + "/Settings", "Online");
            }

            var config = ProjectSetupStage3.GetOrCreateAsset<PlayFabConfig>(ConfigPath, log);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static bool AssignConfigToAppRoot(PlayFabConfig config, StringBuilder log)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(AppRootPrefabPath);
            try
            {
                var app = contents.GetComponent<AppCompositionRoot>();
                if (app == null)
                {
                    log.AppendLine($"- ERROR: {AppRootPrefabPath} no tiene AppCompositionRoot.");
                    return false;
                }

                ProjectSetupStage2.AssignReference(app, "_playFabConfig", config);
                PrefabUtility.SaveAsPrefabAsset(contents, AppRootPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            bool ok = ProjectSetupStage3.VerifyNoMissingReferences(AssetDatabase.LoadAssetAtPath<AppCompositionRoot>(AppRootPrefabPath), log);
            if (ok)
            {
                log.AppendLine("- PF_AppRoot: SO_PlayFabConfig asignado.");
            }

            return ok;
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
                ? $"- Símbolo {Define} activado para Android (se compila el adapter de PlayFab; Unity recompila ahora)."
                : $"- Símbolo {Define} quitado (sin SDK no se puede compilar el adapter).");
        }
    }
}
