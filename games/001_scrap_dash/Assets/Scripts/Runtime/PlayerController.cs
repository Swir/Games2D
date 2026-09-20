using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScrapDash
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float maxRunSpeed = 8.5f;
        [SerializeField] private float groundAcceleration = 70f;
        [SerializeField] private float airAcceleration = 35f;
        [SerializeField] private float jumpVelocity = 13.5f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.14f;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 18f;
        [SerializeField] private float dashDuration = 0.16f;
        [SerializeField] private float dashCooldown = 0.45f;

        private readonly HashSet<Collider2D> _groundContacts = new();
        private Rigidbody2D _body;
        private Transform _visualRoot;
        private InputAction _move;
        private InputAction _jump;
        private InputAction _dash;
        private float _coyoteRemaining;
        private float _jumpBufferRemaining;
        private float _dashRemaining;
        private float _dashCooldownRemaining;
        private float _defaultGravity;
        private float _moveX;
        private bool _jumpHeld;
        private int _facing = 1;
        private float _invulnerableUntil;
        private Vector2 _supportVelocity;
        private MovingPlatform _supportPlatform;
        private TrailRenderer _dashTrail;
        private bool _airDashReady = true;
        private bool _jumpPressedWhileAirborne;

        public bool IsGrounded => _groundContacts.Count > 0;
        public bool IsDashing => _dashRemaining > 0f;
        public bool AirDashReady => IsGrounded || _airDashReady;
        public Rigidbody2D Body => _body;
        public int JumpCount { get; private set; }
        public int BufferedJumpCount { get; private set; }
        public int JumpCutCount { get; private set; }
        public bool LastJumpUsedCoyoteTime { get; private set; }
        public int DashCount { get; private set; }
        public int AirDashCount { get; private set; }
        public float TotalDashDistance { get; private set; }
        public bool DashTrailEmitting => _dashTrail != null && _dashTrail.emitting;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _defaultGravity = _body.gravityScale;
            _visualRoot = transform.Find("Visual");
            _dashTrail = GetComponent<TrailRenderer>();

            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            _move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick/x");
            _move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Gamepad>/dpad/left")
                .With("Positive", "<Gamepad>/dpad/right");

            _jump = new InputAction("Jump", InputActionType.Button);
            _jump.AddBinding("<Keyboard>/space");
            _jump.AddBinding("<Keyboard>/w");
            _jump.AddBinding("<Keyboard>/upArrow");
            _jump.AddBinding("<Gamepad>/buttonSouth");

            _dash = new InputAction("Dash", InputActionType.Button);
            _dash.AddBinding("<Keyboard>/leftShift");
            _dash.AddBinding("<Keyboard>/x");
            _dash.AddBinding("<Gamepad>/buttonEast");
        }

        private void OnEnable()
        {
            _move?.Enable();
            _jump?.Enable();
            _dash?.Enable();
        }

        private void OnDisable()
        {
            _move?.Disable();
            _jump?.Disable();
            _dash?.Disable();
        }

        private void OnDestroy()
        {
            _move?.Dispose();
            _jump?.Dispose();
            _dash?.Dispose();
        }

        private void Update()
        {
            if (ScrapDashGame.Instance?.BlocksPlayerControl == true)
            {
                _moveX = 0f;
                _jumpBufferRemaining = 0f;
                return;
            }

            _moveX = Mathf.Clamp(_move.ReadValue<float>(), -1f, 1f);
            _jumpHeld = _jump.IsPressed();

            if (Mathf.Abs(_moveX) > 0.05f)
            {
                _facing = _moveX > 0f ? 1 : -1;
                if (_visualRoot != null)
                {
                    var scale = _visualRoot.localScale;
                    scale.x = Mathf.Abs(scale.x) * _facing;
                    _visualRoot.localScale = scale;
                }
            }

            if (_jump.WasPressedThisFrame())
            {
                _jumpBufferRemaining = jumpBufferTime;
                _jumpPressedWhileAirborne = !IsGrounded;
            }
            else _jumpBufferRemaining = Mathf.Max(0f, _jumpBufferRemaining - Time.deltaTime);

            if (IsGrounded)
            {
                _coyoteRemaining = coyoteTime;
                _airDashReady = true;
            }
            else
            {
                _coyoteRemaining = Mathf.Max(0f, _coyoteRemaining - Time.deltaTime);
            }

            _dashCooldownRemaining = Mathf.Max(0f, _dashCooldownRemaining - Time.deltaTime);

            if (_dash.WasPressedThisFrame()
                && _dashCooldownRemaining <= 0f
                && !IsDashing
                && (IsGrounded || _airDashReady))
            {
                StartDash();
            }

            if (_jump.WasReleasedThisFrame() && _body.linearVelocity.y > 0f && !IsDashing)
            {
                _body.linearVelocity = new Vector2(_body.linearVelocity.x, _body.linearVelocity.y * 0.5f);
                JumpCutCount++;
            }

            if (transform.position.y < -7f)
            {
                FeedbackHub.Instance?.PlayDeath(transform.position);
                ScrapDashGame.Instance?.RespawnPlayer("FALL RECOVERY");
            }
        }

        private void FixedUpdate()
        {
            if (IsDashing)
            {
                _dashRemaining -= Time.fixedDeltaTime;
                _body.linearVelocity = new Vector2(_facing * dashSpeed, 0f);
                TotalDashDistance += Mathf.Abs(_body.linearVelocity.x) * Time.fixedDeltaTime;
                if (_dashRemaining <= 0f) EndDash();
                return;
            }

            if (_jumpBufferRemaining > 0f && _coyoteRemaining > 0f)
            {
                LastJumpUsedCoyoteTime = !IsGrounded;
                if (_jumpPressedWhileAirborne && IsGrounded) BufferedJumpCount++;
                JumpCount++;
                _jumpBufferRemaining = 0f;
                _coyoteRemaining = 0f;
                _jumpPressedWhileAirborne = false;
                _body.linearVelocity = new Vector2(
                    _body.linearVelocity.x,
                    jumpVelocity + Mathf.Max(0f, _supportVelocity.y)
                );
                FeedbackHub.Instance?.PlayJump();
            }

            var platformVelocity = IsGrounded ? _supportVelocity : Vector2.zero;
            var targetX = platformVelocity.x + _moveX * maxRunSpeed;
            var acceleration = IsGrounded ? groundAcceleration : airAcceleration;
            var nextX = Mathf.MoveTowards(_body.linearVelocity.x, targetX, acceleration * Time.fixedDeltaTime);
            _body.linearVelocity = new Vector2(nextX, _body.linearVelocity.y);

            if (!_jumpHeld && _body.linearVelocity.y > 0f)
            {
                _body.AddForce(Physics2D.gravity * 0.8f, ForceMode2D.Force);
            }
        }

        private void StartDash()
        {
            var startedInAir = !IsGrounded;
            if (startedInAir)
            {
                _airDashReady = false;
                AirDashCount++;
            }
            DashCount++;
            _dashRemaining = dashDuration;
            _dashCooldownRemaining = dashCooldown;
            _body.gravityScale = 0f;
            _body.linearVelocity = new Vector2(_facing * dashSpeed, 0f);
            if (_dashTrail != null)
            {
                _dashTrail.Clear();
                _dashTrail.emitting = true;
            }
            FeedbackHub.Instance?.PlayDash(transform.position);
        }

        private void EndDash()
        {
            _dashRemaining = 0f;
            _body.gravityScale = _defaultGravity;
            if (_dashTrail != null) _dashTrail.emitting = false;
        }

        public void ApplyMagnet(Vector2 force)
        {
            if (IsDashing) return;
            _body.AddForce(force, ForceMode2D.Force);
            var velocity = _body.linearVelocity;
            velocity.y = Mathf.Clamp(velocity.y, -14f, 11f);
            _body.linearVelocity = velocity;
        }

        public void TakeHit(Vector2 source)
        {
            if (Time.unscaledTime < _invulnerableUntil) return;
            _invulnerableUntil = Time.unscaledTime + 0.8f;
            EndDash();

            var direction = ((Vector2)transform.position - source).normalized;
            if (direction.sqrMagnitude < 0.1f) direction = Vector2.left;
            _body.linearVelocity = new Vector2(direction.x * 7f, 8f);
            ScrapDashGame.Instance?.NotifyHit();
            FeedbackHub.Instance?.PlayHit(transform.position);
        }

        public void Bounce(float velocity)
        {
            EndDash();
            _airDashReady = true;
            _dashCooldownRemaining = 0f;
            _groundContacts.Clear();
            _supportPlatform = null;
            _supportVelocity = Vector2.zero;
            _body.linearVelocity = new Vector2(_body.linearVelocity.x, velocity);
        }

        public void ResetMotion()
        {
            EndDash();
            _airDashReady = true;
            _groundContacts.Clear();
            _supportPlatform = null;
            _supportVelocity = Vector2.zero;
            _moveX = 0f;
            _jumpHeld = false;
            _jumpBufferRemaining = 0f;
            _coyoteRemaining = 0f;
            _jumpPressedWhileAirborne = false;
            _invulnerableUntil = Time.unscaledTime + 0.65f;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            var supports = false;
            foreach (var contact in collision.contacts)
            {
                if (contact.normal.y > 0.55f)
                {
                    supports = true;
                    break;
                }
            }

            if (supports)
            {
                _groundContacts.Add(collision.collider);
                var platform = collision.collider.GetComponent<MovingPlatform>();
                if (platform != null)
                {
                    _supportPlatform = platform;
                    _supportVelocity = platform.Velocity;
                }
            }
            else
            {
                _groundContacts.Remove(collision.collider);
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            _groundContacts.Remove(collision.collider);
            if (_supportPlatform != null && collision.collider.gameObject == _supportPlatform.gameObject)
            {
                _supportPlatform = null;
                _supportVelocity = Vector2.zero;
            }
        }
    }
}
