# 🎯 HƯỚNG DẪN NÂNG CẤP MOVEMENT 8 HƯỚNG VỚI BLEND TREE

## 🎯 MỤC TIÊU

Nâng cấp hệ thống animation từ **1 animation chung** sang **8 hướng di chuyển** sử dụng Blend Tree 2D.

---

## ✅ YÊU CẦU

**Sprites cần có:**

- Walk Down (S) - 6 frames
- Walk Up (W) - 6 frames
- Walk Left (A) - 6 frames
- Walk Right (D) - 6 frames
- Walk DownLeft (S+A)
- Walk DownRight (S+D)
- Walk UpLeft (W+A)
- Walk UpRight (W+D)

**Hiện tại có:**

- ✅ Dage_Walk1-6.png (1 hướng)
- ✅ Dash 8 hướng (có thể tham khảo)

**Cần tạo thêm:**

- ❌ 7 animation walks còn lại HOẶC reuse + flip

---

## 📋 PHƯƠNG ÁN 1: REUSE 2 HƯỚNG (NHANH - 15 phút)

Sử dụng sprite flip để tạo 8 hướng từ 2 animations:

- Walk Side (trái/phải → flip)
- Walk Vertical (lên/xuống)
- Diagonal = blend giữa 2

### Bước 1.1: Tạo Animation Clips

**1. Tạo Dage_Walk_Up.anim**

- Window → Animation → Create New Clip
- Save: `Assets/Animation/Dage/Dage_Walk_Up.anim`
- Kéo sprites: `Dage_Walk1-6.png` (reuse làm up)
- Sample Rate: 12
- Loop Time: ✓

**2. Tạo Dage_Walk_Down.anim**

- Tương tự, reuse sprites Walk1-6
- Sample Rate: 12
- Loop Time: ✓

**3. Tạo Dage_Walk_Right.anim**

- Reuse sprites Walk1-6 (hoặc tìm sprites side nếu có)
- Sample Rate: 12
- Loop Time: ✓

**4. Tạo Dage_Walk_Left.anim**

- Clone Dage_Walk_Right
- Flip bằng script sau

### Bước 1.2: Tạo Blend Tree trong Animator

**1. Mở Animator Controller**

- Window → Animator
- Hierarchy → Select Player
- Animator window → Dage.controller

**2. Xóa state Walk cũ**

- Right-click "Dage_Walk" → Delete

**3. Tạo Blend Tree mới**

- Right-click → Create State → From New Blend Tree
- Đổi tên: `Walk_8Dir`

**4. Double-click vào Walk_8Dir → Configure**

**Inspector → Blend Tree settings:**

```
Blend Type: 2D Simple Directional
Parameters:
  - Horizontal (Float)
  - Vertical (Float)
```

**5. Add Motion Fields (4 hướng chính):**

Click (+) → Add Motion Field 4 lần:

| Motion          | Pos X | Pos Y | Hướng   |
| --------------- | ----- | ----- | ------- |
| Dage_Walk_Up    | 0     | 1     | North ↑ |
| Dage_Walk_Down  | 0     | -1    | South ↓ |
| Dage_Walk_Left  | -1    | 0     | West ←  |
| Dage_Walk_Right | 1     | 0     | East →  |

> **Lưu ý:** Blend Tree tự động blend các góc chéo (NE, NW, SE, SW) từ 4 animations này!

**6. Tương tự cho Idle**

Tạo Blend Tree: `Idle_8Dir`

- Idle_Up, Idle_Down, Idle_Left, Idle_Right
- Hoặc chỉ dùng 1 Idle chung nếu không cần phân hướng

### Bước 1.3: Tạo Parameters trong Animator

**Animator → Parameters tab:**

Thêm 2 parameters mới:

- `Horizontal` (Float) - giá trị -1 to 1
- `Vertical` (Float) - giá trị -1 to 1

Giữ lại:

- `Speed` (Float)
- `IsRolling` (Bool)
- `IsDead` (Bool)
- `TakeDamage` (Trigger)

### Bước 1.4: Setup Transitions

**1. Idle_8Dir ↔ Walk_8Dir**

**Idle_8Dir → Walk_8Dir:**

```
Condition: Speed > 0.1
Has Exit Time: ✗
Transition Duration: 0.1
```

**Walk_8Dir → Idle_8Dir:**

```
Condition: Speed < 0.1
Has Exit Time: ✗
Transition Duration: 0.1
```

**2. Any State → Other animations**

- Death, Damage, Dash giữ nguyên như cũ

### Bước 1.5: Cập Nhật PlayerAnimationController.cs

Mở `Assets/Scripts/Player/PlayerAnimationController.cs`

**Thêm code cập nhật Horizontal/Vertical:**

```csharp
private void UpdateAnimations()
{
    if (animator == null || rb == null) return;

    // Lấy velocity
    Vector2 velocity = rb.linearVelocity;
    float speed = velocity.magnitude;

    // Cập nhật Speed
    animator.SetFloat("Speed", speed);

    // Cập nhật Horizontal/Vertical cho Blend Tree
    if (speed > 0.1f) // Chỉ cập nhật khi đang di chuyển
    {
        Vector2 direction = velocity.normalized;
        animator.SetFloat("Horizontal", direction.x);
        animator.SetFloat("Vertical", direction.y);
    }
    // Khi dừng lại, giữ nguyên hướng cuối cùng (không reset về 0)

    // Cập nhật IsRolling
    if (playerController != null)
    {
        animator.SetBool("IsRolling", playerController.IsRolling);
    }
}
```

**Giải thích:**

- `Horizontal` = -1 (trái) → 1 (phải)
- `Vertical` = -1 (xuống) → 1 (lên)
- Blend Tree sẽ tự động blend animations dựa trên 2 giá trị này

### Bước 1.6: Test

**Play mode (Ctrl+P):**

1. Di chuyển W → Walk_Up animation
2. Di chuyển S → Walk_Down animation
3. Di chuyển A → Walk_Left animation
4. Di chuyển D → Walk_Right animation
5. Di chuyển W+A → Blend giữa Walk_Up và Walk_Left
6. Dừng lại → Idle với hướng tương ứng

---

## 📋 PHƯƠNG ÁN 2: TẠO FULL 8 ANIMATIONS (CHẤT LƯỢNG CAO - 45 phút)

Nếu có sprites riêng cho 8 hướng (từ Aseprite hoặc vẽ thêm):

### Bước 2.1: Tạo 8 Animation Clips

Tạo lần lượt:

- Dage_Walk_N.anim (North ↑)
- Dage_Walk_NE.anim (NorthEast ↗)
- Dage_Walk_E.anim (East →)
- Dage_Walk_SE.anim (SouthEast ↘)
- Dage_Walk_S.anim (South ↓)
- Dage_Walk_SW.anim (SouthWest ↙)
- Dage_Walk_W.anim (West ←)
- Dage_Walk_NW.anim (NorthWest ↖)

### Bước 2.2: Blend Tree với 8 Motion Fields

**Inspector → Blend Tree:**

```
Blend Type: 2D Simple Directional
```

| Motion  | Pos X  | Pos Y  | Góc   |
| ------- | ------ | ------ | ----- |
| Walk_N  | 0      | 1      | 90°   |
| Walk_NE | 0.707  | 0.707  | 45°   |
| Walk_E  | 1      | 0      | 0°    |
| Walk_SE | 0.707  | -0.707 | -45°  |
| Walk_S  | 0      | -1     | -90°  |
| Walk_SW | -0.707 | -0.707 | -135° |
| Walk_W  | -1     | 0      | 180°  |
| Walk_NW | -0.707 | 0.707  | 135°  |

> **Lưu ý:** 0.707 = sin(45°) = cos(45°)

**Code PlayerAnimationController giữ nguyên như Phương án 1.**

---

## 📋 PHƯƠNG ÁN 3: SỬ DỤNG SPRITES DASH 8 HƯỚNG CHO WALK (TẠM THỜI)

Nếu chưa có sprites Walk 8 hướng, có thể:

**1. Reuse Dash sprites làm Walk tạm:**

- `Dage_Dash_up` → Dage_Walk_Up
- `Dage_Dash_Down` → Dage_Walk_Down
- `Dage_Dash_left` → Dage_Walk_Left
- `Dage_Dash_right` → Dage_Walk_Right
- Tương tự cho 4 góc chéo

**2. Adjust Sample Rate:**

- Dash: 20-24 FPS (nhanh)
- Walk: 10-12 FPS (chậm hơn)

---

## 🎯 KHUYẾN NGHỊ

**Cho dự án hiện tại:**
→ **Sử dụng PHƯƠNG ÁN 1** (Reuse + Flip)

**Lý do:**

- ✅ Nhanh nhất (15 phút)
- ✅ Sprites hiện có đã đủ
- ✅ Blend Tree tự động xử lý 8 hướng
- ✅ Tiết kiệm dung lượng

**Nâng cấp sau:**

- Khi có budget → vẽ thêm sprites 8 hướng riêng
- Hoặc dùng tool như Aseprite để tạo variations

---

## ✅ CHECKLIST HOÀN THÀNH

**Blend Tree Setup:**

- [ ] Tạo 4 Walk animations (Up/Down/Left/Right)
- [ ] Tạo Blend Tree Walk_8Dir
- [ ] Add 4 Motion Fields với đúng vị trí
- [ ] Set Blend Type = 2D Simple Directional
- [ ] Tạo Parameters: Horizontal, Vertical

**Animator:**

- [ ] Xóa Walk state cũ
- [ ] Walk_8Dir làm state chính
- [ ] Transitions Idle ↔ Walk_8Dir
- [ ] Test trong Animator window (manual slider)

**Code:**

- [ ] Update PlayerAnimationController.cs
- [ ] Set Horizontal/Vertical dựa trên velocity
- [ ] Test trong Play mode

**Testing:**

- [ ] Walk lên (W) → animation đúng hướng
- [ ] Walk xuống (S) → animation đúng hướng
- [ ] Walk trái (A) → animation đúng hướng + flip
- [ ] Walk phải (D) → animation đúng hướng
- [ ] Walk chéo (W+A, W+D, S+A, S+D) → blend mượt
- [ ] Idle giữ hướng cuối cùng

---

## 📚 TÀI LIỆU THAM KHẢO

**Unity Blend Trees:**

- [Unity Manual - Blend Trees](https://docs.unity3d.com/Manual/class-BlendTree.html)
- [2D Simple Directional](https://docs.unity3d.com/Manual/BlendTree-2DBlending.html)

**Sprites location:**

- Current Walk: `Assets/Art/MungeonDage/Dage/anim/Dage_Walk1-6.png`
- Dash 8 dir: `Assets/Art/MungeonDage/Dage/anim/dash/`

**Scripts:**

- `Assets/Scripts/Player/PlayerAnimationController.cs`
- `Assets/Scripts/Player/PlayerController2D.cs`

---

**Hoàn thành guide này → Movement animation 8 hướng mượt mà! 🎉**
