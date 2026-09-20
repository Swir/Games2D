using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScrapDash
{
    public static class ScrapDashBootstrap
    {
        private static readonly Color Background = ProceduralVisuals.Hex("#02050A");
        private static readonly Color Surface = ProceduralVisuals.Hex("#07111C");
        private static readonly Color Cyan = ProceduralVisuals.Hex("#62E5FF");
        private static readonly Color Blue = ProceduralVisuals.Hex("#0088FF");
        private static readonly Color Yellow = ProceduralVisuals.Hex("#FFE066");
        private static readonly Color Danger = ProceduralVisuals.Hex("#FF426D");
        private static readonly Color Purple = ProceduralVisuals.Hex("#8B5CF6");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!SceneManager.GetActiveScene().IsValid()) return;
            BuildLevelForTests();
        }

        public static GameObject BuildLevelForTests()
        {
            var existing = GameObject.Find("SCRAP_DASH_LEVEL");
            if (existing != null) return existing;

            var root = new GameObject("SCRAP_DASH_LEVEL");
            var gameObject = new GameObject("GameState");
            gameObject.transform.SetParent(root.transform);
            gameObject.AddComponent<FeedbackHub>();
            var game = gameObject.AddComponent<ScrapDashGame>();

            CreateBackdrop(root.transform);
            var player = CreatePlayer(root.transform, new Vector2(-10f, -1.1f));
            game.RegisterPlayer(player, player.transform.position);
            CreateCamera(root.transform, player.transform);
            CreateLevelGeometry(root.transform);
            CreateGameplay(root.transform);
            CreateWayfinding(root.transform);
            return root;
        }

        private static void CreateBackdrop(Transform parent)
        {
            var cameraColor = Background;
            var camera = Camera.main;
            if (camera != null) camera.backgroundColor = cameraColor;

            var sky = ProceduralVisuals.Rect("NightSky", new Vector2(8f, 2f), new Vector2(50f, 18f), Background, parent, -30);
            sky.transform.position = new Vector3(8f, 2f, 5f);

            ProceduralVisuals.Rect("DistantRideBase", new Vector2(7f, -0.3f), new Vector2(10f, 0.45f), new Color(0.04f, 0.08f, 0.13f), parent, -20);
            ProceduralVisuals.Rect("DistantRidePole", new Vector2(8f, 2f), new Vector2(0.35f, 6f), new Color(0.05f, 0.1f, 0.16f), parent, -20);
            ProceduralVisuals.Rect("SignGlow", new Vector2(-6f, 1.8f), new Vector2(4f, 0.3f), Blue, parent, -19);
            ProceduralVisuals.Rect("SignBody", new Vector2(-6f, 1.45f), new Vector2(4.6f, 0.9f), new Color(0.04f, 0.08f, 0.14f), parent, -20);

            for (var i = 0; i < 18; i++)
            {
                var x = -12f + i * 2.4f;
                var light = ProceduralVisuals.Rect($"FenceLight_{i:00}", new Vector2(x, -0.15f), new Vector2(0.08f, 0.08f), i % 3 == 0 ? Yellow : Cyan, parent, -15);
                light.transform.localScale *= 1.5f;
            }
        }

        private static PlayerController CreatePlayer(Transform parent, Vector2 position)
        {
            var player = new GameObject("Player");
            player.transform.SetParent(parent, false);
            player.transform.position = position;

            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 3.2f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = player.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.85f, 1.35f);
            collider.offset = new Vector2(0f, 0.05f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            ProceduralVisuals.Rect("Body", new Vector2(0f, -0.05f), new Vector2(0.8f, 0.8f), new Color(0.08f, 0.16f, 0.22f), visual.transform, 20);
            ProceduralVisuals.Rect("Head", new Vector2(0f, 0.48f), new Vector2(0.72f, 0.5f), Cyan, visual.transform, 21);
            ProceduralVisuals.Rect("EyeLeft", new Vector2(-0.18f, 0.52f), new Vector2(0.1f, 0.1f), Background, visual.transform, 22);
            ProceduralVisuals.Rect("EyeRight", new Vector2(0.18f, 0.52f), new Vector2(0.1f, 0.1f), Background, visual.transform, 22);
            var core = ProceduralVisuals.Rect("Core", new Vector2(0f, -0.08f), new Vector2(0.28f, 0.28f), Yellow, visual.transform, 22);
            core.AddComponent<CorePulse>();
            ProceduralVisuals.Rect("Antenna", new Vector2(0.18f, 0.86f), new Vector2(0.08f, 0.34f), Yellow, visual.transform, 20);
            ProceduralVisuals.Rect("AntennaTip", new Vector2(0.18f, 1.04f), new Vector2(0.18f, 0.18f), Danger, visual.transform, 22);
            ProceduralVisuals.Rect("ArmLeft", new Vector2(-0.52f, -0.03f), new Vector2(0.2f, 0.48f), Blue, visual.transform, 19);
            ProceduralVisuals.Rect("ArmRight", new Vector2(0.52f, -0.03f), new Vector2(0.2f, 0.48f), Blue, visual.transform, 19);
            ProceduralVisuals.Rect("FootLeft", new Vector2(-0.25f, -0.55f), new Vector2(0.25f, 0.18f), Blue, visual.transform, 21);
            ProceduralVisuals.Rect("FootRight", new Vector2(0.25f, -0.55f), new Vector2(0.25f, 0.18f), Blue, visual.transform, 21);

            var trail = player.AddComponent<TrailRenderer>();
            trail.time = 0.16f;
            trail.startWidth = 0.58f;
            trail.endWidth = 0f;
            trail.startColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.72f);
            trail.endColor = new Color(Blue.r, Blue.g, Blue.b, 0f);
            trail.sortingOrder = 18;
            trail.emitting = false;
            var trailShader = Shader.Find("Sprites/Default");
            if (trailShader != null) trail.material = new Material(trailShader);

            return player.AddComponent<PlayerController>();
        }

        private static void CreateCamera(Transform parent, Transform target)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(-8f, 0.2f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.2f;
            camera.backgroundColor = Background;
            camera.clearFlags = CameraClearFlags.SolidColor;

            cameraObject.AddComponent<AudioListener>();
            var follow = cameraObject.AddComponent<FollowCamera>();
            follow.SetTarget(target);
        }

        private static void CreateLevelGeometry(Transform parent)
        {
            CreatePlatform(parent, "StartDeck", new Vector2(-8.2f, -2.5f), new Vector2(8f, 1f));
            CreatePlatform(parent, "TicketBoothDeck", new Vector2(-1.1f, -2.5f), new Vector2(4.2f, 1f));
            CreatePlatform(parent, "MidwayDeck", new Vector2(8f, -2.5f), new Vector2(5f, 1f));
            CreatePlatform(parent, "ArcadeDeck", new Vector2(13.3f, -2.5f), new Vector2(5.4f, 1f));
            CreatePlatform(parent, "MagnetLanding", new Vector2(18.2f, 2.25f), new Vector2(4f, 0.65f));
            CreatePlatform(parent, "FinishDeck", new Vector2(23.5f, -2.5f), new Vector2(8f, 1f));

            var mover = CreatePlatform(parent, "RunawayCart", new Vector2(3.2f, -1.4f), new Vector2(2.4f, 0.45f), true);
            var moving = mover.AddComponent<MovingPlatform>();
            moving.Configure(new Vector2(2.4f, -1.4f), new Vector2(5.2f, -0.4f), 2.1f);
            ProceduralVisuals.Rect("CartCab", new Vector2(0f, 0.58f), new Vector2(0.62f, 1.15f), Blue, mover.transform, 10);
            ProceduralVisuals.Rect("CartBeacon", new Vector2(0f, 1.2f), new Vector2(0.2f, 0.2f), Yellow, mover.transform, 12).AddComponent<CorePulse>();
            ProceduralVisuals.Rect("CartWheelLeft", new Vector2(-0.3f, -0.64f), new Vector2(0.2f, 0.36f), Cyan, mover.transform, 10);
            ProceduralVisuals.Rect("CartWheelRight", new Vector2(0.3f, -0.64f), new Vector2(0.2f, 0.36f), Cyan, mover.transform, 10);

            CreateRail(parent, new Vector2(16.9f, -1f), new Vector2(0.12f, 6f));
            CreateRail(parent, new Vector2(19.4f, -0.1f), new Vector2(0.12f, 4f));
        }

        private static void CreateGameplay(Transform parent)
        {
            CreateHazard(parent, "ElectricPitA", new Vector2(-3.6f, -3.4f), new Vector2(1.2f, 1.1f));
            CreateHazard(parent, "ArcPad", new Vector2(-0.3f, -1.85f), new Vector2(0.9f, 0.25f));
            CreateHazard(parent, "ElectricPitB", new Vector2(18.7f, -3.8f), new Vector2(2f, 1.5f));
            CreateHazard(parent, "KillFloor", new Vector2(8f, -7.2f), new Vector2(45f, 0.8f), false);

            CreateScrap(parent, "Scrap_01", new Vector2(-7.2f, -0.7f));
            CreateScrap(parent, "Scrap_02", new Vector2(-0.8f, 0.1f));
            CreateScrap(parent, "Scrap_03", new Vector2(7.8f, -0.3f));
            CreateScrap(parent, "Scrap_04", new Vector2(13.6f, -0.35f));
            CreateScrap(parent, "Scrap_05", new Vector2(18.2f, 3.35f));

            var enemy = ProceduralVisuals.Rect("BrokenToyEnemy", new Vector2(12.2f, -1.45f), new Vector2(0.9f, 0.9f), Purple, parent, 12);
            var enemyCollider = enemy.AddComponent<BoxCollider2D>();
            enemyCollider.isTrigger = true;
            var patrol = enemy.AddComponent<PatrolEnemy>();
            patrol.Configure(11.2f, 14.2f, 2.4f);
            ProceduralVisuals.Rect("EnemyEye", new Vector2(0.18f, 0.1f), new Vector2(0.18f, 0.18f), Yellow, enemy.transform, 13);
            ProceduralVisuals.Rect("EnemyJaw", new Vector2(0f, -0.34f), new Vector2(0.65f, 0.12f), Danger, enemy.transform, 13);
            ProceduralVisuals.Rect("EnemyWheelLeft", new Vector2(-0.3f, -0.55f), new Vector2(0.24f, 0.24f), Surface, enemy.transform, 13);
            ProceduralVisuals.Rect("EnemyWheelRight", new Vector2(0.3f, -0.55f), new Vector2(0.24f, 0.24f), Surface, enemy.transform, 13);

            var spring = ProceduralVisuals.Rect("ScrapSpring", new Vector2(15.55f, -1.84f), new Vector2(0.9f, 0.22f), Yellow, parent, 14);
            var springCollider = spring.AddComponent<BoxCollider2D>();
            springCollider.isTrigger = true;
            springCollider.size = new Vector2(1f, 1.9f);
            spring.AddComponent<SpringPad>().Configure(17.5f);
            ProceduralVisuals.Rect("SpringCoil", new Vector2(0f, -0.18f), new Vector2(0.42f, 0.32f), Cyan, spring.transform, 13);

            var magnet = ProceduralVisuals.Rect("MagnetLiftZone", new Vector2(17.1f, 0f), new Vector2(2.1f, 5.2f), new Color(0.0f, 0.55f, 1f, 0.16f), parent, 5);
            var magnetCollider = magnet.AddComponent<BoxCollider2D>();
            magnetCollider.isTrigger = true;
            var magnetZone = magnet.AddComponent<MagnetZone>();
            magnetZone.Configure(new Vector2(0f, 39f));
            ProceduralVisuals.Rect("MagnetCore", new Vector2(0f, -2.1f), new Vector2(1.25f, 0.3f), Blue, magnet.transform, 7);

            var checkpoint = ProceduralVisuals.Rect("Checkpoint", new Vector2(14.8f, -1.15f), new Vector2(0.35f, 1.7f), Yellow, parent, 10);
            var checkpointCollider = checkpoint.AddComponent<BoxCollider2D>();
            checkpointCollider.isTrigger = true;
            checkpoint.AddComponent<Checkpoint>();
            ProceduralVisuals.Rect("CheckpointBeacon", new Vector2(0f, 1.05f), new Vector2(0.42f, 0.42f), Yellow, checkpoint.transform, 12).AddComponent<CorePulse>();

            var finish = ProceduralVisuals.Rect("FinishGate", new Vector2(25.5f, -0.85f), new Vector2(0.4f, 2.5f), Danger, parent, 10);
            var finishCollider = finish.AddComponent<BoxCollider2D>();
            finishCollider.isTrigger = true;
            finish.AddComponent<FinishGate>();
            ProceduralVisuals.Rect("FinishTop", new Vector2(-0.65f, 1f), new Vector2(1.7f, 0.25f), Danger, finish.transform, 11);
        }

        private static void CreateWayfinding(Transform parent)
        {
            CreateRouteChevrons(parent, "DashRoute", new Vector2(-4.1f, -0.65f), Vector2.right, Cyan);
            CreateRouteChevrons(parent, "CartRoute", new Vector2(3.4f, 0.35f), Vector2.right, Yellow);
            CreateRouteChevrons(parent, "MagnetRoute", new Vector2(15.55f, -0.75f), Vector2.up, Cyan);
            CreateRouteChevrons(parent, "FinishRoute", new Vector2(23.35f, -0.4f), Vector2.right, Yellow);
        }

        private static void CreateRouteChevrons(Transform parent, string name, Vector2 position, Vector2 direction, Color color)
        {
            var route = new GameObject(name);
            route.transform.SetParent(parent, false);
            route.transform.position = position;
            route.transform.rotation = Quaternion.Euler(0f, 0f, direction == Vector2.up ? 90f : 0f);
            route.AddComponent<CorePulse>();

            for (var i = 0; i < 3; i++)
            {
                var x = (i - 1) * 0.42f;
                var upper = ProceduralVisuals.Rect($"Chevron_{i}_Upper", new Vector2(x, 0.12f), new Vector2(0.1f, 0.42f), color, route.transform, 16);
                var lower = ProceduralVisuals.Rect($"Chevron_{i}_Lower", new Vector2(x, -0.12f), new Vector2(0.1f, 0.42f), color, route.transform, 16);
                upper.transform.localRotation = Quaternion.Euler(0f, 0f, -42f);
                lower.transform.localRotation = Quaternion.Euler(0f, 0f, 42f);
            }
        }

        private static GameObject CreatePlatform(Transform parent, string name, Vector2 position, Vector2 size, bool kinematic = false)
        {
            var platform = ProceduralVisuals.Rect(name, position, size, Surface, parent, 8);
            var collider = platform.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            if (kinematic)
            {
                var body = platform.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.useFullKinematicContacts = true;
            }

            var edge = ProceduralVisuals.Rect($"{name}_Edge", new Vector2(0f, 0.5f), new Vector2(1f, 0.08f), Blue, platform.transform, 9);
            edge.transform.localPosition = new Vector3(0f, 0.47f, 0f);
            return platform;
        }

        private static void CreateRail(Transform parent, Vector2 position, Vector2 size)
        {
            ProceduralVisuals.Rect("RideRail", position, size, new Color(0.05f, 0.2f, 0.27f), parent, 3);
        }

        private static void CreateHazard(Transform parent, string name, Vector2 position, Vector2 size, bool visible = true)
        {
            var hazard = ProceduralVisuals.Rect(name, position, size, visible ? Danger : new Color(0f, 0f, 0f, 0f), parent, 11);
            var collider = hazard.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            hazard.AddComponent<Hazard>();
        }

        private static void CreateScrap(Transform parent, string name, Vector2 position)
        {
            var scrap = ProceduralVisuals.Rect(name, position, new Vector2(0.38f, 0.38f), Yellow, parent, 15);
            scrap.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            ProceduralVisuals.Rect("EnergyCore", Vector2.zero, new Vector2(0.18f, 0.18f), Cyan, scrap.transform, 16);
            var collider = scrap.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.7f;
            scrap.AddComponent<ScrapCollectible>();
        }
    }
}
