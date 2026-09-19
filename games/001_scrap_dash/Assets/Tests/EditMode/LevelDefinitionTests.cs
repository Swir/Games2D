using NUnit.Framework;

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
            Assert.That(LevelDefinition.CheckpointCount, Is.GreaterThan(0));
        }
    }
}
