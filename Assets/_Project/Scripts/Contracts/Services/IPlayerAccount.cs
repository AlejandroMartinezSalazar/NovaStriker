using System;
using NovaStriker.Contracts.Data;

namespace NovaStriker.Contracts.Services
{
    /// <summary>
    /// Cuenta del jugador en el backend (PlayFab), creada sin contraseña al abrir la app.
    /// Nunca bloquea el juego: si no hay sesión, todo sigue funcionando en local.
    /// </summary>
    public interface IPlayerAccount
    {
        LoginStatus Status { get; }

        /// <summary>Id del jugador en el backend, o null si aún no hay sesión.</summary>
        string PlayerId { get; }

        bool IsLoggedIn { get; }

        event Action<LoginStatus> StatusChanged;
    }
}
