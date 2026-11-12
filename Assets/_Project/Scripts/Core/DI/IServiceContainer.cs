using System;

namespace NovaStriker.Core.DI
{
    /// <summary>
    /// Registro explícito de servicios. Solo las composition roots lo usan:
    /// el resto del código recibe sus dependencias por constructor o por Construct(...).
    /// </summary>
    public interface IServiceContainer : IDisposable
    {
        /// <summary>Registra una instancia ya creada. Si <paramref name="disposeWithContainer"/> es true, el contenedor la libera al liberarse.</summary>
        void RegisterInstance<T>(T instance, bool disposeWithContainer = false) where T : class;

        /// <summary>Registra una fábrica perezosa: se invoca una sola vez, en el primer Resolve.</summary>
        void RegisterLazy<T>(Func<IServiceContainer, T> factory, bool disposeWithContainer = false) where T : class;

        /// <summary>Indica si el tipo está registrado en este contenedor o en sus padres.</summary>
        bool IsRegistered<T>() where T : class;

        /// <summary>Devuelve el servicio o lanza InvalidOperationException si no existe.</summary>
        T Resolve<T>() where T : class;

        bool TryResolve<T>(out T instance) where T : class;

        /// <summary>Crea un contenedor hijo (p. ej. por escena) que busca en este si no encuentra un tipo.</summary>
        IServiceContainer CreateChild();
    }
}
