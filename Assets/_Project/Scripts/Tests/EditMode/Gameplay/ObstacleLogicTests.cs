using System.Collections.Generic;
using NovaStriker.Gameplay.Characters;
using NovaStriker.Gameplay.Debris;
using NovaStriker.Gameplay.Obstacles;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Gameplay
{
    public class ObstacleLogicTests
    {
        private const float Tolerance = 1e-4f;
        private static readonly Vector3 A = Vector3.zero;
        private static readonly Vector3 B = new Vector3(4f, 0f, 0f);
        private static readonly Vector3 C = new Vector3(4f, 0f, 4f);

        // ---------- PathMotion ----------

        [Test]
        public void PingPong_WaitsThenTravelsThereAndBack()
        {
            // 4 m a 2 m/s = 2 s por tramo; pausa 1 s en cada punto. Vuelta: (1+2)+(1+2) = 6 s.
            var motion = new PathMotion(new[] { A, B }, speed: 2f, waitAtWaypoint: 1f, PathMode.PingPong, smooth: false);

            Assert.AreEqual(6f, motion.CycleDuration, Tolerance);
            AssertNear(A, motion.Evaluate(0.5f), "Esperando en A.");
            AssertNear(new Vector3(2f, 0f, 0f), motion.Evaluate(2f), "Mitad del tramo A→B.");
            AssertNear(B, motion.Evaluate(3.5f), "Esperando en B.");
            AssertNear(new Vector3(2f, 0f, 0f), motion.Evaluate(5f), "Mitad del tramo B→A.");
            AssertNear(A, motion.Evaluate(6.2f), "Vuelve a empezar.");
        }

        [Test]
        public void Loop_ReturnsToFirstPoint()
        {
            var motion = new PathMotion(new[] { A, B, C }, speed: 4f, waitAtWaypoint: 0f, PathMode.Loop, smooth: false);

            // Tramos: A→B 1 s, B→C 1 s, C→A ~1.414 s.
            Assert.AreEqual(2f + Mathf.Sqrt(2f), motion.CycleDuration, Tolerance);
            AssertNear(B, motion.Evaluate(1f), "Llega a B.");
            AssertNear(C, motion.Evaluate(2f), "Llega a C.");
            AssertNear(new Vector3(2f, 0f, 2f), motion.Evaluate(2f + Mathf.Sqrt(2f) * 0.5f), "Mitad de C→A.");
        }

        [Test]
        public void Smooth_KeepsEndpointsAndMidpoint_ButStartsSlower()
        {
            var linear = new PathMotion(new[] { A, B }, 2f, 0f, PathMode.PingPong, smooth: false);
            var smooth = new PathMotion(new[] { A, B }, 2f, 0f, PathMode.PingPong, smooth: true);

            AssertNear(linear.Evaluate(1f), smooth.Evaluate(1f), "Mitad del tramo igual.");
            AssertNear(B, smooth.Evaluate(2f), "Extremo igual.");
            Assert.Less(smooth.Evaluate(0.2f).x, linear.Evaluate(0.2f).x, "Arranca más despacio.");
        }

        [TestCase(1, 2f)]
        [TestCase(2, 0f)]
        public void NoMovement_WithOnePointOrZeroSpeed(int points, float speed)
        {
            Vector3[] waypoints = points == 1 ? new[] { B } : new[] { B, C };
            var motion = new PathMotion(waypoints, speed, 1f, PathMode.PingPong, true);

            Assert.IsFalse(motion.IsMoving);
            AssertNear(B, motion.Evaluate(123f), "Se queda en el primer punto.");
        }

        // ---------- GlassDamage ----------

        [Test]
        public void Glass_CracksTwice_ThenBreaksOnThirdValidHit()
        {
            var glass = new GlassDamage(hitsToBreak: 3, minBallSpeed: 3f, minImpactSpeed: 4f);

            Assert.AreEqual(GlassHitResult.Cracked, glass.RegisterHit(CollisionKind.Ball, 10f));
            Assert.AreEqual(GlassHitResult.Ignored, glass.RegisterHit(CollisionKind.Ball, 2f), "Demasiado flojo.");
            Assert.AreEqual(GlassHitResult.Cracked, glass.RegisterHit(CollisionKind.Impact, 5f), "Una caja también cuenta.");
            Assert.AreEqual(2f / 3f, glass.Damage01, Tolerance);
            Assert.AreEqual(GlassHitResult.Broken, glass.RegisterHit(CollisionKind.Ball, 3f));
            Assert.IsTrue(glass.IsBroken);
            Assert.AreEqual(GlassHitResult.Ignored, glass.RegisterHit(CollisionKind.Ball, 50f), "Roto: ya no cuenta.");
        }

        [Test]
        public void Glass_IgnoresNonHittingLayers_AndClampsHitsToAtLeastOne()
        {
            var glass = new GlassDamage(hitsToBreak: 0, minBallSpeed: 1f, minImpactSpeed: 1f);

            Assert.AreEqual(1, glass.HitsToBreak);
            Assert.AreEqual(GlassHitResult.Ignored, glass.RegisterHit(CollisionKind.Ignore, 100f));
            Assert.AreEqual(GlassHitResult.Broken, glass.RegisterHit(CollisionKind.Ball, 1f));
        }

        // ---------- ShardLayout ----------

        [Test]
        public void ShardLayout_CoversThePlateWithoutGaps()
        {
            var shards = new List<ShardLayout.Shard>();
            ShardLayout.Compute(new Vector3(3f, 2f, 0.1f), columns: 3, rows: 2, shards);

            Assert.AreEqual(6, shards.Count);
            AssertNear(new Vector3(1f, 1f, 0.1f), shards[0].Size, "Tamaño de cada fragmento.");
            AssertNear(new Vector3(-1f, -0.5f, 0f), shards[0].LocalCenter, "Esquina inferior izquierda.");
            AssertNear(new Vector3(1f, 0.5f, 0f), shards[5].LocalCenter, "Esquina superior derecha.");
        }

        [Test]
        public void ShardLayout_ClampsToAtLeastOneShard()
        {
            var shards = new List<ShardLayout.Shard>();
            ShardLayout.Compute(new Vector3(2f, 2f, 0.1f), columns: 0, rows: -3, shards);

            Assert.AreEqual(1, shards.Count);
            AssertNear(Vector3.zero, shards[0].LocalCenter, "Un solo fragmento centrado.");
        }

        private static void AssertNear(Vector3 expected, Vector3 actual, string message)
        {
            Assert.Less(Vector3.Distance(expected, actual), 1e-3f, $"{message} Esperado {expected}, real {actual}.");
        }
    }
}
