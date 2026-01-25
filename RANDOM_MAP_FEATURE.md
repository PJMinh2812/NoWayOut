# Random Map Generation Feature

## Tổng quan

Tính năng này cho phép tạo layout map khác nhau mỗi lần chơi game, tránh việc map bị lặp lại giống nhau.

## Các thay đổi đã thực hiện

### 1. MapInitializationManager.cs

- **Method mới: `RegenerateWithNewSeed()`**
  - Tạo seed ngẫu nhiên mới mỗi lần gọi
  - Clear map hiện tại
  - Generate map mới với seed khác nhau
  - Đảm bảo layout hoàn toàn khác so với lần chơi trước

### 2. GameManager.cs

- **Cập nhật `RestartGame()`**
  - Thay vì reload toàn bộ scene (mất thời gian)
  - Chỉ regenerate map với seed mới (nhanh hơn)
  - Reset health của player
  - Giữ lại các object khác trong scene

### 3. PlayerHealth2D.cs

- **Method mới: `ResetHealth()`**
  - Reset máu về full
  - Revive player
  - Re-enable controls
  - Đảm bảo player sẵn sàng chơi lại

## Cách hoạt động

1. **Khi bắt đầu game:**
   - MapInitializationManager sử dụng DungeonConfig để generate map
   - Nếu `useSeed = false` → random seed tự động
   - Nếu `useSeed = true` → dùng seed cố định (để test)

2. **Khi restart game (nhấn nút Restart):**
   - GameManager gọi `RegenerateWithNewSeed()`
   - Seed mới được tạo ngẫu nhiên
   - Map được tạo lại hoàn toàn khác
   - Player được reset health và spawn vào vị trí mới

## Lợi ích

✅ **Replayability cao hơn** - Mỗi lần chơi là một trải nghiệm mới  
✅ **Performance tốt hơn** - Không cần reload scene  
✅ **Seamless transition** - Chuyển tiếp mượt mà hơn  
✅ **Linh hoạt** - Có thể dùng seed cố định để debug

## Cách sử dụng

### Trong Editor:

1. Chọn GameObject có `MapInitializationManager`
2. Trong Inspector, tìm `DungeonConfig`
3. Bỏ tích `Use Seed` để map random mỗi lần
4. Hoặc tích `Use Seed` và điền số để test map cụ thể

### Trong Game:

- Chơi game bình thường
- Khi chết → màn hình Game Over hiện ra
- Nhấn nút **Restart**
- Map mới được tạo với layout hoàn toàn khác!

## Technical Details

### Seed Generation:

```csharp
config.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
```

### Map Regeneration Flow:

1. Generate new random seed
2. Clear existing tilemap and entities
3. Generate new dungeon layout
4. Render new tilemap
5. Spawn player at new start position
6. Spawn enemies and items

## Notes

- Nếu `MapInitializationManager` không tìm thấy, system sẽ fallback về reload scene (legacy behavior)
- Seed được log ra console để có thể reproduce map nếu cần
- Player health và controls được reset tự động khi restart

## Future Enhancements

- [ ] Lưu seed của map hiện tại để player có thể replay nếu thích
- [ ] Tăng difficulty theo số lần chơi (more enemies, larger map, etc.)
- [ ] Daily challenge với seed cố định mỗi ngày
- [ ] Leaderboard với seed cụ thể
