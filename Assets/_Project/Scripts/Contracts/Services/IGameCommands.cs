namespace NovaStriker.Contracts.Services
{
    /// <summary>
    /// Órdenes que la UI puede dar a la partida. Cada orden se ignora si no tiene sentido en el estado actual
    /// (p. ej. Pause durante la pantalla de victoria), así la UI no necesita conocer los estados.
    /// Tras la victoria no hay orden: la partida vuelve sola al menú principal (Etapa 7).
    /// </summary>
    public interface IGameCommands
    {
        void Pause();
        void Resume();

        /// <summary>Reconstruye el nivel actual desde cero (en pausa o tras la derrota).</summary>
        void RestartLevel();

        /// <summary>Desde "Próximamente" dentro de la partida: vuelve a empezar en el nivel 1.</summary>
        void RestartCampaign();

        /// <summary>
        /// Tras la derrota: añade un balón y sigue jugando (anuncio recompensado "+1 balón", D5).
        /// Solo una vez por intento del nivel.
        /// </summary>
        void ContinueWithExtraBall();

        /// <summary>Se puede continuar con "+1 balón": hay derrota y aún no se usó en este intento del nivel.</summary>
        bool CanContinueWithExtraBall { get; }

        void GoToMainMenu();
    }
}
