using System.Collections.Generic;

namespace NovaStriker.Infrastructure.Analytics
{
    /// <summary>
    /// Adapter del servicio de analíticas (Firebase hoy; consola en el Editor). Nunca debe lanzar excepciones
    /// hacia el juego: si el servicio no está listo o falla, los eventos se guardan en cola o se descartan.
    /// </summary>
    public interface IAnalyticsBackend
    {
        void Initialize();

        void LogEvent(string name, IReadOnlyList<AnalyticsParameter> parameters);

        void SetUserProperty(string name, string value);

        void SetUserId(string userId);
    }
}
