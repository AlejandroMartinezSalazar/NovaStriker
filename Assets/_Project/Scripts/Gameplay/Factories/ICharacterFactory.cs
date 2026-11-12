using NovaStriker.Gameplay.Characters;
using UnityEngine;

namespace NovaStriker.Gameplay.Factories
{
    /// <summary>Crea futbolistas configurados a partir de su definición.</summary>
    public interface ICharacterFactory
    {
        FootballerController Create(CharacterDefinition definition, Vector3 position, Quaternion rotation);
    }
}
