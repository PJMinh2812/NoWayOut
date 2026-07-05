# Mirror Puzzle System - Hướng Dẫn Sử Dụng

## Tổng Quan

Hệ thống **Mirror Puzzle** cho phép tạo các câu đố sử dụng gương phản chiếu ánh sáng để kích hoạt công tắc và mở cửa. Đây là feature #5 trong roadmap 10 tính năng.

## Cơ Chế Hoạt Động

1. **Player** có đèn sáng (Light2D) chiếu xung quanh
2. **Mirror (Gương)** phát hiện ánh sáng player và phản chiếu theo góc rotation của nó
3. Ánh sáng phản chiếu chiếu đến **Light Receiver (Công tắc)** để kích hoạt
4. Khi đủ receiver được kích hoạt → **Puzzle Door** mở ra

### Cách Phản Chiếu

- Gương phản chiếu ánh sáng theo **định luật phản xạ**: góc tới = góc phản xạ
- Xoay gương (rotation Z) để điều chỉnh hướng phản chiếu
- Flash of Truth cũng có thể kích hoạt trực tiếp các receiver trong bán kính flash

## Các Script

### 1. LightMirror.cs

**Chức năng:** Gương phản chiếu ánh sáng

**Settings:**

- `detectionRadius` (8f): Phạm vi phát hiện ánh sáng player
- `maxReflectionDistance` (15f): Khoảng cách tối đa tia phản xạ
- `beamColor`: Màu của tia sáng phản chiếu (vàng nhạt mặc định)
- `beamWidth` (0.2f): Độ rộng tia sáng

**Visual:**

- Tự động tạo LineRenderer để hiển thị tia phản xạ
- Gương chuyển màu sáng khi activated
- Tia sáng có hiệu ứng pulse (nhấp nháy)

**Cách dùng:**

1. Tạo GameObject mới
2. Add component `LightMirror`
3. Add SpriteRenderer với sprite hình gương
4. Xoay gương để điều chỉnh hướng phản xạ
5. Set layer cho walls trong `obstacleLayer`

### 2. LightReceiver.cs

**Chức năng:** Công tắc được kích hoạt bởi ánh sáng

**Settings:**

- `requiresContinuousLight` (true): Cần ánh sáng liên tục hay kích hoạt 1 lần
- `oneTimeActivation` (false): Chỉ kích hoạt được 1 lần duy nhất
- `activationDelay` (0.5s): Thời gian cần giữ ánh sáng để kích hoạt

**Events:**

- `OnActivated`: Được gọi khi receiver kích hoạt
- `OnDeactivated`: Được gọi khi mất kích hoạt

**Visual:**

- Đổi màu từ xám (inactive) sang vàng (active)
- Light2D indicator tăng cường độ khi active
- Hỗ trợ ParticleSystem và AudioClip

**Cách dùng:**

1. Tạo GameObject mới cho công tắc
2. Add component `LightReceiver`
3. Add SpriteRenderer với sprite công tắc
4. (Optional) Add Light2D child cho hiệu ứng sáng
5. Setup các UnityEvent trong Inspector

### 3. MirrorPuzzleDoor.cs

**Chức năng:** Cửa mở khi đủ điều kiện receiver

**Settings:**

- `requiredReceivers[]`: Mảng các receiver cần kích hoạt
- `requireAllReceivers` (true): Cần TẤT CẢ hay CHỈ 1 receiver
- `openOffset`: Vector di chuyển khi cửa mở (mặc định Vector3.up \* 3)
- `openSpeed` (2f): Tốc độ mở cửa

**Cách dùng:**

1. Tạo GameObject cửa với SpriteRenderer và Collider2D
2. Add component `MirrorPuzzleDoor`
3. Kéo các LightReceiver vào array `requiredReceivers`
4. Chọn logic: requireAllReceivers = true (AND) hoặc false (OR)

### 4. FlashOfTruth Integration

**Flash of Truth** giờ cũng kích hoạt LightReceivers:

- Khi dùng Space, tất cả receiver trong bán kính flash sẽ được kích hoạt tạm thời
- Kích hoạt trong suốt `flashDuration` (5s)
- Hữu ích cho puzzle cần giữ nhiều receiver cùng lúc

## Thiết Kế Puzzle Mẫu

### Puzzle 1: Gương Đơn Giản

```
[Player] ---light---> [Mirror] ---reflected---> [Receiver] ---> [Door Open]
```

**Setup:**

1. Đặt 1 Mirror giữa player và receiver
2. Xoay mirror để phản chiếu đúng hướng
3. Tạo 1 receiver
4. Tạo door liên kết với receiver

### Puzzle 2: Nhiều Gương

```
[Player] --> [Mirror1] --> [Mirror2] --> [Receiver]
```

**Setup:**

1. Tạo chuỗi 2-3 gương phản chiếu liên tiếp
2. Player cần tìm vị trí đứng đúng
3. Hoặc xoay các gương theo thứ tự

### Puzzle 3: Multiple Receivers + Flash

```
[Player] ----> [Mirror] ----> [Receiver1]
    |
  Flash of Truth
    |
    +----------> [Receiver2]
    +----------> [Receiver3]
```

**Setup:**

1. 1 receiver xa cần mirror phản xạ
2. 2 receiver gần cần Flash of Truth
3. Door cần TẤT CẢ 3 receiver (requireAllReceivers = true)

## Tips Thiết Kế

### Độ Khó Tăng Dần

1. **Easy**: 1 gương → 1 receiver → 1 cửa
2. **Medium**: 2-3 gương chuỗi, hoặc nhiều receiver
3. **Hard**: Kết hợp mirror + flash, time pressure, moving mirrors

### Visual Clarity

- Dùng màu sắc rõ ràng cho gương (cyan/white)
- Receiver nên dễ nhận biết (vàng/cam)
- Tia sáng phải dễ thấy (vàng nhạt, alpha 0.8)

### Placement Tips

- Đặt gương ở vị trí strategic, không quá xa receiver
- Tránh để gương che khuất receiver
- Tạo nhiều góc nhìn để player hiểu puzzle

### Performance

- LineRenderer chỉ active khi gương hoạt động
- Dùng layer mask để tối ưu raycasting
- Limit số lượng mirror/receiver mỗi room (3-5)

## Testing Checklist

- [ ] Gương phát hiện ánh sáng player đúng range
- [ ] Tia phản xạ hiển thị chính xác
- [ ] Receiver kích hoạt khi nhận ánh sáng
- [ ] Door mở/đóng đúng logic (AND/OR)
- [ ] Flash of Truth kích hoạt receiver trong range
- [ ] Walls block light correctly
- [ ] Visual feedback rõ ràng (colors, particles, sounds)
- [ ] No performance issues with multiple mirrors

## Example Scene Setup

1. **Tạo Room:**
   - Walls với Collider2D, layer "Obstacle"

2. **Đặt Player:**
   - GameObject "Player" với Light2D
   - FlashOfTruth component

3. **Tạo Mirror:**
   - New GameObject "Mirror_01"
   - Add LightMirror component
   - Add sprite (ví dụ: square trắng)
   - Rotation Z = 45° (hoặc góc bất kỳ)
   - Set obstacleLayer = "Obstacle"

4. **Tạo Receiver:**
   - New GameObject "Switch_01"
   - Add LightReceiver component
   - Add sprite (circle/square vàng)

5. **Tạo Door:**
   - New GameObject "PuzzleDoor"
   - Add sprite cửa + BoxCollider2D
   - Add MirrorPuzzleDoor component
   - Assign Switch_01 vào requiredReceivers[0]

6. **Test:**
   - Play scene
   - Di chuyển player gần mirror
   - Kiểm tra tia sáng phản chiếu
   - Receiver sáng lên → Door mở

## Troubleshooting

**Gương không phản chiếu:**

- Kiểm tra player có Light2D component
- Kiểm tra detectionRadius đủ lớn
- Kiểm tra không có wall che giữa player và mirror

**Receiver không kích hoạt:**

- Kiểm tra tia phản xạ có chạm vào receiver
- Kiểm tra activationDelay (mặc định 0.5s)
- Thử gọi receiver.ForceActivate() để test

**Door không mở:**

- Kiểm tra tất cả receiver trong array được kích hoạt
- Kiểm tra requireAllReceivers logic
- Xem Console log "[MirrorPuzzleDoor] opened"

**Flash không kích hoạt receiver:**

- Kiểm tra receiver trong flashLightRadius (50 units)
- Kiểm tra Flash is unlocked (cần 3 fragments)

## Tích Hợp Với Các System Khác

### DoorTrigger Integration (Optional)

Có thể kết hợp MirrorPuzzleDoor với DoorTrigger hiện có:

- MirrorPuzzleDoor.OnDoorOpened → DoorTrigger.UnlockDoor()

### GameManager Integration

- Track số puzzle completed
- Save puzzle state khi transition room

## Tương Lai / Nâng Cấp

**V1 (Hiện tại):**

- Gương tĩnh, phản xạ cơ bản
- Receiver on/off đơn giản
- Door mở dọc

**V2 (Có thể thêm):**

- Gương xoay được (player push/interact)
- Colored light + colored receivers
- Multiple reflection paths cho 1 mirror
- Moving mirrors (patrol, puzzle element)
- Prism (split light into multiple beams)
- Light absorbers (block reflection)

---

**Status:** ✅ Feature #5 Complete
**Next:** Feature #6 - Upgrade System after Boss
