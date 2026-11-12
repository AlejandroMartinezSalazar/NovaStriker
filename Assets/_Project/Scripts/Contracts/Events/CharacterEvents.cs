using NovaStriker.Contracts.Data;
using NovaStriker.Core.Events;
using UnityEngine;

namespace NovaStriker.Contracts.Events
{
    /// <summary>Un futbolista fue derribado (se publica una sola vez por futbolista).</summary>
    public readonly struct CharacterKnockedDownEvent : IGameEvent
    {
        public readonly int CharacterId;

        /// <summary>Cuenta para el objetivo del nivel. Hoy todos los futbolistas son objetivo (decisión D7).</summary>
        public readonly bool IsTarget;

        public readonly KnockdownCause Cause;
        public readonly Vector3 Position;

        public CharacterKnockedDownEvent(int characterId, bool isTarget, KnockdownCause cause, Vector3 position)
        {
            CharacterId = characterId;
            IsTarget = isTarget;
            Cause = cause;
            Position = position;
        }
    }
}
