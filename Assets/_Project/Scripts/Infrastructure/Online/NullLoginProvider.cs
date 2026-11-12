using System;

namespace NovaStriker.Infrastructure.Online
{
    /// <summary>Sin backend: nunca inicia sesión. Se usa cuando falta el SDK o la configuración.</summary>
    public sealed class NullLoginProvider : ILoginProvider
    {
        public NullLoginProvider(string reason)
        {
            UnavailableReason = reason;
        }

        public bool IsAvailable => false;

        public string UnavailableReason { get; }

        public void Login(string customId, Action<LoginOutcome> done)
        {
            done?.Invoke(LoginOutcome.Failed(UnavailableReason));
        }
    }
}
