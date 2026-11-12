using NovaStriker.Contracts.Services;
using TMPro;
using UnityEngine;

namespace NovaStriker.UI.Localization
{
    /// <summary>
    /// Texto fijo de la interfaz que se traduce solo (títulos, botones...). Guarda su clave (TextKey) y se
    /// actualiza al cambiar de idioma. Lo inicializa LocalizedTextInstaller desde la raíz de composición de la escena.
    /// Los textos que cambian con la partida (nivel, balones...) los pone su presentador, no este componente.
    /// Se da de baja del servicio al cerrarse la escena (Release, desde el instalador) y, por si acaso, también
    /// si recibe un aviso estando ya destruido: un objeto que nunca se activó no recibe OnDestroy.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [Tooltip("Clave del texto (ver Contracts/Data/TextKey).")]
        [SerializeField] private string _key;
        [SerializeField] private TMP_Text _label;

        private ILocalization _localization;

        public string Key => _key;

        public void Construct(ILocalization localization)
        {
            Release();
            _localization = localization;
            if (_localization == null)
            {
                return;
            }

            _localization.LanguageChanged += Refresh;
            Refresh();
        }

        /// <summary>Deja de escuchar los cambios de idioma.</summary>
        public void Release()
        {
            if (_localization != null)
            {
                _localization.LanguageChanged -= Refresh;
                _localization = null;
            }
        }

        private void Refresh()
        {
            if (this == null)
            {
                Release(); // Destruido sin pasar por OnDestroy (nunca estuvo activo).
                return;
            }

            if (_label == null)
            {
                _label = GetComponent<TMP_Text>();
            }

            _label.text = _localization.Get(_key);
        }

        private void OnDestroy() => Release();
    }
}
