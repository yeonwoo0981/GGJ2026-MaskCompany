using UnityEngine;

namespace MaskCompany
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        
        [Header("Follow Settings")]
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
        
        [Header("Bounds (Optional)")]
        [SerializeField] private bool useBounds;
        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 10f;
        [SerializeField] private float minY = -10f;
        [SerializeField] private float maxY = 10f;

        private Vector3 velocity = Vector3.zero;

        private void Start()
        {
            if (target == null)
            {
                var player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    target = player.transform;
                }
            }

            // Snap to target on start
            if (target != null)
            {
                Vector3 targetPos = target.position + offset;
                if (useBounds)
                {
                    targetPos = ClampToBounds(targetPos);
                }
                transform.position = targetPos;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            
            if (useBounds)
            {
                desiredPosition = ClampToBounds(desiredPosition);
            }

            // SmoothDamp for buttery smooth follow without jitter
            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
            transform.position = smoothedPosition;
        }

        private Vector3 ClampToBounds(Vector3 pos)
        {
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            return pos;
        }

        /// <summary>
        /// Set bounds from a collider (e.g., level boundary)
        /// </summary>
        public void SetBoundsFromCollider(Collider2D boundsCollider)
        {
            if (boundsCollider == null) return;
            
            Bounds b = boundsCollider.bounds;
            minX = b.min.x;
            maxX = b.max.x;
            minY = b.min.y;
            maxY = b.max.y;
            useBounds = true;
        }

        private void OnDrawGizmosSelected()
        {
            if (!useBounds) return;
            
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
