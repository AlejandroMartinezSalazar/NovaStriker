using NovaStriker.Infrastructure.Online;
using UnityEngine;

namespace NovaStriker.Bootstrap.Online
{
    /// <summary>
    /// Elige el adapter de cuentas: PlayFab si el SDK está instalado (NOVA_PLAYFAB) y hay Title ID;
    /// si no, ninguno (el juego funciona igual, con un aviso en consola).
    /// </summary>
    public static class LoginProviderFactory
    {
        public static ILoginProvider Create(PlayFabConfig config)
        {
            if (config == null)
            {
                return new NullLoginProvider("falta SO_PlayFabConfig en PF_AppRoot; ejecuta NovaStriker/Setup/PlayFab - Inicio de sesión");
            }

            string disabled = config.GetDisabledReason(Application.isEditor);
            if (disabled != null)
            {
                return new NullLoginProvider(disabled);
            }

#if NOVA_PLAYFAB
            return new PlayFabLoginProvider(config.TitleId);
#else
            return new NullLoginProvider("SDK de PlayFab no instalado (falta el símbolo NOVA_PLAYFAB); " +
                                         "importa el SDK y ejecuta NovaStriker/Setup/PlayFab - Inicio de sesión");
#endif
        }
    }
}
