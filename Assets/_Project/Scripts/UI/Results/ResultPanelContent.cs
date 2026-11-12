namespace NovaStriker.UI.Results
{
    /// <summary>
    /// Lo que muestra el panel. Un botón con texto null se oculta; el secundario puede verse desactivado
    /// (p. ej. "+1 BALÓN" hasta que existan los anuncios).
    /// </summary>
    public readonly struct ResultPanelContent
    {
        public readonly string Title;
        public readonly string Message;
        public readonly string PrimaryLabel;
        public readonly string SecondaryLabel;
        public readonly bool SecondaryEnabled;
        public readonly ResultPanelStyle Style;

        public ResultPanelContent(string title, string message, string primaryLabel, string secondaryLabel = null,
            bool secondaryEnabled = true, ResultPanelStyle style = ResultPanelStyle.Neutral)
        {
            SecondaryEnabled = secondaryEnabled;
            Style = style;
            Title = title;
            Message = message;
            PrimaryLabel = primaryLabel;
            SecondaryLabel = secondaryLabel;
        }
    }
}
