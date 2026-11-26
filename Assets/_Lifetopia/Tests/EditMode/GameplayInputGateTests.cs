using Lifetopia.Core;
using NUnit.Framework;
using UnityEngine;

namespace Lifetopia.Tests
{
    public class GameplayInputGateTests
    {
        private GameObject _go;
        private GameplayInputGate _gate;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("GateTest");
            _gate = _go.AddComponent<GameplayInputGate>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void TwoBlockers_StayBlockedUntilBothRelease()
        {
            var a = new GameObject("A");
            var b = new GameObject("B");
            int events = 0;
            _gate.BlockedChanged += _ => events++;

            _gate.Block(a);
            _gate.Block(b);
            _gate.Unblock(a);
            Assert.That(_gate.IsBlocked, Is.True);

            _gate.Unblock(b);
            Assert.That(_gate.IsBlocked, Is.False);
            Assert.That(events, Is.EqualTo(2), "Only the first block and last unblock should notify.");

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void NullBlocker_IsIgnored()
        {
            _gate.Block(null);
            Assert.That(_gate.IsBlocked, Is.False);
        }

        [Test]
        public void MenuBlock_PausesGameTime_ByDefault()
        {
            var bag = new GameObject("Bag");
            _gate.Block(bag);
            Assert.That(_gate.IsBlocked, Is.True);
            Assert.That(_gate.PausesGameTime, Is.True, "bag/dialogue keep the M3.2 behaviour");
            _gate.Unblock(bag);
            Assert.That(_gate.PausesGameTime, Is.False);
            Object.DestroyImmediate(bag);
        }

        [Test]
        public void MovementOnlyBlock_DoesNotPauseGameTime()
        {
            var fishing = new GameObject("Fishing");
            int timeEvents = 0, blockEvents = 0;
            _gate.TimePauseChanged += _ => timeEvents++;
            _gate.BlockedChanged += _ => blockEvents++;
            _gate.Block(fishing, pausesGameTime: false);
            Assert.That(_gate.IsBlocked, Is.True, "movement and world interaction are locked");
            Assert.That(_gate.PausesGameTime, Is.False, "game time keeps running while fishing");
            Assert.That(timeEvents, Is.EqualTo(0));
            _gate.Unblock(fishing);
            Assert.That(_gate.IsBlocked, Is.False);
            Assert.That(blockEvents, Is.EqualTo(2));
            Object.DestroyImmediate(fishing);
        }

        [Test]
        public void MixedBlockers_TimePausesOnlyWhileAPausingBlockerIsActive()
        {
            var fishing = new GameObject("Fishing");
            var dialogue = new GameObject("Dialogue");
            var seen = new System.Collections.Generic.List<bool>();
            _gate.TimePauseChanged += v => seen.Add(v);
            _gate.Block(fishing, false);
            _gate.Block(dialogue);
            Assert.That(_gate.PausesGameTime, Is.True);
            _gate.Unblock(dialogue);
            Assert.That(_gate.PausesGameTime, Is.False);
            Assert.That(_gate.IsBlocked, Is.True, "fishing still holds the movement lock");
            _gate.Unblock(fishing);
            CollectionAssert.AreEqual(new[] { true, false }, seen);
            Object.DestroyImmediate(fishing);
            Object.DestroyImmediate(dialogue);
        }
    }
}
