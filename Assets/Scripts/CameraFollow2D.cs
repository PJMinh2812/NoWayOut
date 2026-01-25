using UnityEngine;

namespace GloomCraft
{
    /// <summary>
    /// Lightweight camera follow for 2D top-down.
    /// </summary>
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.10f;
        [SerializeField] private Vector3 offset = new(0, 0, -10);

        private Vector3 _velocity;

        public void SetTarget(Transform t) => target = t;

        private void LateUpdate()
        {
            if (target == null) return;
            var desired = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
        }
    }
}


