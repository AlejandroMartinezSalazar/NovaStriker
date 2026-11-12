using NovaStriker.Gameplay.Throwing;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Gameplay
{
    public class ThrowStrategyTests
    {
        private const float Tolerance = 1e-3f;
        private static readonly Vector3 Origin = new Vector3(0f, 0.12f, 0f);

        private static readonly Vector3[] Targets =
        {
            new Vector3(0f, 0f, 6f),
            new Vector3(-3f, 0.5f, 12f),
            new Vector3(4f, 2f, 20f),
            new Vector3(0f, 6f, 35f),
        };

        private static IThrowStrategy[] AllStrategies() => new IThrowStrategy[]
        {
            new StraightThrowStrategy(StraightThrowSettings.Default),
            new LobThrowStrategy(LobThrowSettings.Default),
            new CurveThrowStrategy(CurveThrowSettings.Default),
        };

        [Test]
        public void EveryStrategy_LandsOnTarget_ForSeveralTargets()
        {
            foreach (IThrowStrategy strategy in AllStrategies())
            {
                foreach (Vector3 target in Targets)
                {
                    foreach (float bias in new[] { -1f, 0f, 0.6f })
                    {
                        Assert.IsTrue(strategy.TryCreateThrow(ThrowTestUtils.Context(Origin, target, bias), out ThrowCommand command));
                        Vector3 landed = ThrowTestUtils.Simulate(Origin, command);
                        Assert.Less(Vector3.Distance(landed, target), Tolerance, $"{strategy.Id} → {target} (bias {bias})");
                    }
                }
            }
        }

        [Test]
        public void EveryStrategy_RejectsTargetEqualToOrigin()
        {
            foreach (IThrowStrategy strategy in AllStrategies())
            {
                Assert.IsFalse(strategy.TryCreateThrow(ThrowTestUtils.Context(Origin, Origin), out _), strategy.Id);
            }
        }

        [Test]
        public void FlightTime_IsWholeNumberOfPhysicsSteps()
        {
            foreach (IThrowStrategy strategy in AllStrategies())
            {
                strategy.TryCreateThrow(ThrowTestUtils.Context(Origin, Targets[2]), out ThrowCommand command);
                float steps = command.FlightTime / ThrowTestUtils.FixedDeltaTime;
                Assert.AreEqual(Mathf.Round(steps), steps, 1e-3f, strategy.Id);
            }
        }

        [Test]
        public void Straight_FlightTimeIsDistanceOverSpeed_Clamped()
        {
            var settings = new StraightThrowSettings { Speed = 20f, MinFlightTime = 0.3f, MaxFlightTime = 1f };
            var strategy = new StraightThrowStrategy(settings);

            strategy.TryCreateThrow(ThrowTestUtils.Context(Vector3.zero, new Vector3(0f, 0f, 12f)), out ThrowCommand mid);
            strategy.TryCreateThrow(ThrowTestUtils.Context(Vector3.zero, new Vector3(0f, 0f, 2f)), out ThrowCommand shortThrow);
            strategy.TryCreateThrow(ThrowTestUtils.Context(Vector3.zero, new Vector3(0f, 0f, 60f)), out ThrowCommand longThrow);

            Assert.AreEqual(0.6f, mid.FlightTime, 1e-4f);
            Assert.AreEqual(0.3f, shortThrow.FlightTime, 1e-4f);
            Assert.AreEqual(1f, longThrow.FlightTime, 1e-4f);
            Assert.AreEqual(Vector3.zero, mid.ExtraAcceleration);
        }

        [Test]
        public void Lob_RisesAtLeastToConfiguredApex()
        {
            var settings = new LobThrowSettings { ApexHeight = 3f, ApexPerMeter = 0f };
            var strategy = new LobThrowStrategy(settings);
            var target = new Vector3(0f, 0.5f, 15f);

            strategy.TryCreateThrow(ThrowTestUtils.Context(Origin, target), out ThrowCommand command);
            ThrowTestUtils.Simulate(Origin, command, ThrowTestUtils.Gravity, ThrowTestUtils.FixedDeltaTime, out float maxY, out _);

            Assert.GreaterOrEqual(maxY, target.y + 3f - 0.05f);
        }

        [Test]
        public void Lob_TakesLongerThanStraight()
        {
            var target = new Vector3(0f, 0f, 15f);
            new LobThrowStrategy(LobThrowSettings.Default).TryCreateThrow(ThrowTestUtils.Context(Origin, target), out ThrowCommand lob);
            new StraightThrowStrategy(StraightThrowSettings.Default).TryCreateThrow(ThrowTestUtils.Context(Origin, target), out ThrowCommand straight);

            Assert.Greater(lob.FlightTime, straight.FlightTime);
        }

        [Test]
        public void Lob_FailsWithoutDownwardGravity()
        {
            var strategy = new LobThrowStrategy(LobThrowSettings.Default);
            var context = new ThrowContext(Origin, new Vector3(0f, 0f, 10f), Vector3.zero, ThrowTestUtils.FixedDeltaTime, 0f);

            Assert.IsFalse(strategy.TryCreateThrow(context, out _));
        }

        [TestCase(1f, 1f)]
        [TestCase(-1f, -1f)]
        [TestCase(0.5f, 1f)]
        public void Curve_PathBowsToTheSideOfTheGesture(float bias, float expectedSign)
        {
            var strategy = new CurveThrowStrategy(CurveThrowSettings.Default);
            var target = new Vector3(0f, 0f, 15f);

            strategy.TryCreateThrow(ThrowTestUtils.Context(Origin, target, bias), out ThrowCommand command);
            ThrowTestUtils.Simulate(Origin, command, ThrowTestUtils.Gravity, ThrowTestUtils.FixedDeltaTime, out _, out float lateralX);

            // Dirección del tiro: +Z; derecha: +X. La trayectoria debe abrirse hacia el lado del gesto.
            Assert.AreEqual(expectedSign, Mathf.Sign(command.InitialVelocity.x));
            Assert.AreEqual(expectedSign, Mathf.Sign(lateralX));
            Assert.Greater(Mathf.Abs(lateralX), 0.2f);
        }

        [Test]
        public void Curve_ResolveSide_UsesDefaultSideInsideDeadZone_AndMinimumFraction()
        {
            var settings = CurveThrowSettings.Default;
            settings.DefaultSide = -1f;
            settings.MinCurveFraction = 0.3f;
            settings.BiasDeadZone = 0.1f;
            var strategy = new CurveThrowStrategy(settings);

            Assert.AreEqual(-0.3f, strategy.ResolveSide(0.05f), 1e-5f);
            Assert.AreEqual(0.3f, strategy.ResolveSide(0.2f), 1e-5f);
            Assert.AreEqual(0.8f, strategy.ResolveSide(0.8f), 1e-5f);
            Assert.AreEqual(-1f, strategy.ResolveSide(-5f), 1e-5f);
        }

        [Test]
        public void Curve_HasLateralAccelerationOnly()
        {
            var strategy = new CurveThrowStrategy(CurveThrowSettings.Default);
            strategy.TryCreateThrow(ThrowTestUtils.Context(Origin, new Vector3(0f, 0f, 15f), 1f), out ThrowCommand command);

            Assert.AreEqual(0f, command.ExtraAcceleration.y, 1e-5f);
            Assert.AreEqual(0f, command.ExtraAcceleration.z, 1e-5f);
            Assert.Less(command.ExtraAcceleration.x, 0f, "Abrirse a la derecha exige acelerar hacia la izquierda.");
        }
    }
}
