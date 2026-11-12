using System.Collections.Generic;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.UI.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.UI
{
    public class TutorialHintTests
    {
        private sealed class FakeView : ITutorialHintView
        {
            public readonly List<string> Calls = new List<string>();
            public void Show(Vector3 fromWorld, Vector3 toWorld) => Calls.Add($"Show:{toWorld.z}");
            public void Hide() => Calls.Add("Hide");
        }

        [Test]
        public void Presenter_StartsHidden_AndFollowsEvents()
        {
            var bus = new EventBus();
            var view = new FakeView();
            using (new TutorialHintPresenter(view, bus))
            {
                bus.Publish(new TutorialHintEvent(true, Vector3.zero, new Vector3(0f, 1f, 12f)));
                bus.Publish(TutorialHintEvent.Hidden);
            }

            CollectionAssert.AreEqual(new[] { "Hide", "Show:12", "Hide" }, view.Calls);
        }

        [Test]
        public void Animation_StartsAtOrigin_TouchesWhileDragging_AndEndsAtTarget()
        {
            DragHintAnimation.Frame start = DragHintAnimation.Evaluate(0f);
            Assert.AreEqual(0f, start.Progress, 1e-4f);
            Assert.AreEqual(0f, start.HandAlpha, 1e-4f, "Aparece con un fundido.");
            Assert.IsFalse(start.Touching);

            float midDrag = DragHintAnimation.Appear + DragHintAnimation.Press + DragHintAnimation.Drag * 0.5f;
            DragHintAnimation.Frame drag = DragHintAnimation.Evaluate(midDrag);
            Assert.IsTrue(drag.Touching);
            Assert.AreEqual(0.5f, drag.Progress, 1e-3f);
            Assert.AreEqual(1f, drag.HandAlpha, 1e-4f);

            float release = midDrag + DragHintAnimation.Drag * 0.5f + DragHintAnimation.Hold + DragHintAnimation.Release * 0.5f;
            DragHintAnimation.Frame up = DragHintAnimation.Evaluate(release);
            Assert.AreEqual(1f, up.Progress, 1e-4f);
            Assert.IsFalse(up.Touching, "Suelta en el futbolista.");
            Assert.IsTrue(up.RingAtEnd);
            Assert.Greater(up.RingAlpha, 0f);
        }

        [Test]
        public void Animation_Loops()
        {
            float t = 0.7f;
            Assert.AreEqual(DragHintAnimation.Evaluate(t).Progress,
                DragHintAnimation.Evaluate(t + DragHintAnimation.CycleDuration).Progress, 1e-4f);
        }
    }
}
