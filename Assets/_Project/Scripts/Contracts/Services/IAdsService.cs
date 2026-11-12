using System;

namespace NovaStriker.Contracts.Services
{
    /// <summary>
    /// Anuncios del juego (D5, D6): banner fijo arriba, intersticial tras ciertas victorias y recompensado "+1 balón".
    /// La política (cuándo toca un intersticial) vive en la implementación; el juego solo avisa de los momentos.
    /// </summary>
    public interface IAdsService
    {
        /// <summary>Alto real (píxeles de pantalla) del banner visible; 0 si no hay banner.</summary>
        float BannerHeightPixels { get; }

        /// <summary>Cambió el alto del banner (se cargó, se ocultó...). La UI y la cámara dejan esa franja libre.</summary>
        event Action BannerLayoutChanged;

        bool IsRewardedReady { get; }

        /// <summary>
        /// Tras una victoria: si según la política toca intersticial y hay uno cargado, lo muestra.
        /// <paramref name="onFinished"/> se llama siempre (al cerrarse el anuncio o enseguida si no hay).
        /// </summary>
        void ShowInterstitialIfDue(int totalVictories, Action onFinished);

        /// <summary>Muestra el recompensado. onCompleted(true) solo si se vio entero.</summary>
        void ShowRewarded(Action<bool> onCompleted);
    }
}
