using System;

namespace NovaStriker.Contracts.Services
{
    /// <summary>Carga de escenas desacoplada de SceneManager, para poder probar el flujo de la app sin Unity.</summary>
    public interface ISceneLoader
    {
        /// <summary>True mientras haya al menos una carga en curso.</summary>
        bool IsLoading { get; }

        string ActiveSceneName { get; }

        /// <summary>Carga la escena en modo Single (descarga la anterior) y llama a <paramref name="onLoaded"/> al terminar.</summary>
        void LoadScene(string sceneName, Action onLoaded);
    }
}
