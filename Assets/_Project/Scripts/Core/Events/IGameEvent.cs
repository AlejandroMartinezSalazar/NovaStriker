namespace NovaStriker.Core.Events
{
    /// <summary>
    /// Marca un evento del juego. Los eventos son readonly struct con datos inmutables:
    /// no generan basura al publicarse y nadie puede modificarlos en tránsito.
    /// </summary>
    public interface IGameEvent
    {
    }
}
