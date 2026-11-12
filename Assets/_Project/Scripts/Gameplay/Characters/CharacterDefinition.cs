using UnityEngine;

namespace NovaStriker.Gameplay.Characters
{
    /// <summary>Datos de un tipo de futbolista. La CharacterFactory lo usa para crear y configurar instancias.</summary>
    [CreateAssetMenu(menuName = "NovaStriker/Characters/Character Definition", fileName = "SO_Character_Footballer")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Futbolista";

        [Tooltip("Cuenta para el objetivo del nivel. Hoy todos lo son (D7); false queda para futuros neutrales.")]
        [SerializeField] private bool _isTarget = true;

        [SerializeField] private FootballerController _prefab;

        [SerializeField] private KnockdownSettings _knockdown = KnockdownSettings.Default;

        [Header("Golpe")]
        [Tooltip("Exageración arcade del impulso que recibe el ragdoll al ser golpeado.")]
        [Min(0f)] [SerializeField] private float _hitImpulseMultiplier = 6f;

        [Tooltip("Impulso máximo (N·s) aplicado al ragdoll en un golpe.")]
        [Min(0f)] [SerializeField] private float _maxHitImpulse = 40f;

        public string DisplayName => _displayName;
        public bool IsTarget => _isTarget;
        public FootballerController Prefab => _prefab;
        public KnockdownSettings Knockdown => _knockdown;
        public float HitImpulseMultiplier => _hitImpulseMultiplier;
        public float MaxHitImpulse => _maxHitImpulse;
    }
}
