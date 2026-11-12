using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Events;
using NovaStriker.Gameplay.Obstacles.Portals;
using NovaStriker.Infrastructure.Audio;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Gameplay
{
    public class PortalTests
    {
        [Test]
        public void Budget_AllowsUpToMax_ThenRecoversAfterWindow()
        {
            var budget = new TeleportBudget(3, 2f);

            Assert.IsTrue(budget.TryConsume(0f));
            Assert.IsTrue(budget.TryConsume(0.1f));
            Assert.IsTrue(budget.TryConsume(0.2f));
            Assert.IsFalse(budget.TryConsume(0.3f), "Cuarto viaje seguido: es un bucle.");
            Assert.IsTrue(budget.TryConsume(2.05f), "El primero ya salió de la ventana.");
        }

        [Test]
        public void Budget_RejectsInvalidArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TeleportBudget(0, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TeleportBudget(1, 0f));
        }

        [Test]
        public void ArrivalGate_IgnoresOnlyTheFirstEnterOfTheArrivingBall()
        {
            var gate = new PortalArrivalGate(0.5f);
            object ball = new object();
            gate.Expect(ball, now: 1f);

            Assert.IsFalse(gate.ShouldIgnoreEnter(new object(), 1.02f), "Otro cuerpo no se ve afectado.");
            Assert.IsTrue(gate.ShouldIgnoreEnter(ball, 1.02f), "La llegada no lo devuelve al otro portal.");
            Assert.IsFalse(gate.ShouldIgnoreEnter(ball, 3f), "Si vuelve a entrar más tarde, viaja.");
        }

        [Test]
        public void ArrivalGate_ExpiresIfTheEnterComesLate()
        {
            var gate = new PortalArrivalGate(0.5f);
            object ball = new object();
            gate.Expect(ball, 1f);

            Assert.IsFalse(gate.ShouldIgnoreEnter(ball, 2f));
        }

        [Test]
        public void Teleport_PlaysPortalSound()
        {
            var bus = new EventBus();
            var audio = new RecordingAudio();
            using (new AudioEventRouter(bus, audio))
            {
                bus.Publish(new BallTeleportedEvent(Vector3.zero, Vector3.one));
            }

            CollectionAssert.AreEqual(new[] { SfxId.Portal }, audio.Sfx);
            bus.Dispose();
        }

        [Test]
        public void PortalLayer_IsNotAimable()
        {
            Assert.AreEqual(0, GameLayers.AimMask & (1 << GameLayers.Portal), "Apuntar atraviesa los portales.");
            Assert.AreEqual(0, GameLayers.CharacterSupportMask & (1 << GameLayers.Portal));
        }

        private sealed class RecordingAudio : IAudioService
        {
            public readonly List<SfxId> Sfx = new List<SfxId>();
            public void PlaySfx(SfxId id, float intensity = 1f) => Sfx.Add(id);
            public void PlayUi(SfxId id) { }
            public void PlayMusic(MusicId id) { }
            public void StopMusic() { }
            public void SetAmbience(bool enabled) { }
            public void SetDucked(bool ducked) { }
            public void SetPaused(bool paused) { }
            public void SetSuspended(bool suspended) { }
        }
    }
}
