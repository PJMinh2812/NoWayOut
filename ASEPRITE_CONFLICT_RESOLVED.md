# ✅ ĐÃ SỬA - ASEPRITE IMPORTER CONFLICT

## ❌ VẤN ĐỀ ĐÃ ĐƯỢC GIẢI QUYẾT

Đã xóa package third-party conflict. Unity giờ sử dụng **Unity 2D Aseprite Importer chính thức** (v3.0.1).

---

## 🎯 HƯỚNG DẪN SỬ DỤNG ASEPRITE IMPORTER CHÍNH THỨC

### BƯỚC 1: Restart Unity Editor (BẮT BUỘC)

1. **Đóng Unity Editor hoàn toàn**
2. **Mở lại project**
3. **Đợi Unity reimport packages** (~30 giây)

### BƯỚC 2: Kiểm tra file Aseprite

1. **Unity → Project → Assets/Art/MungeonDage/Dage/aseprite files/**
2. **Click vào `Dage_Anim01.aseprite`**
3. **Inspector → Aseprite Importer (nếu thấy lỗi → Restart Unity)**

### BƯỚC 3: Cấu hình Import Settings

**Inspector → Aseprite Importer:**

```yaml
Texture Type: Sprite (2D and UI)
Sprite Mode: Multiple

Aseprite Options:
  ✓ Import Hidden Layers: No
  ✓ Layer Import Mode: Individual Layers (hoặc Merge)
  
Animation Settings:
  ✓ Generate Animations: TRUE ← QUAN TRỌNG!
  ✓ Animation Name Pattern: [layer]_[tag] (hoặc tùy chọn)

Sprite Settings:
  Pixels Per Unit: 16 (điều chỉnh theo size sprite)
  Filter Mode: Point (no filter) ← Quan trọng cho pixel art!
  Compression: None
  Max Size: 2048
```

### BƯỚC 4: Apply & Generate

1. **Nhấn Apply** (góc dưới Inspector)
2. **Đợi Unity generate sprites & animations**
3. **Click mũi tên ▶ bên cạnh Dage_Anim01.aseprite**
4. **Bạn sẽ thấy:**
   - 📊 Texture atlas
   - 🎬 Animation clips (tự động từ tags/layers)
   - 🖼️ Sprites riêng lẻ

---

## 📋 CẤU TRÚC FILE ASEPRITE (Quan trọng!)

Để Unity tự động tạo animation clips, file Aseprite cần có **TAGS**:

### Trong Aseprite App:

1. **Mở `Dage_Anim01.aseprite` trong Aseprite**
2. **Timeline → Tags panel**
3. **Tạo tags cho từng animation:**
   ```
   Tag "Idle": Frame 0-3
   Tag "Walk": Frame 4-9
   Tag "Attack": Frame 10-15
   Tag "Death": Frame 16-21
   ```
4. **Save file**
5. **Unity sẽ tự động tạo:**
   - `Dage_Anim01_Idle.anim`
   - `Dage_Anim01_Walk.anim`
   - `Dage_Anim01_Attack.anim`
   - `Dage_Anim01_Death.anim`

### Nếu file KHÔNG có Tags:

Unity sẽ generate 1 animation duy nhất với tất cả frames.

**Giải pháp:**
- Option A: Thêm tags trong Aseprite
- Option B: Dùng sprites PNG và tạo animations thủ công (xem DAGE_ANIMATION_SETUP_GUIDE.md)

---

## 🎬 SỬ DỤNG ANIMATIONS TRONG UNITY

### Tìm Generated Animations:

**Cách 1: Mở subfolder**
```
Project → Click ▶ bên Dage_Anim01.aseprite
→ Thấy animation clips
```

**Cách 2: Tìm trong Assets**
```
Project → Search "Dage_Anim01"
→ Lọc Type: Animation Clip
```

### Thêm vào Animator:

1. **Window → Animator (Ctrl+7)**
2. **Kéo animation clips vào Animator window**
3. **Setup states & transitions như hướng dẫn**

---

## ⚙️ TROUBLESHOOTING

### Lỗi "Failed to load .aseprite file"

**Nguyên nhân:** File chưa có Aseprite Importer hoặc conflict
**Giải pháp:** 
- ✅ Đã sửa! Restart Unity

### Không thấy animation clips generate

**Kiểm tra:**
1. Generate Animations = ✓ ?
2. File có tags không? (mở trong Aseprite app để kiểm tra)
3. Đã Apply settings chưa?
4. Thử: Right-click file → Reimport

### Sprites bị mờ/blur

**Sửa:**
- Filter Mode = **Point (no filter)**
- Compression = **None**
- Apply

### Animation clips bị duplicate

**Nguyên nhân:** File có nhiều layers + tags
**Giải pháp:**
- Layer Import Mode = **Merge** (gộp tất cả layers)
- Hoặc chọn layer cụ thể trong Animation Name Pattern

---

## 📚 SO SÁNH: ASEPRITE vs PNG WORKFLOW

| Feature | Aseprite Workflow | PNG Manual |
|---------|-------------------|------------|
| Setup time | 5 phút (config import) | 0 phút |
| Animation creation | Auto-generate từ tags | 20 phút (tạo tay) |
| Chỉnh sửa | Sửa trong Aseprite → Auto-sync | Sửa trong Unity |
| Flexibility | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| Dependencies | Unity 2D Aseprite (built-in) | None |
| Best for | Professional workflow | Quick prototyping |

---

## ✅ CHECKLIST

- [ ] Đã restart Unity sau khi xóa conflict package
- [ ] File Dage_Anim01.aseprite load thành công (không lỗi)
- [ ] Inspector hiển thị Aseprite Importer settings
- [ ] Generate Animations = ✓
- [ ] Filter Mode = Point (no filter)
- [ ] Đã Apply settings
- [ ] Thấy animation clips được generate
- [ ] (Optional) Kiểm tra tags trong Aseprite app

---

## 🚀 BƯỚC TIẾP THEO

1. **Restart Unity ngay bây giờ**
2. **Kiểm tra file Dage_Anim01.aseprite load OK**
3. **Cấu hình Import Settings theo hướng dẫn trên**
4. **Làm theo:** [ASEPRITE_ANIMATION_GUIDE.md](ASEPRITE_ANIMATION_GUIDE.md)

---

**Conflict đã được giải quyết! Bây giờ có thể dùng file .aseprite! 🎉**
