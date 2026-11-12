using NovaStriker.Contracts.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NovaStriker.Tests.EditMode
{
    /// <summary>
    /// Verifica que la configuración de Etapa 0 se aplicó (menú NovaStriker/Setup/Etapa 0).
    /// </summary>
    public class ProjectSetupTests
    {
        private const string ScenesFolder = "Assets/_Project/Scenes/";

        [TestCase(GameLayers.Ball, GameLayers.BallName)]
        [TestCase(GameLayers.Ragdoll, GameLayers.RagdollName)]
        [TestCase(GameLayers.Obstacle, GameLayers.ObstacleName)]
        [TestCase(GameLayers.Breakable, GameLayers.BreakableName)]
        [TestCase(GameLayers.Environment, GameLayers.EnvironmentName)]
        [TestCase(GameLayers.KillZone, GameLayers.KillZoneName)]
        [TestCase(GameLayers.Debris, GameLayers.DebrisName)]
        [TestCase(GameLayers.AimBackstop, GameLayers.AimBackstopName)]
        public void Layer_HasExpectedIndex(int expectedIndex, string layerName)
        {
            Assert.AreEqual(expectedIndex, LayerMask.NameToLayer(layerName));
        }

        [Test]
        public void AimBackstop_CollidesWithNothing()
        {
            for (int layer = 0; layer < 32; layer++)
            {
                Assert.IsTrue(Physics.GetIgnoreLayerCollision(GameLayers.AimBackstop, layer),
                    $"AimBackstop no debería colisionar con la capa {layer}");
            }
        }

        [TestCase(GameLayers.Ball, GameLayers.Ragdoll, true)]
        [TestCase(GameLayers.Ball, GameLayers.Environment, true)]
        [TestCase(GameLayers.Ball, GameLayers.Debris, false)]
        [TestCase(GameLayers.Ragdoll, GameLayers.Debris, false)]
        [TestCase(GameLayers.Debris, GameLayers.Debris, false)]
        public void CollisionMatrix_IsConfigured(int a, int b, bool shouldCollide)
        {
            Assert.AreEqual(shouldCollide, !Physics.GetIgnoreLayerCollision(a, b));
        }

        [Test]
        public void BuildScenes_AreBootMenuGameInOrder()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Assert.GreaterOrEqual(scenes.Length, 3);
            Assert.AreEqual(ScenesFolder + SceneNames.Boot + ".unity", scenes[0].path);
            Assert.AreEqual(ScenesFolder + SceneNames.MainMenu + ".unity", scenes[1].path);
            Assert.AreEqual(ScenesFolder + SceneNames.Game + ".unity", scenes[2].path);
        }

        [Test]
        public void Orientation_IsPortrait()
        {
            Assert.AreEqual(UIOrientation.Portrait, PlayerSettings.defaultInterfaceOrientation);
        }
    }
}
