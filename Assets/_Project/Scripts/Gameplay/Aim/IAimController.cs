namespace NovaStriker.Gameplay.Aim
{
    /// <summary>Permite o bloquea que el jugador apunte y dispare.</summary>
    public interface IAimController
    {
        bool IsEnabled { get; }
        void Enable();
        void Disable();
    }
}
