using NovaStriker.Contracts.Data;

namespace NovaStriker.Infrastructure.Analytics
{
    /// <summary>Describe en una palabra dónde estaba el jugador (parámetro "context" de leave_game).</summary>
    public static class AnalyticsContext
    {
        public static string Describe(AppStateId app, GameStateId game, bool languageChosen)
        {
            switch (app)
            {
                case AppStateId.MainMenu:
                    return languageChosen ? "menu" : "language_picker";
                case AppStateId.Gameplay:
                    return DescribeGame(game);
                default:
                    return "loading";
            }
        }

        private static string DescribeGame(GameStateId game)
        {
            switch (game)
            {
                case GameStateId.Aiming:
                    return "aiming";
                case GameStateId.BallInFlight:
                case GameStateId.ShotResolve:
                    return "shot";
                case GameStateId.Paused:
                    return "paused";
                case GameStateId.Victory:
                    return "victory";
                case GameStateId.Defeat:
                    return "defeat";
                case GameStateId.ComingSoon:
                    return "coming_soon";
                default:
                    return "loading";
            }
        }
    }
}
