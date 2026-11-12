using NovaStriker.Contracts.Services;
using NovaStriker.Core.DI;
using NovaStriker.Core.Utils;
using NovaStriker.Gameplay.Level;
using NovaStriker.UI.Common;
using NovaStriker.UI.Localization;
using NovaStriker.UI.MainMenu;
using NovaStriker.UI.Settings;
using UnityEngine;

namespace NovaStriker.Bootstrap.Scenes
{
    /// <summary>
    /// Raíz de composición de SC_MainMenu. El fondo 3D (campo y futbolista bailando) es decorado de escena:
    /// no necesita servicios.
    /// </summary>
    public sealed class MainMenuCompositionRoot : SceneCompositionRoot
    {
        [SerializeField] private MainMenuView _view;
        [Tooltip("El mismo catálogo que usa SC_Game: da el número total de niveles.")]
        [SerializeField] private LevelCatalog _levelCatalog;
        [SerializeField] private SettingsView _settingsView;
        [Tooltip("Pantalla de idioma de la primera vez (NovaStriker/Setup/Idiomas).")]
        [SerializeField] private LanguagePickerView _languagePickerView;
        [Tooltip("Cámara del escenario 3D (se recorta bajo el banner de anuncios).")]
        [SerializeField] private Camera _camera;

        protected override void OnCompose(IServiceContainer sceneContainer, CompositeDisposable disposables)
        {
            MainMenuView view = Require(_view, nameof(_view));
            LevelCatalog catalog = Require(_levelCatalog, nameof(_levelCatalog));
            var navigator = sceneContainer.Resolve<IAppNavigator>();
            var progress = sceneContainer.Resolve<IProgressService>();
            var localization = sceneContainer.Resolve<ILocalization>();

            disposables.Add(LocalizedTextInstaller.InstallAll(view.transform.root, localization));
            disposables.Add(new MainMenuPresenter(view, navigator, progress, catalog.Count, localization));
            disposables.Add(new SettingsPresenter(Require(_settingsView, nameof(_settingsView)),
                sceneContainer.Resolve<IGameSettings>(), localization));
            if (_languagePickerView != null)
            {
                disposables.Add(new LanguagePickerPresenter(_languagePickerView, localization));
            }
            else
            {
                Debug.LogWarning("[NovaStriker] Falta la pantalla de idioma. Ejecuta NovaStriker/Setup/Idiomas.");
            }
            disposables.Add(new ButtonClickSounds(view.transform.root, sceneContainer.Resolve<IAudioService>()));
            disposables.Add(new BannerLayoutBinder(sceneContainer.Resolve<IAdsService>(), view.transform.root, Require(_camera, nameof(_camera))));
        }
    }
}
