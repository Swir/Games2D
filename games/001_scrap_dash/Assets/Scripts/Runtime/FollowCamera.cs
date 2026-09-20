using UnityEngine;

namespace ScrapDash
{
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Vector2 baseOffset = new(1.2f, 1.2f);
        [SerializeField] private float horizontalLookAhead = 1.4f;
        [SerializeField] private float verticalLookAhead = 0.8f;
        [SerializeField] private float lookAheadSmoothTime = 0.12f;
        [SerializeField] private float positionSmoothTime = 0.16f;
        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 25f;
        [SerializeField] private float minY = -0.5f;
        [SerializeField] private float maxY = 3.5f;

        private Transform _target;
        private Rigidbody2D _targetBody;
        private Vector2 _lookAhead;
        private Vector2 _lookAheadVelocity;
        private Vector3 _positionVelocity;

        public Transform Target => _target;

        public void SetTarget(Transform target)
        {
            _target = target;
            _targetBody = target != null ? target.GetComponent<Rigidbody2D>() : null;
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (_target == null) return;
            _lookAhead = Vector2.zero;
            _lookAheadVelocity = Vector2.zero;
            _positionVelocity = Vector3.zero;
            transform.position = DesiredPosition(Vector2.zero);
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            var velocity = _targetBody != null ? _targetBody.linearVelocity : Vector2.zero;
            var desiredLookAhead = new Vector2(
                Mathf.Clamp(velocity.x / 8.5f, -1f, 1f) * horizontalLookAhead,
                Mathf.Clamp(velocity.y / 13.5f, -1f, 1f) * verticalLookAhead
            );
            _lookAhead = Vector2.SmoothDamp(
                _lookAhead,
                desiredLookAhead,
                ref _lookAheadVelocity,
                lookAheadSmoothTime
            );
            transform.position = Vector3.SmoothDamp(
                transform.position,
                DesiredPosition(_lookAhead),
                ref _positionVelocity,
                positionSmoothTime
            );
        }

        private Vector3 DesiredPosition(Vector2 lookAhead)
        {
            return new Vector3(
                Mathf.Clamp(_target.position.x + baseOffset.x + lookAhead.x, minX, maxX),
                Mathf.Clamp(_target.position.y + baseOffset.y + lookAhead.y, minY, maxY),
                -10f
            );
        }
    }
}
