using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Checkpoint - Player chạm vào sẽ cập nhật vị trí respawn.
    /// Khi chết, player sẽ hồi sinh tại checkpoint gần nhất đã kích hoạt.
    /// 
    /// SETUP:
    /// 1. Tạo Empty GameObject tại vị trí checkpoint
    /// 2. Add BoxCollider2D (IsTrigger = true)
    /// 3. Attach script này
    /// 4. Optional: thêm SpriteRenderer để hiển thị marker
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Visual")]
        [Tooltip("Sprite khi chưa kích hoạt")]
        [SerializeField] private Sprite inactiveSprite;

        [Tooltip("Sprite khi đã kích hoạt")]
        [SerializeField] private Sprite activeSprite;

        [Tooltip("Màu khi kích hoạt")]
        [SerializeField] private Color activeColor = Color.cyan;

        [Header("Audio")]
        [SerializeField] private AudioClip activateSound;

        [Header("Checkpoint ID")]
        [Tooltip("Thứ tự checkpoint (dùng để xác định tiến trình)")]
        [SerializeField] private int checkpointOrder = 0;

        private bool _isActivated = false;
        private SpriteRenderer _sr;

        public bool IsActivated => _isActivated;
        public int Order => checkpointOrder;

        private void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            _sr = GetComponent<SpriteRenderer>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;
            if (_isActivated) return;

            Activate();
        }

        private void Activate()
        {
            _isActivated = true;

            // Update spawn point
            if (PlayerSpawnManager.Instance != null)
            {
                PlayerSpawnManager.Instance.SetCheckpoint(transform);
            }

            // Visual feedback
            if (_sr != null)
            {
                _sr.color = activeColor;
                if (activeSprite != null) _sr.sprite = activeSprite;
            }

            // Audio
            if (activateSound != null)
            {
                AudioSource.PlayClipAtPoint(activateSound, transform.position);
            }

            Debug.Log($"[Checkpoint] Checkpoint #{checkpointOrder} activated at {transform.position}");
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _isActivated ? Color.cyan : Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Flag icon
            Gizmos.color = _isActivated ? Color.green : Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.2f);

#if UNITY_EDITOR
            UnityEditor.Handles.color = _isActivated ? Color.cyan : Color.yellow;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f,
                $"CP #{checkpointOrder}" + (_isActivated ? " ✓" : ""));
#endif
        }
    }
}
