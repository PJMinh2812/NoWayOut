# ⚡ QUICK START - TẠO ANIMATION 5 PHÚT

## 🎯 MỤC TIÊU
Tạo animation cơ bản cho nhân vật Dage trong scene Aethon

---

## 📋 3 BƯỚC NHANH

### **BƯỚC 1: TẠO ANIMATION CLIPS (2 phút)**

1. **Unity → Hierarchy → Chọn Player**
2. **Window → Animation → Animation (Ctrl+6)**
3. **Tạo 5 animations:**

| Animation | Sprites | Sample Rate | Loop |
|-----------|---------|-------------|------|
| Dage_Idle | Dage_Idle01-04 | 8 | ✓ |
| Dage_Walk | Dage_Walk1-6 | 12 | ✓ |
| Dage_Damage | Dage_Dmg1-3 | 20 | ✗ |
| Dage_Death | Dage_Death1-6 | 10 | ✗ |
| Dage_Dash | Dage_Dash_right1-5 | 24 | ✗ |

**Cách làm:**
- Create New Clip → Đặt tên
- Kéo sprites từ `Assets/Art/MungeonDage/Dage/anim/` vào timeline
- Đổi Sample Rate
- Save vào `Assets/Animation/Dage/`

---

### **BƯỚC 2: SETUP ANIMATOR (2 phút)**

1. **Window → Animator (Ctrl+7)**
2. **Kéo 5 animation clips vào Animator window**
3. **Right-click Dage_Idle → Set as Default State**

4. **Tab Parameters → Tạo 4 parameters:**
```
+ Float: Speed = 0
+ Bool: IsDead = false
+ Trigger: TakeDamage
+ Bool: IsRolling = false
```

---

### **BƯỚC 3: TẠO TRANSITIONS (1 phút)**

**Kết nối các states:**

```
Idle → Walk
  Condition: Speed > 0.1
  Has Exit Time: ✗
  
Walk → Idle
  Condition: Speed < 0.1
  Has Exit Time: ✗

Idle/Walk → Dash
  Condition: IsRolling = true
  Has Exit Time: ✗

Dash → Idle
  No condition
  Has Exit Time: ✓ (Exit Time: 0.95)

Any State → Damage
  Condition: TakeDamage (trigger)
  Has Exit Time: ✗
  Duration: 0

Damage → Idle
  No condition
  Has Exit Time: ✓ (Exit Time: 1.0)

Any State → Death
  Condition: IsDead = true
  Has Exit Time: ✗
```

---

## ✅ KIỂM TRA

**Nhấn Play ▶:**
- Di chuyển WASD → Walk animation
- Đứng yên → Idle animation
- Space/Shift → Dash animation

**Nếu OK → XONG! 🎉**

---

## 🔧 SỬA LỖI NHANH

| Vấn đề | Giải pháp |
|--------|-----------|
| Animation không chạy | Kiểm tra Animator enabled, Controller gán đúng |
| Chuyển đổi giật | Tăng Transition Duration lên 0.2 |
| Không lật hướng | Thêm PlayerSpriteController, set FlipOnly |
| Sprite nhấp nháy | Giảm Transition Duration, xóa transitions trùng |

---

## 📚 CHI TIẾT HƠN?

Đọc file: **DAGE_ANIMATION_SETUP_GUIDE.md** (hướng dẫn đầy đủ)

---

**Thời gian:** 5-10 phút | **Độ khó:** ⭐⭐☆☆☆
