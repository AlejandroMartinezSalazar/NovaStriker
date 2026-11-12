using System.Text;
using NovaStriker.Contracts.Data;
using NovaStriker.Gameplay.Aim;
using NovaStriker.Gameplay.Ball;
using NovaStriker.Gameplay.Throwing;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Etapa 3: materiales, PF_Ball y assets de configuración del tiro.
    /// Idempotente: no recrea assets existentes. Desde la Etapa 6 ya no toca SC_Game (el sandbox se retiró;
    /// la escena la monta la Etapa 6) y conserva utilidades compartidas por las demás etapas.
    /// </summary>
    internal static class ProjectSetupStage3
    {
        private const string Root = ProjectSetupStage0.ProjectRoot;
        internal const string BallPrefabPath = Root + "/Prefabs/Balls/PF_Ball.prefab";
        private const string BallPhysicsMaterialPath = Root + "/PhysicsMaterials/PM_Ball.asset";
        internal const string BallConfigPath = Root + "/Settings/Config/SO_BallConfig.asset";
        internal const string AimConfigPath = Root + "/Settings/Config/SO_AimConfig.asset";
        internal const string StraightPath = Root + "/Settings/Throws/SO_Throw_Straight.asset";
        internal const string LobPath = Root + "/Settings/Throws/SO_Throw_Lob.asset";
        internal const string CurvePath = Root + "/Settings/Throws/SO_Throw_Curve.asset";

        internal const string LineMaterialName = "MAT_Trajectory";
        internal const string MarkerMaterialName = "MAT_AimMarker";
        private const string BallMaterialName = "MAT_Ball";
        internal const string GroundMaterialName = "MAT_Ground";
        private const string BoxMaterialName = "MAT_Box";

        internal const float BallDiameter = 0.22f; // Balón talla 5.
        private const float BallMass = 0.43f;

        [MenuItem("NovaStriker/Setup/Etapa 3 - Balon y assets de tiro")]
        private static void Run()
        {
            var log = new StringBuilder("[NovaStriker Setup] Etapa 3\n");

            GetOrCreateMaterial(LineMaterialName, unlit: true, new Color(1f, 0.92f, 0.3f), log);
            GetOrCreateMaterial(MarkerMaterialName, unlit: true, new Color(1f, 0.55f, 0.1f), log);
            Material ballMaterial = GetOrCreateMaterial(BallMaterialName, unlit: false, Color.white, log);
            GetOrCreateMaterial(GroundMaterialName, unlit: false, new Color(0.23f, 0.55f, 0.27f), log);
            GetOrCreateMaterial(BoxMaterialName, unlit: false, new Color(0.85f, 0.55f, 0.25f), log);

            PhysicsMaterial ballPhysics = GetOrCreateBallPhysicsMaterial(log);
            GetOrCreateAsset<BallConfig>(BallConfigPath, log);
            GetOrCreateAsset<AimConfig>(AimConfigPath, log);
            GetOrCreateStrategy<StraightThrowDefinition>(StraightPath, "Recto", log);
            GetOrCreateStrategy<LobThrowDefinition>(LobPath, "Bombeado", log);
            GetOrCreateStrategy<CurveThrowDefinition>(CurvePath, "Curva", log);

            GetOrCreateBallPrefab(ballMaterial, ballPhysics, LoadMaterial(LineMaterialName), log);
            AssetDatabase.SaveAssets();

            log.AppendLine("- La escena SC_Game la monta la Etapa 6.");
            Debug.Log(log.ToString());
        }

        // ---------- Assets ----------

        private static string MaterialPath(string name) => $"{Root}/Art/Materials/{name}.mat";

        internal static Material LoadMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(name));

        private static Material GetOrCreateMaterial(string name, bool unlit, Color color, StringBuilder log)
        {
            string path = MaterialPath(name);
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else
            {
                material.color = color;
            }

            AssetDatabase.CreateAsset(material, path);
            log.AppendLine($"- Material: {path}");
            return material;
        }

        private static PhysicsMaterial GetOrCreateBallPhysicsMaterial(StringBuilder log)
        {
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(BallPhysicsMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            var material = new PhysicsMaterial("PM_Ball")
            {
                bounciness = 0.65f,
                dynamicFriction = 0.35f,
                staticFriction = 0.4f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                frictionCombine = PhysicsMaterialCombine.Average,
            };
            AssetDatabase.CreateAsset(material, BallPhysicsMaterialPath);
            log.AppendLine($"- Physics material: {BallPhysicsMaterialPath}");
            return material;
        }

        internal static T GetOrCreateAsset<T>(string path, StringBuilder log) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            log.AppendLine($"- Asset: {path}");
            return asset;
        }

        private static T GetOrCreateStrategy<T>(string path, string displayName, StringBuilder log) where T : ThrowStrategyDefinition
        {
            bool existed = AssetDatabase.LoadAssetAtPath<T>(path) != null;
            T asset = GetOrCreateAsset<T>(path, log);
            if (!existed)
            {
                var serialized = new SerializedObject(asset);
                serialized.FindProperty("_displayName").stringValue = displayName;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
            return asset;
        }

        private static BallView GetOrCreateBallPrefab(Material ballMaterial, PhysicsMaterial physicsMaterial, Material trailMaterial, StringBuilder log)
        {
            var existing = AssetDatabase.LoadAssetAtPath<BallView>(BallPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "PF_Ball";
            ball.layer = GameLayers.Ball;
            ball.transform.localScale = Vector3.one * BallDiameter;
            ball.GetComponent<MeshRenderer>().sharedMaterial = ballMaterial;
            ball.GetComponent<SphereCollider>().sharedMaterial = physicsMaterial;

            var body = ball.AddComponent<Rigidbody>();
            body.mass = BallMass;
            body.linearDamping = 0f; // Obligatorio: el cálculo del tiro asume que no hay rozamiento con el aire.
            body.angularDamping = 0.4f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.isKinematic = true;

            var trail = ball.AddComponent<TrailRenderer>();
            trail.sharedMaterial = trailMaterial;
            trail.time = 0.3f;
            trail.startWidth = BallDiameter * 0.8f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.shadowCastingMode = ShadowCastingMode.Off;

            var curveForce = ball.AddComponent<BallCurveForce>();
            ProjectSetupStage2.AssignReference(curveForce, "_rigidbody", body);

            var view = ball.AddComponent<BallView>();
            ProjectSetupStage2.AssignReference(view, "_rigidbody", body);
            ProjectSetupStage2.AssignReference(view, "_curveForce", curveForce);
            ProjectSetupStage2.AssignReference(view, "_trail", trail);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(ball, BallPrefabPath);
            Object.DestroyImmediate(ball);
            log.AppendLine($"- Prefab: {BallPrefabPath}");
            return prefab.GetComponent<BallView>();
        }

        // ---------- Utilidades compartidas ----------

        /// <summary>Relee el componente serializado y falla si alguna referencia a objeto quedó vacía.</summary>
        internal static bool VerifyNoMissingReferences(Object target, StringBuilder log)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.GetIterator();
            bool ok = true;
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = true;
                if (property.propertyPath == "m_Script")
                {
                    continue;
                }

                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                {
                    log.AppendLine($"- ERROR: {target.GetType().Name}.{property.propertyPath} quedó vacío.");
                    ok = false;
                }
            }

            return ok;
        }

        /// <summary>Línea de trayectoria + marcador de destino (AimPreviewView).</summary>
        internal static AimPreviewView BuildAimPreview(Transform parent, Material lineMaterial, Material markerMaterial)
        {
            var preview = new GameObject("AimPreview", typeof(LineRenderer), typeof(AimPreviewView));
            preview.transform.SetParent(parent, false);

            var line = preview.GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = lineMaterial;
            line.widthMultiplier = 0.06f;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Marker";
            marker.layer = 2; // Ignore Raycast.
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.transform.SetParent(preview.transform, false);
            marker.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
            var markerRenderer = marker.GetComponent<MeshRenderer>();
            markerRenderer.sharedMaterial = markerMaterial;
            markerRenderer.shadowCastingMode = ShadowCastingMode.Off;

            var view = preview.GetComponent<AimPreviewView>();
            ProjectSetupStage2.AssignReference(view, "_line", line);
            ProjectSetupStage2.AssignReference(view, "_marker", marker.transform);
            return view;
        }

        internal static void AssignArray(Object target, string propertyName, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject gameObject in scene.GetRootGameObjects())
            {
                if (gameObject.name == name)
                {
                    return gameObject;
                }
            }
            return null;
        }

        internal static T FindRootComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject gameObject in scene.GetRootGameObjects())
            {
                if (gameObject.TryGetComponent(out T component))
                {
                    return component;
                }
            }
            return null;
        }

        internal static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject gameObject in scene.GetRootGameObjects())
            {
                T component = gameObject.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }
            return null;
        }
    }
}
