namespace NovaStriker.Infrastructure.Analytics
{
    /// <summary>
    /// Nombres de eventos, parámetros y propiedades. level_start, level_end, tutorial_begin y tutorial_complete
    /// son eventos recomendados de Firebase (con sus parámetros level, level_name y success). "app_background"
    /// está reservado por Firebase, por eso la salida del juego se llama leave_game.
    /// </summary>
    public static class AnalyticsNames
    {
        public static class Events
        {
            public const string LanguageSelected = "language_selected";
            public const string TutorialBegin = "tutorial_begin";
            public const string TutorialComplete = "tutorial_complete";
            public const string LevelStart = "level_start";
            public const string LevelEnd = "level_end";
            public const string LevelRetry = "level_retry";
            public const string LevelQuit = "level_quit";
            public const string LeaveGame = "leave_game";
            public const string RewardedOffer = "rewarded_offer";
            public const string RewardedComplete = "rewarded_complete";
            public const string InterstitialShown = "interstitial_shown";
        }

        public static class Params
        {
            public const string Level = "level";
            public const string LevelName = "level_name";
            public const string Attempt = "attempt";
            public const string Balls = "balls";
            public const string Success = "success";
            public const string Seconds = "seconds";
            public const string BallsUsed = "balls_used";
            public const string BallsLeft = "balls_left";
            public const string From = "from";
            public const string Context = "context";
            public const string Result = "result";
            public const string AdReady = "ad_ready";
            public const string Language = "language";
            public const string FirstTime = "first_time";
        }

        public static class UserProperties
        {
            public const string HighestLevel = "highest_level";
        }

        /// <summary>Valores de "result" en rewarded_offer.</summary>
        public static class OfferResults
        {
            public const string Accepted = "accepted";
            public const string Declined = "declined";
            public const string Left = "left";
        }

        /// <summary>Valores de "from" en level_retry.</summary>
        public static class RetryFrom
        {
            public const string Defeat = "defeat";
            public const string Pause = "pause";
        }
    }
}
