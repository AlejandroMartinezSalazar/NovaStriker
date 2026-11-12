namespace NovaStriker.Contracts.Data
{
    /// <summary>Estados de alto nivel de la aplicación. El 0 está reservado (None) por la StateMachine.</summary>
    public enum AppStateId
    {
        None = 0,
        Boot = 1,
        MainMenu = 2,
        Gameplay = 3,
    }
}
