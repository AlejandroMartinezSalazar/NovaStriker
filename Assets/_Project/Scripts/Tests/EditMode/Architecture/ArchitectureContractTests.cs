using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Architecture
{
    /// <summary>
    /// Prueba de contrato de la arquitectura: comprueba que cada módulo (asmdef) solo depende de los que le permite
    /// el diseño (Docs/ARCHITECTURE.md, apartado 3, y el manual de buenas prácticas).
    /// Lo comprueba dos veces:
    ///  1. Lo DECLARADO: las referencias escritas en cada archivo .asmdef.
    ///  2. Lo COMPILADO: las referencias reales del ensamblado que Unity generó (lo que el código usa de verdad).
    /// Si alguien añade un módulo nuevo, la prueba falla hasta que se le asignen reglas aquí: así ninguna pieza
    /// entra en el proyecto sin decidir a qué capa pertenece.
    /// </summary>
    public class ArchitectureContractTests
    {
        private const string Prefix = "NovaStriker.";
        private const string Core = "NovaStriker.Core";
        private const string Contracts = "NovaStriker.Contracts";
        private const string Gameplay = "NovaStriker.Gameplay";
        private const string UI = "NovaStriker.UI";
        private const string Infrastructure = "NovaStriker.Infrastructure";
        private const string PlayFab = "NovaStriker.Infrastructure.PlayFab";
        private const string Bootstrap = "NovaStriker.Bootstrap";
        private const string EditorTools = "NovaStriker.EditorTools";
        private const string TestsEditMode = "NovaStriker.Tests.EditMode";
        private const string TestsSupport = "NovaStriker.Tests.Support";

        /// <summary>Módulos del juego y a qué otros módulos del proyecto pueden referenciar. Las dependencias solo bajan.</summary>
        private static readonly Dictionary<string, string[]> RuntimeRules = new Dictionary<string, string[]>
        {
            [Core] = new string[0],
            [Contracts] = new[] { Core },
            [Gameplay] = new[] { Core, Contracts },
            [UI] = new[] { Core, Contracts },
            [Infrastructure] = new[] { Core, Contracts },
            [PlayFab] = new[] { Core, Contracts, Infrastructure },
            [Bootstrap] = new[] { Core, Contracts, Gameplay, UI, Infrastructure, PlayFab },
        };

        /// <summary>Módulos que solo existen en el Editor o en las pruebas; pueden usar a todos, nadie del juego a ellos.</summary>
        private static readonly string[] ToolModules = { EditorTools, TestsEditMode, TestsSupport };

        public static IEnumerable<string> RuntimeModules => RuntimeRules.Keys;

        // ---------- 1. Lo declarado en los .asmdef ----------

        [Test]
        public void EveryProjectModule_HasArchitectureRules()
        {
            string[] unknown = LoadProjectAsmdefs().Keys
                .Where(name => !RuntimeRules.ContainsKey(name) && !ToolModules.Contains(name))
                .ToArray();

            Assert.IsEmpty(unknown, "Módulos sin reglas de arquitectura (añádelos a RuntimeRules o ToolModules): " + string.Join(", ", unknown));
        }

        [TestCaseSource(nameof(RuntimeModules))]
        public void DeclaredReferences_OnlyGoDown(string module)
        {
            Dictionary<string, AsmdefFile> asmdefs = LoadProjectAsmdefs();
            Assert.IsTrue(asmdefs.ContainsKey(module), $"No se encontró el asmdef de {module}.");

            string[] forbidden = ProjectReferences(asmdefs[module], asmdefs)
                .Where(reference => !RuntimeRules[module].Contains(reference))
                .ToArray();

            Assert.IsEmpty(forbidden, $"{module} declara dependencias no permitidas: {string.Join(", ", forbidden)}");
        }

        [TestCase(EditorTools)]
        [TestCase(TestsEditMode)]
        public void ToolModules_AreEditorOnly(string module)
        {
            AsmdefFile asmdef = LoadProjectAsmdefs()[module];
            CollectionAssert.AreEqual(new[] { "Editor" }, asmdef.includePlatforms ?? new string[0],
                $"{module} debe compilarse solo en el Editor, nunca en el juego publicado.");
        }

        [Test]
        public void TestSupport_IsOnlyCompiledForTests()
        {
            AsmdefFile asmdef = LoadProjectAsmdefs()[TestsSupport];
            CollectionAssert.Contains(asmdef.defineConstraints ?? new string[0], "UNITY_INCLUDE_TESTS");
        }

        [Test]
        public void PlayFabModule_IsOptional()
        {
            AsmdefFile asmdef = LoadProjectAsmdefs()[PlayFab];
            CollectionAssert.Contains(asmdef.defineConstraints ?? new string[0], "NOVA_PLAYFAB",
                "Sin esta condición el proyecto no compilaría sin el SDK de PlayFab.");
        }

        // ---------- 2. Lo que el código usa de verdad (ensamblados compilados) ----------

        [TestCaseSource(nameof(RuntimeModules))]
        public void CompiledReferences_OnlyGoDown(string module)
        {
            Assembly assembly = FindLoadedAssembly(module);
            if (assembly == null)
            {
                Assert.AreEqual(PlayFab, module, $"{module} no está compilado.");
                Assert.Pass("El módulo de PlayFab no está compilado (SDK sin instalar): no hay nada que comprobar.");
            }

            string[] forbidden = assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal) && !RuntimeRules[module].Contains(name))
                .ToArray();

            Assert.IsEmpty(forbidden, $"El código de {module} usa módulos no permitidos: {string.Join(", ", forbidden)}");
        }

        [TestCaseSource(nameof(RuntimeModules))]
        public void GameCode_NeverUsesTheEditor(string module)
        {
            Assembly assembly = FindLoadedAssembly(module);
            if (assembly == null)
            {
                Assert.AreEqual(PlayFab, module, $"{module} no está compilado.");
                Assert.Pass("El módulo de PlayFab no está compilado (SDK sin instalar): no hay nada que comprobar.");
            }

            string[] editorReferences = assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .Where(name => name.StartsWith("UnityEditor", StringComparison.Ordinal))
                .ToArray();

            Assert.IsEmpty(editorReferences, $"{module} usa APIs del Editor ({string.Join(", ", editorReferences)}); no compilaría en Android.");
        }

        // ---------- Utilidades ----------

        [Serializable]
        private sealed class AsmdefFile
        {
            public string name;
            public string[] references;
            public string[] includePlatforms;
            public string[] defineConstraints;
        }

        private static string ScriptsFolder => Path.Combine(Application.dataPath, "_Project", "Scripts");

        /// <summary>Todos los asmdef del proyecto (Assets/_Project/Scripts), por nombre.</summary>
        private static Dictionary<string, AsmdefFile> LoadProjectAsmdefs()
        {
            var result = new Dictionary<string, AsmdefFile>();
            foreach (string path in Directory.GetFiles(ScriptsFolder, "*.asmdef", SearchOption.AllDirectories))
            {
                AsmdefFile asmdef = JsonUtility.FromJson<AsmdefFile>(File.ReadAllText(path));
                Assert.IsFalse(string.IsNullOrEmpty(asmdef?.name), $"No se pudo leer {path}.");
                result[asmdef.name] = asmdef;
            }

            return result;
        }

        /// <summary>Referencias a módulos del proyecto. Admite referencias por nombre y por GUID ("GUID:xxxx").</summary>
        private static IEnumerable<string> ProjectReferences(AsmdefFile asmdef, Dictionary<string, AsmdefFile> projectAsmdefs)
        {
            Dictionary<string, string> namesByGuid = AsmdefNamesByGuid();
            foreach (string reference in asmdef.references ?? new string[0])
            {
                string name = reference;
                if (reference.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase))
                {
                    namesByGuid.TryGetValue(reference.Substring(5).ToLowerInvariant(), out name);
                }

                if (name != null && projectAsmdefs.ContainsKey(name))
                {
                    yield return name;
                }
            }
        }

        private static Dictionary<string, string> AsmdefNamesByGuid()
        {
            var result = new Dictionary<string, string>();
            foreach (string path in Directory.GetFiles(ScriptsFolder, "*.asmdef", SearchOption.AllDirectories))
            {
                string meta = path + ".meta";
                if (!File.Exists(meta))
                {
                    continue;
                }

                string guidLine = File.ReadAllLines(meta).FirstOrDefault(line => line.StartsWith("guid:", StringComparison.Ordinal));
                if (guidLine != null)
                {
                    result[guidLine.Substring(5).Trim().ToLowerInvariant()] = JsonUtility.FromJson<AsmdefFile>(File.ReadAllText(path)).name;
                }
            }

            return result;
        }

        private static Assembly FindLoadedAssembly(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == name);
        }
    }
}
