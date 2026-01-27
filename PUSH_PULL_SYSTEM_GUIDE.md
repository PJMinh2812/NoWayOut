# 🧩 HƯỚNG DẪN HỆ THỐNG PUSH/PULL OBJECTS - PUZZLE MECHANICS

## 🎯 MỤC TIÊU
Tạo hệ thống đẩy/kéo vật thể theo 8 hướng cho puzzle mechanics trong game 2D Top-down.

---

## ✅ YÊU CẦU

**Gameplay:**
- Player có thể đẩy/kéo vật thể (box, crate, stone)
- Push theo 8 hướng (N, S, E, W, NE, NW, SE, SW)
- Collision chính xác (không đẩy xuyên tường)
- Visual feedback (animation push, particle)

**Technical:**
- Rigidbody2D physics
- Layer-based collision
- OOP (PushableObject class)

---

## 📋 BƯỚC 1: TẠO PUSHABLE OBJECT SCRIPT (15 phút)

### 1.1: Tạo Script

**Assets/Scripts/PushableObject.cs:**

```csharp
using UnityEngine;

namespace GloomCraft
{
    /// <summary>
    /// Vật thể có thể đẩy/kéo theo 8 hướng
    /// - Sử dụng Rigidbody2D với kinematic
    /// - Collision detection với tường
    /// - Grid snapping (optional)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PushableObject : MonoBehaviour
    {
        [Header("Push Settings")]
        [SerializeField] private float pushSpeed = 2f;
        [SerializeField] private float pushForce = 5f;
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private bool snapToGrid = false;
        [SerializeField] private float gridSize = 1f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color pushingColor = new Color(1f, 0.8f, 0.8f);
        
        private Rigidbody2D _rb;
        private BoxCollider2D _collider;
        private Vector3 _originalPosition;
        private Color _originalColor;
        private bool _isPushing;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();
            
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            // Setup Rigidbody2D
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.mass = 10f; // Nặng hơn player
            _rb.linearDamping = 10f; // Dừng nhanh khi không đẩy

            _originalPosition = transform.position;
            if (spriteRenderer != null)
                _originalColor = spriteRenderer.color;
        }

        /// <summary>
        /// Được gọi từ PlayerController khi đang va chạm và di chuyển
        /// </summary>
        public void Push(Vector2 direction, float force)
        {
            if (!CanPushInDirection(direction))
                return;

            _isPushing = true;

            // Apply force theo hướng
            _rb.AddForce(direction.normalized * force, ForceMode2D.Force);

            // Clamp velocity
            if (_rb.linearVelocity.magnitude > pushSpeed)
            {
                _rb.linearVelocity = _rb.linearVelocity.normalized * pushSpeed;
            }

            // Visual feedback
            if (spriteRenderer != null)
                spriteRenderer.color = pushingColor;
        }

        /// <summary>
        /// Kiểm tra xem có thể đẩy theo hướng này không (raycast tường)
        /// </summary>
        private bool CanPushInDirection(Vector2 direction)
        {
            float rayDistance = 0.6f;
            Vector2 origin = (Vector2)transform.position + _collider.offset;

            RaycastHit2D hit = Physics2D.Raycast(
                origin,
                direction,
                rayDistance,
                wallLayer
            );

            // Debug visual
            Debug.DrawRay(origin, direction * rayDistance, hit ? Color.red : Color.green, 0.1f);

            return !hit;
        }

        private void FixedUpdate()
        {
            // Snap to grid nếu bật
            if (snapToGrid && _rb.linearVelocity.magnitude < 0.1f)
            {
                SnapToGrid();
            }

            // Reset visual khi dừng đẩy
            if (_isPushing && _rb.linearVelocity.magnitude < 0.01f)
            {
                _isPushing = false;
                if (spriteRenderer != null)
                    spriteRenderer.color = _originalColor;
            }
        }

        /// <summary>
        /// Snap vị trí vật thể về lưới (grid)
        /// </summary>
        private void SnapToGrid()
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
            pos.y = Mathf.Round(pos.y / gridSize) * gridSize;
            transform.position = pos;
        }

        /// <summary>
        /// Reset vị trí (dùng cho puzzle reset)
        /// </summary>
        public void ResetPosition()
        {
            transform.position = _originalPosition;
            _rb.linearVelocity = Vector2.zero;
            if (spriteRenderer != null)
                spriteRenderer.color = _originalColor;
        }

        // Gizmos để debug
        private void OnDrawGizmosSelected()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(
                    transform.position + (Vector3)col.offset,
                    col.size
                );
            }
        }
    }
}
```

---

## 📋 BƯỚC 2: CẬP NHẬT PLAYERCONTROLLER2D (10 phút)

### 2.1: Thêm Push Detection

**Mở PlayerController2D.cs, thêm:**

```csharp
[Header("Push/Pull")]
[SerializeField] private float pushForce = 10f;
[SerializeField] private LayerMask pushableLayer;

private PushableObject _currentPushable;
```

### 2.2: Thêm Collision Detection

**Thêm methods:**

```csharp
private void OnCollisionStay2D(Collision2D collision)
{
    // Kiểm tra xem có đang va chạm với pushable object không
    if (((1 << collision.gameObject.layer) & pushableLayer) != 0)
    {
        var pushable = collision.gameObject.GetComponent<PushableObject>();
        if (pushable != null)
        {
            _currentPushable = pushable;
            
            // Nếu đang di chuyển → đẩy object
            var input = GetMoveInput();
            if (input.sqrMagnitude > 0.1f && !IsRolling)
            {
                pushable.Push(input.normalized, pushForce);
            }
        }
    }
}

private void OnCollisionExit2D(Collision2D collision)
{
    if (_currentPushable != null && collision.gameObject == _currentPushable.gameObject)
    {
        _currentPushable = null;
    }
}
```

---

## 📋 BƯỚC 3: SETUP LAYERS & PHYSICS (5 phút)

### 3.1: Tạo Layer "Pushable"

**Edit → Project Settings → Tags and Layers:**

```
Layer 10: Pushable
```

### 3.2: Physics 2D Collision Matrix

**Edit → Project Settings → Physics 2D → Layer Collision Matrix:**

```
Pushable vs Player: ✓ (collide)
Pushable vs Wall: ✓ (collide)
Pushable vs Enemy: ✗ (no collide)
Pushable vs Pushable: ✓ (collide - boxes đẩy boxes)
```

### 3.3: Cấu Hình PlayerController

**Inspector → Player → PlayerController2D:**

```
Pushable Layer: Pushable
Push Force: 10
```

---

## 📋 BƯỚC 4: TẠO PUSHABLE OBJECT PREFAB (10 phút)

### 4.1: Tạo GameObject

**Hierarchy → Right-click → 2D Object → Sprites → Square**

**Đổi tên: "Crate"**

### 4.2: Setup Components

**1. Transform:**
```
Position: (0, 0, 0)
Scale: (1, 1, 1)
```

**2. Sprite Renderer:**
```
Sprite: Square (hoặc sprite crate của bạn)
Color: Brown (#8B4513)
Sorting Layer: Default
Order in Layer: 1
```

**3. Add Component → Rigidbody2D:**
```
Body Type: Dynamic
Mass: 10
Linear Drag: 10
Angular Drag: 0.05
Gravity Scale: 0
Constraints: Freeze Rotation Z ✓
```

**4. Add Component → Box Collider 2D:**
```
Size: (0.9, 0.9) - nhỏ hơn sprite 1 chút
Offset: (0, 0)
```

**5. Add Component → PushableObject:**
```
Push Speed: 2
Push Force: 5
Wall Layer: Wall
Snap To Grid: ✓ (nếu muốn)
Grid Size: 1
```

**6. Set Layer:**
```
Layer: Pushable
```

### 4.3: Tạo Prefab

**Kéo Crate từ Hierarchy vào folder:**
```
Assets/Data/Prefabs/Crate.prefab
```

---

## 📋 BƯỚC 5: TẠO SPRITE/ANIMATION CHO PUSH (TÙY CHỌN - 20 phút)

### 5.1: Tạo Push Animation

**Nếu có sprites riêng cho push:**

**Assets/Animation/Dage/Dage_Push.anim:**

1. Create New Clip
2. Kéo sprites push vào timeline
3. Sample Rate: 12
4. Loop Time: ✓

### 5.2: Thêm vào Animator

**Animator → Add State:**
```
Name: Push
Motion: Dage_Push.anim

Transitions:
Walk → Push
  Condition: IsPushing = true
  Has Exit Time: ✗
  
Push → Walk
  Condition: IsPushing = false
  Has Exit Time: ✗
```

### 5.3: Cập Nhật PlayerAnimationController

**Thêm parameter:**

```csharp
public void SetPushing(bool isPushing)
{
    if (animator != null)
    {
        animator.SetBool("IsPushing", isPushing);
    }
}
```

**Gọi từ PlayerController2D:**

```csharp
private void OnCollisionStay2D(Collision2D collision)
{
    // ... existing code ...
    
    // Trigger push animation
    var animController = GetComponent<PlayerAnimationController>();
    if (animController != null)
    {
        animController.SetPushing(true);
    }
}

private void OnCollisionExit2D(Collision2D collision)
{
    // ... existing code ...
    
    var animController = GetComponent<PlayerAnimationController>();
    if (animController != null)
    {
        animController.SetPushing(false);
    }
}
```

---

## 📋 BƯỚC 6: ADVANCED FEATURES (BONUS)

### 6.1: Pull Mechanics

**Thêm vào PushableObject.cs:**

```csharp
[Header("Pull Settings")]
[SerializeField] private bool canPull = true;
[SerializeField] private KeyCode pullKey = KeyCode.LeftShift;

public bool CanPull => canPull;

public void Pull(Vector2 direction, float force)
{
    // Pull = push theo hướng ngược lại
    Push(-direction, force * 0.8f);
}
```

**PlayerController2D.cs:**

```csharp
private void OnCollisionStay2D(Collision2D collision)
{
    // ... existing push code ...
    
    // Pull khi giữ Shift
    if (Keyboard.current.leftShiftKey.isPressed && pushable.CanPull)
    {
        pushable.Pull(input.normalized, pushForce);
    }
}
```

### 6.2: Weight System

**Các vật nặng khác nhau:**

```csharp
public enum ObjectWeight
{
    Light,   // Player đẩy nhanh
    Medium,  // Push bình thường
    Heavy    // Push chậm, cần 2 players (multiplayer)
}

[SerializeField] private ObjectWeight weight = ObjectWeight.Medium;

private float GetWeightMultiplier()
{
    return weight switch
    {
        ObjectWeight.Light => 1.5f,
        ObjectWeight.Medium => 1f,
        ObjectWeight.Heavy => 0.5f,
        _ => 1f
    };
}

public void Push(Vector2 direction, float force)
{
    float weightedForce = force * GetWeightMultiplier();
    // ... rest of code ...
}
```

### 6.3: Pressure Plate Puzzle

**PressurePlate.cs:**

```csharp
using UnityEngine;
using UnityEngine.Events;

namespace GloomCraft
{
    public class PressurePlate : MonoBehaviour
    {
        [SerializeField] private UnityEvent onActivate;
        [SerializeField] private UnityEvent onDeactivate;
        [SerializeField] private SpriteRenderer plateSprite;
        [SerializeField] private Color activatedColor = Color.green;

        private Color _originalColor;
        private bool _isActivated;

        private void Awake()
        {
            if (plateSprite != null)
                _originalColor = plateSprite.color;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PushableObject>() != null && !_isActivated)
            {
                Activate();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PushableObject>() != null && _isActivated)
            {
                Deactivate();
            }
        }

        private void Activate()
        {
            _isActivated = true;
            if (plateSprite != null)
                plateSprite.color = activatedColor;
            onActivate?.Invoke();
            Debug.Log("Pressure plate activated!");
        }

        private void Deactivate()
        {
            _isActivated = false;
            if (plateSprite != null)
                plateSprite.color = _originalColor;
            onDeactivate?.Invoke();
            Debug.Log("Pressure plate deactivated!");
        }
    }
}
```

**Cách dùng:**
1. Tạo GameObject PressurePlate
2. Add BoxCollider2D (IsTrigger = ✓)
3. Add PressurePlate.cs
4. OnActivate → mở cửa, spawn enemy, etc.

---

## ✅ CHECKLIST HOÀN THÀNH

**Scripts:**
- [ ] PushableObject.cs tạo xong
- [ ] PlayerController2D có push detection
- [ ] OnCollisionStay2D/Exit2D implemented

**Layers & Physics:**
- [ ] Layer "Pushable" created
- [ ] Collision matrix configured
- [ ] PlayerController pushableLayer assigned

**Prefab:**
- [ ] Crate prefab created
- [ ] Rigidbody2D configured
- [ ] PushableObject script attached
- [ ] Layer = Pushable

**Testing:**
- [ ] Di chuyển vào crate → đẩy được
- [ ] Đẩy 8 hướng (W, S, A, D, diagonals)
- [ ] Không đẩy xuyên tường
- [ ] Snap to grid (nếu bật)
- [ ] Visual feedback (color change)

**Advanced (Optional):**
- [ ] Pull mechanics
- [ ] Push animation
- [ ] Weight system
- [ ] Pressure plate puzzle

---

## 🎮 TESTING CHECKLIST

**Scene Setup:**
```
1. Đặt 1 Crate prefab vào scene
2. Đặt Player gần crate
3. Tạo tường xung quanh (Layer = Wall)
```

**Test Cases:**
```
✓ Đẩy lên (W): Crate di chuyển lên
✓ Đẩy xuống (S): Crate di chuyển xuống
✓ Đẩy trái (A): Crate di chuyển trái
✓ Đẩy phải (D): Crate di chuyển phải
✓ Đẩy chéo (W+A): Crate di chuyển chéo
✓ Đẩy vào tường: Crate KHÔNG xuyên tường
✓ Dash vào crate: Crate bị đẩy mạnh hơn
✓ Multiple crates: Crate đẩy crate khác
✓ Snap to grid: Crate dừng đúng grid
```

---

## 🐛 TROUBLESHOOTING

**Crate không đẩy được:**
- ✅ Kiểm tra Layer "Pushable"
- ✅ Collision matrix: Player vs Pushable = ✓
- ✅ PlayerController pushableLayer assigned
- ✅ PushableObject script attached

**Crate xuyên tường:**
- ✅ Wall Layer assigned trong PushableObject
- ✅ Tường có Collider2D
- ✅ Collision matrix: Pushable vs Wall = ✓

**Crate di chuyển quá nhanh/chậm:**
- ✅ Adjust pushSpeed (1-3)
- ✅ Adjust pushForce (5-15)
- ✅ Adjust Rigidbody2D.mass (5-20)
- ✅ Adjust linearDamping (5-15)

**Crate không dừng lại:**
- ✅ Tăng linearDamping (10-20)
- ✅ Giảm pushForce

---

## 📚 TÀI LIỆU THAM KHẢO

**Unity Physics 2D:**
- [Rigidbody2D](https://docs.unity3d.com/ScriptReference/Rigidbody2D.html)
- [Collision Detection](https://docs.unity3d.com/Manual/Collider2D.html)

**Scripts location:**
- `Assets/Scripts/PushableObject.cs`
- `Assets/Scripts/Player/PlayerController2D.cs`

**Prefabs:**
- `Assets/Data/Prefabs/Crate.prefab`

---

## 🎯 PUZZLE IDEAS

**Puzzle Examples:**
1. **Box on Pressure Plate:** Đẩy crate lên plate → mở cửa
2. **Ice Puzzle:** Crate trượt liên tục trên ice tiles
3. **Laser Block:** Crate chặn laser để tắt traps
4. **Weight Puzzle:** Cần nhiều crates trên plate
5. **Maze Push:** Đẩy crate qua mê cung hẹp

---

**Hoàn thành guide này → Puzzle mechanics hoàn chỉnh! 🧩**
