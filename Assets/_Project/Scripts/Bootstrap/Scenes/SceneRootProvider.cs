using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovaStriker.Bootstrap.Scenes
{
    /// <summary>
    /// Busca la raíz de composición entre los objetos raíz de la escena.
    /// Es la única búsqueda en escena permitida, y solo la usa la capa de composición.
    /// </summary>
    internal sealed class SceneRootProvider : ISceneRootProvider
    {
        public bool TryGetRoot(string sceneName, out ISceneCompositionRoot root)
        {
            root = null;
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            foreach (GameObject gameObject in scene.GetRootGameObjects())
            {
                if (gameObject.TryGetComponent(out ISceneCompositionRoot found))
                {
                    root = found;
                    return true;
                }
            }

            return false;
        }
    }
}
