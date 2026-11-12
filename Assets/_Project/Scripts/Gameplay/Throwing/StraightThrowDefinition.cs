using UnityEngine;

namespace NovaStriker.Gameplay.Throwing
{
    [CreateAssetMenu(menuName = "NovaStriker/Throws/Straight Throw", fileName = "SO_Throw_Straight")]
    public sealed class StraightThrowDefinition : ThrowStrategyDefinition
    {
        [SerializeField] private StraightThrowSettings _settings = StraightThrowSettings.Default;

        public override IThrowStrategy CreateStrategy() => new StraightThrowStrategy(_settings);
    }
}
