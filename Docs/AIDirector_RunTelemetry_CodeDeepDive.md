# 🤖 AI Director & Run Telemetry — Code Deep Dive

> Tài liệu này giải thích hệ thống AI Director mới trong run progression:
> thu thập telemetry theo map, tính tín hiệu hiệu năng, rồi điều chỉnh độ khó map kế tiếp.

---

## Tổng quan kiến trúc

```text
PlayerHealth2D / Trap scripts
  -> RunAIDirectorTelemetry (counter toàn cục theo map)

DungeonRunProgressionManager
  BeginMapPerformanceTracking()  -> chụp baseline
  FinalizeMapPerformanceIfNeeded() -> chụp delta map vừa xong
  ApplyAIDirectorAdjustments()   -> đổi roomCount/branchProb/trapIntensity

Kết quả áp dụng vào map mới:
  dungeonManager.archetype1RoomCount
  dungeonManager.archetype2RoomCount
  dungeonManager.branchProbability
  trapIntensityMultiplier (cho hệ trap/enemy dùng)
```

---

## 📂 FILE 1: `RunAIDirectorTelemetry.cs`

```text
📁 Assets/Scripts/Core/RunAIDirectorTelemetry.cs
```

### Mục tiêu

Là static runtime aggregator, đếm chỉ số trong 1 map:

- `TotalDamageTaken`
- `TotalDeaths`
- `TotalTrapTriggers`

### API chính

```csharp
RecordPlayerDamageTaken(int amount)
RecordPlayerDeath()
RecordTrapTriggered(Object source)
ResetAll()
```

Đặc điểm:

- Không phụ thuộc scene object lifecycle vì static class.
- Counter reset sau khi map performance đã được finalize.

---

## 📂 FILE 2: `PlayerHealth2D.cs` (hook damage/death)

```text
📁 Assets/Scripts/Player/PlayerHealth2D.cs
```

Điểm hook telemetry:

```csharp
RunAIDirectorTelemetry.RecordPlayerDamageTaken(damageApplied);
RunAIDirectorTelemetry.RecordPlayerDeath();
```

Ý nghĩa:

- Damage tính theo thực nhận sau clamp/invincibility.
- Death count phản ánh fail-pressure thực tế của map.

---

## 📂 FILE 3: Trap scripts (hook trap trigger)

```text
📁 Assets/Scripts/Traps/*.cs
```

Các trap đã gọi `RecordTrapTriggered(this)` gồm:

- `ConfusionRune`
- `FakeFloor`
- `HiddenSpikes`
- `InvisibleBlock`
- `SlipperyFloor`
- `SpringTrap`
- `StatusEffectTrap`

Khi trap bắn sự kiện, AI Director biết player đang bị "ép" nhiều hay ít bởi môi trường.

---

## 📂 FILE 4: `DungeonRunProgressionManager.cs`

```text
📁 Assets/Scripts/ProceduralGeneration/Integration/DungeonRunProgressionManager.cs
```

### 1) Chụp baseline khi map mới tạo

`BeginMapPerformanceTracking()` lưu snapshot lúc bắt đầu map:

- HP đầu map
- damage/death/trap count tại thời điểm start

Mục tiêu: cuối map tính **delta** thay vì tổng session.

### 2) Chốt hiệu năng map vừa clear

`FinalizeMapPerformanceIfNeeded()` tính:

- `clearTime`
- `deaths` (delta)
- `endHealth/maxHealth`
- `damageTaken` (delta)
- `trapTriggers` (delta)

Sau khi chốt:

- lưu vào `lastMapPerformance`
- bật `hasLastMapPerformance`
- gọi `RunAIDirectorTelemetry.ResetAll()`

### 3) Điều chỉnh độ khó map tiếp theo

`ApplyAIDirectorAdjustments(ref roomCount, ref branchProb)` chạy trước mỗi `GenerateDungeon()`.

Điều kiện skip:

- `enableAIDirector == false`
- chưa có `lastMapPerformance` (map đầu run)

### Công thức tín hiệu

Mỗi metric được chuẩn hóa quanh target bằng `GetCenteredSignal(target, tolerance, actual, inverted)`.

- Signal nằm trong `[-1, 1]`.
- `inverted=true` nghĩa là actual cao hơn target -> khó hơn mong muốn -> signal âm.

Tổng hợp:

```text
combinedSignal =
  clearTimeSignal * 0.35
+ endHealthSignal * 0.25
+ damageSignal   * 0.20
+ trapSignal     * 0.10
+ deathSignal    * 0.10
```

Update bias:

```text
aiDifficultyBias = clamp(
  aiDifficultyBias + combinedSignal * aiDifficultyStep,
  minAiDifficultyBias,
  maxAiDifficultyBias)
```

Áp vào generation:

```text
aiRoomDelta   = round(aiDifficultyBias * aiRoomScale)
aiBranchDelta = aiDifficultyBias * aiBranchScale

roomCount = clamp(roomCount + aiRoomDelta, 3, 10)
branchProb = clamp01(branchProb + aiBranchDelta)
trapIntensityMultiplier = clamp(1 + aiDifficultyBias * trapIntensityScale, 0.7, 1.4)
```

---

## Ý nghĩa gameplay

- Nếu player clear map quá dễ (nhanh, ít mất máu, ít dính trap) -> bias tăng -> map sau nhiều room/nhánh hơn.
- Nếu player quá chật vật -> bias giảm -> map sau nhẹ hơn.
- Hệ thống tạo curve độ khó động theo performance thay vì cứng theo round.

---

## Debug log cần theo dõi

Keyword log:

- `[RunProgression][AIDirector] Map performance ...`
- `[RunProgression][AIDirector] Applied bias=...`
- `[AIDirectorTelemetry] Trap triggered: ...`

Dựa vào các log này có thể tune nhanh:

- `targetClearTimeSeconds`
- `targetEndHealthRatio`
- `targetDamageTaken`
- `targetTrapTriggers`
- `aiDifficultyStep`

---

## Checklist test nhanh

1. Clear map rất nhanh, ít dính damage -> map sau room/branch tăng.
2. Chơi chậm và chết nhiều -> map sau room/branch giảm.
3. Xác nhận telemetry reset mỗi map (không dồn vô hạn).
4. Tắt `enableAIDirector` -> generation quay về scaling theo round cơ bản.
