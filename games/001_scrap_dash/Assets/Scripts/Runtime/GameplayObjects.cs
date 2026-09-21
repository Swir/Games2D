using UnityEngine;

namespace ScrapDash
{
    public sealed class Hazard : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Color _baseColor;

        public int TriggerCount { get; private set; }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;
        }

        private void Update()
        {
            if (_renderer == null || _baseColor.a <= 0f) return;

            var warning = 0.5f + Mathf.Sin(Time.unscaledTime * 11f) * 0.5f;
            var color = Color.Lerp(_baseColor, Color.white, warning * 0.22f);
            color.a = _baseColor.a;
            _renderer.color = color;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null) return;

            TriggerCount++;
            FeedbackHub.Instance?.PlayDeath(player.transform.position);
            ScrapDashGame.Instance?.RespawnPlayer("SURGE REBOOT");
        }
    }

    public sealed class ScrapCollectible : MonoBehaviour
    {
        private bool _collected;
        private Vector3 _baseLocalPosition;
        private Vector3 _baseScale;
        private float _phase;

        public bool Collected => _collected;

        private void Start()
        {
            _baseLocalPosition = transform.localPosition;
            _baseScale = transform.localScale;
            _phase = Mathf.Abs(transform.position.x * 0.73f) % (Mathf.PI * 2f);
        }

        private void Update()
        {
            if (_collected) return;

            var time = Time.unscaledTime;
            transform.localPosition = _baseLocalPosition + Vector3.up * (Mathf.Sin(time * 3.2f + _phase) * 0.09f);
            transform.Rotate(0f, 0f, 75f * Time.unscaledDeltaTime);
            transform.localScale = _baseScale * (1f + Mathf.Sin(time * 5f + _phase) * 0.1f);
        }

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
        [SerializeField] private float endpointWait = 0.55f;
        private Rigidbody2D _body;
        private float _distance;
        private float _travel;
        private float _waitRemaining;
        private bool _travellingForward = true;
        private Vector2 _velocity;
        private float _totalDistanceMoved;
        private int _endpointPauseCount;

        public Vector2 Velocity => _velocity;
        public float TotalDistanceMoved => _totalDistanceMoved;
        public float NormalizedProgress => _distance <= 0.001f ? 0f : _travel / _distance;
        public bool IsWaitingAtEndpoint => _waitRemaining > 0f;
        public int EndpointPauseCount => _endpointPauseCount;

        public void Configure(Vector2 a, Vector2 b, float moveSpeed)
        {
            pointA = a;
            pointB = b;
            speed = Mathf.Max(0.1f, moveSpeed);
            _distance = Vector2.Distance(pointA, pointB);
            _travel = 0f;
            _waitRemaining = endpointWait;
            _travellingForward = true;
            _velocity = Vector2.zero;

            if (_body != null)
            {
                _body.position = pointA;
                transform.position = pointA;
            }
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
            if (_waitRemaining > 0f)
            {
                _waitRemaining = Mathf.Max(0f, _waitRemaining - Time.fixedDeltaTime);
                _velocity = Vector2.zero;
                return;
            }

            var direction = _travellingForward ? 1f : -1f;
            _travel = Mathf.Clamp(_travel + direction * speed * Time.fixedDeltaTime, 0f, _distance);
            var t = _travel / _distance;
            var target = Vector2.Lerp(pointA, pointB, t);
            _velocity = (target - _body.position) / Time.fixedDeltaTime;
            _totalDistanceMoved += Vector2.Distance(_body.position, target);
            _body.MovePosition(target);

            if ((_travellingForward && _travel >= _distance)
                || (!_travellingForward && _travel <= 0f))
            {
                _travellingForward = !_travellingForward;
                _waitRemaining = endpointWait;
                _endpointPauseCount++;
            }
        }
    }

    public sealed class MagnetZone : MonoBehaviour
    {
        [SerializeField] private Vector2 force = new(0f, 36f);
        [SerializeField] private float centeringForce = 18f;
        [SerializeField] private float captureDeadZone = 0.12f;
        private SpriteRenderer _renderer;
        private Color _baseColor;
        private PlayerController _activeRider;
        private int _liftApplicationCount;
        private float _lastCenteringForce;

        public Vector2 Force => force;
        public int ActiveRiders => _activeRider == null ? 0 : 1;
        public bool IsEnergized => _activeRider != null;
        public int LiftApplicationCount => _liftApplicationCount;
        public float LastCenteringForce => _lastCenteringForce;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;
        }

        public void Configure(Vector2 magnetForce)
        {
            force = magnetForce;
        }

        private void Update()
        {
            if (_renderer == null) return;

            var pulse = 0.5f + Mathf.Sin(Time.unscaledTime * (IsEnergized ? 13f : 5f)) * 0.5f;
            var color = _baseColor;
            color.a = IsEnergized ? Mathf.Lerp(0.28f, 0.48f, pulse) : Mathf.Lerp(0.12f, 0.2f, pulse);
            _renderer.color = color;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null) _activeRider = player;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null) return;

            _activeRider = player;
            var horizontalOffset = transform.position.x - player.transform.position.x;
            _lastCenteringForce = Mathf.Abs(horizontalOffset) <= captureDeadZone
                ? 0f
                : Mathf.Clamp(horizontalOffset * centeringForce, -centeringForce, centeringForce);
            player.ApplyMagnet(new Vector2(force.x + _lastCenteringForce, force.y));
            _liftApplicationCount++;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null && player == _activeRider)
            {
                _activeRider = null;
                _lastCenteringForce = 0f;
            }
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
        private int _activationCount;
        private SpriteRenderer _renderer;
        private SpriteRenderer _beacon;

        public bool Activated => _activated;
        public int ActivationCount => _activationCount;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _beacon = transform.Find("CheckpointBeacon")?.GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_beacon == null) return;

            var pulse = 0.5f + Mathf.Sin(Time.unscaledTime * (_activated ? 10f : 4f)) * 0.5f;
            _beacon.color = Color.Lerp(
                _activated ? ProceduralVisuals.Hex("#0088FF") : ProceduralVisuals.Hex("#FFE066"),
                Color.white,
                pulse * (_activated ? 0.55f : 0.2f)
            );
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_activated || other.GetComponent<PlayerController>() == null) return;
            _activated = true;
            _activationCount++;
            ScrapDashGame.Instance?.ActivateCheckpoint(transform.position + Vector3.up * 1.4f);
            FeedbackHub.Instance?.PlayCheckpoint(transform.position);

            if (_renderer != null) _renderer.color = ProceduralVisuals.Hex("#62E5FF");
        }
    }

    public sealed class ObjectivePointer : MonoBehaviour
    {
        [SerializeField] private float heightAbovePlayer = 1.75f;
        private Transform _player;
        private Transform _currentTarget;
        private SpriteRenderer[] _renderers;

        public string CurrentTargetName => _currentTarget != null ? _currentTarget.name : string.Empty;
        public bool IsPointingToExit { get; private set; }
        public bool Visible { get; private set; }
        public int TargetChangeCount { get; private set; }
        public float TargetDistance => _player != null && _currentTarget != null
            ? Vector2.Distance(_player.position, _currentTarget.position)
            : 0f;

        public void Configure(Transform player)
        {
            _player = player;
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            RefreshTarget();
        }

        public void RefreshTarget()
        {
            var previousTarget = _currentTarget;
            var game = ScrapDashGame.Instance;
            IsPointingToExit = game != null && game.ScrapObjectiveMet;

            if (IsPointingToExit)
            {
                _currentTarget = FindFirstObjectByType<FinishGate>()?.transform;
            }
            else
            {
                _currentTarget = null;
                var bestDistance = float.PositiveInfinity;
                var scraps = FindObjectsByType<ScrapCollectible>(FindObjectsSortMode.None);
                foreach (var scrap in scraps)
                {
                    var distance = _player != null
                        ? ((Vector2)(scrap.transform.position - _player.position)).sqrMagnitude
                        : 0f;
                    if (distance >= bestDistance) continue;
                    bestDistance = distance;
                    _currentTarget = scrap.transform;
                }
            }

            if (_currentTarget != previousTarget) TargetChangeCount++;
        }

        private void Update()
        {
            var game = ScrapDashGame.Instance;
            if (_player == null || game == null || game.Won)
            {
                SetVisible(false);
                return;
            }

            var shouldPointToExit = game.ScrapObjectiveMet;
            if (_currentTarget == null || shouldPointToExit != IsPointingToExit)
            {
                RefreshTarget();
            }

            SetVisible(_currentTarget != null);
            if (!Visible) return;

            transform.position = _player.position + Vector3.up * heightAbovePlayer;
            var direction = (Vector2)(_currentTarget.position - transform.position);
            if (direction.sqrMagnitude > 0.001f)
            {
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            var baseColor = IsPointingToExit
                ? ProceduralVisuals.Hex("#FFE066")
                : ProceduralVisuals.Hex("#62E5FF");
            var pulse = 0.82f + Mathf.Sin(Time.unscaledTime * 8f) * 0.18f;
            foreach (var sprite in _renderers)
            {
                if (sprite != null) sprite.color = Color.Lerp(baseColor, Color.white, pulse * 0.32f);
            }
        }

        private void SetVisible(bool visible)
        {
            Visible = visible;
            if (_renderers == null) return;
            foreach (var sprite in _renderers)
            {
                if (sprite != null) sprite.enabled = visible;
            }
        }
    }

    public sealed class FinishGate : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private SpriteRenderer _top;

        public int EntryAttempts { get; private set; }
        public int LockedAttempts { get; private set; }
        public bool IsUnlocked => ScrapDashGame.Instance != null && ScrapDashGame.Instance.ScrapObjectiveMet;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _top = transform.Find("FinishTop")?.GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            var baseColor = IsUnlocked ? ProceduralVisuals.Hex("#62E5FF") : ProceduralVisuals.Hex("#FF426D");
            var pulse = 0.5f + Mathf.Sin(Time.unscaledTime * (IsUnlocked ? 9f : 4f)) * 0.5f;
            var color = Color.Lerp(baseColor, Color.white, pulse * (IsUnlocked ? 0.42f : 0.16f));
            if (_renderer != null) _renderer.color = color;
            if (_top != null) _top.color = color;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null) return;

            EntryAttempts++;
            if (!IsUnlocked)
            {
                LockedAttempts++;
                player.RejectFromGate(transform.position);
            }

            ScrapDashGame.Instance?.TryFinish();
        }
    }
}
