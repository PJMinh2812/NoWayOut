# 🤖 GIAI ĐOẠN 3: ENEMY AI SYSTEM - HƯỚNG DẪN CHI TIẾT

> **Trạng thái**: 🚧 SẴN SÀNG TRIỂN KHAI  
> **Yêu cầu**: Hoàn thành Giai đoạn 1 & 2

---

## 📋 TỔNG QUAN

Giai đoạn này bạn sẽ xây dựng:
- ✅ Finite State Machine (FSM) cho Enemy AI
- ✅ States: Idle, Wander, Chase, Attack, Death
- ✅ Pathfinding system (A* hoặc Simple Follow)
- ✅ Enemy Stats & Health system
- ✅ Loot drop system (Energy, Health pickups)
- ✅ Multiple enemy types (Melee, Ranged, Boss)

---

## 🎯 CẤU TRÚC HỆ THỐNG

### Scripts cần tạo:
```
Assets/Scripts/
└── Enemies/
    ├── EnemyController.cs          # Main AI controller
    ├── EnemyStats.cs               # Health, damage, speed
    ├── EnemyStateMachine.cs        # FSM implementation
    ├── States/
    │   ├── IdleState.cs
    │   ├── WanderState.cs
    │   ├── ChaseState.cs
    │   ├── AttackState.cs
    │   └── DeathState.cs
    ├── EnemyTypes/
    │   ├── MeleeEnemy.cs
    │   ├── RangedEnemy.cs
    │   └── BossEnemy.cs
    └── LootDropper.cs              # Drop items on death
```

### Prefabs cần tạo:
```
Assets/Prefabs/Enemies/
├── Enemy_Melee.prefab
├── Enemy_Ranged.prefab
├── Enemy_Boss.prefab
└── Pickups/
    ├── Pickup_Health.prefab
    └── Pickup_Energy.prefab
```

---

## 📝 NỘI DUNG CHI TIẾT

> Sẽ được bổ sung khi bạn yêu cầu triển khai Giai đoạn 3
> 
> **Để bắt đầu, hãy nói**: "Triển khai Giai đoạn 3: Enemy AI"

---

## 🔗 LIÊN KẾT

- ← [Quay lại Giai đoạn 2](PHASE_2_DUNGEON_GENERATION.md)
- → [Tiếp tục Giai đoạn 4](PHASE_4_UI_SYSTEM.md)
