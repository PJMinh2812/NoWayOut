# 🎮 GIAI ĐOẠN 1: PLAYER & WEAPON SYSTEM - HƯỚNG DẪN CHI TIẾT

## 📋 MỤC LỤC
1. [Cài đặt Unity Packages](#step-1-cài-đặt-unity-packages)
2. [Cấu hình Project Settings](#step-2-cấu-hình-project-settings)
3. [Tạo Input Actions](#step-3-tạo-input-actions)
4. [Setup Player GameObject](#step-4-setup-player-gameobject)
5. [Tạo Bullet Prefabs](#step-5-tạo-bullet-prefabs)
6. [Setup Object Pooler](#step-6-setup-object-pooler)
7. [Tạo Weapon Data](#step-7-tạo-weapon-data-scriptableobjects)
8. [Setup Camera System](#step-8-setup-camera-với-cinemachine)
9. [Testing & Debugging](#step-9-testing--debugging)

---

## STEP 1: Cài đặt Unity Packages

### 1.1 Mở Package Manager
1. Trong Unity Editor, chọn **Window > Package Manager**
2. Đảm bảo dropdown góc trên trái đang ở **"Unity Registry"**

### 1.2 Cài đặt Input System
```
1. Tìm "Input System" trong danh sách
2. Click [Install]
3. Khi popup xuất hiện "Enable new input system?", chọn [Yes]
4. Unity sẽ restart
```
⚠️ **Lưu ý**: Sau khi restart, bạn cần import lại project nếu có lỗi.

### 1.3 Cài đặt Cinemachine
```
1. Tìm "Cinemachine" trong Package Manager
2. Click [Install]
3. Đợi import hoàn tất (menu GameObject sẽ có thêm "Cinemachine")
```

### 1.4 Kiểm tra 2D Packages (đã có sẵn)
✅ Kiểm tra các package sau đã được cài:
- **2D Sprite** (com.unity.2d.sprite)
- **2D Tilemap Editor** (com.unity.2d.tilemap)
- **Universal RP** (com.unity.render-pipelines.universal)

---

## STEP 2: Cấu hình Project Settings

### 2.1 Setup Tags
**Edit > Project Settings > Tags and Layers**

**Tags tab**, click `+` để thêm:
```
✅ Player
✅ Enemy
✅ Wall
✅ Bullet
```

### 2.2 Setup Layers
**Layers tab**, gán vào các slot trống:
```
Layer 6: Player
Layer 7: Enemy
Layer 8: Projectile
Layer 9: Wall
Layer 10: Obstacle
```

### 2.3 Cấu hình Physics 2D Collision Matrix
**Edit > Project Settings > Physics 2D**

Scroll xuống **Layer Collision Matrix**, BỎ TICK các ô sau:
```
❌ Player ↔ Player
❌ Enemy ↔ Enemy
❌ Projectile ↔ Projectile
✅ Projectile ↔ Enemy (giữ tick)
✅ Projectile ↔ Player (giữ tick)
✅ Projectile ↔ Wall (giữ tick)
```

**Tại sao?** Ngăn bullets va chạm với nhau, tối ưu hiệu năng.

### 2.4 Cấu hình Graphics (Y-Sorting cho 2D)
**Edit > Project Settings > Graphics**

Scroll xuống **Camera Settings**:
```
Transparency Sort Mode: Custom Axis
Transparency Sort Axis: 
  X: 0
  Y: 1  ← QUAN TRỌNG
  Z: 0
```

**Giải thích**: Làm cho sprites tự động sort theo trục Y (nhân vật đứng phía sau sẽ render trước).

### 2.5 Cấu hình Quality Settings (tùy chọn)
**Edit > Project Settings > Quality**
```
V Sync Count: Don't Sync (để FPS tự do, hoặc Every V Blank)
Antialiasing: Disabled (2D không cần)
```

---

## STEP 3: Tạo Input Actions

### 3.1 Tạo Input Actions Asset
```
1. Right-click trong Assets/
2. Create > Input Actions
3. Đặt tên: "InputSystem_Actions"
4. QUAN TRỌNG: Kéo vào thư mục Assets/ (đã có sẵn InputSystem_Actions.inputactions)
```

### 3.2 Chỉnh sửa Input Actions
**Double-click vào InputSystem_Actions** để mở cửa sổ Editor:

#### 3.2.1 Tạo Action Map
```
1. Click [+] bên cạnh "Action Maps"
2. Đặt tên: "Player"
```

#### 3.2.2 Tạo Action: Move
```
1. Click [+] trong "Actions" tab
2. Tên: "Move"
3. Action Type: Value
4. Control Type: Vector 2
5. Click [+] bên cạnh "Move" để thêm Binding:
   
   Binding 1: Keyboard WASD
   - Click "<No Binding>"
   - Path: Keyboard > W/A/S/D
   - Hoặc chọn "2D Vector Composite" > WASD

   Binding 2: Gamepad Left Stick
   - Click [+] > Add Binding
   - Path: Gamepad > Left Stick
```

#### 3.2.3 Tạo Action: Dash
```
1. Tạo action mới: "Dash"
2. Action Type: Button
3. Bindings:
   - Keyboard > Space
   - Gamepad > Button South (A/Cross)
```

#### 3.2.4 Tạo Action: Fire
```
1. Tạo action mới: "Fire"
2. Action Type: Button
3. Bindings:
   - Mouse > Left Button
   - Gamepad > Right Trigger
```

### 3.3 Generate C# Class (Tùy chọn nhưng khuyên dùng)
```
1. Click checkbox "Generate C# Class"
2. Class Name: "InputSystem_Actions"
3. Namespace: "SoulKnightClone"
4. C# Class File: chọn thư mục Assets/Scripts/Core/
5. Click [Apply]
```

### 3.4 Save Asset
⚠️ **QUAN TRỌNG**: Click **[Save Asset]** ở góc trên!

---

## STEP 4: Setup Player GameObject

### 4.1 Tạo Hierarchy Structure
**Trong Hierarchy**, tạo cấu trúc sau:

```
📦 Player (Empty GameObject)
  ├─ 🎨 PlayerSprite (GameObject with SpriteRenderer)
  ├─ 🔫 WeaponPivot (Empty GameObject)
  │   ├─ 📍 FirePoint (Empty GameObject)
  │   └─ 🎨 WeaponSprite (GameObject with SpriteRenderer)
  └─ ✨ DashTrail (GameObject with TrailRenderer)
```

**Cách tạo**:
```
1. Right-click Hierarchy > Create Empty
2. Đặt tên "Player", Position (0, 0, 0)

3. Right-click Player > Create Empty
   - Tên: "PlayerSprite"
   - Add Component > Sprite Renderer
   - Sprite: chọn sprite nhân vật (tạm thời dùng Unity's Square)
   - Sorting Layer: chọn hoặc tạo "Characters"
   - Order in Layer: 1

4. Right-click Player > Create Empty
   - Tên: "WeaponPivot"
   - Position: (0, 0, 0) - sẽ xoay quanh Player

5. Right-click WeaponPivot > Create Empty
   - Tên: "FirePoint"
   - Position: (0.5, 0, 0) - offset về phía phải (mỏ súng)

6. Right-click WeaponPivot > Create Empty
   - Tên: "WeaponSprite"
   - Add Component > Sprite Renderer
   - Sprite: chọn sprite súng (hoặc dùng hình chữ nhật tạm)
   - Sorting Layer: "Characters"
   - Order in Layer: 2 (render trên Player)

7. Right-click Player > Effects > Trail
   - Tên: "DashTrail"
   - Position: (0, 0, 0)
```

### 4.2 Configure Player Components

#### 4.2.1 Rigidbody2D
```
1. Select "Player" GameObject
2. Add Component > Rigidbody 2D
3. Cấu hình:
   Body Type: Dynamic
   Simulated: ✅
   Gravity Scale: 0  ← QUAN TRỌNG
   Linear Drag: 0
   Angular Drag: 0
   Collision Detection: Continuous  ← Tránh đạn xuyên tường
   Sleeping Mode: Never Sleep
   Interpolate: Interpolate  ← Smooth movement
   Constraints:
     Freeze Position: (không check)
     Freeze Rotation: ✅ Z  ← Ngăn Player xoay
```

#### 4.2.2 Collider2D
```
1. Add Component > Capsule Collider 2D (hoặc Circle Collider 2D)
2. Cấu hình:
   Material: None
   Is Trigger: ❌ (không tick)
   Size: điều chỉnh cho vừa sprite (vd: 0.8 x 1.2 cho Capsule)
   Offset: (0, 0)
```

#### 4.2.3 Player Scripts
**Add lần lượt các script**:
```
1. Add Component > Player Controller (script)
   - Move Speed: 5
   - Dash Speed: 15
   - Dash Duration: 0.2
   - Dash Cooldown: 1
   - Character Sprite: kéo PlayerSprite vào
   - Weapon Pivot: kéo WeaponPivot vào
   - Dash Trail: kéo DashTrail vào

2. Add Component > Player Stats (script)
   - Max Health: 100
   - Max Armor: 50
   - Max Energy: 200
   - Armor Regen Delay: 3
   - Armor Regen Rate: 10
   - Energy Regen Rate: 20

3. Add Component > Player Input Handler (script)
   (không cần config gì)

4. Add Component > Player Input (Unity component)
   - Actions: kéo InputSystem_Actions asset vào
   - Default Map: Player
   - Behavior: Invoke Unity Events
```

#### 4.2.4 Configure DashTrail
```
1. Select DashTrail object
2. Trail Renderer component:
   Time: 0.3
   Min Vertex Distance: 0.1
   Width: Curve từ 0.5 → 0
   Color: Gradient (trắng → transparent)
   Material: Default-Particle
   Emitting: ❌ (bỏ tick - sẽ được script bật)
```

### 4.3 Setup WeaponPivot Components
```
1. Select WeaponPivot
2. Add Component > Weapon Controller (script)
   - Current Weapon: (để trống, sẽ gán sau)
   - Fire Point: kéo FirePoint GameObject vào
   - Weapon Sprite: kéo WeaponSprite > SpriteRenderer vào

3. Add Component > Audio Source
   - Play On Awake: ❌
   - Loop: ❌
   - Volume: 0.7
```

### 4.4 Gán Layer & Tag
```
1. Select Player (root)
2. Tag: Player
3. Layer: Player
4. Popup "Change layer for children?" → [Yes, change children]
```

---

## STEP 5: Tạo Bullet Prefabs

### 5.1 Tạo Base Bullet GameObject
```
1. Hierarchy > Right-click > 2D Object > Sprite
2. Đặt tên: "BulletPistol"
3. Transform:
   - Position: (0, 0, 0)
   - Rotation: (0, 0, 0)
   - Scale: (0.2, 0.2, 1)  ← Viên đạn nhỏ

4. Sprite Renderer:
   - Sprite: Circle hoặc tạo sprite đạn
   - Color: Yellow
   - Sorting Layer: "Projectiles" (tạo mới nếu chưa có)
   - Order in Layer: 0
```

### 5.2 Add Trail Renderer (Optional nhưng đẹp)
```
1. Select BulletPistol
2. Add Component > Trail Renderer
3. Cấu hình:
   Time: 0.15
   Min Vertex Distance: 0.05
   Width: 0.1 → 0
   Color: Yellow → Transparent
   Material: Default-Particle
```

### 5.3 Add Physics Components
```
1. Add Component > Rigidbody 2D
   Body Type: Dynamic
   Gravity Scale: 0
   Collision Detection: Continuous  ← QUAN TRỌNG
   Constraints: Freeze Rotation Z ✅

2. Add Component > Circle Collider 2D
   Is Trigger: ✅  ← QUAN TRỌNG (OnTriggerEnter2D)
   Radius: 0.1
```

### 5.4 Add Projectile Script
```
1. Add Component > Projectile (script)
2. Cấu hình:
   - Bullet Sprite: kéo SpriteRenderer vào
   - Trail: kéo TrailRenderer vào (nếu có)
   - Hit Effect Prefab: (để trống lúc này, sẽ tạo sau)
```

### 5.5 Set Layer
```
Layer: Projectile
```

### 5.6 Tạo Prefab
```
1. Tạo folder: Assets/Prefabs/Bullets/
2. Kéo BulletPistol từ Hierarchy vào folder này
3. Delete BulletPistol khỏi Hierarchy (giữ lại prefab)
```

### 5.7 Tạo Variants cho các loại đạn khác
**BulletShotgun**:
```
1. Duplicate BulletPistol prefab
2. Đổi tên: BulletShotgun
3. Chỉnh:
   - Color: Orange
   - Scale: (0.15, 0.15, 1) - nhỏ hơn một chút
```

**BulletRifle**:
```
1. Duplicate BulletPistol prefab
2. Đổi tên: BulletRifle
3. Chỉnh:
   - Color: Cyan
   - Scale: (0.25, 0.12, 1) - dạng viên đạn dài
```

---

## STEP 6: Setup Object Pooler

### 6.1 Tạo ObjectPooler GameObject
```
1. Hierarchy > Create Empty
2. Đặt tên: "ObjectPooler"
3. Position: (0, 0, 0)
4. Add Component > Object Pooler (script)
```

### 6.2 Configure Pools
**Trong Inspector của ObjectPooler**:

Click **[+]** 3 lần để tạo 3 pools:

**Pool 0**:
```
Tag: BulletPistol
Prefab: [kéo BulletPistol prefab vào]
Size: 50
```

**Pool 1**:
```
Tag: BulletShotgun
Prefab: [kéo BulletShotgun prefab vào]
Size: 30
```

**Pool 2**:
```
Tag: BulletRifle
Prefab: [kéo BulletRifle prefab vào]
Size: 100
```

⚠️ **Lưu ý**: Tag phải khớp với `bulletPoolTag` trong WeaponData!

---

## STEP 7: Tạo Weapon Data (ScriptableObjects)

### 7.1 Tạo folder
```
Assets/ > Right-click > Create Folder > "WeaponData"
```

### 7.2 Tạo Pistol Data
```
1. Right-click Assets/WeaponData/
2. Create > Soul Knight > Weapon Data
3. Đặt tên: "Pistol_Data"
4. Cấu hình trong Inspector:

[Weapon Info]
  Weapon Name: Pistol
  Weapon Type: Pistol
  Weapon Sprite: [sprite súng, hoặc để trống]

[Stats]
  Damage: 10
  Energy Cost: 5

[Fire Rate]
  Fire Rate: 5  (5 viên/giây)
  Is Automatic: ❌  (Semi-auto)

[Projectile]
  Bullet Speed: 20
  Bullet Lifetime: 3
  Bullet Pool Tag: "BulletPistol"  ← Khớp với ObjectPooler

[Accuracy]
  Accuracy: 2
  Bullets Per Shot: 1
  Spread Angle: 0

[Recoil]
  Recoil Force: 0.5

[Effects]
  Fire Sound: (để trống)
  Muzzle Flash Prefab: (để trống)
  Screen Shake Intensity: 0.1
```

### 7.3 Tạo Shotgun Data
```
1. Duplicate Pistol_Data
2. Đổi tên: "Shotgun_Data"
3. Chỉnh:
   Weapon Name: Shotgun
   Weapon Type: Shotgun
   Damage: 8
   Energy Cost: 10
   Fire Rate: 1.5
   Bullet Speed: 15
   Bullet Pool Tag: "BulletShotgun"
   Accuracy: 5  ← Spread cao hơn
   Bullets Per Shot: 5  ← Bắn 5 viên
   Spread Angle: 30  ← Góc spread
```

### 7.4 Tạo Rifle Data
```
1. Duplicate Pistol_Data
2. Đổi tên: "Rifle_Data"
3. Chỉnh:
   Weapon Name: Rifle
   Weapon Type: Rifle
   Damage: 15
   Energy Cost: 8
   Fire Rate: 10  ← Bắn nhanh
   Is Automatic: ✅  ← Giữ chuột để bắn
   Bullet Speed: 30
   Bullet Pool Tag: "BulletRifle"
   Accuracy: 1  ← Chính xác cao
```

### 7.5 Gán Weapon cho Player
```
1. Select Player > WeaponPivot trong Hierarchy
2. Weapon Controller component
3. Current Weapon: kéo Pistol_Data vào
```

---

## STEP 8: Setup Camera với Cinemachine

### 8.1 Tạo Virtual Camera
```
1. GameObject > Cinemachine > Virtual Camera
2. Đặt tên: "PlayerCamera"
3. Trong Inspector:
   
   [Body]
   - Follow: kéo Player GameObject vào
   - Tracking: Framing Transposer
   - Screen X: 0.5
   - Screen Y: 0.5
   - Camera Distance: 10
   - Damping: (1, 1, 1) - càng cao càng mượt nhưng lag hơn

   [Aim]
   - Tracked Object Offset: (0, 0, 0)
```

### 8.2 Add Noise (cho Screen Shake)
```
1. Virtual Camera Inspector
2. Add Extension > CinemachineBasicMultiChannelPerlin
3. Cấu hình:
   - Noise Profile: Basic Multi Channel Perlin
   - Amplitude Gain: 0  (sẽ được script thay đổi)
   - Frequency Gain: 1
```

### 8.3 Tạo CameraShaker GameObject
```
1. Hierarchy > Create Empty
2. Đặt tên: "CameraShaker"
3. Add Component > Camera Shaker (script)
4. Cấu hình:
   - Virtual Camera: kéo PlayerCamera vào
   - Default Intensity: 1
   - Default Duration: 0.1
```

### 8.4 Adjust Main Camera
```
1. Select Main Camera
2. Camera component:
   - Projection: Orthographic
   - Size: 5 (zoom level, điều chỉnh theo ý thích)
   - Clipping Planes: Near: 0.3, Far: 1000
```

---

## STEP 9: Testing & Debugging

### 9.1 Test Checklist

**✅ Scene Setup**:
```
□ Main Camera có Cinemachine Brain component (tự động thêm)
□ ObjectPooler có trong scene
□ CameraShaker có trong scene
□ Player có đầy đủ components
```

**✅ Input Test**:
```
1. Click Play
2. Console có lỗi không? (fix nếu có)
3. Test WASD: Player có di chuyển mượt?
4. Test Space: Player có dash?
5. Test Left Click: Có spawn bullets không?
```

### 9.2 Debug Common Issues

**❌ Lỗi: "NullReferenceException in PlayerController"**
```
Nguyên nhân: Chưa assign references
Fix:
- Kiểm tra Character Sprite, Weapon Pivot đã assign chưa
- Kiểm tra Dash Trail đã assign chưa
```

**❌ Lỗi: "Bullet pool doesn't exist"**
```
Nguyên nhân: Tag trong WeaponData không khớp ObjectPooler
Fix:
- Kiểm tra bulletPoolTag trong WeaponData
- Kiểm tra tag trong ObjectPooler Pools
- Đảm bảo chữ hoa/thường giống nhau
```

**❌ Player không flip sprite**
```
Nguyên nhân: Character Sprite chưa assign
Fix:
- Assign PlayerSprite > SpriteRenderer vào PlayerController
```

**❌ Bullets không xuất hiện**
```
Nguyên nhân: FirePoint chưa được assign
Fix:
- Assign FirePoint vào WeaponController
- Kiểm tra FirePoint position (phải offset khỏi Player)
```

**❌ Camera không theo Player**
```
Nguyên nhân: Cinemachine chưa setup đúng
Fix:
- Kiểm tra Virtual Camera > Follow đã assign Player chưa
- Kiểm tra Main Camera có CinemachineBrain component
```

### 9.3 Performance Check

**Trong Play Mode**:
```
1. Window > Analysis > Profiler
2. Bắn nhiều viên đạn
3. Kiểm tra FPS có giảm không
4. Kiểm tra Memory có tăng liên tục không (memory leak)
```

**Kỳ vọng**:
- FPS: 60+ stable
- Batches: <50 (kiểm tra trong Game view > Stats)
- Garbage Collection: không xảy ra liên tục

---

## ✅ HOÀN THÀNH GIAI ĐOẠN 1!

Bây giờ bạn có:
- ✅ Player di chuyển 8 hướng mượt mà
- ✅ Dash với I-frames và trail effect
- ✅ Hệ thống bắn súng với fire rate
- ✅ Object pooling cho bullets
- ✅ 3 loại vũ khí: Pistol, Shotgun, Rifle
- ✅ Camera follow player mượt mà

---

## 🎯 NEXT STEPS

**Test thêm**:
1. Thử thay đổi `Current Weapon` trong WeaponController
2. Test Shotgun (5 viên spread)
3. Test Rifle (automatic mode)
4. Điều chỉnh các thông số cho phù hợp game feel

**Khi sẵn sàng, tiếp tục**:
→ **[PHASE_2_DUNGEON_GENERATION.md](PHASE_2_DUNGEON_GENERATION.md)** (sẽ tạo)  
→ **[PHASE_3_ENEMY_AI.md](PHASE_3_ENEMY_AI.md)** (sẽ tạo)

---

## 📞 TROUBLESHOOTING SUPPORT

**Nếu gặp vấn đề**:
1. Kiểm tra Console errors
2. Đối chiếu với checklist trên
3. Kiểm tra script references trong Inspector
4. Restart Unity Editor nếu cần

**Debug Tips**:
- Sử dụng `Debug.Log()` để kiểm tra giá trị
- Sử dụng Gizmos trong Scene view (Player có vẽ line tới chuột)
- Kiểm tra Layer Collision Matrix nếu collision không hoạt động
