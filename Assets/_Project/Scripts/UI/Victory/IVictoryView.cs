namespace NovaStriker.UI.Victory
{
    /// <summary>Pantalla de victoria animada ("¡VICTORIA!" + subtítulo).</summary>
    public interface IVictoryView
    {
        void Show(string subtitle);
        void Hide();
    }
}
