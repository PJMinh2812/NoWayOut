# NoWayOut

🎮 **Soul Knight Clone** - Game 2D Top-down Rogue-like được xây dựng với Unity

---

## 📚 TÀI LIỆU HƯỚNG DẪN

### 🚀 Quick Start
- **[SETUP_GUIDE.md](SETUP_GUIDE.md)**: Tổng quan và hướng dẫn nhanh
- **[docs/ROADMAP.md](docs/ROADMAP.md)**: Lộ trình phát triển chi tiết

### 📖 Hướng dẫn từng giai đoạn

| Giai đoạn | Nội dung | Trạng thái | Link |
|-----------|----------|------------|------|
| **1** | Player & Weapon System | ✅ Hoàn thành | **[Chi tiết](docs/PHASE_1_SETUP_GUIDE.md)** |
| **2** | Dungeon Generation | 🚧 Sẵn sàng | [Template](docs/PHASE_2_DUNGEON_GENERATION.md) |
| **3** | Enemy AI System | 🚧 Sẵn sàng | [Template](docs/PHASE_3_ENEMY_AI.md) |
| **4** | UI/UX System | 🚧 Sẵn sàng | [Template](docs/PHASE_4_UI_SYSTEM.md) |

---

## ✨ Tính năng (Giai đoạn 1 - Đã hoàn thành)

- ✅ **Player Movement**: Di chuyển 8 hướng mượt mà với Rigidbody2D
- ✅ **Dash System**: Lướt nhanh với I-frames và trail effect
- ✅ **Weapon System**: ScriptableObject-based, 3 loại vũ khí (Pistol, Shotgun, Rifle)
- ✅ **Stats System**: Health, Armor (tự hồi), Energy
- ✅ **Object Pooling**: Tối ưu hiệu năng cho bullets
- ✅ **Camera Follow**: Cinemachine với screen shake
- ✅ **Input System**: Hỗ trợ cả PC (WASD) và Controller

---

## 🛠️ Công nghệ sử dụng

- **Unity**: 2022.3 LTS+
- **Packages**:
  - Input System (com.unity.inputsystem)
  - Cinemachine (com.unity.cinemachine)
  - Universal Render Pipeline (URP)
  - 2D Tilemap Editor

---

## 📦 Cấu trúc Code

```
Assets/Scripts/
├── Core/
│   ├── GameManager.cs
│   ├── ObjectPooler.cs
│   ├── GameConstants.cs
│   └── CameraShaker.cs
├── Player/
│   ├── PlayerController.cs
│   ├── PlayerStats.cs
│   └── PlayerInputHandler.cs
└── Weapons/
    ├── WeaponData.cs
    ├── WeaponController.cs
    └── Projectile.cs
```

---

## 🎮 Cách chơi

**PC**:
- `WASD`: Di chuyển
- `Mouse`: Nhắm bắn
- `Left Click`: Bắn
- `Space`: Dash

**Controller**:
- `Left Stick`: Di chuyển
- `Right Stick`: Nhắm
- `Right Trigger`: Bắn
- `A/Cross`: Dash

---

## 🚀 Bắt đầu

1. Clone repository
2. Mở project bằng Unity 2022.3 LTS+
3. Đọc [SETUP_GUIDE.md](SETUP_GUIDE.md) để setup Unity scene
4. Xem [docs/PHASE_1_SETUP_GUIDE.md](docs/PHASE_1_SETUP_GUIDE.md) để hướng dẫn chi tiết

---

## 📊 Tiến độ

```
Phase 1: ████████████████████ 100% ✅ Player & Weapons
Phase 2: ░░░░░░░░░░░░░░░░░░░░   0% 🚧 Dungeon Generation
Phase 3: ░░░░░░░░░░░░░░░░░░░░   0% 🚧 Enemy AI
Phase 4: ░░░░░░░░░░░░░░░░░░░░   0% 🚧 UI/UX
─────────────────────────────────────────────────────
Overall: █████░░░░░░░░░░░░░░░  25%
```

---

## 🎯 Roadmap

- [x] **Phase 1**: Core Player & Weapon System
- [ ] **Phase 2**: Procedural Dungeon Generation
- [ ] **Phase 3**: Enemy AI with FSM
- [ ] **Phase 4**: Complete UI/UX
- [ ] **Phase 5**: Polish & Mobile Support

Xem chi tiết: [docs/ROADMAP.md](docs/ROADMAP.md)

---

## 📝 License

Dự án học tập, không dành cho mục đích thương mại.

---

**Developed with ❤️ using Unity**