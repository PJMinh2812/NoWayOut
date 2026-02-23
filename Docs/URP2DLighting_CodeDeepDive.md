# 💡 Hệ thống Ánh sáng URP 2D — Giải thích Code từng dòng

> Hệ thống lighting tạo bầu không khí horror: dungeon tối đen, player chỉ có đốm sáng nhỏ,
> nhặt Light Fragment → mở rộng tầm nhìn dần dần.

---

## Tổng quan kiến trúc

```
DungeonLightingManager (Singleton)
├── Global Light 2D  → intensity 0.005 (gần TỐI ĐEN)
├── Player Light 2D  → radius 2.5 units (sát người)
├── Start Room Light  → radius = room size (SÁng hoàn toàn)
└── Sprite → Lit Material conversion
         ↑
LightFragment (Item)               LightFragmentSpawner
├── Light2D nhấp nháy              ├── Fisher-Yates shuffle chọn phòng
├── Xoay + bay lên xuống           └── Spawn 3 fragments ở phòng Archetype
└── Thu thập → GameManager.CollectLightFragment()
         ↓
GameManager.OnLightFragmentCollected event
         ↓
DungeonLightingManager.ExpandPlayerLight()
├── radius += 1.25 mỗi fragment
├── Flash sáng 2x rồi fade về bình thường
└── SmoothStep animation 1 giây
```

---

## 📂 FILE 1: `DungeonLightingManager.cs` (464 dòng)

```
📁 Assets/Scripts/Core/DungeonLightingManager.cs
```

### Singleton Pattern

```csharp
public static DungeonLightingManager Instance { get; private set; }

private void Awake()
{
    // Chỉ cho phép 1 instance tồn tại
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);  // Destroy bản sao
        return;
    }
    Instance = this;  // Lưu reference tĩnh
}
```
**Tại sao Singleton?** Vì chỉ cần 1 lighting manager cho toàn bộ game. Các script khác truy cập qua `DungeonLightingManager.Instance` mà không cần `FindFirstObjectByType()`.

### Cấu hình — Tại sao những con số này?

```csharp
// DUNGEON TỐI ĐEN
float globalLightIntensity = 0.005f;  // Gần 0 → hầu như không thấy gì
Color globalLightColor = new Color(0.05f, 0.05f, 0.1f);  // Xanh đen rất nhạt

// PLAYER — Đốm sáng rất nhỏ sát người
float playerDefaultLightRadius = 2.5f;   // Chỉ thấy ≈1 ô xung quanh
float radiusPerFragment = 1.25f;          // Mỗi fragment tăng thêm 1.25
float playerMaxLightRadius = 6.5f;        // Tối đa sau 3 fragments
float playerLightIntensity = 0.8f;        // Không sáng 100% → vẫn có bóng tối
Color playerLightColor = new Color(0.9f, 0.85f, 0.7f);  // Vàng nhạt ấm (ngọn nến)
```

**Thiết kế horror**: Player chỉ thấy rất ít → tạo cảm giác sợ hãi vì không biết gì ở góc tối.

### SetupGlobalLight — Tạo bóng tối cho dungeon

```csharp
private void SetupGlobalLight()
{
    // Bước 1: Tìm Global Light đã tồn tại trong scene
    if (globalLight == null)
    {
        var existingLights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (var light in existingLights)
        {
            if (light.lightType == Light2D.LightType.Global)
            // Light2D.LightType.Global = chiếu sáng TẤT CẢ sprites
            // Khác với Point (hình tròn) hay Freeform (hình tùy chỉnh)
            {
                globalLight = light;
                break;
            }
        }
    }

    // Bước 2: Tạo mới nếu chưa có
    if (globalLight == null)
    {
        var globalLightObj = new GameObject("GlobalLight2D");
        globalLightObj.transform.SetParent(transform);
        globalLight = globalLightObj.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
    }

    // Bước 3: Set intensity cực thấp → dungeon TỐI ĐEN
    globalLight.intensity = globalLightIntensity;  // 0.005
    globalLight.color = globalLightColor;           // Xanh đen
}
```

**URP 2D Lighting hoạt động thế nào?**
```
Sprite-Default (unlit):  Luôn sáng 100%, KHÔNG bị ảnh hưởng bởi light
Sprite-Lit-Default:      Phản ứng với Light2D → bị tối/sáng theo light

Global Light intensity 0.005 + Sprite-Lit-Default:
→ TẤT CẢ sprites gần như đen hoàn toàn
→ Chỉ vùng có Point Light (player) mới sáng lên
```

### CreatePlayerLight — Đốm sáng theo player

```csharp
private void CreatePlayerLight()
{
    // Convert sprite player sang Lit material (BẮT BUỘC)
    var playerSR = playerObj.GetComponent<SpriteRenderer>();
    if (playerSR != null)
    {
        Material litMat = FindLitMaterial();
        // Kiểm tra: nếu material hiện tại KHÔNG phải Lit → đổi
        if (!playerSR.sharedMaterial.name.Contains("Lit"))
        {
            playerSR.sharedMaterial = litMat;
        }
    }

    // Tạo child object cho light (không add trực tiếp vào player)
    var lightObj = new GameObject("PlayerLight");
    lightObj.transform.SetParent(playerObj.transform);  // Gắn vào player
    lightObj.transform.localPosition = Vector3.zero;     // Đúng tâm player

    playerLight = lightObj.AddComponent<Light2D>();
    playerLight.lightType = Light2D.LightType.Point;  // Hình tròn

    // Outer radius = vùng sáng tối đa
    playerLight.pointLightOuterRadius = playerDefaultLightRadius;  // 2.5
    // Inner radius = vùng sáng 100% (lõi)
    playerLight.pointLightInnerRadius = playerDefaultLightRadius * 0.2f;  // 0.5

    playerLight.intensity = 0.8f;
    playerLight.color = playerLightColor;  // Vàng ấm

    // Shadow = bóng đổ từ vật thể
    playerLight.shadowIntensity = 0.9f;    // Bóng ĐẬM → horror
    // Falloff = tốc độ giảm sáng từ lõi → rìa
    playerLight.falloffIntensity = 0.8f;   // Giảm NHANH → vùng sáng nhỏ hơn nữa
}
```

**Minh họa Inner vs Outer Radius**:
```
     ████████████████████████
   ██                        ██      ← Outer (2.5): ánh sáng giảm dần
  █    ░░░░░░░░░░░░░░░░░░     █
 █   ░░                  ░░    █
█   ░░    ▓▓▓▓▓▓▓▓▓▓▓    ░░   █
█   ░░    ▓ PLAYER  ▓    ░░   █     ← Inner (0.5): sáng 100%
█   ░░    ▓▓▓▓▓▓▓▓▓▓▓    ░░   █
 █   ░░                  ░░    █     ░ = falloff zone (sáng dần giảm)
  █    ░░░░░░░░░░░░░░░░░░     █
   ██                        ██
     ████████████████████████
                                     TỐI ĐEN bên ngoài outer radius
```

### Start Room Light — Phòng đầu sáng hoàn toàn

```csharp
private bool TrySetupStartRoomLight()
{
    // Tìm DungeonManager → lấy danh sách rooms → tìm Start room
    var dungeonManager = FindFirstObjectByType<DungeonManager>();
    var allRooms = dungeonManager.GetAllRooms();

    Room startRoom = null;
    foreach (var room in allRooms)
    {
        if (room.roomData.roomType == RoomType.Start)
        {
            startRoom = room;
            break;
        }
    }

    // Tính kích thước phòng từ renderer bounds
    float roomRadius = 10f;
    var renderers = startRoom.roomInstance.GetComponentsInChildren<Renderer>(true);
    if (renderers.Length > 0)
    {
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);  // Gộp tất cả bounds

        // Radius = max(width, height) × 0.75 → bao phủ cả phòng
        roomRadius = Mathf.Max(bounds.size.x, bounds.size.y) * 0.75f;
    }

    // Tạo Point Light lớn cho phòng Start
    var roomLight = lightObj.AddComponent<Light2D>();
    roomLight.lightType = Light2D.LightType.Point;
    roomLight.pointLightOuterRadius = roomRadius;       // Bao phủ cả phòng
    roomLight.pointLightInnerRadius = roomRadius * 0.7f; // 70% sáng đều
    roomLight.intensity = 1.2f;                          // Sáng hơn bình thường
    roomLight.shadowIntensity = 0f;  // KHÔNG đổ bóng → feel an toàn
}
```

**Tại sao Start Room sáng?** Đây là "safe zone" — player bắt đầu ở đây, cần thấy rõ để hiểu cách chơi. Khi rời khỏi → bước vào bóng tối → horror effect bắt đầu.

### Fragment Collection → Expand Light

```csharp
private void OnFragmentCollected(int current, int total)
{
    fragmentsCollected = current;
    ExpandPlayerLight();
}

public void ExpandPlayerLight()
{
    // Tính radius mới: default + (số fragment × bonus mỗi fragment)
    float targetRadius = Mathf.Min(
        playerDefaultLightRadius + fragmentsCollected * radiusPerFragment,
        //        2.5           +       1           ×       1.25     = 3.75
        //        2.5           +       2           ×       1.25     = 5.0
        //        2.5           +       3           ×       1.25     = 6.25
        playerMaxLightRadius  // Clamp tối đa = 6.5
    );

    StartCoroutine(AnimateRadiusExpand(targetRadius));
}
```

**Bảng tầm nhìn theo fragments**:

| Fragments | Radius | Tầm nhìn |
|-----------|--------|----------|
| 0 | 2.5 | ≈1 ô xung quanh (rất tối) |
| 1 | 3.75 | ≈1.5 ô |
| 2 | 5.0 | ≈2 ô |
| 3 | 6.25 | ≈2.5 ô (tối đa) |

### AnimateRadiusExpand — Animation mượt mà

```csharp
private IEnumerator AnimateRadiusExpand(float targetRadius)
{
    float startRadius = playerLight.pointLightOuterRadius;  // Radius hiện tại
    float duration = 1f;  // 1 giây animation
    float elapsed = 0f;

    // ① FLASH: Sáng gấp đôi tức thì (hiệu ứng "wow")
    float originalIntensity = playerLight.intensity;
    playerLight.intensity = originalIntensity * 2f;  // 0.8 → 1.6

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        // SmoothStep: 0→1 nhưng CHẬM ở đầu và cuối, NHANH ở giữa
        // Tạo cảm giác "organic" thay vì linear (đều đều)
        float smooth = Mathf.SmoothStep(0f, 1f, t);

        // ② EXPAND: Radius tăng dần (SmoothStep)
        playerLight.pointLightOuterRadius = Mathf.Lerp(startRadius, targetRadius, smooth);

        // ③ FADE: Intensity giảm từ 2x về 1x (linear)
        playerLight.intensity = Mathf.Lerp(originalIntensity * 2f, originalIntensity, t);

        yield return null;  // Chờ next frame
    }

    // Đảm bảo giá trị cuối chính xác
    playerLight.pointLightOuterRadius = targetRadius;
    playerLight.intensity = originalIntensity;
}
```

**SmoothStep vs Lerp**:
```
Lerp (tuyến tính):     |▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬|  Đều đều, máy móc
SmoothStep:            |▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬|  Chậm-Nhanh-Chậm, tự nhiên
                       start              end
```

### ConvertSpritesToLitMaterial — Bước BẮT BUỘC

```csharp
private IEnumerator ConvertSpritesToLitMaterial()
{
    // Đợi scene load xong (3 frames)
    yield return null; yield return null; yield return null;

    Material litMaterial = FindLitMaterial();
    // Tìm "Sprite-Lit-Default" bằng 3 cách:
    //   1. Resources.Load("Sprite-Lit-Default")
    //   2. Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")
    //   3. Shader.Find("Sprites/Lit")

    // Convert TẤT CẢ SpriteRenderer
    var allRenderers = FindObjectsByType<SpriteRenderer>(
        FindObjectsInactive.Include,  // Bao gồm cả inactive objects
        FindObjectsSortMode.None
    );
    foreach (var sr in allRenderers)
    {
        // Chỉ convert nếu đang dùng "Sprites-Default" (unlit)
        if (sr.sharedMaterial.name.Contains("Default") 
            && !sr.sharedMaterial.name.Contains("Lit"))
        {
            sr.sharedMaterial = litMaterial;
        }
    }

    // Convert TilemapRenderer cũng vậy
    var tilemapRenderers = FindObjectsByType<TilemapRenderer>(...);
    // Tương tự...
}
```

**Tại sao phải convert?** Đây là điểm HAY QUÊN nhất:
```
Sprite-Default + Light2D intensity 0 = Sprite VẪN SÁNG 100% (vô dụng!)
Sprite-Lit-Default + Light2D intensity 0 = Sprite TỐI ĐEN (đúng ý!)
```

---

## 📂 FILE 2: `LightFragment.cs` (213 dòng)

```
📁 Assets/Scripts/Items/LightFragment.cs
```

### Hiệu ứng visual — Xoay + Bay + Nhấp nháy

```csharp
private void Update()
{
    if (isCollected) return;  // Đã nhặt → dừng animation

    // ① XOAY liên tục quanh trục Z (2D rotation)
    transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
    // rotationSpeed = 50 → xoay 50°/giây

    // ② BAY LÊN XUỐNG (sine wave)
    float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
    // Sin(time × 2) × 0.3 → dao động ±0.3 units, chu kỳ ~3 giây
    transform.position = new Vector3(transform.position.x, newY, transform.position.z);

    // ③ NHẤP NHÁY ánh sáng (URP Light2D pulse)
    if (itemLight != null)
    {
        // Sin → giá trị từ -1 đến 1 → chuyển thành 0 đến 1 bằng (+1)/2
        float intensity = Mathf.Lerp(lightMinIntensity, lightMaxIntensity,
            (Mathf.Sin(Time.time * lightPulseSpeed) + 1f) / 2f);
        // Kết quả: intensity dao động giữa 0.5 và 1.2
        itemLight.intensity = intensity;
    }
}
```

### CollectFragment — Khi player nhặt

```csharp
private void CollectFragment(GameObject player)
{
    isCollected = true;

    // ① Thông báo GameManager (central event system)
    GameManager.Instance.CollectLightFragment(fragmentID);
    // → GameManager raise OnLightFragmentCollected event
    // → DungeonLightingManager.ExpandPlayerLight() được gọi
    // → MinimapManager.OnFragmentCollected() được gọi
    // → LightFragmentUI.UpdateDisplay() được gọi

    // ② Particle effect tại vị trí nhặt
    if (collectEffect != null)
        Instantiate(collectEffect, transform.position, Quaternion.identity);

    // ③ Âm thanh
    AudioSource.PlayClipAtPoint(collectSound, transform.position);
    // PlayClipAtPoint tạo temporary AudioSource → tự destroy

    // ④ Animation thu nhỏ + bay lên + fade out → Destroy
    StartCoroutine(CollectAnimation());
}
```

### CollectAnimation — Bay lên và biến mất

```csharp
private IEnumerator CollectAnimation()
{
    Vector3 startScale = transform.localScale;
    Vector3 targetPos = transform.position + Vector3.up * 2f;  // Bay lên 2 units
    float duration = 0.5f;

    while (elapsed < duration)
    {
        float t = elapsed / duration;  // 0 → 1

        // Scale LÊN (phóng to 1.5x)
        transform.localScale = Vector3.Lerp(startScale, startScale * 1.5f, t);

        // Bay LÊN
        transform.position = Vector3.Lerp(transform.position, targetPos, t);

        // Fade OUT (alpha giảm từ 1 → 0)
        Color color = sprite.color;
        color.a = 1f - t;
        sprite.color = color;

        yield return null;
    }

    Destroy(gameObject);  // Xóa hoàn toàn
}
```

---

## 📂 FILE 3: `LightFragmentSpawner.cs` (198 dòng)

```
📁 Assets/Scripts/Items/LightFragmentSpawner.cs
```

### Thuật toán spawn — Chọn phòng ngẫu nhiên

```csharp
public void SpawnFragmentsInDungeon(DungeonManager dungeonManager)
{
    var allRooms = dungeonManager.GetAllRooms();

    // ① LỌC: chỉ spawn trong Archetype1 và Archetype2
    // KHÔNG spawn trong Start, Boss, Goal (quá dễ/quá khó)
    var eligibleRooms = new List<Room>();
    foreach (var room in allRooms)
    {
        foreach (var allowedType in spawnInRoomTypes)
        {
            if (room.roomData.roomType == allowedType)
            {
                eligibleRooms.Add(room);
                break;
            }
        }
    }

    // ② SHUFFLE: Fisher-Yates → chọn 3 phòng ngẫu nhiên không trùng
    int count = Mathf.Min(totalFragments, eligibleRooms.Count);
    // Shuffle toàn bộ list, lấy 3 phần tử đầu tiên

    // ③ SPAWN: 1 fragment mỗi phòng được chọn
    foreach (var room in selectedRooms)
        SpawnFragmentInRoom(room);
}
```

### SpawnFragmentInRoom — Vị trí random trong phòng

```csharp
private void SpawnFragmentInRoom(Room room)
{
    // Tính vùng interior (tránh wall)
    float margin = 2.5f;  // Cách wall 2.5 units

    // Dùng renderer bounds để tính chính xác kích thước thực tế
    Bounds bounds = renderers[0].bounds;
    foreach (var r in renderers)
        bounds.Encapsulate(r.bounds);

    // Random vị trí BÊN TRONG phòng, cách wall 2.5 units
    float x = Random.Range(bounds.min.x + margin, bounds.max.x - margin);
    float y = Random.Range(bounds.min.y + margin, bounds.max.y - margin);

    // Instantiate fragment (hoặc tạo runtime nếu không có prefab)
    GameObject fragmentObj = lightFragmentPrefab != null
        ? Instantiate(lightFragmentPrefab, spawnPos, Quaternion.identity, room.roomInstance.transform)
        : CreateRuntimeFragment(spawnPos, room.roomInstance.transform);
    // Parent = room → khi room deactivate, fragment cũng ẩn
}
```

### CreateRuntimeFragment — Tạo diamond sprite bằng code

```csharp
private GameObject CreateRuntimeFragment(Vector3 position, Transform parent)
{
    // Tạo texture 16×16 hình kim cương
    Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
    for (int px = 0; px < 16; px++)
    for (int py = 0; py < 16; py++)
    {
        int dx = Mathf.Abs(px - 8);  // Khoảng cách X từ center (8,8)
        int dy = Mathf.Abs(py - 8);  // Khoảng cách Y từ center
        if (dx + dy <= 7)
            // Manhattan distance ≤ 7 → NẰM TRONG hình kim cương
            pixels[py * 16 + px] = new Color32(255, 240, 150, 255);  // Vàng
        else
            pixels[py * 16 + px] = new Color32(0, 0, 0, 0);  // Trong suốt
    }
}
```

**Hình kim cương 16×16** (dx + dy ≤ 7):
```
        ●
       ●●●
      ●●●●●
     ●●●●●●●
    ●●●●●●●●●
     ●●●●●●●
      ●●●●●
       ●●●
        ●
```

---

## Danh sách file hệ thống Lighting

| File | Dòng | Vai trò |
|------|------|---------|
| `Core/DungeonLightingManager.cs` | 464 | Singleton quản lý Global/Player/Room lights, sprite conversion |
| `Items/LightFragment.cs` | 213 | Item nhặt được: xoay + bay + nhấp nháy, expand player vision |
| `Items/LightFragmentSpawner.cs` | 198 | Auto-spawn 3 fragments vào phòng Archetype |
| `Editor/LightFragmentEditor.cs` | ~50 | Custom Inspector cho LightFragment |

---

## 10 Câu hỏi Review — Hệ thống Ánh sáng

**Q1: Tại sao Global Light intensity = 0.005 thay vì 0?**
> 0.005 cho phép player vẫn _hơi_ thấy outline tường/sàn, tạo cảm giác "có gì đó trong bóng tối" thay vì đen hoàn toàn (chán). Giá trị 0 = player không thấy GÌ → kém gameplay.

**Q2: Sprite-Default vs Sprite-Lit-Default — khác biệt quan trọng nhất?**
> Sprite-Default luôn sáng 100% bất kể light. Sprite-Lit-Default phản ứng với Light2D. Nếu quên convert → lighting vô tác dụng, sprites sáng trưng dù Global Light = 0.

**Q3: shadowIntensity = 0.9 trên player light có ý nghĩa gì?**
> Bóng đậm (0.9/1.0) → vật thể chắn sáng tạo bóng tối rõ rệt. Kẻ thù đứng sau cột → player không thấy → horror effect. Nếu 0 → không có bóng → mất chiều sâu.

**Q4: SmoothStep vs Lerp trong AnimateRadiusExpand?**
> SmoothStep tạo hiệu ứng ease-in-out (chậm-nhanh-chậm), tự nhiên hơn Lerp (tuyến tính đều). Player cảm nhận ánh sáng "nở ra" organic thay vì mechanical.

**Q5: Tại sao Start Room có shadowIntensity = 0?**
> Start Room là safe zone — không cần tạo bóng horror. Player cần thấy RÕ mọi thứ để hiểu controls. Bỏ bóng → feel an toàn, tương phản mạnh khi rời phòng.

**Q6: Tại sao Light Fragment dùng Light2D.Point thay vì Global?**
> Point = hình tròn cục bộ → chỉ sáng quanh fragment, tạo "beacon" dẫn player. Global = sáng toàn scene → mất hiệu ứng, player không biết fragment ở đâu.

**Q7: falloffIntensity = 0.8 nghĩa gì?**
> Ánh sáng giảm RẤT NHANH từ inner → outer radius. 0.8 = 80% energy mất đi ở vùng falloff. Kết quả: vùng sáng thực tế _nhỏ hơn_ outer radius rất nhiều → player luôn cảm thấy "thiếu sáng".

**Q8: ConvertSpritesToLitMaterial chạy sau 3 frame — tại sao không chạy ngay?**
> yield return null × 3 = đợi 3 frame. Lý do: DungeonManager và RoomVisualGenerator cần thời gian instantiate rooms và tilemaps. Nếu chạy ngay → nhiều sprites chưa tồn tại → bị bỏ sót.

**Q9: Fragment spawn chỉ ở Archetype1/Archetype2 — tại sao?**
> Start Room quá dễ (nhặt ngay đầu game → không có progression). Boss Room quá nguy hiểm. Archetype = phòng thường, buộc player explore trước khi "mạnh lên".

**Q10: Khi nhặt fragment, intensity flash 2× rồi fade — tên kỹ thuật gì?**
> **Juice / Game Feel technique**: flash sáng tức thì → tạo cảm giác "reward" mạnh, dopamine hit. Fade about 1s → smooth transition. Nếu chỉ tăng radius mà không flash → player có thể không nhận ra thay đổi.
