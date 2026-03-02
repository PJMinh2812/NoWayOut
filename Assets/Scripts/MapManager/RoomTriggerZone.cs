using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Trigger zone đặt ở cửa phòng - khi player bước vào sẽ kích hoạt spawn enemies.
    /// Dùng với LevelManager_TheAwakening.useRoomTriggers = true
    /// 
    /// SETUP:
    /// 1. Tạo Empty GameObject tại cửa phòng
    /// 2. Add BoxCollider2D (IsTrigger = true)
    /// 3. Attach script này
    /// 4. Đặt roomGroupName khớp với tên group trong EnemySpawnManager
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class RoomTriggerZone : MonoBehaviour
    {
        [Header("Room Settings")]
        [Tooltip("Tên group spawn (khớp với SpawnGroup.groupName)")]
        [SerializeField] private string roomGroupName = "Room_Start";

        [Tooltip("Chỉ trigger một lần")]
        [SerializeField] private bool triggerOnce = true;

        [Header("Visual")]
        [SerializeField] private Color gizmoColor = new Color(0f, 1f, 1f, 0.3f);

        private bool _hasTriggered = false;

        private void Awake()
        {
            // Đảm bảo collider là trigger
            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;
            if (triggerOnce && _hasTriggered) return;

            _hasTriggered = true;

            Debug.Log($"[RoomTriggerZone] Player entered '{roomGroupName}'");

            // Sử dụng LevelManager
            var levelMgr = FindFirstObjectByType<LevelManager_TheAwakening>();
            if (levelMgr != null)
            {
                levelMgr.OnPlayerEnterRoom(roomGroupName);
                return;
            }

            // Fallback: sử dụng EnemySpawnManager trực tiếp
            if (EnemySpawnManager.Instance != null)
            {
                EnemySpawnManager.Instance.ActivateGroup(roomGroupName);
            }
        }

        private void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col == null) return;

            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(
                transform.position + (Vector3)col.offset,
                col.size);

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (col.size.y / 2f + 0.3f),
                $"🚪 {roomGroupName}");
#endif
        }
    }
}
