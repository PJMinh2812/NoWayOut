# ✅ CHECKLIST - SOUL KNIGHT CLONE

> Sử dụng file này để theo dõi tiến độ từng bước

---

## 🎯 GIAI ĐOẠN 1: PLAYER & WEAPON SYSTEM ✅

### Setup Unity Project
- [x] Cài đặt Input System package
- [x] Cài đặt Cinemachine package
- [x] Cấu hình Project Settings (Tags, Layers, Physics 2D)
- [x] Cấu hình Graphics (Y-Sorting)

### Core Scripts
- [x] GameManager.cs
- [x] ObjectPooler.cs
- [x] GameConstants.cs
- [x] CameraShaker.cs

### Player System
- [x] PlayerController.cs (Movement + Dash)
- [x] PlayerStats.cs (Health/Armor/Energy)
- [x] PlayerInputHandler.cs

### Weapon System
- [x] WeaponData.cs (ScriptableObject)
- [x] WeaponController.cs
- [x] Projectile.cs

### Unity Setup
- [ ] Tạo Input Actions asset
- [ ] Setup Player GameObject hierarchy
- [ ] Tạo 3 Bullet prefabs (Pistol, Shotgun, Rifle)
- [ ] Configure ObjectPooler
- [ ] Tạo 3 Weapon Data (Pistol, Shotgun, Rifle)
- [ ] Setup Cinemachine Virtual Camera
- [ ] Setup CameraShaker

### Testing
- [ ] Test movement 8 hướng
- [ ] Test Dash với I-frames
- [ ] Test bắn súng Pistol
- [ ] Test Shotgun spread pattern
- [ ] Test Rifle automatic
- [ ] Test Object Pooling (performance)
- [ ] Test Camera follow và screen shake

---

## 🗺️ GIAI ĐOẠN 2: DUNGEON GENERATION 🚧

### Scripts cần tạo
- [ ] DungeonGenerator.cs
- [ ] Room.cs
- [ ] RoomTemplates.cs
- [ ] DoorController.cs
- [ ] DestructibleObject.cs
- [ ] MinimapController.cs

### Unity Setup
- [ ] Import Tilemap package
- [ ] Tạo Tilemap palettes (Floor, Wall)
- [ ] Tạo Rule Tiles cho walls
- [ ] Tạo Room prefabs (Start, Combat, Treasure, Boss)
- [ ] Tạo Door prefab
- [ ] Tạo Destructible object prefabs

### Testing
- [ ] Test Random Walk algorithm
- [ ] Test room connections
- [ ] Test door open/close logic
- [ ] Test minimap
- [ ] Test performance với large dungeons

---

## 🤖 GIAI ĐOẠN 3: ENEMY AI SYSTEM 🚧

### Core AI Scripts
- [ ] EnemyController.cs
- [ ] EnemyStats.cs
- [ ] EnemyStateMachine.cs

### AI States
- [ ] IdleState.cs
- [ ] WanderState.cs
- [ ] ChaseState.cs
- [ ] AttackState.cs
- [ ] DeathState.cs

### Enemy Types
- [ ] MeleeEnemy.cs
- [ ] RangedEnemy.cs
- [ ] BossEnemy.cs

### Other Systems
- [ ] LootDropper.cs
- [ ] Pathfinding integration

### Unity Setup
- [ ] Tạo Enemy prefabs (Melee, Ranged, Boss)
- [ ] Tạo Pickup prefabs (Health, Energy)
- [ ] Setup enemy animations
- [ ] Configure enemy stats

### Testing
- [ ] Test FSM transitions
- [ ] Test pathfinding
- [ ] Test chase behavior
- [ ] Test attack patterns
- [ ] Test loot drops
- [ ] Test multiple enemies

---

## 🎨 GIAI ĐOẠN 4: UI/UX SYSTEM 🚧

### HUD Scripts
- [ ] HUDController.cs
- [ ] HealthBarUI.cs
- [ ] ArmorBarUI.cs
- [ ] EnergyBarUI.cs
- [ ] WeaponIconUI.cs

### Other UI Scripts
- [ ] DamageNumberSpawner.cs
- [ ] PauseMenuController.cs
- [ ] MinimapUI.cs

### Unity Setup
- [ ] Tạo Canvas với HUD elements
- [ ] Import UI sprites (bars, icons)
- [ ] Setup TextMeshPro
- [ ] Tạo Pause Menu UI
- [ ] Tạo Game Over screen
- [ ] Setup Minimap camera

### Testing
- [ ] Test bar updates
- [ ] Test damage numbers
- [ ] Test pause menu
- [ ] Test weapon icon switching
- [ ] Test minimap rendering

---

## 🏆 POLISH & FINAL 🚧

### Visual Effects
- [ ] Particle systems (muzzle flash, hit effects)
- [ ] Screen shake refinement
- [ ] Trail effects
- [ ] Death animations

### Audio
- [ ] Import sound effects
- [ ] Setup AudioManager
- [ ] Background music
- [ ] Weapon sounds
- [ ] UI sounds

### Performance Optimization
- [ ] Object pooling cho effects
- [ ] Optimize draw calls
- [ ] Optimize collision detection
- [ ] Profile với Unity Profiler

### Mobile Support
- [ ] Add touch controls (virtual joystick)
- [ ] UI scaling cho different resolutions
- [ ] Performance optimization cho mobile
- [ ] Test trên Android/iOS

### Build
- [ ] Configure build settings
- [ ] Test standalone build
- [ ] Test mobile build
- [ ] Create installer/APK

---

## 📝 NOTES

### Bugs cần fix
- [ ] (Ghi chú bugs tại đây)

### Features muốn thêm
- [ ] Power-ups system
- [ ] More weapon types
- [ ] Boss patterns
- [ ] Save/Load system
- [ ] Achievements

### Optimization TODO
- [ ] Reduce draw calls
- [ ] Optimize bullet pooling size
- [ ] Compress textures

---

**Cập nhật lần cuối**: Giai đoạn 1 hoàn thành  
**Tiếp theo**: Triển khai Giai đoạn 2
