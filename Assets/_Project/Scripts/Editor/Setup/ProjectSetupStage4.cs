using System.Linq;
using System.Text;
using NovaStriker.Contracts.Data;
using NovaStriker.Gameplay.Characters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Etapa 4: genera PF_Footballer (modelo Humanoid + baile + ragdoll) y su definición.
    /// (Desde la Etapa 6 los futbolistas se colocan en los prefabs de nivel con FootballerSpawnPoint.)
    /// El prefab se REGENERA en cada ejecución (conserva su GUID), así que se puede volver a lanzar tras cambiar
    /// el modelo o los parámetros de abajo.
    /// </summary>
    internal static class ProjectSetupStage4
    {
        private const string Root = ProjectSetupStage0.ProjectRoot;
        private const string ModelPath = Root + "/Art/Models/Characters/Player_Tpose.fbx";
        private const string AnimationPath = Root + "/Art/Models/Characters/HipHopDacingAnimation.fbx";
        private const string ControllerPath = Root + "/Art/Animations/AC_Footballer.controller";
        internal const string PrefabPath = Root + "/Prefabs/Characters/PF_Footballer.prefab";
        internal const string DefinitionPath = Root + "/Settings/Characters/SO_Character_Footballer.asset";

        // Valores arcade: masas bajas para que el balón (0,43 kg) y las cajas (4 kg) los derriben con facilidad.
        private const float RootMass = 12f;
        private const float RagdollTotalMass = 12f;
        private const int BoneSolverIterations = 12;
        private const float CapsuleRadiusAtReferenceHeight = 0.2f;
        private const float ReferenceHeight = 1.8f;
        private const float MinExpectedHeight = 1.4f;
        private const float MaxExpectedHeight = 2.2f;

        [MenuItem("NovaStriker/Setup/Etapa 4 - Futbolista ragdoll")]
        private static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[NovaStriker Setup] Cancelado: hay escenas sin guardar.");
                return;
            }

            var log = new StringBuilder("[NovaStriker Setup] Etapa 4\n");
            bool ok = BuildFootballerAssets(log);

            if (ok)
            {
                Debug.Log(log.ToString());
            }
            else
            {
                Debug.LogError(log.ToString());
            }
        }

        // ---------- Assets ----------

        private static bool BuildFootballerAssets(StringBuilder log)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                log.AppendLine($"- ERROR: no se encontró el modelo en {ModelPath}.");
                return false;
            }

            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(AnimationPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null)
            {
                log.AppendLine($"- ERROR: no se encontró un AnimationClip en {AnimationPath}.");
                return false;
            }

            AnimatorController controller = GetOrCreateController(clip, log);
            string idleStateName = controller.layers[0].stateMachine.defaultState.name;

            if (!BuildPrefab(model, controller, idleStateName, log))
            {
                return false;
            }

            var definition = ProjectSetupStage3.GetOrCreateAsset<CharacterDefinition>(DefinitionPath, log);
            var prefab = AssetDatabase.LoadAssetAtPath<FootballerController>(PrefabPath);
            ProjectSetupStage2.AssignReference(definition, "_prefab", prefab);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static AnimatorController GetOrCreateController(AnimationClip clip, StringBuilder log)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null)
            {
                return existing;
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(ControllerPath, clip);
            log.AppendLine($"- AnimatorController: {ControllerPath} (estado '{controller.layers[0].stateMachine.defaultState.name}').");
            return controller;
        }

        private static bool BuildPrefab(GameObject model, AnimatorController controller, string idleStateName, StringBuilder log)
        {
            var root = new GameObject("PF_Footballer");
            try
            {
                var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                modelInstance.name = "Model";
                modelInstance.transform.SetParent(root.transform, false);

                var animator = modelInstance.GetComponent<Animator>();
                if (animator == null || animator.avatar == null || !animator.isHuman)
                {
                    log.AppendLine("- ERROR: el modelo no tiene un Animator con avatar Humanoid válido (pestaña Rig → Humanoid).");
                    return false;
                }

                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false; // El baile se queda en el sitio.
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                // Medir y apoyar los pies en y = 0 del prefab.
                Bounds bounds = CalculateRenderBounds(modelInstance);
                float height = bounds.size.y;
                modelInstance.transform.localPosition = new Vector3(0f, -bounds.min.y, 0f);
                log.AppendLine($"- Modelo: altura medida {height:0.00} m.");
                if (height < MinExpectedHeight || height > MaxExpectedHeight)
                {
                    log.AppendLine($"- AVISO: la altura esperada es ~{ReferenceHeight} m. Revisa el Scale Factor del FBX (pestaña Model).");
                }

                SetLayerRecursively(root, GameLayers.Ragdoll);

                HumanoidRagdollBuilder.Result ragdoll =
                    HumanoidRagdollBuilder.Build(animator, height, RagdollTotalMass, BoneSolverIterations, log);
                if (ragdoll == null)
                {
                    return false;
                }

                var body = root.AddComponent<Rigidbody>();
                body.mass = RootMass;
                body.linearDamping = 0f;
                body.angularDamping = 0.05f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

                var capsule = root.AddComponent<CapsuleCollider>();
                capsule.direction = 1;
                capsule.height = height;
                capsule.radius = Mathf.Min(CapsuleRadiusAtReferenceHeight * height / ReferenceHeight, height * 0.5f);
                capsule.center = new Vector3(0f, height * 0.5f, 0f);
                capsule.sharedMaterial = FootballerFriction.GetOrCreateMaterial(log);

                var rig = root.AddComponent<RagdollRig>();
                ProjectSetupStage2.AssignReference(rig, "_animator", animator);
                ProjectSetupStage3.AssignArray(rig, "_bodies", ragdoll.Bodies.Cast<Object>().ToArray());
                ProjectSetupStage3.AssignArray(rig, "_colliders", ragdoll.Colliders.Cast<Object>().ToArray());

                var controllerComponent = root.AddComponent<FootballerController>();
                ProjectSetupStage2.AssignReference(controllerComponent, "_body", body);
                ProjectSetupStage2.AssignReference(controllerComponent, "_capsule", capsule);
                ProjectSetupStage2.AssignReference(controllerComponent, "_ragdoll", rig);
                ProjectSetupStage2.AssignReference(controllerComponent, "_animator", animator);
                AssignString(controllerComponent, "_idleStateName", idleStateName);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                log.AppendLine($"- Prefab regenerado: {PrefabPath}");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Bounds CalculateRenderBounds(GameObject gameObject)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(gameObject.transform.position, Vector3.one * ReferenceHeight);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void AssignString(Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
