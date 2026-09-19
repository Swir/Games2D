using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScrapDash.Tests
{
    public sealed class LevelBootstrapPlayModeTests
    {
        [UnityTest]
        public IEnumerator LevelOneBuildsAllRequiredGameplayObjects()
        {
            var root = ScrapDashBootstrap.BuildLevelForTests();
            yield return null;

            Assert.That(root, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PlayerController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<ScrapDashGame>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<MovingPlatform>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<MagnetZone>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PatrolEnemy>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<Checkpoint>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<FinishGate>(), Is.Not.Null);
            Assert.That(
                Object.FindObjectsByType<ScrapCollectible>(FindObjectsSortMode.None).Length,
                Is.EqualTo(LevelDefinition.TotalScrap)
            );

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BootstrapIsIdempotent()
        {
            var first = ScrapDashBootstrap.BuildLevelForTests();
            var second = ScrapDashBootstrap.BuildLevelForTests();
            yield return null;

            Assert.That(second, Is.SameAs(first));

            Object.Destroy(first);
            yield return null;
        }
    }
}
