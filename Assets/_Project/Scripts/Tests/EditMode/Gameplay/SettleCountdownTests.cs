using System;
using NovaStriker.Gameplay.Level;
using NUnit.Framework;

namespace NovaStriker.Tests.EditMode.Gameplay
{
    public class SettleCountdownTests
    {
        [Test]
        public void FiresOnce_WhenDurationIsReached()
        {
            var countdown = new SettleCountdown(0.4f);

            Assert.IsFalse(countdown.Step(0.2f));
            Assert.IsFalse(countdown.IsFinished);
            Assert.IsTrue(countdown.Step(0.2f));
            Assert.IsTrue(countdown.IsFinished);
            Assert.IsFalse(countdown.Step(0.2f), "Solo avisa una vez.");
        }

        [Test]
        public void ZeroDuration_FiresOnFirstStep()
        {
            var countdown = new SettleCountdown(0f);

            Assert.IsTrue(countdown.Step(0f));
        }

        [Test]
        public void NegativeDelta_DoesNotGoBackwards()
        {
            var countdown = new SettleCountdown(0.1f);

            Assert.IsFalse(countdown.Step(-5f));
            Assert.IsTrue(countdown.Step(0.1f));
        }

        [Test]
        public void SettleTime_IsShorterThanKnockdownArming()
        {
            Assert.Less(LevelBuilder.SettleTime, NovaStriker.Gameplay.Characters.KnockdownSettings.Default.ArmingDelay,
                "Si el nivel se durmiera después de armar el detector, el asentamiento podría contar como caída.");
        }

        [Test]
        public void NegativeDuration_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SettleCountdown(-1f));
        }
    }
}
