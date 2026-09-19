using UnityEngine;

namespace ScrapDash
{
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Vector2 offset = new(2.2f, 1.2f);
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 25f;
        [SerializeField] private float minY = -0.5f;
        [SerializeField] private float maxY = 3.5f;

        private Transform _target;
        private Vector3 _velocity;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            var target = new Vector3(
                Mathf.Clamp(_target.position.x + offset.x, minX, maxX),
                Mathf.Clamp(_target.position.y + offset.y, minY, maxY),
                -10f
            );
            transform.position = Vector3.SmoothDamp(transform.position, target, ref _velocity, smoothTime);
        }
    }
}
