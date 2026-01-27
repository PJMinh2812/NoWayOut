# 🎬 HƯỚNG DẪN THIẾT LẬP ANIMATION CHO DAGE - SCENE AETHON

## ✅ KIỂM TRA HIỆN TRẠNG

**Đã có sẵn:**
- ✅ Scene Aethon với Player GameObject
- ✅ PlayerAnimationController.cs script (hoàn chỉnh)
- ✅ PlayerController2D.cs với IsRolling property
- ✅ Animator component trên Player
- ✅ Sprites Dage đầy đủ trong `Assets/Art/MungeonDage/Dage/anim/`
- ✅ Folder `Assets/Animation/Dage/` đã tạo

**Cần bổ sung:**
- ❌ Animation Clips cho Dage (Idle, Walk, Damage, Death, Dash)
- ❌ Cấu hình Animator Controller Parameters
- ❌ Transitions giữa các states
- ❌ Gán references trong scene

---

## 📋 PHẦN 1: CHUẨN BỊ SPRITES

### Bước 1.1: Kiểm tra Import Settings của Sprites

1. **Mở Unity Editor**
2. **Project → Assets/Art/MungeonDage/Dage/anim/**
3. **Chọn TẤT CẢ sprites .png** (Ctrl + A)
4. **Inspector → kiểm tra settings:**

```
Texture Type: Sprite (2D and UI)
Sprite Mode: Single
Pixels Per Unit: 16
Filter Mode: Point (no filter) ← QUAN TRỌNG!
Compression: None
Max Size: 2048
```

5. Nếu chưa đúng → **Sửa lại → Apply**

### Bước 1.2: Kiểm tra sprites trong folder dash/

1. **Assets/Art/MungeonDage/Dage/anim/dash/**
2. **Chọn tất cả Dash sprites**
3. **Kiểm tra Import Settings tương tự**

---

## 📋 PHẦN 2: TẠO ANIMATION CLIPS

### Bước 2.1: Mở scene Aethon

1. **Project → Assets/Settings/Scenes/**
2. **Double-click Aethon.unity**
3. **Hierarchy → Chọn "Player" GameObject**

### Bước 2.2: Tạo Animation IDLE

1. **Window → Animation → Animation** (Ctrl + 6)
2. Đảm bảo Player đang được chọn trong Hierarchy
3. **Trong Animation window:**
   - Nếu có animation cũ → Click dropdown → **Create New Clip**
   - Nếu chưa có → Click **Create**
4. **Save vào:** `Assets/Animation/Dage/Dage_Idle.anim`

5. **Thêm sprites:**
   - **Project → Assets/Art/MungeonDage/Dage/anim/**
   - **Chọn 4 sprites Idle** (giữ Ctrl):
     ```
     Dage_Idle01.png
     Dage_Idle02.png
     Dage_Idle03.png
     Dage_Idle04.png
     ```
   - **Kéo vào timeline** (dòng Sprite Renderer trong Animation window)

6. **Điều chỉnh timing:**
   - **Sample Rate**: Đổi thành `8` (góc trên Animation window)
   - Animation sẽ tự động điều chỉnh keyframes

7. **Kiểm tra settings:**
   - **Project → Click vào Dage_Idle.anim**
   - **Inspector → Tích ✓ vào "Loop Time"**

### Bước 2.3: Tạo Animation WALK

1. **Animation window → Dropdown → Create New Clip**
2. **Save:** `Assets/Animation/Dage/Dage_Walk.anim`
3. **Project → Chọn 6 sprites Walk:**
   ```
   Dage_Walk1.png
   Dage_Walk2.png
   Dage_Walk3.png
   Dage_Walk4.png
   Dage_Walk5.png
   Dage_Walk6.png
   ```
4. **Kéo vào timeline**
5. **Sample Rate**: `12` (nhanh hơn idle)
6. **Loop Time**: Tích ✓

### Bước 2.4: Tạo Animation DAMAGE

1. **Create New Clip → Dage_Damage.anim**
2. **Chọn 3 sprites:**
   ```
   Dage_Dmg1.png
   Dage_Dmg2.png
   Dage_Dmg3.png
   ```
3. **Kéo vào timeline**
4. **Sample Rate**: `20` (animation nhanh, hiệu ứng sốc)
5. **Loop Time**: BỎ TÍCH ✗ (chỉ chạy 1 lần)

### Bước 2.5: Tạo Animation DEATH

1. **Create New Clip → Dage_Death.anim**
2. **Chọn 6 sprites:**
   ```
   Dage_Death1.png
   Dage_Death2.png
   Dage_Death3.png
   Dage_Death4.png
   Dage_Death5.png
   Dage_Death6.png
   ```
3. **Kéo vào timeline**
4. **Sample Rate**: `10`
5. **Loop Time**: BỎ TÍCH ✗

### Bước 2.6: Tạo Animation DASH (Rolling)

1. **Create New Clip → Dage_Dash.anim**
2. **Assets/Art/MungeonDage/Dage/anim/dash/**
3. **Chọn 5 sprites** (bắt đầu với right - hướng mặc định):
   ```
   Dage_Dash_right1.png
   Dage_Dash_right2.png
   Dage_Dash_right3.png
   Dage_Dash_right4.png
   Dage_Dash_right5.png
   ```
4. **Kéo vào timeline**
5. **Sample Rate**: `24` (rất nhanh)
6. **Loop Time**: BỎ TÍCH ✗

> **Lưu ý:** Dash có 8 hướng. Hiện tại tạo 1 animation cho hướng right trước.
> Nếu muốn dash 8 hướng → cần tạo 8 animations riêng + Blend Tree (nâng cao).

---

## 📋 PHẦN 3: CẤU HÌNH ANIMATOR CONTROLLER

### Bước 3.1: Mở Animator

1. **Window → Animation → Animator** (Ctrl + 7)
2. **Hierarchy → Chọn Player**
3. **Animator window sẽ hiện Player.controller**

### Bước 3.2: Làm sạch States cũ

Bạn sẽ thấy các states cũ: Idle, walk_O, damage, death, spell01Idle

**XÓA tất cả states cũ:**
1. **Click vào từng state cũ → Delete**
2. Giữ lại: **Entry**, **Any State**, **Exit**

### Bước 3.3: Thêm Animation States mới

1. **Kéo các animation clips vào Animator:**
   - **Dage_Idle.anim** → Kéo vào Animator window
   - **Dage_Walk.anim** → Kéo vào
   - **Dage_Damage.anim** → Kéo vào
   - **Dage_Death.anim** → Kéo vào
   - **Dage_Dash.anim** → Kéo vào

2. **Set Default State:**
   - **Right-click Dage_Idle → Set as Layer Default State**
   - State sẽ có màu **cam**

### Bước 3.4: Tạo Parameters

**Trong tab Parameters (góc trái Animator):**

1. **Nhấn "+" → Float**
   - Name: `Speed`
   - Default: `0`

2. **Nhấn "+" → Bool**
   - Name: `IsDead`
   - Default: `false`

3. **Nhấn "+" → Trigger**
   - Name: `TakeDamage`

4. **Nhấn "+" → Bool**
   - Name: `IsRolling`
   - Default: `false`

**Kết quả:**
```
Parameters:
├─ Speed (Float) = 0
├─ IsDead (Bool) = false
├─ TakeDamage (Trigger)
└─ IsRolling (Bool) = false
```

### Bước 3.5: Tạo Transitions

#### **A. Idle ↔ Walk (Di chuyển bình thường)**

**Idle → Walk:**
1. **Right-click Dage_Idle** → **Make Transition**
2. **Click vào Dage_Walk**
3. **Click vào transition (mũi tên trắng)**
4. **Inspector:**
   ```
   Has Exit Time: BỎ TÍCH ✗
   Transition Duration: 0.1
   Interruption Source: Current State
   ```
5. **Conditions (góc dưới):**
   - **Nhấn "+"**
   - **Speed** → **Greater** → `0.1`

**Walk → Idle:**
1. **Right-click Dage_Walk → Make Transition → Dage_Idle**
2. **Inspector:**
   ```
   Has Exit Time: BỎ TÍCH ✗
   Transition Duration: 0.15
   ```
3. **Conditions:**
   - **Speed** → **Less** → `0.1`

#### **B. Idle/Walk → Dash (Rolling)**

**Dage_Idle → Dage_Dash:**
1. **Right-click Dage_Idle → Make Transition → Dage_Dash**
2. **Inspector:**
   ```
   Has Exit Time: BỎ TÍCH ✗
   Transition Duration: 0.05
   ```
3. **Conditions:**
   - **IsRolling** → **true**

**Dage_Walk → Dage_Dash:**
1. **Right-click Dage_Walk → Make Transition → Dage_Dash**
2. **Inspector:**
   ```
   Has Exit Time: BỎ TÍCH ✗
   Transition Duration: 0.05
   ```
3. **Conditions:**
   - **IsRolling** → **true**

**Dage_Dash → Dage_Idle:**
1. **Right-click Dage_Dash → Make Transition → Dage_Idle**
2. **Inspector:**
   ```
   Has Exit Time: TÍCH ✓
   Exit Time: 0.95
   Transition Duration: 0.1
   ```
3. **Conditions:**
   - KHÔNG CẦN (chuyển tự động khi animation hết)

#### **C. Any State → Damage**

1. **Right-click Any State → Make Transition → Dage_Damage**
2. **Inspector:**
   ```
   Has Exit Time: BỎ TÍCH ✗
   Transition Duration: 0
   Interruption Source: None
   ```
3. **Conditions:**
   - **TakeDamage** (trigger - tự động hiện)

**Dage_Damage → Dage_Idle:**
1. **Right-click Dage_Damage → Make Transition → Dage_Idle**
2. **Inspector:**
   ```
   Has Exit Time: TÍCH ✓
   Exit Time: 1.0
   Transition Duration: 0.1
   ```
3. **NO CONDITIONS**

#### **D. Any State → Death**

1. **Right-click Any State → Make Transition → Dage_Death**
2. **Inspector:**
   ```
   Has Exit Time: BỎ TÍCH ✗
   Transition Duration: 0.2
   ```
3. **Conditions:**
   - **IsDead** → **true**

**KHÔNG TẠO transition từ Dage_Death ra** (chết rồi không sống lại!)

---

## 📋 PHẦN 4: KIỂM TRA REFERENCES TRONG SCENE

### Bước 4.1: Kiểm tra Player GameObject

1. **Hierarchy → Chọn Player**
2. **Inspector → PlayerAnimationController component**
3. **Kiểm tra references:**
   ```
   Animator: Player (Animator) ← tự động
   Player Controller: Player (PlayerController2D) ← tự động
   Rb: Player (Rigidbody2D) ← tự động
   Speed Threshold: 0.1
   ```

4. Nếu bị `None (Missing)`:
   - **Click icon⊙ bên cạnh field**
   - **Chọn component đúng từ danh sách**

### Bước 4.2: Kiểm tra Animator Component

1. **Inspector → Animator component**
2. **Kiểm tra:**
   ```
   Controller: Player ← Animator Controller
   Avatar: None (2D không cần)
   Update Mode: Normal
   Culling Mode: Always Animate
   ```

### Bước 4.3: Save Scene

**File → Save (Ctrl + S)**

---

## 📋 PHẦN 5: TEST ANIMATION

### Bước 5.1: Test trong Play Mode

1. **Nhấn Play ▶ (Ctrl + P)**
2. **Di chuyển bằng WASD hoặc Arrow keys**

**Kiểm tra:**
- ✅ Đứng yên → Idle animation loop
- ✅ Di chuyển → Walk animation mượt mà
- ✅ Thả phím → Walk → Idle mượt
- ✅ Nhấn Space/Shift → Dash animation

### Bước 5.2: Test Damage Animation

**Tạo test script tạm:**

1. **Mở PlayerAnimationController.cs**
2. **Thêm vào Update():**
   ```csharp
   private void Update()
   {
       UpdateAnimations();
       
       // TEST DAMAGE - Remove sau khi test xong
       if (Input.GetKeyDown(KeyCode.T))
       {
           TriggerDamage();
       }
       
       // TEST DEATH
       if (Input.GetKeyDown(KeyCode.Y))
       {
           TriggerDeath();
       }
   }
   ```

3. **Play mode → Nhấn T** → Damage animation chạy
4. **Nhấn Y** → Death animation chạy

### Bước 5.3: Kiểm tra Animator Parameters

1. **Play mode**
2. **Window → Animation → Animator**
3. **Tab Parameters** → xem giá trị real-time:
   - **Speed**: thay đổi khi di chuyển (0 → 5)
   - **IsRolling**: true khi dash
   - **TakeDamage**: flash khi trigger
   - **IsDead**: true khi chết

---

## 🎯 PHẦN 6: TỐI ƯU VÀ ĐIỀU CHỈNH

### Nếu animation KHÔNG mượt:

**Vấn đề: Chuyển đổi giật, không smooth**
- ✅ **Tăng Transition Duration** lên 0.2 - 0.3
- ✅ Kiểm tra **Has Exit Time** (bỏ tích cho transitions nhanh)

**Vấn đề: Animation chạy quá chậm/nhanh**
- ✅ Điều chỉnh **Sample Rate** trong animation clip
  - Idle: 6-8 fps
  - Walk: 10-12 fps
  - Dash: 20-24 fps
  - Damage: 18-20 fps

**Vấn đề: Nhân vật không lật hướng**
- ✅ Kiểm tra **PlayerSpriteController.cs** đã attach chưa
- ✅ Rotation Mode = **FlipOnly**

### Điều chỉnh Collider

1. **Chọn Player → Box Collider 2D**
2. **Edit Collider** (nút Edit ở Inspector)
3. **Điều chỉnh size/offset** cho vừa sprite

---

## ✅ CHECKLIST HOÀN THÀNH

**Sprites:**
- [ ] Import settings đúng (Point filter, Single mode)
- [ ] Tất cả sprites trong anim/ và dash/ đã sẵn sàng

**Animation Clips:**
- [ ] Dage_Idle.anim (4 frames, 8 fps, Loop)
- [ ] Dage_Walk.anim (6 frames, 12 fps, Loop)
- [ ] Dage_Damage.anim (3 frames, 20 fps, No Loop)
- [ ] Dage_Death.anim (6 frames, 10 fps, No Loop)
- [ ] Dage_Dash.anim (5 frames, 24 fps, No Loop)

**Animator Controller:**
- [ ] 5 States đã tạo và gán clips
- [ ] Dage_Idle là Default State (màu cam)
- [ ] 4 Parameters: Speed, IsDead, TakeDamage, IsRolling
- [ ] Transitions Idle ↔ Walk với conditions Speed
- [ ] Transitions Idle/Walk → Dash với IsRolling
- [ ] Transition Any State → Damage với TakeDamage
- [ ] Transition Any State → Death với IsDead
- [ ] Has Exit Time và Duration đã set đúng

**Scene Setup:**
- [ ] Player trong scene Aethon có Animator component
- [ ] Animator Controller = Player.controller
- [ ] PlayerAnimationController script đã attach
- [ ] References đã tự động assign

**Testing:**
- [ ] Play mode: Idle animation loop
- [ ] WASD di chuyển: Walk mượt mà
- [ ] Thả phím: Walk → Idle smooth
- [ ] Space/Shift: Dash animation chạy
- [ ] Press T: Damage animation (test)
- [ ] Press Y: Death animation (test)
- [ ] Parameters thay đổi real-time trong Animator

---

## 🚀 BƯỚC TIẾP THEO (TÙY CHỌN)

### 1. Animation 8 hướng cho Dash

Nếu muốn dash chính xác theo hướng:

**Tạo Blend Tree:**
1. **Animator → Right-click → Create State → From New Blend Tree**
2. **Đổi tên: Dash_8Dir**
3. **Double-click vào Blend Tree**
4. **Blend Type: 2D Simple Directional**
5. **Parameters: Horizontal, Vertical**
6. **Add Motion cho 8 hướng:**
   - Up: Dage_Dash_up
   - Down: Dage_Dash_Down
   - Left: Dage_Dash_left
   - Right: Dage_Dash_right
   - (tạo 4 animations nữa cho các góc)

### 2. Spell Animations

**Sprites sẵn có:**
- spell01fire (7 frames)
- spell02fire (6 frames)
- spell03fire (8 frames)

**Tạo tương tự như Damage:**
1. Create New Clip → Dage_Spell01.anim
2. Kéo sprites spell01fire1-7
3. Sample Rate: 15-18
4. Transition từ Idle với trigger `CastSpell`

### 3. Staff Animations

**Sprites:**
- Dage_staffIdle (4 frames)
- Dage_staffFire (7 frames)

**Use case:** Khi player cầm staff/weapon khác

### 4. Animation Events

**Thêm sound effects:**
1. Animation window → Chọn frame
2. Event button → Add Event
3. Function: PlayFootstepSound, PlayDamageSound...
4. Implement trong PlayerAnimationController.cs

---

## 💡 TROUBLESHOOTING

### Animation không chạy
- ✅ Kiểm tra Animator enabled
- ✅ Kiểm tra Controller đã gán
- ✅ Kiểm tra Parameters có đúng tên không

### Sprite nhấp nháy
- ✅ Kiểm tra Transition Duration (tăng lên)
- ✅ Xóa duplicate transitions

### Không lật hướng
- ✅ Attach PlayerSpriteController.cs
- ✅ Set Rotation Mode = FlipOnly

### Animation lag
- ✅ Giảm Sample Rate
- ✅ Compression = None trong sprite settings

---

## 📚 TÀI LIỆU THAM KHẢO

**Sprites location:**
- `Assets/Art/MungeonDage/Dage/anim/` - Main animations
- `Assets/Art/MungeonDage/Dage/anim/dash/` - Dash 8 directions

**Scripts location:**
- `Assets/Scripts/Player/PlayerAnimationController.cs`
- `Assets/Scripts/PlayerController2D.cs`
- `Assets/Scripts/PlayerSpriteController.cs`

**Animation files:**
- `Assets/Animation/Dage/` - Dage animation clips
- `Assets/Animation/Player.controller` - Animator Controller

---

**Hoàn thành guide này → Bạn sẽ có hệ thống animation hoàn chỉnh cho nhân vật Dage! 🎉**

**Thời gian ước tính:** 30-45 phút (nếu làm lần đầu)
