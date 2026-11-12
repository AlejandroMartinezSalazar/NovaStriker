namespace NovaStriker.Core.Loop
{
    /// <summary>IClock sobre UnityEngine.Time.time.</summary>
    public sealed class UnityClock : IClock
    {
        public float Time => UnityEngine.Time.time;
    }
}
