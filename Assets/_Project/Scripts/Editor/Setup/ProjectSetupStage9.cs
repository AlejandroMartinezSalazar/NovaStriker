using System.Linq;
using System.Text;
using NovaStriker.Bootstrap;
using NovaStriker.Contracts.Data;
using NovaStriker.EditorTools.Setup.Ui;
using NovaStriker.Infrastructure.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Etapa 9: audio. Configura la importación de música y efectos, crea SO_SoundLibrary, la asigna a PF_AppRoot
    /// y añade los ajustes de sonido al menú. Idempotente: la ficha solo se rellena si se crea o si una entrada
    /// quedó sin clips (se conservan tus cambios de volumen y tus sonidos).
    /// </summary>
    internal static class ProjectSetupStage9
    {
        private const string Root = ProjectSetupStage0.ProjectRoot;
        private const string SfxFolder = Root + "/Audio/SFX/";
        private const string MusicFolder = Root + "/Audio/Music/";
        private const string AudioSettingsFolder = Root + "/Settings/Audio";
        private const string LibraryPath = AudioSettingsFolder + "/SO_SoundLibrary.asset";
        private const string AppRootPrefabPath = Root + "/Resources/" + AppBootstrapper.AppRootResourcePath + ".prefab";

        private sealed class SfxSpec
        {
            public SfxId Id;
            public string[] Files;
            public float Volume;
            public float Pitch = 0.06f;
            public float Interval = 0.05f;
        }

        private static readonly SfxSpec[] Effects =
        {
            new SfxSpec { Id = SfxId.Aim, Files = new[] { "SFX_Aim" }, Volume = 0.35f, Interval = 0.2f },
            new SfxSpec { Id = SfxId.Kick, Files = new[] { "SFX_Kick" }, Volume = 0.9f },
            new SfxSpec { Id = SfxId.BallGround, Files = new[] { "SFX_BallGround" }, Volume = 0.6f, Interval = 0.08f },
            new SfxSpec { Id = SfxId.BallBox, Files = new[] { "SFX_BallBox" }, Volume = 0.8f },
            new SfxSpec { Id = SfxId.BallWall, Files = new[] { "SFX_BallWall" }, Volume = 0.75f },
            new SfxSpec { Id = SfxId.BallBody, Files = new[] { "SFX_BallBody" }, Volume = 0.85f },
            new SfxSpec { Id = SfxId.Knockdown, Files = new[] { "SFX_Knockdown_1", "SFX_Knockdown_2", "SFX_Knockdown_3" }, Volume = 0.8f, Pitch = 0.1f, Interval = 0.12f },
            new SfxSpec { Id = SfxId.GlassCrack, Files = new[] { "SFX_GlassCrack" }, Volume = 0.8f },
            new SfxSpec { Id = SfxId.GlassBreak, Files = new[] { "SFX_GlassBreak" }, Volume = 0.9f },
            new SfxSpec { Id = SfxId.BoxImpact, Files = new[] { "SFX_BoxImpact" }, Volume = 0.6f, Interval = 0.08f },
            new SfxSpec { Id = SfxId.Victory, Files = new[] { "SFX_Victory" }, Volume = 0.9f, Pitch = 0f, Interval = 0.5f },
            new SfxSpec { Id = SfxId.Defeat, Files = new[] { "SFX_Defeat" }, Volume = 0.9f, Pitch = 0f, Interval = 0.5f },
            new SfxSpec { Id = SfxId.Click, Files = new[] { "SFX_Click" }, Volume = 0.6f, Pitch = 0.04f, Interval = 0.03f },
            new SfxSpec { Id = SfxId.Whoosh, Files = new[] { "SFX_Whoosh" }, Volume = 0.5f, Interval = 0.1f },
        };

        private static readonly (MusicId Id, string File, float Volume)[] Music =
        {
            (MusicId.Menu, "MUS_Menu", 0.5f),
            (MusicId.Gameplay, "MUS_Gameplay", 0.45f),
        };

        private const string AmbienceFile = "AMB_Crowd";

        [MenuItem("NovaStriker/Setup/Etapa 9 - Audio")]
        private static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[NovaStriker Setup] Cancelado: hay escenas sin guardar.");
                return;
            }

            var log = new StringBuilder("[NovaStriker Setup] Etapa 9\n");
            bool ok = ConfigureImporters(log)
                      && CreateOrFillLibrary(log)
                      && AssignLibraryToAppRoot(log)
                      && UiKit.PrepareAssets(log)
                      && MainMenuSceneBuilder.Setup(log);

            if (ok)
            {
                Debug.Log(log.ToString());
            }
            else
            {
                Debug.LogError(log.ToString());
            }
        }

        // ---------- Importación ----------

        private static bool ConfigureImporters(StringBuilder log)
        {
            bool ok = true;
            foreach (string file in Effects.SelectMany(e => e.Files))
            {
                ok &= ConfigureClip(SfxFolder + file + ".wav", streaming: false, log);
            }

            foreach (var music in Music)
            {
                ok &= ConfigureClip(MusicFolder + music.File + ".wav", streaming: true, log);
            }

            ok &= ConfigureClip(MusicFolder + AmbienceFile + ".wav", streaming: true, log);
            return ok;
        }

        private static bool ConfigureClip(string path, bool streaming, StringBuilder log)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                log.AppendLine($"- ERROR: falta {path}.");
                return false;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            AudioClipLoadType loadType = streaming ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            AudioCompressionFormat format = streaming ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            if (importer.forceToMono && settings.loadType == loadType && settings.compressionFormat == format)
            {
                return true;
            }

            importer.forceToMono = true;
            importer.loadInBackground = streaming;
            settings.loadType = loadType;
            settings.compressionFormat = format;
            settings.quality = 0.5f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            log.AppendLine($"- Audio: {System.IO.Path.GetFileName(path)} ({(streaming ? "streaming, Vorbis" : "en memoria, ADPCM")}).");
            return true;
        }

        // ---------- Ficha de sonidos ----------

        private static bool CreateOrFillLibrary(StringBuilder log)
        {
            if (!AssetDatabase.IsValidFolder(AudioSettingsFolder))
            {
                AssetDatabase.CreateFolder(Root + "/Settings", "Audio");
            }

            bool existed = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath) != null;
            var library = ProjectSetupStage3.GetOrCreateAsset<SoundLibrary>(LibraryPath, log);

            bool needsContent = !existed || Effects.Any(e => !HasClips(library.GetSfx(e.Id)))
                                         || Music.Any(m => library.GetMusic(m.Id)?.Clip == null);
            if (!needsContent)
            {
                log.AppendLine("- SO_SoundLibrary ya estaba completa, no se modifica.");
                return true;
            }

            var effects = Effects.Select(spec => KeepOrCreate(library.GetSfx(spec.Id), spec)).ToArray();
            var music = Music.Select(m => KeepOrCreate(library.GetMusic(m.Id), m.Id, m.File, m.Volume)).ToArray();
            AudioClip ambience = library.Ambience != null ? library.Ambience : LoadClip(MusicFolder + AmbienceFile);

            if (effects.Any(e => !HasClips(e)) || music.Any(m => m.Clip == null) || ambience == null)
            {
                log.AppendLine("- ERROR: no se pudieron cargar todos los clips de Audio/SFX y Audio/Music.");
                return false;
            }

            library.SetContent(effects, music, ambience);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            log.AppendLine($"- SO_SoundLibrary: {effects.Length} efectos, {music.Length} músicas y ambiente.");
            return true;
        }

        private static bool HasClips(SfxEntry entry) =>
            entry != null && entry.Clips != null && entry.Clips.Length > 0 && entry.Clips.All(c => c != null);

        private static SfxEntry KeepOrCreate(SfxEntry existing, SfxSpec spec)
        {
            if (HasClips(existing))
            {
                return existing;
            }

            return new SfxEntry
            {
                Id = spec.Id,
                Clips = spec.Files.Select(f => LoadClip(SfxFolder + f)).ToArray(),
                Volume = spec.Volume,
                PitchJitter = spec.Pitch,
                VolumeJitter = 0.1f,
                MinInterval = spec.Interval,
            };
        }

        private static MusicEntry KeepOrCreate(MusicEntry existing, MusicId id, string file, float volume)
        {
            if (existing != null && existing.Clip != null)
            {
                return existing;
            }

            return new MusicEntry { Id = id, Clip = LoadClip(MusicFolder + file), Volume = volume };
        }

        private static AudioClip LoadClip(string pathWithoutExtension) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>(pathWithoutExtension + ".wav");

        // ---------- PF_AppRoot ----------

        private static bool AssignLibraryToAppRoot(StringBuilder log)
        {
            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
            GameObject contents = PrefabUtility.LoadPrefabContents(AppRootPrefabPath);
            try
            {
                var app = contents.GetComponent<AppCompositionRoot>();
                if (app == null)
                {
                    log.AppendLine($"- ERROR: {AppRootPrefabPath} no tiene AppCompositionRoot. Ejecuta la Etapa 2.");
                    return false;
                }

                ProjectSetupStage2.AssignReference(app, "_soundLibrary", library);
                PrefabUtility.SaveAsPrefabAsset(contents, AppRootPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            var saved = AssetDatabase.LoadAssetAtPath<AppCompositionRoot>(AppRootPrefabPath);
            bool ok = ProjectSetupStage3.VerifyNoMissingReferences(saved, log);
            if (ok)
            {
                log.AppendLine("- PF_AppRoot: SO_SoundLibrary asignada.");
            }

            return ok;
        }
    }
}
