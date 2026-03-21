using UnityEngine;
using ProceduralGeneration.Data;

namespace ProceduralGeneration.Components
{
    /// <summary>
    /// Editor-only component để hiển thị Gizmos khi thiết kế room prefab thủ công.
    /// Attach vào root GO của room template, không làm gì ở runtime.
    /// </summary>
    [ExecuteInEditMode]
    public class RoomTemplateMarker : MonoBehaviour
    {
        [Header("Room Config")]
        public RoomData roomData;

        [Tooltip("Override kích thước room (world units = tiles). Default = roomData.size")]
        public Vector2Int roomSize;

        private void OnDrawGizmos()
        {
            if (roomData == null) return;

            Vector2Int size = roomSize.x > 0 && roomSize.y > 0 ? roomSize : roomData.size;
            float w = size.x;
            float h = size.y;

            Vector3 origin = transform.position;
            Vector3 center = origin + new Vector3(w * 0.5f, h * 0.5f, 0f);

            // Room bounds - yellow
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.85f);
            Gizmos.DrawWireCube(center, new Vector3(w, h, 0f));

            // Safe decor area (2 tiles padding) - green
            if (w > 4 && h > 4)
            {
                Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.35f);
                Gizmos.DrawWireCube(center, new Vector3(w - 4f, h - 4f, 0f));
            }

            // Center marker
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(center, 0.25f);

            // Door markers
            if (roomData.doorAnchors == null) return;

            foreach (var door in roomData.doorAnchors)
            {
                Vector3 doorPos = GetDoorWorldPos(origin, door, w, h);
                Gizmos.color = GetDoorColor(door.direction);
                Gizmos.DrawSphere(doorPos, 0.4f);
                Gizmos.DrawLine(doorPos, doorPos + GetDoorNormal(door.direction) * 1.5f);
            }
        }

        private Vector3 GetDoorWorldPos(Vector3 origin, DoorAnchor door, float w, float h)
        {
            float offsetX = door.localPosition.x;
            float offsetY = door.localPosition.y;
            switch (door.direction)
            {
                case DoorDirection.Top:    return origin + new Vector3(w * 0.5f + offsetX, h,        0f);
                case DoorDirection.Bottom: return origin + new Vector3(w * 0.5f + offsetX, 0f,       0f);
                case DoorDirection.Left:   return origin + new Vector3(0f,                 h * 0.5f + offsetY, 0f);
                case DoorDirection.Right:  return origin + new Vector3(w,                  h * 0.5f + offsetY, 0f);
                default:                   return origin;
            }
        }

        private static Color GetDoorColor(DoorDirection dir)
        {
            switch (dir)
            {
                case DoorDirection.Top:    return Color.cyan;
                case DoorDirection.Bottom: return Color.magenta;
                case DoorDirection.Left:   return Color.yellow;
                case DoorDirection.Right:  return Color.green;
                default:                   return Color.white;
            }
        }

        private static Vector3 GetDoorNormal(DoorDirection dir)
        {
            switch (dir)
            {
                case DoorDirection.Top:    return Vector3.up;
                case DoorDirection.Bottom: return Vector3.down;
                case DoorDirection.Left:   return Vector3.left;
                case DoorDirection.Right:  return Vector3.right;
                default:                   return Vector3.zero;
            }
        }
    }
}
