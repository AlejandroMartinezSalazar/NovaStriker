namespace NovaStriker.Core.Pooling
{
    /// <summary>
    /// Componente que necesita reiniciarse al salir del pool o al volver a él.
    /// Ejemplo: el balón pone su velocidad en cero y limpia su trail.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Se llama al sacar el objeto del pool, ya activo y en su posición.</summary>
        void OnSpawned();

        /// <summary>Se llama al devolver el objeto, antes de desactivarlo.</summary>
        void OnDespawned();
    }
}
