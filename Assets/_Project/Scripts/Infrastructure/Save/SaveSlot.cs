using System;
using UnityEngine;

namespace NovaStriker.Infrastructure.Save
{
    /// <summary>
    /// Repositorio de un archivo de guardado tipado (clave + datos).
    /// Si no existe se usan los valores por defecto. Si está dañado se avisa en consola, se conserva una copia
    /// "clave.corrupt" para poder revisarla y se empieza con los valores por defecto (el juego no se bloquea).
    /// </summary>
    public sealed class SaveSlot<T> where T : class
    {
        private const string CorruptSuffix = ".corrupt";

        private readonly ISaveStorage _storage;
        private readonly ISerializer _serializer;
        private readonly string _key;
        private readonly Func<T> _createDefault;

        public SaveSlot(ISaveStorage storage, ISerializer serializer, string key, Func<T> createDefault)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            SaveKey.Validate(key);
            _key = key;
            _createDefault = createDefault ?? throw new ArgumentNullException(nameof(createDefault));
        }

        public string Key => _key;

        public T Load()
        {
            string raw;
            try
            {
                raw = _storage.Read(_key);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[NovaStriker] No se pudo leer '{_key}': {exception.Message}. Se usan valores por defecto.");
                return _createDefault();
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                return _createDefault();
            }

            try
            {
                T data = _serializer.Deserialize<T>(raw);
                if (data != null)
                {
                    return data;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[NovaStriker] '{_key}' está dañado ({exception.Message}).");
            }

            Debug.LogWarning($"[NovaStriker] '{_key}' no se pudo interpretar: se guarda una copia '{_key}{CorruptSuffix}' y se empieza de cero.");
            TryKeepCorruptCopy(raw);
            return _createDefault();
        }

        /// <summary>Guarda los datos. Devuelve false (y avisa) si el almacenamiento falla, sin interrumpir el juego.</summary>
        public bool Save(T data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            try
            {
                _storage.Write(_key, _serializer.Serialize(data));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[NovaStriker] No se pudo guardar '{_key}': {exception.Message}");
                return false;
            }
        }

        public void Delete() => _storage.Delete(_key);

        private void TryKeepCorruptCopy(string raw)
        {
            try
            {
                _storage.Write(_key + CorruptSuffix, raw);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[NovaStriker] No se pudo guardar la copia del archivo dañado: {exception.Message}");
            }
        }
    }
}
