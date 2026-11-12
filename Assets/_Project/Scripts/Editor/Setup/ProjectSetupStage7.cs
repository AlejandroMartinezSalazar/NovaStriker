using System.Text;
using NovaStriker.Bootstrap.Scenes;
using NovaStriker.Contracts.Data;
using NovaStriker.EditorTools.Setup.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Etapa 7: interfaz final. Prepara sprites y fuente, monta la UI de SC_Game (HUD, victoria animada,
    /// pausa/derrota) y SC_MainMenu (escenario 3D, panel de nivel, JUGAR, Próximamente).
    /// Idempotente: lo que ya esté en formato Etapa 7 no se toca (para rehacerlo, borra el canvas y vuelve a ejecutar).
    /// </summary>
    internal static class ProjectSetupStage7
    {
        [MenuItem("NovaStriker/Setup/Etapa 7 - Interfaz final")]
        private static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[NovaStriker Setup] Cancelado: hay escenas sin guardar.");
                return;
            }

            var log = new StringBuilder("[NovaStriker Setup] Etapa 7\n");
            bool ok = UiKit.PrepareAssets(log) && SetupGameScene(log) && MainMenuSceneBuilder.Setup(log);

            if (ok)
            {
                Debug.Log(log.ToString());
            }
            else
            {
                Debug.LogError(log.ToString());
            }
        }

        private static bool SetupGameScene(StringBuilder log)
        {
            Scene scene = EditorSceneManager.OpenScene(ProjectSetupStage0.ScenePath(SceneNames.Game), OpenSceneMode.Single);
            var root = ProjectSetupStage3.FindRootComponent<GameplayCompositionRoot>(scene);
            if (root == null)
            {
                log.AppendLine("- ERROR: SC_Game no tiene GameplayCompositionRoot. Ejecuta antes las Etapas 2 y 6.");
                return false;
            }

            bool uiOk = GameplayUiBuilder.Ensure(scene, root, log);
            ProjectSetupStage2.SaveScene(scene);
            return uiOk && ProjectSetupStage3.VerifyNoMissingReferences(root, log);
        }
    }
}
