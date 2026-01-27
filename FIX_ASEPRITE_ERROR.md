# 🔧 SỬA LỖI: Failed to load Aseprite file

## ❌ LỖI
```
Failed to load 'Dage_Anim01.aseprite'. 
File may be corrupted or was serialized with a newer version of Unity.
```

## ✅ NGUYÊN NHÂN
Unity **THIẾU Aseprite Importer Package** để đọc file `.aseprite`

---

## 🛠️ GIẢI PHÁP 1: CÀI ĐẶT ASEPRITE IMPORTER (Khuyến nghị)

### Cách 1: Cài qua Package Manager (Nhanh nhất)

1. **Unity → Window → Package Manager**
2. **Góc trái trên: "+" → Add package from git URL**
3. **Nhập URL:**
   ```
   https://github.com/martinhodler/unity-aseprite-importer.git
   ```
4. **Nhấn Add**
5. **Đợi Unity import xong** (~30 giây)
6. **Restart Unity Editor**

### Cách 2: Cài thủ công vào Packages/manifest.json

1. **Mở file:** `Packages/manifest.json`
2. **Thêm dòng này vào "dependencies":**
   ```json
   "com.seanba.super-tiled2unity": "https://github.com/martinhodler/unity-aseprite-importer.git",
   ```
   
   **Hoặc dùng package chính thức (nếu có):**
   ```json
   "com.unity.2d.aseprite": "1.0.0",
   ```

3. **Save file**
4. **Unity sẽ tự động reload**

### Sau khi cài xong:

1. **Project → Click Dage_Anim01.aseprite**
2. **Inspector sẽ hiện Aseprite Importer settings**
3. **Kiểm tra:**
   ```
   ✓ Generate Animation Clips: TRUE
   ✓ Pixels Per Unit: 16
   ✓ Filter Mode: Point (no filter)
   ```
4. **Apply**
5. **File sẽ load thành công!**

---

## 🛠️ GIẢI PHÁP 2: DÙNG SPRITES PNG THAY VÌ ASEPRITE FILE

**Nếu không muốn cài Aseprite Importer:**

### Bạn ĐÃ CÓ sprites PNG sẵn:
```
Assets/Art/MungeonDage/Dage/anim/
├─ Dage_Idle01-04.png
├─ Dage_Walk1-6.png
├─ Dage_Dmg1-3.png
├─ Dage_Death1-6.png
└─ dash/ (8 directions)
```

### Làm theo hướng dẫn tạo thủ công:

1. **ĐỌC FILE:** `DAGE_ANIMATION_SETUP_GUIDE.md`
2. **Hoặc:** `QUICK_ANIMATION_GUIDE.md`
3. **Tạo animation clips bằng cách kéo PNG sprites vào Animation window**
4. **KHÔNG CẦN file .aseprite**

---

## 📊 SO SÁNH 2 PHƯƠNG PHÁP

| Tiêu chí | Aseprite Importer | PNG Manual |
|----------|-------------------|------------|
| Thời gian setup | 5 phút (cài package) | 0 phút |
| Thời gian tạo animations | 10 phút | 20 phút |
| Dễ chỉnh sửa | ⭐⭐⭐⭐⭐ (edit trong Aseprite) | ⭐⭐⭐ (edit trong Unity) |
| Tự động sync | ✅ Có | ❌ Không |
| Phụ thuộc package | ✅ Cần cài | ❌ Không cần |

---

## ✅ KHUYẾN NGHỊ

### **Nếu bạn có Aseprite app:**
→ **CÀI ĐẶT IMPORTER** (Giải pháp 1)
- Workflow chuyên nghiệp
- Chỉnh sửa dễ dàng
- Auto-sync khi update file

### **Nếu chỉ muốn setup nhanh:**
→ **DÙNG PNG SPRITES** (Giải pháp 2)
- Không cần cài thêm gì
- Đơn giản hơn
- Làm theo `DAGE_ANIMATION_SETUP_GUIDE.md`

---

## 🚀 BƯỚC TIẾP THEO

### Nếu chọn Giải pháp 1 (Aseprite):
1. Cài Aseprite Importer
2. Restart Unity
3. Làm theo `ASEPRITE_ANIMATION_GUIDE.md`

### Nếu chọn Giải pháp 2 (PNG):
1. BỎ QUA file .aseprite
2. Làm theo `DAGE_ANIMATION_SETUP_GUIDE.md`
3. Dùng sprites PNG có sẵn

---

**Chọn phương pháp nào phù hợp với bạn và bắt đầu! 🎨**
