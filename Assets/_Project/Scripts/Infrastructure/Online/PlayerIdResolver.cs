using System;
using NovaStriker.Infrastructure.Save;

namespace NovaStriker.Infrastructure.Online
{
    /// <summary>
    /// Elige el id con el que el dispositivo inicia sesión (sin contraseña) y lo guarda para usar siempre el mismo:
    /// 1) el ya guardado; 2) el id del dispositivo (en Android se mantiene al reinstalar, así no se cuenta dos veces
    /// al mismo jugador); 3) uno aleatorio si el dispositivo no da id.
    /// </summary>
    public static class PlayerIdResolver
    {
        internal const string StorageKey = "player_id";
        internal const int MaxLength = 100;

        /// <param name="deviceId">SystemInfo.deviceUniqueIdentifier.</param>
        /// <param name="unsupportedId">SystemInfo.unsupportedIdentifier: lo que devuelve Unity si no hay id.</param>
        public static string Resolve(string deviceId, string unsupportedId, ISaveStorage storage, Func<string> newId)
        {
            string stored = TryRead(storage);
            if (IsUsable(stored, unsupportedId))
            {
                return stored.Trim();
            }

            string id = IsUsable(deviceId, unsupportedId) ? deviceId.Trim() : newId();
            TryWrite(storage, id);
            return id;
        }

        internal static bool IsUsable(string id, string unsupportedId)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            string trimmed = id.Trim();
            return trimmed != unsupportedId && trimmed.Length <= MaxLength && trimmed.Trim('0', '-') != string.Empty;
        }

        // Guardar el id es una mejora, no un requisito: si el disco falla, se juega igual.
        private static string TryRead(ISaveStorage storage)
        {
            try
            {
                return storage?.Read(StorageKey);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void TryWrite(ISaveStorage storage, string id)
        {
            try
            {
                storage?.Write(StorageKey, id);
            }
            catch (Exception)
            {
                // Sin guardar: la próxima vez se vuelve a resolver (el id del dispositivo suele ser el mismo).
            }
        }
    }
}
