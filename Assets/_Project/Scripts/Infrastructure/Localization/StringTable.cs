using System.Collections.Generic;
using NovaStriker.Contracts.Data;

namespace NovaStriker.Infrastructure.Localization
{
    /// <summary>
    /// Traducciones de todos los textos del juego (clave → español, inglés, portugués).
    /// Para añadir un texto: añade su clave en TextKey (y en TextKey.All) y una línea aquí.
    /// Las pruebas comprueban que ninguna clave falta y que los {0}/{1} coinciden en los tres idiomas.
    /// </summary>
    public static class StringTable
    {
        private readonly struct Row
        {
            public readonly string Spanish;
            public readonly string English;
            public readonly string Portuguese;

            public Row(string spanish, string english, string portuguese)
            {
                Spanish = spanish;
                English = english;
                Portuguese = portuguese;
            }
        }

        private static readonly Dictionary<string, Row> Rows = new Dictionary<string, Row>
        {
            [TextKey.MenuPlay] = new Row("JUGAR", "PLAY", "JOGAR"),
            [TextKey.MenuLevel] = new Row("NIVEL {0}", "LEVEL {0}", "NÍVEL {0}"),

            [TextKey.HudTargets] = new Row("Por derribar: {0}/{1}", "To knock down: {0}/{1}", "Para derrubar: {0}/{1}"),
            [TextKey.HudBalls] = new Row("Balones: {0}", "Balls: {0}", "Bolas: {0}"),

            [TextKey.PauseTitle] = new Row("PAUSA", "PAUSED", "PAUSA"),
            [TextKey.PauseMessage] = new Row("Nivel {0}", "Level {0}", "Nível {0}"),
            [TextKey.PauseContinue] = new Row("CONTINUAR", "CONTINUE", "CONTINUAR"),
            [TextKey.PauseRestart] = new Row("REINICIAR", "RESTART", "REINICIAR"),

            [TextKey.DefeatOutOfBalls] = new Row("¡SIN BALONES!", "OUT OF BALLS!", "SEM BOLAS!"),
            [TextKey.DefeatFailed] = new Row("NIVEL FALLIDO", "LEVEL FAILED", "NÍVEL FALHOU"),
            [TextKey.DefeatRemaining] = new Row("Quedaron {0} por derribar", "{0} left to knock down", "Faltaram {0} para derrubar"),
            [TextKey.DefeatRetry] = new Row("REINTENTAR", "RETRY", "TENTAR DE NOVO"),
            [TextKey.DefeatExtraBall] = new Row("+2 BALONES", "+2 BALLS", "+2 BOLAS"),
            [TextKey.ResultsMenu] = new Row("MENÚ", "MENU", "MENU"),

            [TextKey.ComingSoonTitle] = new Row("PRÓXIMAMENTE", "COMING SOON", "EM BREVE"),
            [TextKey.ComingSoonSubtitle] = new Row("NUEVOS NIVELES", "NEW LEVELS", "NOVOS NÍVEIS"),
            [TextKey.ComingSoonMessage] = new Row("Has superado todos los niveles disponibles.",
                "You have beaten every available level.", "Você superou todos os níveis disponíveis."),
            [TextKey.ComingSoonOnTheWay] = new Row("Nuevos niveles en camino.", "New levels are on the way.",
                "Novos níveis a caminho."),
            [TextKey.ComingSoonRestart] = new Row("EMPEZAR DESDE EL NIVEL 1", "START FROM LEVEL 1", "COMEÇAR DO NÍVEL 1"),

            [TextKey.VictoryTitle] = new Row("¡VICTORIA!", "VICTORY!", "VITÓRIA!"),
            [TextKey.VictorySubtitle] = new Row("NIVEL {0} COMPLETADO", "LEVEL {0} COMPLETE", "NÍVEL {0} CONCLUÍDO"),

            [TextKey.SettingsTitle] = new Row("AJUSTES", "SETTINGS", "AJUSTES"),
            [TextKey.SettingsMusic] = new Row("MÚSICA", "MUSIC", "MÚSICA"),
            [TextKey.SettingsSfx] = new Row("EFECTOS", "SOUND EFFECTS", "EFEITOS"),
            [TextKey.SettingsLanguage] = new Row("IDIOMA", "LANGUAGE", "IDIOMA"),
            [TextKey.SettingsClose] = new Row("CERRAR", "CLOSE", "FECHAR"),

            [TextKey.LanguagePickerTitle] = new Row("ELIGE TU IDIOMA", "CHOOSE YOUR LANGUAGE", "ESCOLHA SEU IDIOMA"),

            [TextKey.TutorialDrag] = new Row("Arrastra el dedo hasta el futbolista\ny suéltalo para lanzar",
                "Drag your finger to the player\nand release to shoot",
                "Arraste o dedo até o jogador\ne solte para chutar"),
        };

        public static IEnumerable<string> Keys => Rows.Keys;

        /// <summary>Texto de <paramref name="key"/> en <paramref name="language"/>, o null si no existe.</summary>
        public static string Find(string key, Language language)
        {
            if (key == null || !Rows.TryGetValue(key, out Row row))
            {
                return null;
            }

            switch (language)
            {
                case Language.English:
                    return row.English;
                case Language.Portuguese:
                    return row.Portuguese;
                default:
                    return row.Spanish;
            }
        }
    }
}
