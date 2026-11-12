using NovaStriker.Gameplay.Throwing;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Gameplay
{
    public class BallisticSolverTests
    {
        private const float Tolerance = 1e-3f;

        [TestCase(0.5f, 0.02f, 0.5f)]
        [TestCase(0.51f, 0.02f, 0.52f)]
        [TestCase(0.001f, 0.02f, 0.02f)]
        [TestCase(0.7f, 0f, 0.7f)]
        public void QuantizeFlightTime_RoundsUpToWholeSteps(float time, float dt, float expected)
        {
            Assert.AreEqual(expected, BallisticSolver.QuantizeFlightTime(time, dt), 1e-5f);
        }

        [Test]
        public void ContinuousModel_ReachesTargetExactly()
        {
            var origin = new Vector3(0f, 0.12f, 0f);
            var target = new Vector3(3f, 1.5f, 14f);
            var acceleration = new Vector3(2f, -9.81f, 0f);
            const float time = 0.8f;

            Vector3 velocity = BallisticSolver.SolveInitialVelocity(origin, target, acceleration, time, 0f);
            Vector3 reached = BallisticSolver.PositionAt(origin, velocity, acceleration, time, 0f);

            Assert.Less(Vector3.Distance(reached, target), Tolerance);
        }

        [TestCase(0f, 0f)]
        [TestCase(6f, 0f)]
        [TestCase(-10f, 3f)]
        public void DiscreteSimulation_WithCompensation_LandsOnTarget(float lateral, float extraUp)
        {
            var origin = new Vector3(0f, 0.12f, 0f);
            var target = new Vector3(-2f, 0.5f, 18f);
            var extra = new Vector3(lateral, extraUp, 0f);
            Vector3 total = ThrowTestUtils.Gravity + extra;
            float time = BallisticSolver.QuantizeFlightTime(0.93f, ThrowTestUtils.FixedDeltaTime);

            Vector3 velocity = BallisticSolver.SolveInitialVelocity(origin, target, total, time, ThrowTestUtils.FixedDeltaTime);
            var command = new ThrowCommand(velocity, extra, time, Vector3.zero);

            Vector3 landed = ThrowTestUtils.Simulate(origin, command);

            Assert.Less(Vector3.Distance(landed, target), Tolerance);
        }

        [Test]
        public void DiscreteSimulation_WithoutCompensation_WouldMissByCentimeters()
        {
            // Documenta por qué existe la compensación: sin ella el error es de ~½·g·dt·T.
            var origin = Vector3.zero;
            var target = new Vector3(0f, 0f, 20f);
            const float time = 1f;

            Vector3 velocity = BallisticSolver.SolveInitialVelocity(origin, target, ThrowTestUtils.Gravity, time, 0f);
            var command = new ThrowCommand(velocity, Vector3.zero, time, Vector3.zero);

            float error = Vector3.Distance(ThrowTestUtils.Simulate(origin, command), target);

            Assert.Greater(error, 0.05f);
        }

        [Test]
        public void SampleTrajectory_StartsAtOriginAndEndsAtTarget()
        {
            var origin = new Vector3(0f, 0.12f, 0f);
            var target = new Vector3(1f, 0.5f, 12f);
            var strategy = new StraightThrowStrategy(StraightThrowSettings.Default);
            strategy.TryCreateThrow(ThrowTestUtils.Context(origin, target), out ThrowCommand command);
            var buffer = new Vector3[16];

            int count = BallisticSolver.SampleTrajectory(origin, command, ThrowTestUtils.Gravity, ThrowTestUtils.FixedDeltaTime, buffer);

            Assert.AreEqual(16, count);
            Assert.Less(Vector3.Distance(buffer[0], origin), Tolerance);
            Assert.Less(Vector3.Distance(buffer[15], target), Tolerance);
        }
    }
}
