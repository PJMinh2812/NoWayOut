# 🎮 SOUL KNIGHT CLONE - UNITY PROJECT

## 📋 Tổng quan

Dự án game 2D Top-down Rogue-like lấy cảm hứng từ Soul Knight, được xây dựng với Unity sử dụng kiến trúc modular và các best practices.

---

## 📚 HƯỚNG DẪN CHI TIẾT THEO GIAI ĐOẠN

| Giai đoạn | Nội dung | Trạng thái | Link |
|-----------|----------|------------|------|
| **1** | Player & Weapon System | ✅ Hoàn thành | **[Xem hướng dẫn chi tiết](docs/PHASE_1_SETUP_GUIDE.md)** |
| **2** | Dungeon Generation | 🚧 Sẵn sàng | [Xem template](docs/PHASE_2_DUNGEON_GENERATION.md) |
| **3** | Enemy AI System | 🚧 Sẵn sàng | [Xem template](docs/PHASE_3_ENEMY_AI.md) |
| **4** | UI/UX System | 🚧 Sẵn sàng | [Xem template](docs/PHASE_4_UI_SYSTEM.md) |

> 💡 **Khuyến nghị**: Làm theo thứ tự từ Giai đoạn 1 → 4 để đảm bảo các dependency được setup đúng.

---

## ✅ GIAI ĐOẠN 1 - ĐÃ HOÀN THÀNH

### 🎯 Core Systems

- **GameManager**: Singleton quản lý trạng thái game, scene loading
- **ObjectPooler**: Hệ thống pooling tối ưu cho bullets và effects
- **GameConstants**: Enums và constants cho toàn bộ game
- **CameraShaker**: Utility rung màn hình với Cinemachine

### 👤 Player System

- **PlayerController**: Di chuyển 8 hướng mượt mà với Rigidbody2D
  - Dash có I-frames và cooldown
  - Sprite flip theo hướng chuột
  - Tích hợp Input System mới

- **PlayerStats**: Quản lý Health, Armor (tự hồi), Energy
  - Armor regeneration sau khi không nhận damage
  - Energy regeneration liên tục
  - Event system cho UI updates

- **PlayerInputHandler**: Bridge giữa Input System và Controllers

### 🔫 Weapon System

- **WeaponData (ScriptableObject)**:
  - Damage, Fire Rate, Accuracy
  - Energy Cost, Bullet Speed
  - Automatic/Semi-automatic mode
  - Recoil, Screen Shake intensity

- **WeaponController**:
  - Xoay 360° theo chuột
  - Fire rate control
  - Energy consumption
  - Shotgun spread pattern support

### 💥 Projectile System

- **Projectile**:
  - Object Pooling integration
  - Auto-destroy on collision/lifetime
  - Damage calculation
  - Hit effects

---

## � QUICK START (Giai đoạn 1)

> **📖 Hướng dẫn từng bước chi tiết**: [docs/PHASE_1_SETUP_GUIDE.md](docs/PHASE_1_SETUP_GUIDE.md)

Dưới đây là tóm tắt nhanh, xem file hướng dẫn để biết chi tiết:

### 1️⃣ Cài đặt Packages cần thiết

Mở **Window > Package Manager** và cài:

- ✅ **Input System** (com.unity.inputsystem)
- ✅ **Cinemachine** (com.unity.cinemachine)
- ✅ **2D Sprite** (com.unity.2d.sprite)
- ✅ **Universal Render Pipeline (URP)** (đã có)

### 2️⃣ Tạo Input Actions Asset

1. Right-click trong **Assets/** → **Create > Input Actions**
2. Đặt tên: `InputSystem_Actions` (đã có file này)
3. Mở và tạo các Actions:

**Action Map: "Player"**

```
- Move (Value, Vector2) → Binding: WASD, Left Stick
- Dash (Button) → Binding: Space, South Button (A/Cross)
- Fire (Button) → Binding: Left Mouse, Right Trigger
```

4. Click **Generate C# Class** (tùy chọn)
5. **Save Asset**

### 3️⃣ Setup Layers & Tags

#### Tags (Edit > Project Settings > Tags and Layers)

```
- Player
- Enemy
- Wall
- Bullet
```

#### Layers

```
- Layer 6: Player
- Layer 7: Enemy
- Layer 8: Projectile
- Layer 9: Wall
```

#### Physics 2D Collision Matrix (Edit > Project Settings > Physics 2D)

Bỏ tick các tương tác không cần thiết:

- ❌ Player vs Player
- ❌ Enemy vs Enemy
- ❌ Projectile vs Projectile
- ✅ Projectile vs Enemy
- ✅ Projectile vs Player
- ✅ Projectile vs Wall

### 4️⃣ Setup Sprite Sorting (Y-Sorting)

**Edit > Project Settings > Graphics:**

- **Transparency Sort Mode**: Custom Axis
- **Transparency Sort Axis**: `X: 0, Y: 1, Z: 0`

### 5️⃣ Tạo Player GameObject

**Hierarchy Structure:**

```
Player (Empty GameObject)
├─ PlayerSprite (SpriteRenderer)
│   └─ Attach sprite nhân vật
├─ WeaponPivot (Empty GameObject)
│   ├─ FirePoint (Empty GameObject) - vị trí spawn đạn
│   └─ WeaponSprite (SpriteRenderer)
└─ DashTrail (TrailRenderer)
```

**Components trên Player:**

- Rigidbody2D (Gravity Scale = 0, Freeze Rotation)
- Collider2D (Circle hoặc Capsule)
- PlayerController
- PlayerStats
- PlayerInputHandler
- Player Input (assign Input Actions asset)

**Components trên WeaponPivot:**

- WeaponController
- Audio Source

### 6️⃣ Tạo Bullet Prefab

**Bullet GameObject:**

```
Bullet
├─ BulletSprite (SpriteRenderer)
└─ Trail (TrailRenderer - optional)
```

**Components:**

- Rigidbody2D (Gravity = 0, Continuous Detection)
- Collider2D (Circle, Is Trigger = true)
- Projectile Script

**Tạo 3 variants:**

- BulletPistol
- BulletShotgun
- BulletRifle

### 7️⃣ Setup ObjectPooler

**Tạo GameObject "ObjectPooler" trong scene:**

- Add component: ObjectPooler
- Tạo các Pools:

| Tag           | Prefab        | Size |
| ------------- | ------------- | ---- |
| BulletPistol  | BulletPistol  | 50   |
| BulletShotgun | BulletShotgun | 30   |
| BulletRifle   | BulletRifle   | 100  |

### 8️⃣ Tạo Weapon Data (ScriptableObjects)

**Right-click Assets > Create > Soul Knight > Weapon Data**

**Ví dụ Pistol:**

```yaml
Name: Pistol
Damage: 10
Energy Cost: 5
Fire Rate: 5 (shots/sec)
Is Automatic: false
Bullet Speed: 20
Accuracy: 2
Bullets Per Shot: 1
Bullet Pool Tag: BulletPistol
```

**Ví dụ Shotgun:**

```yaml
Name: Shotgun
Damage: 8
Fire Rate: 1.5
Bullets Per Shot: 5
Spread Angle: 30
Bullet Pool Tag: BulletShotgun
```

### 9️⃣ Setup Camera với Cinemachine

1. **GameObject > Cinemachine > Virtual Camera**
2. **Follow**: kéo Player vào
3. Thêm **CinemachineBasicMultiChannelPerlin** extension:
   - Noise Profile: chọn preset hoặc custom
4. Tạo GameObject "CameraShaker" với script CameraShaker
   - Assign Virtual Camera vào Inspector

---

## 🎮 CÁCH CHƠI

**Điều khiển PC:**

- `WASD`: Di chuyển
- `Mouse`: Nhắm bắn
- `Left Click`: Bắn
- `Space`: Dash

**Điều khiển Controller:**

- `Left Stick`: Di chuyển
- `Right Stick`: Nhắm bắn
- `Right Trigger`: Bắn
- `A/Cross`: Dash

---

## 📊 KIẾN TRÚC CODE

```
Assets/Scripts/
├── Core/
│   ├── GameManager.cs          # Singleton quản lý game state
│   ├── ObjectPooler.cs         # Object pooling system
│   ├── GameConstants.cs        # Constants & Enums
│   └── CameraShaker.cs         # Screen shake utility
│
├── Player/
│   ├── PlayerController.cs     # Movement & Dash
│   ├── PlayerStats.cs          # Health, Armor, Energy
│   └── PlayerInputHandler.cs   # Input bridge
│
└── Weapons/
    ├── WeaponData.cs           # ScriptableObject
    ├── WeaponController.cs     # Weapon rotation & firing
    └── Projectile.cs           # Bullet behavior
```

---

## 🔜 TIẾP THEO: CÁC GIAI ĐOẠN KHÁC

Sau khi hoàn thành Giai đoạn 1 và test kỹ, bạn có thể tiếp tục:

### **📍 Giai đoạn 2: Dungeon Generation** → [Xem template](docs/PHASE_2_DUNGEON_GENERATION.md)
- Procedural tilemap generation với Random Walk
- Room types: Start, Combat, Treasure, Boss
- Door system tự động đóng khi có quái
- Minimap integration

### **🤖 Giai đoạn 3: Enemy AI System** → [Xem template](docs/PHASE_3_ENEMY_AI.md)
- Finite State Machine (FSM)
- States: Idle, Wander, Chase, Attack, Death
- Pathfinding với A* hoặc Simple Follow
- Loot drop system (Health, Energy pickups)

### **🎨 Giai đoạn 4: UI/UX System** → [Xem template](docs/PHASE_4_UI_SYSTEM.md)
- HUD: Health/Armor/Energy bars pixel art
- Weapon icon display
- Damage numbers (floating text)
- Pause menu & Game Over screen

**Để triển khai giai đoạn tiếp theo, hãy nói**:  
→ *"Triển khai Giai đoạn 2: Dungeon Generation"*  
→ *"Triển khai Giai đoạn 3: Enemy AI"*  
→ *"Triển khai Giai đoạn 4: UI System"*

---

## ⚠️ TROUBLESHOOTING

**Lỗi: "Input Actions not found"**
→ Đảm bảo đã Generate C# Class và Save Input Actions asset

**Lỗi: "Bullet pool doesn't exist"**
→ Kiểm tra ObjectPooler có đúng tag và prefab chưa

**Player không flip sprite**
→ Kiểm tra WeaponPivot có đúng localScale trong PlayerController

**Bullet không spawn**
→ Kiểm tra FirePoint position và ObjectPooler có active trong scene

**Camera không shake**
→ Cài Cinemachine package và assign Virtual Camera vào CameraShaker

---

## 📝 CREDITS

- **Unity Version**: 2022.3 LTS hoặc mới hơn
- **Architecture**: Event-driven, ScriptableObject-based
- **Optimization**: Object Pooling, Y-Sorting

**Developed with ❤️ for learning purposes**
