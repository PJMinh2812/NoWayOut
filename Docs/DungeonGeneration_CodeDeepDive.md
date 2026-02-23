# 🔬 Dungeon Generation — Giải thích Code từng dòng

> Tài liệu này đi sâu vào từng file, giải thích **tại sao** viết như vậy, **cơ chế** hoạt động, và **mối liên hệ** giữa các file.

---

## 📂 FILE 1: `DungeonTypes.cs` — Nền tảng dữ liệu

```
📁 Assets/Settings/Dungeon/Core/DungeonTypes.cs (39 dòng)
```

### Enum DungeonCell — Mỗi ô trên bản đồ là gì?

```csharp
public enum DungeonCell
{
    Wall,       // = 0: Tường, player không đi qua được
    Room,       // = 1: Sàn phòng, player đi được
    Tunnel,     // = 2: Đường hầm nối 2 phòng
    Furniture,  // = 3: Nội thất (chưa dùng)
    Start,      // = 4: Điểm bắt đầu của player
    Finish      // = 5: Điểm kết thúc (thoát dungeon)
}
```

**Tại sao dùng Enum?**
- Mỗi ô chỉ chiếm **4 bytes** (int) thay vì lưu string
- So sánh nhanh (`== DungeonCell.Wall`) thay vì so sánh chuỗi
- TypeSafe: compiler báo lỗi nếu gõ sai

### Class DungeonMap — Bản đồ 2D lưu dạng 1D

```csharp
public sealed class DungeonMap
{
    public int Columns;      // Số cột (chiều ngang)
    public int Rows;         // Số hàng (chiều dọc)
    public DungeonCell[] Cells;  // ★ Mảng 1 chiều chứa toàn bộ map
    public Vector2Int Start;     // Tọa độ điểm bắt đầu
    public Vector2Int Finish;    // Tọa độ điểm kết thúc
```

**Tại sao mảng 1D thay vì 2D (`DungeonCell[,]`)?**

Hãy tưởng tượng map 4×3:
```
Mảng 2D (4 cột × 3 hàng):        Mảng 1D tương đương:
[0,0] [1,0] [2,0] [3,0]           [0] [1] [2] [3] [4] [5] [6] [7] [8] [9] [10] [11]
[0,1] [1,1] [2,1] [3,1]     →     hàng 0: [0..3]  hàng 1: [4..7]  hàng 2: [8..11]
[0,2] [1,2] [2,2] [3,2]
```

Công thức chuyển đổi: **`index = row × Columns + column`**
- Ô (2, 1) → `1 × 4 + 2 = 6` → `Cells[6]`
- Ô (3, 2) → `2 × 4 + 3 = 11` → `Cells[11]`

**Lợi ích mảng 1D**: Nằm liên tục trong RAM → CPU cache đọc nhanh hơn 30-50% so với mảng 2D.

```csharp
    // Helper methods — đọc/ghi ô bằng tọa độ (column, row)
    public DungeonCell Get(int c, int r) => Cells[r * Columns + c];
    public void Set(int c, int r, DungeonCell v) => Cells[r * Columns + c] = v;

    // Kiểm tra tọa độ có nằm trong map không
    public bool InBounds(int c, int r) => c >= 0 && r >= 0 && c < Columns && r < Rows;

    // Kiểm tra ô có phải "không gian đi được" không
    public bool IsSpace(int c, int r)
    {
        if (!InBounds(c, r)) return false;
        var v = Get(c, r);
        // Room, Tunnel, Start, Finish đều là không gian đi được
        return v is DungeonCell.Room or DungeonCell.Tunnel 
                  or DungeonCell.Start or DungeonCell.Finish;
    }
}
```

---

## 📂 FILE 2: `DungeonGenerator2D.cs` — Thuật toán sinh bản đồ

```
📁 Assets/Settings/Dungeon/Generation/DungeonGenerator2D.cs (213 dòng)
```

### Tổng quan thuật toán

```
Bước 1: Tạo lưới toàn TƯỜNG
Bước 2: Đặt phòng ngẫu nhiên (lặp cho đến khi hết density)
Bước 3: Nối phòng bằng đường hầm hình chữ L
Bước 4: Thêm viền (border) bao quanh
Bước 5: Chọn phòng Start & Finish
```

### Bước 1: Khởi tạo lưới toàn tường

```csharp
public static Result Generate(int columns, int rows, 
    int minimumRoomSize, int maximumRoomSize, int density, int? seed = null)
{
    // Kiểm tra input hợp lệ
    if (columns < 8 || rows < 8) 
        throw new ArgumentOutOfRangeException(nameof(columns), "Dungeon too small.");
    if (minimumRoomSize < 2) 
        throw new ArgumentOutOfRangeException(nameof(minimumRoomSize));
    if (maximumRoomSize <= minimumRoomSize) 
        throw new ArgumentOutOfRangeException(nameof(maximumRoomSize));
```

**`int? seed = null`** — Dấu `?` nghĩa seed là **nullable**. Nếu không truyền seed → dùng random. Nếu truyền seed → dungeon giống nhau mỗi lần (cho testing).

```csharp
    // Tạo Random Number Generator
    // seed.HasValue = true nếu caller truyền seed
    var rng = seed.HasValue 
        ? new System.Random(seed.Value)   // Deterministic: cùng seed → cùng map
        : new System.Random();             // Random thật

    // Map nhỏ hơn input 2 đơn vị mỗi chiều (vì border sẽ thêm lại sau)
    var innerCols = columns - 2;
    var innerRows = rows - 2;

    // Tạo map rỗng, fill toàn bộ = Wall
    var map = new DungeonMap {
        Columns = innerCols,
        Rows = innerRows,
        Cells = new DungeonCell[innerCols * innerRows]  // VD: 28×28 = 784 ô
    };
    Fill(map, DungeonCell.Wall);  // Tất cả 784 ô = Wall
```

**Tại sao trừ 2?** Vì cuối cùng sẽ thêm viền (border) 1 ô mỗi bên → kích thước cuối cùng = input.

### Bước 2: Đặt phòng ngẫu nhiên

```csharp
    var rooms = new List<Room>();

    while (true)  // Lặp vô hạn cho đến khi hết chỗ hoặc hết attempts
    {
        // Tạo phòng với kích thước ngẫu nhiên
        var newRoom = new Room
        {
            // rng.Next(max - min) + min → random trong khoảng [min, max)
            Width  = rng.Next(maximumRoomSize - minimumRoomSize) + minimumRoomSize,
            Height = rng.Next(maximumRoomSize - minimumRoomSize) + minimumRoomSize,
            Column = 0,  // Sẽ random vị trí ở dưới
            Row    = 0
        };

        var attempts = 0;
        while (true)  // Thử đặt phòng tại vị trí ngẫu nhiên
        {
            // Random vị trí: đảm bảo phòng không vượt ra ngoài map
            //   innerCols - newRoom.Width = vị trí tối đa (ở bên phải)
            newRoom.Column = rng.Next(innerCols - newRoom.Width);
            newRoom.Row    = rng.Next(innerRows - newRoom.Height);

            if (CanPlaceRoom(map, newRoom)) break;  // Đặt được → thoát
            if (attempts > density) break;            // Hết lượt → dừng
            attempts++;
        }

        if (attempts > density) break;  // Hết chỗ đặt → kết thúc tạo phòng

        rooms.Add(newRoom);
        AddRoom(map, newRoom);  // Đánh dấu ô = Room trên map

        // Nối phòng mới với phòng trước đó bằng tunnel
        if (rooms.Count > 1)
        {
            var prev = rooms[rooms.Count - 2];  // Phòng trước
            CreateTunnel(map, rng, newRoom, prev);
        }
    }
```

**`density = 30` nghĩa gì?** Thuật toán sẽ thử đặt phòng tối đa 30 lần tại vị trí ngẫu nhiên. Nếu sau 30 lần vẫn không đặt được (do map đầy) → dừng lại. `density` cao → nhiều phòng hơn.

### Kiểm tra có đặt phòng được không

```csharp
private static bool CanPlaceRoom(DungeonMap map, Room room)
{
    // Kiểm tra vùng LỚN HƠN phòng 1 ô mỗi chiều
    //   room.Row - 1     → 1 ô trên phòng
    //   room.Row + Height + 1  → 1 ô dưới phòng
    //   Tương tự cho Column
    for (var r = room.Row - 1; r <= room.Row + room.Height + 1; r++)
    for (var c = room.Column - 1; c <= room.Column + room.Width + 1; c++)
    {
        if (!map.InBounds(c, r)) continue;  // Ngoài map thì bỏ qua
        if (map.Get(c, r) != DungeonCell.Wall) return false;  // Có gì đó → KHÔNG đặt được
    }
    return true;  // Toàn bộ vùng đều là tường → OK
}
```

**Hình ảnh minh họa** — phòng 3×2 tại (2,1):
```
Vùng kiểm tra (padding ±1):
. . . . . .      . = phải là Wall
. P P P . .      P = phòng sẽ đặt
. P P P . .
. . . . . .
```
→ Padding đảm bảo phòng cách nhau ít nhất 1 ô tường.

### Bước 3: Tạo tunnel hình chữ L

```csharp
private static void CreateTunnel(DungeonMap map, System.Random rng, Room a, Room b)
{
    // 50% xác suất: đi ngang trước, rồi dọc
    // 50% xác suất: đi dọc trước, rồi ngang
    if (rng.NextDouble() < 0.5)
    {
        // Lấy 1 điểm ngẫu nhiên trong phòng a
        var startColumn = rng.Next(a.Column, a.Column + a.Width);
        var startRow    = rng.Next(a.Row, a.Row + a.Height);
        // Lấy 1 cột ngẫu nhiên trong phòng b
        var endColumn   = rng.Next(b.Column, b.Column + b.Width);

        // ① Đi ngang từ startColumn → endColumn (giữ nguyên startRow)
        CreateTunnelHorizontally(map, startColumn, endColumn, startRow);

        // Lấy 1 hàng ngẫu nhiên trong phòng b
        var middleRow = rng.Next(b.Row, b.Row + b.Height);
        // ② Đi dọc từ startRow → middleRow (giữ nguyên endColumn)
        CreateTunnelVertically(map, startRow, middleRow, endColumn);
    }
    // ... tương tự cho trường hợp dọc trước
}
```

**Hình ảnh tunnel chữ L**:
```
Phòng A          Phòng B
┌────┐           ┌────┐
│    │           │    │
│  ●─┼───────────┤    │    ← ① Đi ngang
│    │           │    │
└────┘           │ ●  │    ← ② Đi dọc
                 │ │  │
                 └─┘──┘
```

### Bước 4: Thêm border

```csharp
private static DungeonMap AddBorder(DungeonMap inner)
{
    // Tạo map mới lớn hơn 2 mỗi chiều
    var outer = new DungeonMap {
        Columns = inner.Columns + 2,
        Rows = inner.Rows + 2,
        Cells = new DungeonCell[(inner.Columns + 2) * (inner.Rows + 2)]
    };
    Fill(outer, DungeonCell.Wall);  // Toàn bộ = Wall

    // Copy nội dung cũ vào giữa (offset +1 mỗi chiều)
    for (var r = 0; r < inner.Rows; r++)
    for (var c = 0; c < inner.Columns; c++)
        outer.Set(c + 1, r + 1, inner.Get(c, r));

    return outer;
}
```

```
TRƯỚC (28×28):         SAU (30×30):
┌──────────┐           ████████████████
│ Room     │           █              █
│    Tunnel│     →     █  Room        █
│          │           █     Tunnel   █
└──────────┘           ████████████████
                       ↑ Border tường bao quanh
```

### Bước 5: Chọn Start & Finish

```csharp
Room startRoom, finishRoom;
do {
    startRoom  = rooms[rng.Next(rooms.Count)];  // Random 1 phòng
    finishRoom = rooms[rng.Next(rooms.Count)];  // Random 1 phòng khác
} while (ReferenceEquals(startRoom, finishRoom)); // Lặp nếu trùng

// Random 1 điểm BÊN TRONG phòng Start
var start = new Vector2Int(
    rng.Next(startRoom.Column, startRoom.Column + startRoom.Width + 1),
    rng.Next(startRoom.Row, startRoom.Row + startRoom.Height + 1)
);
// Clamp để không vượt ra ngoài map
start.x = Mathf.Clamp(start.x, 0, map.Columns - 1);
```

---

## 📂 FILE 3: `RoomData.cs` — Bản thiết kế phòng

```
📁 Assets/Scripts/ProceduralGeneration/Data/RoomData.cs (85 dòng)
```

```csharp
// ScriptableObject = asset file trong Unity, có thể tạo nhiều instances
// từ menu: Assets > Create > Procedural Generation > Room Data
[CreateAssetMenu(fileName = "New Room", menuName = "Procedural Generation/Room Data")]
public class RoomData : ScriptableObject
{
    public GameObject roomPrefab;   // Prefab của phòng (có thể null → auto-gen)
    public RoomType roomType;       // Start, Archetype1, Boss, Goal...
    public Vector2Int size = Vector2Int.one;  // Kích thước lưới (VD: 15×15)

    // Danh sách cửa — mỗi cửa có hướng và vị trí
    public List<DoorAnchor> doorAnchors = new List<DoorAnchor>();

    public int maxTrapSpawnPoints = 3;      // Số bẫy tối đa
    [Range(0f, 1f)] public float enemySpawnRate = 0.5f;  // 50% spawn enemy
}
```

### Enum RoomType — Các loại phòng

```csharp
public enum RoomType
{
    Start,       // Phòng bắt đầu — player spawn ở đây
    Archetype1,  // Phòng thường giai đoạn 1 (dễ)
    Archetype2,  // Phòng thường giai đoạn 2 (khó hơn)
    MidBoss,     // Phòng mini-boss (giữa dungeon)
    Boss,        // Phòng boss cuối
    Goal,        // Phòng thoát/chiến thắng
    Treasure,    // Phòng thưởng (optional)
    Secret       // Phòng bí mật (optional)
}
```

### DoorAnchor — Thông tin 1 cửa

```csharp
[System.Serializable]  // Hiển thị trong Inspector
public class DoorAnchor
{
    public DoorDirection direction;    // Top/Bottom/Left/Right
    public Vector3 localPosition;      // Vị trí cửa (offset từ center)
    public GameObject doorObject;      // GO hiển thị cửa
    public GameObject wallObject;      // GO tường thay thế (khi không có phòng kế bên)
}

public enum DoorDirection
{
    Top, Bottom, Left, Right
}
```

**Ví dụ**: Phòng 15×15 có 4 cửa:
```
         Door(Top, y=+7)
    ┌────────┤├────────┐
    │                  │
Door│                  │Door
(Left)                (Right)
    │                  │
    └────────┤├────────┘
         Door(Bottom, y=-7)
```

---

## 📂 FILE 4: `DungeonManager.cs` — Bộ não chính (1046 dòng)

```
📁 Assets/Scripts/ProceduralGeneration/Core/DungeonManager.cs
```

### Các biến quan trọng

```csharp
public class DungeonManager : MonoBehaviour
{
    // === CẤU HÌNH TRÊN INSPECTOR ===
    public int seed = 0;                    // Seed cố định (0 = random)
    public bool useRandomSeed = true;       // Bật random seed?
    public float worldScale = 1f;           // 1 grid cell = bao nhiêu world units
    public int archetype1RoomCount = 5;     // Số phòng giai đoạn 1
    public int archetype2RoomCount = 5;     // Số phòng giai đoạn 2
    public float branchProbability = 0.2f;  // 20% tạo phòng nhánh
    public List<RoomData> roomDatabase;     // Danh sách bản thiết kế phòng

    // === DỮ LIỆU RUNTIME (không hiện Inspector) ===
    private Dictionary<Vector2Int, Room> occupiedCells;  // ô nào đã bị chiếm?
    private List<Room> allRooms;      // Tất cả phòng đã tạo
    private List<Room> mainPath;      // Chỉ phòng trên đường chính
    private Room startRoom;           // Phòng bắt đầu
    private Room goalRoom;            // Phòng kết thúc
```

**`occupiedCells`** — Dictionary mapping Vector2Int → Room. Khi đặt phòng 15×15 tại (0,0), nó đánh dấu **225 ô** (15×15) vào dictionary này:
```
(0,0)→RoomA, (1,0)→RoomA, (2,0)→RoomA, ... (14,0)→RoomA,
(0,1)→RoomA, (1,1)→RoomA, ... (14,14)→RoomA
```

### GenerateDungeon — Hàm chính

```csharp
public void GenerateDungeon()
{
    // ① SETUP SEED
    if (useRandomSeed || seed == 0)
        currentSeed = System.DateTime.Now.Millisecond + Random.Range(0, 10000);
    else
        currentSeed = seed;

    Random.InitState(currentSeed);
    // Sau dòng này, mọi Random.Range() sẽ cho kết quả CỐ ĐỊNH với seed này
    // → Cùng seed = cùng dungeon layout
```

**Random.InitState(seed)** — Đây là core của "seed-based generation". Random trong Unity dùng thuật toán Xorshift128. InitState reset trạng thái → cùng chuỗi số ngẫu nhiên.

```csharp
    // ② XÓA DUNGEON CŨ
    ClearDungeon();

    // ③ KHỞI TẠO CẤU TRÚC DỮ LIỆU
    Initialize();
    // → occupiedCells = new Dictionary
    // → allRooms = new List
    // → mainPath = new List
    // → Tạo DungeonContainer nếu chưa có

    // ④ TẠO DUNGEON THEO FLOW
    if (!GenerateDungeonFlow()) {
        Debug.LogError("Failed!"); return;
    }

    // ⑤ INSTANTIATE PHÒNG TRONG SCENE
    InstantiateAllRooms();

    // ⑥ ĐẢM BẢO CÓ TRANSITION MANAGER
    EnsureTransitionManager();

    isGenerated = true;
}
```

### GenerateDungeonFlow — Tạo chuỗi phòng

```csharp
private bool GenerateDungeonFlow()
{
    // Start tại (0,0) — không thể fail
    if (!CreateStartRoom()) return false;

    // 5 phòng Archetype1 — mỗi phòng nối tiếp phòng trước
    // Trong mỗi phòng có 20% tạo branch
    if (!CreateRoomSequence(RoomType.Archetype1, archetype1RoomCount)) return false;

    // 1 phòng MidBoss
    if (!CreateRoomOfType(RoomType.MidBoss)) return false;

    // 5 phòng Archetype2
    if (!CreateRoomSequence(RoomType.Archetype2, archetype2RoomCount)) return false;

    // 1 phòng Boss
    if (!CreateRoomOfType(RoomType.Boss)) return false;

    // 1 phòng Goal (kết thúc)
    if (!CreateGoalRoom()) return false;

    // Tính danger level dựa theo khoảng cách từ Start
    CalculateDangerLevels();
    // Phòng xa → danger cao → enemy mạnh hơn
    return true;
}
```

### TryPlaceRoom — Thuật toán đặt phòng (phức tạp nhất)

```csharp
private Room TryPlaceRoom(RoomData roomData, Room fromRoom)
{
    // ① Tìm hướng còn trống từ phòng hiện tại
    List<DoorDirection> availableDirections = GetAvailableDirections(fromRoom);
    // VD: fromRoom đã có phòng ở Right → trả về [Top, Bottom, Left]

    // ② Xáo trộn ngẫu nhiên để không luôn đi theo 1 hướng
    DungeonUtils.Shuffle(availableDirections);
    // VD: [Top, Bottom, Left] → [Left, Top, Bottom]

    // ③ Tính size phòng mới (đảm bảo lẻ)
    Vector2Int adjustedSize = CalculateAdjustedRoomSize(roomData);

    // ④ Thử từng hướng
    foreach (var direction in availableDirections)
    {
        // Chuyển hướng thành vector: Right → (1,0), Top → (0,1)
        Vector2Int offset = DungeonUtils.DirectionToVector(direction);

        // QUAN TRỌNG: Offset phải × kích thước phòng hiện tại
        // Nếu phòng hiện tại 15×15, đi Right → newPos = pos + (15, 0)
        Vector2Int scaledOffset = new Vector2Int(
            offset.x * fromRoom.actualSize.x,
            offset.y * fromRoom.actualSize.y
        );
        Vector2Int newPosition = fromRoom.gridPosition + scaledOffset;
```

**Minh họa scaledOffset**:
```
fromRoom (15×15) tại (0,0)     direction = Right
                                offset = (1, 0)
┌───────────────┐               scaledOffset = (15, 0)
│ fromRoom      │               newPosition = (0,0) + (15,0) = (15, 0)
│ (0,0)→(14,14) │
└───────────────┘ ← → ┌───────────────┐
                       │ newRoom        │
                       │ (15,0)→(29,14) │
                       └───────────────┘
```

```csharp
        // Kiểm tra tất cả ô phòng mới có trống không
        if (DungeonUtils.CanPlaceRoomAt(newPosition, adjustedSize, occupiedCells))
        {
            // Kiểm tra door compatibility
            // Nếu đi Right → phòng mới cần có cửa Left (đối diện)
            DoorDirection requiredDoor = DungeonUtils.GetOppositeDirection(direction);

            if (roomData.doorAnchors.Any(anchor => anchor.direction == requiredDoor))
            {
                // ✅ Thành công! Tạo phòng mới
                Room newRoom = new Room(roomData, newPosition, adjustedSize);
                newRoom.distanceFromStart = fromRoom.distanceFromStart + 1;

                AddRoomToGrid(newRoom); // Đánh dấu 225 ô vào occupiedCells

                // Kết nối 2 chiều
                fromRoom.connectedRooms[direction] = newRoom;     // A → B (Right)
                newRoom.connectedRooms[requiredDoor] = fromRoom;  // B → A (Left)

                return newRoom;
            }
        }
    }

    // ⑤ TẤT CẢ HƯỚNG ĐỀU FAIL → BACKTRACK
    // Quay lại thử phòng cũ hơn trên mainPath
    for (int i = mainPath.Count - 2; 
         i >= Mathf.Max(0, mainPath.Count - maxBacktrackAttempts); i--)
    {
        Room backtrackRoom = mainPath[i];
        Room result = TryPlaceRoom(roomData, backtrackRoom); // ĐỆ QUY!
        if (result != null) return result;
    }

    return null; // Thất bại hoàn toàn
}
```

**Khi nào cần Backtrack?**
```
Phòng bị "bao vây":
     ┌─────┐
     │ R3  │
┌────┤     ├────┐
│ R2 │     │ R4 │
└────┤     ├────┘
     │     │
     └──┬──┘
        │ chỉ còn 1 hướng (Down) nhưng...
     ┌──┴──┐
     │ R1  │  ← Start
     └─────┘

Nếu TryPlaceRoom từ R4 thất bại → backtrack về R3 → R2 → R1
```

### InstantiateAllRooms — Hiện phòng trong Scene

```csharp
private void InstantiateAllRooms()
{
    Room startRoom = null;

    foreach (var room in allRooms)
    {
        // ① Tạo empty GameObject với tên: "Room_Start_0_0"
        room.InstantiateRoom(dungeonContainer, worldScale);

        // ② Truyền tile references xuống RoomVisualGenerator
        room.ConfigureVisualGenerator(autoFillTiles, floorTiles,
            wallCenter, wallTopLeft, wallTopRight, ...);

        // ③ Generate Floor Tilemap + Wall Tilemap + Doors
        room.GenerateVisuals();

        if (room.roomData.roomType == RoomType.Start)
            startRoom = room;
    }

    // ④ CHỈ BẬT START ROOM — tất cả rooms khác ẩn
    foreach (var room in allRooms)
    {
        room.roomInstance.SetActive(room == startRoom);
        // → Performance: chỉ render + physics 1 phòng
    }

    // ⑤ Camera background = đen (vùng ngoài room)
    Camera.main.backgroundColor = Color.black;
}
```

---

## 📂 FILE 5: `Room.cs` — Instance của mỗi phòng

```
📁 Assets/Scripts/ProceduralGeneration/Core/Room.cs (199 dòng)
```

```csharp
public class Room  // KHÔNG phải MonoBehaviour!
{
    public RoomData roomData;           // Bản thiết kế (ScriptableObject)
    public Vector2Int gridPosition;     // Vị trí trên grid (VD: 0,0 hoặc 15,0)
    public Vector2Int actualSize;       // Size thực tế sau adjust (VD: 15×15)
    public GameObject roomInstance;     // GameObject trong scene
    public float worldScale = 1f;       // Scale factor

    // KẾT NỐI VỚI PHÒNG KHÁC
    // Key = hướng, Value = phòng kết nối
    // VD: { Right: Room_Arch1_15_0, Top: Room_Arch1_0_15 }
    public Dictionary<DoorDirection, Room> connectedRooms;

    public int distanceFromStart;  // Dùng tính danger level
    public int dangerLevel;        // Enemy strength scale
    public bool isMainPath;        // true = đường chính, false = nhánh
```

**Tại sao Room là C# class thay vì MonoBehaviour?**

| C# Class | MonoBehaviour |
|-----------|---------------|
| ✅ Linh hoạt: tạo/xóa bất cứ lúc nào | ❌ Phải AttachTo GameObject |
| ✅ Dictionary, List dễ dàng | ❌ Serialize phức tạp |
| ❌ Không tự serialize khi reload | ✅ Unity tự serialize |
| ❌ Phải rebuild khi Play Mode | ✅ Persist tự động |

→ Chọn C# class vì cần `Dictionary<DoorDirection, Room>` — Unity không serialize Dictionary.

```csharp
    // Tính tất cả ô grid mà phòng chiếm
    public List<Vector2Int> GetOccupiedGridCells()
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        // Phòng 3×2 tại (10, 5):
        // → (10,5), (11,5), (12,5), (10,6), (11,6), (12,6)
        for (int x = 0; x < actualSize.x; x++)
            for (int y = 0; y < actualSize.y; y++)
                cells.Add(gridPosition + new Vector2Int(x, y));
        return cells;
    }
```

```csharp
    // Tạo GameObject trong scene
    public void InstantiateRoom(Transform parent, float worldScale)
    {
        this.worldScale = worldScale;

        // gridPosition đã ở hệ grid → nhân worldScale cho world units
        // VD: gridPosition (15, 0), worldScale 1 → worldPos (15, 0, 0)
        Vector3 worldPosition = new Vector3(
            gridPosition.x * worldScale,
            gridPosition.y * worldScale, 0);

        // Tạo empty GO (KHÔNG dùng Instantiate prefab)
        roomInstance = new GameObject($"Room_{roomData.roomType}_{gridPosition.x}_{gridPosition.y}");
        roomInstance.transform.SetParent(parent);
        roomInstance.transform.position = worldPosition;

        // Add RoomVisualGenerator — component sẽ tạo visual SAU
        var visualGen = roomInstance.AddComponent<RoomVisualGenerator>();
    }
```

---

## 📂 FILE 6: `RoomVisualGenerator.cs` — Vẽ phòng

```
📁 Assets/Scripts/ProceduralGeneration/Components/RoomVisualGenerator.cs (588 dòng)
```

### Cấu trúc Hierarchy tạo ra

```
Room_Start_0_0 (GameObject)
└── Visuals
    ├── FloorGrid
    │   └── FloorTilemap (Tilemap + TilemapRenderer)
    ├── WallsGrid  
    │   └── WallsTilemap (Tilemap + TilemapRenderer + TilemapCollider2D 
    │                      + Rigidbody2D + CompositeCollider2D)
    └── Doors
        ├── Door_Top (Prefab + DoorTrigger + BoxCollider2D)
        ├── Door_Bottom
        ├── Door_Left
        └── Door_Right
```

### GenerateVisuals — Hàm chính

```csharp
public void GenerateVisuals(RoomData data, Dictionary<DoorDirection, Room> connections)
{
    roomData = data;
    connectedRooms = connections;

    // Lưu hướng nào có phòng kế bên
    activeDirections = new HashSet<DoorDirection>();
    if (connections != null)
        foreach (var kvp in connections)
            activeDirections.Add(kvp.Key);
    // VD: connections có Right và Top → activeDirections = {Right, Top}
    // → Chỉ tạo cửa ở 2 hướng này, Left và Bottom = tường đặc

    // Tạo container
    GameObject visualContainer = new GameObject("Visuals");
    visualContainer.transform.SetParent(transform);

    // TẠO FLOOR
    if (autoFillTiles && floorTiles != null)
        CreateAutoFilledFloor(visualContainer.transform);

    // TẠO WALLS
    if (autoFillTiles && wallCenter != null)
        CreateAutoFilledWalls(visualContainer.transform);

    // TẠO DOORS
    if (doorPrefab != null)
        GenerateDoorPrefabs(visualContainer.transform);
}
```

### CreateAutoFilledFloor — Trải sàn

```csharp
private void CreateAutoFilledFloor(Transform parent)
{
    // Tạo Grid → Tilemap structure
    Grid grid = gridObj.AddComponent<Grid>();
    grid.cellSize = new Vector3(1f, 1f, 0);  // 1 cell = 1 world unit

    Tilemap tilemap = tilemapObj.AddComponent<Tilemap>();
    TilemapRenderer renderer = tilemapObj.AddComponent<TilemapRenderer>();
    renderer.sortingOrder = -10;  // Nằm DƯỚI tất cả (wall = 0, door = 10)

    int tilesX = currentRoom.actualSize.x;  // VD: 15
    int tilesY = currentRoom.actualSize.y;  // VD: 15

    // Fill INTERIOR ONLY (x=1 đến 13, y=1 đến 13) — edge dành cho wall
    for (int x = 1; x < tilesX - 1; x++)
    {
        for (int y = 1; y < tilesY - 1; y++)
        {
            // Random pick 1 tile từ mảng → variation tự nhiên
            TileBase randomTile = floorTiles[Random.Range(0, floorTiles.Length)];
            tilemap.SetTile(new Vector3Int(x, y, 0), randomTile);
        }
    }
    // Phòng 15×15: fill 13×13 = 169 tiles floor
}
```

```
Phòng 15×15:
W W W W W W W W W W W W W W W    W = Wall edge (không fill floor)
W F F F F F F F F F F F F F W    F = Floor tiles (random)
W F F F F F F F F F F F F F W
W F F F F F F F F F F F F F W
W F F F F F F F F F F F F F W
...
W W W W W W W W W W W W W W W
```

### CreateAutoFilledWalls — Xây tường có collision

```csharp
private void CreateAutoFilledWalls(Transform parent)
{
    // ... tạo Grid + Tilemap ...

    // QUAN TRỌNG: Thêm collision components
    TilemapCollider2D tilemapCollider = tilemapObj.AddComponent<TilemapCollider2D>();
    Rigidbody2D rb = tilemapObj.AddComponent<Rigidbody2D>();
    rb.bodyType = RigidbodyType2D.Static;  // Tường không di chuyển

    // CompositeCollider2D gộp 56 colliders nhỏ → vài polygon lớn
    CompositeCollider2D composite = tilemapObj.AddComponent<CompositeCollider2D>();
    tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
```

**Tại sao cần CompositeCollider2D?**

```
KHÔNG có Composite (56 colliders riêng lẻ):     CÓ Composite (2-3 polygon):
┌─┐┌─┐┌─┐┌─┐┌─┐┌─┐┌─┐┌─┐┌─┐                   ┌─────────────────────┐
│ ││ ││ ││ ││ ││ ││ ││ ││ │                   │                     │
└─┘└─┘└─┘└─┘└─┘└─┘└─┘└─┘└─┘                   └─────────────────────┘
→ 56 collision checks mỗi frame               → 3 collision checks mỗi frame
→ LAG khi nhiều rooms                           → MƯỢT
```

```csharp
    // Tính vị trí door → "khoét lỗ" tường tại đó
    var doorTilePositions = new HashSet<Vector2Int>();
    foreach (var door in roomData.doorAnchors)
    {
        // CHỈ tạo lỗ nếu hướng này có phòng kế bên
        if (!activeDirections.Contains(door.direction)) continue;

        // Mỗi door xóa 2 tile: edge + inner (để player đi qua)
        switch (door.direction)
        {
            case DoorDirection.Top:
                int cx = tilesX / 2;           // Center = 7
                doorTilePositions.Add(new Vector2Int(cx, tilesY - 1)); // Tile edge (7, 14)
                doorTilePositions.Add(new Vector2Int(cx, tilesY - 2)); // Tile inner (7, 13)
                break;
            // ... tương tự cho Bottom, Left, Right
        }
    }

    // Fill tường ở EDGE ONLY, skip tại door positions
    for (int x = 0; x < tilesX; x++)
    for (int y = 0; y < tilesY; y++)
    {
        bool isEdge = (x == 0 || x == tilesX-1 || y == 0 || y == tilesY-1);
        if (!isEdge) continue;  // Interior → không fill wall

        if (doorTilePositions.Contains(new Vector2Int(x, y)))
            continue;  // Door position → KHÔNG đặt wall

        // Chọn tile phù hợp theo vị trí
        TileBase wallTile = GetWallTileForPosition(x, y, tilesX, tilesY);
        tilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
    }
}
```

```
Phòng 15×15 có door ở Top và Right:
W W W W W W W . W W W W W W W    . = door hole (không có wall)
W                           .
W                           W
W                           W
...
W W W W W W W W W W W W W W W
```

### GenerateDoorPrefabs — Tạo cửa thật

```csharp
private void GenerateDoorPrefabs(Transform parent)
{
    foreach (var door in roomData.doorAnchors)
    {
        // CHỈ tạo door nếu có phòng ở hướng đó
        if (!activeDirections.Contains(door.direction)) continue;

        // Instantiate prefab (có sprite + animation)
        GameObject doorObj = Instantiate(doorPrefab, doorContainer.transform);
        doorObj.name = $"Door_{door.direction}";

        // Tính vị trí: door nằm ở CENTER của wall tile
        Vector3 doorPos = GetDoorPosition(door, roomWidth, roomHeight);
        doorObj.transform.localPosition = doorPos;

        // ★ ADD DoorTrigger component VÀ configure
        var doorTrigger = doorObj.AddComponent<DoorTrigger>();
        doorTrigger.doorDirection = door.direction;
        doorTrigger.currentRoom = currentRoom;  // Phòng chứa door này

        // Gán phòng đích từ connectedRooms
        if (connectedRooms.ContainsKey(door.direction))
            doorTrigger.targetRoom = connectedRooms[door.direction];

        // Collider bắt đầu SOLID (chặn player)
        // DoorTrigger.Update() sẽ chuyển sang Trigger khi player lại gần
        boxCollider.isTrigger = false;
    }
}
```

---

## 📂 FILE 7: `DoorTrigger.cs` — Cửa thông minh

```
📁 Assets/Scripts/ProceduralGeneration/Components/DoorTrigger.cs (448 dòng)
```

### Lifecycle của Door

```
Awake() → Setup collider, RB, audio
Start() → Tìm player, tìm rooms từ DungeonManager
Update() → Kiểm tra khoảng cách → auto open/close
OnTriggerEnter2D() → Player bước vào → transition phòng
```

### Update — Auto mở/đóng cửa

```csharp
void Update()
{
    // Chỉ xử lý nếu: auto mode BẬT, cửa CHƯA KHÓA, player TỒN TẠI
    if (autoOpenClose && !isLocked && player != null)
    {
        float distance = Vector2.Distance(transform.position, player.position);

        // Player TRONG phạm vi (4 units) VÀ cửa ĐANG ĐÓNG → MỞ
        if (distance <= detectionRange && !isOpen)
            OpenDoor();

        // Player NGOÀI phạm vi VÀ cửa ĐANG MỞ → ĐÓNG
        else if (distance > detectionRange && isOpen)
            CloseDoor();
    }
}
```

### OpenDoor / CloseDoor — Dual-mode collider

```csharp
public void OpenDoor()
{
    if (isLocked) { PlaySound(lockedSound); return; }

    isOpen = true;
    UpdateVisualFeedback();  // Hiện openVisual, ẩn closedVisual
    UpdateColliderState();   // ★ Collider chuyển sang Trigger mode
    PlayAnimation("Open");   // doorAnimator.SetBool("isOpen", true)
    PlaySound(openSound);
}

private void UpdateColliderState()
{
    if (isOpen)
    {
        // ★ CỬA MỞ: Collider = TRIGGER
        // Player đi qua KHÔNG bị chặn → trigger OnTriggerEnter2D
        doorCollider.enabled = true;
        doorCollider.isTrigger = true;
    }
    else
    {
        // ★ CỬA ĐÓNG: Collider = SOLID
        // Player đi vào BỊ CHẶN (như tường)
        doorCollider.enabled = true;
        doorCollider.isTrigger = false;
    }
}
```

### OnTriggerEnter2D — Chuyển phòng

```csharp
void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Player"))
    {
        if (isOpen && !isLocked)
        {
            TriggerRoomTransition(other.gameObject);
        }
        else if (isLocked)
        {
            PlaySound(lockedSound);
            // Cửa khóa → cho boss fights (khóa khi vào, mở khi clear)
        }
    }
}

private void TriggerRoomTransition(GameObject player)
{
    // Tìm RoomTransitionManager trong scene
    var transitionManager = FindFirstObjectByType<RoomTransitionManager>();
    // Gọi transition: từ phòng A → phòng B, theo hướng door
    transitionManager.TransitionToRoom(currentRoom, targetRoom, doorDirection, player);
}
```

---

## 📂 FILE 8: `RoomTransitionManager.cs` — Chuyển phòng mượt

```
📁 Assets/Scripts/Core/RoomTransitionManager.cs (325 dòng)
```

### TransitionCoroutine — Toàn bộ quá trình chuyển phòng

```csharp
private IEnumerator TransitionCoroutine(Room fromRoom, Room toRoom,
    DoorDirection doorDirection, GameObject player)
{
    isTransitioning = true;  // Flag chống spam (click liên tục)

    // === BƯỚC 1: MÀN HÌNH TỐI DẦN (0.5 giây) ===
    yield return StartCoroutine(FadeOut());
    // CanvasGroup.alpha: 0 → 1 (trong suốt → đen hoàn toàn)

    // === BƯỚC 2: ẨN PHÒNG CŨ ===
    fromRoom.roomInstance.SetActive(false);
    // → Tất cả components ngừng Update(), Renderer ngừng vẽ, Collider disable
    // → Tiết kiệm CPU + GPU

    // === BƯỚC 3: HIỆN PHÒNG MỚI ===
    toRoom.roomInstance.SetActive(true);

    // === BƯỚC 3.5: ÁP DỤNG URP 2D LIGHTING ===
    ConvertRoomSpritesToLit(toRoom);
    // Chuyển material: Sprite-Default → Sprite-Lit-Default
    // Nếu không chuyển: sprite sẽ SÁNG HOÀN TOÀN bất kể có light hay không

    // === BƯỚC 4: TELEPORT PLAYER ===
    Vector3 spawnPosition = CalculatePlayerSpawnPosition(toRoom, doorDirection);
    player.transform.position = spawnPosition;

    // === BƯỚC 5: MÀN HÌNH SÁNG DẦN (0.5 giây) ===
    yield return StartCoroutine(FadeIn());
    // CanvasGroup.alpha: 1 → 0 (đen → trong suốt)

    isTransitioning = false;
}
```

### CalculatePlayerSpawnPosition — Đặt player ở đâu?

```csharp
private Vector3 CalculatePlayerSpawnPosition(Room room, DoorDirection enteredFrom)
{
    // Lấy bounds thực tế từ Renderer components
    Renderer[] renderers = room.roomInstance.GetComponentsInChildren<Renderer>();
    Bounds combinedBounds = renderers[0].bounds;
    foreach (var renderer in renderers)
        combinedBounds.Encapsulate(renderer.bounds);  // Gộp tất cả bounds

    float spawnOffset = 2.5f;  // Cách wall 2.5 units

    switch (enteredFrom)
    {
        case DoorDirection.Top:
            // Đi vào từ cửa TOP phòng cũ → spawn ở cửa BOTTOM phòng mới
            return new Vector3(
                roomOrigin.x + roomWidth / 2f,    // Center X
                roomOrigin.y + spawnOffset, 0);    // Gần bottom
```

**Logic**: Player đi ra cửa TOP phòng cũ → phòng mới nằm BÊN TRÊN → player spawn ở cửa BOTTOM phòng mới (đối diện).

```
Phòng cũ:           Phòng mới:
┌──────────┐        ┌──────────┐
│          │        │          │
│          │        │          │
│  Player→ ┤ TOP    │          │
└──────────┘        │  ★ spawn │ ← BOTTOM
                    └──────────┘
```

---

## 📂 FILE 9: `DungeonUtils.cs` — Công cụ tiện ích

```
📁 Assets/Scripts/ProceduralGeneration/Core/DungeonUtils.cs (245 dòng)
```

### Fisher-Yates Shuffle — Xáo trộn không thiên vị

```csharp
public static void Shuffle<T>(List<T> list)
{
    // Duyệt từ cuối → đầu
    for (int i = list.Count - 1; i > 0; i--)
    {
        // Random 1 index từ 0 đến i (bao gồm i)
        int j = Random.Range(0, i + 1);

        // Swap phần tử i với j
        T temp = list[i];
        list[i] = list[j];
        list[j] = temp;
    }
}
```

**Ví dụ**: Shuffle [Top, Bottom, Left, Right]
```
i=3: j=random(0..3)=1 → swap [3]↔[1] → [Top, Right, Left, Bottom]
i=2: j=random(0..2)=0 → swap [2]↔[0] → [Left, Right, Top, Bottom]
i=1: j=random(0..1)=1 → swap [1]↔[1] → [Left, Right, Top, Bottom]
Kết quả: [Left, Right, Top, Bottom]
```

### A* Pathfinding — Tìm đường giữa phòng

```csharp
public static List<Room> FindPath(Room start, Room end, List<Room> allRooms)
{
    // gScore[room] = chi phí thực tế từ start → room
    Dictionary<Room, int> gScore = new Dictionary<Room, int>();
    // fScore[room] = gScore + heuristic (ước lượng → end)
    Dictionary<Room, int> fScore = new Dictionary<Room, int>();
    // cameFrom[room] = phòng trước đó trên đường đi
    Dictionary<Room, Room> cameFrom = new Dictionary<Room, Room>();

    List<Room> openSet = new List<Room> { start };
    gScore[start] = 0;
    fScore[start] = ManhattanDistance(start.gridPosition, end.gridPosition);

    while (openSet.Count > 0)
    {
        // Lấy phòng có fScore thấp nhất
        Room current = openSet.OrderBy(r => fScore[r]).First();

        if (current == end)
            return ReconstructPath(cameFrom, current);  // ĐÃ TÌM THẤY!

        openSet.Remove(current);

        // Duyệt phòng kế bên (qua connectedRooms)
        foreach (var neighbor in current.connectedRooms.Values)
        {
            int tentativeG = gScore[current] + 1;  // Mỗi bước = cost 1

            if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
            {
                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] = tentativeG + ManhattanDistance(...);
                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
            }
        }
    }
    return null;  // Không tìm thấy đường
}
```

**Ví dụ A* trên room graph**:
```
Start(0,0) → Arch1(15,0) → Arch1(30,0) → MidBoss(30,15) → Goal(45,15)

Tìm đường Start → Goal:
gScore: Start=0, Arch1_15=1, Arch1_30=2, MidBoss=3, Goal=4
Path: [Start, Arch1_15, Arch1_30, MidBoss, Goal]
```

### CanPlaceRoomAt — Check overlap

```csharp
public static bool CanPlaceRoomAt(Vector2Int position, Vector2Int size,
    Dictionary<Vector2Int, Room> occupiedCells)
{
    // Duyệt qua TẤT CẢ ô mà phòng mới sẽ chiếm
    for (int x = 0; x < size.x; x++)
    for (int y = 0; y < size.y; y++)
    {
        Vector2Int cellPos = position + new Vector2Int(x, y);
        // Nếu ô đã bị phòng khác chiếm → KHÔNG đặt được
        if (occupiedCells.ContainsKey(cellPos))
            return false;
    }
    return true;  // Tất cả ô đều trống → OK
}
```

**Dictionary lookup là O(1)** → check 225 ô (15×15) chỉ mất 225 × O(1) = O(225), rất nhanh.

---

## 📂 FILE 10: `DungeonEvents.cs` — Hệ thống sự kiện

```
📁 Assets/Settings/Dungeon/Core/DungeonEvents.cs (65 dòng)
```

```csharp
// STATIC class = không cần tạo instance, gọi trực tiếp
public static class DungeonEvents
{
    // event = cho phép hệ thống khác "đăng ký" nhận thông báo
    public static event Action<DungeonGenerator2D.Result> OnDungeonGenerated;
    public static event Action<DungeonMap> OnDungeonRendered;
    public static event Action OnEntitiesSpawned;
    public static event Action<Vector3> OnPlayerSpawned;
    public static event Action OnMapReady;
```

**Cách hoạt động Event**:
```csharp
// === BÊN PHÁT (MapInitializationManager) ===
DungeonEvents.RaiseDungeonGenerated(result);
// → Gọi tất cả subscriber

// === BÊN NHẬN (Minimap script) ===
void OnEnable()
{
    DungeonEvents.OnDungeonGenerated += HandleDungeonGenerated;
    // "Tôi muốn biết khi dungeon được tạo"
}

void HandleDungeonGenerated(DungeonGenerator2D.Result result)
{
    // "Dungeon đã tạo! Tôi update minimap"
    UpdateMinimap(result);
}
```

```csharp
    // XÓA TẤT CẢ SUBSCRIBERS — gọi khi chuyển scene
    public static void ClearAll()
    {
        OnDungeonGenerated = null;
        OnDungeonRendered = null;
        OnEntitiesSpawned = null;
        OnPlayerSpawned = null;
        OnMapReady = null;
    }
    // TẠI SAO CẦN? Vì events là STATIC → tồn tại xuyên scenes
    // Nếu không clear: subscriber từ scene cũ (đã destroy) vẫn được gọi
    // → NullReferenceException!
```

---

## Tổng kết mối liên hệ giữa các file

```
Player nhấn Play
    ↓
DungeonManager.Awake()
    ↓ đọc
RoomData (ScriptableObjects) ← cấu hình size, doors, type
    ↓ tạo
Room (C# objects) ← giữ gridPosition, connections
    ↓ instantiate
RoomVisualGenerator (Component) ← tạo Floor + Wall Tilemaps
    ↓ tạo
DoorTrigger (Component) ← auto open/close, trigger transition
    ↓ gọi
RoomTransitionManager ← fade + swap rooms + teleport player
    ↓ convert
URP 2D Lighting ← cho phòng mới nhận ánh sáng

Hệ thống hỗ trợ:
  DungeonUtils → shuffle, A*, collision check
  DungeonEvents → thông báo cho minimap, lighting, audio
  DungeonGeneratorWindow → Editor UI cho designer
```
