# 🔁 Auto Generate Map Khi Qua Màn — Code Deep Dive

> Tài liệu này mô tả luồng **hoàn thành map -> qua portal -> sinh map mới**,
> kèm cơ chế **save/continue** để giữ đúng seed, vị trí, và trạng thái progression.

---

## Tổng quan flow runtime

```text
DungeonRunProgressionManager.Start()
  -> TryRestoreMapFromSave()
      -> có save: restore round/map/seed/map anchor/chest state
      -> không save: GenerateCurrentMap()

Trong lúc chơi map:
  Update() kiểm tra auto-complete khi hết enemy
    -> MarkCurrentMapCompleted()
      -> spawn GoalChest (nếu map x-5 và chest round chưa mở)
      -> spawn GoalPortal

Player tương tác GoalPortal (nhấn E):
  GoalPortal.TryAdvanceToNextMap(spawnAnchor)
    -> tăng currentMap/currentRound
    -> GenerateCurrentMap()
        -> apply difficulty + AI director bias
        -> dungeonManager.GenerateDungeon()
        -> align dungeon về spawn anchor
        -> reset/tracking telemetry map mới
```

---

## 📂 FILE 1: `DungeonRunProgressionManager.cs`

```text
📁 Assets/Scripts/ProceduralGeneration/Integration/DungeonRunProgressionManager.cs
```

### Vai trò

- Quản lý run theo cấu trúc `totalRounds x mapsPerRound` (mặc định 3 x 5).
- Quyết định khi nào map được xem là hoàn thành.
- Spawn `GoalChest` và `GoalPortal` trong Goal room.
- Thực hiện chuyển map tiếp theo và generate dungeon mới.
- Theo dõi hiệu năng map để feed AI Director.

### Nhóm state quan trọng

```csharp
private int currentRound = 1;
private int currentMap = 1;
private bool currentMapCompleted;

private bool hasPendingMapSpawnOverride;
private Vector3 pendingMapSpawnPosition;

private bool hasForcedSeedForCurrentGenerate;
private int forcedSeedForCurrentGenerate;

private int openedGoalChestMask;
private int openedGoalChestCount;
```

Ý nghĩa:

- `currentRound/currentMap`: vị trí hiện tại trong run.
- `currentMapCompleted`: khóa logic qua map khi chưa clear.
- `pendingMapSpawnPosition`: điểm neo map mới (lấy từ vị trí player khi vào portal).
- `forcedSeedForCurrentGenerate`: đảm bảo continue ra đúng layout cũ.
- `openedGoalChestMask`: bitmask lưu chest đã mở theo round.

### Start và khôi phục save

```csharp
private void Start()
{
    if (!TryRestoreMapFromSave())
    {
        GenerateCurrentMap();
    }
}
```

`TryRestoreMapFromSave()` làm 5 việc chính:

1. Đọc cờ restore từ `PlayerPrefs` (`RestoreDungeonFromSave` / `LoadFromSave`).
2. Load `SaveData` và khôi phục:

- `runCurrentRound`, `runCurrentMap`
- `runOpenedGoalChestMask`
- `runCurrentMapCompleted`

3. Khôi phục `map anchor` để map mới nằm lại đúng offset cũ.
4. Khôi phục seed dungeon từ save (hoặc fallback `LastDungeonSeed`).
5. Gọi `GenerateCurrentMap()` rồi re-apply trạng thái map completed nếu cần.

### Điều kiện tự complete map

```csharp
if (autoCompleteWhenNoEnemiesAlive)
{
    bool allEnemiesDead = EnemySpawnManager.Instance != null
                          && EnemySpawnManager.Instance.AliveEnemyCount <= 0;
    if (allEnemiesDead)
        MarkCurrentMapCompleted();
}
```

Guard quan trọng:

- Có `minAutoCompleteDelay` để tránh complete ngay khi vừa generate.
- Chỉ complete khi `EnemySpawnManager` tồn tại và `AliveEnemyCount == 0`.

### `MarkCurrentMapCompleted()`

Trình tự:

1. `FinalizeMapPerformanceIfNeeded()` chốt telemetry map.
2. Set `currentMapCompleted = true`.
3. Spawn chest (nếu hợp lệ).
4. Spawn portal (retry nhiều lần nếu Goal room chưa sẵn).

### Qua map mới: `TryAdvanceToNextMap(...)`

Logic:

1. Nếu map chưa completed:

- thử fallback complete nếu không có enemy manager hoặc enemy đã chết.
- vẫn chưa complete thì từ chối qua map.

2. Nếu đã ở map cuối run (`3-5`) thì gọi `HandleRunFinished()` -> ending.
3. Capture spawn anchor từ portal/player.
4. Tăng `currentMap`/`currentRound`.
5. `GenerateCurrentMap()`.

### Generate map mới: `GenerateCurrentMap()`

Các bước:

1. Resolve spawn mong muốn (`ResolveDesiredSpawnPosition`).
2. Dọn object cũ: portal/chest/retry coroutine/enemies.
3. Tính difficulty theo round:

- tăng số room
- tăng branch probability

4. Apply AI Director adjustment vào `roomCount/branchProb`.
5. Gán seed cho `DungeonManager` nếu là restore.
6. Gọi `dungeonManager.GenerateDungeon()`.
7. `AlignGeneratedDungeonToSpawnPosition(...)` để map đúng neo spawn.
8. Spawn fragment ở map 1-1 nếu là run mới.
9. Set `RoomTransitionManager` current room = start room.
10. Refresh light đặc biệt và bắt đầu tracking telemetry map mới.

---

## 📂 FILE 2: `SaveManager.cs`

```text
📁 Assets/Scripts/Core/SaveManager.cs
```

### Dữ liệu save liên quan tới run progression

```csharp
public bool hasDungeonSeed;
public int dungeonSeed;

public bool hasMapAnchor;
public float mapAnchorX;
public float mapAnchorY;

public bool hasRunProgressionState;
public int runCurrentRound;
public int runCurrentMap;
public int runOpenedGoalChestMask;

public bool hasRunCurrentMapCompleted;
public bool runCurrentMapCompleted;
```

Tại sao cần đầy đủ bộ này:

- `dungeonSeed`: để continue sinh lại đúng layout.
- `mapAnchor`: giữ offset world ổn định, tránh "lệch map".
- `runCurrentRound/map`: quay lại đúng tiến trình run.
- `openedGoalChestMask`: không cho farm chest lại.
- `runCurrentMapCompleted`: map đã clear thì portal/chest phải xuất hiện lại.

### SaveGame() kết hợp dungeon + progression

`SaveGame()` sẽ:

- Lấy seed hiện tại từ `DungeonManager`.
- Lấy map anchor từ `Respawn_Point`.
- Lưu room hiện hành để continue spawn sát ngữ cảnh.
- Lưu progression state từ `DungeonRunProgressionManager`.

---

## 📂 FILE 3: `GameManager.cs` (liên quan gián tiếp)

```text
📁 Assets/Scripts/Core/GameManager.cs
```

Vai trò liên quan run:

- Duy trì các manager singleton cần thiết (`SaveManager`, `DungeonLightingManager`, `MinimapManager`, ...).
- Đảm bảo hệ thống save/load và UI không bị thiếu component khi scene khởi động.

---

## Cơ chế spawn anchor và align map

### Vì sao không teleport player đơn thuần?

Code chọn cách **dịch cả dungeon container** theo offset từ `Respawn_Point` cũ -> `desiredSpawnPosition`:

```csharp
Vector3 offset = desiredSpawnPosition - currentSpawnPosition;
dungeonManager.dungeonContainer.position += offset;
```

Ưu điểm:

- Tất cả room giữ nguyên tương quan vị trí.
- Collider/light/minimap đồng bộ theo map mới.
- Tránh sai lệch do tính thủ công center từng room.

---

## Rule spawn chest/portal theo map

- Portal: spawn khi map completed.
- Chest: chỉ spawn ở `map x-5` của từng round, và chỉ khi chest round đó chưa mở.

Điều kiện chest:

```csharp
if (currentMap != mapsPerRound) return false;
if (currentRound < 1 || currentRound > TotalGoalChests) return false;
return !IsChestOpenedForRound(currentRound);
```

---

## Bitmask chest progress

Ví dụ `totalGoalChests = 3`:

- Round 1 mở chest -> set bit 0
- Round 2 mở chest -> set bit 1
- Round 3 mở chest -> set bit 2

Nếu đã mở round 1 và 3:

```text
mask = 0b101 = 5
openedGoalChestCount = 2
```

Dùng bitmask giúp save nhẹ và check O(1).

---

## Điều kiện kết thúc run

Khi player qua portal tại map `3-5`:

- Nếu `openedGoalChestCount >= totalGoalChests` -> `goodEndingSceneName`
- Ngược lại -> `badEndingSceneName`

Điểm này tạo liên kết gameplay giữa exploration (mở chest) và outcome ending.

---

## Checklist test nhanh

1. Vào map 1-1, clear hết enemy -> thấy portal spawn.
2. Qua portal -> map mới generate, player/map anchor đúng vị trí.
3. Đến map x-5 mỗi round -> có chest, mở được 1 lần.
4. Save giữa run -> Continue -> round/map/seed/chest state khôi phục đúng.
5. Hoàn thành 3-5 -> chuyển đúng ending theo số chest đã mở.
