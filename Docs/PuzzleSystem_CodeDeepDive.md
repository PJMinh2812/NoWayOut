# 🧩 Puzzle System — Giải thích Code từng dòng

> Hai loại puzzle chính: **Flash of Truth** (ánh sáng sự thật) và **Mirror Puzzle** (giải đố gương).
> Cả hai đều tích hợp với hệ thống URP 2D Lighting.

---

## Tổng quan kiến trúc

```
GameManager
├── OnAllLightFragmentsCollected event
│          ↓
FlashOfTruth (Player component)
├── Nhấn Space → burst ánh sáng 50 radius, 5 giây
├── Reveal traps (highlight đỏ nhấp nháy)
├── Stun enemies (disable AI 3 giây)
└── Activate LightReceivers
         ↓
LightMirror                          LightReceiver
├── Detect player light trong range  ├── Nhận light → timer tích lũy
├── Raycast check line of sight      ├── activationDelay (0.5s) → Activate
├── Vector2.Reflect() tính phản xạ   ├── UnityEvent OnActivated
├── Raycast reflected beam           └── requiresContinuousLight?
└── LineRenderer hiển thị beam             ↓
         ↓                           MirrorPuzzleDoor
    Hit LightReceiver?               ├── Subscribe OnActivated events
    → receiver.ReceiveLight()        ├── Check tất cả receivers active?
                                     └── Mở cửa (Lerp position + disable collider)
```

---

## 📂 FILE 1: `FlashOfTruth.cs` (341 dòng)

```
📁 Assets/Scripts/Player/FlashOfTruth.cs
```

### Cách mở khóa ability

```csharp
public class FlashOfTruth : MonoBehaviour
{
    private bool isUnlocked = false;
    private bool isOnCooldown = false;
    private bool isFlashActive = false;
    private float cooldownTimer = 0f;

    // URP Light2D reference — lấy từ DungeonLightingManager
    private Light2D playerLight;

    private void Start()
    {
        // Lấy player light từ lighting system
        playerLight = DungeonLightingManager.Instance.GetPlayerLight();

        // Subscribe event "đã nhặt đủ 3 Light Fragments"
        GameManager.Instance.OnAllLightFragmentsCollected += OnAllFragmentsCollected;

        // Trường hợp load save: kiểm tra đã đủ fragments chưa
        if (GameManager.Instance.LightFragmentsCollected >= GameManager.Instance.TotalLightFragments)
        {
            UnlockAbility();  // isUnlocked = true
        }
    }
}
```

**Flow mở khóa**: Player nhặt 3/3 Light Fragments → `GameManager` raise `OnAllLightFragmentsCollected` → `FlashOfTruth.UnlockAbility()` → player có thể nhấn Space.

### Input — New Input System

```csharp
private void Update()
{
    // Cooldown countdown
    if (isOnCooldown)
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            isOnCooldown = false;
            cooldownTimer = 0f;
        }
    }

    // Kiểm tra phím Space (Unity New Input System)
    var keyboard = Keyboard.current;
    if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && CanActivate())
    {
        ActivateFlash();
    }
    // wasPressedThisFrame: chỉ true 1 frame duy nhất khi nhấn
    // → tránh spam liên tục
}

private bool CanActivate()
{
    return isUnlocked        // Đã nhặt đủ 3 fragments
        && !isOnCooldown     // Hết cooldown
        && !isFlashActive;   // Không đang flash
}
```

### FlashSequence — Toàn bộ hiệu ứng

```csharp
private IEnumerator FlashSequence()
{
    isFlashActive = true;
    isOnCooldown = true;
    cooldownTimer = cooldownTime;  // 15 giây

    // ① LƯU trạng thái gốc của player light
    originalRadius = playerLight.pointLightOuterRadius;     // VD: 6.25
    originalIntensity = playerLight.intensity;               // VD: 0.8
    originalColor = playerLight.color;                       // VD: vàng nhạt

    // ② BURST: Tăng light tức thì
    playerLight.pointLightOuterRadius = flashLightRadius;   // 50 units!
    playerLight.pointLightInnerRadius = flashLightRadius * 0.6f;  // 30 units
    playerLight.intensity = flashIntensity;                  // 3.0
    playerLight.color = flashColor;                          // Trắng tinh
```

**Hiệu ứng burst**: Từ radius 6.25 → 50 tức thì. Player light bao phủ GẦN NHƯ TOÀN BỘ phòng. Intensity 3.0 = sáng chói.

```csharp
    // ③ BA HIỆU ỨNG GAMEPLAY đồng thời:
    RevealTraps();              // Highlight bẫy đỏ nhấp nháy
    StunEnemies();              // Vô hiệu hóa AI 3 giây
    ActivateLightReceivers();   // Kích hoạt puzzle receivers

    // ④ GIỮ flash 5 giây
    yield return new WaitForSeconds(flashDuration);  // 5s

    // ⑤ FADE về trạng thái gốc (0.5 giây)
    float fadeTime = 0.5f;
    float elapsed = 0f;
    while (elapsed < fadeTime)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / fadeTime;

        // Lerp TẤT CẢ properties đồng thời
        playerLight.pointLightOuterRadius = Mathf.Lerp(50, 6.25, t);
        playerLight.intensity = Mathf.Lerp(3.0, 0.8, t);
        playerLight.color = Color.Lerp(Color.white, originalColor, t);

        yield return null;
    }

    // ⑥ Đảm bảo giá trị chính xác
    playerLight.pointLightOuterRadius = originalRadius;
    playerLight.intensity = originalIntensity;
    playerLight.color = originalColor;

    isFlashActive = false;
}
```

### RevealTraps — Highlight bẫy

```csharp
private void RevealTraps()
{
    // Tìm TẤT CẢ objects có tên/tag/component chứa "Trap"
    GameObject[] allObjects = FindObjectsByType<GameObject>(...);

    foreach (var obj in allObjects)
    {
        bool isTrap = obj.name.Contains("Trap") ||
                     obj.CompareTag("Trap") ||
                     obj.GetComponent<MonoBehaviour>()?.GetType().Name.Contains("Trap") == true;
        // ?.GetType().Name: Reflection — lấy tên class của component
        // VD: SpikeTrap → "SpikeTrap".Contains("Trap") = true

        if (isTrap)
            StartCoroutine(HighlightObject(obj, flashDuration));
    }
}

private IEnumerator HighlightObject(GameObject obj, float duration)
{
    // Đổi màu sprite → ĐỎ
    foreach (var renderer in renderers)
        renderer.color = trapRevealColor;  // (1, 0.3, 0.3, 0.8)

    // NHẤP NHÁY suốt thời gian flash
    while (elapsed < duration)
    {
        // Sin × 10 → nhấp nháy RẤT NHANH (10 lần/giây)
        float alpha = 0.5f + 0.5f * Mathf.Sin(elapsed * 10f);
        renderer.color = new Color(1f, 0.3f, 0.3f, alpha);
        yield return null;
    }

    // Khôi phục màu gốc
    renderer.color = originalColors[i];
}
```

### StunEnemies — Disable AI

```csharp
private IEnumerator StunEnemy(GameObject enemy)
{
    // Disable MonoBehaviour → AI ngừng hoạt động
    var enemyScript = enemy.GetComponent<MonoBehaviour>();
    enemyScript.enabled = false;
    // enabled = false: Update(), FixedUpdate() KHÔNG được gọi
    // → Enemy đứng yên, không chase player

    // Visual: nhấp nháy trắng
    while (elapsed < enemyStunDuration)  // 3 giây
    {
        float flash = Mathf.PingPong(elapsed * 5f, 1f);
        // PingPong: giá trị dao động 0↔1 liên tục
        renderer.color = Color.Lerp(originalColor, Color.white, flash * 0.5f);
        yield return null;
    }

    // Khôi phục
    enemyScript.enabled = true;  // AI hoạt động lại
    renderer.color = originalColor;
}
```

### ActivateLightReceivers — Kích hoạt puzzle

```csharp
private void ActivateLightReceivers()
{
    // Tìm TẤT CẢ LightReceiver trong scene
    var receivers = FindObjectsByType<Puzzle.LightReceiver>(...);

    foreach (var receiver in receivers)
    {
        float distance = Vector2.Distance(transform.position, receiver.transform.position);

        // CHỈ kích hoạt receivers TRONG phạm vi flash (50 units)
        if (distance <= flashLightRadius)
        {
            StartCoroutine(TemporarilyActivateReceiver(receiver, flashDuration));
        }
    }
}

private IEnumerator TemporarilyActivateReceiver(LightReceiver receiver, float duration)
{
    receiver.ReceiveLight();  // Bắt đầu nhận ánh sáng

    // GỌI LIÊN TỤC mỗi frame để duy trì activation
    while (elapsed < duration)
    {
        receiver.ReceiveLight();  // Phải gọi liên tục!
        yield return null;
    }

    receiver.LoseLight();  // Hết flash → ngừng nhận
}
```

**Tại sao gọi ReceiveLight() liên tục?** Vì LightReceiver có `requiresContinuousLight = true` — nếu 1 frame không nhận light → `activationTimer` giảm → deactivate.

---

## 📂 FILE 2: `LightMirror.cs` (201 dòng)

```
📁 Assets/Scripts/Puzzle/LightMirror.cs
```

### Cách gương hoạt động

```
Player Light ──→ ● LightMirror ──→ ● LightReceiver
     (Point)     ↑ phản xạ theo     ↑ nhận light
                   góc gương           → kích hoạt
```

### Kiểm tra player light trong tầm

```csharp
private void CheckPlayerLightInRange()
{
    float distance = Vector2.Distance(transform.position, playerTransform.position);

    // ĐK1: Player gần ĐỦ (detectionRadius = 8)
    // ĐK2: Player light đang BẬT (intensity > 0.1)
    if (distance <= detectionRadius && playerLight.intensity > 0.1f)
    {
        // ĐK3: Không có vật cản giữa player và mirror
        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,      // Bắt đầu từ mirror
            directionToPlayer,       // Hướng về player
            distance,                // Khoảng cách
            obstacleLayer            // Chỉ check tường
        );

        // Nếu KHÔNG chạm vật cản → gương ACTIVATED
        isActivated = (hit.collider == null);
    }
    else
    {
        isActivated = false;
    }
}
```

**Minh họa Raycast line of sight**:
```
❌ Có vật cản:               ✅ Không vật cản:
Player ─── Wall ─── Mirror   Player ──────── Mirror
       ↑ Raycast chạm wall          ↑ Raycast thông
       → isActivated = false        → isActivated = true
```

### CalculateReflection — Toán phản xạ

```csharp
private void CalculateReflection()
{
    // ① Hướng ánh sáng ĐẾN gương (từ player → mirror)
    Vector2 incomingDirection = (transform.position - playerTransform.position).normalized;
    // normalized: vector độ dài 1 → chỉ giữ hướng

    // ② Tính PHÁP TUYẾN gương (vuông góc với mặt gương)
    float mirrorAngleRad = transform.eulerAngles.z * Mathf.Deg2Rad;
    // eulerAngles.z = góc xoay 2D (VD: 45°)
    // Deg2Rad = nhân π/180 → chuyển sang radian

    Vector2 mirrorNormal = new Vector2(
        -Mathf.Sin(mirrorAngleRad),  // Component X
        Mathf.Cos(mirrorAngleRad)    // Component Y
    );
    // Gương xoay 0° → Normal = (0, 1) = hướng LÊN
    // Gương xoay 45° → Normal = (-0.707, 0.707) = hướng trên-trái

    // ③ PHẢN XẠ: Unity có sẵn Vector2.Reflect()
    Vector2 reflectedDirection = Vector2.Reflect(incomingDirection, mirrorNormal);
    // Công thức: reflect = incoming - 2 × dot(incoming, normal) × normal
```

**Minh họa phản xạ**:
```
        incoming ↘     ↗ reflected
                  ↘   ↗
                   ↘ ↗
    ─────────────── ● ────────────── mirror surface
                    ↑
                  normal (vuông góc)
   Góc vào = Góc ra (luật phản xạ ánh sáng)
```

```csharp
    // ④ RAYCAST theo hướng phản xạ để tìm điểm kết thúc
    RaycastHit2D hit = Physics2D.Raycast(
        transform.position,        // Bắt đầu từ mirror
        reflectedDirection,        // Hướng phản xạ
        maxReflectionDistance,     // Tối đa 15 units
        obstacleLayer              // Chỉ check tường
    );

    if (hit.collider != null)
    {
        reflectionEndPoint = hit.point;

        // Kiểm tra: chạm LightReceiver không?
        hitReceiver = hit.collider.GetComponent<LightReceiver>();
        if (hitReceiver != null)
        {
            hitReceiver.ReceiveLight();  // ★ Kích hoạt receiver!
        }
    }
    else
    {
        // Không chạm gì → beam kết thúc ở khoảng cách tối đa
        reflectionEndPoint = transform.position + reflectedDirection * maxReflectionDistance;
    }
}
```

### LineRenderer — Hiển thị tia sáng

```csharp
private void UpdateBeamVisualization()
{
    beamRenderer.enabled = true;
    beamRenderer.SetPosition(0, transform.position);     // Điểm đầu = mirror
    beamRenderer.SetPosition(1, reflectionEndPoint);      // Điểm cuối = receiver/wall

    // Pulse: tia sáng nhấp nháy nhẹ
    float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 3f);
    // Dao động alpha: 0.2 → 1.0 (3 lần/giây)
    Color currentColor = beamColor;
    currentColor.a = beamColor.a * pulse;
    beamRenderer.startColor = currentColor;
    beamRenderer.endColor = currentColor;
}
```

---

## 📂 FILE 3: `LightReceiver.cs` (193 dòng)

```
📁 Assets/Scripts/Puzzle/LightReceiver.cs
```

### Hai chế độ hoạt động

| Setting | Giá trị | Hành vi |
|---------|---------|---------|
| `requiresContinuousLight = true` | Mặc định | Receiver CHỈ active KHI đang NHẬN light. Bỏ light → deactivate |
| `requiresContinuousLight = false` | Optional | Một lần active → giữ mãi |
| `oneTimeActivation = true` | Optional | Chỉ active 1 lần (không thể deactivate) |

### ReceiveLight / LoseLight API

```csharp
// Gọi bởi LightMirror hoặc FlashOfTruth MỖI FRAME
public void ReceiveLight()
{
    isReceivingLight = true;
    // Chỉ set flag — logic xử lý trong Update()
}

public void LoseLight()
{
    isReceivingLight = false;
}
```

### Update — Timer-based activation

```csharp
private void Update()
{
    if (oneTimeActivation && hasBeenActivated) return;  // Đã kích hoạt 1 lần → skip

    if (isReceivingLight)
    {
        // ĐANG nhận light → tăng timer
        activationTimer += Time.deltaTime;

        // Đủ 0.5 giây → ACTIVATE
        if (activationTimer >= activationDelay && !isActivated)
        {
            Activate();
        }
    }
    else if (requiresContinuousLight)
    {
        // KHÔNG nhận light (và cần continuous) → giảm timer
        activationTimer -= Time.deltaTime * 2f;  // Giảm GẤP ĐÔI tốc độ tăng
        // → Punish nhanh: mất light 0.25s = mất full timer

        if (activationTimer <= 0f && isActivated)
        {
            Deactivate();
        }
    }

    activationTimer = Mathf.Clamp(activationTimer, 0f, activationDelay);

    UpdateVisuals();  // Gradient màu theo progress
}
```

**activationDelay = 0.5s** → Player phải giữ mirror hướng đúng ít nhất 0.5 giây. Tránh việc vô tình kích hoạt khi đi ngang qua.

### Activate — Kích hoạt puzzle

```csharp
private void Activate()
{
    isActivated = true;
    hasBeenActivated = true;

    // ★ UnityEvent — cho phép kết nối hành động TỪ INSPECTOR
    OnActivated?.Invoke();
    // VD: OnActivated → MirrorPuzzleDoor.CheckPuzzleState()
    //     OnActivated → OpenSecretPassage()
    //     OnActivated → SpawnReward()

    audioSource.PlayOneShot(activationSound);
    activationParticles.Play();
}
```

**UnityEvent** = "kéo thả" action trong Inspector. Designer không cần code — chỉ cần drag `MirrorPuzzleDoor.CheckPuzzleState` vào OnActivated list.

### UpdateVisuals — Gradient theo progress

```csharp
private void UpdateVisuals()
{
    float progress = activationTimer / activationDelay;  // 0 → 1

    // Màu sprite: xám → vàng sáng (theo progress)
    receiverSprite.color = Color.Lerp(inactiveColor, activeColor, progress);
    // progress 0: (0.3, 0.3, 0.3) = xám
    // progress 0.5: (0.65, 0.65, 0.15) = vàng nhạt
    // progress 1: (1, 1, 0.3) = vàng sáng

    // Indicator light: cũng tăng theo
    indicatorLight.intensity = Mathf.Lerp(0.2f, 1.5f, progress);
    indicatorLight.pointLightOuterRadius = Mathf.Lerp(2f, 5f, progress);
}
```

---

## 📂 FILE 4: `MirrorPuzzleDoor.cs` (195 dòng)

```
📁 Assets/Scripts/Puzzle/MirrorPuzzleDoor.cs
```

### Subscribe receivers — Event wiring

```csharp
private void Start()
{
    closedPosition = doorVisual.transform.position;
    openPosition = closedPosition + openOffset;  // Di chuyển lên 3 units

    // Đăng ký nhận sự kiện từ TẤT CẢ receivers
    foreach (var receiver in requiredReceivers)
    {
        receiver.OnActivated.AddListener(CheckPuzzleState);
        receiver.OnDeactivated.AddListener(CheckPuzzleState);
        // Mỗi lần bất kỳ receiver nào thay đổi trạng thái
        // → kiểm tra lại toàn bộ puzzle
    }
}
```

### CheckPuzzleState — Kiểm tra điều kiện mở cửa

```csharp
private void CheckPuzzleState()
{
    bool shouldOpen = false;

    if (requireAllReceivers)  // Cần TẤT CẢ receivers active
    {
        shouldOpen = true;
        foreach (var receiver in requiredReceivers)
        {
            // ★ Dùng REFLECTION để đọc private field
            var field = receiver.GetType().GetField("isActivated",
                BindingFlags.NonPublic | BindingFlags.Instance);
            bool receiverActive = (bool)field.GetValue(receiver);

            if (!receiverActive)
            {
                shouldOpen = false;
                break;  // 1 receiver chưa active → cửa ĐÓNG
            }
        }
    }
    // else: chỉ cần 1 receiver (tương tự nhưng check Any)
```

**Reflection** — Đọc field `private` từ bên ngoài class:
```csharp
// Bình thường: không thể đọc receiver.isActivated (private)
// Reflection: GetField("isActivated", NonPublic) → bypass access
// Trade-off: chậm hơn property, nhưng không cần sửa LightReceiver
```

### Mở/đóng cửa — Smooth Lerp

```csharp
private void Update()
{
    // Target = openPosition nếu mở, closedPosition nếu đóng
    Vector3 targetPosition = isOpen ? openPosition : closedPosition;

    // Lerp mỗi frame → di chuyển SMOOTH
    doorVisual.transform.position = Vector3.Lerp(
        doorVisual.transform.position,
        targetPosition,
        Time.deltaTime * openSpeed  // 2.0 → tốc độ mở
    );
    // Lerp(current, target, speed×dt) = exponential decay
    // → Nhanh lúc đầu, chậm dần → smooth motion
}

private void OpenDoor()
{
    isOpen = true;
    doorCollider.enabled = false;  // Player đi qua được
    openEffects.Play();            // Particle
    audioSource.PlayOneShot(openSound);
}

private void CloseDoor()
{
    isOpen = false;
    doorCollider.enabled = true;   // Player bị chặn
}
```

---

## Danh sách file Puzzle System

| File | Dòng | Vai trò |
|------|------|---------|
| `Player/FlashOfTruth.cs` | 341 | Ability: burst light, reveal traps, stun enemies, activate receivers |
| `Puzzle/LightMirror.cs` | 201 | Phản xạ ánh sáng player → beam → hit receivers |
| `Puzzle/LightReceiver.cs` | 193 | Nhận light, timer-based activation, UnityEvent → trigger actions |
| `Puzzle/MirrorPuzzleDoor.cs` | 195 | Cửa mở khi tất cả receivers active, Lerp animation |
| `UI/FlashOfTruthUI.cs` | 208 | Radial cooldown fill, trạng thái LOCKED/READY/ACTIVE |

---

## 10 Câu hỏi Review — Puzzle System

**Q1: Flash of Truth mở khóa khi nào?**
> Khi player nhặt đủ 3/3 Light Fragments → GameManager raise `OnAllLightFragmentsCollected` → `FlashOfTruth.UnlockAbility()`.

**Q2: Vector2.Reflect() hoạt động thế nào?**
> `reflect = incoming - 2 × dot(incoming, normal) × normal`. Góc tới = góc phản xạ. Unity cung cấp sẵn hàm này.

**Q3: Tại sao LightReceiver cần activationDelay 0.5s?**
> Tránh kích hoạt vô tình khi player đi ngang mirror. Phải giữ beam hướng đúng ít nhất 0.5s → intentional gameplay.

**Q4: requiresContinuousLight = true có nghĩa gì?**
> Receiver CHỈ active khi ĐANG nhận light. Bỏ light (player rời mirror) → deactivate → cửa đóng lại. Buộc player giải puzzle đúng timing.

**Q5: Tại sao deactivation nhanh gấp đôi activation?**
> `activationTimer -= Time.deltaTime * 2f` — Punish nhanh: nếu player lỡ mất beam 0.25s → mất toàn bộ tiến trình. Tạo tension.

**Q6: MirrorPuzzleDoor dùng Reflection — ưu/nhược điểm?**
> Ưu: không cần sửa LightReceiver (thêm public property). Nhược: chậm hơn direct access, dễ lỗi nếu rename field, compile-time không check được.

**Q7: Raycast trong LightMirror check obstacleLayer — tại sao cần LayerMask?**
> LayerMask lọc collision: chỉ check tường, KHÔNG check player/enemy/items. Không có layer mask → beam bị chặn bởi player standing gần mirror.

**Q8: FlashOfTruth gọi RevealTraps() nhưng dùng FindObjectsByType — performance?**
> O(n) duyệt TẤT CẢ GameObjects trong scene. Chấp nhận được vì: (1) chỉ gọi 1 lần mỗi 15s (cooldown), (2) tổng objects ~ vài trăm. Nếu cần optimize → cache trap references.

**Q9: TemporarilyActivateReceiver gọi ReceiveLight() mỗi frame — tại sao?**
> Vì LightReceiver.Update() check `isReceivingLight` flag, và flag được reset mỗi frame khi LightMirror gọi `LoseLight()`. Phải "keep alive" liên tục.

**Q10: MirrorPuzzleDoor dùng Lerp(pos, target, speed×dt) — đây là loại interpolation nào?**
> **Exponential decay** (không phải linear Lerp). Mỗi frame di chuyển một % khoảng cách còn lại → nhanh đầu, chậm cuối, KHÔNG BAO GIỜ đến chính xác target. Dùng được vì visually smooth, nhưng nếu cần precision → dùng Mathf.MoveTowards.
