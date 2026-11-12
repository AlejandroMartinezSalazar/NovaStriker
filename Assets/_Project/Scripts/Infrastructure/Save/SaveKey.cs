using System;

namespace NovaStriker.Infrastructure.Save
{
    /// <summary>Valida las claves de guardado (se usan como nombre de archivo).</summary>
    public static class SaveKey
    {
        public static void Validate(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("La clave no puede estar vacía.", nameof(key));
            }

            foreach (char c in key)
            {
                bool valid = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                             || c == '.' || c == '-' || c == '_';
                if (!valid)
                {
                    throw new ArgumentException($"Clave de guardado no válida: '{key}'.", nameof(key));
                }
            }

            if (key.StartsWith(".", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Clave de guardado no válida: '{key}'.", nameof(key));
            }
        }
    }
}
