using System.Collections.Generic;

namespace NovaStriker.Infrastructure.Analytics
{
    /// <summary>Sin analíticas: descarta todo (SDK sin instalar).</summary>
    public sealed class NullAnalyticsBackend : IAnalyticsBackend
    {
        public void Initialize()
        {
        }

        public void LogEvent(string name, IReadOnlyList<AnalyticsParameter> parameters)
        {
        }

        public void SetUserProperty(string name, string value)
        {
        }

        public void SetUserId(string userId)
        {
        }
    }
}
