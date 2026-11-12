using UnityEngine;

namespace NovaStriker.Infrastructure.Analytics
{
    /// <summary>Elige el adapter: consola en el Editor, Firebase en el móvil (NOVA_FIREBASE) o ninguno, con aviso.</summary>
    public static class AnalyticsBackendFactory
    {
        public static IAnalyticsBackend Create()
        {
#if UNITY_EDITOR
            return new ConsoleAnalyticsBackend();
#elif NOVA_FIREBASE
            return new FirebaseAnalyticsBackend();
#else
            Debug.LogWarning("[NovaStriker] Firebase no está instalado (falta el símbolo NOVA_FIREBASE): no habrá analíticas. " +
                             "Ejecuta NovaStriker/Setup/Firebase - Analíticas.");
            return new NullAnalyticsBackend();
#endif
        }
    }
}
