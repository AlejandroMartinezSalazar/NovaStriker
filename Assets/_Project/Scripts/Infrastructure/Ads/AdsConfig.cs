using UnityEngine;

namespace NovaStriker.Infrastructure.Ads
{
    /// <summary>
    /// IDs de AdMob y política de intersticiales. Etapa 10: bloques de prueba oficiales de Google;
    /// en la Etapa 11 se cambian por los reales (solo los tres IDs de bloque).
    /// El App ID no se usa en código: el setup lo copia a la configuración del plugin (AndroidManifest).
    /// </summary>
    [CreateAssetMenu(menuName = "NovaStriker/Ads/Ads Config", fileName = "SO_AdsConfig")]
    public sealed class AdsConfig : ScriptableObject
    {
        public const string TestBannerId = "ca-app-pub-3940256099942544/9214589741";
        public const string TestInterstitialId = "ca-app-pub-3940256099942544/1033173712";
        public const string TestRewardedId = "ca-app-pub-3940256099942544/5224354917";

        [Header("Android")]
        [SerializeField] private string _androidAppId = "ca-app-pub-6654333646555327~5481850987";
        [SerializeField] private string _androidBannerId = TestBannerId;
        [SerializeField] private string _androidInterstitialId = TestInterstitialId;
        [SerializeField] private string _androidRewardedId = TestRewardedId;

        [Header("Intersticial tras victorias")]
        [Tooltip("Hasta 'Switch At Victories' sale cada N victorias.")]
        [SerializeField, Min(1)] private int _earlyInterval = 4;

        [Tooltip("Victoria a partir de la cual cambia la frecuencia.")]
        [SerializeField, Min(1)] private int _switchAtVictories = 16;

        [Tooltip("Después sale cada N victorias.")]
        [SerializeField, Min(1)] private int _lateInterval = 2;

        [Header("Editor")]
        [Tooltip("En el Editor usar anuncios simulados (sin SDK ni red).")]
        [SerializeField] private bool _useFakeAdsInEditor = true;

        public string AndroidAppId => _androidAppId;
        public string BannerId => _androidBannerId;
        public string InterstitialId => _androidInterstitialId;
        public string RewardedId => _androidRewardedId;
        public bool UseFakeAdsInEditor => _useFakeAdsInEditor;

        public InterstitialPolicy CreatePolicy() => new InterstitialPolicy(_earlyInterval, _switchAtVictories, _lateInterval);

        public bool UsesTestAdUnits =>
            _androidBannerId == TestBannerId || _androidInterstitialId == TestInterstitialId || _androidRewardedId == TestRewardedId;
    }
}
