using UnityEngine;
using System.Collections.Generic;

namespace ProceduralGeneration.Data
{
    /// <summary>
    /// ScriptableObject chứa thông tin về một loại phòng trong dungeon
    /// </summary>
    [CreateAssetMenu(fileName = "New Room", menuName = "Procedural Generation/Room Data")]
    public class RoomData : ScriptableObject
    {
        [Header("Room Information")]
        [Tooltip("Prefab của phòng, phải chứa các DoorAnchor và SpawnPoint")]
        public GameObject roomPrefab;
        
        [Tooltip("Loại phòng trong dungeon flow")]
        public RoomType roomType;
        
        [Header("Grid Size")]
        [Tooltip("Kích thước phòng trên lưới (1x1 = 1 ô)")]
        public Vector2Int size = Vector2Int.one;
        
        [Header("Door Configuration")]
        [Tooltip("Vị trí các cửa: Top, Bottom, Left, Right (relative to room center)")]
        public List<DoorAnchor> doorAnchors = new List<DoorAnchor>();
        
        [Header("Spawn Settings")]
        [Tooltip("Số lượng spawn point tối đa cho bẫy")]
        public int maxTrapSpawnPoints = 3;
        
        [Tooltip("Spawn rate cho enemies (0-1)")]
        [Range(0f, 1f)]
        public float enemySpawnRate = 0.5f;
        
        [Header("Visual")]
        [Tooltip("Màu hiển thị trong Editor")]
        public Color editorColor = Color.white;
    }

    /// <summary>
    /// Các loại phòng trong dungeon theo game flow
    /// </summary>
    public enum RoomType
    {
        Start,          // Phòng bắt đầu
        Archetype1,     // Phòng thường giai đoạn 1
        Archetype2,     // Phòng thường giai đoạn 2 (khó hơn)
        MidBoss,        // Phòng mid-boss
        Boss,           // Phòng boss cuối
        Goal,           // Phòng mục tiêu/kết thúc
        Treasure,       // Phòng bonus (optional)
        Secret          // Phòng bí mật (optional)
    }

    /// <summary>
    /// Thông tin về một cửa của phòng
    /// </summary>
    [System.Serializable]
    public class DoorAnchor
    {
        [Tooltip("Hướng của cửa")]
        public DoorDirection direction;
        
        [Tooltip("Vị trí local của cửa trong phòng")]
        public Vector3 localPosition;
        
        [Tooltip("GameObject của cửa (sẽ được enable/disable)")]
        public GameObject doorObject;
        
        [Tooltip("GameObject của tường (sẽ thay thế cửa nếu không có phòng kế bên)")]
        public GameObject wallObject;
    }

    /// <summary>
    /// Hướng của cửa
    /// </summary>
    public enum DoorDirection
    {
        Top,        // Phía trên
        Bottom,     // Phía dưới
        Left,       // Phía trái
        Right       // Phía phải
    }
}
