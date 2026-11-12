namespace NovaStriker.Infrastructure.Save
{
    /// <summary>
    /// Adapter de almacenamiento: guarda textos por clave. El juego no sabe si va a disco, PlayerPrefs o la nube.
    /// Claves: letras, números, '.', '-' y '_'.
    /// </summary>
    public interface ISaveStorage
    {
        bool Exists(string key);

        /// <summary>Devuelve el contenido, o null si no existe.</summary>
        string Read(string key);

        /// <summary>Escribe de forma segura: si la app se cierra a mitad, queda la versión anterior o la nueva, nunca una mezcla.</summary>
        void Write(string key, string data);

        void Delete(string key);
    }
}
