using UnityEngine;

namespace ProceduralGeneration.Data
{
    /// <summary>
    /// ScriptableObject chứa thông tin về một loại bẫy
    /// </summary>
    [CreateAssetMenu(fileName = "New Trap", menuName = "Procedural Generation/Trap Data")]
    public class TrapData : ScriptableObject
    {
        [Header("Trap Prefab")]
        [Tooltip("Prefab của bẫy")]
        public GameObject trapPrefab;
        
        [Header("Difficulty Settings")]
        [Tooltip("Điểm nguy hiểm của bẫy (cao hơn = nguy hiểm hơn)")]
        [Range(1, 10)]
        public int dangerScore = 5;
        
        [Tooltip("Danger level tối thiểu để spawn bẫy này")]
        [Range(0, 10)]
        public int minDangerLevel = 0;
        
        [Header("Spawn Configuration")]
        [Tooltip("Xác suất spawn bẫy này khi đủ điều kiện (0-1)")]
        [Range(0f, 1f)]
        public float spawnProbability = 1f;
        
        [Tooltip("Logic spawn bẫy")]
        public TrapSpawnLogic spawnLogic = TrapSpawnLogic.Random;
        
        [Tooltip("Số lượng tối đa trong một phòng")]
        public int maxPerRoom = 3;
        
        [Header("Placement Rules")]
        [Tooltip("Khoảng cách tối thiểu từ cửa (để không chặn lối đi)")]
        public float minDistanceFromDoor = 2f;
        
        [Tooltip("Có thể đặt trên đường đi chính không?")]
        public bool canBlockMainPath = false;
        
        [Tooltip("Kích thước vùng chiếm dụng (để kiểm tra overlap)")]
        public Vector2 occupiedArea = Vector2.one;
        
        [Header("Visual")]
        [Tooltip("Icon hiển thị trong Editor")]
        public Sprite editorIcon;
        
        [Tooltip("Màu hiển thị trong Editor")]
        public Color editorColor = Color.red;
    }

    /// <summary>
    /// Logic spawn bẫy
    /// </summary>
    public enum TrapSpawnLogic
    {
        Random,             // Spawn ngẫu nhiên
        NearEntrance,       // Ưu tiên gần cửa vào
        NearExit,           // Ưu tiên gần cửa ra
        PathBlocking,       // Ưu tiên chặn đường đi
        RoomCenter,         // Ưu tiên ở giữa phòng
        Corners,            // Ưu tiên ở góc phòng
        AlongWalls          // Dọc theo tường
    }
}
