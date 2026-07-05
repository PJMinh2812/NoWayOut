# QUICK START: Setup Map "The Awakening" trong 30 phút

## TÓM TẮT

**Bạn đã có:**

- ✅ 5 script bẫy (FakeFloor, InvisibleBlock, SlipperyFloor, SpringTrap, HiddenSpikes)
- ✅ Script LightFragment và IllusionWall
- ✅ Asset MungeonDage (tileset + sprites)

**Bạn cần làm:**

1. Vẽ 5 phòng theo layout
2. Đặt bẫy vào đúng vị trí
3. Test và polish

---

## BƯỚC 1: Tạo Scene (5 phút)

1. `File > New Scene` → "Level_01_TheAwakening"
2. Tạo Grid + 4 Tilemap layers:
   - Background (-10)
   - Floor (0)
   - Wall (1) + Tilemap Collider 2D
   - Decoration (2)
3. Import tileset từ `MungeonDage/Tileset` vào Tile Palette

---

## BƯỚC 2: Vẽ 5 Phòng (10 phút)

### Layout Tổng:

```
[START-Đỏ] → [HÀNH LANG-Vàng] → [BOSS-Xanh]
                    ↓                  ↓
              [KHU VỰC-Cam]    → [GOAL-Tím]
```

**Kích thước:**

- Start: 10x8 tiles
- Hành Lang: 30x6 tiles
- Boss: 12x12 tiles
- Khu Vực Cam: 20x8 tiles
- Goal: 10x10 tiles

---

## BƯỚC 3: Đặt Bẫy (10 phút)

### Phòng START

**Vị trí:** (10, 4) - ngay sau tấm biển

```
GameObject: "FakeFloor_Start"
- Script: FakeFloor.cs
- Sprite: Copy từ Floor layer
- BoxCollider2D: IsTrigger ✓
```

### Hành Lang VÀNG

**Vị trí:** (16, 5) - trên không, giữa hố

```
GameObject: "InvisibleBlock_01"
- Script: InvisibleBlock.cs
- BoxCollider2D: 1x1
- SpriteRenderer: Disabled ban đầu
```

### Phòng BOSS

**Vị trí:** 8 ô bao quanh con chuột (3x3 grid)

```
GameObject: "SlipperyFloor_01" → "08"
- Script: SlipperyFloor.cs
- Sprite: Màu xanh nhạt
- BoxCollider2D: IsTrigger ✓
```

### Khu Vực CAM

**Vị trí:** Trước Mảnh Sáng #3 (1 tile)

```
GameObject: "SpringTrap_Cam"
- Script: SpringTrap.cs
- Push Direction: (-1, 0)
- Spring Force: 20
- BoxCollider2D: IsTrigger ✓
```

### Phòng GOAL

**Vị trí:** (5, 5) - trước bức tường ảo

```
GameObject: "HiddenSpikes_Goal"
- Script: HiddenSpikes.cs
- Child: "Spikes_Visual"
  - Position Y: -1 (ẩn dưới)
- BoxCollider2D: IsTrigger ✓
```

---

## BƯỚC 4: Đặt Items (3 phút)

### Mảnh Sáng x3

**Vị trí:**

- #1: Hành Lang Vàng (16, 3)
- #2: Phòng Boss (6, 6)
- #3: Khu Vực Cam (cuối phòng)

```
GameObject: "LightFragment_01"
- Script: LightFragment.cs
- Sprite: Vật phẩm sáng
- CircleCollider2D: IsTrigger ✓
- Add Light2D (Point Light)
```

### Bức Tường Ảo

**Vị trí:** Phòng Goal (5, 3)

```
GameObject: "IllusionWall"
- Script: IllusionWall.cs
- Sprite: Tường mờ nhạt
- BoxCollider2D
- Alpha: 0.8
```

---

## BƯỚC 5: Polish & Test (2 phút)

### Add vào Main Camera:

```csharp
// CameraShake.cs (đã tạo trong hướng dẫn)
public void Shake(float duration, float magnitude)
```

### Test Checklist:

- [ ] FakeFloor rơi khi bước vào
- [ ] InvisibleBlock đập đầu khi nhảy
- [ ] SlipperyFloor trượt không dừng
- [ ] SpringTrap bật ngược lại
- [ ] HiddenSpikes mọc khi đứng gần
- [ ] LightFragment nhặt được
- [ ] IllusionWall biến mất khi bấm Space

---

## HOTKEYS HỮU ÍCH

**Unity Editor:**

- `F`: Focus vào object đang chọn
- `V`: Vertex Snap (snap tile chính xác)
- `Ctrl+D`: Duplicate object
- `Alt+Shift+D`: Duplicate với tăng số

**Tilemap:**

- `B`: Brush tool
- `I`: Eyedropper (lấy tile)
- `Shift+Drag`: Paint line
- `Ctrl+Shift+Drag`: Fill area

---

## COMMON ISSUES & FIX

### ❌ Bẫy không hoạt động

**Fix:** Check Layer Collision Matrix

- `Edit > Project Settings > Physics 2D`
- "Player" phải va chạm với "Traps"

### ❌ Người chơi đi xuyên tường

**Fix:**

- Wall_Layer cần Tilemap Collider 2D
- Add Composite Collider 2D
- Tick "Used By Composite"

### ❌ Script báo lỗi `linearVelocity`

**Fix:**

- Unity 6+ dùng `linearVelocity`
- Unity 2022 trở xuống dùng `velocity`

---

## VỊ TRÍ FILES ĐÃ TẠO

```
Assets/
├── Scripts/
│   ├── Traps/
│   │   ├── FakeFloor.cs ✅
│   │   ├── InvisibleBlock.cs ✅
│   │   ├── SlipperyFloor.cs ✅
│   │   ├── SpringTrap.cs ✅
│   │   └── HiddenSpikes.cs ✅
│   ├── Items/
│   │   └── LightFragment.cs ✅
│   └── Environment/
│       └── IllusionWall.cs ✅
├── Scenes/
│   └── Level_01_TheAwakening.unity (bạn tạo)
└── Prefabs/
    └── Traps/ (bạn tạo từ các script trên)
```

---

## NEXT STEPS

1. **Tạo Prefabs** từ các GameObject có bẫy
2. **Setup Audio** (âm thanh BONK, BOING, cracking)
3. **Add Particle Effects** (dust, sparkle, ice shards)
4. **Lighting 2D** (global light + point lights)
5. **Playtest với bạn bè** và ghi hình phản ứng 😈

---

**Tham khảo chi tiết:** Đọc file `MAP_SETUP_GUIDE_TheAwakening.md` để có hướng dẫn đầy đủ.

**Good luck and have fun trolling your players! 🎮💀**
