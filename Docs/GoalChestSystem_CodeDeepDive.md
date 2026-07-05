# 🧰 Goal Chest System — Code Deep Dive

> Goal chest là cơ chế progression phụ tại map `x-5` mỗi round.
> Player mở chest để ghi nhận mốc run (ảnh hưởng ending tốt/xấu).

---

## Tổng quan flow

```text
Map hoàn thành
  -> DungeonRunProgressionManager.MarkCurrentMapCompleted()
    -> nếu ShouldSpawnGoalChestOnCurrentMap() == true
       -> SpawnGoalChestAtGoalRoom()

Player vào trigger chest + nhấn E
  -> GoalChest.TryOpenChest()
    -> progressionManager.TryOpenGoalChest()
      -> set bit openedGoalChestMask
      -> tăng openedGoalChestCount
```

---

## 📂 FILE 1: `GoalChest.cs`

```text
📁 Assets/Scripts/ProceduralGeneration/Integration/GoalChest.cs
```

### Vai trò

- Quản lý tương tác nhấn phím mở chest.
- Đồng bộ trạng thái visual (đóng/mở) với progression thật.
- Phát SFX mở rương.

### Thiết kế interaction

```csharp
[SerializeField] private Key confirmKey = Key.E;
[SerializeField] private bool requirePlayerInTrigger = true;
```

- Mặc định xác nhận bằng `E`.
- Có thể bắt buộc player đứng trong trigger mới mở được.

### Auto setup an toàn trong `Awake()`

Nếu prefab thiếu component, script tự bổ sung:

- thiếu `Collider2D` -> thêm `BoxCollider2D`
- thiếu `Rigidbody2D` -> thêm kinematic RB
- ép collider sang trigger mode

Mục tiêu: giảm lỗi do prefab chưa wire đầy đủ.

### Trigger tracking chống sai collider

Script dùng `HashSet<int> playerBodies` để theo dõi overlap theo body id.

Lợi ích:

- Player có nhiều collider con vẫn xử lý đúng vào/ra trigger.
- Tránh flicker `playerInTrigger` khi overlap phức tạp.

### Tương tác mở chest

```csharp
bool opened = progressionManager.TryOpenGoalChest();
```

- Nếu `false`: chest giữ trạng thái cũ, sync lại từ progression.
- Nếu `true`: set `isOpened`, update animator bool, play SFX.

### Đồng bộ state từ progression

`SyncOpenedStateFromProgression()` đảm bảo:

- Load save xong chest hiển thị đúng trạng thái đã mở/chưa mở.
- Không phụ thuộc trạng thái local trước đó.

---

## 📂 FILE 2: `DungeonRunProgressionManager.cs` (phần chest)

```text
📁 Assets/Scripts/ProceduralGeneration/Integration/DungeonRunProgressionManager.cs
```

### Điều kiện spawn chest

`ShouldSpawnGoalChestOnCurrentMap()`:

1. Chỉ ở map cuối round (`currentMap == mapsPerRound`).
2. `currentRound` hợp lệ trong khoảng chest support.
3. Chest round đó chưa mở.

### Spawn vị trí chest

- Tâm Goal room = `roomInstance.position + actualSize * 0.5f`.
- Chest đặt lệch khỏi portal theo:
- `goalChestOffsetDirection` (vector hướng)
- `goalChestDistanceFromPortal` (khoảng cách)

Điều này tránh chest đè đúng tâm portal.

### Mở chest và ghi tiến trình

```csharp
openedGoalChestMask |= 1 << (currentRound - 1);
openedGoalChestCount = CountSetBits(openedGoalChestMask);
```

- Mỗi round là 1 bit trong mask.
- Không thể mở lặp lại cùng round vì bit đã set.

### Đồng bộ ánh sáng với portal

Nếu `syncGoalChestLightWithPortal=true`:

- lấy `Light2D` từ portal
- copy sang chest bằng `CopyLight2D(...)`

Giúp visual consistency trong Goal room.

---

## Bitmask ví dụ nhanh

Với 3 chest tổng:

- Chỉ mở round 2 -> mask = `010` (2)
- Mở round 1 + 3 -> mask = `101` (5)
- Mở đủ -> mask = `111` (7)

`openedGoalChestCount` được tính bằng popcount.

---

## Liên kết ending

Khi run kết thúc (map 3-5):

- `openedGoalChestCount >= TotalGoalChests` -> ending tốt
- Ngược lại -> ending xấu

Nên chest là objective meta xuyên suốt run, không chỉ là reward map đơn.

---

## Checklist test nhanh

1. Ở map không phải x-5 -> không spawn chest.
2. Ở map x-5 round chưa mở -> spawn chest.
3. Mở chest xong save/load -> chest vẫn mở đúng trạng thái.
4. Không thể mở chest lần 2 cùng round.
5. Kết thúc run với đủ/thiếu chest -> chuyển đúng ending scene.
