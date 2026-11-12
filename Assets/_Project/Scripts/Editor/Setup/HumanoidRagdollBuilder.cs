using System.Collections.Generic;
using System.Text;
using NovaStriker.Contracts.Data;
using UnityEngine;

namespace NovaStriker.EditorTools.Setup
{
    /// <summary>
    /// Construye un ragdoll de 11 huesos sobre un modelo con rig Humanoid (inspirado en el Ragdoll Wizard de Unity).
    /// Los huesos se obtienen con Animator.GetBoneTransform, así que funciona con cualquier nombre de esqueleto.
    /// Supone el modelo en T-pose, mirando a +Z. Los límites de las articulaciones son aproximados y se ajustan probando.
    /// Deja los Rigidbodies kinemáticos y los colliders apagados (estado "animado").
    /// </summary>
    internal static class HumanoidRagdollBuilder
    {
        public sealed class Result
        {
            public readonly List<Rigidbody> Bodies = new List<Rigidbody>();
            public readonly List<Collider> Colliders = new List<Collider>();
        }

        private enum Shape { Box, Capsule, Sphere }

        private sealed class BoneSpec
        {
            public HumanBodyBones Bone;
            public HumanBodyBones Parent;
            public bool IsRoot;
            public Shape Shape;
            public float MassFraction;
            public Vector3 TwistAxis;
            public Vector3 SwingAxis;
            public float LowTwist;
            public float HighTwist;
            public float Swing;
        }

        /// <summary>Altura de referencia (m) para escalar tamaños fijos como el radio de la cabeza.</summary>
        private const float ReferenceHeight = 1.8f;

        private static readonly BoneSpec[] Specs =
        {
            new BoneSpec { Bone = HumanBodyBones.Hips, IsRoot = true, Shape = Shape.Box, MassFraction = 0.17f },

            Limb(HumanBodyBones.LeftUpperLeg, HumanBodyBones.Hips, 0.10f, Vector3.right, Vector3.forward, -20f, 70f, 30f),
            Limb(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftUpperLeg, 0.07f, Vector3.right, Vector3.forward, -80f, 0f, 0f),
            Limb(HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips, 0.10f, Vector3.right, Vector3.forward, -20f, 70f, 30f),
            Limb(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightUpperLeg, 0.07f, Vector3.right, Vector3.forward, -80f, 0f, 0f),

            new BoneSpec { Bone = HumanBodyBones.Spine, Parent = HumanBodyBones.Hips, Shape = Shape.Box, MassFraction = 0.23f,
                TwistAxis = Vector3.right, SwingAxis = Vector3.forward, LowTwist = -20f, HighTwist = 20f, Swing = 10f },
            new BoneSpec { Bone = HumanBodyBones.Head, Parent = HumanBodyBones.Spine, Shape = Shape.Sphere, MassFraction = 0.08f,
                TwistAxis = Vector3.right, SwingAxis = Vector3.forward, LowTwist = -40f, HighTwist = 25f, Swing = 25f },

            Limb(HumanBodyBones.LeftUpperArm, HumanBodyBones.Spine, 0.05f, Vector3.up, Vector3.forward, -60f, 60f, 50f),
            Limb(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftUpperArm, 0.04f, Vector3.up, Vector3.forward, -90f, 0f, 0f),
            Limb(HumanBodyBones.RightUpperArm, HumanBodyBones.Spine, 0.05f, Vector3.up, Vector3.forward, -60f, 60f, 50f),
            Limb(HumanBodyBones.RightLowerArm, HumanBodyBones.RightUpperArm, 0.04f, Vector3.down, Vector3.forward, -90f, 0f, 0f),
        };

        private static BoneSpec Limb(HumanBodyBones bone, HumanBodyBones parent, float mass, Vector3 twist, Vector3 swing,
            float lowTwist, float highTwist, float swingLimit)
        {
            return new BoneSpec
            {
                Bone = bone, Parent = parent, Shape = Shape.Capsule, MassFraction = mass,
                TwistAxis = twist, SwingAxis = swing, LowTwist = lowTwist, HighTwist = highTwist, Swing = swingLimit,
            };
        }

        public static Result Build(Animator animator, float modelHeight, float totalMass, int solverIterations, StringBuilder log)
        {
            float scale = modelHeight / ReferenceHeight;
            var result = new Result();
            var bodies = new Dictionary<HumanBodyBones, Rigidbody>();

            foreach (BoneSpec spec in Specs)
            {
                Transform bone = animator.GetBoneTransform(spec.Bone);
                if (bone == null)
                {
                    log.AppendLine($"- ERROR: el avatar no tiene el hueso {spec.Bone}.");
                    return null;
                }

                Collider collider = AddCollider(animator, spec, bone, scale);
                collider.enabled = false;
                bone.gameObject.layer = GameLayers.Ragdoll;

                var body = bone.gameObject.AddComponent<Rigidbody>();
                body.mass = totalMass * spec.MassFraction;
                body.linearDamping = 0.05f;
                body.angularDamping = 0.3f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.solverIterations = solverIterations;
                body.solverVelocityIterations = Mathf.Max(1, solverIterations / 3);
                body.isKinematic = true;

                if (!spec.IsRoot)
                {
                    AddJoint(bone, body, bodies[spec.Parent], spec);
                }

                bodies[spec.Bone] = body;
                result.Bodies.Add(body);
                result.Colliders.Add(collider);
            }

            log.AppendLine($"- Ragdoll: {result.Bodies.Count} huesos, masa total {totalMass:0.#} kg.");
            return result;
        }

        private static Collider AddCollider(Animator animator, BoneSpec spec, Transform bone, float scale)
        {
            switch (spec.Bone)
            {
                case HumanBodyBones.Hips:
                    return AddBox(bone, 0.08f * scale,
                        animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position,
                        animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position,
                        animator.GetBoneTransform(HumanBodyBones.Spine).position);

                case HumanBodyBones.Spine:
                    return AddBox(bone, 0.06f * scale,
                        animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position,
                        animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position,
                        animator.GetBoneTransform(HumanBodyBones.Head).position);

                case HumanBodyBones.Head:
                    return AddHeadSphere(bone, 0.11f * scale);

                case HumanBodyBones.LeftUpperLeg:
                    return AddCapsule(bone, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position, 0.22f);
                case HumanBodyBones.RightUpperLeg:
                    return AddCapsule(bone, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position, 0.22f);
                case HumanBodyBones.LeftLowerLeg:
                    return AddCapsule(bone, animator.GetBoneTransform(HumanBodyBones.LeftFoot).position, 0.18f);
                case HumanBodyBones.RightLowerLeg:
                    return AddCapsule(bone, animator.GetBoneTransform(HumanBodyBones.RightFoot).position, 0.18f);
                case HumanBodyBones.LeftUpperArm:
                    return AddCapsule(bone, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position, 0.2f);
                case HumanBodyBones.RightUpperArm:
                    return AddCapsule(bone, animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position, 0.2f);
                case HumanBodyBones.LeftLowerArm:
                    return AddCapsule(bone, ExtendToIncludeHand(animator, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand), 0.18f);
                case HumanBodyBones.RightLowerArm:
                    return AddCapsule(bone, ExtendToIncludeHand(animator, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand), 0.18f);
                default:
                    return AddHeadSphere(bone, 0.05f * scale);
            }
        }

        private static Vector3 ExtendToIncludeHand(Animator animator, HumanBodyBones forearm, HumanBodyBones hand)
        {
            Vector3 elbow = animator.GetBoneTransform(forearm).position;
            Vector3 wrist = animator.GetBoneTransform(hand).position;
            return elbow + (wrist - elbow) * 1.25f;
        }

        /// <summary>Cápsula desde el hueso hasta <paramref name="endWorld"/>, alineada al eje local más parecido.</summary>
        private static CapsuleCollider AddCapsule(Transform bone, Vector3 endWorld, float radiusFactor)
        {
            Vector3 localEnd = bone.InverseTransformPoint(endWorld);
            var capsule = bone.gameObject.AddComponent<CapsuleCollider>();
            capsule.direction = LargestAxis(localEnd);
            capsule.center = localEnd * 0.5f;
            capsule.height = localEnd.magnitude;
            capsule.radius = localEnd.magnitude * radiusFactor;
            return capsule;
        }

        /// <summary>Caja en el espacio local del hueso que envuelve los puntos dados, con margen.</summary>
        private static BoxCollider AddBox(Transform bone, float worldPadding, params Vector3[] worldPoints)
        {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (Vector3 point in worldPoints)
            {
                bounds.Encapsulate(bone.InverseTransformPoint(point));
            }
            bounds.Expand(worldPadding * 2f / UniformScale(bone));

            var box = bone.gameObject.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
            return box;
        }

        private static SphereCollider AddHeadSphere(Transform bone, float worldRadius)
        {
            var sphere = bone.gameObject.AddComponent<SphereCollider>();
            sphere.radius = worldRadius / UniformScale(bone);
            sphere.center = bone.InverseTransformPoint(bone.position + Vector3.up * worldRadius * 0.8f);
            return sphere;
        }

        private static void AddJoint(Transform bone, Rigidbody body, Rigidbody parent, BoneSpec spec)
        {
            var joint = bone.gameObject.AddComponent<CharacterJoint>();
            joint.connectedBody = parent;
            joint.axis = bone.InverseTransformDirection(spec.TwistAxis).normalized;
            joint.swingAxis = bone.InverseTransformDirection(spec.SwingAxis).normalized;
            joint.lowTwistLimit = new SoftJointLimit { limit = spec.LowTwist };
            joint.highTwistLimit = new SoftJointLimit { limit = spec.HighTwist };
            joint.swing1Limit = new SoftJointLimit { limit = spec.Swing };
            joint.swing2Limit = new SoftJointLimit { limit = spec.Swing };
            joint.enablePreprocessing = false; // Más estable en ragdolls.
            joint.enableProjection = true;      // Evita que las articulaciones se estiren con golpes fuertes.
        }

        private static int LargestAxis(Vector3 v)
        {
            float x = Mathf.Abs(v.x), y = Mathf.Abs(v.y), z = Mathf.Abs(v.z);
            if (x >= y && x >= z)
            {
                return 0;
            }
            return y >= z ? 1 : 2;
        }

        private static float UniformScale(Transform bone)
        {
            Vector3 s = bone.lossyScale;
            float average = (Mathf.Abs(s.x) + Mathf.Abs(s.y) + Mathf.Abs(s.z)) / 3f;
            return average > 1e-6f ? average : 1f;
        }
    }
}
