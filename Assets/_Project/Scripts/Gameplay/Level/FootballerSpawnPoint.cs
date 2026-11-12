using NovaStriker.Gameplay.Characters;
using UnityEngine;

namespace NovaStriker.Gameplay.Level
{
    /// <summary>
    /// Marca dónde aparece un futbolista en el prefab del nivel y de qué tipo es.
    /// Solo es un dato: el futbolista lo crea CharacterFactory al construir el nivel.
    /// </summary>
    public sealed class FootballerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private CharacterDefinition _character;

        public CharacterDefinition Character => _character;

        private void OnDrawGizmos()
        {
            bool isTarget = _character == null || _character.IsTarget;
            Gizmos.color = isTarget ? new Color(1f, 0.35f, 0.2f) : new Color(0.4f, 0.8f, 1f);
            Vector3 feet = transform.position;
            Gizmos.DrawWireSphere(feet + Vector3.up * 0.3f, 0.3f);
            Gizmos.DrawWireSphere(feet + Vector3.up * 1.5f, 0.3f);
            Gizmos.DrawLine(feet, feet + transform.forward * 0.6f);
        }
    }
}
