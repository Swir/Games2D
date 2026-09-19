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
            Assert.That(Object.FindFirstObjectByType<FeedbackHub>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<MovingPlatform>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<MagnetZone>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PatrolEnemy>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<Checkpoint>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<FinishGate>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<TrailRenderer>(), Is.Not.Null);
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

        [UnityTest]
        public IEnumerator FinishRequiresScrapThenLocksInWinState()
        {
            var root = ScrapDashBootstrap.BuildLevelForTests();
            var game = Object.FindFirstObjectByType<ScrapDashGame>();
            yield return null;

            game.TryFinish();
            Assert.That(game.Won, Is.False);

            for (var i = 0; i < LevelDefinition.ScrapRequiredForFinish; i++)
            {
                game.CollectScrap();
            }

            game.TryFinish();
            Assert.That(game.Won, Is.True);
            Assert.That(game.BlocksPlayerControl, Is.True);

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CheckpointRespawnRestoresPlayerPositionAndMotion()
        {
            var root = ScrapDashBootstrap.BuildLevelForTests();
            var game = Object.FindFirstObjectByType<ScrapDashGame>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var checkpoint = new Vector2(9.25f, 1.5f);
            yield return null;

            game.ActivateCheckpoint(checkpoint);
            player.transform.position = new Vector2(-20f, -20f);
            player.Body.linearVelocity = new Vector2(8f, -12f);
            game.RespawnPlayer();

            Assert.That((Vector2)player.transform.position, Is.EqualTo(checkpoint));
            Assert.That(player.Body.linearVelocity, Is.EqualTo(Vector2.zero));

            Object.Destroy(root);
            yield return null;
        }
    }
}
