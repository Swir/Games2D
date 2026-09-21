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
        public IEnumerator ReleasingJumpEarlyCutsUpwardVelocity()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return WaitForGrounding(player);

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            InputSystem.Update();
            player.SendMessage("Update");
            player.SendMessage("FixedUpdate");
            var heldVelocity = player.Body.linearVelocity.y;

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.Update();
            player.SendMessage("Update");

            Assert.That(player.JumpCount, Is.EqualTo(1));
            Assert.That(player.JumpCutCount, Is.EqualTo(1));
            Assert.That(player.Body.linearVelocity.y, Is.GreaterThan(0f));
            Assert.That(player.Body.linearVelocity.y, Is.LessThanOrEqualTo(heldVelocity * 0.55f));
        }

        [UnityTest]
        public IEnumerator CoyoteTimeAcceptsJumpJustAfterLeavingTheDeck()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return WaitForGrounding(player);

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.Update();
            player.SendMessage("Update");
            player.Bounce(0.8f);
            Assert.That(player.IsGrounded, Is.False);

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            InputSystem.Update();
            player.SendMessage("Update");
            player.SendMessage("FixedUpdate");

            Assert.That(player.JumpCount, Is.EqualTo(1));
            Assert.That(player.LastJumpUsedCoyoteTime, Is.True);
            Assert.That(player.Body.linearVelocity.y, Is.GreaterThan(8f));
        }

        [UnityTest]
        public IEnumerator BufferedJumpFiresWhenThePlayerTouchesDown()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return WaitForGrounding(player);

            player.Bounce(4f);
            for (var i = 0; i < 8; i++)
            {
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            Assert.That(player.IsGrounded, Is.False);
            Assert.That(player.JumpCount, Is.Zero, "The bounce itself is not a player jump.");
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            InputSystem.Update();
            player.SendMessage("Update");

            for (var i = 0; i < 10 && player.BufferedJumpCount == 0; i++)
            {
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            Assert.That(player.BufferedJumpCount, Is.EqualTo(1), "A pre-landing press must fire on touchdown.");
            Assert.That(player.JumpCount, Is.EqualTo(1));
            Assert.That(player.Body.linearVelocity.y, Is.GreaterThan(0f));
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
        public IEnumerator KeyboardRunJumpAndDashCrossTheFirstElectricGap()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var game = Object.FindFirstObjectByType<ScrapDashGame>();
            _keyboard = InputSystem.AddDevice<Keyboard>();

            player.transform.position = new Vector2(-5.35f, -1.1f);
            player.ResetMotion();
            Physics2D.SyncTransforms();
            yield return WaitForGrounding(player, 40);
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(game.DashHudText, Is.EqualTo("DASH READY"));

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D, Key.Space));
            InputSystem.Update();
            player.SendMessage("Update");
            for (var i = 0; i < 4; i++) yield return new WaitForFixedUpdate();

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D, Key.LeftShift));
            InputSystem.Update();
            player.SendMessage("Update");
            Assert.That(player.IsDashing, Is.True);
            Assert.That(game.DashHudText, Is.EqualTo("DASH BURST"));

            for (var i = 0; i < 18; i++) yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(player.transform.position.x, Is.GreaterThan(-3.05f),
                "Real keyboard movement must carry the player across ElectricPitA.");
            Assert.That(game.Deaths, Is.Zero, "The intended jump-dash route must clear the hazard.");
            Assert.That(player.DashCount, Is.EqualTo(1));
            Assert.That(player.TotalDashDistance, Is.GreaterThan(2.5f));
        }

        [UnityTest]
        public IEnumerator GroundDashRunsItsFullBurstAndRestoresNormalPhysics()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return WaitForGrounding(player);
            var normalGravity = player.Body.gravityScale;

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.LeftShift));
            InputSystem.Update();
            player.SendMessage("Update");

            Assert.That(player.DashCount, Is.EqualTo(1));
            Assert.That(player.AirDashCount, Is.Zero);
            Assert.That(player.IsDashing, Is.True);
            Assert.That(player.DashTrailEmitting, Is.True);
            Assert.That(player.Body.gravityScale, Is.Zero);

            for (var i = 0; i < 10; i++) player.SendMessage("FixedUpdate");

            Assert.That(player.IsDashing, Is.False, "The burst must end instead of locking movement.");
            Assert.That(player.TotalDashDistance, Is.GreaterThan(2.5f), "A dash must cover a meaningful gap.");
            Assert.That(player.Body.gravityScale, Is.EqualTo(normalGravity));
            Assert.That(player.DashTrailEmitting, Is.False);
        }

        [UnityTest]
        public IEnumerator PhysicalDashAttackRecyclesThePatrolEnemy()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var enemy = Object.FindFirstObjectByType<PatrolEnemy>();
            var game = Object.FindFirstObjectByType<ScrapDashGame>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return WaitForGrounding(player);

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.LeftShift));
            InputSystem.Update();
            player.SendMessage("Update");
            Assert.That(player.IsDashing, Is.True);

            player.transform.position = enemy.transform.position;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(game.EnemiesDefeated, Is.EqualTo(1), "Dash contact must defeat the enemy.");
            Assert.That(enemy == null, Is.True, "The defeated enemy must leave the route.");
            Assert.That(game.Hits, Is.Zero, "A successful dash attack must not damage the player.");
        }

        [UnityTest]
        public IEnumerator BounceImmediatelyRefreshesTheSingleAirDash()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return WaitForGrounding(player);

            player.Bounce(6f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.LeftShift));
            InputSystem.Update();
            player.SendMessage("Update");
            Assert.That(player.AirDashCount, Is.EqualTo(1));
            Assert.That(player.AirDashReady, Is.False);

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.Update();
            player.SendMessage("Update");
            player.Bounce(6f);
            Assert.That(player.AirDashReady, Is.True, "A spring or stomp bounce must recharge air dash.");

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.LeftShift));
            InputSystem.Update();
            player.SendMessage("Update");
            Assert.That(player.IsDashing, Is.True);
            Assert.That(player.DashCount, Is.EqualTo(2));
            Assert.That(player.AirDashCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ObjectivePointerGuidesScrapCollectionThenSwitchesToTheExit()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var game = Object.FindFirstObjectByType<ScrapDashGame>();
            var pointer = Object.FindFirstObjectByType<ObjectivePointer>();
            Object.FindFirstObjectByType<PatrolEnemy>().gameObject.SetActive(false);
            yield return null;

            Assert.That(pointer, Is.Not.Null);
            Assert.That(pointer.Visible, Is.True);
            Assert.That(pointer.IsPointingToExit, Is.False);
            Assert.That(pointer.CurrentTargetName, Does.StartWith("Scrap_"));

            for (var i = 0; i < LevelDefinition.ScrapRequiredForFinish; i++)
            {
                pointer.RefreshTarget();
                var target = GameObject.Find(pointer.CurrentTargetName);
                Assert.That(target, Is.Not.Null, "The pointer must always select a remaining physical scrap.");
                player.transform.position = target.transform.position;
                player.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            pointer.RefreshTarget();
            pointer.SendMessage("Update");

            Assert.That(game.ScrapObjectiveMet, Is.True);
            Assert.That(pointer.IsPointingToExit, Is.True, "The pointer must switch goals as soon as the scrap objective is met.");
            Assert.That(pointer.CurrentTargetName, Is.EqualTo("FinishGate"));
            Assert.That(pointer.TargetDistance, Is.GreaterThan(0f));
            Assert.That(pointer.TargetChangeCount, Is.GreaterThanOrEqualTo(LevelDefinition.ScrapRequiredForFinish + 1));
        }

        [UnityTest]
        public IEnumerator PhysicalScrapTriggersUnlockTheFinishGate()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var game = Object.FindFirstObjectByType<ScrapDashGame>();
            var scrap = Object.FindObjectsByType<ScrapCollectible>(FindObjectsSortMode.None);
            var finish = Object.FindFirstObjectByType<FinishGate>();
            Object.FindFirstObjectByType<PatrolEnemy>().gameObject.SetActive(false);
            yield return null;

            Assert.That(finish.IsUnlocked, Is.False);

            for (var i = 0; i < LevelDefinition.ScrapRequiredForFinish; i++)
            {
                player.transform.position = scrap[i].transform.position;
                player.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            Assert.That(game.Scrap, Is.EqualTo(LevelDefinition.ScrapRequiredForFinish));
            Assert.That(finish.IsUnlocked, Is.True);

            player.transform.position = finish.transform.position;
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.That(game.Won, Is.True, "The real finish trigger must complete the level.");
            Assert.That(finish.EntryAttempts, Is.EqualTo(1));

        }

        [UnityTest]
        public IEnumerator CompletePhysicalLoopLocksThenRebootsAndReachesTheWinScreen()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var game = Object.FindFirstObjectByType<ScrapDashGame>();
            var finish = Object.FindFirstObjectByType<FinishGate>();
            var checkpoint = Object.FindFirstObjectByType<Checkpoint>();
            var scrap = Object.FindObjectsByType<ScrapCollectible>(FindObjectsSortMode.None);
            Object.FindFirstObjectByType<PatrolEnemy>().gameObject.SetActive(false);
            yield return null;

            player.transform.position = finish.transform.position;
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(game.Won, Is.False, "The finish must stay locked before the scrap objective.");
            Assert.That(finish.LockedAttempts, Is.EqualTo(1));
            Assert.That(player.Body.linearVelocity.x, Is.LessThan(0f), "A locked gate must push the player back into the level.");
            Assert.That(player.Body.linearVelocity.y, Is.GreaterThan(0f), "Gate rejection must clear the trigger safely.");
            Assert.That(game.Hits, Is.Zero, "Gate rejection is guidance, not combat damage.");

            player.transform.position = checkpoint.transform.position;
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(checkpoint.Activated, Is.True);
            Assert.That(checkpoint.ActivationCount, Is.EqualTo(1));

            var hazards = Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None);
            Hazard arcPad = null;
            foreach (var hazard in hazards)
            {
                if (hazard.name == "ArcPad") arcPad = hazard;
            }

            Assert.That(arcPad, Is.Not.Null);
            player.transform.position = arcPad.transform.position;
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(game.Deaths, Is.EqualTo(1));
            Assert.That((Vector2)player.transform.position, Is.EqualTo(game.Checkpoint));

            foreach (var piece in scrap)
            {
                player.transform.position = piece.transform.position;
                player.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            Assert.That(game.Scrap, Is.EqualTo(LevelDefinition.TotalScrap));
            Assert.That(finish.IsUnlocked, Is.True);

            player.transform.position = finish.transform.position;
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.That(game.Won, Is.True, "The complete physical loop must reach the win state.");
            Assert.That(finish.EntryAttempts, Is.EqualTo(2));
            Assert.That(finish.LockedAttempts, Is.EqualTo(1));
            Assert.That(game.CenterPanelVisible, Is.True);
            Assert.That(game.CenterPanelTitle, Does.Contain("LEVEL 1 COMPLETE"));
            Assert.That(game.CenterPanelSubtitle, Does.Contain("R / Back to replay"));
        }

        [UnityTest]
        public IEnumerator KeyboardAndGamepadPauseAndRestartControlsAreWired()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var game = Object.FindFirstObjectByType<ScrapDashGame>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            yield return null;

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape));
            InputSystem.Update();
            game.SendMessage("Update");
            Assert.That(game.Paused, Is.True);
            Assert.That(game.BlocksPlayerControl, Is.True);
            Assert.That(game.CenterPanelTitle, Is.EqualTo("PAUSED"));
            Assert.That(Time.timeScale, Is.Zero);

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.Update();
            game.SendMessage("Update");
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.Start));
            InputSystem.Update();
            game.SendMessage("Update");
            Assert.That(game.Paused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            InputSystem.QueueStateEvent(_gamepad, new GamepadState());
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.R));
            InputSystem.Update();
            game.SendMessage("Update");
            Assert.That(game.RestartRequests, Is.EqualTo(1), "R must request a safe level reload.");

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.Select));
            InputSystem.Update();
            game.SendMessage("Update");
            Assert.That(game.RestartRequests, Is.EqualTo(2), "Gamepad Back must request a safe level reload.");
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
            Assert.That(checkpoint.Activated, Is.True);
            Assert.That(checkpoint.ActivationCount, Is.EqualTo(1));

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

            Assert.That((Vector2)mover.transform.position, Is.EqualTo(new Vector2(2.4f, -1.4f)),
                "The cart must begin exactly on its authored rail instead of snapping on the first physics tick.");
            var moverStart = (Vector2)mover.transform.position;
            for (var i = 0; i < 34; i++) yield return new WaitForFixedUpdate();
            Assert.That(Vector2.Distance(moverStart, mover.transform.position), Is.GreaterThan(0.05f));
            Assert.That(mover.TotalDistanceMoved, Is.GreaterThan(0.05f));
            Assert.That(mover.NormalizedProgress, Is.InRange(0f, 1f));

            for (var i = 0; i < 80 && mover.EndpointPauseCount == 0; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(mover.EndpointPauseCount, Is.EqualTo(1), "The cart must stop at the far platform once per trip.");
            Assert.That(mover.IsWaitingAtEndpoint, Is.True, "The endpoint dwell gives the player a safe boarding window.");
            Assert.That(mover.Velocity, Is.EqualTo(Vector2.zero));

            player.transform.position = (Vector2)magnet.transform.position + new Vector2(0.65f, 0f);
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            var horizontalDistanceBeforeLift = Mathf.Abs(player.transform.position.x - magnet.transform.position.x);
            for (var i = 0; i < 8; i++) yield return new WaitForFixedUpdate();

            Assert.That(player.Body.linearVelocity.y, Is.GreaterThan(0f), "Magnet Lift must overcome gravity.");
            Assert.That(Mathf.Abs(player.transform.position.x - magnet.transform.position.x),
                Is.LessThan(horizontalDistanceBeforeLift), "Magnet Lift must guide an off-centre rider toward its safe lane.");
            Assert.That(magnet.IsEnergized, Is.True, "Magnet Lift must visibly energize while carrying the player.");
            Assert.That(magnet.ActiveRiders, Is.EqualTo(1));
            Assert.That(magnet.Force.y, Is.GreaterThan(0f));
            Assert.That(magnet.LiftApplicationCount, Is.GreaterThan(0));
            Assert.That(magnet.LastCenteringForce, Is.LessThan(0f));

            var landing = GameObject.Find("MagnetLanding");
            var landingCollider = landing.GetComponent<BoxCollider2D>();
            var landingEffector = landing.GetComponent<PlatformEffector2D>();
            Assert.That(landingEffector, Is.Not.Null, "Magnet landing must allow the lift to pass through from below.");
            Assert.That(landingCollider.usedByEffector, Is.True);
            Assert.That(landingEffector.useOneWay, Is.True);

            var landingTop = landing.transform.position.y
                + landing.transform.lossyScale.y * landingCollider.size.y * 0.5f;
            var liftedAboveLanding = false;
            for (var i = 0; i < 180; i++)
            {
                yield return new WaitForFixedUpdate();
                if (player.transform.position.y > landingTop + 0.25f)
                {
                    liftedAboveLanding = true;
                    break;
                }
            }

            Assert.That(liftedAboveLanding, Is.True, "Magnet Lift must carry the player through the one-way landing.");

            player.transform.position = new Vector2(landing.transform.position.x + 1.45f, landingTop + 1.8f);
            player.Body.linearVelocity = Vector2.down * 3f;
            Physics2D.SyncTransforms();
            for (var i = 0; i < 80 && !player.IsGrounded; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(player.IsGrounded, Is.True, "The same landing must catch the player safely from above.");
            Assert.That(player.transform.position.y, Is.GreaterThan(landing.transform.position.y));
        }

        [UnityTest]
        public IEnumerator RunawayCartCarriesAnIdleRiderAlongItsRail()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var mover = Object.FindFirstObjectByType<MovingPlatform>();
            var platformCollider = mover.GetComponent<BoxCollider2D>();
            var playerCollider = player.GetComponent<CapsuleCollider2D>();
            yield return null;

            var platformTop = mover.transform.position.y
                + mover.transform.lossyScale.y * platformCollider.size.y * 0.5f;
            var playerBottomOffset = playerCollider.offset.y - playerCollider.size.y * 0.5f;
            player.transform.position = new Vector2(
                mover.transform.position.x,
                platformTop - playerBottomOffset + 0.02f
            );
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();

            for (var i = 0; i < 50 && (mover.IsWaitingAtEndpoint || !player.IsRidingMovingPlatform); i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(player.IsRidingMovingPlatform, Is.True, "The player must recognize the cart as active ground support.");
            var playerStart = (Vector2)player.transform.position;
            var moverStart = (Vector2)mover.transform.position;
            var relativeStart = playerStart - moverStart;

            for (var i = 0; i < 18; i++) yield return new WaitForFixedUpdate();

            var playerTravel = (Vector2)player.transform.position - playerStart;
            var moverTravel = (Vector2)mover.transform.position - moverStart;
            var relativeEnd = (Vector2)player.transform.position - (Vector2)mover.transform.position;

            Assert.That(moverTravel.magnitude, Is.GreaterThan(0.2f), "The cart must advance along its rail during the ride.");
            Assert.That(playerTravel.x, Is.GreaterThan(0.12f), "An idle rider must be carried forward by the cart.");
            Assert.That(Vector2.Distance(relativeStart, relativeEnd), Is.LessThan(0.45f), "The rider must remain stable relative to the cart.");
            Assert.That(player.SupportVelocity.sqrMagnitude, Is.GreaterThan(0f), "The controller must consume the cart velocity while supported.");
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
        public IEnumerator PhysicalEnemyHitsRespectRecoveryGraceAndRebootAtTheCheckpoint()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var enemy = Object.FindFirstObjectByType<PatrolEnemy>();
            var game = Object.FindFirstObjectByType<ScrapDashGame>();
            yield return null;

            var enemyPosition = (Vector2)enemy.transform.position;
            var safePoint = new Vector2(8f, 0.25f);
            game.ActivateCheckpoint(safePoint);
            enemy.Configure(enemyPosition.x, enemyPosition.x, 0f);

            player.transform.position = enemyPosition;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(game.Hits, Is.EqualTo(1));
            Assert.That(game.Integrity, Is.EqualTo(2));
            Assert.That(player.IsInvulnerable, Is.True, "A hit must provide a short recovery window.");

            player.transform.position = enemyPosition + Vector2.left * 2f;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            player.transform.position = enemyPosition;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(game.Hits, Is.EqualTo(1), "Immediate repeat contact must not drain the full core.");

            for (var acceptedHit = 2; acceptedHit <= ScrapDashGame.MaxIntegrityValue; acceptedHit++)
            {
                player.transform.position = enemyPosition + Vector2.left * 2f;
                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return new WaitForSecondsRealtime(0.85f);
                player.transform.position = enemyPosition;
                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return null;
                Assert.That(game.Hits, Is.EqualTo(acceptedHit));
            }

            Assert.That(game.Deaths, Is.EqualTo(1), "Three accepted hits must trigger one reboot.");
            Assert.That(game.Integrity, Is.EqualTo(ScrapDashGame.MaxIntegrityValue));
            Assert.That((Vector2)player.transform.position, Is.EqualTo(safePoint));
            Assert.That(player.Body.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(player.IsInvulnerable, Is.True, "Respawn must include safe recovery grace.");
        }

        [UnityTest]
        public IEnumerator MagnetLandingHasASafePhysicalDescentToTheFinishDeck()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var playerCollider = player.GetComponent<CapsuleCollider2D>();
            var highStep = GameObject.Find("ExitStepHigh");
            var lowStep = GameObject.Find("ExitStepLow");
            var finishDeck = GameObject.Find("FinishDeck");
            yield return null;

            Assert.That(highStep, Is.Not.Null);
            Assert.That(lowStep, Is.Not.Null);
            Assert.That(finishDeck, Is.Not.Null);
            Assert.That(GameObject.Find("ExitRoute"), Is.Not.Null, "The descent must have a visible route marker.");

            player.transform.position = new Vector2(20.8f, 2f);
            player.ResetMotion();
            player.Body.linearVelocity = Vector2.down * 3f;
            Physics2D.SyncTransforms();
            yield return WaitForGrounding(player, 80);
            Assert.That(player.IsGrounded, Is.True, "The upper exit step must catch the player.");
            Assert.That(player.transform.position.y, Is.EqualTo(ExpectedStandingY(highStep, playerCollider)).Within(0.18f));

            player.transform.position = new Vector2(21.65f, 0.35f);
            player.ResetMotion();
            player.Body.linearVelocity = Vector2.down * 3f;
            Physics2D.SyncTransforms();
            yield return WaitForGrounding(player, 80);
            Assert.That(player.IsGrounded, Is.True, "The lower exit step must catch the player.");
            Assert.That(player.transform.position.y, Is.EqualTo(ExpectedStandingY(lowStep, playerCollider)).Within(0.18f));

            player.transform.position = new Vector2(22.25f, -0.35f);
            player.ResetMotion();
            player.Body.linearVelocity = Vector2.down * 3f;
            Physics2D.SyncTransforms();
            yield return WaitForGrounding(player, 80);
            Assert.That(player.IsGrounded, Is.True, "The descent must end safely on the finish deck.");
            Assert.That(player.transform.position.y, Is.EqualTo(ExpectedStandingY(finishDeck, playerCollider)).Within(0.18f));
        }

        [UnityTest]
        public IEnumerator FollowCameraKeepsTheCriticalRouteReadableAndSnapsAfterRespawn()
        {
            _root = ScrapDashBootstrap.BuildLevelForTests();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var follow = Object.FindFirstObjectByType<FollowCamera>();
            var camera = follow.GetComponent<Camera>();
            yield return null;

            var snapCountBeforeTeleport = follow.SnapCount;
            var cameraStartX = follow.transform.position.x;
            player.transform.position = new Vector2(8f, -1.1f);
            player.Body.linearVelocity = Vector2.right * 8f;
            for (var i = 0; i < 4; i++) yield return null;

            Assert.That(follow.SnapCount, Is.GreaterThan(snapCountBeforeTeleport), "Camera must recover immediately after a large displacement.");
            Assert.That(follow.transform.position.x, Is.GreaterThan(cameraStartX + 0.1f), "Camera must follow forward motion.");
            Assert.That(follow.transform.position.x, Is.LessThan(player.transform.position.x + 3f), "Camera must not overshoot the player.");
            Assert.That(follow.IsWithinBounds, Is.True, "Camera must remain inside the authored Level 1 bounds.");

            player.transform.position = new Vector2(12f, 0f);
            player.Body.linearVelocity = new Vector2(0f, 0.5f);
            follow.SnapToTarget();
            yield return null;

            Assert.That(Mathf.Abs(follow.CurrentLookAhead.y), Is.LessThan(0.05f), "Small vertical motion must not make the camera bob.");

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
                Assert.That(follow.IsWithinBounds, Is.True, $"Camera at route point {point} must stay inside level bounds.");
            }

            var checkpoint = new Vector2(14.8f, 0.25f);
            ScrapDashGame.Instance.ActivateCheckpoint(checkpoint);
            player.transform.position = new Vector2(-20f, -8f);
            ScrapDashGame.Instance.RespawnPlayer();

            Assert.That((Vector2)player.transform.position, Is.EqualTo(checkpoint));
            Assert.That(Vector2.Distance(follow.transform.position, checkpoint), Is.LessThan(4f));
            Assert.That(follow.IsWithinBounds, Is.True, "Respawn camera must return to a valid composition.");
        }

        private static float ExpectedStandingY(GameObject platform, CapsuleCollider2D playerCollider)
        {
            var platformCollider = platform.GetComponent<BoxCollider2D>();
            var platformTop = platform.transform.position.y
                + platform.transform.lossyScale.y * platformCollider.size.y * 0.5f;
            var playerBottomOffset = playerCollider.offset.y - playerCollider.size.y * 0.5f;
            return platformTop - playerBottomOffset;
        }

        private static IEnumerator WaitForGrounding(PlayerController player, int fixedFrameLimit = 20)
        {
            for (var i = 0; i < fixedFrameLimit && !player.IsGrounded; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
