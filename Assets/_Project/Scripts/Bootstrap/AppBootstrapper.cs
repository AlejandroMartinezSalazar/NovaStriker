using UnityEngine;

namespace NovaStriker.Bootstrap
{
    /// <summary>
    /// Crea la raíz global antes de cargar la primera escena, tanto en builds como en el Editor
    /// (aunque se dé Play desde SC_MainMenu o SC_Game). No guarda estado estático.
    /// </summary>
    internal static class AppBootstrapper
    {
        internal const string AppRootResourcePath = "PF_AppRoot";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateAppRoot()
        {
            AppCompositionRoot prefab = Resources.Load<AppCompositionRoot>(AppRootResourcePath);
            if (prefab == null)
            {
                Debug.LogError(
                    $"[AppBootstrapper] No se encontró Resources/{AppRootResourcePath}. " +
                    "Ejecuta NovaStriker/Setup/Etapa 2 - AppRoot y escenas provisionales.");
                return;
            }

            AppCompositionRoot instance = Object.Instantiate(prefab);
            instance.name = "[AppRoot]";
        }
    }
}
