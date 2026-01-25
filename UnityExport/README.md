# GloomCraft (microStudio) → Unity (Port Guide + Importer)

## Reality check (quan trọng)
Project này dùng **microStudio MicroScript (`.ms`)**. Unity **không compile/chạy trực tiếp** MicroScript, nên “convert” sẽ là:
- **Import asset tự động** (sprites/audio/font) ✅
- **Port logic gameplay** (scene loop, input, physics, dungeon gen, UI inventory...) sang C# ❗ (viết lại)

Thư mục `UnityExport/` cung cấp **khung Unity 2D** + **Editor importer** để bạn vào Unity Hub tạo project nhanh, import asset đúng format, và có sẵn scaffolding để port từng module.

---

## 1) Tạo Unity project
- Unity Hub → New project → **2D (Built-in)** (hoặc URP 2D nếu bạn muốn)
- Unity version khuyến nghị: 2022 LTS / 2023 LTS

Sau khi tạo project:
- Copy toàn bộ folder `UnityExport/Assets` vào `YourUnityProject/Assets/`
- Copy thư mục asset từ repo này vào Unity:
  - `sprites/` → `Assets/Art/Sprites/`
  - `sounds/` → `Assets/Audio/SFX/`
  - `music/` → `Assets/Audio/Music/`
  - `assets/font.ttf` → `Assets/Art/Fonts/font.ttf`
  - `project.json` → `Assets/MicroStudio/project.json`

---

## 2) Auto-config spritesheets từ project.json
Unity menu: **Tools → MicroStudio → Configure Imports**

Nó sẽ:
- Scan `Assets/MicroStudio/project.json`
- Tìm key kiểu `sprites/<name>.png` có `properties.frames` / `properties.fps`
- Set TextureImporter:
  - Sprite Mode: Multiple (nếu frames > 1)
  - Pixels Per Unit: 16 (mặc định map tile 16px như code microStudio)
  - Filter: Point, Compression: None
  - Slice đều theo chiều ngang (giả định spritesheet là strip ngang)

> Nếu spritesheet của bạn không phải strip ngang, mình sẽ chỉnh importer theo layout thật (gửi mình 1-2 file spritesheet cụ thể).

---

## 3) Mapping kiến trúc microStudio → Unity

### Game loop
microStudio:
- `global.init()` / `global.update()` / `global.draw()`

Unity:
- `Awake/Start()` / `Update()` / `OnRenderObject` hoặc render bằng SpriteRenderer + Canvas

### Scene system
microStudio:
- `global.SceneManager` tạo `IntroductionScene/MainMenu/GameScene/...`

Unity:
- Mỗi scene game (MainMenu/Game/GameOver) nên là Unity Scene riêng, hoặc vẫn dùng 1 Unity Scene và đổi state bằng script.

### Physics
microStudio đang dùng custom:
- `global.applyPhysics(entity)` + `global.updatePhysics(entities)`

Unity:
- Dùng `Rigidbody2D/Collider2D` **hoặc** giữ custom grid collision (tile collider map) như hiện tại.

### Dungeon generation
microStudio:
- `new global.Dungeon(...)` (lib plasmapuffs)

Unity:
- Port thuật toán sang C# (giữ seed/random, map, colliderMap, layer textures).

---

## 4) Những gì mình cần từ bạn (để port chuẩn nhất)
Trả lời ngắn 3 ý:
- Bạn muốn Unity **2D Built-in** hay **URP 2D**?
- Bạn muốn giữ collision kiểu **grid/tile custom** (giống microStudio) hay chuyển sang **Rigidbody2D**?
- Bạn muốn build target: **PC only** hay có **mobile**?

### Lựa chọn của bạn (đã chốt)
- **Render pipeline**: Unity **2D Built-in**
- **Physics**: **Rigidbody2D/Collider2D**
- **Target**: **PC only**



