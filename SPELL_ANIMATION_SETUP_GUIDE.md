# 🔮 HƯỚNG DẪN THIẾT LẬP SPELL ANIMATION SYSTEM

## 🎯 MỤC TIÊU

Tạo hệ thống spell/magic animation với 3 spell khác nhau, mỗi spell có Idle và Fire states.

---

## ✅ SPRITES CÓ SẴN (Từ Aseprite)

**Spell 01:**
- ✅ `Dage_Anim01_spell01Idle` (5 frames)
- ✅ `Dage_Anim01_spell01fire` (7 frames)

**Spell 02:**
- ✅ `Dage_Anim01_spell02Idle` (4 frames)
- ✅ `Dage_Anim01_spell02fire` (6 frames)

**Spell 03:**
- ✅ `Dage_Anim01_spell03Idle` (5 frames)
- ✅ `Dage_Anim01_spell03Fire` (8 frames)

**Staff (Optional):**
- ✅ `Dage_Anim01_staffIdle` (4 frames)
- ✅ `Dage_Anim01_staffFire` (7 frames)

---

## 📋 PHƯƠNG ÁN 1: SPELL SYSTEM ĐƠN GIẢN (30 phút)

Spell trigger bằng phím số (1, 2, 3) hoặc mouse click.

### Bước 1.1: Tạo Spell Controller Script

**Assets/Scripts/Player/PlayerSpellController.cs:**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace GloomCraft
{
    /// <summary>
    /// Quản lý spell casting cho player
    /// - 3 spell types (Spell01, Spell02, Spell03)
    /// - Mỗi spell có Idle và Fire animation
    /// - Cast bằng phím số 1, 2, 3
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerSpellController : MonoBehaviour
    {
        [Header("Spell Settings")]
        [SerializeField] private float spell01Cooldown = 2f;
        [SerializeField] private float spell02Cooldown = 3f;
        [SerializeField] private float spell03Cooldown = 5f;

        [Header("Spell Damage")]
        [SerializeField] private int spell01Damage = 10;
        [SerializeField] private int spell02Damage = 20;
        [SerializeField] private int spell03Damage = 35;

        [Header("Spell Range")]
        [SerializeField] private float spell01Range = 5f;
        [SerializeField] private float spell02Range = 7f;
        [SerializeField] private float spell03Range = 10f;

        [Header("Spell Prefabs (Optional)")]
        [SerializeField] private GameObject spell01Projectile;
        [SerializeField] private GameObject spell02Projectile;
        [SerializeField] private GameObject spell03Projectile;

        private Animator _animator;
        private PlayerController2D _controller;
        
        private float _spell01CooldownRemaining;
        private float _spell02CooldownRemaining;
        private float _spell03CooldownRemaining;

        private int _currentSpell = 1; // Default spell 1
        private bool _isCasting;

        public int CurrentSpell => _currentSpell;
        public bool IsCasting => _isCasting;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _controller = GetComponent<PlayerController2D>();
        }

        private void Update()
        {
            // Cooldown timers
            if (_spell01CooldownRemaining > 0f) _spell01CooldownRemaining -= Time.deltaTime;
            if (_spell02CooldownRemaining > 0f) _spell02CooldownRemaining -= Time.deltaTime;
            if (_spell03CooldownRemaining > 0f) _spell03CooldownRemaining -= Time.deltaTime;

            // Không cast khi đang dash hoặc đang cast
            if (_controller != null && (_controller.IsRolling || _isCasting))
                return;

            // Switch spell type
            HandleSpellSwitch();

            // Cast spell
            HandleSpellCast();
        }

        private void HandleSpellSwitch()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                SwitchToSpell(1);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                SwitchToSpell(2);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                SwitchToSpell(3);
            }
        }

        private void SwitchToSpell(int spellNumber)
        {
            if (_currentSpell == spellNumber) return;

            _currentSpell = spellNumber;
            _animator.SetInteger("SpellType", spellNumber);
            
            Debug.Log($"[Spell] Switched to Spell {spellNumber}");
        }

        private void HandleSpellCast()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // Cast spell với Left Click hoặc Q key
            bool castInput = mouse.leftButton.wasPressedThisFrame 
                          || Keyboard.current.qKey.wasPressedThisFrame;

            if (!castInput) return;

            // Check cooldown
            bool canCast = _currentSpell switch
            {
                1 => _spell01CooldownRemaining <= 0f,
                2 => _spell02CooldownRemaining <= 0f,
                3 => _spell03CooldownRemaining <= 0f,
                _ => false
            };

            if (canCast)
            {
                CastCurrentSpell();
            }
            else
            {
                Debug.Log($"[Spell] Spell {_currentSpell} on cooldown!");
            }
        }

        private void CastCurrentSpell()
        {
            // Trigger animation
            _animator.SetTrigger("CastSpell");
            _isCasting = true;

            // Set cooldown
            switch (_currentSpell)
            {
                case 1:
                    _spell01CooldownRemaining = spell01Cooldown;
                    Debug.Log($"[Spell] Cast Spell 01 - Damage: {spell01Damage}");
                    break;
                case 2:
                    _spell02CooldownRemaining = spell02Cooldown;
                    Debug.Log($"[Spell] Cast Spell 02 - Damage: {spell02Damage}");
                    break;
                case 3:
                    _spell03CooldownRemaining = spell03Cooldown;
                    Debug.Log($"[Spell] Cast Spell 03 - Damage: {spell03Damage}");
                    break;
            }

            // Spawn projectile sẽ được gọi từ Animation Event
        }

        /// <summary>
        /// Được gọi từ Animation Event khi spell animation kết thúc
        /// </summary>
        public void OnSpellCastComplete()
        {
            _isCasting = false;
            Debug.Log("[Spell] Cast complete!");
        }

        /// <summary>
        /// Được gọi từ Animation Event tại frame spawn projectile
        /// </summary>
        public void OnSpawnSpellProjectile()
        {
            GameObject projectilePrefab = _currentSpell switch
            {
                1 => spell01Projectile,
                2 => spell02Projectile,
                3 => spell03Projectile,
                _ => null
            };

            if (projectilePrefab == null) return;

            // Get aim direction
            Vector2 aimDirection = Vector2.right; // Default
            if (_controller != null)
            {
                float aimAngle = _controller.AimAngleDeg;
                aimDirection = new Vector2(
                    Mathf.Cos(aimAngle * Mathf.Deg2Rad),
                    Mathf.Sin(aimAngle * Mathf.Deg2Rad)
                );
            }

            // Spawn projectile
            var projectile = Instantiate(
                projectilePrefab, 
                transform.position, 
                Quaternion.identity
            );

            // Set projectile direction
            var proj = projectile.GetComponent<Projectile2D>();
            if (proj != null)
            {
                proj.Fire(aimDirection);
            }

            Debug.Log($"[Spell] Spawned projectile for Spell {_currentSpell}");
        }

        // UI Helper - Get cooldown percentage
        public float GetSpellCooldownPercent(int spellNumber)
        {
            return spellNumber switch
            {
                1 => Mathf.Clamp01(_spell01CooldownRemaining / spell01Cooldown),
                2 => Mathf.Clamp01(_spell02CooldownRemaining / spell02Cooldown),
                3 => Mathf.Clamp01(_spell03CooldownRemaining / spell03Cooldown),
                _ => 0f
            };
        }
    }
}
```

---

### Bước 1.2: Cấu Hình Animator Controller

**Window → Animator → Dage.controller**

#### **A. Tạo Parameters:**

```
SpellType (Int) - Giá trị: 1, 2, 3
CastSpell (Trigger) - Trigger khi cast
IsCasting (Bool) - Đang cast hay không
```

#### **B. Tạo Spell States:**

**Cấu trúc Animator:**

```
Idle/Walk Layer (Base Layer):
  ├── Idle_8Dir
  ├── Walk_8Dir
  ├── Dash_8Dir
  └── (existing states...)

Spell Layer (New Layer - Weight: 1):
  ├── Spell01_Idle
  ├── Spell01_Fire
  ├── Spell02_Idle
  ├── Spell02_Fire
  ├── Spell03_Idle
  └── Spell03_Fire
```

#### **C. Tạo Layer mới cho Spell:**

1. **Animator window → Layers tab**
2. Click (+) → **New Layer**
3. Đổi tên: `Spell Layer`
4. Settings:
   - Weight: 1
   - Blending: Override (hoặc Additive nếu muốn blend với movement)

#### **D. Thêm States vào Spell Layer:**

**Chọn Spell Layer → Right-click → Create State → Empty:**

**Spell 01:**
1. State: `Spell01_Idle`
   - Motion: `Dage_Anim01_spell01Idle`
   - Speed: 1
   - Loop: ✓

2. State: `Spell01_Fire`
   - Motion: `Dage_Anim01_spell01fire`
   - Speed: 1.5 (tùy chỉnh)
   - Loop: ✗

**Spell 02:**
3. State: `Spell02_Idle`
   - Motion: `Dage_Anim01_spell02Idle`

4. State: `Spell02_Fire`
   - Motion: `Dage_Anim01_spell02fire`

**Spell 03:**
5. State: `Spell03_Idle`
   - Motion: `Dage_Anim01_spell03Idle`

6. State: `Spell03_Fire`
   - Motion: `Dage_Anim01_spell03Fire`

---

### Bước 1.3: Setup Transitions

**Trong Spell Layer:**

#### **1. Entry → Default (Spell01_Idle):**
```
Entry → Spell01_Idle (Set as Layer Default State)
```

#### **2. Spell Type Switching:**

**Spell01_Idle ↔ Spell02_Idle:**
```
Spell01_Idle → Spell02_Idle
  Condition: SpellType == 2
  Has Exit Time: ✗
  Duration: 0.1

Spell02_Idle → Spell01_Idle
  Condition: SpellType == 1
  Has Exit Time: ✗
  Duration: 0.1
```

**Tương tự cho Spell03:**
```
Spell01_Idle ↔ Spell03_Idle (SpellType == 3 / == 1)
Spell02_Idle ↔ Spell03_Idle (SpellType == 3 / == 2)
```

#### **3. Cast Transitions:**

**Spell01_Idle → Spell01_Fire:**
```
Condition: CastSpell (Trigger)
Has Exit Time: ✗
Duration: 0.05
```

**Spell01_Fire → Spell01_Idle:**
```
Condition: None
Has Exit Time: ✓
Exit Time: 0.95
Duration: 0.1
```

**Lặp lại cho Spell02 và Spell03.**

---

### Bước 1.4: Thêm Animation Events

**Để spawn projectile và kết thúc cast:**

1. **Window → Animation**
2. **Chọn Player trong Hierarchy**
3. **Animation dropdown → Chọn Dage_Anim01_spell01fire**

#### **Event 1: Spawn Projectile (Frame giữa animation)**

**Frame ~50% animation:**
- Click Add Event (tại timeline)
- Function: `OnSpawnSpellProjectile()`

#### **Event 2: Cast Complete (Frame cuối)**

**Frame cuối (95-100%):**
- Add Event
- Function: `OnSpellCastComplete()`

**Lặp lại cho spell02fire và spell03Fire.**

---

### Bước 1.5: Setup trong Scene

**Hierarchy → Player:**

1. **Add Component → Player Spell Controller**

2. **Inspector settings:**
```
Spell01 Cooldown: 2
Spell02 Cooldown: 3
Spell03 Cooldown: 5

Spell01 Damage: 10
Spell02 Damage: 20
Spell03 Damage: 35

Spell01 Range: 5
Spell02 Range: 7
Spell03 Range: 10

Spell Prefabs: (Gán projectile prefabs nếu có)
```

---

## 📋 PHƯƠNG ÁN 2: SPELL 8 HƯỚNG VỚI BLEND TREE (60 phút)

Nếu có sprites spell cho 8 hướng (như Dash_8Dir).

### Bước 2.1: Tạo Blend Trees cho mỗi Spell

**Thay vì single states, tạo Blend Trees:**

```
Spell01_Fire_8Dir (Blend Tree)
  ├── spell01fire (North)
  ├── spell01fire_L (North-West)
  ├── spell01fire (East)
  └── ... (8 hướng)
```

**Parameters giữ nguyên:** Horizontal, Vertical

**Animation sẽ cast theo hướng player đang nhìn.**

---

## 🎮 PHƯƠNG ÁN 3: STAFF WEAPON SYSTEM (45 phút)

Nếu muốn player cầm staff/wand:

### Bước 3.1: Weapon State Machine

**Tạo Layer "Weapon":**

```
Weapon Layer:
  ├── Unarmed (normal animations)
  ├── Staff_Idle
  ├── Staff_Fire
  └── Transitions based on HasWeapon (Bool)
```

### Bước 3.2: Script

**PlayerWeaponController.cs:**

```csharp
public enum WeaponType
{
    None,
    Staff,
    Sword,
    Bow
}

[SerializeField] private WeaponType currentWeapon = WeaponType.None;

// Switch weapon
public void EquipWeapon(WeaponType weapon)
{
    currentWeapon = weapon;
    animator.SetInteger("WeaponType", (int)weapon);
}
```

---

## 🎯 KHUYẾN NGHỊ CHO DỰ ÁN

**→ Bắt đầu với PHƯƠNG ÁN 1** (Spell system đơn giản)

**Lý do:**
- ✅ Nhanh nhất (30 phút)
- ✅ Sử dụng animations có sẵn từ Aseprite
- ✅ Đủ cho gameplay cơ bản
- ✅ Dễ mở rộng sau

**Nâng cấp sau:**
- Phase 2: Thêm Spell 8 hướng nếu cần
- Phase 3: Staff/Weapon system nếu có design

---

## ✅ CHECKLIST HOÀN THÀNH

**Scripts:**
- [ ] PlayerSpellController.cs created
- [ ] Spell switch (1, 2, 3 keys) working
- [ ] Spell cast (Left Click or Q) working
- [ ] Cooldown system functional

**Animator:**
- [ ] Spell Layer created (Weight = 1)
- [ ] 6 States added (3 Idle + 3 Fire)
- [ ] Parameters: SpellType, CastSpell, IsCasting
- [ ] Transitions setup (Idle ↔ Fire, Spell switching)

**Animation Events:**
- [ ] OnSpawnSpellProjectile() in spell01fire
- [ ] OnSpellCastComplete() in spell01fire
- [ ] Events added to spell02fire
- [ ] Events added to spell03Fire

**Testing:**
- [ ] Press 1: Switch to Spell 1 (Idle animation)
- [ ] Press 2: Switch to Spell 2 (Idle animation)
- [ ] Press 3: Switch to Spell 3 (Idle animation)
- [ ] Left Click: Cast current spell (Fire animation)
- [ ] Spell returns to Idle after cast
- [ ] Cooldown prevents spam casting
- [ ] Projectile spawns (if prefab assigned)

---

## 🎮 CONTROLS SUMMARY

**Spell Controls:**
```
1 - Switch to Spell 1 (Fast, low damage)
2 - Switch to Spell 2 (Medium speed, medium damage)
3 - Switch to Spell 3 (Slow, high damage)

Left Click / Q - Cast current spell
```

**Existing Controls:**
```
WASD - Movement
Shift/Space - Dash
Mouse - Aim direction (for spell)
```

---

## 🐛 TROUBLESHOOTING

**Spell animation không chạy:**
- ✅ Kiểm tra Spell Layer Weight = 1
- ✅ Kiểm tra transitions có đúng conditions
- ✅ Verify PlayerSpellController attached

**Spell cast liên tục:**
- ✅ Check OnSpellCastComplete() được gọi
- ✅ Verify `_isCasting` flag reset về false

**Animation bị override bởi Walk:**
- ✅ Spell Layer phải ở trên Base Layer
- ✅ Hoặc dùng Blending mode khác

**Projectile không spawn:**
- ✅ Check Animation Event đã add
- ✅ Verify function name đúng: OnSpawnSpellProjectile()
- ✅ Gán Spell Prefab trong Inspector

---

## 📚 TÀI LIỆU THAM KHẢO

**Animations location:**
- `Assets/Art/MungeonDage/Dage/aseprite files/Dage_Anim01.aseprite`
- Generated clips: `Dage_Anim01_spell01Idle`, `spell01fire`, etc.

**Scripts location:**
- `Assets/Scripts/Player/PlayerSpellController.cs` (new)
- `Assets/Scripts/Player/PlayerController2D.cs`
- `Assets/Scripts/Projectile2D.cs` (existing)

**Unity Docs:**
- [Animation Layers](https://docs.unity3d.com/Manual/AnimationLayers.html)
- [Animation Events](https://docs.unity3d.com/Manual/script-AnimationWindowEvent.html)

---

## 🚀 NÂNG CAO (OPTIONAL)

### **1. Spell VFX:**
- Thêm Particle Systems khi cast
- Screen shake cho spell mạnh
- Camera flash effect

### **2. Spell Combo System:**
- Cast spell liên tiếp = combo
- Bonus damage/effects

### **3. Mana System:**
- Mỗi spell tốn mana
- Mana regeneration

### **4. Spell Upgrade:**
- Level up spell = damage tăng
- Cooldown giảm
- Range tăng

---

**Hoàn thành guide này → Spell system hoàn chỉnh với 3 spell types! 🔮**

*Next: Tích hợp với UI để hiển thị spell icons và cooldowns.*
