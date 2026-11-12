using UnityEngine;

namespace NovaStriker.Gameplay.Throwing
{
    [CreateAssetMenu(menuName = "NovaStriker/Throws/Lob Throw", fileName = "SO_Throw_Lob")]
    public sealed class LobThrowDefinition : ThrowStrategyDefinition
    {
        [SerializeField] private LobThrowSettings _settings = LobThrowSettings.Default;

        public override IThrowStrategy CreateStrategy() => new LobThrowStrategy(_settings);
    }
}
