using UnityEngine;

namespace NovaStriker.Gameplay.Throwing
{
    [CreateAssetMenu(menuName = "NovaStriker/Throws/Curve Throw", fileName = "SO_Throw_Curve")]
    public sealed class CurveThrowDefinition : ThrowStrategyDefinition
    {
        [SerializeField] private CurveThrowSettings _settings = CurveThrowSettings.Default;

        public override IThrowStrategy CreateStrategy() => new CurveThrowStrategy(_settings);
    }
}
