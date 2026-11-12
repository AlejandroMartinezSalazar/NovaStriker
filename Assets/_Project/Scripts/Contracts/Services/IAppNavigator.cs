namespace NovaStriker.Contracts.Services
{
    /// <summary>
    /// Navegación entre pantallas de alto nivel. La UI la usa en lugar de tocar la máquina de estados.
    /// Las peticiones hechas mientras se carga una escena se ignoran (evita dobles toques).
    /// </summary>
    public interface IAppNavigator
    {
        void GoToMainMenu();
        void GoToGameplay();
    }
}
