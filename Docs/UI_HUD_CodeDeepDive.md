# 🖥️ UI/HUD System — Giải thích Code từng dòng

> Bao gồm: Spell Hotbar (4 slots), Minimap hình tròn (RenderTexture), Hotbar tích hợp ánh sáng.

---

## Tổng quan kiến trúc

```
Canvas (ScreenSpaceOverlay)
├── SpellHotbarUI (bottom-center)
│   └── SpellHotbarSlot × 4 (Idle, Spell1, Spell2, Spell3)
│       ├── Icon Image
│       ├── Selection Border
│       ├── Cooldown Overlay (radial fill)
│       └── KeyBind Text (0, 1, 2, 3)
│
├── LightingHotbarUI (bottom-right)
│   └── Slot × 5 (items, tích hợp glow)
│       ├── Background (dark horror theme)
│       ├── Glow overlay (syncs with player light)
│       ├── Icon Image
│       └── KeyBind Text (1–5)
│
├── LightFragmentUI (top-right)
│   ├── Fragment Counter ("✦ 2/3")
│   └── Notification popup
│
├── FlashOfTruthUI (bottom-left)
│   ├── Radial cooldown fill
│   ├── Key hint "[SPACE]"
│   └── Status text (LOCKED/READY/ACTIVE/cooldown)
│
└── MinimapManager (top-right, offset below fragment counter)
    ├── Minimap Camera (orthographic, RenderTexture)
    ├── Minimap Light (Point, follows camera)
    ├── Circle Mask + RawImage
    ├── Player Marker (cyan dot + glow)
    ├── Radar Sweep Line (rotating)
    └── Compass Labels (N/S/E/W)
```

---

## 📂 FILE 1: `SpellHotbarUI.cs` (186 dòng)

```
📁 Assets/Scripts/UI/SpellHotbarUI.cs
```

### Khởi tạo 4 Slots

```csharp
public sealed class SpellHotbarUI : MonoBehaviour
{
    [SerializeField] private PlayerSpellController spellController;
    [SerializeField] private SpellHotbarSlot[] spellSlots;  // 4 slots

    // Icons cho mỗi slot
    [SerializeField] private Sprite idleIcon;     // Slot 0: không spell
    [SerializeField] private Sprite spell01Icon;  // Slot 1
    [SerializeField] private Sprite spell02Icon;  // Slot 2
    [SerializeField] private Sprite spell03Icon;  // Slot 3

    [SerializeField] private string[] keyLabels = { "0", "1", "2", "3" };

    private void Start()
    {
        InitializeSlots();
        UpdateSelection(0);  // Bắt đầu = Idle (slot 0)
    }

    private void InitializeSlots()
    {
        // Gọi Initialize() cho từng slot
        spellSlots[0].Initialize(0, idleIcon, "0", "Idle");
        spellSlots[1].Initialize(1, spell01Icon, "1", "Spell 1");
        spellSlots[2].Initialize(2, spell02Icon, "2", "Spell 2");
        spellSlots[3].Initialize(3, spell03Icon, "3", "Spell 3");
    }
}
```

**sealed class**: Không cho phép kế thừa. SpellHotbarUI là final — không ai extend được. Best practice cho MonoBehaviour không cần polymorphism.

### Update — Sync với PlayerSpellController

```csharp
private void Update()
{
    if (spellController == null) return;

    // Lấy spell hiện tại từ PlayerSpellController (0–3)
    int currentSpell = spellController.CurrentSpell;
    UpdateSelection(currentSpell);  // Highlight slot tương ứng

    UpdateCooldowns();  // Cập nhật cooldown overlay
}

private void UpdateSelection(int selectedSpell)
{
    for (int i = 0; i < spellSlots.Length; i++)
    {
        // TRUE chỉ cho slot đang selected
        spellSlots[i].SetSelected(i == selectedSpell);
    }
}

private void UpdateCooldowns()
{
    spellSlots[0].SetCooldown(0f);  // Idle KHÔNG có cooldown

    for (int i = 1; i <= 3; i++)
    {
        // GetSpellCooldownPercent: 0 = ready, 1 = full cooldown
        float cooldownPercent = spellController.GetSpellCooldownPercent(i);
        spellSlots[i].SetCooldown(cooldownPercent);
    }
}
```

---

## 📂 FILE 2: `SpellHotbarSlot.cs` (124 dòng)

```
📁 Assets/Scripts/UI/SpellHotbarSlot.cs
```

### UI References

```csharp
public sealed class SpellHotbarSlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;          // Icon spell
    [SerializeField] private Image backgroundImage;    // Nền slot
    [SerializeField] private Image selectionBorder;    // Viền highlight
    [SerializeField] private Image cooldownOverlay;    // Overlay tối khi cooldown
    [SerializeField] private TextMeshProUGUI keyBindText;   // "0", "1", "2", "3"
    [SerializeField] private TextMeshProUGUI spellNameText; // "Idle", "Spell 1"...

    // Màu sắc horror theme
    Color normalBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);    // Xám tối
    Color selectedBackgroundColor = new Color(0.4f, 0.6f, 1f, 1f);      // Xanh sáng
    Color normalBorderColor = new Color(0.5f, 0.5f, 0.5f, 1f);          // Xám
    Color selectedBorderColor = new Color(1f, 1f, 0f, 1f);              // Vàng
    Color cooldownColor = new Color(0f, 0f, 0f, 0.7f);                   // Đen bán trong suốt
}
```

### SetCooldown — Radial Fill overlay

```csharp
public void SetCooldown(float cooldownPercent)
{
    if (cooldownOverlay != null)
    {
        if (cooldownPercent > 0f)
        {
            cooldownOverlay.enabled = true;
            cooldownOverlay.color = cooldownColor;     // Đen 70% opacity
            cooldownOverlay.fillAmount = cooldownPercent;
            // fillAmount: 0 = ẩn hoàn toàn, 1 = phủ hết
            // Image.Type phải = Filled, FillMethod = Radial360
            // → cooldown kiểu clock quay (giống MOBA)
        }
        else
        {
            cooldownOverlay.enabled = false;  // Hết cooldown → ẩn overlay
        }
    }
}
```

**Minh họa fillAmount**:
```
fillAmount = 0.0   fillAmount = 0.5   fillAmount = 1.0
┌──────────┐       ┌──────────┐       ┌──────────┐
│          │       │████████░░│       │██████████│
│   ICON   │       │████ICON░░│       │██LOCKED██│
│          │       │████████░░│       │██████████│
└──────────┘       └──────────┘       └──────────┘
  READY              50% CD            FULL CD
```

---

## 📂 FILE 3: `MinimapManager.cs` (465 dòng)

```
📁 Assets/Scripts/UI/MinimapManager.cs
```

### RenderTexture — Camera thứ hai

```csharp
public class MinimapManager : MonoBehaviour
{
    [SerializeField] private float minimapDiameter = 200f;
    [SerializeField] private int renderTextureSize = 512;   // 512×512 pixels
    [SerializeField] private float cameraOrthoSize = 25f;   // Bao nhiêu world area visible

    private Camera minimapCamera;
    private RenderTexture minimapRT;
    private Light2D minimapLight;

    private void CreateRenderTexture()
    {
        minimapRT = new RenderTexture(
            renderTextureSize,       // Width = 512
            renderTextureSize,       // Height = 512

            24,                      // Depth buffer = 24 bits
            // 24-bit depth: cần cho URP 2D rendering pipeline

            RenderTextureFormat.ARGB32  // 4 channels, 8 bits each
        );
        minimapRT.antiAliasing = 2;     // 2× MSAA → smooth edges
        minimapRT.filterMode = FilterMode.Bilinear;  // Smooth khi scale
        minimapRT.Create();  // Cấp phát GPU memory
    }
}
```

**RenderTexture là gì?**
```
Camera thông thường → màn hình
Camera + RenderTexture → "vẽ" vào texture thay vì màn hình
→ Texture này hiển thị trên RawImage (UI element)
→ = "Màn hình TV trong game"

Main Camera:    Scene → Screen
Minimap Camera: Scene → RenderTexture → RawImage (UI)
```

### CreateMinimapCamera — Camera nhìn từ trên xuống

```csharp
private void CreateMinimapCamera()
{
    var camObj = new GameObject("MinimapCamera");
    camObj.transform.position = new Vector3(playerPos.x, playerPos.y, -50f);
    // Z = -50: rất xa phía TRƯỚC (2D) → nhìn thấy tất cả

    minimapCamera = camObj.AddComponent<Camera>();
    minimapCamera.orthographic = true;           // 2D view (không perspective)
    minimapCamera.orthographicSize = cameraOrthoSize;  // 25 → nhìn 50 units chiều cao
    minimapCamera.targetTexture = minimapRT;      // ★ Render vào texture, KHÔNG ra screen
    minimapCamera.clearFlags = CameraClearFlags.SolidColor;
    minimapCamera.backgroundColor = new Color(0.02f, 0.02f, 0.04f);  // Gần đen
    minimapCamera.depth = -10;                    // Render TRƯỚC main camera
    minimapCamera.cullingMask = ~(1 << 5);       // Tất cả TRỪU UI layer (5)
    // ~(1 << 5) = bitwise NOT of layer 5 = render everything except UI

    // URP: camera phải có component này
    var urpData = camObj.AddComponent<UniversalAdditionalCameraData>();
    urpData.renderType = CameraRenderType.Base;
    // Base = render độc lập (không phải Overlay camera)

    CreateMinimapLight(camObj.transform);
}
```

**Tại sao cullingMask loại bỏ UI?** Nếu không → minimap camera render cả UI elements → minimap hiện bên trong chính nó → infinite recursion visual.

### CreateMinimapLight — Ánh sáng riêng cho minimap

```csharp
private void CreateMinimapLight(Transform parent)
{
    // ★ QUAN TRỌNG: Dùng Point light, KHÔNG dùng Global
    minimapLight = lightObj.AddComponent<Light2D>();
    minimapLight.lightType = Light2D.LightType.Point;  // Cục bộ
    minimapLight.pointLightOuterRadius = cameraOrthoSize * 2.5f;  // 62.5 units
    minimapLight.pointLightInnerRadius = cameraOrthoSize * 1.5f;  // 37.5 units
    minimapLight.intensity = 0.35f;
    minimapLight.color = new Color(0.3f, 0.35f, 0.5f);  // Xanh dương nhạt
    minimapLight.falloffIntensity = 0.3f;  // Giảm dần ở rìa
}
```

**Tại sao Point thay vì Global?**
```
Global Light → ảnh hưởng TẤT CẢ cameras (cả main camera!)
→ Main camera cũng sáng thêm → PHÁ VỠ horror darkness

Point Light (attached to minimap camera):
→ Chỉ sáng vùng minimap camera nhìn thấy
→ Main camera KHÔNG bị ảnh hưởng
→ Minimap sáng đủ để nhìn map, main view vẫn TỐI
```

### CreateMinimapUI — Tạo UI hoàn toàn bằng code

```csharp
private void CreateMinimapUI()
{
    // ── CANVAS (root UI) ──
    minimapCanvas = canvasObj.AddComponent<Canvas>();
    minimapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
    minimapCanvas.sortingOrder = 100;  // Trên cùng, che tất cả

    var scaler = canvasObj.AddComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920, 1080);
    // ScaleWithScreenSize: UI auto scale theo resolution
    // Reference 1920×1080 → design tại Full HD, auto scale khác

    // ── FRAME (container chính, góc trên-phải) ──
    var frameRT = AnchorTopRight(frameObj, d, d, minimapOffset);
    // AnchorTopRight: anchor (1,1), pivot (1,1), offset (-16,-16)
    var frameBg = frameObj.AddComponent<Image>();
    frameBg.sprite = CreateCircleSprite(128);  // Hình tròn!
    frameBg.color = frameBgColor;  // Gần đen, 92% opacity

    // ── CIRCLE MASK (clip content thành hình tròn) ──
    var maskImg = maskObj.AddComponent<Image>();
    maskImg.sprite = CreateCircleSprite(128);
    var mask = maskObj.AddComponent<Mask>();
    mask.showMaskGraphic = false;  // Ẩn mask image, chỉ giữ clip effect
    // Mask: TẤT CẢ children bên trong sẽ bị crop theo hình tròn
```

**Mask hoạt động thế nào?**
```
Không có Mask:        Có Circle Mask:
┌──────────────┐      ┌──────────────┐
│ ╔══════════╗ │      │   .--"""--,  │
│ ║  MAP     ║ │      │  /  MAP    \ │
│ ║  RENDER  ║ │  →   │ |  RENDER  | │  ← Cắt theo hình tròn
│ ║  TEXTURE ║ │      │  \        /  │
│ ╚══════════╝ │      │   '--___--'  │
└──────────────┘      └──────────────┘
     Vuông                 Tròn
```

```csharp
    // ── MAP VIEW (RawImage hiển thị RenderTexture) ──
    mapRawImage = rawObj.AddComponent<RawImage>();
    mapRawImage.texture = minimapRT;  // ★ Gán RenderTexture
    // RawImage: hiển thị texture thô (khác Image dùng Sprite)
    // Vì RenderTexture KHÔNG phải Sprite → dùng RawImage

    // ── PLAYER MARKER (chấm cyan ở giữa) ──
    playerMarkerImage = markerObj.AddComponent<Image>();
    playerMarkerImage.sprite = CreateCircleSprite(64);
    playerMarkerImage.color = playerMarkerColor;  // Cyan sáng
    // Anchor (0.5, 0.5) → luôn ở GIỮA minimap
    // Camera follow player → player luôn ở giữa

    // ── MARKER GLOW (hào quang xung quanh marker) ──
    glowMarkerRT.sizeDelta = new Vector2(
        playerMarkerSize * 2.5f,  // 2.5× lớn hơn marker
        playerMarkerSize * 2.5f
    );
    glowMarkerImg.color = new Color(r, g, b, 0.15f);  // Alpha 15% → mờ nhạt

    // ── RADAR SWEEP (đường quét xoay tròn) ──
    sweepRect.pivot = new Vector2(0.5f, 0f);  // Pivot ở ĐÁY
    // Pivot ở đáy → xoay quanh TÂM minimap
    sweepRect.sizeDelta = new Vector2(2f, d * 0.45f);  // Mỏng, dài nửa đường kính
    sweepImg.color = new Color(0.4f, 0.6f, 1f, 0.08f);  // Rất mờ
```

### Radar Sweep Animation

```csharp
private void Update()
{
    // Xoay sweep line liên tục
    if (sweepRect != null)
        sweepRect.Rotate(0, 0, -25f * Time.deltaTime);
    // -25°/giây → xoay kim đồng hồ
    // 360° / 25° = 14.4 giây mỗi vòng
}
```

### CreateCircleSprite — Tạo hình tròn bằng code

```csharp
private Sprite CreateCircleSprite(int res)
{
    if (_cachedCircle != null) return _cachedCircle;  // Cache!

    var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
    float c = res * 0.5f;  // Center = 64 (nếu res=128)
    float r = c - 1f;       // Radius = 63

    for (int y = 0; y < res; y++)
    for (int x = 0; x < res; x++)
    {
        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
        // Distance từ pixel đến center

        tex.SetPixel(x, y, new Color(1, 1, 1,
            Mathf.Clamp01(r - dist + 0.5f)  // Anti-aliasing!
        ));
        // dist < r → alpha ≈ 1 (bên trong)
        // dist > r → alpha ≈ 0 (bên ngoài)
        // dist ≈ r → alpha = 0.5 (edge → smooth!)
    }

    tex.Apply();
    _cachedCircle = Sprite.Create(tex, new Rect(0,0,res,res), new Vector2(0.5f,0.5f), 100f);
    return _cachedCircle;
}
```

**Anti-aliasing tại edge**: `r - dist + 0.5` tạo gradient 1 pixel → edge mượt, không bị "răng cưa".

### Fragment-aware Minimap

```csharp
public void OnFragmentCollected(int totalFragments)
{
    if (minimapLight != null)
    {
        // Mỗi fragment → minimap SÁNG hơn và rộng hơn
        minimapLight.intensity = minimapLightIntensity + totalFragments * 0.15f;
        // 0 fragment: 0.35
        // 1 fragment: 0.50
        // 3 fragment: 0.80 → gần sáng rõ

        minimapLight.pointLightOuterRadius = cameraOrthoSize * 2.5f + totalFragments * 5f;
        // 0 fragment: 62.5 (vùng xung quanh player)
        // 3 fragment: 77.5 (rộng hơn nhiều)
    }
}

public void RevealAllRooms()
{
    // Full brightness — khi nhặt đủ 3 fragments
    minimapLight.intensity = 1.0f;
    minimapLight.pointLightOuterRadius = cameraOrthoSize * 4f;  // 100 units
}
```

---

## 📂 FILE 4: `LightingHotbarUI.cs` (410 dòng)

```
📁 Assets/Scripts/UI/LightingHotbarUI.cs
```

### Tích hợp ánh sáng — Glow syncs với fragments

```csharp
public class LightingHotbarUI : MonoBehaviour
{
    // Horror theme colors
    Color baseSlotColor = new Color(0.08f, 0.08f, 0.12f, 0.85f);      // Gần đen
    Color selectedSlotColor = new Color(0.9f, 0.85f, 0.6f, 0.95f);    // Vàng ấm (như nến)
    Color glowColor = new Color(0.9f, 0.85f, 0.7f, 0.4f);             // Ánh vàng mờ

    float baseGlowIntensity = 0.1f;   // Khi chưa có fragment: glow yếu
    float maxGlowIntensity = 0.6f;    // Khi đủ 3 fragments: glow mạnh
    float glowPulseSpeed = 1.5f;       // Nhấp nháy 1.5 lần/giây
}
```

### CreateSlot — Cấu trúc 1 slot

```csharp
private void CreateSlot(Transform parent, int index)
{
    // ① Background (nền đen horror)
    slotBackgrounds[index] = slotObj.AddComponent<Image>();
    slotBackgrounds[index].color = baseSlotColor;

    // ② Glow overlay (ánh sáng phản ánh player light)
    slotGlows[index] = glowObj.AddComponent<Image>();
    slotGlows[index].color = new Color(r, g, b, 0f);  // Alpha 0 = ẩn lúc đầu
    // Glow image LỚRN hơn slot: offset (-3, -3) → (3,3) = tràn 3px mỗi bên
    glowRect.offsetMin = new Vector2(-3f, -3f);
    glowRect.offsetMax = new Vector2(3f, 3f);

    // ③ Icon (item image, ban đầu ẩn)
    slotIcons[index] = iconObj.AddComponent<Image>();
    slotIcons[index].enabled = false;  // Chưa có item → ẩn
    // Padding 6px mỗi bên (icon nhỏ hơn slot)
    iconRect.offsetMin = new Vector2(6f, 6f);
    iconRect.offsetMax = new Vector2(-6f, -6f);

    // ④ KeyBind text (số 1-5 góc trên-trái)
    slotKeyBinds[index].text = $"{index + 1}";  // "1", "2", "3", "4", "5"
    slotKeyBinds[index].fontSize = 10f;
    slotKeyBinds[index].color = new Color(0.5f, 0.5f, 0.6f, 0.5f);  // Mờ nhạt
}
```

**Minh họa cấu trúc 1 slot**:
```
┌───────────────────────┐ ← Glow overlay (tràn 3px, alpha = glow level)
│ ┌───────────────────┐ │
│ │1                  │ │ ← KeyBind "1" (góc trên-trái)
│ │                   │ │
│ │    ┌─────────┐    │ │
│ │    │  ICON   │    │ │ ← Item icon (padding 6px)
│ │    │         │    │ │
│ │    └─────────┘    │ │
│ │                   │ │ ← Background (baseSlotColor)
│ └───────────────────┘ │
└───────────────────────┘
```

### UpdateGlowAnimation — Pulse đồng bộ với lighting

```csharp
private void UpdateGlowAnimation()
{
    // Smooth lerp: currentGlow → targetGlow (speed × dt)
    currentGlowLevel = Mathf.Lerp(currentGlowLevel, targetGlowLevel, Time.deltaTime * 2f);

    // Pulse: nhấp nháy nhẹ ±15%
    float pulse = 1f + Mathf.Sin(Time.time * glowPulseSpeed) * 0.15f;
    // Sin → [-1, 1] × 0.15 → [-0.15, 0.15] + 1 → [0.85, 1.15]

    float finalGlow = currentGlowLevel * pulse;

    // Apply glow cho TẤT CẢ slots
    for (int i = 0; i < slotCount; i++)
    {
        // Selected slot glow MẠNH hơn 1.5×
        float slotGlow = (i == selectedSlot) ? finalGlow * 1.5f : finalGlow;
        slotGlows[i].color = new Color(glowColor.r, glowColor.g, glowColor.b, slotGlow);
    }
}
```

**Flow glow**:
```
0 fragments: glow = 0.1 × [0.85-1.15] = [0.085 - 0.115] → rất mờ
1 fragment:  glow = 0.267 × pulse → nhìn thấy nhẹ
2 fragments: glow = 0.433 × pulse → rõ hơn
3 fragments: glow = 0.6 × pulse → sáng rõ
Selected slot: × 1.5 → nổi bật hơn
```

### FlashGlow — Hiệu ứng khi nhặt fragment

```csharp
private IEnumerator FlashGlow()
{
    float flashDuration = 0.5f;
    float originalGlow = currentGlowLevel;

    while (elapsed < flashDuration)
    {
        float t = elapsed / flashDuration;
        // Bắt đầu từ MAX → Lerp về original
        currentGlowLevel = Mathf.Lerp(maxGlowIntensity, originalGlow, t);
        // t=0: glow = 0.6 (max)
        // t=1: glow = originalGlow (VD: 0.267)
        yield return null;
    }
}
```

### Input — Keyboard + Mouse Wheel

```csharp
private void HandleKeyboardInput()
{
    var keyboard = Keyboard.current;

    // Phím số 1-5
    for (int i = 0; i < slotCount && i < 9; i++)
    {
        Key key = Key.Digit1 + i;  // Digit1 → Digit5
        if (keyboard[key].wasPressedThisFrame)
        {
            SelectSlot(i);
            break;
        }
    }

    // Mouse wheel scroll
    var mouse = Mouse.current;
    float scroll = mouse.scroll.ReadValue().y;
    if (scroll > 0.1f)       // Scroll UP = slot trước
        SelectSlot((selectedSlot - 1 + slotCount) % slotCount);
    else if (scroll < -0.1f) // Scroll DOWN = slot sau
        SelectSlot((selectedSlot + 1) % slotCount);
    // Modulo % slotCount → wrap around: 0↔4
}
```

---

## 📂 FILE 5: `LightFragmentUI.cs` (261 dòng)

```
📁 Assets/Scripts/UI/LightFragmentUI.cs
```

### Fragment Counter — Pulse animation

```csharp
private void Update()
{
    if (!allCollected && fragmentCountText != null)
    {
        |// Khi chưa đủ → text NHÁY NHẸ (nhắc player tìm fragment)
        float alpha = 0.7f + 0.3f * Mathf.Sin(Time.time * pulseSpeed);
        // alpha dao động: [0.4, 1.0] — 2 lần/giây
        fragmentCountText.alpha = alpha;
    }
}

private void OnAllCollected()
{
    allCollected = true;
    fragmentCountText.color = completeColor;  // Xanh lá = hoàn thành
    fragmentCountText.alpha = 1f;              // Sáng 100%, không nhấp nháy nữa
    ShowNotification("All Fragments Collected!\nFlash of Truth Unlocked!");
}
```

### PulseEffect — Scale bounce khi nhặt

```csharp
private IEnumerator PulseEffect()
{
    Vector3 originalScale = fragmentCountText.transform.localScale;
    Vector3 targetScale = originalScale * 1.3f;  // Phóng to 30%
    float duration = 0.3f;

    // ① Scale UP (0.15s)
    while (elapsed < duration / 2f)
    {
        float t = elapsed / (duration / 2f);
        fragmentCountText.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
        yield return null;
    }

    // ② Scale DOWN (0.15s)
    while (elapsed < duration / 2f)
    {
        float t = elapsed / (duration / 2f);
        fragmentCountText.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
        yield return null;
    }
}
```

---

## 📂 FILE 6: `FlashOfTruthUI.cs` (208 dòng)

```
📁 Assets/Scripts/UI/FlashOfTruthUI.cs
```

### Radial Cooldown — UI kiểu MOBA

```csharp
private void Update()
{
    if (flashAbility == null) return;

    // Fade in UI khi mở khóa
    if (flashAbility.IsUnlocked && canvasGroup.alpha < 1f)
    {
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.deltaTime * 3f);
        // Smooth fade: 0.3 → 1.0 qua ~1 giây
    }

    // Radial fill (kiểu đồng hồ)
    cooldownFillImage.fillAmount = flashAbility.CooldownProgress;
    // CooldownProgress: 0 = full cooldown, 1 = ready
    // fillAmount = progress → "đổ đầy" theo thời gian

    // Đổi màu theo trạng thái
    if (flashAbility.IsFlashActive)
        cooldownFillImage.color = Color.white;    // Đang active = TRẮNG SÁNG
    else if (flashAbility.IsOnCooldown)
        cooldownFillImage.color = cooldownColor;  // Đang CD = XÁM
    else
        cooldownFillImage.color = readyColor;     // Sẵn sàng = VÀNG

    // Text trạng thái
    if (!flashAbility.IsUnlocked)
        cooldownText.text = "LOCKED";
    else if (flashAbility.IsFlashActive)
        cooldownText.text = "ACTIVE!";
    else if (flashAbility.IsOnCooldown)
    {
        float remaining = 15f * (1f - flashAbility.CooldownProgress);
        cooldownText.text = $"{remaining:F1}s";  // VD: "12.3s"
    }
    else
        cooldownText.text = "READY";
}
```

### Auto-create UI bằng code

```csharp
private void CreateFlashUI()
{
    // Container 80×80, góc dưới-trái
    containerRect.anchorMin = new Vector2(0f, 0f);  // Bottom-left
    containerRect.anchoredPosition = new Vector2(20f, 20f);
    containerRect.sizeDelta = new Vector2(80f, 80f);

    canvasGroup = container.AddComponent<CanvasGroup>();
    canvasGroup.alpha = 0.3f;  // Bắt đầu MỜ (locked state)

    // Cooldown fill (radial 360°, bắt đầu từ TOP, chiều kim đồng hồ)
    cooldownFillImage.type = Image.Type.Filled;
    cooldownFillImage.fillMethod = Image.FillMethod.Radial360;
    cooldownFillImage.fillOrigin = (int)Image.Origin360.Top;  // Bắt đầu 12 giờ
    cooldownFillImage.fillClockwise = true;
    cooldownFillImage.fillAmount = 0f;  // 0 = empty → chưa ready
}
```

---

## 📂 FILE 7 & 8: `HotbarUI.cs` + `HotbarSlot.cs`

```
📁 Assets/Scripts/UI/HotbarUI.cs (119 dòng)
📁 Assets/Scripts/UI/HotbarSlot.cs (104 dòng)
```

### HotbarUI — Inventory-backed hotbar

```csharp
public sealed class HotbarUI : MonoBehaviour
{
    [SerializeField] private InventoryManager inventory;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsContainer;

    private void Start()
    {
        CreateSlots();
        // Subscribe events từ InventoryManager
        inventory.OnSlotChanged += OnSlotSelected;   // Slot mới được chọn
        inventory.OnItemChanged += OnItemChanged;     // Item thay đổi (nhặt/dùng)
    }

    private void CreateSlots()
    {
        for (int i = 0; i < inventory.HotbarSize; i++)
        {
            // Dùng prefab nếu có, fallback tạo mặc định
            GameObject slotGo = slotPrefab != null
                ? Instantiate(slotPrefab, slotsContainer)
                : CreateDefaultSlot(i);

            var slot = slotGo.GetComponent<HotbarSlot>();
            slot.Initialize(i);
            slot.SetItem(inventory.GetItemAt(i), inventory.GetCountAt(i));
        }
    }
}
```

### HotbarSlot — Item display

```csharp
public sealed class HotbarSlot : MonoBehaviour
{
    public void SetItem(Item item, int count)
    {
        if (item == null || count <= 0)
        {
            SetEmpty();  // Ẩn icon + count
            return;
        }

        iconImage.sprite = item.icon;   // Hiển thị icon
        iconImage.enabled = true;
        iconImage.color = Color.white;

        // Chỉ hiện số lượng KHI > 1
        if (count > 1)
        {
            countText.text = count.ToString();
            countText.enabled = true;
        }
        else
        {
            countText.enabled = false;  // 1 item → không hiện số
        }
    }
}
```

---

## Bảng tổng hợp UI files

| File | Dòng | Vai trò | Vị trí trên màn hình |
|------|------|---------|---------------------|
| `SpellHotbarUI.cs` | 186 | 4 spell slots, cooldown display | Bottom-center |
| `SpellHotbarSlot.cs` | 124 | 1 slot: icon, border, cooldown radial fill | — |
| `MinimapManager.cs` | 465 | Camera-based circular minimap, RenderTexture | Top-right |
| `LightingHotbarUI.cs` | 410 | 5 item slots, glow syncs with light fragments | Bottom-right |
| `LightFragmentUI.cs` | 261 | Fragment counter x/3, notification popup | Top-right |
| `FlashOfTruthUI.cs` | 208 | Radial cooldown, LOCKED/READY/ACTIVE states | Bottom-left |
| `HotbarUI.cs` | 119 | Inventory-backed item hotbar (prefab-based) | — |
| `HotbarSlot.cs` | 104 | 1 inventory slot: icon, count, selection | — |

**Bố trí màn hình**:
```
┌────────────────────────────────────────────────────┐
│                                   [✦ 2/3] [MAP]   │ ← Fragment + Minimap
│                                                    │
│                                                    │
│                                                    │
│                                                    │
│                                                    │
│ [SPACE]                                   [ITEMS]  │ ← Flash + LightingHotbar
│ [FLASH]    [ 0 ][ 1 ][ 2 ][ 3 ]   [1][2][3][4][5] │ ← SpellHotbar
└────────────────────────────────────────────────────┘
```

---

## 10 Câu hỏi Review — UI/HUD

**Q1: RenderTexture là gì? Khác gì so với vẽ minimap bằng Image/Sprite?**
> RenderTexture là texture được render bởi camera riêng. Ưu: hiển thị scene THẬT (tilemap, enemies). Sprite minimap chỉ vẽ sơ đồ abstract. RenderTexture cho phép player thấy đúng what they see in-game.

**Q2: Tại sao minimap dùng Point Light thay vì Global Light?**
> Global Light ảnh hưởng MỌI camera → main camera cũng sáng thêm → phá horror darkness. Point Light gắn vào minimap camera → chỉ ảnh hưởng minimap render.

**Q3: CreateCircleSprite tạo anti-aliased edge bằng cách nào?**
> `Clamp01(r - dist + 0.5f)` — tại biên (dist ≈ r), alpha = 0.5 tạo smooth gradient 1 pixel. Pixels inside = 1.0, outside = 0.0, edge = 0.5 → anti-aliased.

**Q4: Mask component trong Unity hoạt động thế nào?**
> Mask crop TẤT CẢ children theo hình dạng Image gắn cùng GameObject. showMaskGraphic = false → mask image ẩn, chỉ giữ clip effect. Dùng circle sprite → clip thành minimap tròn.

**Q5: cooldownOverlay dùng Image.Type.Filled — tại sao không dùng shader?**
> Built-in fillAmount + FillMethod.Radial360 đã đủ cho cooldown indicator (kiểu đồng hồ). Custom shader phức tạp hơn, khó maintain, và không cần thiết cho UI đơn giản.

**Q6: LightingHotbarUI glow pulse dùng `Mathf.Sin(Time.time * speed) * 0.15` — hiệu ứng gì?**
> Sin dao động [-1,1] × 0.15 = [-0.15, +0.15], + 1 = [0.85, 1.15]. Glow intensity nhấp nháy nhẹ ±15% → tạo cảm giác "sống" (alive UI), phù hợp horror theme.

**Q7: Sweep line trong minimap có chức năng gameplay không?**
> Không — purely cosmetic. Tạo cảm giác "radar/scanner" sci-fi. Pivot ở bottom + Rotate(-25°/s) → xoay quanh tâm minimap.

**Q8: `ScaleWithScreenSize` reference 1920×1080 — trên màn 2560×1440 thì sao?**
> CanvasScaler tự tính scale factor: 2560/1920 = 1.33. UI elements phóng to 33% → vẫn giữ tỷ lệ. Trên mobile (720×1280) → thu nhỏ 37.5%.

**Q9: SpellHotbarUI dùng sealed class — tại sao?**
> `sealed` = không cho kế thừa. Benefits: (1) JIT compiler optimize tốt hơn (devirtualization), (2) rõ ý design: class này là final, không cần extend. Best practice cho MonoBehaviour cụ thể.

**Q10: MinimapManager cleanup RT trong OnDestroy — tại sao quan trọng?** 
> RenderTexture là **unmanaged resource** (GPU memory). Nếu không Release() → GPU memory leak. GC của C# KHÔNG tự dọn GPU resources. Phải manually cleanup: `minimapRT.Release()` rồi `Destroy(minimapRT)`.
