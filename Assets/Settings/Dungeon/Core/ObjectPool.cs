using System.Collections.Generic;
using UnityEngine;

namespace NWO.Dungeon
{
    /// <summary>
    /// Generic object pool for performance optimization.
    /// Reuses GameObjects instead of creating/destroying them.
    /// </summary>
    public sealed class GameObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Queue<GameObject> _available = new();
        private readonly HashSet<GameObject> _inUse = new();
        private readonly string _poolName;

        public int AvailableCount => _available.Count;
        public int InUseCount => _inUse.Count;
        public int TotalCount => AvailableCount + InUseCount;

        public GameObjectPool(GameObject prefab, Transform parent, int initialSize = 0, string name = null)
        {
            _prefab = prefab;
            _parent = parent;
            _poolName = name ?? (prefab != null ? prefab.name : "Pool");

            // Pre-warm pool
            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateNewObject();
                obj.SetActive(false);
                _available.Enqueue(obj);
            }
        }

        /// <summary>
        /// Get an object from the pool. Creates new if none available.
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj;

            if (_available.Count > 0)
            {
                obj = _available.Dequeue();
            }
            else
            {
                obj = CreateNewObject();
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            _inUse.Add(obj);
            return obj;
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;
            if (!_inUse.Contains(obj)) return;

            _inUse.Remove(obj);
            obj.SetActive(false);
            _available.Enqueue(obj);
        }

        /// <summary>
        /// Return all in-use objects to the pool.
        /// </summary>
        public void ReturnAll()
        {
            foreach (var obj in _inUse)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    _available.Enqueue(obj);
                }
            }
            _inUse.Clear();
        }

        /// <summary>
        /// Destroy all pooled objects.
        /// </summary>
        public void Clear()
        {
            foreach (var obj in _available)
            {
                if (obj != null) Object.Destroy(obj);
            }
            foreach (var obj in _inUse)
            {
                if (obj != null) Object.Destroy(obj);
            }
            _available.Clear();
            _inUse.Clear();
        }

        private GameObject CreateNewObject()
        {
            GameObject obj;
            
            if (_prefab != null)
            {
                obj = Object.Instantiate(_prefab, _parent);
            }
            else
            {
                obj = new GameObject($"{_poolName}_{TotalCount}");
                obj.transform.SetParent(_parent, false);
            }

            return obj;
        }
    }

    /// <summary>
    /// Pool specifically for SpriteRenderer overlays (wall tiles, effects, etc.)
    /// </summary>
    public sealed class SpriteOverlayPool
    {
        private readonly GameObjectPool _pool;
        private readonly string _sortingLayer;
        private readonly int _baseSortingOrder;

        public SpriteOverlayPool(Transform parent, int initialSize = 50, 
            string sortingLayer = "Default", int baseSortingOrder = 1)
        {
            _sortingLayer = sortingLayer;
            _baseSortingOrder = baseSortingOrder;
            _pool = new GameObjectPool(null, parent, 0, "SpriteOverlay");

            // Pre-warm with sprite renderers
            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateSpriteObject(parent);
                obj.SetActive(false);
                // Return to pool via reflection workaround - just let pool handle it
            }
        }

        private GameObject CreateSpriteObject(Transform parent)
        {
            var go = new GameObject("Overlay");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = _sortingLayer;
            sr.sortingOrder = _baseSortingOrder;
            return go;
        }

        /// <summary>
        /// Get a sprite overlay at the specified position.
        /// </summary>
        public GameObject Get(Vector3 position, Sprite sprite, int orderOffset = 0)
        {
            var obj = _pool.Get(position, Quaternion.identity);
            
            // Ensure has SpriteRenderer
            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = obj.AddComponent<SpriteRenderer>();
            
            sr.sprite = sprite;
            sr.sortingLayerName = _sortingLayer;
            sr.sortingOrder = _baseSortingOrder + orderOffset;
            
            return obj;
        }

        public void ReturnAll() => _pool.ReturnAll();
        public void Clear() => _pool.Clear();
    }
}
