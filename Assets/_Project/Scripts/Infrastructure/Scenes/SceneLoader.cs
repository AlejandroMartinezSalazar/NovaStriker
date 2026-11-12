using System;
using NovaStriker.Contracts.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovaStriker.Infrastructure.Scenes
{
    /// <summary>Implementación de ISceneLoader sobre SceneManager.LoadSceneAsync (modo Single).</summary>
    public sealed class SceneLoader : ISceneLoader
    {
        private int _loadsInProgress;

        public bool IsLoading => _loadsInProgress > 0;

        public string ActiveSceneName => SceneManager.GetActiveScene().name;

        public void LoadScene(string sceneName, Action onLoaded)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                throw new ArgumentException("El nombre de la escena está vacío.", nameof(sceneName));
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                throw new InvalidOperationException(
                    $"[SceneLoader] La escena '{sceneName}' no está en Build Settings.");
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                throw new InvalidOperationException($"[SceneLoader] Unity no pudo iniciar la carga de '{sceneName}'.");
            }

            _loadsInProgress++;
            operation.completed += _ =>
            {
                _loadsInProgress--;
                onLoaded?.Invoke();
            };
        }
    }
}
