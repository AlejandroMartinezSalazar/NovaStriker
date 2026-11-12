using System;
using System.Collections.Generic;
using NovaStriker.Gameplay.Obstacles;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.Gameplay
{
    public class GlassCrateLayoutTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void TwoBoxes_FitInsideWithGap()
        {
            var layout = new GlassCrateLayout(new Vector3Int(2, 1, 1), boxSize: 1f, thickness: 0.06f, gap: 0.02f);

            Assert.AreEqual(2.06f, layout.Interior.x, Tolerance);
            Assert.AreEqual(1.04f, layout.Interior.y, Tolerance);
            Assert.AreEqual(2.18f, layout.Outer.x, Tolerance);
            Assert.AreEqual(0.58f, layout.CrateCenter.y, Tolerance);
        }

        [Test]
        public void Boxes_RestOnBottomPanel_AndNeverTouchGlassOrEachOther()
        {
            var layout = new GlassCrateLayout(new Vector3Int(2, 2, 1), boxSize: 1f, thickness: 0.06f, gap: 0.02f);
            IReadOnlyList<Vector3> centers = layout.BoxCenters();
            float half = layout.BoxSize * 0.5f;

            Assert.AreEqual(4, centers.Count);
            foreach (Vector3 c in centers)
            {
                // Dentro del hueco interior con al menos la holgura (base del hueco en y = grosor).
                Assert.GreaterOrEqual(c.x - half, -layout.Interior.x * 0.5f + layout.Gap - Tolerance);
                Assert.Less(c.x + half, layout.Interior.x * 0.5f - layout.Gap + Tolerance + 1e-6f);
                Assert.GreaterOrEqual(c.y - half, layout.Thickness + layout.Gap - Tolerance);
                Assert.Less(c.y + half, layout.Thickness + layout.Interior.y - layout.Gap + Tolerance + 1e-6f);
            }

            Assert.AreEqual(layout.Thickness + layout.Gap + half, centers[0].y, Tolerance);
            Assert.AreEqual(layout.BoxSize + layout.Gap, centers[1].x - centers[0].x, Tolerance, "Vecinas separadas por la holgura.");
        }

        [Test]
        public void SixPanels_EncloseTheInterior()
        {
            var layout = new GlassCrateLayout(new Vector3Int(1, 1, 1), boxSize: 1f, thickness: 0.1f, gap: 0f);
            IReadOnlyList<GlassCrateLayout.Panel> panels = layout.Panels();

            Assert.AreEqual(6, panels.Count);
            foreach (GlassCrateLayout.Panel panel in panels)
            {
                // El grosor siempre va en Z local: así DebrisPool reparte bien los fragmentos.
                Assert.AreEqual(layout.Thickness, panel.Scale.z, Tolerance, panel.Name);
                // El centro del panel queda a media pared del hueco: (interior + grosor) / 2 en su eje.
                float distance = panel.LocalPosition.magnitude;
                Assert.AreEqual((layout.Interior.x + layout.Thickness) * 0.5f, distance, Tolerance, panel.Name);
            }
        }

        [Test]
        public void Pyramid_TwoBelowAndOneCenteredOnTop()
        {
            var layout = new GlassCrateLayout(new Vector3Int(2, 2, 1), boxSize: 1f, thickness: 0.06f, gap: 0.02f);
            IReadOnlyList<Vector3> centers = layout.PyramidCenters();

            Assert.AreEqual(3, centers.Count);
            Assert.AreEqual(-0.51f, centers[0].x, Tolerance);
            Assert.AreEqual(0.51f, centers[1].x, Tolerance);
            Assert.AreEqual(centers[0].y, centers[1].y, Tolerance);
            Assert.AreEqual(0f, centers[2].x, Tolerance, "La de arriba va centrada sobre las dos.");
            Assert.AreEqual(centers[0].y + layout.BoxSize + layout.Gap, centers[2].y, Tolerance);
            Assert.Less(centers[2].y + 0.5f, layout.Thickness + layout.Interior.y + Tolerance, "Cabe bajo el panel de arriba.");
        }

        [Test]
        public void Bounce_GoesAwayFromCrate_AndUp_WithReducedSpeed()
        {
            Vector3 incoming = new Vector3(0f, 2f, 20f);
            Vector3 bounce = GlassCrateBounce.Compute(new Vector3(0f, 3f, 13.4f), new Vector3(0f, 3.9f, 14f), incoming, 0.4f);

            Assert.Less(bounce.z, 0f, "Vuelve hacia la cámara, lejos de las cajas que caen.");
            Assert.Greater(bounce.y, 0f);
            Assert.AreEqual(incoming.magnitude * 0.4f, bounce.magnitude, 1e-3f);
        }

        [Test]
        public void Bounce_FromStraightAbove_ReturnsTheWayItCame()
        {
            Vector3 bounce = GlassCrateBounce.Compute(new Vector3(0f, 5f, 14f), new Vector3(0f, 3.9f, 14f), new Vector3(1f, -3f, 4f), 0.5f);

            Assert.Less(bounce.z, 0f);
            Assert.Less(bounce.x, 0f);
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GlassCrateLayout(new Vector3Int(0, 1, 1), 1f, 0.06f, 0.02f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GlassCrateLayout(Vector3Int.one, 1f, 0f, 0.02f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GlassCrateLayout(Vector3Int.one, 1f, 0.06f, -0.1f));
        }
    }
}
