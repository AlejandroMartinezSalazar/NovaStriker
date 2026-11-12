namespace NovaStriker.Core.Loop
{
    /// <summary>Adapter sobre la escala de tiempo del motor (la pausa la pone a 0).</summary>
    public interface ITimeScale
    {
        float Scale { get; set; }
    }
}
