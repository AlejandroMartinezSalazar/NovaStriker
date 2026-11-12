using System.Collections.Generic;
using System.Text;
using NovaStriker.Contracts.Data;
using NovaStriker.Gameplay.Obstacles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Genera PF_GlassCrate: caja de vidrio rompible que se desplaza por una ruta editable, con cajas (PF_Box) dentro.
    /// Jerarquía:
    ///   PF_GlassCrate (raíz quieta, origen en la base)
    ///   └─ Crate     Rigidbody cinemático + PathMover + GlassCrate + 6 paneles (capa Obstacle)
    ///      └─ Contents  3 PF_Box en pirámide (2 abajo, 1 encima): viajan fijas dentro y caen al romperse
    /// La ruta se edita por instancia como en muros y plataformas móviles (PathMover, en el objeto Crate).
    /// Los paneles van en la capa Obstacle: Breakable no choca con Environment.
    /// Si el prefab ya existe, se actualiza en su sitio (conserva su GUID, la ruta y los ajustes de GlassCrate):
    /// se recolocan los paneles y se rehace la pirámide de cajas si no coincide con la de aquí.
    /// </summary>
    internal static class GlassCrateBuilder
    {
        internal const string PrefabPath = ProjectSetupStage0.ProjectRoot + "/Prefabs/Obstacles/PF_GlassCrate.prefab";
        private const string GlassMaterialPath = ProjectSetupStage0.ProjectRoot + "/Art/Materials/MAT_GlassBreakable.mat";
        private const string GlassPhysicsPath = ProjectSetupStage0.ProjectRoot + "/PhysicsMaterials/PM_Glass.asset";

        // Hueco para 2 × 2 cajas de 1 m (se llena en pirámide: 2 abajo y 1 encima), paneles de 6 cm y 2 cm de holgura.
        private static readonly Vector3Int Boxes = new Vector3Int(2, 2, 1);
        private const float BoxSize = 1f;
        private const float PanelThickness = 0.06f;
        private const float Gap = 0.02f;
        private const float CrateMass = 2f;

        // Ruta por defecto: 3 m a la derecha y vuelta, como el muro móvil pero algo más lenta.
        private static readonly Vector3[] DefaultWaypoints = { Vector3.zero, new Vector3(3f, 0f, 0f) };
        private const float DefaultSpeed = 1.5f;
        private const float DefaultWait = 0.5f;

        [MenuItem("NovaStriker/Obstáculos/Crear caja de vidrio con cajas (PF_GlassCrate)")]
        private static void Run()
        {
            var log = new StringBuilder("[NovaStriker Obstáculos] Caja de vidrio con cajas\n");
            if (EnsurePrefab(log))
            {
                Debug.Log(log.ToString());
            }
            else
            {
                Debug.LogError(log.ToString());
            }
        }

        /// <summary>Crea PF_GlassCrate, o lo actualiza si ya existe. Devuelve false si algo falla.</summary>
        internal static bool EnsurePrefab(StringBuilder log)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                return UpgradeExisting(log);
            }

            var glassMaterial = AssetDatabase.LoadAssetAtPath<Material>(GlassMaterialPath);
            var glassPhysics = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(GlassPhysicsPath);
            var boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectSetupStage5.BoxPath);
            if (glassMaterial == null || glassPhysics == null || boxPrefab == null)
            {
                log.AppendLine($"- ERROR: faltan {GlassMaterialPath}, {GlassPhysicsPath} o {ProjectSetupStage5.BoxPath}. " +
                               "Ejecuta antes la Etapa 5.");
                return false;
            }

            var layout = new GlassCrateLayout(Boxes, BoxSize, PanelThickness, Gap);
            var root = new GameObject("PF_GlassCrate");
            try
            {
                root.layer = GameLayers.Obstacle;
                GlassCrate crate = BuildCrate(root.transform, layout, glassMaterial, glassPhysics);
                Transform contents = BuildContents(crate.transform, layout, boxPrefab);
                ProjectSetupStage2.AssignReference(crate, "_contents", contents);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                log.AppendLine($"- Prefab: {PrefabPath}");
                log.AppendLine($"  · Exterior {Format(layout.Outer)} m, {contents.childCount} cajas en pirámide. Se mueve por su ruta " +
                               "(PathMover del objeto Crate). Se agrieta al primer golpe y se rompe al segundo.");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------- Actualizar un prefab existente ----------

        /// <summary>
        /// Pasa una PF_GlassCrate de la versión anterior (empujable, con Contents al lado) a la que se mueve por ruta,
        /// sin tocar lo que hayas cambiado: paneles, ajustes, cajas de dentro. Se puede ejecutar varias veces.
        /// </summary>
        private static bool UpgradeExisting(StringBuilder log)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform crateTransform = root.transform.Find(GlassCrateNames.Crate);
                GlassCrate crate = crateTransform != null ? crateTransform.GetComponent<GlassCrate>() : null;
                Rigidbody body = crateTransform != null ? crateTransform.GetComponent<Rigidbody>() : null;
                if (crate == null || body == null)
                {
                    log.AppendLine($"- ERROR: {PrefabPath} no tiene el objeto '{GlassCrateNames.Crate}' con GlassCrate y Rigidbody. " +
                                   "Bórralo y vuelve a ejecutar este menú para generarlo de nuevo.");
                    return false;
                }

                var changes = new List<string>();

                if (!body.isKinematic)
                {
                    body.isKinematic = true;
                    changes.Add("Rigidbody cinemático");
                }

                if (crateTransform.GetComponent<PathMover>() == null)
                {
                    AddPathMover(crateTransform.gameObject, body);
                    changes.Add("ruta (PathMover)");
                }

                Transform contents = root.transform.Find(GlassCrateNames.Contents);
                if (contents == null)
                {
                    contents = crateTransform.Find(GlassCrateNames.Contents);
                }

                if (contents != null && contents.parent != crateTransform)
                {
                    contents.SetParent(crateTransform, true);
                    changes.Add("cajas de dentro movidas dentro del vidrio");
                }

                var serialized = new SerializedObject(crate);
                SerializedProperty contentsField = serialized.FindProperty("_contents");
                if (contents != null && contentsField.objectReferenceValue != contents)
                {
                    contentsField.objectReferenceValue = contents;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    changes.Add("referencia a Contents");
                }

                ApplyGeometry(crateTransform, contents, changes, log);

                if (changes.Count == 0)
                {
                    log.AppendLine($"- {PrefabPath} ya está al día.");
                    return true;
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                log.AppendLine($"- {PrefabPath} actualizado: {string.Join(", ", changes)}.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Recoloca paneles y pirámide de cajas según las medidas de este script.</summary>
        private static void ApplyGeometry(Transform crate, Transform contents, List<string> changes, StringBuilder log)
        {
            var layout = new GlassCrateLayout(Boxes, BoxSize, PanelThickness, Gap);

            if (crate.localPosition != layout.CrateCenter)
            {
                crate.localPosition = layout.CrateCenter;
                changes.Add("altura del vidrio");
            }

            bool panelsChanged = false;
            foreach (GlassCrateLayout.Panel spec in layout.Panels())
            {
                Transform panel = crate.Find(spec.Name);
                if (panel == null)
                {
                    log.AppendLine($"- AVISO: falta el panel '{spec.Name}'. Borra el prefab y vuelve a generarlo.");
                    continue;
                }

                if (panel.localPosition != spec.LocalPosition || panel.localScale != spec.Scale ||
                    Quaternion.Angle(panel.localRotation, Quaternion.Euler(spec.LocalEuler)) > 0.01f)
                {
                    panel.localPosition = spec.LocalPosition;
                    panel.localEulerAngles = spec.LocalEuler;
                    panel.localScale = spec.Scale;
                    panelsChanged = true;
                }
            }

            if (panelsChanged)
            {
                changes.Add($"vidrio de {Format(layout.Outer)} m");
            }

            if (contents == null)
            {
                log.AppendLine("- AVISO: no hay objeto Contents; la caja queda vacía.");
                return;
            }

            contents.localPosition = -layout.CrateCenter;
            IReadOnlyList<Vector3> centers = layout.PyramidCenters();
            if (MatchesPyramid(contents, centers))
            {
                return;
            }

            var boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectSetupStage5.BoxPath);
            if (boxPrefab == null)
            {
                log.AppendLine($"- ERROR: falta {ProjectSetupStage5.BoxPath}; no se pudieron rehacer las cajas de dentro.");
                return;
            }

            for (int i = contents.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(contents.GetChild(i).gameObject);
            }

            FillContents(contents, centers, boxPrefab);
            changes.Add($"{centers.Count} cajas en pirámide");
        }

        private static bool MatchesPyramid(Transform contents, IReadOnlyList<Vector3> centers)
        {
            if (contents.childCount != centers.Count)
            {
                return false;
            }

            for (int i = 0; i < centers.Count; i++)
            {
                if ((contents.GetChild(i).localPosition - centers[i]).sqrMagnitude > 1e-6f)
                {
                    return false;
                }
            }

            return true;
        }

        // ---------- Construcción ----------

        private static GlassCrate BuildCrate(Transform root, GlassCrateLayout layout, Material material, PhysicsMaterial physics)
        {
            var crateObject = new GameObject(GlassCrateNames.Crate) { layer = GameLayers.Obstacle };
            crateObject.transform.SetParent(root, false);
            crateObject.transform.localPosition = layout.CrateCenter;

            var body = crateObject.AddComponent<Rigidbody>();
            body.mass = CrateMass;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            AddPathMover(crateObject, body);

            var colliders = new List<Object>();
            var renderers = new List<Object>();
            foreach (GlassCrateLayout.Panel spec in layout.Panels())
            {
                GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name = spec.Name;
                panel.layer = GameLayers.Obstacle;
                panel.transform.SetParent(crateObject.transform, false);
                panel.transform.localPosition = spec.LocalPosition;
                panel.transform.localEulerAngles = spec.LocalEuler;
                panel.transform.localScale = spec.Scale;

                var renderer = panel.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;

                var collider = panel.GetComponent<BoxCollider>();
                collider.sharedMaterial = physics;

                colliders.Add(collider);
                renderers.Add(renderer);
            }

            var crate = crateObject.AddComponent<GlassCrate>();
            ProjectSetupStage2.AssignReference(crate, "_body", body);
            ProjectSetupStage3.AssignArray(crate, "_panels", colliders.ToArray());
            ProjectSetupStage3.AssignArray(crate, "_renderers", renderers.ToArray());
            return crate;
        }

        private static void AddPathMover(GameObject crate, Rigidbody body)
        {
            var mover = crate.AddComponent<PathMover>();
            var serialized = new SerializedObject(mover);
            serialized.FindProperty("_body").objectReferenceValue = body;
            SerializedProperty list = serialized.FindProperty("_waypoints");
            list.arraySize = DefaultWaypoints.Length;
            for (int i = 0; i < DefaultWaypoints.Length; i++)
            {
                list.GetArrayElementAtIndex(i).vector3Value = DefaultWaypoints[i];
            }
            serialized.FindProperty("_speed").floatValue = DefaultSpeed;
            serialized.FindProperty("_waitAtWaypoint").floatValue = DefaultWait;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Las cajas van dentro del objeto Crate; sus posiciones se pasan de la base al centro del vidrio.</summary>
        private static Transform BuildContents(Transform crate, GlassCrateLayout layout, GameObject boxPrefab)
        {
            var contents = new GameObject(GlassCrateNames.Contents) { layer = GameLayers.Obstacle };
            contents.transform.SetParent(crate, false);
            contents.transform.localPosition = -layout.CrateCenter;

            FillContents(contents.transform, layout.PyramidCenters(), boxPrefab);
            return contents.transform;
        }

        private static void FillContents(Transform contents, IReadOnlyList<Vector3> centers, GameObject boxPrefab)
        {
            for (int i = 0; i < centers.Count; i++)
            {
                var box = (GameObject)PrefabUtility.InstantiatePrefab(boxPrefab, contents);
                box.name = $"Box_{i + 1}";
                box.transform.localPosition = centers[i];
                box.transform.localRotation = Quaternion.identity;
            }
        }

        private static string Format(Vector3 v) => $"{v.x:0.##} × {v.y:0.##} × {v.z:0.##}";
    }
}
