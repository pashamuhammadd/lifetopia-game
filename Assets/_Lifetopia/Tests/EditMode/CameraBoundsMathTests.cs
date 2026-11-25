using Lifetopia.CameraSystem;
using NUnit.Framework;
using UnityEngine;

namespace Lifetopia.Tests
{
    public class CameraBoundsMathTests
    {
        private static readonly Rect Map = new Rect(-16f, -11f, 32f, 22f);

        [Test]
        public void HalfExtents_UseOrthoSizeAndAspect()
        {
            Vector2 half = CameraBoundsMath.HalfExtents(5.5f, 16f / 9f);
            Assert.That(half.y, Is.EqualTo(5.5f).Within(1e-5f));
            Assert.That(half.x, Is.EqualTo(5.5f * 16f / 9f).Within(1e-5f));
        }

        [Test]
        public void InsideMap_IsUnchanged()
        {
            Vector2 half = CameraBoundsMath.HalfExtents(5.5f, 16f / 9f);
            Assert.That(CameraBoundsMath.Clamp(Vector2.zero, Map, half), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void NearCorner_ViewEdgeStopsAtMapEdge()
        {
            Vector2 half = CameraBoundsMath.HalfExtents(5.5f, 16f / 9f);
            Vector2 result = CameraBoundsMath.Clamp(new Vector2(100f, -100f), Map, half);
            Assert.That(result.x + half.x, Is.EqualTo(Map.xMax).Within(1e-4f));
            Assert.That(result.y - half.y, Is.EqualTo(Map.yMin).Within(1e-4f));
        }

        [Test]
        public void MapSmallerThanView_CentresCamera()
        {
            var tiny = new Rect(0f, 0f, 4f, 4f);
            Vector2 result = CameraBoundsMath.Clamp(new Vector2(50f, 50f), tiny, new Vector2(10f, 5f));
            Assert.That(result, Is.EqualTo(new Vector2(2f, 2f)));
        }
    }
}
