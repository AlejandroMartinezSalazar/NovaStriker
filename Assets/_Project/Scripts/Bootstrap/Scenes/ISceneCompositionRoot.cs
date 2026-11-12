using System;
using NovaStriker.Core.DI;

namespace NovaStriker.Bootstrap.Scenes
{
    /// <summary>
    /// Raíz de composición de una escena. La app la compone después de cargar la escena
    /// y la libera (Dispose) antes de salir del estado que la cargó.
    /// </summary>
    public interface ISceneCompositionRoot : IDisposable
    {
        /// <summary>True solo antes de la primera composición: una raíz no se compone dos veces.</summary>
        bool CanCompose { get; }

        void Compose(IServiceContainer appContainer);
    }
}
