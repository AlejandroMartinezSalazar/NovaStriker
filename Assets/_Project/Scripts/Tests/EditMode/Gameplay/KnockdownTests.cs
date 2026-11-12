using NovaStriker.Contracts.Data;
using NovaStriker.Gameplay.Characters;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Gameplay
{
    public class KnockdownTests
    {
        private const float Dt = 0.02f;
        private static readonly Vector3 Up = Vector3.up;

        private static KnockdownSettings Settings() => new KnockdownSettings
        {
            ArmingDelay = 0.1f,
            UngroundedGrace = 0.2f,
            FallSpeed = 2.5f,
            MaxSupportTiltDegrees = 25f,
            TiltGrace = 0.08f,
            PushedSpeed = 1.5f,
            BallHitMinSpeed = 2f,
            ImpactMinSpeed = 2.5f,
        };

        private static KnockdownDetector ArmedDetector()
        {
            var detector = new KnockdownDetector(Settings());
            detector.Step(0.1f, true, Up, Vector3.zero);
            Assert.IsTrue(detector.IsArmed);
            return detector;
        }

        private static KnockdownCause StepFor(KnockdownDetector detector, float seconds, bool grounded, Vector3 normal, Vector3 velocity)
        {
            int steps = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < steps; i++)
            {
                KnockdownCause cause = detector.Step(Dt, grounded, normal, velocity);
                if (cause != KnockdownCause.None)
                {
                    return cause;
                }
            }
            return KnockdownCause.None;
        }

        // ---------- Detector ----------

        [Test]
        public void StandingStill_OnFlatGround_NeverKnocksDown()
        {
            KnockdownDetector detector = ArmedDetector();
            Assert.AreEqual(KnockdownCause.None, StepFor(detector, 5f, true, Up, Vector3.zero));
        }

        [Test]
        public void BeforeArming_FallAndTiltAreIgnored()
        {
            var detector = new KnockdownDetector(Settings());

            Assert.AreEqual(KnockdownCause.None, detector.Step(0.05f, false, Up, new Vector3(5f, -10f, 0f)));
            Assert.IsFalse(detector.IsArmed);
        }

        [Test]
        public void LosingSupport_BriefGap_IsTolerated_LongerGap_IsFall()
        {
            KnockdownDetector detector = ArmedDetector();

            Assert.AreEqual(KnockdownCause.None, StepFor(detector, 0.1f, false, Up, Vector3.zero));
            Assert.AreEqual(KnockdownCause.None, StepFor(detector, 0.1f, true, Up, Vector3.zero), "Recupera apoyo: se reinicia.");
            Assert.AreEqual(KnockdownCause.Fall, StepFor(detector, 0.3f, false, Up, Vector3.zero));
        }

        [Test]
        public void FastDownwardVelocity_IsFall_Immediately()
        {
            KnockdownDetector detector = ArmedDetector();
            Assert.AreEqual(KnockdownCause.Fall, detector.Step(Dt, true, Up, new Vector3(0f, -3f, 0f)));
        }

        [Test]
        public void SupportTilt_AboveLimit_Sustained_IsTilt()
        {
            KnockdownDetector detector = ArmedDetector();
            Vector3 tilted = Quaternion.Euler(0f, 0f, 35f) * Vector3.up;

            Assert.AreEqual(KnockdownCause.None, detector.Step(Dt, true, tilted, Vector3.zero), "Un solo paso no basta (filtra bordes).");
            Assert.AreEqual(KnockdownCause.Tilt, StepFor(detector, 0.1f, true, tilted, Vector3.zero));
        }

        [Test]
        public void SupportTilt_BelowLimit_IsIgnored()
        {
            KnockdownDetector detector = ArmedDetector();
            Vector3 gentle = Quaternion.Euler(15f, 0f, 0f) * Vector3.up;

            Assert.AreEqual(KnockdownCause.None, StepFor(detector, 2f, true, gentle, Vector3.zero));
        }

        [Test]
        public void HorizontalSpeed_AbovePushedLimit_IsPushed()
        {
            KnockdownDetector detector = ArmedDetector();

            Assert.AreEqual(KnockdownCause.None, detector.Step(Dt, true, Up, new Vector3(1f, 0f, 0f)));
            Assert.AreEqual(KnockdownCause.Pushed, detector.Step(Dt, true, Up, new Vector3(1.2f, 0f, 1.2f)));
        }

        [TestCase(CollisionKind.Ball, 1.9f, KnockdownCause.None)]
        [TestCase(CollisionKind.Ball, 2f, KnockdownCause.BallHit)]
        [TestCase(CollisionKind.Impact, 2f, KnockdownCause.None)]
        [TestCase(CollisionKind.Impact, 3f, KnockdownCause.Impact)]
        [TestCase(CollisionKind.Ignore, 50f, KnockdownCause.None)]
        public void Collisions_UseTheirThresholds(CollisionKind kind, float speed, KnockdownCause expected)
        {
            var detector = new KnockdownDetector(Settings());
            Assert.AreEqual(expected, detector.EvaluateCollision(kind, speed), "Los choques cuentan incluso antes de armarse.");
        }

        // ---------- Clasificación ----------

        [TestCase(GameLayers.Ball, CollisionKind.Ball)]
        [TestCase(GameLayers.Obstacle, CollisionKind.Impact)]
        [TestCase(GameLayers.Breakable, CollisionKind.Impact)]
        [TestCase(GameLayers.Ragdoll, CollisionKind.Impact)]
        [TestCase(GameLayers.Debris, CollisionKind.Impact)]
        [TestCase(GameLayers.Environment, CollisionKind.Ignore)]
        [TestCase(0, CollisionKind.Ignore)]
        public void CollisionClassifier_MapsLayers(int layer, CollisionKind expected)
        {
            Assert.AreEqual(expected, CollisionClassifier.FromLayer(layer));
        }

        // ---------- Impulso ----------

        [Test]
        public void HitImpulse_AlwaysPushesFromHitterTowardsTarget()
        {
            var toTarget = new Vector3(0f, 0f, 1f);

            Vector3 a = HitImpulseCalculator.Compute(new Vector3(0f, 0f, 10f), toTarget, 0.5f, 2f, 100f);
            Vector3 b = HitImpulseCalculator.Compute(new Vector3(0f, 0f, -10f), toTarget, 0.5f, 2f, 100f);

            Assert.Greater(a.z, 0f);
            Assert.Greater(b.z, 0f, "Con el signo contrario de relativeVelocity el resultado no cambia.");
            Assert.AreEqual(10f, a.magnitude, 1e-4f);
        }

        [Test]
        public void HitImpulse_IsClamped_AndZeroWithoutSpeed()
        {
            Vector3 clamped = HitImpulseCalculator.Compute(new Vector3(30f, 0f, 0f), Vector3.right, 5f, 10f, 40f);
            Vector3 none = HitImpulseCalculator.Compute(Vector3.zero, Vector3.right, 5f, 10f, 40f);

            Assert.AreEqual(40f, clamped.magnitude, 1e-4f);
            Assert.AreEqual(Vector3.zero, none);
        }
    }
}
