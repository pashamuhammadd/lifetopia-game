using System.Collections.Generic;
using Lifetopia.Player;
using NUnit.Framework;

namespace Lifetopia.Tests
{
    public class SpriteFrameMathTests
    {
        [Test]
        public void SingleFrame_NeverAdvances()
        {
            Assert.That(SpriteFrameMath.FrameIndex(10f, 9f, 1), Is.EqualTo(0));
        }

        [Test]
        public void ZeroFps_OrNegativeTime_StaysOnFirstFrame()
        {
            Assert.That(SpriteFrameMath.FrameIndex(3f, 0f, 4), Is.EqualTo(0));
            Assert.That(SpriteFrameMath.FrameIndex(-1f, 4f, 4), Is.EqualTo(0));
        }

        [Test]
        public void Idle_FourFramesAtFourFps_StepsEveryQuarterSecond()
        {
            Assert.That(SpriteFrameMath.FrameIndex(0.24f, 4f, 4), Is.EqualTo(0));
            Assert.That(SpriteFrameMath.FrameIndex(0.26f, 4f, 4), Is.EqualTo(1));
            Assert.That(SpriteFrameMath.FrameIndex(0.99f, 4f, 4), Is.EqualTo(3));
        }

        [Test]
        public void Walk_EightFrames_Loops()
        {
            Assert.That(SpriteFrameMath.FrameIndex(1.01f, 8f, 8), Is.EqualTo(0));
            Assert.That(SpriteFrameMath.FrameIndex(1.13f, 8f, 8), Is.EqualTo(1));
        }

        [Test]
        public void Walk_TwelveFps_AdvancesSlotEveryTwelfthSecond()
        {
            Assert.That(SpriteFrameMath.FrameIndex(0.08f, 12f, 8), Is.EqualTo(0));
            Assert.That(SpriteFrameMath.FrameIndex(0.09f, 12f, 8), Is.EqualTo(1));
            Assert.That(SpriteFrameMath.FrameIndex(0.70f, 12f, 8), Is.EqualTo(0));
        }

        [Test]
        public void VisiblyWalking_NeedsIntentAndSpeed()
        {
            Assert.That(SpriteFrameMath.IsVisiblyWalking(true, 4f, 0.5f), Is.True);
            Assert.That(SpriteFrameMath.IsVisiblyWalking(true, 0f, 0.5f), Is.False, "pushing into a wall");
            Assert.That(SpriteFrameMath.IsVisiblyWalking(false, 3f, 0.5f), Is.False, "released, still decelerating");
        }

        private static System.Func<bool, FacingDirection, bool> Has(params (bool, FacingDirection)[] clips)
        {
            var set = new HashSet<(bool, FacingDirection)>(clips);
            return (w, d) => set.Contains((w, d));
        }

        [Test]
        public void Resolve_UsesRequestedClipWhenPresent()
        {
            bool ok = SpriteFrameMath.ResolveClip(true, FacingDirection.Left,
                Has((true, FacingDirection.Left), (false, FacingDirection.Down)), out bool w, out FacingDirection d);
            Assert.That(ok && w && d == FacingDirection.Left);
        }

        [Test]
        public void Resolve_WalkWithoutArt_FallsBackToIdleSameDirection()
        {
            bool ok = SpriteFrameMath.ResolveClip(true, FacingDirection.Up,
                Has((false, FacingDirection.Up), (false, FacingDirection.Down)), out bool w, out FacingDirection d);
            Assert.That(ok && !w && d == FacingDirection.Up);
        }

        [Test]
        public void Resolve_A0StaticOnly_AlwaysShowsIdleDown()
        {
            var a0 = Has((false, FacingDirection.Down));
            foreach (FacingDirection dir in System.Enum.GetValues(typeof(FacingDirection)))
            {
                bool ok = SpriteFrameMath.ResolveClip(true, dir, a0, out bool w, out FacingDirection d);
                Assert.That(ok && !w && d == FacingDirection.Down, dir.ToString());
            }
        }

        [Test]
        public void Resolve_NoArt_ReturnsFalse()
        {
            Assert.That(SpriteFrameMath.ResolveClip(false, FacingDirection.Down, Has(), out _, out _), Is.False);
        }
    }
}
