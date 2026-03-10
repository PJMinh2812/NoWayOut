using UnityEngine;

namespace ProceduralGeneration.Data
{
    /// <summary>
    /// ScriptableObject chứa pool đồ trang trí ngẫu nhiên cho phòng
    /// </summary>
    [CreateAssetMenu(fileName = "New Decor Data", menuName = "Procedural Generation/Decor Data")]
    public class DecorData : ScriptableObject
    {
        [Header("Decor Pool")]
        public DecorItem[] items;

        [Header("Density")]
        [Range(0f, 1f)]
        [Tooltip("Tỷ lệ ô interior sẽ có decor (0.1 = 10%)")]
        public float density = 0.15f;

        [Header("Constraints")]
        [Tooltip("Khoảng cách tối thiểu giữa 2 decor (tiles)")]
        public int minSpacing = 2;

        [Tooltip("Khoảng cách tối thiểu từ door (tiles)")]
        public int doorClearance = 3;
    }

    [System.Serializable]
    public class DecorItem
    {
        public GameObject prefab;

        [Tooltip("Xác suất chọn (weight)")]
        public float weight = 1f;

        [Tooltip("Đồ vật chiếm bao nhiêu ô")]
        public Vector2Int size = Vector2Int.one;

        [Tooltip("Có chặn di chuyển player không")]
        public bool blocksMovement = false;

        [Tooltip("Sorting order offset")]
        public int sortingOrderOffset = 0;
    }
}
