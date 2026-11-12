namespace NovaStriker.Core.Pooling
{
    /// <summary>
    /// Pool de objetos reutilizables. Se llama IPool (no IObjectPool) para no chocar con UnityEngine.Pool.IObjectPool.
    /// </summary>
    public interface IPool<T> where T : class
    {
        int CountActive { get; }
        int CountInactive { get; }

        T Get();

        /// <summary>Devuelve un objeto al pool. Devolverlo dos veces es un error.</summary>
        void Release(T item);

        /// <summary>Devuelve todos los objetos activos (p. ej. al reiniciar el nivel).</summary>
        void ReleaseAll();

        /// <summary>Crea objetos por adelantado hasta tener <paramref name="count"/> inactivos.</summary>
        void Prewarm(int count);

        /// <summary>Devuelve los activos y destruye todos los objetos del pool.</summary>
        void Clear();
    }
}
