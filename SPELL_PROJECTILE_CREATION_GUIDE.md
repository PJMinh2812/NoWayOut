# 🎆 HƯỚNG DẪN TẠO SPELL PROJECTILE VỚI ANIMATION

## 🎯 MỤC TIÊU

Biến các sprite spell (idle và fire) thành animated projectiles bay trong game.

---

## ✅ SPRITES PHÂN TÍCH

Bạn có sẵn các sprites spell **KHÔNG CÓ NHÂN VẬT** - Perfect cho projectiles!

**Spell 01 (Fire Magic - Màu đỏ/cam):**
- Right: `Dage_spell01fire1-7.png` (7 frames)
- Left: `Dage_spell01fire_L1-7.png` (7 frames)
- Idle: `Dage_spell01Idle1-5.png` (5 frames) + Left variant

**Spell 02 (Ice/Blue Magic):**
- Right: `Dage_spell02fire1-6.png` (6 frames)
- Left: `Dage_spell02fire_L1-6.png` (6 frames)
- Idle: `Dage_spell02Idle1-4.png` (4 frames) + Left variant

**Spell 03 (Lightning/Purple Magic):**
- Right: `Dage_spell03fire1-8.png` (8 frames)
- Left: `Dage_spell03fire_L1-8.png` (8 frames)
- Idle: `Dage_spell03Idle1-5.png` (5 frames) + Left variant

**Staff Magic:**
- Staff fire sprites cũng có thể dùng!

---

## 📋 PHƯƠNG ÁN: TẠO ANIMATED PROJECTILE (45 phút)

### Bước 1: Tạo Animation Clips cho Projectiles (15 phút)

#### 1.1: Tạo Folder

**Project → Assets → Animation → Create Folder:**
- Tên: `Projectiles`

#### 1.2: Tạo Animation Spell01_Projectile

**1. Tạo GameObject tạm:**
- Hierarchy → Right-click → 2D Object → Sprites → Square
- Đổi tên: `Spell01_Projectile_Temp`

**2. Tạo Animation:**
- Window → Animation → Animation (Ctrl+6)
- Chọn Spell01_Projectile_Temp
- Click "Create"
- Save: `Assets/Animation/Projectiles/Spell01_Projectile_Travel.anim`

**3. Thêm sprites:**
- **Project → Assets/Art/MungeonDage/Dage/anim/**
- Chọn 7 sprites (Ctrl+Click):
  ```
  Dage_spell01fire1.png
  Dage_spell01fire2.png
  Dage_spell01fire3.png
  Dage_spell01fire4.png
  Dage_spell01fire5.png
  Dage_spell01fire6.png
  Dage_spell01fire7.png
  ```
- Kéo vào Animation timeline (dòng Sprite Renderer)

**4. Cấu hình:**
- **Sample Rate: 15** (tốc độ animation)
- **Loop Time: ✓** (animation lặp khi projectile bay)

**5. Lặp lại cho Spell02 và Spell03:**

**Spell02_Projectile_Travel.anim:**
- Sprites: `Dage_spell02fire1-6.png`
- Sample Rate: 12

**Spell03_Projectile_Travel.anim:**
- Sprites: `Dage_spell03fire1-8.png`
- Sample Rate: 18

**6. Xóa temp GameObjects sau khi tạo xong animations.**

---

### Bước 2: Tạo Spell Projectile Script (15 phút)

**Assets/Scripts/SpellProjectile.cs:**

```csharp
using UnityEngine;

namespace GloomCraft
{
    /// <summary>
    /// Spell Projectile với animation
    /// - Bay theo hướng được set
    /// - Animated sprite
    /// - Gây damage cho enemy
    /// - Tự hủy sau lifetime hoặc khi va chạm
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Collider2D))]
    public class SpellProjectile : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 8f;
        [SerializeField] private float lifetime = 3f;

        [Header("Damage")]
        [SerializeField] private int damage = 10;
        [SerializeField] private float knockbackForce = 3f;

        [Header("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private bool destroyOnHit = true;

        private Rigidbody2D _rb;
        private Animator _animator;
        private Vector2 _direction;
        private float _timeAlive;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();

            // Setup Rigidbody2D
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        /// <summary>
        /// Bắn projectile theo hướng
        /// </summary>
        public void Fire(Vector2 direction)
        {
            _direction = direction.normalized;
            _rb.linearVelocity = _direction * speed;

            // Xoay projectile theo hướng bay
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // Flip sprite nếu bay sang trái (< 0°)
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && angle > 90f || angle < -90f)
            {
                spriteRenderer.flipY = true;
            }
        }

        private void Update()
        {
            _timeAlive += Time.deltaTime;

            // Auto destroy sau lifetime
            if (_timeAlive >= lifetime)
            {
                DestroyProjectile();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Hit enemy
            if (other.TryGetComponent<Enemy2D>(out var enemy))
            {
                var hitDir = (Vector2)other.transform.position - (Vector2)transform.position;
                enemy.TakeDamage(damage, hitDir.normalized, knockbackForce);

                Debug.Log($"[SpellProjectile] Hit enemy! Damage: {damage}");

                if (destroyOnHit)
                {
                    SpawnHitEffect();
                    DestroyProjectile();
                }
                return;
            }

            // Hit wall/obstacle
            if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
            {
                SpawnHitEffect();
                DestroyProjectile();
            }
        }

        private void SpawnHitEffect()
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
        }

        private void DestroyProjectile()
        {
            Destroy(gameObject);
        }
    }
}
```

---

### Bước 3: Tạo Projectile Prefabs (15 phút)

#### 3.1: Tạo Spell01 Projectile Prefab

**1. Tạo GameObject:**
- Hierarchy → Right-click → Create Empty
- Đổi tên: `Spell01_Projectile`

**2. Add Components:**

**A. Sprite Renderer:**
- Add Component → Sprite Renderer
- Sprite: `Dage_spell01fire1.png` (frame đầu)
- Material: Default
- Sorting Layer: Projectile (tạo layer mới nếu chưa có)
- Order in Layer: 5

**B. Animator:**
- Add Component → Animator
- Controller: Tạo mới → `Assets/Animation/Projectiles/Spell01_ProjectileController.controller`
- Double-click controller → Kéo animation `Spell01_Projectile_Travel` vào
- Set as default state (màu cam)

**C. Rigidbody2D:**
- Add Component → Rigidbody2D
- Body Type: Dynamic
- Gravity Scale: 0
- Linear Drag: 0
- Angular Drag: 0
- Constraints: Freeze Rotation Z ✓

**D. Circle Collider 2D:**
- Add Component → Circle Collider 2D
- Is Trigger: ✓
- Radius: 0.2 (tùy chỉnh theo sprite size)

**E. SpellProjectile Script:**
- Add Component → Spell Projectile
- Speed: 8
- Lifetime: 3
- Damage: 10
- Knockback Force: 3

**3. Tạo Prefab:**
- Kéo `Spell01_Projectile` từ Hierarchy vào folder:
  ```
  Assets/Data/Prefabs/Spell01_Projectile.prefab
  ```
- Xóa GameObject trong Hierarchy

#### 3.2: Lặp lại cho Spell02 và Spell03

**Spell02_Projectile.prefab:**
- Animation: `Spell02_Projectile_Travel.anim`
- Damage: 20
- Speed: 10 (nhanh hơn)
- Color Tint: Xanh lam (optional)

**Spell03_Projectile.prefab:**
- Animation: `Spell03_Projectile_Travel.anim`
- Damage: 35
- Speed: 12 (nhanh nhất)
- Color Tint: Tím (optional)

---

### Bước 4: Gán Prefabs vào PlayerSpellController

**Hierarchy → Player → Inspector:**

**PlayerSpellController component:**
```
Spell01 Projectile: Spell01_Projectile prefab
Spell02 Projectile: Spell02_Projectile prefab
Spell03 Projectile: Spell03_Projectile prefab
```

---

## 🎨 NÂNG CAO: THÊM HIT EFFECTS (20 phút)

### Bước 5.1: Tạo Hit Effect Animation

**1. Tạo animation từ spell Idle sprites:**

**Spell01_Hit.anim:**
- Sprites: `Dage_spell01Idle1-5.png`
- Sample Rate: 20 (nhanh)
- Loop: ✗ (không loop)

**2. Tạo Prefab:**

**GameObject: Spell01_HitEffect**
- Sprite Renderer + Animator
- Animation: Spell01_Hit
- **Add Script: SelfDestruct.cs**

**SelfDestruct.cs:**
```csharp
using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
```

**3. Gán vào Projectile:**
- Spell01_Projectile → Hit Effect Prefab: Spell01_HitEffect

---

## 🎯 VARIANT: SPELL HOMING (TÙY CHỌN)

Tạo spell tự động đuổi theo enemy.

**HomingSpellProjectile.cs:**

```csharp
public class HomingSpellProjectile : SpellProjectile
{
    [Header("Homing")]
    [SerializeField] private float homingStrength = 5f;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private LayerMask enemyLayer;

    private Transform _target;

    private void Update()
    {
        base.Update();

        // Find closest enemy
        if (_target == null)
        {
            FindClosestEnemy();
        }

        // Home to target
        if (_target != null)
        {
            Vector2 directionToTarget = (_target.position - transform.position).normalized;
            Vector2 newDirection = Vector2.Lerp(_direction, directionToTarget, homingStrength * Time.deltaTime);
            
            _rb.linearVelocity = newDirection * speed;
            _direction = newDirection;

            // Rotate towards target
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void FindClosestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, enemyLayer);
        
        float closestDistance = Mathf.Infinity;
        
        foreach (var hit in hits)
        {
            float distance = Vector2.Distance(transform.position, hit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                _target = hit.transform;
            }
        }
    }
}
```

---

## 📊 SO SÁNH 3 SPELL PROJECTILES

| Spell | Animation | Frames | Speed | Damage | Visual | Đặc điểm |
|-------|-----------|--------|-------|--------|--------|----------|
| Spell 01 | spell01fire | 7 | 8 | 10 | 🔥 Lửa đỏ | Nhanh, spam được |
| Spell 02 | spell02fire | 6 | 10 | 20 | ❄️ Băng xanh | Trung bình |
| Spell 03 | spell03fire | 8 | 12 | 35 | ⚡ Sét tím | Chậm, mạnh |

---

## ✅ CHECKLIST HOÀN THÀNH

**Animations:**
- [ ] Spell01_Projectile_Travel.anim created
- [ ] Spell02_Projectile_Travel.anim created
- [ ] Spell03_Projectile_Travel.anim created
- [ ] Hit effect animations (optional)

**Scripts:**
- [ ] SpellProjectile.cs created
- [ ] SelfDestruct.cs created (for hit effects)
- [ ] HomingSpellProjectile.cs (optional)

**Prefabs:**
- [ ] Spell01_Projectile.prefab created
- [ ] Spell02_Projectile.prefab created
- [ ] Spell03_Projectile.prefab created
- [ ] Hit effect prefabs (optional)

**Setup:**
- [ ] Prefabs gán vào PlayerSpellController
- [ ] Layer "Projectile" created
- [ ] Collision matrix configured

**Testing:**
- [ ] Cast Spell 1 → animated projectile spawns
- [ ] Cast Spell 2 → different animation
- [ ] Cast Spell 3 → third animation
- [ ] Projectile hits enemy → damage applied
- [ ] Projectile hits wall → destroyed
- [ ] Hit effects spawn (if setup)

---

## 🎮 PHYSICS 2D COLLISION MATRIX

**Edit → Project Settings → Physics 2D:**

```
Projectile vs Enemy: ✓ (trigger)
Projectile vs Wall: ✓ (trigger)
Projectile vs Player: ✗ (no collide)
Projectile vs Projectile: ✗ (no collide)
```

---

## 🐛 TROUBLESHOOTING

**Projectile không có animation:**
- ✅ Kiểm tra Animator Controller đã gán
- ✅ Verify animation clip đã kéo vào controller
- ✅ Check Sprite Renderer có sprites

**Projectile không bay:**
- ✅ Rigidbody2D Gravity Scale = 0
- ✅ Fire() method được gọi
- ✅ Speed > 0

**Projectile xuyên qua enemy:**
- ✅ Circle Collider Is Trigger = ✓
- ✅ Enemy có Collider2D
- ✅ Collision matrix đúng

**Animation bị flip sai:**
- ✅ Adjust flip logic trong Fire()
- ✅ Hoặc dùng separate animations (R/L)

**Projectile không destroy:**
- ✅ Check OnTriggerEnter2D được gọi
- ✅ Verify layer names đúng
- ✅ Test với Debug.Log

---

## 🚀 TIPS NÂNG CAO

### **1. Particle Trail:**
```csharp
[SerializeField] private TrailRenderer trail;

private void Awake()
{
    // ...
    if (trail != null)
    {
        trail.time = 0.3f;
        trail.startWidth = 0.2f;
        trail.endWidth = 0f;
    }
}
```

### **2. Speed Variation:**
```csharp
// Projectile nhanh dần
private void Update()
{
    speed += 0.5f * Time.deltaTime;
    _rb.linearVelocity = _direction * speed;
}
```

### **3. Piercing Projectile:**
```csharp
[SerializeField] private int maxPierceCount = 3;
private int _pierceCount = 0;

private void OnTriggerEnter2D(Collider2D other)
{
    if (other.TryGetComponent<Enemy2D>(out var enemy))
    {
        enemy.TakeDamage(damage, ...);
        
        _pierceCount++;
        
        if (_pierceCount >= maxPierceCount)
        {
            DestroyProjectile();
        }
    }
}
```

### **4. Area of Effect (AoE):**
```csharp
private void DestroyProjectile()
{
    // Explode và damage enemies xung quanh
    Collider2D[] hits = Physics2D.OverlapCircleAll(
        transform.position, 
        explosionRadius, 
        enemyLayer
    );
    
    foreach (var hit in hits)
    {
        if (hit.TryGetComponent<Enemy2D>(out var enemy))
        {
            enemy.TakeDamage(damage, ...);
        }
    }
    
    Destroy(gameObject);
}
```

---

## 📚 TÀI LIỆU THAM KHẢO

**Sprites location:**
- `Assets/Art/MungeonDage/Dage/anim/Dage_spell01fire1-7.png`
- `Assets/Art/MungeonDage/Dage/anim/Dage_spell02fire1-6.png`
- `Assets/Art/MungeonDage/Dage/anim/Dage_spell03fire1-8.png`

**Scripts:**
- `Assets/Scripts/SpellProjectile.cs` (new)
- `Assets/Scripts/Player/PlayerSpellController.cs` (existing)

**Prefabs:**
- `Assets/Data/Prefabs/Spell01_Projectile.prefab`
- `Assets/Data/Prefabs/Spell02_Projectile.prefab`
- `Assets/Data/Prefabs/Spell03_Projectile.prefab`

**Unity Docs:**
- [2D Animation](https://docs.unity3d.com/Manual/class-AnimationClip.html)
- [Rigidbody2D](https://docs.unity3d.com/ScriptReference/Rigidbody2D.html)
- [OnTriggerEnter2D](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnTriggerEnter2D.html)

---

## 🎯 WORKFLOW SUMMARY

1. **Tạo Animations** từ spell fire sprites → 3 clips
2. **Tạo Script** SpellProjectile.cs → Movement + Damage
3. **Tạo Prefabs** → Animator + Rigidbody2D + Collider + Script
4. **Gán vào PlayerSpellController** → Test!
5. **Polish** → Hit effects, trails, sounds

---

**Hoàn thành guide này → 3 spell projectiles đẹp mắt với animation! 🎆**

*Mỗi spell có visual khác nhau, tạo sự đa dạng cho combat system!*
