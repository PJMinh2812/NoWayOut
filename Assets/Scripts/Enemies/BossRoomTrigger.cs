using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Cảm biến vô hình đặt ở cửa phòng boss.
    /// Khi Player bước vào vùng trigger, sẽ kích hoạt BossCinematicController.
    ///
    /// SETUP INSPECTOR:
    ///   1. Tạo một GameObject trống, đặt ở cửa phòng boss.
    ///   2. Gắn Collider2D (BoxCollider2D hoặc CircleCollider2D), đánh dấu IsTrigger = true.
    ///   3. Gắn script này + kéo BossCinematicController vào.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossRoomTrigger : MonoBehaviour
    {
        [Tooltip("Cinematic controller trong boss room")]
        [SerializeField] private BossCinematicController cinematic;

        private void Awake()
        {
            // Đảm bảo Collider2D luôn là trigger
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Chỉ kích hoạt khi player chạm vào
            if (other.GetComponent<PlayerController2D>() == null) return;
            if (cinematic == null) return;

            cinematic.PlayCinematic();

            // Hủy trigger sau khi dùng – cinematic chỉ phát 1 lần duy nhất
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.35f);
            var col = GetComponent<Collider2D>();
            if (col is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.offset, box.size);
                Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
                Gizmos.DrawWireCube(box.offset, box.size);
            }
            else if (col is CircleCollider2D circle)
            {
                Gizmos.DrawSphere((Vector3)circle.offset + transform.position, circle.radius);
            }
        }
#endif
    }
}
