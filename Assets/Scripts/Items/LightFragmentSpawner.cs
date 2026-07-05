using UnityEngine;
using UnityEngine.Rendering.Universal;
using ProceduralGeneration.Core;
using ProceduralGeneration.Data;
using System.Collections.Generic;

namespace NWO
{
    /// <summary>
    /// Tá»± Ä‘á»™ng spawn Light Fragments vÃ o cÃ¡c phÃ²ng khi dungeon Ä‘Æ°á»£c táº¡o.
    /// Attach vÃ o DungeonManager hoáº·c Ä‘á»ƒ trong scene.
    /// </summary>
    public class LightFragmentSpawner : MonoBehaviour
    {
        [Header("Fragment Settings")]
        [Tooltip("Prefab Light Fragment (náº¿u null sáº½ táº¡o runtime)")]
        [SerializeField] private GameObject lightFragmentPrefab;
        
        [Tooltip("Tá»•ng sá»‘ Light Fragment cáº§n spawn")]
        [SerializeField] private int totalFragments = 3;
        
        [Tooltip("Chá»‰ spawn trong room types nÃ y")]
        [SerializeField] private RoomType[] spawnInRoomTypes = { RoomType.Archetype1, RoomType.Archetype2 };
        
        [Header("Visual (náº¿u khÃ´ng cÃ³ prefab)")]
        [SerializeField] private Sprite fragmentSprite;
        [SerializeField] private Color fragmentColor = new Color(1f, 0.95f, 0.6f);
        [SerializeField] private float fragmentScale = 0.8f;
        
        private int fragmentIDCounter = 0;

        /// <summary>
        /// Spawn cố định cho run mới map 1-1:
        /// - Archetype1 đầu tiên
        /// - Archetype2 đầu tiên
        /// - Boss
        /// </summary>
        public void SpawnFragmentsForRunStart(DungeonManager dungeonManager)
        {
            if (dungeonManager == null) return;

            var allRooms = dungeonManager.GetAllRooms();
            if (allRooms == null || allRooms.Count == 0) return;

            ClearExistingFragments(dungeonManager);

            var targets = new List<Room>();

            Room firstArchetype1 = FindFirstRoomByType(allRooms, RoomType.Archetype1);
            Room firstArchetype2 = FindFirstRoomByType(allRooms, RoomType.Archetype2);
            Room bossRoom = FindFirstRoomByType(allRooms, RoomType.Boss);

            if (firstArchetype1 != null) targets.Add(firstArchetype1);
            if (firstArchetype2 != null && firstArchetype2 != firstArchetype1) targets.Add(firstArchetype2);
            if (bossRoom != null && bossRoom != firstArchetype1 && bossRoom != firstArchetype2) targets.Add(bossRoom);

            if (targets.Count == 0)
            {
                Debug.LogWarning("[LightFragmentSpawner] No valid target rooms for run-start fragment spawn.");
                return;
            }

            int count = Mathf.Min(totalFragments, targets.Count);
            fragmentIDCounter = 0;
            for (int i = 0; i < count; i++)
            {
                SpawnFragmentInRoom(targets[i], i + 1);
            }

            Debug.Log($"[LightFragmentSpawner] Spawned {count} fragments for run start (A1-first, A2-first, Boss).");
        }
        
        /// <summary>
        /// Gá»i sau khi dungeon Ä‘Ã£ generate xong Ä‘á»ƒ spawn Light Fragments
        /// </summary>
        public void SpawnFragmentsInDungeon(DungeonManager dungeonManager)
        {
            if (dungeonManager == null) return;
            
            var allRooms = dungeonManager.GetAllRooms();
            if (allRooms == null || allRooms.Count == 0) return;
            
            // Lá»c rooms phÃ¹ há»£p Ä‘á»ƒ spawn
            var eligibleRooms = new System.Collections.Generic.List<Room>();
            foreach (var room in allRooms)
            {
                if (room.roomInstance == null) continue;
                
                foreach (var allowedType in spawnInRoomTypes)
                {
                    if (room.roomData.roomType == allowedType)
                    {
                        eligibleRooms.Add(room);
                        break;
                    }
                }
            }
            
            if (eligibleRooms.Count == 0)
            {
                Debug.LogWarning("[LightFragmentSpawner] No eligible rooms found for Light Fragment spawning!");
                return;
            }
            
            // Chá»n random rooms Ä‘á»ƒ spawn (khÃ´ng trÃ¹ng)
            int count = Mathf.Min(totalFragments, eligibleRooms.Count);
            var selectedRooms = new System.Collections.Generic.List<Room>();
            var shuffled = new System.Collections.Generic.List<Room>(eligibleRooms);
            
            // Fisher-Yates shuffle
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }
            
            for (int i = 0; i < count; i++)
            {
                selectedRooms.Add(shuffled[i]);
            }
            
            // Spawn fragment trong má»—i room
            fragmentIDCounter = 0;
            foreach (var room in selectedRooms)
            {
                SpawnFragmentInRoom(room);
            }
            
            // Update GameManager total count
            if (GameManager.Instance != null)
            {
                // DÃ¹ng reflection hoáº·c public method Ä‘á»ƒ set total
                // Hiá»‡n táº¡i GameManager Ä‘Ã£ cÃ³ totalLightFragments = 3 serialized

            }
        }
        
        /// <summary>
        /// Spawn 1 Light Fragment á»Ÿ vá»‹ trÃ­ random trong room (interior, trÃ¡nh wall)
        /// </summary>
        private void SpawnFragmentInRoom(Room room, int forcedFragmentId = -1)
        {
            fragmentIDCounter++;
            int fragmentId = forcedFragmentId > 0 ? forcedFragmentId : fragmentIDCounter;
            
            // TÃ­nh vÃ¹ng interior cá»§a room (trÃ¡nh wall = 2 tile tá»« edge)
            Vector3 roomOrigin = room.roomInstance.transform.position;
            float roomWidth = room.actualSize.x * 11f; // actualSize * tiles per unit
            float roomHeight = room.actualSize.y * 11f;
            
            // Fallback: tÃ­nh tá»« renderer bounds
            var renderers = room.roomInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers)
                    bounds.Encapsulate(r.bounds);
                
                roomOrigin = bounds.min;
                roomWidth = bounds.size.x;
                roomHeight = bounds.size.y;
            }
            
            // Random position trong interior (offset 2 tiles tá»« wall)
            float margin = 2.5f;
            float x = Random.Range(roomOrigin.x + margin, roomOrigin.x + roomWidth - margin);
            float y = Random.Range(roomOrigin.y + margin, roomOrigin.y + roomHeight - margin);
            Vector3 spawnPos = new Vector3(x, y, 0);
            
            // Táº¡o fragment
            GameObject fragmentObj;
            if (lightFragmentPrefab != null)
            {
                fragmentObj = Instantiate(lightFragmentPrefab, spawnPos, Quaternion.identity, room.roomInstance.transform);
            }
            else
            {
                // Táº¡o runtime fragment
                fragmentObj = CreateRuntimeFragment(spawnPos, room.roomInstance.transform);
            }
            
            fragmentObj.name = $"LightFragment_{fragmentId}";
            
            // Configure LightFragment component
            var fragment = fragmentObj.GetComponent<LightFragment>();
            if (fragment == null)
                fragment = fragmentObj.AddComponent<LightFragment>();
            fragment.Configure(fragmentId, $"Light Fragment #{fragmentId}");

        }

        private Room FindFirstRoomByType(List<Room> rooms, RoomType roomType)
        {
            Room selected = null;
            int bestDistance = int.MaxValue;

            foreach (var room in rooms)
            {
                if (room == null || room.roomInstance == null || room.roomData == null)
                    continue;

                if (room.roomData.roomType != roomType)
                    continue;

                if (room.distanceFromStart < bestDistance)
                {
                    bestDistance = room.distanceFromStart;
                    selected = room;
                }
            }

            return selected;
        }

        private void ClearExistingFragments(DungeonManager dungeonManager)
        {
            if (dungeonManager == null || dungeonManager.dungeonContainer == null)
                return;

            var existing = dungeonManager.dungeonContainer.GetComponentsInChildren<LightFragment>(true);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(existing[i].gameObject);
                else
                    DestroyImmediate(existing[i].gameObject);
            }
        }
        
        /// <summary>
        /// Táº¡o Light Fragment object runtime (khi khÃ´ng cÃ³ prefab)
        /// </summary>
        private GameObject CreateRuntimeFragment(Vector3 position, Transform parent)
        {
            GameObject obj = new GameObject("LightFragment");
            obj.transform.position = position;
            obj.transform.SetParent(parent);
            obj.transform.localScale = Vector3.one * fragmentScale;
            obj.layer = LayerMask.NameToLayer("Default");
            
            // Sprite
            var sr = obj.AddComponent<SpriteRenderer>();
            if (fragmentSprite != null)
            {
                sr.sprite = fragmentSprite;
            }
            else
            {
                // Táº¡o simple diamond sprite
                Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                Color32[] pixels = new Color32[16 * 16];
                // Váº½ hÃ¬nh kim cÆ°Æ¡ng
                for (int px = 0; px < 16; px++)
                {
                    for (int py = 0; py < 16; py++)
                    {
                        int dx = Mathf.Abs(px - 8);
                        int dy = Mathf.Abs(py - 8);
                        if (dx + dy <= 7)
                            pixels[py * 16 + px] = new Color32(255, 240, 150, 255);
                        else
                            pixels[py * 16 + px] = new Color32(0, 0, 0, 0);
                    }
                }
                tex.SetPixels32(pixels);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            }
            sr.color = fragmentColor;
            sr.sortingOrder = 5;
            
            return obj;
        }
    }
}
