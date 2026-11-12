using UnityEngine;

namespace NovaStriker.Infrastructure.Online
{
    /// <summary>
    /// Configuración de PlayFab (Settings/Online/SO_PlayFabConfig). El Title ID está en el Game Manager de PlayFab,
    /// arriba a la izquierda, junto al nombre del título (4-6 caracteres, p. ej. "A1B2C").
    /// </summary>
    [CreateAssetMenu(menuName = "NovaStriker/Online/PlayFab Config", fileName = "SO_PlayFabConfig")]
    public sealed class PlayFabConfig : ScriptableObject
    {
        [Tooltip("Title ID de PlayFab (Game Manager > tu título).")]
        [SerializeField] private string _titleId = "";

        [Tooltip("Iniciar sesión automáticamente al abrir la app.")]
        [SerializeField] private bool _enabled = true;

        [Tooltip("Iniciar sesión también al pulsar Play en el Editor (crea un jugador para tu PC).")]
        [SerializeField] private bool _loginInEditor = true;

        [Tooltip("Segundos de espera antes de dar un intento por fallido (y reintentar más tarde).")]
        [SerializeField, Min(3f)] private float _timeoutSeconds = 15f;

        public string TitleId => string.IsNullOrWhiteSpace(_titleId) ? string.Empty : _titleId.Trim();
        public float TimeoutSeconds => _timeoutSeconds;

        /// <summary>Por qué no se debe intentar, o null si se puede.</summary>
        public string GetDisabledReason(bool isEditor) => DisabledReason(_enabled, TitleId, isEditor, _loginInEditor);

        internal static string DisabledReason(bool enabled, string titleId, bool isEditor, bool loginInEditor)
        {
            if (!enabled)
            {
                return "desactivado en SO_PlayFabConfig";
            }

            if (string.IsNullOrWhiteSpace(titleId))
            {
                return "falta el Title ID en SO_PlayFabConfig";
            }

            if (isEditor && !loginInEditor)
            {
                return "desactivado en el Editor (Login In Editor)";
            }

            return null;
        }
    }
}
