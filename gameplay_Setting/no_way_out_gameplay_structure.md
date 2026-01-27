# CẤU TRÚC GAMEPLAY: **NO WAY OUT**

**Thể loại:** Grid-based Survival Puzzle (Giải đố sinh tồn trên lưới)\
**Cơ chế cốt lõi:** Ánh sáng (Vision) vs Bóng tối (Darkness)

------------------------------------------------------------------------

## 1. Vòng Lặp Chính Của Một Màn Chơi (Core Loop)

Mỗi màn chơi gồm 4 giai đoạn:

### Giai đoạn 1: Mò mẫm (The Blind Phase)

-   Người chơi bắt đầu ở rìa bản đồ với tầm nhìn cực thấp (1 ô xung
    quanh)\
-   Bóng tối bao trùm (Fog of War)\
-   Nghe tiếng quái vật nhưng không thấy chúng\
-   Di chuyển theo lưới để tìm **3 Mảnh Vỡ Ánh Sáng (Light Fragments)**\
-   Tránh **Sàn nứt (Crumbling Tiles)** -- nghe tiếng "rắc" phải rời đi
    ngay

### Giai đoạn 2: Khai sáng & Giải đố (The Puzzle Phase)

-   Nhặt đủ mảnh sáng → Tầm nhìn mở rộng 3 ô\
-   Mở khóa kỹ năng **Flash of Truth (Space)**\
-   Thu thập **Nguyên Tố Cốt Lõi (Key Elements)** để mở cửa Boss

**Kết hợp Combat & Puzzle** - Làm choáng quái bằng đèn rồi chạy hoặc tấn
công\
- Dùng gương để phản chiếu ánh sáng tìm công tắc ẩn\
- Linh hồn con gái bay quanh, tự động chặn 1 đòn đánh

### Giai đoạn 3: Đấu Trùm (The Boss Fight)

-   Boss xuất hiện chặn cửa thoát khi đủ nguyên tố\
-   Boss chiếm diện tích **3x3 ô**\
-   Ô sắp bị đánh sẽ nhấp nháy **đỏ** trước 2 giây\
-   Dụ Boss húc vào vật thể môi trường để làm choáng rồi gây sát thương

### Giai đoạn 4: Thăng cấp & Thoát hiểm (Upgrade & Escape)

-   Boss bị đánh bại → cửa mở\
-   Chọn 1 nâng cấp:
    -   Tăng máu\
    -   Tăng tốc chạy\
    -   Tăng thời gian đèn sáng\
-   Nhân vật đổi ngoại hình\
-   Bước vào cửa → Lưu game → Màn mới khó hơn

------------------------------------------------------------------------

## 2. Chi Tiết Các Cơ Chế (Mechanics Breakdown)

### A. Di chuyển & Điều khiển

-   Phím: **WASD** hoặc **Mũi tên**\
-   Di chuyển từng ô, không đi chéo\
-   Không xuyên tường, va quái bị đẩy lùi 1 ô

### B. Cơ chế Ánh Sáng (Lantern)

**Passive** - Vùng sáng quanh nhân vật\
- Quái sợ ánh sáng, không lại gần

**Active -- Space** - Bùng nổ ánh sáng trong 5 giây\
- Lộ bẫy, làm choáng quái, kích hoạt gương\
- Hồi chiêu: 15 giây

### C. Môi Trường Tương Tác

-   Thùng/Đá: Nấp tránh đòn Boss\
-   Bẫy chông: Ẩn trong bóng tối, hiện khi bật đèn\
-   Sàn nứt: Sập sau 1 giây, rơi xuống mất 1 máu

### D. Hệ Thống UI

-   Thanh máu: 3--5 tim\
-   Pin đèn: Hao khi dùng kỹ năng, hồi khi nhặt vật phẩm\
-   Điểm số tăng khi nhặt đồ hoặc phá môi trường bằng Boss

------------------------------------------------------------------------

## 3. Cấu Trúc Màn Chơi

### Level 1 -- The Cellar

-   Tutorial di chuyển & dùng đèn\
-   Quái: Chuột khổng lồ\
-   Không có Boss hoặc Mini-boss nhỏ

### Level 2 -- Hall of Mirrors

-   Giải đố phản chiếu ánh sáng\
-   Quái tàng hình Shadow Stalker\
-   Boss: The Phantom (phân thân)

### Level 3 -- The Void

-   Sàn liên tục sập, chạy đua thời gian\
-   Boss: Construct Golem (3x3)

------------------------------------------------------------------------

## 4. Các Class Cần Code (Gợi ý C#)

  Class              Vai trò
  ------------------ --------------------------------------
  GameManager        Quản lý điểm, Game Over, chuyển cảnh
  PlayerController   Di chuyển grid, input, máu
  LightSystem        Quản lý ánh sáng & Fog of War
  EnemyAI            AI quái đuổi người chơi
  BossController     Điều khiển phase tấn công Boss
  InteractiveTile    Class cha cho các loại ô đặc biệt

------------------------------------------------------------------------

**Tài liệu thiết kế gameplay hoàn chỉnh cho project game "No Way Out".**
