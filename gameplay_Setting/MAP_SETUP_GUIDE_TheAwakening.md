# HƯỚNG DẪN SETUP MAP: THE AWAKENING (RAGE MODE)

**Asset sử dụng:** `MungeonDage` folder (Tileset + Dage + Marshmallow sprites)  
**Loại map:** Grid-based với bẫy troll kiểu Rage Game  
**Thời gian hoàn thành:** 2-3 giờ (tùy độ chi tiết)

---

## I. CHUẨN BỊ

### 1. Tạo Scene Mới

1. Trong Unity: `File > New Scene`
2. Đặt tên: `Level_01_TheAwakening`
3. Save vào: `Assets/Scenes/`

### 2. Import Tileset từ MungeonDage

1. Mở folder `Assets/Art/MungeonDage/Tileset/tiles`
2. Tạo **Tile Palette** mới:
   - Window > 2D > Tile Palette
   - Create New Palette: `MungeonDageTileset`
3. Kéo tất cả tile vào Palette

### 3. Tạo Tilemap Layers

Trong Hierarchy, tạo các layers sau (theo thứ tự từ dưới lên):

```
Grid (Main)
├── Background_Layer (Tilemap) - Order in Layer: -10
├── Floor_Layer (Tilemap) - Order in Layer: 0
├── Wall_Layer (Tilemap, Collider) - Order in Layer: 1
├── Decoration_Layer (Tilemap) - Order in Layer: 2
└── Traps_Layer (Empty GameObject) - Chứa các Prefab bẫy
```

**Cài đặt Collider:**

- `Wall_Layer`: Add Component → Tilemap Collider 2D
- `Wall_Layer`: Add Component → Composite Collider 2D
- Tick "Used By Composite" trong Tilemap Collider 2D

---

## II. VẼ BẢN ĐỒ THEO THIẾT KẾ

### Bố Cục Tổng Thể (Grid-based)

Mỗi "ô" = 1 tile (1x1 unit trong Unity)

```
┌──────────────────────────────────────────────────────┐
│  [PHÒNG START]                                        │
│      (Đỏ)     ──▶  [HÀNH LANG VÀNG] ──▶ [PHÒNG BOSS] │
│                         │                   (Xanh)   │
│                         ▼                      │      │
│                   [KHU VỰC CAM]               ▼      │
│                         │               [PHÒNG GOAL] │
│                         ▼                   (Tím)    │
│                  [Mảnh Sáng #3]                       │
│                    + SPRING TRAP                      │
└──────────────────────────────────────────────────────┘
```

### 1. Phòng Start (Màu Đỏ) - 10x8 tiles

**Vẽ Tilemap:**

- Nền: Gạch đá tối màu
- Tường: Bao quanh 4 cạnh
- Cửa ra: Bên phải, giữa tường

**Đặt Bẫy:**

1. Đặt tấm biển (Text hoặc Sprite):
   - Vị trí: (9, 4) - ngay trước cửa
   - Text: "Hãy cẩn thận bước chân"
2. Đặt **FakeFloor** Prefab:
   - Vị trí: (10, 4) - ngay sau tấm biển
   - Sprite: Copy texture từ Floor_Layer
   - Add: FakeFloor.cs script

**Cách tạo FakeFloor GameObject:**

```
1. Create Empty GameObject: "FakeFloor_01"
2. Add Component: FakeFloor.cs
3. Add Component: BoxCollider2D
   - IsTrigger: ✓ (checked)
4. Add Component: SpriteRenderer
   - Sprite: Chọn sprite sàn từ MungeonDage
   - Color: Làm nhạt một chút (RGBA: 0.95, 0.95, 0.95, 1)
5. Đặt vào Traps_Layer
6. Drag vào Prefabs folder để tái sử dụng
```

**Prefab Settings:**

- Fall Delay: 0.1
- Respawn Time: 5
- Show Warning: ✓

---

### 2. Khu Vực 1 - Hành Lang Vàng (30x6 tiles)

**Layout:**

```
[START] ══════╗   ╔═══════════════╗
              ║   ║               ║
          [Hố nhỏ]             [Boss]
           + Mảnh Sáng
```

**Vẽ Tilemap:**

- Sàn vàng/be sáng
- Hố nhỏ: 3 tiles rộng (X: 15-17)
- Mảnh Sáng: GameObject ở giữa hố (16, 3)

**Đặt Bẫy InvisibleBlock:**

1. Tạo Empty GameObject: "InvisibleBlock_01"
2. Vị trí: (16, 5) - trên không trung, giữa hố
3. Add: InvisibleBlock.cs
4. Add: BoxCollider2D (1x1 size)
5. Add: SpriteRenderer
   - Sprite: Gạch bình thường
   - **Tắt "Enabled" trong Inspector** (ẩn ban đầu)

**Settings:**

- Reveal On Hit: ✓
- Knockback Force: 10
- Deal Damage: ✓
- Damage Amount: 1

**Gợi ý thiết kế:**

- Đặt 1 gạch giả bên cạnh hố để người chơi nghĩ có thể nhảy sát
- Thực tế phải nhảy ở góc để tránh đập đầu

---

### 3. Phòng Mini-Boss (Màu Xanh Dương) - 12x12 tiles

**Layout:**

```
╔════════════════╗
║    🪵  🪵     ║
║   💤CON CHUỘT💤║  ← Con chuột ngủ giữa
║    🪵  🪵     ║
╚════════════════╝
```

**Vẽ Tilemap:**

- Sàn: Gạch xanh dương (water tiles hoặc ice tiles)
- Thùng gỗ: Sprites từ MungeonDage hoặc tự vẽ
- Con chuột: Sprite Dage (idle animation)

**Đặt Bẫy SlipperyFloor:**

1. Tạo 8 ô sàn trơn bao quanh con chuột (3x3 grid, bỏ ô giữa)
2. Mỗi ô:
   - GameObject: "SlipperyFloor_01" đến "08"
   - Script: SlipperyFloor.cs
   - BoxCollider2D: IsTrigger ✓
   - SpriteRenderer: Màu xanh nhạt (ice effect)

**Settings:**

- Slide Force: 5
- Slide Duration: 1.5
- Can Break Slide: ✓ (va thùng thì dừng)

**Setup Con Chuột (Enemy):**

- Add Enemy2D script (nếu có sẵn)
- Hoặc tạo script đơn giản: ngủ → nghe âm thanh → tỉnh → tấn công

**Thùng Gỗ:**

- Tag: "Obstacle"
- Add: BoxCollider2D
- Add: Rigidbody2D (Dynamic)

---

### 4. Khu Vực 2 (Màu Cam) - 20x8 tiles

**Layout:**

```
[Từ Boss Room] ────────────▶ [⭐ Mảnh Sáng #3]
                                  (Spring Trap)
```

**Vẽ Tilemap:**

- Sàn cam sáng
- Mảnh Sáng: Sprite sáng bóng ở cuối phòng

**Đặt Bẫy SpringTrap:**

1. Vị trí: Ngay trước Mảnh Sáng (1 tile phía trước)
2. GameObject: "SpringTrap_01"
3. Script: SpringTrap.cs
4. Sprite: Hình lò xo hoặc ẩn đi
5. BoxCollider2D: IsTrigger ✓

**Settings:**

- Spring Force: 20
- Push Direction: (-1, 0) → bật sang trái
- Reveal Spring: ✓ (hiện ra để người chơi thấy cái gì vừa bật mình)
- Cooldown: 2 giây

**Đường Tắt Bí Mật (Optional):**

- Vẽ một đường nhỏ phía sau Mảnh Sáng
- Đặt tường giả (không collider) để người chơi phát hiện

---

### 5. Phòng Goal (Màu Tím) - 10x10 tiles

**Layout:**

```
╔═══════════════╗
║               ║
║    [Player]   ║
║       │       ║
║       ▼       ║
║   [🌫 Tường   ║  ← Bức tường ảo ảnh
║      Ảo]      ║
║   ☠ Spikes    ║  ← Hidden Spikes
╚═══════════════╝
```

**Vẽ Tilemap:**

- Sàn tím/đen
- Bức tường ảo: Sprite có hiệu ứng mờ nhạt
  - Add script: IllusionWall.cs (phát sáng khi bấm Space)

**Đặt Bẫy HiddenSpikes:**

1. Vị trí: (5, 5) - ngay trước bức tường
2. GameObject: "HiddenSpikes_01"
3. Child Object: "Spikes_Visual"
   - SpriteRenderer: Hình chông
   - Transform Position Y: -1 (ẩn xuống dưới)

**Settings:**

- Trigger Delay: 0.2 giây
- Rise Time: 0.3 giây
- Damage: 2
- Instant Kill: ✗ (để người chơi có cơ hội học)

**Cách hoạt động:**

- Người chơi đứng vào ô (5, 5)
- Sau 0.2 giây → chông mọc lên
- Nếu đứng quá lâu → trúng chông mất máu

**Giải pháp:**

- Đứng cách xa 1 ô (vị trí 5, 6) rồi bấm Space

---

## III. SETUP PREFABS

### A. Tạo Prefab Folder

```
Assets/Prefabs/
├── Traps/
│   ├── FakeFloor.prefab
│   ├── InvisibleBlock.prefab
│   ├── SlipperyFloor.prefab
│   ├── SpringTrap.prefab
│   └── HiddenSpikes.prefab
├── Items/
│   └── LightFragment.prefab
└── Environment/
    ├── WoodenCrate.prefab
    └── IllusionWall.prefab
```

### B. Template Cài Đặt Chung

**Mọi Trap Prefab cần có:**

1. **Layer:** "Traps" (tạo mới nếu chưa có)
2. **Tag:** "Trap" hoặc "Hazard"
3. **Collider:**
   - 2D Collider (Box/Circle)
   - IsTrigger: ✓ (với trigger traps)
   - Layer Collision: Va chạm với "Player"
4. **Audio Source (Optional):**
   - Play On Awake: ✗
   - Spatial Blend: 0.7 (2D/3D mix)

---

## IV. SETUP GAME OBJECTS ĐẶC BIỆT

### 1. Light Fragments (Mảnh Sáng)

**GameObject:**

```
LightFragment_01
├── SpriteRenderer (sprite sáng)
├── CircleCollider2D (IsTrigger: ✓)
├── ParticleSystem (ánh sáng lấp lánh)
└── LightFragment.cs (script nhặt đồ)
```

**Script LightFragment.cs** (tạo mới):

```csharp
using UnityEngine;

public class LightFragment : MonoBehaviour
{
    [SerializeField] private int fragmentID = 1;
    [SerializeField] private AudioClip collectSound;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Cộng vào inventory
            GameManager.Instance.CollectLightFragment(fragmentID);

            if (collectSound != null)
                AudioSource.PlayClipAtPoint(collectSound, transform.position);

            // Destroy với hiệu ứng
            Destroy(gameObject);
        }
    }
}
```

### 2. Warning Signs (Tấm Biển Cảnh Báo)

**Setup:**

1. Create → UI → Canvas (World Space)
2. Add TextMeshPro: "Hãy cẩn thận bước chân"
3. Font: Size 0.5, màu vàng
4. Đặt ngay trước FakeFloor

**Animation (Optional):**

- Thêm Animator với animation nhấp nháy

---

## V. TESTING & POLISH

### Checklist Testing

**Phòng Start:**

- [ ] Tấm biển hiển thị đúng
- [ ] FakeFloor rơi khi bước vào
- [ ] Người chơi rơi xuống hố (trigger death zone)
- [ ] Respawn về vị trí Start

**Hành Lang Vàng:**

- [ ] InvisibleBlock đập đầu người chơi khi nhảy
- [ ] Knockback đẩy xuống hố
- [ ] Nhảy sát tường thì không đụng block
- [ ] Mảnh Sáng nhặt được

**Phòng Boss:**

- [ ] Sàn trơn hoạt động (trượt liên tục)
- [ ] Va thùng gỗ thì dừng lại
- [ ] Trượt vào chuột → chuột tỉnh → tấn công
- [ ] Có thể dùng thùng để hãm phanh

**Khu Vực Cam:**

- [ ] SpringTrap bật người chơi bay ngược
- [ ] Âm thanh "BOING!" phát ra
- [ ] Người chơi mất quyền điều khiển khi bay
- [ ] Phải đi lại từ đầu

**Phòng Goal:**

- [ ] Đứng trước tường ảo → chông mọc
- [ ] Đứng xa 1 ô → bấm Space → tường biến mất
- [ ] Không bị chông khi đứng đúng vị trí

### Visual Polish

**Lighting:**

1. Add Light 2D (Global Light):
   - Intensity: 0.3 (tối một chút)
2. Add Point Light 2D cho Mảnh Sáng:
   - Radius: 3
   - Intensity: 1
   - Color: Vàng sáng

**Particle Effects:**

- Dust khi FakeFloor rơi
- Sparkle khi nhặt Mảnh Sáng
- Ice shards khi trượt trên sàn băng
- Stars khi đập đầu InvisibleBlock

**Camera:**

- Add Cinemachine Virtual Camera
- Follow Player
- Dead Zone: 0.1 x 0.1
- Camera Shake script (khi đập bẫy)

---

## VI. TIPS THIẾT KẾ LEVEL

### Nguyên Tắc Vàng

**1. "Unfair but Fair"**

- Luôn cho người chơi MỘT gợi ý nhỏ (màu sắc khác, vết nứt, âm thanh)
- Bẫy lần đầu có thể bất ngờ, nhưng lần sau phải tránh được

**2. "Punishment Progression"**

- Phòng đầu: Chết nhanh, respawn gần
- Phòng giữa: Chết + phải đi lại xa
- Phòng cuối: Chết + mất item + quay về checkpoint

**3. "False Sense of Security"**

- Cho vài ô an toàn trước khi bẫy
- Đặt item tốt ngay trước bẫy
- Dụ người chơi vào tình huống "quá dễ = đáng ngờ"

### Common Mistakes Cần Tránh

❌ **ĐỪNG:**

- Đặt bẫy instant-kill không có cảnh báo
- Làm người chơi phải đoán mò (phải có logic)
- Bẫy không thể tránh (impossible)
- Quá nhiều bẫy cùng lúc (overwhelming)

✅ **NÊN:**

- Test với người khác (không phải bạn)
- Ghi âm lại phản ứng của người chơi
- Nếu họ nổi giận rồi cười → Perfect!
- Nếu họ chỉ nổi giận → Quá khó

---

## VII. CAMERA SHAKE SCRIPT (BONUS)

Tạo file `CameraShake.cs`:

```csharp
using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(x, y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }
}
```

Attach vào Main Camera.

---

## VIII. EXPORT & FINAL CHECKLIST

### Pre-Export

- [ ] Tất cả Prefabs đã được Save
- [ ] Scene được Save
- [ ] Scripts không có lỗi compile
- [ ] Test chơi từ đầu đến cuối ít nhất 3 lần

### Build Settings

- [ ] Add Scene "TheAwakening" vào Build Settings
- [ ] Set làm Scene đầu tiên (index 0)

### Optimization

- [ ] Sprite Atlas cho MungeonDage tiles
- [ ] Object Pooling cho Particle Effects
- [ ] Disable unused Colliders

---

## LINK THAM KHẢO

**Similar Games cho Inspiration:**

- I Wanna Be The Guy
- Getting Over It
- Jump King
- Trap Adventure 2

**Unity Tutorials:**

- Tilemap 2D Workflow
- 2D Lighting
- Cinemachine Basics

---

**Chúc bạn thành công trong việc tạo ra một map "gây ức chế" nhưng đầy nghệ thuật! 🎮💀**

_P.S: Nhớ để test với bạn bè và ghi hình lại phản ứng của họ để đăng lên mạng. Đó mới là phần vui nhất!_ 😈
