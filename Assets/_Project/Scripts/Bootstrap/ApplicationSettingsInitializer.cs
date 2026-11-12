using UnityEngine;

namespace NovaStriker.Bootstrap
{
    /// <summary>
    /// Ajustes de plataforma que deben aplicarse antes de cargar la primera escena.
    /// No guarda estado: es seguro con Domain Reload desactivado.
    /// </summary>
    internal static class ApplicationSettingsInitializer
    {
        private const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // En móviles el valor por defecto es 30 FPS.
            Application.targetFrameRate = TargetFrameRate;
            // Evita que la pantalla se apague mientras se apunta.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
