namespace NovaStriker.Core.Loop
{
    /// <summary>Fuente de tiempo inyectable (permite tests sin depender de UnityEngine.Time).</summary>
    public interface IClock
    {
        /// <summary>Tiempo escalado en segundos (se detiene con la pausa).</summary>
        float Time { get; }
    }
}
