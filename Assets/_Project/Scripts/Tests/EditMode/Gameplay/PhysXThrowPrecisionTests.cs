using NovaStriker.Gameplay.Throwing;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Gameplay
{
    /// <summary>
    /// Riesgo 3b: comprueba con PhysX real (Physics.Simulate en modo Script) que el balón pasa por el objetivo.
    /// El balón no tiene collider, así que no choca con nada de la escena abierta.
    /// Imprime el error medido de cada caso en la consola del Test Runner.
    /// </summary>
    public class PhysXThrowPrecisionTests
    {
        private const float MaxErrorMeters = 0.05f;

        private SimulationMode _previousMode;
        private GameObject _ballObject;
        private Rigidbody _ball;

        [SetUp]
        public void SetUp()
        {
            _previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            _ballObject = new GameObject("PrecisionTestBall");
            _ball = _ballObject.AddComponent<Rigidbody>();
            _ball.mass = 0.43f;
            _ball.linearDamping = 0f;
            _ball.angularDamping = 0f;
            _ball.useGravity = true;
            _ball.interpolation = RigidbodyInterpolation.None;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_ballObject);
            Physics.simulationMode = _previousMode;
        }

        private static IThrowStrategy CreateStrategy(string id)
        {
            switch (id)
            {
                case StraightThrowStrategy.StrategyId: return new StraightThrowStrategy(StraightThrowSettings.Default);
                case LobThrowStrategy.StrategyId: return new LobThrowStrategy(LobThrowSettings.Default);
                default: return new CurveThrowStrategy(CurveThrowSettings.Default);
            }
        }

        [TestCase(StraightThrowStrategy.StrategyId, 0f, 0f, 8f, 0f)]
        [TestCase(StraightThrowStrategy.StrategyId, 3f, 1f, 20f, 0f)]
        [TestCase(LobThrowStrategy.StrategyId, 0f, 0f, 12f, 0f)]
        [TestCase(LobThrowStrategy.StrategyId, -4f, 2f, 25f, 0f)]
        [TestCase(CurveThrowStrategy.StrategyId, 0f, 0f, 15f, 1f)]
        [TestCase(CurveThrowStrategy.StrategyId, 2f, 1f, 22f, -0.6f)]
        public void RealPhysics_BallPassesThroughTarget(string strategyId, float x, float y, float z, float curveBias)
        {
            float dt = Time.fixedDeltaTime;
            var origin = new Vector3(0f, 0.12f, 0f);
            var target = new Vector3(x, y, z);

            _ballObject.transform.position = origin;
            Physics.SyncTransforms();

            var context = new ThrowContext(origin, target, Physics.gravity, dt, curveBias);
            Assert.IsTrue(CreateStrategy(strategyId).TryCreateThrow(context, out ThrowCommand command));

            _ball.linearVelocity = command.InitialVelocity;
            int steps = Mathf.RoundToInt(command.FlightTime / dt);
            for (int i = 0; i < steps; i++)
            {
                if (command.ExtraAcceleration != Vector3.zero)
                {
                    _ball.AddForce(command.ExtraAcceleration, ForceMode.Acceleration);
                }
                Physics.Simulate(dt);
            }

            float error = Vector3.Distance(_ball.position, target);
            Debug.Log($"[Precisión] {strategyId} → {target} bias {curveBias}: error {error * 100f:0.00} cm en {steps} pasos.");
            Assert.Less(error, MaxErrorMeters, $"Error {error * 100f:0.00} cm");
        }
    }
}
