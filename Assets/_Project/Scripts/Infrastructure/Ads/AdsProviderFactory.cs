using UnityEngine;

namespace NovaStriker.Infrastructure.Ads
{
    /// <summary>
    /// Elige el adapter de anuncios: simulado en el Editor (configurable), AdMob si está instalado (NOVA_ADMOB)
    /// y, si no, ninguno, con un aviso claro en consola (sin respaldo silencioso).
    /// </summary>
    public static class AdsProviderFactory
    {
        public static IAdsProvider Create(AdsConfig config, Transform host)
        {
#if UNITY_EDITOR
            if (config.UseFakeAdsInEditor)
            {
                return new FakeAdsProvider(host);
            }
#endif

#if NOVA_ADMOB
            if (config.UsesTestAdUnits && !Application.isEditor && !Debug.isDebugBuild)
            {
                Debug.LogWarning("[NovaStriker] Build de publicación con IDs de PRUEBA de AdMob. Cámbialos en SO_AdsConfig (Etapa 11).");
            }

            return new AdMobAdsProvider(config);
#else
            Debug.LogWarning("[NovaStriker] AdMob no está instalado (falta el símbolo NOVA_ADMOB): no habrá anuncios. " +
                             "Importa Google Mobile Ads y ejecuta NovaStriker/Setup/Etapa 10.");
            return new NullAdsProvider();
#endif
        }
    }
}
