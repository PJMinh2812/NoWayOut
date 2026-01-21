# 🗺️ GIAI ĐOẠN 2: DUNGEON GENERATION - HƯỚNG DẪN CHI TIẾT

> **Trạng thái**: 🚧 SẴN SÀNG TRIỂN KHAI  
> **Yêu cầu**: Hoàn thành [PHASE_1_SETUP_GUIDE.md](PHASE_1_SETUP_GUIDE.md)

---

## 📋 TỔNG QUAN

Giai đoạn này bạn sẽ xây dựng:
- ✅ Procedural Dungeon Generation với Random Walk Algorithm
- ✅ Room-based system (Start, Combat, Treasure, Boss)
- ✅ Tilemap integration cho walls, floors
- ✅ Door system (tự động đóng khi có quái)
- ✅ Minimap system
- ✅ Destructible obstacles (barrels, crates)

---

## 🎯 CẤU TRÚC HỆ THỐNG

### Scripts cần tạo:
```
Assets/Scripts/
└── Dungeon/
    ├── DungeonGenerator.cs        # Core generation logic
    ├── Room.cs                     # Room data structure
    ├── RoomTemplates.cs            # Room prefabs manager
    ├── DoorController.cs           # Door behavior
    ├── DestructibleObject.cs       # Barrels, crates
    └── MinimapController.cs        # Minimap camera
```

### Prefabs cần tạo:
```
Assets/Prefabs/Dungeon/
├── Rooms/
│   ├── Room_Start.prefab
│   ├── Room_Combat.prefab
│   ├── Room_Treasure.prefab
│   └── Room_Boss.prefab
├── Doors/
│   └── Door.prefab
└── Obstacles/
    ├── Barrel.prefab
    └── Crate.prefab
```

---

## 📝 NỘI DUNG CHI TIẾT

> Sẽ được bổ sung khi bạn yêu cầu triển khai Giai đoạn 2
> 
> **Để bắt đầu, hãy nói**: "Triển khai Giai đoạn 2: Dungeon Generation"

---

## 🔗 LIÊN KẾT

- ← [Quay lại Giai đoạn 1](PHASE_1_SETUP_GUIDE.md)
- → [Tiếp tục Giai đoạn 3](PHASE_3_ENEMY_AI.md)
