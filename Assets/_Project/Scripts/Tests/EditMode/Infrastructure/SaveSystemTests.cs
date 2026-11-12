using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NovaStriker.Infrastructure.Progress;
using NovaStriker.Infrastructure.Save;
using NovaStriker.Infrastructure.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NovaStriker.Tests.EditMode.Infrastructure
{
    public class SaveSystemTests
    {
        /// <summary>Almacenamiento en memoria que puede simular fallos de escritura.</summary>
        private sealed class MemoryStorage : ISaveStorage
        {
            public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
            public bool FailWrites;
            public int WriteCount;

            public bool Exists(string key) => Files.ContainsKey(key);
            public string Read(string key) => Files.TryGetValue(key, out string value) ? value : null;

            public void Write(string key, string data)
            {
                if (FailWrites)
                {
                    throw new IOException("Disco lleno");
                }

                WriteCount++;
                Files[key] = data;
            }

            public void Delete(string key) => Files.Remove(key);
        }

        private string _directory;
        private readonly ISerializer _serializer = new JsonUtilitySerializer();

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "NovaStrikerTests_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        // ---------- FileSaveStorage ----------

        [Test]
        public void File_WriteReadAndOverwrite_LeavesNoTemporaryFiles()
        {
            var storage = new FileSaveStorage(_directory);
            Assert.IsFalse(storage.Exists("a.json"));
            Assert.IsNull(storage.Read("a.json"));

            storage.Write("a.json", "uno");
            storage.Write("a.json", "dos");

            Assert.IsTrue(storage.Exists("a.json"));
            Assert.AreEqual("dos", storage.Read("a.json"));
            CollectionAssert.AreEqual(new[] { Path.Combine(_directory, "a.json") }, Directory.GetFiles(_directory));
        }

        [Test]
        public void File_RecoversFromBackup_WhenClosedMidReplace()
        {
            var storage = new FileSaveStorage(_directory);
            storage.Write("a.json", "anterior");
            // Simula un cierre justo después de mover el archivo a .bak y antes de colocar el nuevo.
            File.Move(Path.Combine(_directory, "a.json"), Path.Combine(_directory, "a.json.bak"));

            Assert.IsTrue(storage.Exists("a.json"));
            Assert.AreEqual("anterior", storage.Read("a.json"));

            storage.Write("a.json", "nuevo");
            Assert.AreEqual("nuevo", storage.Read("a.json"));
            Assert.IsFalse(File.Exists(Path.Combine(_directory, "a.json.bak")));
        }

        [Test]
        public void File_DeleteRemovesEverything_AndKeysAreValidated()
        {
            var storage = new FileSaveStorage(_directory);
            storage.Write("a.json", "x");
            storage.Delete("a.json");
            Assert.IsFalse(storage.Exists("a.json"));

            Assert.Throws<ArgumentException>(() => storage.Write("../fuera.json", "x"));
            Assert.Throws<ArgumentException>(() => storage.Read("sub/a.json"));
            Assert.Throws<ArgumentException>(() => storage.Read(""));
        }

        // ---------- SaveSlot ----------

        [Test]
        public void Slot_MissingFile_ReturnsDefaults()
        {
            var slot = new SaveSlot<ProgressData>(new MemoryStorage(), _serializer, "p.json", () => new ProgressData());
            ProgressData data = slot.Load();

            Assert.AreEqual(0, data.CurrentLevelIndex);
            Assert.AreEqual(ProgressData.CurrentVersion, data.SaveVersion);
        }

        [Test]
        public void Slot_CorruptFile_KeepsACopy_AndReturnsDefaults()
        {
            var storage = new MemoryStorage();
            storage.Files["p.json"] = "{ esto no es json";
            var slot = new SaveSlot<ProgressData>(storage, _serializer, "p.json", () => new ProgressData());

            ProgressData data = slot.Load();

            Assert.AreEqual(0, data.CurrentLevelIndex);
            Assert.AreEqual("{ esto no es json", storage.Files["p.json.corrupt"]);
        }

        [Test]
        public void Slot_WriteFailure_DoesNotThrow()
        {
            var storage = new MemoryStorage { FailWrites = true };
            var slot = new SaveSlot<ProgressData>(storage, _serializer, "p.json", () => new ProgressData());
            LogAssert.Expect(LogType.Error, new Regex("No se pudo guardar 'p.json'"));

            Assert.IsFalse(slot.Save(new ProgressData { CurrentLevelIndex = 3 }));
        }

        // ---------- Progreso ----------

        [Test]
        public void Progress_SurvivesRestart_OnRealFiles()
        {
            var storage = new FileSaveStorage(_directory);
            var first = new PersistentProgressService(storage, _serializer);
            first.CompleteLevel(0);
            first.CompleteLevel(1);

            var afterRestart = new PersistentProgressService(new FileSaveStorage(_directory), _serializer);

            Assert.AreEqual(2, afterRestart.CurrentLevelIndex);
        }

        [Test]
        public void Progress_OnlyAdvances_ButCountsEveryVictory()
        {
            var storage = new MemoryStorage();
            var progress = new PersistentProgressService(storage, _serializer);
            progress.CompleteLevel(2);
            progress.CompleteLevel(0);

            Assert.AreEqual(3, progress.CurrentLevelIndex, "Repetir un nivel anterior no hace retroceder.");
            Assert.AreEqual(2, progress.TotalVictories);
            Assert.AreEqual(2, new PersistentProgressService(storage, _serializer).TotalVictories, "Las victorias se guardan.");
        }

        [Test]
        public void Progress_Version1File_LoadsWithZeroVictories()
        {
            var storage = new MemoryStorage();
            storage.Files[PersistentProgressService.SaveKeyName] = "{\"SaveVersion\":1,\"CurrentLevelIndex\":2}";

            var progress = new PersistentProgressService(storage, _serializer);

            Assert.AreEqual(2, progress.CurrentLevelIndex);
            Assert.AreEqual(0, progress.TotalVictories);
        }

        [Test]
        public void Progress_RestartCampaign_IsSaved()
        {
            var storage = new MemoryStorage();
            var progress = new PersistentProgressService(storage, _serializer);
            progress.CompleteLevel(4);
            progress.RestartCampaign();

            Assert.AreEqual(0, new PersistentProgressService(storage, _serializer).CurrentLevelIndex);
        }

        [Test]
        public void Progress_NegativeIndexInFile_IsSanitized()
        {
            var storage = new MemoryStorage();
            storage.Files[PersistentProgressService.SaveKeyName] = _serializer.Serialize(new ProgressData { CurrentLevelIndex = -5 });

            Assert.AreEqual(0, new PersistentProgressService(storage, _serializer).CurrentLevelIndex);
        }

        // ---------- Ajustes ----------

        [Test]
        public void Settings_DefaultsClampPersistAndNotify()
        {
            var storage = new MemoryStorage();
            var settings = new PersistentGameSettings(storage, _serializer);
            Assert.AreEqual(SettingsData.DefaultMusicVolume, settings.MusicVolume, 1e-5f);
            Assert.AreEqual(SettingsData.DefaultSfxVolume, settings.SfxVolume, 1e-5f);

            int changes = 0;
            settings.Changed += () => changes++;
            settings.SetMusicVolume(1.7f);
            settings.SetSfxVolume(-1f);
            settings.SetSfxVolume(0f); // Sin cambio: no avisa.

            Assert.AreEqual(2, changes);
            Assert.AreEqual(0, storage.WriteCount, "Arrastrar una barra no escribe en disco.");
            settings.Save();
            settings.Save(); // Sin cambios nuevos: no vuelve a escribir.
            Assert.AreEqual(1, storage.WriteCount);
            var reloaded = new PersistentGameSettings(storage, _serializer);
            Assert.AreEqual(1f, reloaded.MusicVolume, 1e-5f);
            Assert.AreEqual(0f, reloaded.SfxVolume, 1e-5f);
        }
    }
}
