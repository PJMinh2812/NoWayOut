# 🌀 Goal Portal Transition — Code Deep Dive

> Goal portal là điểm xác nhận qua map tiếp theo.
> Portal làm 2 việc: nhận input player và chuyển thông tin spawn anchor cho progression manager.

---

## Tổng quan flow

```text
DungeonRunProgressionManager.MarkCurrentMapCompleted()
  -> SpawnPortalAtGoalRoom()
    -> portal.Setup(this)

Player đứng trong trigger portal + nhấn E
  -> GoalPortal.Update()
    -> progressionManager.TryAdvanceToNextMap(spawnAnchor)
      -> GenerateCurrentMap()
      -> map mới sinh theo anchor truyền vào
```

---

## 📂 FILE 1: `GoalPortal.cs`

```text
📁 Assets/Scripts/ProceduralGeneration/Integration/GoalPortal.cs
```

### Trách nhiệm chính

- Bắt input xác nhận qua map.
- Đảm bảo chỉ player hợp lệ mới trigger.
- Truyền `spawnAnchor` vào `DungeonRunProgressionManager`.

### Auto wiring manager

Portal hỗ trợ 2 mode:

- `Setup(manager)` khi spawn động từ progression manager.
- `autoFindProgressionManager` để tự tìm trong scene.

Nếu thiếu manager, portal log warning và không cho advance.

### Input confirm

`WasConfirmPressedThisFrame()` check:

- key đã cấu hình (`confirmKey`, mặc định E)
- fallback: `eKey`, `enterKey`, `numpadEnterKey`

Fallback này xử lý tình huống prefab cũ deserialize key bị `None`.

### Trigger state ổn định

Portal dùng `HashSet<int> playerBodyIdsInTrigger`:

- hỗ trợ player nhiều collider
- tránh trường hợp enter/exit lệch gây false negative

### Spawn anchor truyền cho map mới

Khi confirm:

```csharp
Vector3 spawnAnchor = triggerPlayerTransform != null
    ? triggerPlayerTransform.position
    : transform.position;

progressionManager.TryAdvanceToNextMap(spawnAnchor);
```

Ý nghĩa:

- Nếu xác định được transform player trong trigger -> neo theo player thật.
- Nếu không -> fallback neo tại vị trí portal.

---

## 📂 FILE 2: `DungeonRunProgressionManager.cs` (phần portal)

```text
📁 Assets/Scripts/ProceduralGeneration/Integration/DungeonRunProgressionManager.cs
```

### Spawn portal sau khi complete map

`SpawnPortalAtGoalRoom()`:

1. Lấy Goal room từ `dungeonManager.GetGoalRoom()`.
2. Tính center room.
3. Instantiate `portalPrefab` làm child của Goal room instance.
4. Gắn/khởi tạo `GoalPortal` component.
5. `portal.Setup(this)` để link manager.

### Retry spawn khi room chưa sẵn

Nếu spawn lần đầu fail:

- chạy coroutine `RetrySpawnPortalAtGoalRoom()`
- chờ theo `portalSpawnRetryInterval`
- retry tối đa `portalSpawnMaxRetries`

Điều này giảm race condition khi map vừa generate xong nhưng room object chưa stable.

### Chặn qua màn sớm

`TryAdvanceToNextMap(...)` có guard:

- map phải completed, hoặc fallback complete khi không có enemy manager/all enemies dead.
- nếu vẫn chưa completed -> return false.

Mục tiêu: tránh player "nhảy map" ngoài ý muốn.

---

## Liên kết với map alignment

Spawn anchor từ portal được chuyển thành `pendingMapSpawnPosition`.
Sau khi generate map mới, manager gọi:

```csharp
AlignGeneratedDungeonToSpawnPosition(desiredSpawnPosition)
```

Kết quả:

- Không chỉ player, mà toàn bộ dungeon được align về anchor mong muốn.
- Transition giữa map mượt hơn, cảm giác không "dịch tâm" bất thường.

---

## Khác biệt Portal vs Chest

- Portal: unlock progression sang map tiếp theo.
- Chest: mở mốc objective meta của round (ảnh hưởng ending).

Trong map x-5 đã complete, thường cả 2 cùng tồn tại:

- mở chest trước (khuyến khích)
- sau đó vào portal để đi map mới

---

## Checklist test nhanh

1. Confirm ngoài trigger -> không qua map.
2. Confirm trong trigger khi map chưa complete -> bị chặn.
3. Complete map -> portal xuất hiện ở Goal room.
4. Confirm trong portal -> qua map mới và spawn anchor hoạt động đúng.
5. Goal room chưa sẵn ngay frame đầu -> retry spawn thành công.
