# 🏰 PROCEDURAL DUNGEON GENERATOR - Unity 2D

Hệ thống tạo dungeon tự động hoàn chỉnh cho Unity 2D với khả năng lưu trữ, backtracking, và tích hợp NavMesh.

---

## 📋 MỤC LỤC

1. [Tính năng](#tính-năng)
2. [Cấu trúc thư mục](#cấu-trúc-thư-mục)
3. [Cài đặt](#cài-đặt)
4. [Hướng dẫn sử dụng](#hướng-dẫn-sử-dụng)
5. [Cấu hình RoomData](#cấu-hình-roomdata)
6. [Cấu hình TrapData](#cấu-hình-trapdata)
7. [Editor Window](#editor-window)
8. [API Reference](#api-reference)
9. [Tips & Best Practices](#tips--best-practices)

---

## ✨ TÍNH NĂNG

### Core Features

- ✅ **Grid-based dungeon generation** với kiểm tra overlap tự động
- ✅ **Dungeon flow** tuân thủ: Start → Archetype1 → MidBoss → Archetype2 → Boss → Goal
- ✅ **Backtracking algorithm** khi gặp dead-end
- ✅ **Branch rooms** (phòng nhánh) để tăng tính khám phá
- ✅ **Seed system** cho reproducible generation
- ✅ **Door connection system** tự động
- ✅ **Trap spawning** với danger level scaling
- ✅ **Path validation** đảm bảo traps không chặn hoàn toàn

### Editor Tools

- 🎨 **Custom Editor Window** với UI trực quan
- 💾 **Save Map as Prefab** với tên duy nhất theo seed
- 🔍 **Debug Gizmos** hiển thị grid và connections
- ⚙️ **One-click generation** trong Editor mode

### Integration

- 🗺️ **NavMesh support** (NavMeshPlus cho 2D)
- 🎯 **Static batching** tự động cho optimization
- 🔊 **Audio zone setup** (extensible)
- 🗺️ **Minimap generation** (extensible)

---

## 📁 CẤU TRÚC THƯ MỤC

```
Assets/Scripts/ProceduralGeneration/
├── Data/
│   ├── RoomData.cs              # ScriptableObject cho rooms
│   └── TrapData.cs              # ScriptableObject cho traps
├── Core/
│   ├── Room.cs                  # Room instance class
│   ├── DungeonManager.cs        # Main generation logic
│   └── DungeonUtils.cs          # Helper utilities
├── Components/
│   ├── TrapSpawnPoint.cs        # Đánh dấu trap spawn
│   ├── EnemySpawnPoint.cs       # Đánh dấu enemy spawn
│   └── DoorController.cs        # Door behavior
├── Editor/
│   └── DungeonGeneratorWindow.cs # Editor Window
└── Integration/
    ├── NavMeshIntegration.cs    # NavMesh baking
    └── DungeonIntegrator.cs     # System integration
```

---

## 🚀 CÀI ĐẶT

### Bước 1: Import Scripts

Copy toàn bộ folder `ProceduralGeneration` vào thư mục `Assets/Scripts/` của project.

### Bước 2: Tạo Room Prefabs

#### 2.1. Cấu trúc một Room Prefab

```
RoomPrefab
├── Visuals (SpriteRenderer, Tilemap, etc.)
├── Colliders (walls, obstacles)
├── Doors
│   ├── Door_Top
│   ├── Door_Bottom
│   ├── Door_Left
│   └── Door_Right
├── Walls (thay thế doors khi không có kết nối)
│   ├── Wall_Top
│   ├── Wall_Bottom
│   ├── Wall_Left
│   └── Wall_Right
├── TrapSpawnPoints (Empty GameObjects)
│   ├── TrapSpawn_01
│   ├── TrapSpawn_02
│   └── TrapSpawn_03
└── EnemySpawnPoints (Empty GameObjects)
    ├── EnemySpawn_01
    └── EnemySpawn_02
```

#### 2.2. Thêm Components

- **TrapSpawnPoint.cs** vào các Empty GameObjects trong `TrapSpawnPoints`
- **EnemySpawnPoint.cs** vào các Empty GameObjects trong `EnemySpawnPoints`
- **DoorController.cs** vào các Door objects (optional, cho advanced behavior)

### Bước 3: Tạo RoomData Assets

1. Click chuột phải trong Project → `Create > Procedural Generation > Room Data`
2. Cấu hình các thông số:
   - **Room Prefab**: Kéo prefab vào đây
   - **Room Type**: Chọn loại phòng (Start, Archetype1, MidBoss, etc.)
   - **Size**: Kích thước trên grid (Vector2Int)
   - **Door Anchors**: Cấu hình vị trí các cửa

### Bước 4: Tạo TrapData Assets

1. Click chuột phải trong Project → `Create > Procedural Generation > Trap Data`
2. Cấu hình:
   - **Trap Prefab**: Kéo trap prefab vào
   - **Danger Score**: Mức độ nguy hiểm (1-10)
   - **Min Danger Level**: Level tối thiểu để spawn
   - **Spawn Logic**: Random, NearEntrance, RoomCenter, etc.

### Bước 5: Setup Scene

1. Tạo Empty GameObject tên `DungeonManager`
2. Add component `DungeonManager.cs`
3. Cấu hình trong Inspector:
   - **Archetype 1 Room Count**: 5 (số phòng giai đoạn 1)
   - **Archetype 2 Room Count**: 5 (số phòng giai đoạn 2)
   - **Branch Probability**: 0.2 (20% tạo phòng nhánh)
   - **Room Database**: Kéo tất cả RoomData assets vào list
   - **Trap Database**: Kéo tất cả TrapData assets vào list

4. (Optional) Add component `DungeonIntegrator.cs` và `NavMeshIntegration.cs` cho advanced features

---

## 📖 HƯỚNG DẪN SỬ DỤNG

### Cách 1: Sử dụng Editor Window (Khuyến nghị)

1. Mở Editor Window: `Tools > Procedural Generation > Dungeon Generator`
2. Assign hoặc tạo DungeonManager
3. Cấu hình seed (optional):
   - Check "Use Custom Seed" để dùng seed cố định
   - Hoặc để random mỗi lần generate
4. Click **"GENERATE DUNGEON"**
5. Xem kết quả trong Scene view
6. Click **"SAVE MAP AS PREFAB"** để lưu dungeon thành prefab

### Cách 2: Generate qua Script

```csharp
using ProceduralGeneration.Core;

public class GameManager : MonoBehaviour
{
    public DungeonManager dungeonManager;

    void Start()
    {
        // Generate với seed cố định
        dungeonManager.seed = 12345;
        dungeonManager.useRandomSeed = false;
        dungeonManager.GenerateDungeon();

        // Hoặc generate với random seed
        dungeonManager.useRandomSeed = true;
        dungeonManager.GenerateDungeon();

        // Lấy seed đã dùng
        int usedSeed = dungeonManager.GetCurrentSeed();
        Debug.Log($"Generated with seed: {usedSeed}");
    }
}
```

### Cách 3: Generate Runtime trong Game

```csharp
public class LevelGenerator : MonoBehaviour
{
    public DungeonManager dungeonManager;
    public int levelNumber = 1;

    public void GenerateNewLevel()
    {
        // Clear old dungeon
        dungeonManager.ClearDungeon();

        // Scale difficulty
        dungeonManager.archetype1RoomCount = 3 + levelNumber;
        dungeonManager.archetype2RoomCount = 3 + levelNumber;

        // Generate
        dungeonManager.GenerateDungeon();

        // Post-processing
        OnDungeonGenerated();
    }

    void OnDungeonGenerated()
    {
        // Spawn player
        // Setup camera
        // Enable gameplay
    }
}
```

---

## 🎨 CẤU HÌNH ROOMDATA

### Inspector Fields

| Field                     | Mô tả                 | Ví dụ                                    |
| ------------------------- | --------------------- | ---------------------------------------- |
| **Room Prefab**           | Prefab của phòng      | RoomPrefab_Basic                         |
| **Room Type**             | Loại phòng trong flow | Archetype1, Boss, etc.                   |
| **Size**                  | Kích thước grid (x,y) | (1,1) cho phòng nhỏ, (2,2) cho phòng lớn |
| **Door Anchors**          | List các cửa          | Top, Bottom, Left, Right                 |
| **Max Trap Spawn Points** | Số trap tối đa        | 3                                        |
| **Enemy Spawn Rate**      | Tỷ lệ spawn enemies   | 0.5 (50%)                                |

### Door Anchor Configuration

Mỗi Door Anchor cần:

- **Direction**: Top/Bottom/Left/Right
- **Local Position**: Vị trí cửa trong phòng (Vector3)
- **Door Object**: GameObject của cửa (sẽ enable khi có kết nối)
- **Wall Object**: GameObject của tường (sẽ enable khi không có kết nối)

**Ví dụ:**

```
Direction: Top
Local Position: (0, 5, 0)  // 5 units phía trên center
Door Object: Door_Top
Wall Object: Wall_Top
```

---

## 🔧 CẤU HÌNH TRAPDATA

### Inspector Fields

| Field                      | Mô tả                         | Giá trị Đề xuất                    |
| -------------------------- | ----------------------------- | ---------------------------------- |
| **Trap Prefab**            | Prefab của bẫy                | SpikeTrap, FireTrap, etc.          |
| **Danger Score**           | Độ nguy hiểm (1-10)           | Easy: 1-3, Medium: 4-7, Hard: 8-10 |
| **Min Danger Level**       | Level tối thiểu để spawn      | 0 cho early game, 5+ cho late game |
| **Spawn Probability**      | Xác suất spawn (0-1)          | 0.7 = 70%                          |
| **Spawn Logic**            | Logic đặt bẫy                 | Random, NearEntrance, RoomCenter   |
| **Max Per Room**           | Số lượng tối đa trong 1 phòng | 2-3                                |
| **Min Distance From Door** | Khoảng cách tối thiểu từ cửa  | 2.0 units                          |
| **Can Block Main Path**    | Có thể chặn đường đi chính?   | false (đảm bảo có thể đi qua)      |

### Spawn Logic Types

- **Random**: Spawn ngẫu nhiên ở bất kỳ spawn point nào
- **NearEntrance**: Ưu tiên spawn gần cửa vào
- **NearExit**: Ưu tiên spawn gần cửa ra
- **PathBlocking**: Cố gắng chặn đường đi (nếu canBlockMainPath = true)
- **RoomCenter**: Ưu tiên spawn ở giữa phòng
- **Corners**: Ưu tiên spawn ở các góc
- **AlongWalls**: Spawn dọc theo tường

---

## 🖥️ EDITOR WINDOW

### Các Chức Năng

#### 1. Dungeon Manager Section

- **Find in Scene**: Tìm DungeonManager trong scene hiện tại
- **Create New Manager**: Tạo GameObject mới với DungeonManager component
- **Select Manager**: Focus vào Manager trong Hierarchy

#### 2. Seed Configuration

- **Use Custom Seed**: Bật/tắt seed cố định
- **Seed Value**: Nhập seed (số nguyên)
- **Random Seed**: Generate seed ngẫu nhiên mới
- **Copy Current**: Copy seed đang dùng

#### 3. Generation

- **GENERATE DUNGEON**: Tạo dungeon mới
- **CLEAR DUNGEON**: Xóa dungeon hiện tại

#### 4. Save & Export

- **Save Folder**: Chọn thư mục lưu prefabs
- **Browse**: Chọn folder qua file dialog
- **Create Folder**: Tạo folder nếu chưa tồn tại
- **SAVE MAP AS PREFAB**: Lưu dungeon thành prefab với tên: `DungeonMap_Seed_{seed}_{timestamp}.prefab`

#### 5. Information

- Hiển thị current seed
- Số lượng rooms
- Dungeon flow structure

---

## 📚 API REFERENCE

### DungeonManager

#### Public Methods

```csharp
// Generate dungeon mới
public void GenerateDungeon()

// Clear dungeon hiện tại
public void ClearDungeon()

// Lấy seed đã sử dụng
public int GetCurrentSeed()
```

#### Public Fields

```csharp
// Generation settings
public int seed;
public bool useRandomSeed;
public int archetype1RoomCount;
public int archetype2RoomCount;
public float branchProbability;

// Data
public List<RoomData> roomDatabase;
public List<TrapData> trapDatabase;
public bool spawnTraps;

// Danger scaling
public AnimationCurve dangerCurve;

// References
public Transform dungeonContainer;

// Debug
public bool showDebugGizmos;
public bool verboseLogging;
```

### DungeonUtils

#### Static Methods

```csharp
// Chuyển đổi direction
public static Vector2Int DirectionToVector(DoorDirection direction)
public static DoorDirection VectorToDirection(Vector2Int vector)
public static DoorDirection GetOppositeDirection(DoorDirection direction)

// Grid operations
public static bool IsGridCellFree(Vector2Int position, Dictionary<Vector2Int, Room> occupiedCells)
public static bool CanPlaceRoomAt(Vector2Int position, RoomData roomData, Dictionary<Vector2Int, Room> occupiedCells)

// Pathfinding
public static List<Room> FindPath(Room start, Room end, List<Room> allRooms)
public static int ManhattanDistance(Vector2Int a, Vector2Int b)

// Utilities
public static void Shuffle<T>(List<T> list)
public static bool HasEnoughSpaceForTrap(Vector3 position, float radius, List<Vector3> existingTraps, float minDistance)
```

### Room

#### Properties

```csharp
public RoomData roomData;
public Vector2Int gridPosition;
public GameObject roomInstance;
public Dictionary<DoorDirection, Room> connectedRooms;
public List<DoorInstance> doors;
public int distanceFromStart;
public int dangerLevel;
public bool isMainPath;
```

#### Methods

```csharp
public bool HasDoorInDirection(DoorDirection direction)
public List<Vector2Int> GetOccupiedGridCells()
public bool CanConnectTo(Room otherRoom)
public DoorDirection? GetDirectionTo(Room otherRoom)
public void InstantiateRoom(Transform parent)
```

---

## 💡 TIPS & BEST PRACTICES

### 1. Room Design

✅ **DO:**

- Tạo rooms với kích thước nhất quán (1x1, 2x2, etc.)
- Đặt doors ở vị trí chuẩn (center của mỗi cạnh)
- Tạo nhiều variants cho mỗi RoomType
- Sử dụng naming convention: `Room_{Type}_{Variant}`
- Test từng room prefab riêng lẻ trước

❌ **DON'T:**

- Đặt doors ở vị trí lệch lạc
- Quên thêm walls để thay thế doors
- Tạo rooms quá lớn (>3x3) nếu không cần thiết
- Quên thêm spawn points

### 2. Trap Placement

✅ **DO:**

- Đặt nhiều TrapSpawnPoints hơn maxTrapSpawnPoints
- Sử dụng danger score phù hợp với game progression
- Test path validation (đảm bảo player có thể đi qua)
- Combine nhiều trap types trong cùng dungeon

❌ **DON'T:**

- Đặt canBlockMainPath = true nếu không có alternate path
- Đặt minDistanceFromDoor quá nhỏ (<1.5)
- Spawn too many traps trong early rooms

### 3. Performance

✅ **DO:**

- Sử dụng object pooling cho runtime generation
- Enable static batching cho walls và floors
- Bake lighting và NavMesh sau khi generate
- Sử dụng occlusion culling cho dungeons lớn

❌ **DON'T:**

- Generate quá nhiều rooms (>30) trong một lần
- Quên cleanup old dungeons trước khi generate mới
- Instantiate enemies ngay khi generate (lazy spawn instead)

### 4. Seed Management

✅ **DO:**

- Lưu seed của mỗi level trong save file
- Log seed ra console để reproduce bugs
- Offer "daily dungeon" với seed dựa trên date
- Allow players share seeds với nhau

### 5. Debugging

✅ **DO:**

- Enable `showDebugGizmos` để visualize grid
- Enable `verboseLogging` khi gặp vấn đề
- Test với cùng seed nhiều lần
- Kiểm tra door connections trong Scene view

❌ **DON'T:**

- Disable gizmos khi đang debug generation issues
- Ignore console warnings về missing prefabs

---

## 🎯 DUNGEON FLOW

Hệ thống tuân thủ flow cố định:

```
START ROOM (spawn point)
    ↓
[N] ARCHETYPE 1 ROOMS (easy difficulty)
    ↓
MID-BOSS ROOM (checkpoint)
    ↓
[M] ARCHETYPE 2 ROOMS (harder difficulty)
    ↓
BOSS ROOM (final challenge)
    ↓
GOAL ROOM (exit/treasure)
```

### Danger Level Scaling

- **Start**: Danger Level = 0
- **Archetype 1**: DL tăng từ 1-3
- **MidBoss**: DL = 5
- **Archetype 2**: DL tăng từ 6-8
- **Boss**: DL = 10

Danger Level được dùng để:

- Chọn trap types phù hợp (minDangerLevel)
- Scale enemy stats
- Adjust reward rarity

---

## 🐛 TROUBLESHOOTING

### Vấn đề: "Failed to generate dungeon flow"

**Nguyên nhân:**

- Không đủ RoomData cho các RoomTypes
- Room sizes không tương thích
- Door configurations không match

**Giải pháp:**

- Kiểm tra Room Database có đủ mỗi loại: Start, Archetype1, Archetype2, MidBoss, Boss, Goal
- Đảm bảo mỗi room có ít nhất 2 doors (để connect)
- Giảm `archetype1RoomCount` và `archetype2RoomCount`

### Vấn đề: Rooms bị overlap

**Nguyên nhân:**

- Room size không được set đúng
- Bug trong overlap checking

**Giải pháp:**

- Verify `roomData.size` matches actual prefab size
- Enable gizmos để visualize grid cells

### Vấn đề: Doors không kết nối

**Nguyên nhân:**

- Door anchors không được setup đúng
- Door objects bị null

**Giải pháp:**

- Kiểm tra mỗi DoorAnchor có doorObject và wallObject assigned
- Verify local positions của doors

### Vấn đề: Traps spawn chặn hoàn toàn

**Nguyên nhân:**

- `minDistanceFromDoor` quá nhỏ
- Path validation không hoạt động

**Giải pháp:**

- Tăng `minDistanceFromDoor` lên ≥ 2.0
- Set `canBlockMainPath = false` cho important traps
- Thêm nhiều spawn points hơn

---

## 📝 CHANGELOG

### Version 1.0.0 (Initial Release)

- ✅ Core dungeon generation system
- ✅ Grid-based placement với overlap checking
- ✅ Backtracking algorithm
- ✅ Door connection system
- ✅ Trap spawning với validation
- ✅ Seed system
- ✅ Editor Window
- ✅ Save as Prefab functionality
- ✅ NavMesh integration
- ✅ Debug gizmos

---

## 📄 LICENSE

MIT License - Feel free to use in your projects!

---

## 🙏 CREDITS

Developed by: [Your Name]
For: Unity 2D Procedural Generation

---

## 📞 SUPPORT

Nếu gặp vấn đề hoặc có câu hỏi, hãy:

1. Kiểm tra Troubleshooting section
2. Enable verbose logging và check console
3. Verify tất cả prefabs và ScriptableObjects đã setup đúng

Happy dungeon generating! 🎮🏰
