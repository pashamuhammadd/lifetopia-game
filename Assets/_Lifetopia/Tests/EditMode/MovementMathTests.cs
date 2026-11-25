using Lifetopia.Player;
using NUnit.Framework;
using UnityEngine;

namespace Lifetopia.Tests
{
    public class MovementMathTests
    {
        [Test]
        public void KeyboardDiagonal_IsNormalizedToLengthOne()
        {
            Vector2 result = MovementMath.ClampInput(new Vector2(1f, 1f));
            Assert.That(result.magnitude, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(result.x, Is.EqualTo(result.y).Within(1e-5f));
        }

        [Test]
        public void StraightInput_KeepsFullLength()
        {
            Assert.That(MovementMath.ClampInput(Vector2.right).magnitude, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void PartialJoystickTilt_IsPreserved()
        {
            Vector2 result = MovementMath.ClampInput(new Vector2(0.5f, 0f));
            Assert.That(result.x, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void TinyInput_BelowDeadZone_IsZero()
        {
            Assert.That(MovementMath.ClampInput(new Vector2(0.05f, 0.05f), 0.1f), Is.EqualTo(Vector2.zero));
        }

        [TestCase(0f, -1f, FacingDirection.Down)]
        [TestCase(0f, 1f, FacingDirection.Up)]
        [TestCase(-1f, 0f, FacingDirection.Left)]
        [TestCase(1f, 0f, FacingDirection.Right)]
        public void CardinalInput_ResolvesFacing(float x, float y, FacingDirection expected)
        {
            Assert.That(MovementMath.ResolveFacing(new Vector2(x, y), FacingDirection.Down), Is.EqualTo(expected));
        }

        [Test]
        public void NoInput_KeepsCurrentFacing()
        {
            Assert.That(MovementMath.ResolveFacing(Vector2.zero, FacingDirection.Left), Is.EqualTo(FacingDirection.Left));
        }

        [Test]
        public void Diagonal_KeepsAgreeingFacing_NoFlicker()
        {
            Vector2 upRight = new Vector2(0.707f, 0.707f);
            Assert.That(MovementMath.ResolveFacing(upRight, FacingDirection.Right), Is.EqualTo(FacingDirection.Right));
            Assert.That(MovementMath.ResolveFacing(upRight, FacingDirection.Up), Is.EqualTo(FacingDirection.Up));
        }

        [Test]
        public void Diagonal_FromOppositeFacing_TurnsToADiagonalComponent()
        {
            FacingDirection result = MovementMath.ResolveFacing(new Vector2(0.707f, 0.707f), FacingDirection.Down);
            Assert.That(result, Is.EqualTo(FacingDirection.Up).Or.EqualTo(FacingDirection.Right));
        }
    }
}
