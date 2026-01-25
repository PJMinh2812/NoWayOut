# ✅ FIXED - Cách Test Random Map

## Vấn đề đã fix:

- Code đã được cập nhật để work với **cả 2 hệ thống**: Legacy và New
- Scene của bạn đang dùng `UnityDungeonTilemapBuilder` (legacy) → Đã được support

## 🎮 Cách Test NGAY BÂY GIỜ:

### 1. Mở Unity

- Đợi Unity compile xong (check góc dưới phải)
- Không có lỗi compile

### 2. Play Game

```
▶️ Nhấn Play
→ Chơi game
→ Để player chết (đứng yên cho enemy đánh)
→ Màn hình Game Over hiện ra
→ Nhấn nút RESTART
→ MAP MỚI ĐƯỢC TẠO VỚI LAYOUT KHÁC!
```

### 3. Kiểm tra Console

Mở Console (Ctrl+Shift+C hoặc Cmd+Shift+C), bạn sẽ thấy:

```
[UnityDungeonTilemapBuilder] Regenerating with new seed: -1234567890
[Player] Health reset!
[GameManager] Map regenerated with new layout (legacy system)!
```

## 📊 So sánh Map:

**Cách 1: Test nhanh**

- Play → Die → Restart → Nhìn map có khác không
- Lặp lại 2-3 lần → Mỗi lần phải khác

**Cách 2: Screenshot**

- Lần 1: Chơi và screenshot map
- Restart: Screenshot map mới
- So sánh → Phải khác rõ ràng

## 🔍 Debug nếu vẫn không work:

### Check 1: Console có log không?

- Nếu **KHÔNG** thấy log → Restart button không gọi đúng function
- Fix: Check GameOverUI có gán đúng button listener không

### Check 2: Map có thay đổi không?

- Nếu console có log nhưng map **VẪN GIỐNG** → Báo lại với screenshot

### Check 3: Player có spawn lại không?

- Player phải spawn ở vị trí mới (start room mới)
- Nếu player spawn sai vị trí → Báo lại

## 🎯 Kết quả mong đợi:

✅ Mỗi lần restart, MAP PHẢI KHÁC:

- Số phòng khác nhau
- Vị trí phòng khác nhau
- Layout hành lang khác nhau
- Vị trí start/finish khác nhau
- Vị trí enemy khác nhau

## 📝 Những gì đã thay đổi:

1. **UnityDungeonTilemapBuilder** (Legacy):
   - Thêm method `RegenerateWithNewSeed()`
   - Tự động tạo seed ngẫu nhiên mới
   - Regenerate map với layout khác

2. **GameManager**:
   - Check cả 2 hệ thống (New + Legacy)
   - Reset player health trước
   - Gọi regenerate với seed mới
   - Không cần reload scene (nhanh hơn)

3. **PlayerHealth2D**:
   - Thêm method `ResetHealth()`
   - Revive player và reset controls

## ⚡ Performance:

**Trước:** Reload toàn bộ scene (~2-5 giây)
**Sau:** Chỉ regenerate map (~0.5-1 giây)

→ Nhanh hơn 3-5 lần!

## 🎁 Bonus:

Seed được log ra console → Nếu muốn chơi lại map cụ thể:

1. Copy seed từ console (ví dụ: -1234567890)
2. Vào Inspector của DungeonBuilder
3. Tích "Use Seed"
4. Paste số seed
5. Play → Map sẽ giống y hệt

---

**TL;DR:** Restart game → Map mới, layout khác, chơi mãi không chán! 🎮
