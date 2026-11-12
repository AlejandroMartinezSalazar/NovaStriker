using System;
using System.Collections.Generic;
using NovaStriker.Bootstrap.App;
using NovaStriker.Bootstrap.Scenes;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.DI;
using NovaStriker.Core.StateMachine;
using NUnit.Framework;

namespace NovaStriker.Tests.EditMode.Bootstrap
{
    public class AppFlowTests
    {
        private sealed class FakeSceneLoader : ISceneLoader
        {
            public readonly List<(string Scene, Action OnLoaded)> Requests = new List<(string, Action)>();
            private int _pending;

            public bool IsLoading => _pending > 0;
            public string ActiveSceneName { get; set; } = SceneNames.Boot;

            public void LoadScene(string sceneName, Action onLoaded)
            {
                _pending++;
                Requests.Add((sceneName, onLoaded));
            }

            public void Complete(int index)
            {
                _pending--;
                ActiveSceneName = Requests[index].Scene;
                Requests[index].OnLoaded();
            }
        }

        private sealed class FakeRoot : ISceneCompositionRoot
        {
            public int ComposeCount;
            public int DisposeCount;
            public IServiceContainer ReceivedContainer;

            public bool CanCompose => ComposeCount == 0;

            public void Compose(IServiceContainer appContainer)
            {
                ComposeCount++;
                ReceivedContainer = appContainer;
            }

            public void Dispose() => DisposeCount++;
        }

        private sealed class FakeRootProvider : ISceneRootProvider
        {
            public readonly Dictionary<string, FakeRoot> Roots = new Dictionary<string, FakeRoot>();

            public bool TryGetRoot(string sceneName, out ISceneCompositionRoot root)
            {
                bool found = Roots.TryGetValue(sceneName, out FakeRoot fake);
                root = fake;
                return found;
            }
        }

        private FakeSceneLoader _loader;
        private FakeRootProvider _roots;
        private ServiceContainer _container;
        private StateMachine<AppStateId> _machine;
        private AppNavigator _navigator;

        [SetUp]
        public void SetUp()
        {
            _loader = new FakeSceneLoader();
            _roots = new FakeRootProvider();
            _roots.Roots[SceneNames.MainMenu] = new FakeRoot();
            _roots.Roots[SceneNames.Game] = new FakeRoot();
            _container = new ServiceContainer();
            _machine = new AppStateMachineFactory(_loader, _roots, _container).Create();
            _navigator = new AppNavigator(_machine, _loader);
        }

        [TearDown]
        public void TearDown() => _container.Dispose();

        [TestCase(SceneNames.Boot, AppStateId.Boot)]
        [TestCase(SceneNames.MainMenu, AppStateId.MainMenu)]
        [TestCase(SceneNames.Game, AppStateId.Gameplay)]
        [TestCase("SampleScene", AppStateId.Boot)]
        public void ResolveInitialState_DependsOnActiveScene(string activeScene, AppStateId expected)
        {
            Assert.AreEqual(expected, AppStateMachineFactory.ResolveInitialState(activeScene));
        }

        [TestCase(SceneNames.Game, false, AppStateId.MainMenu)]
        [TestCase(SceneNames.Game, true, AppStateId.Gameplay)]
        [TestCase(SceneNames.Boot, false, AppStateId.Boot)]
        [TestCase(SceneNames.MainMenu, false, AppStateId.MainMenu)]
        public void ResolveInitialState_WithoutLanguage_NeverStartsPlaying(string activeScene, bool languageChosen, AppStateId expected)
        {
            Assert.AreEqual(expected, AppStateMachineFactory.ResolveInitialState(activeScene, languageChosen));
        }

        [Test]
        public void Boot_GoesToMainMenu_LoadsSceneAndComposesRootWithAppContainer()
        {
            _machine.ChangeState(AppStateId.Boot);

            Assert.AreEqual(AppStateId.MainMenu, _machine.CurrentId);
            Assert.AreEqual(1, _loader.Requests.Count);
            Assert.AreEqual(SceneNames.MainMenu, _loader.Requests[0].Scene);
            Assert.AreEqual(0, _roots.Roots[SceneNames.MainMenu].ComposeCount, "No se compone antes de que termine la carga.");

            _loader.Complete(0);

            Assert.AreEqual(1, _roots.Roots[SceneNames.MainMenu].ComposeCount);
            Assert.AreSame(_container, _roots.Roots[SceneNames.MainMenu].ReceivedContainer);
        }

        [Test]
        public void MenuToGameplay_DisposesMenuRoot_ThenComposesGameRoot()
        {
            _machine.ChangeState(AppStateId.Boot);
            _loader.Complete(0);

            _navigator.GoToGameplay();

            Assert.AreEqual(1, _roots.Roots[SceneNames.MainMenu].DisposeCount);
            Assert.AreEqual(SceneNames.Game, _loader.Requests[1].Scene);

            _loader.Complete(1);
            Assert.AreEqual(1, _roots.Roots[SceneNames.Game].ComposeCount);
        }

        [Test]
        public void GameplayToMenu_DisposesGameRoot()
        {
            _machine.ChangeState(AppStateId.Boot);
            _loader.Complete(0);
            _navigator.GoToGameplay();
            _loader.Complete(1);
            _roots.Roots[SceneNames.MainMenu] = new FakeRoot(); // La escena recargada trae una raíz nueva.

            _navigator.GoToMainMenu();
            _loader.Complete(2);

            Assert.AreEqual(1, _roots.Roots[SceneNames.Game].DisposeCount);
            Assert.AreEqual(1, _roots.Roots[SceneNames.MainMenu].ComposeCount);
            Assert.AreEqual(AppStateId.MainMenu, _machine.CurrentId);
        }

        [Test]
        public void EnteringStateWhoseSceneIsAlreadyActive_ComposesWithoutLoading()
        {
            _loader.ActiveSceneName = SceneNames.Game;

            _machine.ChangeState(AppStateId.Gameplay);

            Assert.AreEqual(0, _loader.Requests.Count);
            Assert.AreEqual(1, _roots.Roots[SceneNames.Game].ComposeCount);
        }

        [Test]
        public void SceneAlreadyActiveButRootUsed_ReloadsScene()
        {
            _loader.ActiveSceneName = SceneNames.Game;
            _machine.ChangeState(AppStateId.Gameplay);

            _machine.ChangeState(AppStateId.Gameplay);

            Assert.AreEqual(1, _loader.Requests.Count, "La raíz ya se usó: hay que recargar la escena.");
            Assert.AreEqual(1, _roots.Roots[SceneNames.Game].DisposeCount);
        }

        [Test]
        public void LoadFinishingAfterLeavingState_IsIgnored()
        {
            _machine.ChangeState(AppStateId.MainMenu);
            _machine.ChangeState(AppStateId.Gameplay); // Sale antes de que termine la carga del menú.

            _loader.Complete(0);
            _loader.Complete(1);

            Assert.AreEqual(0, _roots.Roots[SceneNames.MainMenu].ComposeCount);
            Assert.AreEqual(1, _roots.Roots[SceneNames.Game].ComposeCount);
        }

        [Test]
        public void Navigator_IgnoresRequestsWhileLoading()
        {
            _machine.ChangeState(AppStateId.Boot); // Menú cargando.

            _navigator.GoToGameplay();

            Assert.AreEqual(AppStateId.MainMenu, _machine.CurrentId);
            Assert.AreEqual(1, _loader.Requests.Count);
        }
    }
}
