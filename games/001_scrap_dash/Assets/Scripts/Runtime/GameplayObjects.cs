using UnityEngine;

namespace ScrapDash
{
    public sealed class Hazard : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null) ScrapDashGame.Instance?.RespawnPlayer();
        }
    }

    public sealed class ScrapCollectible : MonoBehaviour
    {
        private bool _collected;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || other.GetComponent<PlayerController>() == null) return;
            _collected = true;
            FeedbackHub.Instance?.PlayCollect(transform.position);
            ScrapDashGame.Instance?.CollectScrap();
            Destroy(gameObject);
        }
    }

    public sealed class PatrolEnemy : MonoBehaviour
    {
        [SerializeField] private float leftX;
        [SerializeField] private float rightX;
        [SerializeField] private float speed = 2.6f;
        [SerializeField] private float chargeSpeed = 5.2f;
        [SerializeField] private float alertDistance = 3.2f;
        [SerializeField] private float verticalAlertRange = 1.6f;
        private int _direction = 1;
        private bool _defeated;
        private bool _alerted;
        private Transform _player;
        private SpriteRenderer _eye;
        private Vector3 _eyeScale = Vector3.one;

        public bool Defeated => _defeated;
        public bool IsAlerted => _alerted;

        public void Configure(float left, float right, float moveSpeed)
        {
            leftX = Mathf.Min(left, right);
            rightX = Mathf.Max(left, right);
            speed = moveSpeed;
        }

        private void Start()
        {
            _player = FindFirstObjectByType<PlayerController>()?.transform;
            _eye = transform.Find("EnemyEye")?.GetComponent<SpriteRenderer>();
            if (_eye != null) _eyeScale = _eye.transform.localScale;
        }

        private void Update()
        {
            if (_defeated) return;

            var position = transform.position;
            var playerDelta = _player == null ? Vector2.positiveInfinity : (Vector2)(_player.position - position);
            _alerted = Mathf.Abs(playerDelta.x) <= alertDistance
                && Mathf.Abs(playerDelta.y) <= verticalAlertRange;

            if (_alerted && Mathf.Abs(playerDelta.x) > 0.1f)
            {
                _direction = playerDelta.x > 0f ? 1 : -1;
            }

            position.x += _direction * (_alerted ? chargeSpeed : speed) * Time.deltaTime;

            if (position.x >= rightX)
            {
                position.x = rightX;
                _direction = -1;
            }
            else if (position.x <= leftX)
            {
                position.x = leftX;
                _direction = 1;
            }

            transform.position = position;
            UpdateWarningVisual();
        }

        private void UpdateWarningVisual()
        {
            if (_eye == null) return;

            _eye.color = _alerted
                ? new Color(1f, 0.18f, 0.32f, 1f)
                : new Color(1f, 0.88f, 0.4f, 1f);
            var pulse = _alerted ? 1.15f + Mathf.Sin(Time.time * 20f) * 0.2f : 1f;
            _eye.transform.localScale = _eyeScale * pulse;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null || _defeated) return;

            if (player.IsDashing)
            {
                _defeated = true;
                FeedbackHub.Instance?.PlayStomp(transform.position);
                ScrapDashGame.Instance?.NotifyEnemyDefeated();
                Destroy(gameObject);
                return;
            }

            var isStomp = player.Body.linearVelocity.y < -0.5f
                && player.transform.position.y > transform.position.y + 0.2f;
            if (isStomp)
            {
                _defeated = true;
                player.Bounce(9.5f);
                FeedbackHub.Instance?.PlayStomp(transform.position);
                ScrapDashGame.Instance?.NotifyEnemyDefeated();
                Destroy(gameObject);
                return;
            }

            player.TakeHit(transform.position);
        }
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector2 pointA;
        [SerializeField] private Vector2 pointB;
        [SerializeField] private float speed = 2f;
        private Rigidbody2D _body;
        private float _distance;
        private float _travel;
        private Vector2 _velocity;

        public Vector2 Velocity => _velocity;

        public void Configure(Vector2 a, Vector2 b, float moveSpeed)
        {
            pointA = a;
            pointB = b;
            speed = moveSpeed;
            _distance = Vector2.Distance(pointA, pointB);
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.useFullKinematicContacts = true;
            _distance = Vector2.Distance(pointA, pointB);
        }

        private void FixedUpdate()
        {
            if (_distance <= 0.001f) return;
            _travel += speed * Time.fixedDeltaTime;
            var loop = Mathf.PingPong(_travel, _distance);
            var t = loop / _distance;
            var target = Vector2.Lerp(pointA, pointB, t);
            _velocity = (target - _body.position) / Time.fixedDeltaTime;
            _body.MovePosition(target);
        }
    }

    public sealed class MagnetZone : MonoBehaviour
    {
        [SerializeField] private Vector2 force = new(0f, 36f);

        public void Configure(Vector2 magnetForce)
        {
            force = magnetForce;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null) player.ApplyMagnet(force);
        }
    }

    public sealed class SpringPad : MonoBehaviour
    {
        [SerializeField] private float bounceVelocity = 17f;
        private float _readyAt;

        public void Configure(float velocity)
        {
            bounceVelocity = Mathf.Max(1f, velocity);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Bounce(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Bounce(other);
        }

        private void Bounce(Collider2D other)
        {
            if (Time.time < _readyAt) return;
            var player = other.GetComponent<PlayerController>();
            if (player == null || player.Body.linearVelocity.y > 2f) return;

            _readyAt = Time.time + 0.18f;
            player.Bounce(bounceVelocity);
            FeedbackHub.Instance?.PlaySpring(transform.position);
        }
    }

    public sealed class Checkpoint : MonoBehaviour
    {
        private bool _activated;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_activated || other.GetComponent<PlayerController>() == null) return;
            _activated = true;
            ScrapDashGame.Instance?.ActivateCheckpoint(transform.position + Vector3.up * 1.4f);
            FeedbackHub.Instance?.PlayCheckpoint(transform.position);

            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = ProceduralVisuals.Hex("#62E5FF");
        }
    }

    public sealed class FinishGate : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null)
            {
                ScrapDashGame.Instance?.TryFinish();
            }
        }
    }
}
