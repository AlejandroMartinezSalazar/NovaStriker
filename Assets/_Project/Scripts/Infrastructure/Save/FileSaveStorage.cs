using System;
using System.IO;
using System.Text;

namespace NovaStriker.Infrastructure.Save
{
    /// <summary>
    /// ISaveStorage sobre archivos (en el juego: Application.persistentDataPath/Save).
    /// Escritura atómica: se escribe "clave.tmp", la versión anterior pasa a "clave.bak" y el temporal ocupa su lugar.
    /// Si al leer falta el archivo principal pero existe el .bak (cierre en mitad del cambio), se usa el .bak.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private const string TempSuffix = ".tmp";
        private const string BackupSuffix = ".bak";
        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        private readonly string _directory;

        public FileSaveStorage(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("Falta la carpeta de guardado.", nameof(directory));
            }

            _directory = directory;
        }

        public string Directory => _directory;

        public bool Exists(string key) => File.Exists(PathOf(key)) || File.Exists(PathOf(key) + BackupSuffix);

        public string Read(string key)
        {
            string path = PathOf(key);
            if (File.Exists(path))
            {
                return File.ReadAllText(path, Utf8);
            }

            string backup = path + BackupSuffix;
            return File.Exists(backup) ? File.ReadAllText(backup, Utf8) : null;
        }

        public void Write(string key, string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string path = PathOf(key);
            string temp = path + TempSuffix;
            string backup = path + BackupSuffix;

            System.IO.Directory.CreateDirectory(_directory);
            File.WriteAllText(temp, data, Utf8);

            if (File.Exists(backup))
            {
                File.Delete(backup);
            }

            if (File.Exists(path))
            {
                File.Move(path, backup);
            }

            File.Move(temp, path);

            if (File.Exists(backup))
            {
                File.Delete(backup);
            }
        }

        public void Delete(string key)
        {
            string path = PathOf(key);
            DeleteIfExists(path);
            DeleteIfExists(path + TempSuffix);
            DeleteIfExists(path + BackupSuffix);
        }

        private string PathOf(string key)
        {
            SaveKey.Validate(key);
            return Path.Combine(_directory, key);
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
