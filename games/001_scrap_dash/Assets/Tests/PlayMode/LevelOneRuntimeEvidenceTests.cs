using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace ScrapDash.Tests
{
    public sealed class LevelOneRuntimeEvidenceTests
    {
        private GameObject _root;
        private Keyboard _keyboard;
        private Gamepad _gamepad;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
            if (_gamepad != null && _gamepad.added) InputSystem.RemoveDevice(_gamepad);
            if (_root != null) Object.Destroy(_root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator KeyboardRunJumpAndDashDriveThePlayerController()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            _keyboard = InputSystem.AddDevice<Keyboard>();

            yield return WaitForGrounding(player);
            Assert.That(player.IsGrounded, Is.True, "Player must settle on the start deck.");

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D, Key.Space));
            InputSystem.Update();
            player.SendMessage("Update");
            player.SendMessage("FixedUpdate");

            Assert.That(player.Body.linearVelocity.x, Is.GreaterThan(0f), "D must accelerate right.");
            Assert.That(player.Body.linearVelocity.y, Is.GreaterThan(0f), "Space must jump.");

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.LeftShift));
            InputSystem.Update();
            player.SendMessage("Update");

            Assert.That(player.IsDashing, Is.True, "Left Shift must start a dash.");

        }

        [UnityTest]
        public IEnumerator GamepadMoveAndDashDriveThePlayerController()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            yield return WaitForGrounding(player);
            Assert.That(player.IsGrounded, Is.True, "Player must settle on the start deck.");

            var moveAndJump = new GamepadState { leftStick = Vector2.right }.WithButton(GamepadButton.South);
            InputSystem.QueueStateEvent(_gamepad, moveAndJump);
            InputSystem.Update();
            player.SendMessage("Update");
            player.SendMessage("FixedUpdate");
            Assert.That(player.Body.linearVelocity.x, Is.GreaterThan(0f), "Left stick must accelerate right.");
            Assert.That(player.Body.linearVelocity.y, Is.GreaterThan(0f), "South/A button must jump.");

            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.East));
            InputSystem.Update();
            player.SendMessage("Update");
            Assert.That(player.IsDashing, Is.True, "East/B button must start a dash.");

        }

        [UnityTest]
        public IEnumerator PhysicalScrapTriggersUnlockTheFinishGate()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var game = Object.FindFirstObjectByType<ScrapDashGame>();
            var scrap = Object.FindObjectsByType<ScrapCollectible>(FindObjectsSortMode.None);
            Object.FindFirstObjectByType<PatrolEnemy>().gameObject.SetActive(false);
            yield return null;

            for (var i = 0; i < LevelDefinition.ScrapRequiredForFinish; i++)
            {
                player.transform.position = scrap[i].transform.position;
                player.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            Assert.That(game.Scrap, Is.EqualTo(LevelDefinition.ScrapRequiredForFinish));

            var finish = Object.FindFirstObjectByType<FinishGate>();
            player.transform.position = finish.transform.position;
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.That(game.Won, Is.True, "The real finish trigger must complete the level.");

        }

        [UnityTest]
        public IEnumerator AllFivePhysicalScrapsUpdateTheHudAndRemainCollected()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var game = Object.FindFirstObjectByType<ScrapDashGame>();
            var scrap = Object.FindObjectsByType<ScrapCollectible>(FindObjectsSortMode.None);
            yield return null;

            Assert.That(scrap.Length, Is.EqualTo(LevelDefinition.TotalScrap));
            Assert.That(game.ScrapHudText, Is.EqualTo("SCRAP 0/5"));

            foreach (var piece in scrap)
            {
                player.transform.position = piece.transform.position;
                player.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            Assert.That(game.Scrap, Is.EqualTo(LevelDefinition.TotalScrap));
            Assert.That(game.ScrapRemaining, Is.Zero);
            Assert.That(game.ScrapObjectiveMet, Is.True);
            Assert.That(game.ScrapHudText, Is.EqualTo("SCRAP 5/5"));
            Assert.That(
                Object.FindObjectsByType<ScrapCollectible>(FindObjectsSortMode.None).Length,
                Is.Zero,
                "Every collected part must stay removed for the rest of the run."
            );
        }

        [UnityTest]
        public IEnumerator PhysicalCheckpointThenHazardRespawnsAtTheCheckpoint()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var game = Object.FindFirstObjectByType<ScrapDashGame>();
            var checkpoint = Object.FindFirstObjectByType<Checkpoint>();
            yield return null;

            player.transform.position = checkpoint.transform.position;
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            var activatedPoint = game.Checkpoint;
            Assert.That(activatedPoint.y, Is.GreaterThan(checkpoint.transform.position.y));

            var hazards = Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None);
            Hazard visibleHazard = null;
            foreach (var hazard in hazards)
            {
                if (hazard.name == "ArcPad") visibleHazard = hazard;
            }

            Assert.That(visibleHazard, Is.Not.Null);
            var feedbackBeforeHazard = Object.FindObjectsByType<FeedbackSpark>(FindObjectsSortMode.None).Length;
            player.transform.position = visibleHazard.transform.position;
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.That(game.Deaths, Is.EqualTo(1));
            Assert.That((Vector2)player.transform.position, Is.EqualTo(activatedPoint));
            Assert.That(visibleHazard.TriggerCount, Is.EqualTo(1));
            Assert.That(game.RespawnFlashActive, Is.True, "Fatal contact must provide visible reboot feedback.");
            Assert.That(
                Object.FindObjectsByType<FeedbackSpark>(FindObjectsSortMode.None).Length,
                Is.GreaterThan(feedbackBeforeHazard),
                "Fatal contact must emit its own danger burst."
            );

        }

        [UnityTest]
        public IEnumerator FallingOutOfTheArenaUsesTheSafeCheckpointRecovery()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var game = Object.FindFirstObjectByType<ScrapDashGame>();
            yield return null;

            var safePoint = new Vector2(14.8f, 0.25f);
            game.ActivateCheckpoint(safePoint);
            player.transform.position = new Vector2(8f, -8f);
            player.Body.linearVelocity = new Vector2(4f, -15f);
            yield return null;

            Assert.That(game.Deaths, Is.EqualTo(1));
            Assert.That((Vector2)player.transform.position, Is.EqualTo(safePoint));
            Assert.That(player.Body.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(game.RespawnFlashActive, Is.True);
        }

        [UnityTest]
        public IEnumerator RunawayCartMovesAndMagnetLiftRaisesThePlayer()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var mover = Object.FindFirstObjectByType<MovingPlatform>();
            var magnet = Object.FindFirstObjectByType<MagnetZone>();
            yield return null;

            var moverStart = (Vector2)mover.transform.position;
            for (var i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
            Assert.That(Vector2.Distance(moverStart, mover.transform.position), Is.GreaterThan(0.05f));
            Assert.That(mover.TotalDistanceMoved, Is.GreaterThan(0.05f));
            Assert.That(mover.NormalizedProgress, Is.InRange(0f, 1f));

            player.transform.position = magnet.transform.position;
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            for (var i = 0; i < 8; i++) yield return new WaitForFixedUpdate();

            Assert.That(player.Body.linearVelocity.y, Is.GreaterThan(0f), "Magnet Lift must overcome gravity.");
            Assert.That(magnet.IsEnergized, Is.True, "Magnet Lift must visibly energize while carrying the player.");
            Assert.That(magnet.ActiveRiders, Is.EqualTo(1));
            Assert.That(magnet.Force.y, Is.GreaterThan(0f));

        }

        [UnityTest]
        public IEnumerator PhysicalPatrolEnemyWarnsChargesAndDamagesThePlayer()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var enemy = Object.FindFirstObjectByType<PatrolEnemy>();
            var game = Object.FindFirstObjectByType<ScrapDashGame>();
            yield return null;

            player.transform.position = (Vector2)enemy.transform.position + new Vector2(-2.2f, 0f);
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return null;

            Assert.That(enemy.IsAlerted, Is.True, "Enemy must visibly enter its charge state before contact.");

            player.transform.position = enemy.transform.position;
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(game.Hits, Is.EqualTo(1), "The real enemy trigger must register exactly one hit.");
            Assert.That(game.Integrity, Is.EqualTo(ScrapDashGame.MaxIntegrityValue - 1));
            Assert.That(player.Body.linearVelocity.y, Is.GreaterThan(0f), "Enemy contact must knock the player clear.");
        }

        [UnityTest]
        public IEnumerator FollowCameraKeepsTheCriticalRouteReadableAndSnapsAfterRespawn()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var follow = Object.FindFirstObjectByType<FollowCamera>();
            var camera = follow.GetComponent<Camera>();
            yield return null;

            var cameraStartX = follow.transform.position.x;
            player.transform.position = new Vector2(8f, -1.1f);
            player.Body.linearVelocity = Vector2.right * 8f;
            for (var i = 0; i < 4; i++) yield return null;
            Assert.That(follow.transform.position.x, Is.GreaterThan(cameraStartX + 0.1f), "Camera must smoothly follow forward motion.");
            Assert.That(follow.transform.position.x, Is.LessThan(player.transform.position.x + 3f), "Camera smoothing must not overshoot the player.");

            var routePoints = new[]
            {
                new Vector2(-10f, -1.1f),
                new Vector2(18.2f, 3.35f),
                new Vector2(25.5f, -1.1f)
            };

            foreach (var point in routePoints)
            {
                player.transform.position = point;
                player.Body.linearVelocity = Vector2.zero;
                follow.SnapToTarget();
                var viewport = camera.WorldToViewportPoint(player.transform.position);
                Assert.That(viewport.x, Is.InRange(0.15f, 0.85f), $"Route point {point} must remain horizontally readable.");
                Assert.That(viewport.y, Is.InRange(0.15f, 0.85f), $"Route point {point} must remain vertically readable.");
            }

            var checkpoint = new Vector2(14.8f, 0.25f);
            ScrapDashGame.Instance.ActivateCheckpoint(checkpoint);
            player.transform.position = new Vector2(-20f, -8f);
            ScrapDashGame.Instance.RespawnPlayer();

            Assert.That((Vector2)player.transform.position, Is.EqualTo(checkpoint));
            Assert.That(Vector2.Distance(follow.transform.position, checkpoint), Is.LessThan(4f));
        }

        private static IEnumerator WaitForGrounding(PlayerController player)
        {
            for (var i = 0; i < 20 && !player.IsGrounded; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
