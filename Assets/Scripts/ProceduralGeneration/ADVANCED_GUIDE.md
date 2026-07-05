# 🎓 ADVANCED TECHNIQUES & BEST PRACTICES

Hướng dẫn nâng cao cho Procedural Dungeon Generator System

---

## 📐 ROOM DESIGN PATTERNS

### Pattern 1: Multi-Size Rooms

```csharp
// Small room (1x1)
RoomData smallRoom:
- Size: (1, 1)
- Doors: Top, Bottom, Left, Right
- Use: Corridors, transition rooms

// Medium room (2x2)
RoomData mediumRoom:
- Size: (2, 2)
- Doors: Top, Bottom, Left, Right (at edges)
- Use: Combat rooms, treasure rooms

// Large room (3x3)
RoomData largeRoom:
- Size: (3, 3)
- Doors: Multiple per side
- Use: Boss rooms, special events
```

### Pattern 2: Asymmetric Doors

```csharp
// L-shaped room
RoomData lShapedRoom:
- Size: (2, 2)
- Doors: Top, Left only
- Use: Corner rooms, special layouts
```

### Pattern 3: Multi-Entrance Rooms

```csharp
// Hub room
RoomData hubRoom:
- Size: (2, 2)
- Doors: 2x Top, 2x Bottom, 2x Left, 2x Right
- Use: Central meeting points, branching areas
```

---

## 🎯 ADVANCED GENERATION TECHNIQUES

### Technique 1: Weighted Room Selection

Modify `GetRoomDataOfType()` để ưu tiên certain rooms:

```csharp
private RoomData GetRoomDataOfType(RoomType roomType)
{
    var rooms = roomDatabase.Where(r => r.roomType == roomType).ToList();
    if (rooms.Count == 0) return null;

    // Weighted random selection
    float totalWeight = 0f;
    foreach (var room in rooms)
    {
        totalWeight += room.spawnWeight; // Add this field to RoomData
    }

    float randomValue = Random.Range(0f, totalWeight);
    float currentWeight = 0f;

    foreach (var room in rooms)
    {
        currentWeight += room.spawnWeight;
        if (randomValue <= currentWeight)
        {
            return room;
        }
    }

    return rooms[0];
}
```

### Technique 2: Theme-Based Generation

```csharp
public enum DungeonTheme
{
    Cave,
    Castle,
    Crypt,
    Temple
}

public DungeonTheme currentTheme;

private RoomData GetRoomDataOfType(RoomType roomType)
{
    // Filter by both type AND theme
    var rooms = roomDatabase.Where(r =>
        r.roomType == roomType &&
        r.theme == currentTheme).ToList();

    // Fallback to any theme if none found
    if (rooms.Count == 0)
        rooms = roomDatabase.Where(r => r.roomType == roomType).ToList();

    return rooms[Random.Range(0, rooms.Count)];
}
```

### Technique 3: Guaranteed Special Rooms

```csharp
private void GenerateDungeonFlow()
{
    // ... existing flow ...

    // Guarantee at least 1 treasure room
    if (Random.value > 0.5f)
    {
        CreateRoomOfType(RoomType.Treasure);
    }

    // Guarantee 1 secret room for exploration
    if (archetype1RoomCount + archetype2RoomCount > 8)
    {
        CreateSecretRoom();
    }
}

private void CreateSecretRoom()
{
    // Tìm phòng xa nhất từ main path
    Room farthestRoom = allRooms
        .Where(r => !r.isMainPath)
        .OrderByDescending(r => r.distanceFromStart)
        .FirstOrDefault();

    if (farthestRoom != null)
    {
        RoomData secretData = GetRoomDataOfType(RoomType.Secret);
        if (secretData != null)
        {
            Room secretRoom = TryPlaceRoom(secretData, farthestRoom);
            if (secretRoom != null)
            {
                secretRoom.isMainPath = false;
                Debug.Log("Secret room created!");
            }
        }
    }
}
```

---

## 🔧 OPTIMIZATION STRATEGIES

### Strategy 1: Object Pooling

```csharp
public class DungeonObjectPool : MonoBehaviour
{
    private Dictionary<string, Queue<GameObject>> pools;

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string key = prefab.name;

        if (!pools.ContainsKey(key))
            pools[key] = new Queue<GameObject>();

        GameObject obj;
        if (pools[key].Count > 0)
        {
            obj = pools[key].Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation);
        }

        return obj;
    }

    public void Despawn(GameObject obj)
    {
        obj.SetActive(false);
        pools[obj.name].Enqueue(obj);
    }
}

// Modify DungeonManager to use pooling
private DungeonObjectPool objectPool;

private void InstantiateAllRooms()
{
    foreach (var room in allRooms)
    {
        if (objectPool != null)
        {
            Vector3 pos = new Vector3(room.gridPosition.x * 10f, room.gridPosition.y * 10f, 0);
            room.roomInstance = objectPool.Spawn(room.roomData.roomPrefab, pos, Quaternion.identity);
        }
        else
        {
            room.InstantiateRoom(dungeonContainer);
        }
    }
}
```

### Strategy 2: Lazy Enemy Spawning

```csharp
public class RoomEnemySpawner : MonoBehaviour
{
    private Room room;
    private bool enemiesSpawned = false;
    private bool playerEntered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !enemiesSpawned)
        {
            SpawnEnemies();
            enemiesSpawned = true;
        }
    }

    private void SpawnEnemies()
    {
        foreach (var spawnPoint in room.enemySpawnPoints)
        {
            if (Random.value < room.roomData.enemySpawnRate)
            {
                // Spawn enemy
                GameObject enemy = ChooseEnemyForDangerLevel(room.dangerLevel);
                Instantiate(enemy, spawnPoint.position, Quaternion.identity);
            }
        }
    }

    private GameObject ChooseEnemyForDangerLevel(int dangerLevel)
    {
        // Your enemy selection logic
        return null;
    }
}
```

### Strategy 3: Chunk-Based Loading

```csharp
public class ChunkedDungeonLoader : MonoBehaviour
{
    public float loadRadius = 20f;
    private Transform player;
    private HashSet<Room> loadedRooms = new HashSet<Room>();

    private void Update()
    {
        if (player == null) return;

        foreach (var room in allRooms)
        {
            float distance = Vector2.Distance(
                player.position,
                new Vector2(room.gridPosition.x * 10f, room.gridPosition.y * 10f)
            );

            if (distance <= loadRadius)
            {
                LoadRoom(room);
            }
            else
            {
                UnloadRoom(room);
            }
        }
    }

    private void LoadRoom(Room room)
    {
        if (!loadedRooms.Contains(room))
        {
            if (room.roomInstance != null)
                room.roomInstance.SetActive(true);
            loadedRooms.Add(room);
        }
    }

    private void UnloadRoom(Room room)
    {
        if (loadedRooms.Contains(room))
        {
            if (room.roomInstance != null)
                room.roomInstance.SetActive(false);
            loadedRooms.Remove(room);
        }
    }
}
```

---

## 🎮 GAMEPLAY INTEGRATION

### Integration 1: Room Completion Tracking

```csharp
public class RoomCompletionTracker : MonoBehaviour
{
    public Room room;
    public bool isCompleted = false;

    private int enemiesRemaining;

    private void Start()
    {
        enemiesRemaining = room.enemySpawnPoints.Count;
    }

    public void OnEnemyKilled()
    {
        enemiesRemaining--;

        if (enemiesRemaining <= 0)
        {
            CompleteRoom();
        }
    }

    private void CompleteRoom()
    {
        isCompleted = true;

        // Unlock doors
        foreach (var door in room.doors)
        {
            door.anchorData.doorObject?.GetComponent<DoorController>()?.UnlockDoor();
        }

        // Spawn rewards
        SpawnRewards();

        Debug.Log($"Room at {room.gridPosition} completed!");
    }

    private void SpawnRewards()
    {
        // Spawn loot, XP, etc.
    }
}
```

### Integration 2: Minimap System

```csharp
public class MinimapSystem : MonoBehaviour
{
    public Camera minimapCamera;
    public RenderTexture minimapTexture;
    private Dictionary<Room, GameObject> roomIcons;

    public void GenerateMinimap(List<Room> allRooms)
    {
        roomIcons = new Dictionary<Room, GameObject>();

        foreach (var room in allRooms)
        {
            // Create minimap icon for room
            GameObject icon = CreateMinimapIcon(room);
            roomIcons[room] = icon;

            // Initially hide (fog of war)
            icon.SetActive(false);
        }
    }

    public void RevealRoom(Room room)
    {
        if (roomIcons.ContainsKey(room))
        {
            roomIcons[room].SetActive(true);
        }
    }

    private GameObject CreateMinimapIcon(Room room)
    {
        GameObject icon = GameObject.CreatePrimitive(PrimitiveType.Quad);
        icon.transform.position = new Vector3(
            room.gridPosition.x * 10f,
            room.gridPosition.y * 10f,
            0
        );
        icon.transform.localScale = new Vector3(8f, 8f, 1f);

        // Color based on room type
        Renderer renderer = icon.GetComponent<Renderer>();
        renderer.material.color = GetColorForRoomType(room.roomData.roomType);

        return icon;
    }

    private Color GetColorForRoomType(RoomType type)
    {
        switch (type)
        {
            case RoomType.Start: return Color.green;
            case RoomType.Boss: return Color.red;
            case RoomType.MidBoss: return Color.yellow;
            case RoomType.Goal: return Color.cyan;
            default: return Color.gray;
        }
    }
}
```

### Integration 3: Dynamic Difficulty

```csharp
public class DynamicDifficultyManager : MonoBehaviour
{
    public DungeonManager dungeonManager;

    private float playerSkillScore = 1f; // 0-2 range

    public void AdjustDifficultyForNextLevel()
    {
        // Scale based on player performance
        float difficultyMultiplier = playerSkillScore;

        dungeonManager.archetype1RoomCount = Mathf.RoundToInt(5 * difficultyMultiplier);
        dungeonManager.archetype2RoomCount = Mathf.RoundToInt(5 * difficultyMultiplier);
        dungeonManager.branchProbability = 0.2f * difficultyMultiplier;
    }

    public void UpdatePlayerSkillScore(float deathCount, float completionTime, float damageTaken)
    {
        // Calculate skill score based on performance metrics
        float deathPenalty = Mathf.Clamp(1f - (deathCount * 0.1f), 0.5f, 1f);
        float speedBonus = completionTime < 300f ? 1.2f : 1f;
        float damageBonus = damageTaken < 50f ? 1.1f : 1f;

        playerSkillScore = deathPenalty * speedBonus * damageBonus;
        playerSkillScore = Mathf.Clamp(playerSkillScore, 0.5f, 2f);

        Debug.Log($"Player skill score: {playerSkillScore}");
    }
}
```

---

## 🐛 ADVANCED DEBUG TECHNIQUES

### Debug 1: Visual Room Flow Debugger

```csharp
#if UNITY_EDITOR
public class DungeonFlowDebugger : MonoBehaviour
{
    public DungeonManager dungeonManager;
    public bool showFlow = true;
    public bool showDangerLevels = true;
    public bool showConnections = true;

    private void OnDrawGizmos()
    {
        if (!showFlow || dungeonManager == null) return;

        var mainPath = dungeonManager.GetMainPath(); // Add this getter
        if (mainPath == null) return;

        // Draw main path
        for (int i = 0; i < mainPath.Count - 1; i++)
        {
            Vector3 start = GetRoomCenter(mainPath[i]);
            Vector3 end = GetRoomCenter(mainPath[i + 1]);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(start, end);

            // Draw arrow
            DrawArrow(start, end);
        }

        // Draw danger levels
        if (showDangerLevels)
        {
            foreach (var room in mainPath)
            {
                Vector3 center = GetRoomCenter(room);
                UnityEditor.Handles.Label(center + Vector3.up * 2f,
                    $"DL: {room.dangerLevel}\nDist: {room.distanceFromStart}");
            }
        }
    }

    private Vector3 GetRoomCenter(Room room)
    {
        return new Vector3(room.gridPosition.x * 10f, room.gridPosition.y * 10f, 0);
    }

    private void DrawArrow(Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 arrowHead1 = end - direction * 0.5f + Vector3.Cross(direction, Vector3.forward) * 0.3f;
        Vector3 arrowHead2 = end - direction * 0.5f - Vector3.Cross(direction, Vector3.forward) * 0.3f;

        Gizmos.DrawLine(end, arrowHead1);
        Gizmos.DrawLine(end, arrowHead2);
    }
}
#endif
```

### Debug 2: Generation Statistics

```csharp
public class DungeonGenerationStats
{
    public int totalRooms;
    public int mainPathRooms;
    public int branchRooms;
    public int backtrackAttempts;
    public float generationTime;
    public Dictionary<RoomType, int> roomTypeCounts;

    public override string ToString()
    {
        string result = $"=== GENERATION STATS ===\n";
        result += $"Total Rooms: {totalRooms}\n";
        result += $"Main Path: {mainPathRooms}\n";
        result += $"Branches: {branchRooms}\n";
        result += $"Backtracks: {backtrackAttempts}\n";
        result += $"Time: {generationTime:F2}s\n";
        result += $"Room Types:\n";

        foreach (var kvp in roomTypeCounts)
        {
            result += $"  {kvp.Key}: {kvp.Value}\n";
        }

        return result;
    }
}

// Add to DungeonManager
public DungeonGenerationStats GetGenerationStats()
{
    var stats = new DungeonGenerationStats
    {
        totalRooms = allRooms.Count,
        mainPathRooms = mainPath.Count,
        branchRooms = allRooms.Count - mainPath.Count,
        roomTypeCounts = new Dictionary<RoomType, int>()
    };

    foreach (var room in allRooms)
    {
        var type = room.roomData.roomType;
        if (!stats.roomTypeCounts.ContainsKey(type))
            stats.roomTypeCounts[type] = 0;
        stats.roomTypeCounts[type]++;
    }

    return stats;
}
```

---

## 🎨 VISUAL POLISH

### Polish 1: Room Transition Effects

```csharp
public class RoomTransitionEffect : MonoBehaviour
{
    public AnimationCurve fadeInCurve;
    public float transitionDuration = 0.5f;

    public IEnumerator FadeInRoom(GameObject room)
    {
        CanvasGroup canvasGroup = room.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = room.AddComponent<CanvasGroup>();

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            canvasGroup.alpha = fadeInCurve.Evaluate(t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}
```

### Polish 2: Procedural Lighting

```csharp
public class ProceduralLighting : MonoBehaviour
{
    public GameObject lightPrefab;
    public float lightIntensityVariation = 0.3f;

    public void GenerateLightsForRoom(Room room)
    {
        // Tính số lights dựa trên room size
        int lightCount = room.roomData.size.x * room.roomData.size.y;

        for (int i = 0; i < lightCount; i++)
        {
            // Random position trong room
            Vector3 position = GetRandomPositionInRoom(room);

            // Spawn light
            GameObject light = Instantiate(lightPrefab, position, Quaternion.identity,
                room.roomInstance.transform);

            // Vary intensity
            Light lightComponent = light.GetComponent<Light>();
            if (lightComponent != null)
            {
                lightComponent.intensity *= Random.Range(1f - lightIntensityVariation,
                    1f + lightIntensityVariation);
            }
        }
    }

    private Vector3 GetRandomPositionInRoom(Room room)
    {
        Vector3 roomCenter = room.roomInstance.transform.position;
        float x = Random.Range(-4f, 4f);
        float y = Random.Range(-4f, 4f);
        return roomCenter + new Vector3(x, y, 0);
    }
}
```

---

## 🔮 FUTURE ENHANCEMENTS

1. **Multi-Floor Dungeons**: Stairs connecting multiple floors
2. **Procedural Boss Patterns**: AI-generated boss behaviors
3. **Narrative Generation**: Procedural story elements
4. **Music System**: Dynamic music based on danger level
5. **Weather System**: Procedural environmental effects
6. **NPC Placement**: Quest givers, merchants in specific rooms
7. **Puzzle Generation**: Procedural puzzle mechanics
8. **Lore System**: Randomly placed lore items telling dungeon story

---

## 📚 ADDITIONAL RESOURCES

- Unity NavMeshPlus: https://github.com/Unity-Technologies/NavMeshPlus
- Procedural Generation in Games: http://pcgbook.com/
- Dungeon Generation Algorithms: https://www.gridsagegames.com/blog/

---

Happy Advanced Dungeon Generating! 🚀🏰
