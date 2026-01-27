# ⌨️ HƯỚNG DẪN NÂNG CẤP INPUT SYSTEM - KEY BINDING TÙY CHỈNH

## 🎯 MỤC TIÊU
Nâng cấp từ **hard-coded input** sang **Unity Input System với rebinding** để người chơi tùy chỉnh phím.

---

## ⚠️ VẤN ĐỀ HIỆN TẠI

**PlayerController2D.cs hiện tại:**
```csharp
private static Vector2 GetMoveInput()
{
    var keyboard = Keyboard.current;
    if (keyboard == null) return Vector2.zero;

    var x = 0f;
    var y = 0f;

    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;

    return new Vector2(x, y);
}
```

**Vấn đề:**
- ❌ Hard-coded keys (không thể thay đổi)
- ❌ Không support gamepad
- ❌ Không có rebinding UI

---

## ✅ GIẢI PHÁP

Sử dụng file `InputSystem_Actions.inputactions` có sẵn trong project.

---

## 📋 BƯỚC 1: CẤU HÌNH INPUT ACTIONS (5 phút)

### 1.1: Mở Input Actions Asset

**Project → Assets → InputSystem_Actions.inputactions**
- Double-click để mở Input Actions editor

### 1.2: Tạo Action Map "Player"

**Nếu chưa có Action Map "Player":**
1. Click (+) → "Add Action Map"
2. Đổi tên: `Player`

### 1.3: Tạo Actions

**Trong Action Map "Player", tạo các actions:**

**1. Movement (Vector2):**
```
Name: Movement
Action Type: Value
Control Type: Vector2

Bindings:
  - WASD (Keyboard)
    - Up: W
    - Down: S
    - Left: A
    - Right: D
  - Arrow Keys (Keyboard)
    - Up: UpArrow
    - Down: DownArrow
    - Left: LeftArrow
    - Right: RightArrow
  - Left Stick (Gamepad)
    - Left Stick [Gamepad]
```

**Cách tạo WASD Composite:**
1. Click (+) bên cạnh "Movement"
2. Chọn "Add 2D Vector Composite"
3. Đổi tên: "WASD"
4. Expand WASD:
   - Up → Path: `<Keyboard>/w`
   - Down → Path: `<Keyboard>/s`
   - Left → Path: `<Keyboard>/a`
   - Right → Path: `<Keyboard>/d`

**2. Roll/Dash (Button):**
```
Name: Dash
Action Type: Button

Bindings:
  - Space (Keyboard): <Keyboard>/space
  - Left Shift (Keyboard): <Keyboard>/leftShift
  - South Button (Gamepad): <Gamepad>/buttonSouth (A/X)
```

**3. Fire (Button):**
```
Name: Fire
Action Type: Button

Bindings:
  - Left Mouse: <Mouse>/leftButton
  - East Button (Gamepad): <Gamepad>/buttonEast (B/Circle)
```

**4. Interact (Button):**
```
Name: Interact
Action Type: Button

Bindings:
  - E (Keyboard): <Keyboard>/e
  - West Button (Gamepad): <Gamepad>/buttonWest (X/Square)
```

**5. Pause (Button):**
```
Name: Pause
Action Type: Button

Bindings:
  - Escape: <Keyboard>/escape
  - Start Button: <Gamepad>/start
```

### 1.4: Lưu và Generate C# Class

**Click "Save Asset"**

**Tick "Generate C# Class":**
- Class Name: `InputActions`
- C# Class File: `Assets/Scripts/InputActions.cs`
- Click "Apply"

> Unity sẽ tự động generate class `InputActions.cs`

---

## 📋 BƯỚC 2: CẬP NHẬT PLAYERCONTROLLER2D.CS (10 phút)

### 2.1: Import Namespace

```csharp
using UnityEngine;
using UnityEngine.InputSystem; // Đã có
```

### 2.2: Thêm InputActions Field

**Thêm vào đầu class PlayerController2D:**

```csharp
public sealed class PlayerController2D : MonoBehaviour
{
    // ... existing fields ...

    // Input System
    private InputActions _inputActions;
    private InputAction _moveAction;
    private InputAction _dashAction;

    // ... rest of code ...
}
```

### 2.3: Khởi Tạo Input Actions

**Sửa Awake():**

```csharp
private void Awake()
{
    _rb = GetComponent<Rigidbody2D>();
    if (worldCamera == null) worldCamera = Camera.main;

    // Initialize Input System
    _inputActions = new InputActions();
    _moveAction = _inputActions.Player.Movement;
    _dashAction = _inputActions.Player.Dash;
}
```

### 2.4: Enable/Disable Input

**Thêm OnEnable/OnDisable:**

```csharp
private void OnEnable()
{
    _inputActions.Player.Enable();
}

private void OnDisable()
{
    _inputActions.Player.Disable();
}
```

### 2.5: Sửa GetMoveInput()

**Thay thế method cũ:**

```csharp
private Vector2 GetMoveInput()
{
    // Đọc từ InputAction (hỗ trợ cả keyboard, gamepad, rebinding)
    return _moveAction.ReadValue<Vector2>();
}
```

**Xóa method cũ:** `GetMoveInput()` có Keyboard.current

### 2.6: Sửa GetRollPressed()

**Thay thế method cũ:**

```csharp
private bool GetRollPressed()
{
    // Sử dụng InputAction
    return _dashAction.WasPressedThisFrame();
}
```

**Xóa method cũ:** `GetRollPressed()` có Keyboard.current

### 2.7: Code Hoàn Chỉnh (PlayerController2D.cs)

**Full code sau khi sửa:**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace GloomCraft
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveAcceleration = 45f;
        [SerializeField] private float maxMoveSpeed = 5.0f;
        [SerializeField] private float linearDrag = 8f;

        [Header("Roll")]
        [SerializeField] private float rollSpeed = 10f;
        [SerializeField] private float rollDuration = 0.20f;
        [SerializeField] private float rollCooldown = 0.50f;

        [Header("Aim")]
        [SerializeField] private Camera worldCamera;

        public float AimAngleDeg { get; private set; }
        public bool IsRolling => _rollingTimeRemaining > 0f;

        private Rigidbody2D _rb;
        private float _cooldownRemaining;
        private float _rollingTimeRemaining;
        private Vector2 _rollVelocity;

        // Input System
        private InputActions _inputActions;
        private InputAction _moveAction;
        private InputAction _dashAction;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (worldCamera == null) worldCamera = Camera.main;

            // Initialize Input System
            _inputActions = new InputActions();
            _moveAction = _inputActions.Player.Movement;
            _dashAction = _inputActions.Player.Dash;
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();
        }

        private void OnDisable()
        {
            _inputActions.Player.Disable();
        }

        private void Update()
        {
            UpdateAim();

            if (_cooldownRemaining > 0f) _cooldownRemaining -= Time.deltaTime;
            if (_rollingTimeRemaining > 0f) _rollingTimeRemaining -= Time.deltaTime;

            if (!IsRolling && _cooldownRemaining <= 0f && GetRollPressed())
            {
                var input = GetMoveInput();
                if (input.sqrMagnitude > 0.0001f)
                {
                    StartRoll(input.normalized);
                }
            }
        }

        private void FixedUpdate()
        {
            _rb.linearDamping = linearDrag;

            if (IsRolling)
            {
                _rb.linearVelocity = _rollVelocity;
                return;
            }

            var input = GetMoveInput();
            if (input.sqrMagnitude > 1f) input.Normalize();

            _rb.AddForce(input * moveAcceleration, ForceMode2D.Force);

            var v = _rb.linearVelocity;
            if (v.magnitude > maxMoveSpeed)
            {
                _rb.linearVelocity = v.normalized * maxMoveSpeed;
            }
        }

        private void StartRoll(Vector2 dir)
        {
            _rollingTimeRemaining = rollDuration;
            _cooldownRemaining = rollCooldown;
            _rollVelocity = dir * rollSpeed;
        }

        private void UpdateAim()
        {
            if (worldCamera == null) return;
            var mouse = Mouse.current.position.ReadValue();
            var world = worldCamera.ScreenToWorldPoint(mouse);
            var delta = (Vector2)world - (Vector2)transform.position;
            AimAngleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        }

        private Vector2 GetMoveInput()
        {
            return _moveAction.ReadValue<Vector2>();
        }

        private bool GetRollPressed()
        {
            return _dashAction.WasPressedThisFrame();
        }
    }
}
```

---

## 📋 BƯỚC 3: CẬP NHẬT PLAYERSHOOTER2D.CS (5 phút)

**Mở `Assets/Scripts/Player/PlayerShooter2D.cs`**

### 3.1: Thêm Input Actions

```csharp
private InputActions _inputActions;
private InputAction _fireAction;

private void Awake()
{
    // ... existing code ...
    
    _inputActions = new InputActions();
    _fireAction = _inputActions.Player.Fire;
}

private void OnEnable()
{
    _inputActions.Player.Enable();
}

private void OnDisable()
{
    _inputActions.Player.Disable();
}
```

### 3.2: Sửa Fire Input

**Trong Update(), thay thế:**

```csharp
// Cũ:
// if (Mouse.current.leftButton.wasPressedThisFrame)

// Mới:
if (_fireAction.WasPressedThisFrame())
{
    TryFire();
}
```

---

## 📋 BƯỚC 4: TẠO UI REBINDING (TÙY CHỌN - 30 phút)

### 4.1: Tạo Settings UI

**Canvas → Settings Panel:**
```
Canvas
└── SettingsPanel
    ├── Title (Text: "Settings")
    ├── MovementRebind (Button)
    ├── DashRebind (Button)
    ├── FireRebind (Button)
    └── CloseButton
```

### 4.2: Tạo Script RebindUI.cs

**Assets/Scripts/UI/RebindUI.cs:**

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace GloomCraft
{
    public class RebindUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputActionReference actionToRebind;
        [SerializeField] private Button rebindButton;
        [SerializeField] private TextMeshProUGUI buttonText;

        private InputActionRebindingExtensions.RebindingOperation _rebindOperation;

        private void Start()
        {
            rebindButton.onClick.AddListener(StartRebinding);
            UpdateButtonText();
        }

        private void StartRebinding()
        {
            buttonText.text = "Press any key...";

            var action = actionToRebind.action;
            action.Disable();

            _rebindOperation = action.PerformInteractiveRebinding()
                .WithControlsExcluding("Mouse")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(operation => RebindComplete())
                .Start();
        }

        private void RebindComplete()
        {
            _rebindOperation?.Dispose();
            actionToRebind.action.Enable();
            UpdateButtonText();
        }

        private void UpdateButtonText()
        {
            var action = actionToRebind.action;
            var bindingIndex = action.GetBindingIndexForControl(action.controls[0]);
            var displayString = action.GetBindingDisplayString(bindingIndex);
            buttonText.text = $"{action.name}: {displayString}";
        }
    }
}
```

### 4.3: Setup UI trong Scene

**Cho mỗi rebind button:**
1. Add component `RebindUI.cs`
2. Gán `Input Action Reference` → chọn action (Movement, Dash, Fire...)
3. Gán `Rebind Button` → chính button đó
4. Gán `Button Text` → TextMeshPro con

---

## 📋 BƯỚC 5: SPRINT FEATURE (BONUS - 10 phút)

### 5.1: Thêm Sprint Action

**InputSystem_Actions.inputactions:**
```
Name: Sprint
Action Type: Button

Bindings:
  - Left Shift: <Keyboard>/leftShift
  - Left Trigger: <Gamepad>/leftTrigger
```

### 5.2: Cập Nhật PlayerController2D

**Thêm fields:**

```csharp
[Header("Sprint")]
[SerializeField] private float sprintMultiplier = 1.8f;

private InputAction _sprintAction;
```

**Awake():**

```csharp
_sprintAction = _inputActions.Player.Sprint;
```

**FixedUpdate():**

```csharp
var input = GetMoveInput();
if (input.sqrMagnitude > 1f) input.Normalize();

// Sprint multiplier
float speedMultiplier = _sprintAction.IsPressed() ? sprintMultiplier : 1f;

_rb.AddForce(input * moveAcceleration * speedMultiplier, ForceMode2D.Force);

var maxSpeed = maxMoveSpeed * speedMultiplier;
var v = _rb.linearVelocity;
if (v.magnitude > maxSpeed)
{
    _rb.linearVelocity = v.normalized * maxSpeed;
}
```

### 5.3: Visual Feedback (Particle/Trail)

**Thêm Trail Renderer vào Player:**
1. Hierarchy → Player → Add Component → Trail Renderer
2. Settings:
   - Time: 0.3
   - Width: 0.05 → 0
   - Color: Gradient (white → transparent)
3. Enable chỉ khi sprint

**Script:**

```csharp
[SerializeField] private TrailRenderer sprintTrail;

// Update()
sprintTrail.emitting = _sprintAction.IsPressed();
```

---

## ✅ CHECKLIST HOÀN THÀNH

**Input Actions:**
- [ ] Tạo Action Map "Player"
- [ ] Movement (Vector2) - WASD + Arrows + Gamepad
- [ ] Dash (Button) - Space/Shift/Gamepad
- [ ] Fire (Button) - Mouse/Gamepad
- [ ] Interact (Button) - E/Gamepad
- [ ] Sprint (Button - optional)
- [ ] Generate C# Class

**PlayerController2D:**
- [ ] Import InputActions
- [ ] Initialize actions trong Awake
- [ ] Enable/Disable trong OnEnable/OnDisable
- [ ] Sử dụng _moveAction.ReadValue<Vector2>()
- [ ] Sử dụng _dashAction.WasPressedThisFrame()

**PlayerShooter2D:**
- [ ] Sử dụng _fireAction thay Mouse.current

**Testing:**
- [ ] WASD di chuyển
- [ ] Arrow keys di chuyển
- [ ] Space/Shift dash
- [ ] Mouse bắn
- [ ] Gamepad (nếu có)

**Advanced (Optional):**
- [ ] Rebinding UI
- [ ] Sprint feature
- [ ] Trail effect khi sprint
- [ ] Save/Load bindings (PlayerPrefs)

---

## 🎮 GAMEPAD SUPPORT

**Testing với Gamepad:**
1. Cắm gamepad (Xbox/PS4/PS5)
2. Unity tự detect
3. Left Stick → Movement
4. A/X Button → Dash
5. B/Circle → Fire

**Debug gamepad:**
```csharp
Debug.Log(Gamepad.current?.name);
```

---

## 📚 TÀI LIỆU THAM KHẢO

**Unity Input System:**
- [Input System Package](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/index.html)
- [Rebinding UI](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/ActionBindings.html)

**Scripts location:**
- `Assets/Scripts/Player/PlayerController2D.cs`
- `Assets/Scripts/Player/PlayerShooter2D.cs`
- `Assets/InputSystem_Actions.inputactions`

---

**Hoàn thành guide này → Input system linh hoạt với rebinding! 🎮**
