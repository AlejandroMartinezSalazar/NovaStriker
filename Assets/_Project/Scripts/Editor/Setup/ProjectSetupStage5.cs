using System.Text;
using NovaStriker.Contracts.Data;
using NovaStriker.Gameplay.Debris;
using NovaStriker.Gameplay.Obstacles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Etapa 5: materiales, materiales físicos y prefabs de los 7 obstáculos + fragmento de vidrio.
    /// (Desde la Etapa 6 los obstáculos se colocan en los prefabs de nivel.)
    /// Los prefabs solo se crean si NO existen (así se conservan los cambios que hagas en ellos).
    /// </summary>
    internal static class ProjectSetupStage5
    {
        private const string Root = ProjectSetupStage0.ProjectRoot;
        private const string ObstaclePrefabs = Root + "/Prefabs/Obstacles/";
        internal const string ShardPrefabPath = Root + "/Prefabs/VFX/PF_GlassShard.prefab";
        private const string Materials = Root + "/Art/Materials/";
        private const string PhysicsMaterials = Root + "/PhysicsMaterials/";

        internal const string BoxPath = ObstaclePrefabs + "PF_Box.prefab";
        internal const string StaticBoxPath = ObstaclePrefabs + "PF_StaticBox.prefab";
        internal const string WallPath = ObstaclePrefabs + "PF_Wall.prefab";
        internal const string GlassPath = ObstaclePrefabs + "PF_Glass.prefab";
        internal const string BreakableGlassPath = ObstaclePrefabs + "PF_GlassBreakable.prefab";
        internal const string MovingWallPath = ObstaclePrefabs + "PF_MovingWall.prefab";
        internal const string MovingPlatformPath = ObstaclePrefabs + "PF_MovingPlatform.prefab";

        private static readonly Vector3 GlassSize = new Vector3(3f, 2.5f, 0.08f);

        [MenuItem("NovaStriker/Setup/Etapa 5 - Obstaculos")]
        private static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[NovaStriker Setup] Cancelado: hay escenas sin guardar.");
                return;
            }

            var log = new StringBuilder("[NovaStriker Setup] Etapa 5\n");
            CreateAssets(log);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        // ---------- Assets ----------

        private static void CreateAssets(StringBuilder log)
        {
            Material box = Opaque("MAT_Box", new Color(0.85f, 0.55f, 0.25f), log);
            Material staticBox = Opaque("MAT_StaticBox", new Color(0.35f, 0.4f, 0.5f), log);
            Material wall = Opaque("MAT_Wall", new Color(0.8f, 0.8f, 0.75f), log);
            Material movingWall = Opaque("MAT_MovingWall", new Color(0.85f, 0.25f, 0.25f), log);
            Material platform = Opaque("MAT_Platform", new Color(0.25f, 0.55f, 0.85f), log);
            Material glass = Transparent("MAT_Glass", new Color(0.7f, 0.9f, 1f, 0.2f), log);
            Material breakableGlass = Transparent("MAT_GlassBreakable", new Color(0.75f, 0.9f, 1f, 0.25f), log);

            PhysicsMaterial boxPhysics = PhysicsMat("PM_Box", 0.1f, 0.5f, PhysicsMaterialCombine.Average, log);
            PhysicsMaterial bouncy = PhysicsMat("PM_Bouncy", 0.85f, 0.1f, PhysicsMaterialCombine.Maximum, log);
            PhysicsMaterial glassPhysics = PhysicsMat("PM_Glass", 0.5f, 0.1f, PhysicsMaterialCombine.Average, log);

            CreateObstacle(BoxPath, ObstacleType.Box, Vector3.one, GameLayers.Obstacle, box, boxPhysics, BodyKind.Dynamic, log);
            CreateObstacle(StaticBoxPath, ObstacleType.StaticBox, Vector3.one, GameLayers.Environment, staticBox, boxPhysics, BodyKind.None, log);
            CreateObstacle(WallPath, ObstacleType.Wall, new Vector3(4f, 3f, 0.4f), GameLayers.Environment, wall, bouncy, BodyKind.None, log);
            CreateObstacle(GlassPath, ObstacleType.Glass, GlassSize, GameLayers.Environment, glass, glassPhysics, BodyKind.None, log);
            CreateBreakableGlass(breakableGlass, glassPhysics, log);
            CreateObstacle(MovingWallPath, ObstacleType.MovingWall, new Vector3(3f, 2.5f, 0.4f), GameLayers.Obstacle,
                movingWall, bouncy, BodyKind.Kinematic, log, new[] { Vector3.zero, new Vector3(4f, 0f, 0f) }, 2f, 0.5f);
            CreateObstacle(MovingPlatformPath, ObstacleType.MovingPlatform, new Vector3(2.5f, 0.3f, 2.5f), GameLayers.Obstacle,
                platform, boxPhysics, BodyKind.Kinematic, log, new[] { Vector3.zero, new Vector3(0f, 0f, 5f) }, 1.5f, 1f);
            CreateShard(glass, log);
        }

        private enum BodyKind { None, Dynamic, Kinematic }

        private static void CreateObstacle(string path, ObstacleType type, Vector3 size, int layer, Material material,
            PhysicsMaterial physicsMaterial, BodyKind body, StringBuilder log,
            Vector3[] waypoints = null, float speed = 0f, float wait = 0f)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                return;
            }

            GameObject gameObject = CreateCube(System.IO.Path.GetFileNameWithoutExtension(path), size, layer, material, physicsMaterial);

            if (body != BodyKind.None)
            {
                var rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                if (body == BodyKind.Dynamic)
                {
                    rigidbody.mass = 4f;
                }
                else
                {
                    rigidbody.isKinematic = true;
                    rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                }

                if (waypoints != null)
                {
                    var mover = gameObject.AddComponent<PathMover>();
                    var serialized = new SerializedObject(mover);
                    serialized.FindProperty("_body").objectReferenceValue = rigidbody;
                    SerializedProperty list = serialized.FindProperty("_waypoints");
                    list.arraySize = waypoints.Length;
                    for (int i = 0; i < waypoints.Length; i++)
                    {
                        list.GetArrayElementAtIndex(i).vector3Value = waypoints[i];
                    }
                    serialized.FindProperty("_speed").floatValue = speed;
                    serialized.FindProperty("_waitAtWaypoint").floatValue = wait;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var view = gameObject.AddComponent<ObstacleView>();
            var viewSerialized = new SerializedObject(view);
            viewSerialized.FindProperty("_type").intValue = (int)type;
            viewSerialized.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(gameObject, path, log);
        }

        private static void CreateBreakableGlass(Material material, PhysicsMaterial physicsMaterial, StringBuilder log)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BreakableGlassPath) != null)
            {
                return;
            }

            GameObject gameObject = CreateCube("PF_GlassBreakable", GlassSize, GameLayers.Breakable, material, physicsMaterial);
            var glass = gameObject.AddComponent<BreakableGlass>();
            ProjectSetupStage2.AssignReference(glass, "_collider", gameObject.GetComponent<Collider>());
            ProjectSetupStage2.AssignReference(glass, "_renderer", gameObject.GetComponent<Renderer>());
            SavePrefab(gameObject, BreakableGlassPath, log);
        }

        private static void CreateShard(Material material, StringBuilder log)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ShardPrefabPath) != null)
            {
                return;
            }

            GameObject gameObject = CreateCube("PF_GlassShard", Vector3.one, GameLayers.Debris, material, null);
            var body = gameObject.AddComponent<Rigidbody>();
            body.mass = 0.15f;
            body.isKinematic = true;
            var shard = gameObject.AddComponent<DebrisShard>();
            ProjectSetupStage2.AssignReference(shard, "_body", body);
            SavePrefab(gameObject, ShardPrefabPath, log);
        }

        private static GameObject CreateCube(string name, Vector3 size, int layer, Material material, PhysicsMaterial physicsMaterial)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.layer = layer;
            gameObject.transform.localScale = size;
            var renderer = gameObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (material != null && material.renderQueue >= (int)RenderQueue.Transparent)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            gameObject.GetComponent<BoxCollider>().sharedMaterial = physicsMaterial;
            return gameObject;
        }

        private static void SavePrefab(GameObject gameObject, string path, StringBuilder log)
        {
            PrefabUtility.SaveAsPrefabAsset(gameObject, path);
            Object.DestroyImmediate(gameObject);
            log.AppendLine($"- Prefab: {path}");
        }

        private static Material Opaque(string name, Color color, StringBuilder log)
        {
            string path = Materials + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            log.AppendLine($"- Material: {path}");
            return material;
        }

        private static Material Transparent(string name, Color color, StringBuilder log)
        {
            string path = Materials + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetFloat("_Surface", 1f); // Transparent
            material.SetFloat("_Blend", 0f);   // Alpha
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Smoothness", 0.9f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            log.AppendLine($"- Material transparente: {path}");
            return material;
        }

        private static PhysicsMaterial PhysicsMat(string name, float bounciness, float friction, PhysicsMaterialCombine bounceCombine, StringBuilder log)
        {
            string path = PhysicsMaterials + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (existing != null)
            {
                return existing;
            }

            var material = new PhysicsMaterial(name)
            {
                bounciness = bounciness,
                dynamicFriction = friction,
                staticFriction = friction,
                bounceCombine = bounceCombine,
                frictionCombine = PhysicsMaterialCombine.Average,
            };
            AssetDatabase.CreateAsset(material, path);
            log.AppendLine($"- Material físico: {path}");
            return material;
        }
    }
}
