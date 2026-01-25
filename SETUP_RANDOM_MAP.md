# Hướng dẫn Setup & Test Random Map Generation

## ⚠️ Vấn đề: "Không có thay đổi ở Unity"

Nếu bạn không thấy thay đổi trong Unity, hãy làm theo các bước sau:

## 🔧 Bước 1: Kiểm tra Unity đã compile chưa

1. **Mở Unity Editor**
2. **Kiểm tra góc dưới bên phải**: 
   - Nếu thấy loading bar hoặc "Compiling..." → Đợi compile xong
   - Nếu thấy số lỗi (ví dụ: 🔴 3 errors) → Mở Console để xem lỗi

3. **Nếu có lỗi compile:**
   ```
   Window → General → Console
   ```
   - Clear console (nút Clear)
   - Chụp ảnh lỗi và báo lại

## 🎮 Bước 2: Kiểm tra Scene Setup

### A. Mở GameScene
1. `File → Open Scene`
2. Chọn `Assets/Settings/Scenes/GameScene.unity`

### B. Tìm MapInitializationManager
1. Trong **Hierarchy**, tìm GameObject có component `MapInitializationManager`
   - Có thể tên là "DungeonManager", "MapManager", hoặc tương tự
   - Dùng Search: gõ `MapInitialization` trong ô search của Hierarchy

2. **Nếu KHÔNG tìm thấy:**
   ```
   Đây là nguyên nhân! Scene chưa có MapInitializationManager
   ```

### C. Kiểm tra GameManager
1. Tìm GameObject có component `GameManager` trong Hierarchy
2. Click vào object đó
3. Trong Inspector, kiểm tra:
   - ✅ Component `GameManager` có script gắn đúng
   - ✅ Field "Game Over UI" có gắn đúng GameObject

## 🔨 Bước 3: Fix - Nếu Scene dùng Legacy System

Nếu scene của bạn đang dùng `UnityDungeonTilemapBuilder` (legacy), làm theo:

### Option 1: Test ngay với Legacy System
1. Tìm GameObject có `UnityDungeonTilemapBuilder`
2. Trong Inspector:
   - Tích ✅ **Use New System**
   - Gán **Map Manager** → Kéo GameObject có `MapInitializationManager` vào
   - Gán **Dungeon Config** → Tạo mới hoặc dùng có sẵn

### Option 2: Migrate hoàn toàn
1. Click vào GameObject có `UnityDungeonTilemapBuilder`
2. Trong Inspector, click nút ⋮ (3 chấm)
3. Chọn **Migrate to New System**
4. Unity sẽ tự động tạo components mới

## 🧪 Bước 4: Test Tính Năng

### Test 1: Generate Map trong Editor
1. Chọn GameObject có `MapInitializationManager`
2. Trong Inspector, click nút ⋮
3. Chọn **Regenerate Map**
4. Lặp lại vài lần → Map phải khác nhau mỗi lần

### Test 2: Test trong Play Mode
1. Nhấn **Play** ▶️
2. Để player chết (đứng yên cho enemy đánh)
3. Màn hình Game Over hiện ra
4. Nhấn nút **Restart**
5. **QUAN TRỌNG**: Mở Console (Ctrl+Shift+C) và check:
   ```
   [MapInitializationManager] Regenerating map with new seed: XXXXXXX
   [MapInitializationManager] Generated dungeon with X rooms
   [GameManager] Map regenerated with new layout!
   ```

## 📋 Checklist Debug

Nếu vẫn không work, check từng mục:

- [ ] **Unity đã compile xong** (không có loading bar)
- [ ] **Không có lỗi compile** (Console sạch, không có dòng đỏ)
- [ ] **Scene đã Save** (Ctrl+S)
- [ ] **Có GameObject với MapInitializationManager** trong scene
- [ ] **MapInitializationManager có DungeonConfig** được gán
- [ ] **GameManager.RestartGame() đang được gọi** khi nhấn Restart button
- [ ] **Console hiển thị log** khi test

## 🔍 Kiểm tra Code đã được apply chưa

### Test GameManager:
1. Mở `Assets/Scripts/GameManager.cs`
2. Tìm method `RestartGame()`
3. Phải có dòng:
   ```csharp
   mapManager.RegenerateWithNewSeed();
   ```

### Test MapInitializationManager:
1. Mở `Assets/Settings/Dungeon/MapInitializationManager.cs`
2. Tìm method `RegenerateWithNewSeed()`
3. Method này phải tồn tại

### Test PlayerHealth2D:
1. Mở `Assets/Scripts/PlayerHealth2D.cs`
2. Tìm method `ResetHealth()`
3. Method này phải tồn tại

## 🆘 Nếu vẫn không work

### Debug Step-by-step:

1. **Test RestartGame có được gọi không:**
   ```csharp
   // Thêm vào đầu GameManager.RestartGame()
   Debug.Log("===== RestartGame CALLED! =====");
   ```

2. **Test MapManager có tìm thấy không:**
   ```csharp
   // Sau dòng FindFirstObjectByType
   Debug.Log($"MapManager found: {mapManager != null}");
   ```

3. **Test trong Play Mode:**
   - Nhấn Play
   - Cho player chết
   - Nhấn Restart
   - Check Console → Phải thấy các log trên

## 📝 Cách hoạt động đúng:

**Luồng chính:**
```
Player chết 
  → GameManager.TriggerGameOver() 
  → Show Game Over UI 
  → User click Restart 
  → GameManager.RestartGame() 
  → MapInitializationManager.RegenerateWithNewSeed() 
  → NEW MAP với seed khác!
```

## 💡 Tips:

1. **Xem seed hiện tại:**
   - Chọn GameObject có MapInitializationManager
   - Trong Inspector → Dungeon Config → Seed number
   - Số này phải thay đổi mỗi lần restart

2. **So sánh map:**
   - Screenshot map lần 1
   - Restart
   - Screenshot map lần 2
   - → Phải khác nhau rõ ràng

3. **Force random mỗi lần play:**
   - Trong DungeonConfig
   - **BỎ TÍCH** "Use Seed" 
   - Hoặc để tích nhưng code sẽ override seed mới

## 📞 Report lỗi:

Nếu vẫn không work, cung cấp thông tin:
1. Screenshot Hierarchy (toàn bộ scene)
2. Screenshot Inspector của GameObject có MapInitializationManager
3. Screenshot Inspector của GameObject có GameManager
4. Screenshot Console sau khi test
5. Screenshot/Text của lỗi compile (nếu có)
