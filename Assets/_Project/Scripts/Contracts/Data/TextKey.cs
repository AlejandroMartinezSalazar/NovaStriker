namespace NovaStriker.Contracts.Data
{
    /// <summary>
    /// Claves de los textos traducibles. Las traducciones están en Infrastructure/Localization/StringTable.
    /// Las que llevan {0}/{1} se rellenan con ILocalization.Format.
    /// </summary>
    public static class TextKey
    {
        public const string MenuPlay = "menu.play";
        public const string MenuLevel = "menu.level";                     // {0} = número de nivel

        public const string HudTargets = "hud.targets";                   // {0} quedan, {1} total
        public const string HudBalls = "hud.balls";                       // {0} balones

        public const string PauseTitle = "pause.title";
        public const string PauseMessage = "pause.message";               // {0} = número de nivel
        public const string PauseContinue = "pause.continue";
        public const string PauseRestart = "pause.restart";

        public const string DefeatOutOfBalls = "defeat.outOfBalls";
        public const string DefeatFailed = "defeat.failed";
        public const string DefeatRemaining = "defeat.remaining";         // {0} = futbolistas en pie
        public const string DefeatRetry = "defeat.retry";
        public const string DefeatExtraBall = "defeat.extraBall";
        public const string ResultsMenu = "results.menu";

        public const string ComingSoonTitle = "comingSoon.title";
        public const string ComingSoonSubtitle = "comingSoon.subtitle";
        public const string ComingSoonMessage = "comingSoon.message";
        public const string ComingSoonOnTheWay = "comingSoon.onTheWay";
        public const string ComingSoonRestart = "comingSoon.restart";

        public const string VictoryTitle = "victory.title";
        public const string VictorySubtitle = "victory.subtitle";         // {0} = número de nivel

        public const string SettingsTitle = "settings.title";
        public const string SettingsMusic = "settings.music";
        public const string SettingsSfx = "settings.sfx";
        public const string SettingsLanguage = "settings.language";
        public const string SettingsClose = "settings.close";

        public const string LanguagePickerTitle = "language.title";

        public const string TutorialDrag = "tutorial.drag";

        /// <summary>Todas las claves (las pruebas comprueban que cada idioma las traduce).</summary>
        public static readonly string[] All =
        {
            MenuPlay, MenuLevel, HudTargets, HudBalls,
            PauseTitle, PauseMessage, PauseContinue, PauseRestart,
            DefeatOutOfBalls, DefeatFailed, DefeatRemaining, DefeatRetry, DefeatExtraBall, ResultsMenu,
            ComingSoonTitle, ComingSoonSubtitle, ComingSoonMessage, ComingSoonOnTheWay, ComingSoonRestart,
            VictoryTitle, VictorySubtitle,
            SettingsTitle, SettingsMusic, SettingsSfx, SettingsLanguage, SettingsClose,
            LanguagePickerTitle, TutorialDrag,
        };
    }
}
