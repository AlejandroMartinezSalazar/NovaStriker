using System;

namespace NovaStriker.Infrastructure.Online
{
    /// <summary>
    /// Adapter del backend de cuentas (PlayFab hoy). Inicia sesión con un id del dispositivo, sin contraseña,
    /// creando la cuenta si no existe. <c>done</c> se llama una vez (éxito o error) en el hilo principal.
    /// </summary>
    public interface ILoginProvider
    {
        /// <summary>false si no se puede intentar (SDK sin instalar, sin Title ID...). El motivo, en <see cref="UnavailableReason"/>.</summary>
        bool IsAvailable { get; }

        string UnavailableReason { get; }

        void Login(string customId, Action<LoginOutcome> done);
    }
}
