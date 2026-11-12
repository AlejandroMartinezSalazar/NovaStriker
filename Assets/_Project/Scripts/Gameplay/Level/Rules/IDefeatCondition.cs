using NovaStriker.Contracts.Data;

namespace NovaStriker.Gameplay.Level.Rules
{
    /// <summary>Strategy: una forma de perder el nivel. Devuelve None si no se cumple.</summary>
    public interface IDefeatCondition
    {
        DefeatReason Check(in LevelStatus status);
    }
}
