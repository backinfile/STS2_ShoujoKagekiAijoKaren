# 闪耀系统（Shine System）

## 核心概念
带 Shine 值的卡牌每次被打出后 Shine -1，归零后从卡组移除。
类似"限定次数使用"机制（不同于消耗 Exhaust，是达到使用次数上限后移除）。

## 数据存储
使用 BaseLib 的 `SpireField<CardModel, int>` 动态附加到 CardModel 实例。
无需修改原始类，通过扩展方法访问：
- `card.GetShineValue()` / `card.SetShineCurrent(n)` / `card.SetShineMax(n)`
- `card.DecreaseShine()` / `card.AddShine(n)` / `card.RestoreShineToMax()`

## 关键文件
- `src/Core/ShineSystem/ShineExtension.cs` — SpireField 扩展方法
- `src/Core/ShineSystem/ShineSaveSystem.cs` — 闪耀值（ShineData）存档逻辑 + `ShineSaveData` 数据类
- `src/Core/ShineSystem/ShinePileManager.cs` — 闪耀牌堆管理器（含牌堆存档逻辑）

## Patch 实现
1. **ShinePatch** — 拦截 `OnPlayWrapper`，打出后调用 `DecreaseShine()`，并同步 DeckVersion 的值
2. **ShineGlobalPatch** — 三个功能：
   - `GetDescriptionForPile` Postfix：将 `{KarenShine}` 替换为当前值
   - `MutableClone` Postfix：克隆时复制 Shine 值
   - `HoverTips_Patch`：自动添加悬浮提示
3. **ShinePilePatch** — 闪耀耗尽两步流程 + 战斗结束日志

## 闪耀耗尽流程（ShinePilePatch）

### Step 1 — 动画结算
**非能力牌**（`CardPileCmd.Add` Prefix，Shine==0 时拦截）：
1. `NCard.FindOnTable(card)` 找到战斗 NCard（须在 `RemoveFromCurrentPile` 前调用，因依赖 `Pile.Type`）
2. 将战斗 NCard `Reparent` 到 `NRun.Instance.GlobalUi.CardPreviewContainer`（顶层）
3. 播放压扁变黑 Tween，结束时 `QueueFree`（fire-and-forget）
4. `card.RemoveFromCurrentPile()` + `RemoveFromState()`（反射调用）清理战斗实例
5. 阻断原 Add，返回成功结果

**能力牌**（`CardPileCmd.RemoveFromCombat` Postfix）：
- 放行原方法，游戏自带能力牌消退动画正常播放
- Postfix 中执行 Step 2

### Step 2 — 数据处理（两种牌均执行）
- `GetShinePileTarget(card)` 优先取 `card.DeckVersion`，回退到自身
- `ShinePileManager.AddToShinePile(target)` — 内部调用 `RemoveFromCurrentPile` 后加入虚拟牌堆

### 战斗结束日志（`Player_AfterCombatEnd_ShinePilePatch`）
Postfix `Player.AfterCombatEnd`，对 Karen 玩家打印闪耀牌堆内容：
`共 N 张（M 种）: 卡名1(MaxShine), 卡名2(MaxShine)...`

### 动画时长（随快速模式缩放）
| FastMode | 展示等待 | 压扁/变黑 |
|:---:|:---:|:---:|
| Normal | 1.5s | 0.30s |
| Fast | 0.4s | 0.15s |
| Instant | 0.01s | 0.01s |

读取方式：`SaveManager.Instance.PrefsSave.FastMode`（枚举：`FastModeType.None/Normal/Fast/Instant`）

### 关键 API
- `NCard.FindOnTable(card)` — 按 `Pile.Type` 查找战斗 NCard（Play pile → `GetCardFromPlayContainer`）
- `node.Reparent(newParent)` — Godot 内置，保留全局坐标
- `NRun.Instance.GlobalUi.CardPreviewContainer` — 顶层容器，`AddChildSafely` 后自动水平居中布局

## 跨战斗保存（已完整接入存档系统）
完整流程：
1. **保存**：`GodotFileIo.WriteFile(Async)` Prefix → 同时收集 ShineData + ShinePileData → 注入 `"karen_mod_data"` JSON 字段
   - 同时拦截单机（`current_run.save`）和联机（`current_run_mp.save`）存档
2. **读取**：`GodotFileIo.ReadFile` Postfix → `KarenModSaveBuffer.Store(data)`（CardModel 实例尚未就绪，需缓冲）
3. **恢复**：`RunManager.SetUpSavedSinglePlayer/SetUpSavedMultiPlayer` Postfix → 分别恢复 ShineData 和 ShinePileData
   - 此时 `RunState` 和卡组均已就绪，不依赖进入战斗

`KarenRunSaveData` 格式：
- `player_shine_data`：`Dictionary<int, List<ShineSaveData>>`，Key = 玩家在 `RunState.Players` 中的下标
- `player_shine_pile_data`：同类型，存耗尽牌堆（ShineCurrent 恒为 0，ShineMax 保留原始值）
- 单机/联机格式统一

`ShineSaveData` 字段：`CardId`、`Index`（Deck.Cards 下标，-1 表示已从 Deck 移出）、`ShineCurrent`、`ShineMax`，按下标+ID双重校验恢复。

## ShinePileManager 职责（`src/Core/ShineSystem/ShinePileManager.cs`）
- `SpireField<Player, List<CardModel>> _shinePile` — 每玩家独立的耗尽卡牌列表
- `GetShinePile(player)` / `IsInShinePile(card)` / `AddToShinePile(card)` / `RemoveFromShinePile(card)`
- `GetShinePileCount(player)` — 总数量
- `GetUniqueCardCount(player)` — 不同种类数量（按 `Id.Entry` 去重）
- **存档方法**（闪耀牌堆相关）：
  - `CollectShinePileData(player)` — 收集单玩家耗尽牌堆数据
  - `CollectAllPlayersShinePileData(players)` — 全玩家
  - `RestoreAllPlayersShinePileData(players, data)` — 恢复全玩家
  - `RestoreShinePileData(player, list)`（private）— 单玩家恢复

## ShineSaveSystem 职责（`src/Core/ShineSystem/ShineSaveSystem.cs`）
仅负责闪耀值（Shine Value）的存档，不含牌堆逻辑：
- `CollectShineData(cards)` — 收集单牌组 Shine 值
- `CollectAllPlayersShineData(players)` — 全玩家
- `RestoreShineData(cards, list)` — 恢复单牌组 Shine 值
- `RestoreAllPlayersShineData(players, data)` — 全玩家

## RunSaveManager_Patches.cs 职责
纯协调层，负责文件拦截和 JSON 注入/提取：
- `InjectModData`：调用 `ShineSaveSystem.CollectAllPlayersShineData` + `ShinePileManager.CollectAllPlayersShinePileData`
- `ConsumeAndRestore`：调用 `ShineSaveSystem.RestoreAllPlayersShineData` + `ShinePileManager.RestoreAllPlayersShinePileData`

## 已知 Bug（已解决）
时序竞争：卡牌打出后删除动画（异步）与主流程卡牌归堆（同步）竞争。
解决方案：拦截 CardPileCmd.Add（即 ShinePilePatch 的实现）。
详见：`docs/ShinePatch_Bug_Analysis.md`

## 卡牌描述占位符
在卡牌描述中使用 `{KarenShine}` 会被自动替换为当前 Shine 值。

## 闪耀文本与消耗关键字同行规则
`ShineGlobalPatch.GetDescriptionForPile_Postfix` 在追加闪耀文本时：
- 若 `CanonicalKeywords` 含 `CardKeyword.Exhaust`：直接拼接（无 `\n`），与"消耗。"同行
  - 中文效果：`消耗。闪耀3。`
- 其他情况：换行追加 `\n` + 闪耀文本

## HoverTips 关键词提示（待修复 BUG）

`ShineGlobalPatch.HoverTips_Patch` 当前实现使用 `LocString("keywords", "KAREN_SHINE.title/.description")`。

**问题**：游戏需要 `keywords` 本地化表，但 Mod 中没有创建该文件，导致运行时报错：
`LocException: The loc table='keywords' does not exist!`

**错误状态**：`cards.json` 中有误加的 `KAREN_SHINE_KEYWORD.*` 条目（键名和表均错误，需清理）。

**正确做法**：
1. 在 `ShoujoKagekiAijoKaren/localization/eng/` 和 `zhs/` 下各创建 `keywords.json`
2. 分别添加 `KAREN_SHINE.title` 和 `KAREN_SHINE.description` 键
3. 清理 `cards.json` 中误加的 `KAREN_SHINE_KEYWORD.*` 条目
4. Mod 注册时需要加载 `keywords` 表（确认 BaseLib 的本地化注册方式）

**Patch 结构**（已是正确的 Keywords 数组模式，无需改代码）：
```csharp
private static readonly (string Key, Func<CardModel, bool> Condition)[] Keywords =
[
    ("KAREN_SHINE", card => card.IsShineInitialized()),
];
// 使用 LocString("keywords", key + ".title/.description")
// 已有重复检查：tips.Any(ht.Title == titleText)
```
