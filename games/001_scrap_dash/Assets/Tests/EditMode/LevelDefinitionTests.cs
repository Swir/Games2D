using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ScrapDash.Tests
{
    public sealed class LevelDefinitionTests
    {
        [Test]
        public void LevelOneContainsCompletePlayableLoop()
        {
            Assert.That(LevelDefinition.IsStructurallyComplete(), Is.True);
            Assert.That(LevelDefinition.LevelName, Is.EqualTo("Closing Time Circuit"));
            Assert.That(LevelDefinition.TotalScrap, Is.GreaterThanOrEqualTo(LevelDefinition.ScrapRequiredForFinish));
            Assert.That(LevelDefinition.HazardCount, Is.GreaterThan(0));
            Assert.That(LevelDefinition.EnemyCount, Is.GreaterThan(0));
            Assert.That(LevelDefinition.MovingPlatformCount, Is.GreaterThan(0));
            Assert.That(LevelDefinition.MagnetZoneCount, Is.GreaterThan(0));
            Assert.That(LevelDefinition.SpringPadCount, Is.GreaterThan(0));
            Assert.That(LevelDefinition.CheckpointCount, Is.GreaterThan(0));
        }

        [Test]
        public void RunTimerUsesStableMinuteSecondFormatting()
        {
            Assert.That(ScrapDashGame.FormatTime(65.349f), Is.EqualTo("01:05.34"));
            Assert.That(ScrapDashGame.FormatTime(-1f), Is.EqualTo("00:00.00"));
        }

        [Test]
        public void OriginalGameIconIsAvailableToWindowsBuilds()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/scrap-dash-icon.png");
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.width, Is.GreaterThanOrEqualTo(256));
            Assert.That(icon.height, Is.EqualTo(icon.width));
        }

        [Test]
        public void WindowsDemoUsesTheNewInputSystemAndPlayableDisplayDefaults()
        {
            Assert.That(PlayerSettings.companyName, Is.EqualTo("SWIR"));
            Assert.That(PlayerSettings.productName, Is.EqualTo("SCRAP DASH"));
            Assert.That(PlayerSettings.defaultScreenWidth, Is.EqualTo(1920));
            Assert.That(PlayerSettings.defaultScreenHeight, Is.EqualTo(1080));
            Assert.That(PlayerSettings.fullScreenMode, Is.EqualTo(FullScreenMode.FullScreenWindow));
            Assert.That(PlayerSettings.resizableWindow, Is.True);

            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            Assert.That(assets, Is.Not.Empty, "The project must commit deterministic player settings.");
            var serialized = new SerializedObject(assets[0]);
            var activeInputHandler = serialized.FindProperty("activeInputHandler");
            Assert.That(activeInputHandler, Is.Not.Null);
            Assert.That(activeInputHandler.intValue, Is.EqualTo(1), "Keyboard and gamepad require the new Input System backend.");
        }
    }
}
