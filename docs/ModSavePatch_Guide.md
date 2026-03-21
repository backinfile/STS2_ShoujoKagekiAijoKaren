# Mod 存档系统 Patch 指南

## 1. 游戏存档系统概述

### 存档文件结构

游戏存档路径格式：`user://profiles/<profileId>/saves/<filename>`

| 文件名 | 类 | 说明 |
|---|---|---|
| `current_run.save` | `SerializableRun` | 当前局游戏完整状态 |
| `current_run_mp.save` | `SerializableRun` | 多人局游戏状态 |
| `progress.save` | `SerializableProgress` | 永久进度（纪元/解锁/统计） |
| `profile.save` | `ProfileSave` | 档案槽选择（最多3个） |

存档目录辅助方法：
```csharp
RunSaveManager.GetRunSavePath(int profileId, string fileName)
// 返回：<ProfileDir>/saves/<fileName>
```

### 核心序列化链

```
RunSaveManager.SaveRun()
  └─ RunManager.Instance.ToSave(preFinishedRoom)   → SerializableRun
  └─ JsonSerializer.SerializeAsync(stream, value, ...)
  └─ _saveStore.WriteFileAsync(path, bytes)         → tmp → rename → backup
```

```
RunSaveManager.LoadRunSave()
  └─ MigrationManager.LoadSave<SerializableRun>(path)
  └─ JsonSerializationUtility.FromJson<T>(json)
  └─ 失败时自动尝试 .backup 文件
```

### ISaveStore 的原子写入策略

`GodotFileIo` 保证写入原子性（防止断电损坏）：
1. 写入 `<path>.tmp` 临时文件
2. 若成功，将原文件复制为 `<path>.backup`
3. 将 `.tmp` 重命名为目标路径

---

## 2. Mod 自定义存档的设计方案

### 方案：伴随文件（Companion File）

在游戏写入 `current_run.save` 的同时，Mod 在相同目录写入一个独立的 `karen_current_run.json`。

**优点**：
- 无需侵入游戏的 AOT 序列化上下文（`MegaCritSerializerContext`）
- 自定义 JSON 结构完全自由
- 不影响游戏迁移系统（MigrationManager）
- 文件删除/损坏不影响主游戏存档

**Hook 点**：
| 游戏方法 | Patch 类型 | 说明 |
|---|---|---|
| `RunSaveManager.SaveRun` | Postfix | 游戏保存后，写入 Mod 数据文件 |
| `RunSaveManager.LoadRunSave` | Postfix | 游戏加载后，读取 Mod 数据文件 |
| `RunSaveManager.DeleteCurrentRun` | Postfix | 游戏删除存档后，同步删除 Mod 数据文件 |

---

## 3. 从 Patch 中获取 ISaveStore 和路径

### 获取 ISaveStore 实例

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

var saveStore = Traverse.Create(__instance)
    .Field("_saveStore")
    .GetValue<ISaveStore>();
```

### 获取当前存档路径

```csharp
// CurrentRunSavePath 是私有属性，通过 Traverse 访问
var currentRunPath = Traverse.Create(__instance)
    .Property("CurrentRunSavePath")
    .GetValue<string>();

// 派生 Mod 文件路径（同目录，不同文件名）
var modDataPath = Path.ChangeExtension(currentRunPath, null) + "_karen.json";
// 结果：.../saves/current_run_karen.json
```

或者，如果需要获取 profileId：
```csharp
var profileIdProvider = Traverse.Create(__instance)
    .Field("_profileIdProvider")
    .GetValue<IProfileIdProvider>();
int profileId = profileIdProvider.CurrentProfileId;
var modDataPath = RunSaveManager.GetRunSavePath(profileId, "karen_current_run.json");
```

---

## 4. 完整实现示例

### 4.1 定义 Mod 存档数据结构

```csharp
// src/Core/Save/KarenRunSaveData.cs

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShoujoKagekiAijoKaren;

/// <summary>
/// Karen Mod 的局内存档数据（随 current_run.save 同步写入/读取）
/// </summary>
public class KarenRunSaveData
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("shine_data")]
    public List<ShineSaveData> ShineData { get; set; } = new();

    // 未来可在此扩展更多 Mod 数据字段
}
```

### 4.2 实现 Patch 类

```csharp
// src/Core/Patches/RunSaveManager_Patches.cs

using System;
using System.IO;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Runs;

namespace ShoujoKagekiAijoKaren;

[HarmonyPatch]
internal static class RunSaveManager_Patches
{
    private const string ModSaveFileName = "karen_current_run.json";

    // 辅助：从 __instance 获取 Mod 存档路径
    private static string? GetModSavePath(RunSaveManager instance)
    {
        try
        {
            var currentRunPath = Traverse.Create(instance)
                .Property("CurrentRunSavePath")
                .GetValue<string>();
            if (currentRunPath == null) return null;
            // 同目录，替换文件名
            var dir = Path.GetDirectoryName(currentRunPath) ?? "";
            return Path.Combine(dir, ModSaveFileName);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[RunSaveManager_Patches] 获取路径失败: {ex.Message}");
            return null;
        }
    }

    // 辅助：从 __instance 获取 ISaveStore
    private static ISaveStore? GetSaveStore(RunSaveManager instance)
    {
        try
        {
            return Traverse.Create(instance)
                .Field("_saveStore")
                .GetValue<ISaveStore>();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[RunSaveManager_Patches] 获取 SaveStore 失败: {ex.Message}");
            return null;
        }
    }

    // ─────────────────────────────────────────────
    // 保存：SaveRun Postfix
    // 注意：SaveRun 是 async Task，Postfix 在 Task 创建时运行（非完成时）
    // 对于写不同文件这是可接受的，使用同步写入避免复杂性
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(RunSaveManager), nameof(RunSaveManager.SaveRun))]
    [HarmonyPostfix]
    private static void SaveRun_Postfix(RunSaveManager __instance)
    {
        try
        {
            var saveStore = GetSaveStore(__instance);
            var modPath = GetModSavePath(__instance);
            if (saveStore == null || modPath == null) return;

            // 如果不是单人局游戏，不保存（与原版 SaveRun 逻辑一致）
            if (!RunManager.Instance.ShouldSave) return;

            // 收集当前局游戏中的 Mod 数据
            var modData = new KarenRunSaveData
            {
                SchemaVersion = 1,
                ShineData = ShineSaveSystem.CollectShineData(
                    RunManager.Instance.Run.GetLocalPlayer().Hand
                    // 实际需根据 RunState 访问完整牌组
                )
            };

            string json = JsonSerializer.Serialize(modData, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            saveStore.WriteFile(modPath, json);
            MainFile.Logger.Info($"[RunSaveManager_Patches] Mod 数据已保存: {modPath}");
        }
        catch (Exception ex)
        {
            // 不抛出异常，避免影响游戏主存档流程
            MainFile.Logger.Error($"[RunSaveManager_Patches] 保存 Mod 数据失败: {ex.Message}");
        }
    }

    // ─────────────────────────────────────────────
    // 加载：LoadRunSave Postfix
    // 仅在成功加载主存档后才恢复 Mod 数据
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(RunSaveManager), nameof(RunSaveManager.LoadRunSave))]
    [HarmonyPostfix]
    private static void LoadRunSave_Postfix(
        RunSaveManager __instance,
        ReadSaveResult<SerializableRun> __result)
    {
        // 只有主存档成功时才尝试加载 Mod 数据
        if (!__result.Success) return;

        try
        {
            var saveStore = GetSaveStore(__instance);
            var modPath = GetModSavePath(__instance);
            if (saveStore == null || modPath == null) return;

            if (!saveStore.FileExists(modPath))
            {
                MainFile.Logger.Info("[RunSaveManager_Patches] 未找到 Mod 存档文件，跳过恢复");
                return;
            }

            string? json = saveStore.ReadFile(modPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                MainFile.Logger.Warn("[RunSaveManager_Patches] Mod 存档文件为空");
                return;
            }

            var modData = JsonSerializer.Deserialize<KarenRunSaveData>(json);
            if (modData == null) return;

            // 将数据暂存到静态容器，等待 RunManager 初始化完成后再恢复到 CardModel
            KarenModSaveBuffer.Pending = modData;
            MainFile.Logger.Info($"[RunSaveManager_Patches] Mod 存档已加载: {modData.ShineData.Count} 条 Shine 数据");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[RunSaveManager_Patches] 加载 Mod 数据失败: {ex.Message}");
        }
    }

    // ─────────────────────────────────────────────
    // 删除：DeleteCurrentRun Postfix
    // 主存档删除时同步删除 Mod 伴随文件
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(RunSaveManager), nameof(RunSaveManager.DeleteCurrentRun))]
    [HarmonyPostfix]
    private static void DeleteCurrentRun_Postfix(RunSaveManager __instance)
    {
        try
        {
            var saveStore = GetSaveStore(__instance);
            var modPath = GetModSavePath(__instance);
            if (saveStore == null || modPath == null) return;

            if (saveStore.FileExists(modPath))
            {
                saveStore.DeleteFile(modPath);
                MainFile.Logger.Info($"[RunSaveManager_Patches] Mod 存档已删除: {modPath}");
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[RunSaveManager_Patches] 删除 Mod 存档失败: {ex.Message}");
        }
    }
}
```

### 4.3 数据缓冲区（加载后延迟恢复）

由于 `LoadRunSave` 时 `RunManager` 的牌组对象还未完全初始化，需要缓冲加载到的数据：

```csharp
// src/Core/Save/KarenModSaveBuffer.cs

namespace ShoujoKagekiAijoKaren;

/// <summary>
/// 存档加载期间的临时缓冲区
/// LoadRunSave 时先缓存，RunManager 初始化完成后再应用到 CardModel
/// </summary>
public static class KarenModSaveBuffer
{
    public static KarenRunSaveData? Pending { get; set; }

    public static bool HasPending => Pending != null;

    /// <summary>
    /// 消费缓冲数据（取出后清空）
    /// </summary>
    public static KarenRunSaveData? Consume()
    {
        var data = Pending;
        Pending = null;
        return data;
    }
}
```

### 4.4 在战斗开始时恢复数据

```csharp
// 在 ShinePatch.cs 或战斗开始 Hook 中调用

// Hook: BeforeCombatStart 或 AfterCombatStart
if (KarenModSaveBuffer.HasPending)
{
    var saved = KarenModSaveBuffer.Consume();
    if (saved != null)
    {
        var deck = RunManager.Instance.Run.GetLocalPlayer().Deck;
        ShineSaveSystem.RestoreShineData(deck, saved.ShineData);
    }
}
```

---

## 5. 关键注意事项

### async 方法的 Postfix 时序

`RunSaveManager.SaveRun` 返回 `Task`，Harmony Postfix 在 Task **创建时**执行，而非 Task **完成时**。

- **对于写不同文件**：可接受，使用同步写入 `saveStore.WriteFile()`（非 async）即可
- **若需严格在主存档写入完成后执行**：需在 Postfix 中对 `__result`（Task）调用 `.ContinueWith`

```csharp
// 严格时序版本（复杂度更高）
[HarmonyPostfix]
private static void SaveRun_Postfix(RunSaveManager __instance, ref Task __result)
{
    __result = __result.ContinueWith(_ => WriteModData(__instance));
}
```

### 多人游戏

`RunSaveManager` 同时管理单人和多人存档。若 Mod 数据不涉及多人，建议检查：
```csharp
if (RunManager.Instance.NetService.Type != NetGameType.Singleplayer) return;
```

### 存档版本控制

当 Mod 更新需要更改 `KarenRunSaveData` 结构时，通过 `schema_version` 字段手动处理兼容性：
```csharp
if (modData.SchemaVersion < 2)
{
    // 从旧格式迁移...
}
```

### ISaveStore.WriteFile 路径要求

`GodotFileIo` 的 `ValidateGodotFilePath` 要求路径包含 `://`（Godot 虚拟路径，如 `user://`）。`RunSaveManager.GetRunSavePath` 返回的路径已满足此要求，直接使用即可。

---

## 6. 文件路径汇总

| 文件 | 路径 |
|---|---|
| 游戏局内存档 | `user://profiles/<id>/saves/current_run.save` |
| Mod 数据 | 嵌入 `current_run.save`，字段名 `"karen_mod_data"`（无独立文件） |
| 游戏进度存档 | `user://profiles/<id>/saves/progress.save` |
| 游戏存档备份 | `<path>.backup`（自动创建） |

---

## 7. 相关源码位置

| 组件 | 游戏源码路径 |
|---|---|
| `RunSaveManager` | `D:\claudeProj\sts2\src\Core\Saves\Managers\RunSaveManager.cs` |
| `ProgressSaveManager` | `D:\claudeProj\sts2\src\Core\Saves\Managers\ProgressSaveManager.cs` |
| `JsonSerializationUtility` | `D:\claudeProj\sts2\src\Core\Saves\JsonSerializationUtility.cs` |
| `GodotFileIo` / `ISaveStore` | `D:\claudeProj\sts2\src\Core\Saves\GodotFileIo.cs` |
| `MigrationManager` | `D:\claudeProj\sts2\src\Core\Saves\Migrations\MigrationManager.cs` |
| `CombatManager` | `D:\claudeProj\sts2\src\Core\Combat\CombatManager.cs` |

| 组件 | Mod 源码路径 |
|---|---|
| `KarenRunSaveData` | `src/Core/SaveSystem/KarenRunSaveData.cs` |
| `KarenModSaveBuffer` | `src/Core/SaveSystem/KarenModSaveBuffer.cs` |
| `RunSaveManager_Patches` | `src/Core/SaveSystem/RunSaveManager_Patches.cs` |
| `ShineSaveSystem` | `src/Core/Shine/ShineSaveSystem.cs` |
