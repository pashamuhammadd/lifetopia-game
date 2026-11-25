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
    }
}
