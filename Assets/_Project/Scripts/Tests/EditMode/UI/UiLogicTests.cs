using System.Collections.Generic;
using NovaStriker.UI.Common;
using NovaStriker.UI.MainMenu;
using NUnit.Framework;
using UnityEngine;

namespace NovaStriker.Tests.EditMode.UI
{
    public class UiLogicTests
    {
        private const LevelSquareState Done = LevelSquareState.Completed;
        private const LevelSquareState Now = LevelSquareState.Current;
        private const LevelSquareState Todo = LevelSquareState.Locked;

        // ---------- Grupos de 10 niveles ----------

        [TestCase(0, 0)]
        [TestCase(9, 0)]
        [TestCase(10, 10)]
        [TestCase(13, 10)]
        public void Group_StartsAtTheTensOfTheCurrentLevel(int current, int expectedStart)
        {
            var squares = new List<LevelSquareState>();
            Assert.AreEqual(expectedStart, LevelProgressGroup.Build(current, 30, 10, squares));
            Assert.AreEqual(10, squares.Count);
            Assert.AreEqual(Now, squares[current - expectedStart]);
        }

        [Test]
        public void Group_LastGroupOnlyShowsExistingLevels()
        {
            var squares = new List<LevelSquareState>();
            Assert.AreEqual(10, LevelProgressGroup.Build(11, 13, 10, squares));
            CollectionAssert.AreEqual(new[] { Done, Now, Todo }, squares);
        }

        [Test]
        public void Group_ClampsOutOfRangeIndex()
        {
            var squares = new List<LevelSquareState>();
            LevelProgressGroup.Build(99, 3, 10, squares);
            CollectionAssert.AreEqual(new[] { Done, Done, Now }, squares);

            LevelProgressGroup.Build(0, 0, 10, squares);
            Assert.AreEqual(0, squares.Count);
        }

        // ---------- Animación ----------

        [Test]
        public void BackOut_StartsAtZero_OvershootsAndEndsAtOne()
        {
            Assert.AreEqual(0f, UiEasing.BackOut(0f), 1e-4f);
            Assert.AreEqual(1f, UiEasing.BackOut(1f), 1e-4f);
            Assert.AreEqual(1f, UiEasing.BackOut(5f), 1e-4f, "Pasado el final se queda en 1.");
            Assert.Greater(UiEasing.BackOut(0.7f), 1f, "Se pasa un poco antes de asentarse.");
        }

        [Test]
        public void DampedBounce_RestsAtZero_AndFades()
        {
            Assert.AreEqual(0f, UiEasing.DampedBounce(-1f, 2f, 1f), 1e-5f);
            float first = UiEasing.DampedBounce(0.25f, 2f, 1f);
            float later = UiEasing.DampedBounce(2.25f, 2f, 1f);
            Assert.Greater(first, later);
            Assert.GreaterOrEqual(later, 0f);
        }

        // ---------- Zona segura ----------

        [Test]
        public void SafeArea_ConvertsPixelsToAnchors()
        {
            // Pantalla 1000×2000 con muesca arriba (100 px) y barra de gestos abajo (50 px).
            SafeAreaMath.ToAnchors(new Rect(0f, 50f, 1000f, 1850f), new Vector2(1000f, 2000f), out Vector2 min, out Vector2 max);

            Assert.AreEqual(0f, min.x, 1e-5f);
            Assert.AreEqual(0.025f, min.y, 1e-5f);
            Assert.AreEqual(1f, max.x, 1e-5f);
            Assert.AreEqual(0.95f, max.y, 1e-5f);
        }

        [Test]
        public void SafeArea_InvalidScreen_UsesFullScreen()
        {
            SafeAreaMath.ToAnchors(new Rect(0f, 0f, 10f, 10f), Vector2.zero, out Vector2 min, out Vector2 max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }
    }
}
