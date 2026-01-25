# Hướng Dẫn Tạo Main Menu - No Way Out

## Bước 3: Tạo Main Menu Panel

### 3.1. Tạo Panel chính

1. Click chuột phải vào **Canvas** trong Hierarchy
2. Chọn **UI > Panel** → Đổi tên thành `MainMenuPanel`
3. Thiết lập **RectTransform** của MainMenuPanel:
   - **Anchor Presets**: Stretch both (Alt + click vào icon góc phải dưới)
   - **Left, Right, Top, Bottom**: Đặt tất cả = 0 (để panel full màn hình)
   - **Color**: Đổi màu Image thành màu tối (ví dụ: #1A1A1A hoặc #000000 với Alpha = 180)

### 3.2. Tạo Background Image (tùy chọn)

1. Click chuột phải vào **MainMenuPanel**
2. Chọn **UI > Image** → Đổi tên thành `Background`
3. Thiết lập:
   - Anchor: Stretch both
   - Source Image: Để trống hoặc import ảnh background của bạn
   - Color: Màu gradient hoặc #0D0D0D

### 3.3. Tạo Title Text

1. Click chuột phải vào **MainMenuPanel**
2. Chọn **UI > Text - TextMeshPro** → Đổi tên thành `TitleText`
   - _Nếu lần đầu dùng TextMeshPro, Unity sẽ hỏi import TMP Essentials → Click Import_
3. Thiết lập **RectTransform**:
   - **Anchor**: Top Center
   - **Pos X**: 0, **Pos Y**: -100
   - **Width**: 600, **Height**: 150
4. Thiết lập **TextMeshPro**:
   - **Text**: "NO WAY OUT" (hoặc tên game của bạn)
   - **Font Size**: 72
   - **Alignment**: Center & Middle
   - **Color**: #FFD700 (vàng) hoặc #FFFFFF (trắng)
   - **Font Style**: Bold
   - **Vertex Color > Gradient**: Enable (tùy chọn)
   - **Outline**: Enable với Thickness = 0.2, Color = #000000

### 3.4. Tạo Button Container

1. Click chuột phải vào **MainMenuPanel**
2. Chọn **UI > Empty** (hoặc GameObject) → Đổi tên thành `ButtonContainer`
3. Thiết lập **RectTransform**:
   - **Anchor**: Middle Center
   - **Pos X**: 0, **Pos Y**: -100
   - **Width**: 300, **Height**: 400
4. Thêm component **Vertical Layout Group**:
   - **Child Alignment**: Middle Center
   - **Spacing**: 20
   - **Child Force Expand**: Bỏ tick Width và Height
   - **Padding**: Top = 0, Bottom = 0, Left = 0, Right = 0

### 3.5. Tạo Play Button

1. Click chuột phải vào **ButtonContainer**
2. Chọn **UI > Button - TextMeshPro** → Đổi tên thành `PlayButton`
3. Thiết lập **RectTransform** của PlayButton:
   - **Width**: 250, **Height**: 60
4. Thiết lập **Button Component**:
   - **Interactable**: ✓ Checked
   - **Transition**: Colors
   - **Normal Color**: #4A4A4A (xám đậm)
   - **Highlighted Color**: #6A6A6A (xám sáng)
   - **Pressed Color**: #2E7D32 (xanh lá)
   - **Selected Color**: #5A5A5A
   - **Disabled Color**: #3A3A3A
   - **Color Multiplier**: 1
   - **Fade Duration**: 0.1
5. Chỉnh sửa **Background Image** của button:
   - Click vào child object "Background" của PlayButton
   - **Color**: #3A3A3A hoặc theo Normal Color
   - **Material**: Default UI Material
6. Chỉnh sửa **Text (TMP)** của button:
   - Click vào child object "Text (TMP)" của PlayButton
   - **Text**: "PLAY"
   - **Font Size**: 28
   - **Alignment**: Center & Middle
   - **Color**: #FFFFFF (trắng)
   - **Font Style**: Bold
   - **Enable Auto Size**: Bỏ tick (để font size cố định)

### 3.6. Tạo Settings Button

1. Click chuột phải vào **ButtonContainer**
2. Chọn **UI > Button - TextMeshPro** → Đổi tên thành `SettingsButton`
3. Áp dụng các thiết lập giống PlayButton (bước 3.5)
4. Thay đổi **Text**: "SETTINGS"

### 3.7. Tạo Credits Button

1. Click chuột phải vào **ButtonContainer**
2. Chọn **UI > Button - TextMeshPro** → Đổi tên thành `CreditsButton`
3. Áp dụng các thiết lập giống PlayButton
4. Thay đổi **Text**: "CREDITS"

### 3.8. Tạo Quit Button

1. Click chuột phải vào **ButtonContainer**
2. Chọn **UI > Button - TextMeshPro** → Đổi tên thành `QuitButton`
3. Áp dụng các thiết lập giống PlayButton
4. Thay đổi **Text**: "QUIT"
5. Thay đổi **Pressed Color**: #C62828 (đỏ) để nhấn mạnh đây là nút thoát

### 3.9. Kết quả Hierarchy

```
Canvas
└── MainMenuPanel
    ├── Background (Image)
    ├── TitleText (TextMeshPro)
    └── ButtonContainer (GameObject + Vertical Layout Group)
        ├── PlayButton (Button)
        │   ├── Background (Image)
        │   └── Text (TMP)
        ├── SettingsButton (Button)
        │   ├── Background (Image)
        │   └── Text (TMP)
        ├── CreditsButton (Button)
        │   ├── Background (Image)
        │   └── Text (TMP)
        └── QuitButton (Button)
            ├── Background (Image)
            └── Text (TMP)
```

---

## Bước 4: Tạo Settings Panel

### 4.1. Tạo Panel

1. Click chuột phải vào **Canvas**
2. Chọn **UI > Panel** → Đổi tên thành `SettingsPanel`
3. Thiết lập:
   - Anchor: Stretch both
   - Left, Right, Top, Bottom: 0
   - Color: #1A1A1A với Alpha = 220
4. **Quan trọng**: Bỏ tick ô **Active** trong Inspector để ẩn panel này (chỉ hiện khi click Settings)

### 4.2. Tạo Settings Title

1. Click chuột phải vào **SettingsPanel**
2. Chọn **UI > Text - TextMeshPro** → Đổi tên thành `SettingsTitle`
3. Thiết lập:
   - Anchor: Top Center
   - Pos X: 0, Pos Y: -80
   - Width: 400, Height: 80
   - Text: "SETTINGS"
   - Font Size: 48
   - Alignment: Center & Middle
   - Color: #FFFFFF
   - Font Style: Bold

### 4.3. Tạo Settings Container

1. Click chuột phải vào **SettingsPanel**
2. Chọn **UI > Empty** → Đổi tên thành `SettingsContainer`
3. Thiết lập:
   - Anchor: Middle Center
   - Pos X: 0, Pos Y: 0
   - Width: 500, Height: 400
4. Thêm **Vertical Layout Group**:
   - Spacing: 30
   - Child Alignment: Upper Center
   - Padding: 20 cho tất cả các cạnh

### 4.4. Tạo Volume Controls

#### Master Volume

1. Click chuột phải vào **SettingsContainer**
2. Chọn **UI > Slider** → Đổi tên thành `MasterVolumeSlider`
3. Thiết lập:
   - Width: 450, Height: 30
   - **Min Value**: 0.0001, **Max Value**: 1, **Value**: 1
   - **Whole Numbers**: Bỏ tick
4. Tạo Label:
   - Click chuột phải vào **SettingsContainer** (trước Slider)
   - Chọn **UI > Text - TextMeshPro** → Đổi tên thành `MasterVolumeLabel`
   - Text: "Master Volume"
   - Font Size: 24
   - Width: 450, Height: 30

#### Music Volume

1. Lặp lại bước tạo Master Volume
2. Đổi tên thành `MusicVolumeSlider` và `MusicVolumeLabel`
3. Label Text: "Music Volume"

#### SFX Volume

1. Lặp lại bước tạo Master Volume
2. Đổi tên thành `SFXVolumeSlider` và `SFXVolumeLabel`
3. Label Text: "SFX Volume"

### 4.5. Tạo Graphics Controls

#### Fullscreen Toggle

1. Click chuột phải vào **SettingsContainer**
2. Chọn **UI > Toggle** → Đổi tên thành `FullscreenToggle`
3. Thiết lập:
   - Width: 450, Height: 40
   - **Is On**: ✓ Checked (mặc định fullscreen)
4. Chỉnh Label:
   - Mở child object "Label" của Toggle
   - Text: "Fullscreen"
   - Font Size: 24

#### Resolution Dropdown

1. Click chuột phải vào **SettingsContainer**
2. Tạo Label trước:
   - **UI > Text - TextMeshPro** → Đổi tên `ResolutionLabel`
   - Text: "Resolution"
   - Font Size: 24
3. Chọn **UI > Dropdown - TextMeshPro** → Đổi tên thành `ResolutionDropdown`
4. Thiết lập:
   - Width: 450, Height: 40

#### Quality Dropdown

1. Lặp lại bước tạo Resolution Dropdown
2. Đổi tên thành `QualityDropdown` và `QualityLabel`
3. Label Text: "Graphics Quality"

### 4.6. Tạo Back Button

1. Click chuột phải vào **SettingsPanel** (không phải Container)
2. Chọn **UI > Button - TextMeshPro** → Đổi tên thành `BackButton`
3. Thiết lập:
   - Anchor: Bottom Center
   - Pos X: 0, Pos Y: 80
   - Width: 200, Height: 50
   - Text: "BACK"
   - Font Size: 24
   - Colors giống các button trước

### 4.7. Setup SettingsMenuUI Script

1. Click vào **SettingsPanel**
2. Click **Add Component** → Tìm và thêm **SettingsMenuUI**
3. Kéo thả các UI elements vào các trường tương ứng:
   - Master/Music/SFX Volume Sliders
   - Fullscreen Toggle
   - Resolution/Quality Dropdowns
   - (Audio Mixer có thể để null nếu chưa có)

---

## Bước 5: Tạo Credits Panel

### 5.1. Tạo Panel

1. Click chuột phải vào **Canvas**
2. Chọn **UI > Panel** → Đổi tên thành `CreditsPanel`
3. Thiết lập:
   - Anchor: Stretch both
   - Color: #1A1A1A với Alpha = 220
4. **Bỏ tick Active** để ẩn panel

### 5.2. Tạo Credits Title

1. Click chuột phải vào **CreditsPanel**
2. Chọn **UI > Text - TextMeshPro** → Đổi tên `CreditsTitle`
3. Thiết lập:
   - Anchor: Top Center
   - Pos Y: -80
   - Text: "CREDITS"
   - Font Size: 48
   - Alignment: Center & Middle

### 5.3. Tạo Credits Content

1. Click chuột phải vào **CreditsPanel**
2. Chọn **UI > Scroll View** → Đổi tên thành `CreditsScrollView`
3. Thiết lập:
   - Anchor: Middle Center
   - Pos Y: -50
   - Width: 600, Height: 400

4. Mở **Viewport > Content** trong Scroll View
5. Chọn **Content**, thêm component **Vertical Layout Group**:
   - Padding: 20
   - Spacing: 10
   - Child Force Expand: Bỏ tick Height

6. Click chuột phải vào **Content**
7. Chọn **UI > Text - TextMeshPro** → Đổi tên `CreditsText`
8. Thiết lập:
   - Text:

     ```
     DEVELOPED BY
     [Your Team Name]

     PROGRAMMING
     [Your Name]

     ART & DESIGN
     [Artist Name]

     SOUND & MUSIC
     [Sound Designer]

     SPECIAL THANKS
     [Thank you notes]

     © 2026 [Your Studio]
     ```

   - Font Size: 20
   - Alignment: Center & Top
   - Width: 560
   - Enable Auto Size: Bỏ tick
   - Wrap Text: Enable

### 5.4. Tạo Back Button

1. Click chuột phải vào **CreditsPanel**
2. Chọn **UI > Button - TextMeshPro** → Đổi tên `BackButton`
3. Thiết lập giống Back Button của Settings Panel
   - Anchor: Bottom Center
   - Pos Y: 80
   - Width: 200, Height: 50
   - Text: "BACK"

---

## Bước 6: Setup MainMenuUI Script

### 6.1. Gắn Script vào Canvas

1. Click vào **Canvas** trong Hierarchy
2. Click **Add Component** trong Inspector
3. Tìm và chọn **MainMenuUI**

### 6.2. Assign UI Elements

Kéo thả các objects từ Hierarchy vào các trường trong MainMenuUI:

**UI Panels:**

- Main Menu Panel → Kéo `MainMenuPanel`
- Settings Panel → Kéo `SettingsPanel`
- Credits Panel → Kéo `CreditsPanel`

**Buttons:**

- Play Button → Kéo `PlayButton`
- Settings Button → Kéo `SettingsButton`
- Credits Button → Kéo `CreditsButton`
- Quit Button → Kéo `QuitButton`
- Back Button → Kéo `BackButton` từ **SettingsPanel** (hoặc CreditsPanel, chọn một)

**Settings:**

- Game Scene Name → Nhập: `GameScene`

### 6.3. Kiểm tra

1. Đảm bảo tất cả các trường đều có gán objects (không có "None")
2. Kiểm tra **SettingsPanel** và **CreditsPanel** đã bỏ tick Active
3. Chỉ **MainMenuPanel** đang active

---

## Bước 7: Thêm Scenes vào Build Settings

### 7.1. Mở Build Settings

1. Trong Unity Editor, vào **File > Build Settings** (hoặc Ctrl+Shift+B)

### 7.2. Thêm Main Menu Scene

1. Đảm bảo bạn đang mở scene **MainMenu**
2. Trong cửa sổ Build Settings, click **Add Open Scenes**
3. Scene MainMenu sẽ xuất hiện với **Index: 0**

### 7.3. Thêm Game Scene

1. Trong Project window, tìm scene **GameScene** tại `Assets/Settings/Scenes/GameScene.unity`
2. Kéo thả **GameScene** vào Build Settings window
3. GameScene sẽ có **Index: 1**

### 7.4. Sắp xếp Order (Quan trọng!)

- **MainMenu phải ở Index 0** (scene đầu tiên khi chạy game)
- **GameScene ở Index 1**
- Nếu sai thứ tự, kéo thả để sắp xếp lại

### 7.5. Kiểm tra

Danh sách Scenes in Build phải như sau:

```
☑ Scenes/MainMenu          0
☑ Scenes/GameScene         1
```

### 7.6. Cập nhật GameManager (nếu cần)

1. Mở [GameManager.cs](Assets/Scripts/GameManager.cs)
2. Tìm dòng code:
   ```csharp
   SceneManager.LoadScene("MainMenu");
   ```
3. Đảm bảo tên scene chính xác là "MainMenu"

---

## Test Main Menu

### Test trong Editor

1. Mở scene **MainMenu**
2. Click **Play** trong Unity Editor
3. Kiểm tra:
   - ✅ Play button chuyển đến GameScene
   - ✅ Settings button mở Settings panel
   - ✅ Credits button mở Credits panel
   - ✅ Back buttons quay về Main Menu
   - ✅ Quit button (sẽ không hoạt động trong Editor, nhưng sẽ hoạt động khi Build)

### Test Settings

1. Click Settings button
2. Thử điều chỉnh:
   - Volume sliders (cần Audio Mixer để nghe được)
   - Fullscreen toggle
   - Resolution dropdown
   - Quality dropdown
3. Click Back button

### Test Build

1. **File > Build Settings**
2. Click **Build and Run**
3. Chọn folder để lưu build
4. Test đầy đủ tất cả các chức năng

---

## Styling Tips (Tùy chọn)

### Màu sắc đề xuất

- **Dark Theme:**
  - Background: #0D0D0D
  - Panels: #1A1A1A
  - Buttons Normal: #2D2D2D
  - Buttons Hover: #3D3D3D
  - Buttons Pressed: #1E7D32 (xanh lá)
  - Text: #FFFFFF hoặc #FFD700 (vàng)

- **Horror Theme (phù hợp với "No Way Out"):**
  - Background: #0A0A0A
  - Panels: #1F0000 (đỏ đậm)
  - Buttons: #330000
  - Text: #FF0000 (đỏ) hoặc #8B0000 (đỏ đậm)
  - Pressed: #660000

### Font đề xuất

- **Title**: Bold, 60-72pt
- **Buttons**: Bold, 24-28pt
- **Labels**: Regular, 18-24pt
- **Credits**: Regular, 16-20pt

### Animation (Tùy chọn)

Thêm hiệu ứng cho buttons:

1. Chọn Button
2. **Add Component > Animator**
3. Tạo Animation đơn giản: Scale từ 1 → 1.1 khi hover

### Sound Effects (Tùy chọn)

1. Import audio files vào `Assets/Audio/UI/`
2. Thêm **Audio Source** component vào các buttons
3. Assign sound clips:
   - Hover sound: whoosh nhẹ
   - Click sound: click hoặc beep
   - Play button: sound đặc biệt hơn

---

## Troubleshooting

### Buttons không hoạt động

- ✅ Kiểm tra Canvas có **GraphicRaycaster** component
- ✅ Kiểm tra có **EventSystem** trong scene (tự động tạo khi tạo UI)
- ✅ Kiểm tra MainMenuUI script đã assign đúng buttons

### Panel không ẩn/hiện

- ✅ Kiểm tra Settings và Credits panel đã bỏ tick Active
- ✅ Kiểm tra MainMenuUI script đã assign đúng panels

### Scene không load

- ✅ Kiểm tra tên scene trong GameSceneName field chính xác
- ✅ Kiểm tra scenes đã thêm vào Build Settings
- ✅ Kiểm tra thêm `using UnityEngine.SceneManagement;` trong script

### Text bị mờ

- ✅ Chuyển Camera sang Orthographic nếu dùng 2D
- ✅ Kiểm tra Canvas Render Mode (nên để Screen Space - Overlay)
- ✅ Tăng Font Size và enable Clear Type

---

## Next Steps

1. **Thêm Background Music**: Import nhạc nền và thêm Audio Source vào Canvas
2. **Thêm Animation**: Tạo animations cho title text (fade in, glow effect)
3. **Thêm Particle Effects**: Thêm particle system cho background (tuyết, mưa, etc.)
4. **Save/Load System**: Implement save game functionality
5. **Level Selection**: Tạo scene chọn level nếu game có nhiều levels

Chúc bạn thành công! 🎮
