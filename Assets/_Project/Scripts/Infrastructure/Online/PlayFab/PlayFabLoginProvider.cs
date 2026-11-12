#if NOVA_PLAYFAB
using System;
using PlayFab;
using PlayFab.ClientModels;

namespace NovaStriker.Infrastructure.Online
{
    /// <summary>
    /// Adapter de PlayFab: LoginWithCustomID con CreateAccount = true (la primera vez crea el jugador; después
    /// entra en la misma cuenta). Vive en su propio ensamblado, que solo se compila con el símbolo NOVA_PLAYFAB
    /// (lo activa NovaStriker/Setup/PlayFab al detectar el SDK).
    /// </summary>
    public sealed class PlayFabLoginProvider : ILoginProvider
    {
        private readonly string _titleId;

        public PlayFabLoginProvider(string titleId)
        {
            if (string.IsNullOrWhiteSpace(titleId))
            {
                throw new ArgumentException("Falta el Title ID de PlayFab.", nameof(titleId));
            }

            _titleId = titleId.Trim();
        }

        public bool IsAvailable => true;

        public string UnavailableReason => null;

        public void Login(string customId, Action<LoginOutcome> done)
        {
            // El SDK arma la URL del servidor con este Title ID.
            PlayFabSettings.staticSettings.TitleId = _titleId;

            var request = new LoginWithCustomIDRequest
            {
                TitleId = _titleId,
                CustomId = customId,
                CreateAccount = true,
            };

            PlayFabClientAPI.LoginWithCustomID(request,
                result => done?.Invoke(LoginOutcome.Succeeded(result.PlayFabId, result.NewlyCreated)),
                error => done?.Invoke(LoginOutcome.Failed(error.GenerateErrorReport())));
        }
    }
}
#endif
