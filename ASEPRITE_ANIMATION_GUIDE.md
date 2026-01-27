# ⚡ HƯỚNG DẪN NHANH - SỬ DỤNG FILE ASEPRITE SẴN CÓ

## 🎯 BẠN ĐÃ CÓ ASEPRITE FILE
✅ `Assets/Art/MungeonDage/Dage/aseprite files/Dage_Anim01.aseprite`  
✅ Unity Aseprite Importer đã được cài đặt  
✅ File có 48+ frames animation

---

## 📋 BƯỚC 1: KIỂM TRA & CẤU HÌNH ASEPRITE IMPORT (2 phút)

### 1.1 Kiểm tra Import Settings

1. **Unity → Project panel**
2. **Navigate:** `Assets/Art/MungeonDage/Dage/aseprite files/`
3. **Click vào `Dage_Anim01.aseprite`**
4. **Inspector → Kiểm tra settings:**

```
✓ Import Sprite Sheet: Yes
✓ Generate Animation Clips: TRUE (QUAN TRỌNG!)
✓ Pixels Per Unit: 16 hoặc 100
✓ Filter Mode: Point (no filter) ← Cho pixel art!
✓ Generate Physics Shape: No
```

5. **Nếu `Generate Animation Clips` CHƯA tích ✓:**
   - Tích vào
   - **Apply**
   - Đợi Unity generate animations

### 1.2 Tìm Generated Animations

Unity sẽ tự động tạo animations trong subfolder:

1. **Project panel → Click mũi tên ▶ bên cạnh `Dage_Anim01.aseprite`**
2. Bạn sẽ thấy:
   - 📊 Texture atlas
   - 🎬 Animation clips (tự động generate từ tags/layers trong Aseprite)
   - 🖼️ Sprites riêng lẻ

**HOẶC** tìm trong:
```
GeneratedAssets/c658e477fd515474d804c8d49227a9d1/
```

### 1.3 Xác định Animation Clips có sẵn

File Aseprite thường có tags để phân chia animations:
- `Idle` → idle animation
- `Walk` / `Run` → walking animation
- `Attack` / `Hit` → attack animation
- `Death` → death animation

**Kiểm tra trong Aseprite (nếu cài):**
- Mở file `.aseprite` 
- Xem tags ở timeline
- Mỗi tag = 1 animation clip

**Hoặc kiểm tra trong Unity:**
- Click ▶ mở Dage_Anim01.aseprite
- Xem danh sách animation clips

---

## 📋 BƯỚC 2: SỬ DỤNG ANIMATIONS TRONG SCENE (5 phút)

### 2.1 Thêm vào Player GameObject

1. **Hierarchy → Chọn Player** (trong scene Aethon)
2. **Inspector → Animator component**
3. **Kiểm tra Controller:** `Player.controller`

### 2.2 Tạo Animator Controller mới (nếu cần clean start)

**Nếu muốn bắt đầu từ đầu:**

1. **Project → Assets/Animation/**
2. **Right-click → Create → Animator Controller**
3. **Đặt tên:** `Dage_Animator`
4. **Player GameObject → Animator → Controller:** Kéo `Dage_Animator` vào

### 2.3 Thêm Animation States

1. **Window → Animation → Animator (Ctrl+7)**
2. **Từ Dage_Anim01.aseprite, kéo animation clips vào Animator:**

```
Ví dụ nếu có các animations:
- Dage_Anim01_Idle → Kéo vào làm Idle state
- Dage_Anim01_Walk → Kéo vào làm Walk state  
- Dage_Anim01_Attack → Kéo vào làm Attack state
- Dage_Anim01_Death → Kéo vào làm Death state
```

3. **Right-click Idle state → Set as Layer Default State** (màu cam)

---

## 📋 BƯỚC 3: TẠO PARAMETERS & TRANSITIONS (5 phút)

### 3.1 Tạo Parameters

**Animator window → Tab Parameters → Nhấn "+":**

```
1. Float: Speed = 0
2. Bool: IsDead = false
3. Trigger: TakeDamage
4. Bool: IsRolling = false (hoặc IsAttacking)
```

### 3.2 Tạo Transitions cơ bản

#### **Idle ↔ Walk/Run:**

**Idle → Walk:**
```
Right-click Idle → Make Transition → Walk
Inspector:
  Has Exit Time: ✗
  Transition Duration: 0.1
  Conditions: Speed > 0.1
```

**Walk → Idle:**
```
Right-click Walk → Make Transition → Idle
Inspector:
  Has Exit Time: ✗
  Transition Duration: 0.15
  Conditions: Speed < 0.1
```

#### **Any State → Attack/Damage:**

**Nếu có Attack animation:**
```
Any State → Attack
  Conditions: TakeDamage (trigger)
  Has Exit Time: ✗
  Transition Duration: 0

Attack → Idle
  Has Exit Time: ✓ (Exit Time: 1.0)
  Transition Duration: 0.1
```

#### **Any State → Death:**

```
Any State → Death
  Conditions: IsDead = true
  Has Exit Time: ✗
  Transition Duration: 0.2
  
(Không tạo transition ra khỏi Death)
```

---

## 📋 BƯỚC 4: VERIFY SCRIPT CONNECTIONS (1 phút)

### 4.1 Kiểm tra PlayerAnimationController

1. **Hierarchy → Player**
2. **Inspector → PlayerAnimationController component**
3. **Auto-assign sẽ điền:**
   - Animator: Player (Animator)
   - Player Controller: Player (PlayerController2D)
   - Rb: Player (Rigidbody2D)

4. **Nếu Missing → Click icon ⊙ → Chọn component đúng**

### 4.2 Test Script đã attach?

**Kiểm tra Player có:**
- ✅ Animator
- ✅ Rigidbody2D  
- ✅ Box Collider 2D
- ✅ PlayerController2D script
- ✅ PlayerAnimationController script

**Nếu thiếu AnimationTester:**
- **Add Component → AnimationTester** (để test)

---

## 📋 BƯỚC 5: TEST & VERIFY (2 phút)

### 5.1 Play Mode Test

1. **Nhấn Play ▶ (Ctrl+P)**
2. **Test di chuyển:**
   - WASD → Walk animation
   - Đứng yên → Idle animation
   - Space/Shift → Dash/Attack animation (nếu có)

### 5.2 Test với AnimationTester

**Nếu đã attach AnimationTester script:**
- **Nhấn T** → Trigger damage animation
- **Nhấn Y** → Trigger death animation
- **Nhấn R** → Reset (revive)

### 5.3 Kiểm tra Parameters Real-time

1. **Play mode**
2. **Window → Animator**
3. **Tab Parameters** → xem giá trị:
   - Speed: 0 → ~5 khi di chuyển
   - IsRolling: true khi dash
   - IsDead: true khi chết

---

## 🎯 CHECKLIST HOÀN THÀNH

### Aseprite Import:
- [ ] Dage_Anim01.aseprite có Generate Animation Clips = ✓
- [ ] Đã apply và thấy animation clips được generate
- [ ] Filter Mode = Point (no filter)
- [ ] Pixels Per Unit = 16 hoặc 100

### Animator Setup:
- [ ] Animation clips từ Aseprite đã kéo vào Animator
- [ ] Default state đã set (màu cam)
- [ ] 4 Parameters đã tạo (Speed, IsDead, TakeDamage, IsRolling)
- [ ] Transitions Idle ↔ Walk với conditions Speed
- [ ] Transition Any State → Death với IsDead

### Scene Setup:
- [ ] Player có Animator component
- [ ] Animator Controller đã gán
- [ ] PlayerAnimationController references đã điền
- [ ] AnimationTester attached (optional, cho test)

### Testing:
- [ ] Play mode: Idle animation loop
- [ ] WASD: Walk animation smooth
- [ ] Thả phím: Walk → Idle mượt
- [ ] Test keys T/Y/R hoạt động (nếu có AnimationTester)

---

## 💡 TIPS VỚI ASEPRITE FILES

### Nếu cần chỉnh sửa animations:

1. **Mở file .aseprite trong Aseprite app**
2. **Chỉnh sửa frames, tags, layers**
3. **Save**
4. **Unity tự động re-import**
5. **Animation clips tự động update**

### Nếu không thấy animation clips generate:

**Kiểm tra:**
1. Unity Aseprite Importer đã cài? (Package Manager)
2. Generate Animation Clips = ✓ ?
3. File .aseprite có tags không? (cần tags để tạo clips)
4. Thử: Right-click .aseprite → Reimport

### Nếu muốn tách animations thủ công:

Bạn vẫn có thể tạo animation clips bằng tay từ sprites:
1. **Click ▶ bên cạnh Dage_Anim01.aseprite**
2. **Thấy danh sách sprites (Frame_0, Frame_1...)**
3. **Window → Animation → Animation**
4. **Create New Clip**
5. **Kéo sprites theo range:**
   - Idle: Frame_0 đến Frame_3
   - Walk: Frame_4 đến Frame_9
   - v.v...

---

## 🔧 TROUBLESHOOTING

| Vấn đề | Giải pháp |
|--------|-----------|
| Không thấy animation clips | Tích "Generate Animation Clips" → Apply |
| Sprites bị mờ | Filter Mode = Point (no filter) |
| Animation không chạy | Kiểm tra Animator enabled, Controller gán |
| Chuyển đổi giật | Tăng Transition Duration lên 0.2 |
| File Aseprite không import | Cài Unity Aseprite Importer (Package Manager) |

---

## 📚 FILES LIÊN QUAN

**Scripts:**
- `Assets/Scripts/Player/PlayerAnimationController.cs` ✅ Hoàn chỉnh
- `Assets/Scripts/Player/AnimationTester.cs` ⭕ Optional (test)

**Aseprite:**
- `Assets/Art/MungeonDage/Dage/aseprite files/Dage_Anim01.aseprite`
- `Assets/Art/MungeonDage/Dage/aseprite files/Dage_Anim02.aseprite`

**Scene:**
- `Assets/Settings/Scenes/Aethon.unity`

**Animator:**
- `Assets/Animation/Player.controller` (cũ, có thể dùng lại)
- `Assets/Animation/Dage_Animator.controller` (tạo mới nếu muốn)

---

## ⏱️ THỜI GIAN ƯỚC TÍNH

- **Kiểm tra Import Settings:** 2 phút
- **Setup Animator:** 5 phút
- **Tạo Parameters & Transitions:** 5 phút
- **Test & Verify:** 3 phút

**TỔNG: ~15 phút** 🎉

---

## ✨ LỢI ÍCH CỦA ASEPRITE WORKFLOW

✅ **Không cần tạo animation clips thủ công**  
✅ **Chỉnh sửa trong Aseprite → Unity auto-update**  
✅ **Tags trong Aseprite = Animation clips trong Unity**  
✅ **Dễ dàng thay đổi timing, frames**  
✅ **Workflow nhanh cho pixel art animation**

---

**Bắt đầu với BƯỚC 1 ngay! 🚀**

**Nếu gặp vấn đề với Aseprite import → Đọc file DAGE_ANIMATION_SETUP_GUIDE.md để tạo thủ công**
