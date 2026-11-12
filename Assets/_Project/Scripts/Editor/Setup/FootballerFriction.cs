using System.Text;
using UnityEditor;
using UnityEngine;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Material físico de la cápsula del futbolista: fricción alta para que no resbale sobre las cajas por
    /// vibraciones pequeñas. Un golpe de verdad (balón, caja) lo sigue derribando: eso lo decide KnockdownDetector.
    /// Lo usa la Etapa 4 al regenerar PF_Footballer, y el menú de abajo lo aplica al prefab ya existente.
    /// </summary>
    internal static class FootballerFriction
    {
        internal const string MaterialPath = ProjectSetupStage0.ProjectRoot + "/PhysicsMaterials/PM_Footballer.asset";
        private const float Friction = 1f;

        [MenuItem("NovaStriker/Setup/Ajuste - Fricción del futbolista")]
        private static void Run()
        {
            var log = new StringBuilder("[NovaStriker Setup] Fricción del futbolista\n");
            PhysicsMaterial material = GetOrCreateMaterial(log);

            GameObject contents = PrefabUtility.LoadPrefabContents(ProjectSetupStage4.PrefabPath);
            try
            {
                var capsule = contents.GetComponent<CapsuleCollider>();
                if (capsule == null)
                {
                    Debug.LogError($"{log}- ERROR: {ProjectSetupStage4.PrefabPath} no tiene CapsuleCollider en la raíz.");
                    return;
                }

                capsule.sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(contents, ProjectSetupStage4.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            log.AppendLine($"- {ProjectSetupStage4.PrefabPath}: cápsula con {MaterialPath}.");
            Debug.Log(log.ToString());
        }

        internal static PhysicsMaterial GetOrCreateMaterial(StringBuilder log)
        {
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(MaterialPath);
            if (existing != null)
            {
                return existing;
            }

            var material = new PhysicsMaterial("PM_Footballer")
            {
                bounciness = 0f,
                dynamicFriction = Friction,
                staticFriction = Friction,
                bounceCombine = PhysicsMaterialCombine.Minimum,
                // Maximum: contra una caja (0,5) gana la fricción del futbolista.
                frictionCombine = PhysicsMaterialCombine.Maximum,
            };
            AssetDatabase.CreateAsset(material, MaterialPath);
            AssetDatabase.SaveAssets();
            log.AppendLine($"- Material físico: {MaterialPath}");
            return material;
        }
    }
}
