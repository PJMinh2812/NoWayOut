# 🎬 Ending System — Code Deep Dive

> Tài liệu này bàn cách kết thúc run, chuyển sang ending scene (tốt/xấu),
> và cơ chế async loading scene với SceneLoader.

---

## Tổng quan flow từ portal tới ending

```text
Player tại map 3-5 qua portal
  -> GoalPortal.TryAdvanceToNextMap(spawnAnchor)
    -> DungeonRunProgressionManager.TryAdvanceToNextMap(...)
      -> nếu là map cuối run
         -> HandleRunFinished()
           -> kiểm tra openedGoalChestCount
             -> nếu >= 3 → goodEndingSceneName = "Ending_Good"
             -> nếu < 3 → badEndingSceneName = "Ending_Bad"
           -> SceneLoader.LoadScene(endingScene)
             -> async load scene
             -> show loading UI với progress
             -> activate ending scene

Player ở ending scene
  -> xem animation/UI kết thúc
  -> nhấn nút (Return to Menu / New Run)
  -> load scene tương ứng (MainMenu / Level_01)
```

---

## 📂 FILE 1: `DungeonRunProgressionManager.cs` (phần ending)

```text
📁 Assets/Scripts/ProceduralGeneration/Integration/DungeonRunProgressionManager.cs
```

### HandleRunFinished() — Quyết định ending

```csharp
private bool HandleRunFinished()
{
    Debug.Log($"[RunProgression] Completed all maps (3-5). Chests {openedGoalChestCount}/{TotalGoalChests}.");

    if (!loadEndingSceneOnRunFinish)
    {
        Debug.LogWarning("[RunProgression] loadEndingSceneOnRunFinish=false. Keeping current final map state.");
        return false;
    }

    // Chọn ending dựa trên progress chest
    string endingScene = openedGoalChestCount >= TotalGoalChests
        ? goodEndingSceneName      // V.D: "Ending_Good"
        : badEndingSceneName;      // V.D: "Ending_Bad"

    if (string.IsNullOrWhiteSpace(endingScene))
    {
        Debug.LogWarning("[RunProgression] Ending scene name trống. Bỏ qua chuyển scene ending.");
        return false;
    }

    // Gọi async loader
    SceneLoader.LoadScene(endingScene);
    return true;
}
```

### Điều kiện kích hoạt ending

```csharp
bool isFinalMap = currentRound >= totalRounds && currentMap >= mapsPerRound;
// totalRounds = 3, mapsPerRound = 5 → final map = 3-5

if (isFinalMap)
{
    bool finished = HandleRunFinished();  // Load ending scene nếu return true
    if (!finished)
    {
        // Fallback: chưa config ending scene → giữ map state
        EnsureCompletionObjectsPresent();
    }
    return finished;
}
```

---

## 📂 FILE 2: `SceneLoader.cs`

```text
📁 Assets/Scripts/Core/SceneLoader.cs
```

### Vai trò

Quản lý async loading scene với loading UI + progress bar.
Dùng `DontDestroyOnLoad` → tồn tại xuyên suốt mỗi lần chuyển scene.

### Singleton & Instance Management

```csharp
public sealed class SceneLoader : MonoBehaviour
{
    private static SceneLoader _instance;

    public static void LoadScene(string sceneName)
    {
        EnsureInstance();
        _instance.StartCoroutine(_instance.LoadSceneCoroutine(sceneName));
    }

    private static void EnsureInstance()
    {
        if (_instance != null) return;

        var go = new GameObject("SceneLoader");
        _instance = go.AddComponent<SceneLoader>();
        DontDestroyOnLoad(go);
    }
}
```

**Tại sao DontDestroyOnLoad?**

- Khi chuyển scene (A → B), Unity unload scene A → destroy tất cả objects.
- Nếu SceneLoader ở scene A → bị destroy trước khi load scene B xong → mất reference.
- `DontDestroyOnLoad` giữ SceneLoader sống qua transitions → có thể load N scenes liên tiếp.

### Async Loading Coroutine

```csharp
private IEnumerator LoadSceneCoroutine(string sceneName)
{
    BuildUiIfNeeded();          // Tạo canvas + loading bar nếu chưa có
    _isLoadingScene = true;
    SetUiVisible(true);
    SetProgress(0f);

    var startTime = Time.unscaledTime;

    // ① LOAD SCENE ASYNC
    var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    if (op == null)
    {
        Debug.LogError($"[SceneLoader] LoadSceneAsync failed for '{sceneName}'.");
        SetUiVisible(false);
        yield break;
    }

    op.allowSceneActivation = false;  // Chưa activate ngay, chờ ta bảo
```

**allowSceneActivation = false** → Điều gì xảy ra?

- Scene load vào memory (0% → 90% progress).
- Phần 10% còn lại là scene initialization (scripts Awake/Start, object instantiation).
- Nếu `allowSceneActivation = false` → scene chưa được activate → 10% initialization chưa chạy.
- Load task "kết thúc" (progress = 0.9) nhưng scene chưa active.

Ngoài scene vẫn chạy:

```text
Scene A (old)
├── Update/LateUpdate chạy bình thường
├── SceneLoader coroutine chạy (đợi scene B load)
└── Loading UI animate progress bar

Scene B (being loaded)
├── Resources unpack nhưng Awake/Start chưa gọi
└── Đợi allowSceneActivation = true
```

```csharp
    // ② TRACK PROGRESS (0 → 0.9)
    while (op.progress < 0.9f)
    {
        // Normalize 0..0.9 → 0..1
        var normalized = Mathf.Clamp01(op.progress / 0.9f);
        SetProgress(normalized);
        yield return null;  // Wait next frame
    }

    // ③ SHOW 100% + WAIT MINIMUM TIME
    SetProgress(1f);

    var remaining = minVisibleSeconds - (Time.unscaledTime - startTime);
    if (remaining > 0f)
        yield return new WaitForSecondsRealtime(remaining);
    // Lý do: cho loading screen hiện ít nhất 0.4 giây (UX best practice)
    // Nếu scene load nhanh < 0.4s → vẫn giữ loading UI để user kịp đọc
```

**minVisibleSeconds = 0.4s**

Tuy sao cần?

- Load scene nhanh (< 0.1s) → loading UI chớp nhoáy → user hoang mang.
- Đảm bảo loading UI hiện ít nhất 0.4s → user tâm lý yên tâm, game không bị "lmo" trong mắt họ.

```csharp
    // ④ ACTIVATE SCENE
    op.allowSceneActivation = true;
    // Kích hoạt initialization 10% còn lại (Awake, Start, scene setup)

    while (!op.isDone)
        yield return null;  // Chờ initialization xong

    _isLoadingScene = false;
    if (_blockCount <= 0)
        SetUiVisible(false);  // Ẩn loading UI
}
```

### BeginBlocking / EndBlocking

Khi cần thêm thời gian setup sau khi scene load xong:

```csharp
public static void BeginBlocking(string label = null)
{
    EnsureInstance();
    _instance.BuildUiIfNeeded();
    _instance._blockCount++;
    _instance._indeterminate = true;  // Progress bar chạy animation vô tận
    _instance.SetUiVisible(true);
    _instance.SetLabel(label ?? _instance.blockingLabel);
}

public static void EndBlocking()
{
    if (_instance == null) return;
    if (_instance._blockCount > 0) _instance._blockCount--;
    if (_instance._blockCount == 0 && !_instance._isLoadingScene)
        _instance.SetUiVisible(false);
}
```

**Use case trong ending:**

```csharp
// Ending scene script
void Awake()
{
    SceneLoader.BeginBlocking("Preparing Ending...");
}

void Start()
{
    // Setup UI: hiện chest count, animate intro, ...
    DisplayEndingInfo();
    PlayEndingAnimation();
    yield return new WaitForSeconds(2f);

    SceneLoader.EndBlocking();  // Ẩn loading overlay
}
```

---

## 📂 FILE 3: Ending Scene Structure

Cấu trúc một ending scene típ:

```text
Ending_Good (Scene)
├── Canvas (World Space hoặc ScreenSpace)
│   ├── EndingUI (Panel chứa content)
│   │   ├── Background Image (fullscreen)
│   │   ├── Title: "🎉 VICTORY"
│   │   ├── ChestInfo: "Chests Opened: 3/3"
│   │   ├── StatsPanel: "Run Time: X:XX, Deaths: Y, ..."
│   │   ├── Buttons
│   │   │   ├── "Return to Menu" (LoadScene MainMenu)
│   │   │   └── "New Run" (reset progression, LoadScene Level_01)
│   │   └── Animations (fade in, scale bounce)
│   └── LoadingCanvas (SceneLoader tự tạo)
├── AudioSource (ending theme / SFX)
├── Lights (URP 2D Lights cho atmosphere)
└── Particles (optional: confetti, glow effects)

Ending_Bad (Scene)
├── Canvas
│   ├── EndingUI
│   │   ├── Title: "INCOMPLETE"
│   │   ├── ChestInfo: "Chests Opened: 1/3" (e.g.)
│   │   ├── Message: "Adventure Continues..."
│   │   └── Buttons (Return / New Run)
├── AudioSource (melancholic music)
└── Lights (dimmer, colder tone)
```

### Ending UI Script (ứng dụng)

```csharp
public class EndingUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI chestCountText;
    [SerializeField] private Image endingBackground;
    [SerializeField] private Button returnMenuButton;
    [SerializeField] private Button newRunButton;

    private void Start()
    {
        // Lấy progression manager từ scene trước (nó bị Destroy khi chuyển scene)
        // Sử dụng PlayerPrefs hoặc persist object từ progression
        DisplayChestCount();

        // Hook buttons
        returnMenuButton.onClick.AddListener(() =>
        {
            SaveManager.Instance?.SaveGame();  // Auto-save trước khi rời
            SceneLoader.LoadScene("MainMenu");
        });

        newRunButton.onClick.AddListener(() =>
        {
            // Reset progression: clear save flags
            PlayerPrefs.SetInt("RestoreDungeonFromSave", 0);
            PlayerPrefs.DeleteKey("LastDungeonSeed");

            // Load level mới
            SceneLoader.LoadScene("Level_01_TheAwakening");
        });
    }

    private void DisplayChestCount()
    {
        // Lấy từ PlayerPrefs hoặc Game.Instance
        int chestCount = int.Parse(
            PlayerPrefs.GetString("EndingChestCount", "0")
        );
        chestCountText.text = $"Chests Opened: {chestCount}/3";
    }
}
```

---

## Cơ chế truyền dữ liệu tới ending

### Trước: lưu vào PlayerPrefs

Khi chọn ending scene:

```csharp
// DungeonRunProgressionManager.HandleRunFinished()
PlayerPrefs.SetString("EndingChestCount", openedGoalChestCount.ToString());
PlayerPrefs.SetString("EndingDangerBias", aiDifficultyBias.ToString());
PlayerPrefs.Save();

SceneLoader.LoadScene(endingScene);
```

### Sau: load từ PlayerPrefs

Ở ending scene:

```csharp
int chests = int.Parse(PlayerPrefs.GetString("EndingChestCount", "0"));
float danger = float.Parse(PlayerPrefs.GetString("EndingDangerBias", "0"));
```

**Tại sao PlayerPrefs?**

- `DungeonRunProgressionManager` ở scene cũ → bị destroy khi chuyển scene.
- PlayerPrefs persist qua scene transitions → dữ liệu still available.
- Alternative: có thể dùng `DontDestroyOnLoad` object để giữ progression tồn tại.

---

## Điều kiện Good vs Bad Ending

```csharp
// DungeonRunProgressionManager.HandleRunFinished()
bool allChestsOpened = openedGoalChestCount >= TotalGoalChests;

string endingScene = allChestsOpened
    ? "Ending_Good"    // 3/3 chests mở
    : "Ending_Bad";    // < 3/3 chests
```

### Good Ending Triggers

- Mở tất cả 3 chests (1 ở map 1-5, 1 ở map 2-5, 1 ở map 3-5).
- Phát animation chiến thắng, nhạc hứng khởi.
- Hiển thị player rank/stats (clear time tốt, deaths ít, etc.).

### Bad Ending Triggers

- Bỏ lỡ ≥ 1 chest.
- Phát animation trung lập / buồn, nhạc hành động.
- Hiển thị message: "Adventure continues in darkness."
- Vẫn cho new run → không phạt player.

---

## Checklist test nhanh

1. Qua portal ở map 3-5 → loading screen hiện + progress bar.
2. Loading >= 0.4s dù scene load nhanh.
3. Ending scene load xong → auto-hide loading UI.
4. Good ending: mở 3/3 chest → hiển thị "VICTORY".
5. Bad ending: mở < 3/3 chest → hiển thị "INCOMPLETE".
6. Return to Menu button → load MainMenu.
7. New Run button → reset save flags + load Level_01.
8. SceneLoader tồn tại qua transitions (không bị Destroy).
9. PlayerPrefs lưu chest count trước load ending.
10. (Optional) UI fade in + SFX play khi ending scene active.
