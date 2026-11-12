using NovaStriker.Contracts.Data;
using NovaStriker.Gameplay.Ball;
using NovaStriker.Gameplay.Input;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Gameplay
{
    public class SwipeAndSettleTests
    {
        private static SwipeData Swipe(params Vector2[] points)
        {
            var tracker = new SwipeGestureTracker(fullCurveRatio: 0.25f, minChordPixels: 40f);
            tracker.Begin(points[0], 0f);
            for (int i = 1; i < points.Length - 1; i++)
            {
                tracker.AddPoint(points[i]);
            }
            return tracker.End(points[points.Length - 1], 0.5f);
        }

        [Test]
        public void StraightSwipe_HasNoCurve()
        {
            SwipeData swipe = Swipe(new Vector2(500, 200), new Vector2(500, 600), new Vector2(500, 1000));
            Assert.AreEqual(0f, swipe.CurveBias, 1e-4f);
            Assert.AreEqual(0.5f, swipe.Duration, 1e-5f);
        }

        [Test]
        public void SwipeBowedRight_IsPositive()
        {
            // Hacia arriba, arqueado a la derecha 100 px en una cuerda de 800 px → 100 / (0.25·800) = 0.5.
            SwipeData swipe = Swipe(new Vector2(500, 200), new Vector2(600, 600), new Vector2(500, 1000));
            Assert.AreEqual(0.5f, swipe.CurveBias, 1e-4f);
        }

        [Test]
        public void SwipeBowedLeft_IsNegative_AndClamped()
        {
            SwipeData swipe = Swipe(new Vector2(500, 200), new Vector2(100, 600), new Vector2(500, 1000));
            Assert.AreEqual(-1f, swipe.CurveBias, 1e-4f);
        }

        [Test]
        public void ShortSwipe_HasNoCurve()
        {
            SwipeData swipe = Swipe(new Vector2(500, 200), new Vector2(520, 210), new Vector2(500, 220));
            Assert.AreEqual(0f, swipe.CurveBias, 1e-4f);
        }

        [Test]
        public void EvaluateCurveBias_WorksWhileDragging()
        {
            var tracker = new SwipeGestureTracker(0.25f, 40f);
            tracker.Begin(new Vector2(500, 200), 0f);
            tracker.AddPoint(new Vector2(600, 600));

            Assert.AreEqual(0.5f, tracker.EvaluateCurveBias(new Vector2(500, 1000)), 1e-4f);
        }

        [Test]
        public void SettleDetector_Stopped_AfterContinuousSlowTime()
        {
            var detector = new BallSettleDetector(settleSpeed: 0.2f, settleDuration: 0.5f, maxLifetime: 10f);
            detector.Begin();

            Assert.AreEqual(BallSettleReason.None, detector.Step(5f, 0.3f));
            Assert.AreEqual(BallSettleReason.None, detector.Step(0.1f, 0.3f));
            Assert.AreEqual(BallSettleReason.None, detector.Step(1f, 0.3f), "Moverse reinicia la cuenta.");
            Assert.AreEqual(BallSettleReason.None, detector.Step(0.1f, 0.3f));
            Assert.AreEqual(BallSettleReason.Stopped, detector.Step(0.1f, 0.3f));
            Assert.IsFalse(detector.IsActive);
            Assert.AreEqual(BallSettleReason.None, detector.Step(0f, 1f), "Tras terminar no vuelve a reportar.");
        }

        [Test]
        public void SettleDetector_Timeout()
        {
            var detector = new BallSettleDetector(0.2f, 0.5f, maxLifetime: 1f);
            detector.Begin();

            Assert.AreEqual(BallSettleReason.None, detector.Step(3f, 0.6f));
            Assert.AreEqual(BallSettleReason.Timeout, detector.Step(3f, 0.6f));
        }

        [Test]
        public void SettleDetector_InactiveUntilBegin()
        {
            var detector = new BallSettleDetector(0.2f, 0.1f, 1f);
            Assert.AreEqual(BallSettleReason.None, detector.Step(0f, 5f));
        }
    }
}
