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
            player.transform.position = visibleHazard.transform.position;
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.That(game.Deaths, Is.EqualTo(1));
            Assert.That((Vector2)player.transform.position, Is.EqualTo(activatedPoint));

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

            player.transform.position = magnet.transform.position;
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            for (var i = 0; i < 8; i++) yield return new WaitForFixedUpdate();

            Assert.That(player.Body.linearVelocity.y, Is.GreaterThan(0f), "Magnet Lift must overcome gravity.");

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
