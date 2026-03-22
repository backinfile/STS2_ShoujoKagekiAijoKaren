# PromisePile 接入 GlobalMoveSystem 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 约定牌堆进/出操作触发 `Hook.AfterCardChangedPiles`，使 `GlobalMoveSystem.OnCardMoved` 能感知这些移动。

**Architecture:** 新增自定义 `PileType` 枚举值标识约定牌堆；`GlobalMovePatch` 检测 `IsInPromisePile` 来正确推断 `newPile`；`PromisePileManager` 在进/出操作中手动调 Hook，离开时用 `SuppressOnce` 抑制 `CardPileCmd.Add` 的错误自动触发。

**Tech Stack:** C# .NET 9.0, Harmony (HarmonyLib), BaseLib `[CustomEnum]`, STS2 Hook 系统

---

## 文件变更一览

| 文件 | 操作 |
|---|---|
| `src/Core/KarenCardTags.cs` | 删除（内容迁移至 KarenCustomEnum.cs） |
| `src/Core/KarenCustomEnum.cs` | 新建（原 KarenCardTags 内容 + 新增 PileType PromisePile） |
| `src/Core/Models/Cards/basic/KarenFall.cs` | 修改：`KarenCardTags` → `KarenCustomEnum` |
| `src/Core/Patches/CardHoverTipsPatch.cs` | 修改：`KarenCardTags` → `KarenCustomEnum` |
| `src/Core/PromisePileSystem/Patches/PromisePileHoverPatch.cs` | 修改：`KarenCardTags` → `KarenCustomEnum` |
| `src/Core/GlobalMoveSystem/Patches/GlobalMovePatch.cs` | 修改：新增 `SuppressOnce` + `IsInPromisePile` 检查 |
| `src/Core/PromisePileSystem/PromisePileManager.cs` | 修改：AddToPromisePile / DrawFromPromisePileAsync / DiscardAllAsync / ClearPromisePile 注释 |

---

## Task 1: KarenCardTags → KarenCustomEnum（重命名 + 新增 PileType）

**Files:**
- 新建：`src/Core/KarenCustomEnum.cs`
- 删除：`src/Core/KarenCardTags.cs`
- 修改：`src/Core/Models/Cards/basic/KarenFall.cs:10,22`
- 修改：`src/Core/Patches/CardHoverTipsPatch.cs:24`
- 修改：`src/Core/PromisePileSystem/Patches/PromisePileHoverPatch.cs:4,48`

- [ ] **Step 1：新建 KarenCustomEnum.cs**

内容如下（命名空间与原 KarenCardTags.cs 保持一致 `ShoujoKagekiAijoKaren.src.Core`）：

```csharp
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace ShoujoKagekiAijoKaren.src.Core;

public static class KarenCustomEnum
{
    /// <summary>约定牌堆相关卡牌的标记 Tag</summary>
    [CustomEnum] public static CardTag PromisePileRelated;

    /// <summary>约定牌堆虚拟 PileType，用于 GlobalMoveSystem 事件的 from/to 标识</summary>
    [CustomEnum] public static PileType PromisePile;
}
```

- [ ] **Step 2：删除 KarenCardTags.cs**

```bash
git rm "src/Core/KarenCardTags.cs"
```

- [ ] **Step 3：更新三处引用**

**KarenFall.cs**：仅修改第 22 行的类名引用。第 10 行 `using ShoujoKagekiAijoKaren.src.Core;` 不需要改动（`KarenCustomEnum` 与 `KarenCardTags` 在同一命名空间）：

```csharp
// 第 22 行 - 仅改类名
protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { KarenCustomEnum.PromisePileRelated };
```

**CardHoverTipsPatch.cs** 第 24 行（C# 的命名空间查找会向上遍历父级命名空间；`src.Core.Patches` 内的代码可直接访问 `src.Core` 的类型，无需新增 using——当前文件没有该 using 却引用了 `KarenCardTags` 且编译通过，即为此证）：

```csharp
("KAREN_PROMISE_PILE", card => card.Tags.Contains(KarenCustomEnum.PromisePileRelated)),
```

**PromisePileHoverPatch.cs** 第 48 行：

```csharp
if (!cardModel.Tags.Contains(KarenCustomEnum.PromisePileRelated))
```

- [ ] **Step 4：编译确认无错误**

```bash
cd "D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren" && dotnet build 2>&1 | tail -5
```

期望：`Build succeeded.`（0 errors）

- [ ] **Step 5：提交**

```bash
git add src/Core/KarenCustomEnum.cs \
        src/Core/Models/Cards/basic/KarenFall.cs \
        src/Core/Patches/CardHoverTipsPatch.cs \
        src/Core/PromisePileSystem/Patches/PromisePileHoverPatch.cs
git commit -m "refactor: rename KarenCardTags to KarenCustomEnum, add PileType PromisePile"
```

---

## Task 2: GlobalMovePatch — 新增 SuppressOnce + PromisePile 识别

**Files:**
- 修改：`src/Core/GlobalMoveSystem/Patches/GlobalMovePatch.cs`

- [ ] **Step 1：修改 GlobalMovePatch.cs**

完整替换文件内容为：

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.GlobalMoveSystem.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardChangedPiles))]
internal static class GlobalMovePatch
{
    /// <summary>
    /// 约定牌堆的"离开"操作（Draw/DiscardAll）会先手动 Invoke 正确事件，再调 CardPileCmd.Add。
    /// CardPileCmd.Add 触发的 Hook 携带错误的 oldPile=None，加入此集合以跳过一次。
    /// 经验证 CardPileCmd.Add 对单张卡只触发一次 AfterCardChangedPiles，remove-on-first-hit 安全。
    /// </summary>
    internal static readonly HashSet<CardModel> SuppressOnce = new();

    // Prefix 在 async 状态机启动前同步执行。
    // 此时卡牌已完成物理移动（CardPileCmd.Add 先移牌再调用 Hook），
    // 故 card.Pile?.Type 即为新牌堆，oldPile 参数为旧牌堆。
    [HarmonyPrefix]
    private static void Prefix(
        IRunState runState, CombatState? combatState,
        CardModel card, PileType oldPile, AbstractModel? source)
    {
        if (SuppressOnce.Remove(card)) return;

        PileType newPile;
        // 卡牌进入约定牌堆后 card.Pile 为 null，但 IsInPromisePile 为 true
        if (card.Pile == null && PromisePileManager.IsInPromisePile(card))
            newPile = KarenCustomEnum.PromisePile;
        else
            newPile = card.Pile?.Type ?? PileType.None;

        GlobalMoveSystem.Invoke(card, oldPile, newPile, source);
    }
}
```

- [ ] **Step 2：编译确认无错误**

```bash
cd "D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren" && dotnet build 2>&1 | tail -5
```

期望：`Build succeeded.`

- [ ] **Step 3：提交**

```bash
git add src/Core/GlobalMoveSystem/Patches/GlobalMovePatch.cs
git commit -m "feat(global-move): add SuppressOnce + PromisePile detection to GlobalMovePatch"
```

---

## Task 3: PromisePileManager — 触发 Hook

**Files:**
- 修改：`src/Core/PromisePileSystem/PromisePileManager.cs`

需要修改四处：
1. `AddToPromisePile`：记录 `oldPile`，`pile.AddLast` 后 fire-and-forget 调 Hook
2. `DrawFromPromisePileAsync`：SuppressOnce + CardPileCmd.Add + 手动调 Hook
3. `DiscardAllAsync`：循环内同样处理
4. `ClearPromisePile`：仅更新注释

- [ ] **Step 1：在文件顶部补充 using（先补充，后续编译才不报错）**

在现有 using 区域末尾追加（若已有则跳过）：

```csharp
using MegaCrit.Sts2.Core.Hooks;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.GlobalMoveSystem.Patches;
```

- [ ] **Step 2：修改 AddToPromisePile**

找到 `AddToPromisePile` 方法，将完整方法体替换为（保留方法签名不变）：

```csharp
public static void AddToPromisePile(CardModel card)
{
    if (card?.Owner == null) return;

    var pile = GetPromisePile(card.Owner);
    if (pile.Contains(card))
    {
        MainFile.Logger.Warn($"[PromisePile] '{card.Title}' already in promise pile, skipping");
        return;
    }

    // oldPile 必须在 PlayAddAnimation 之前记录：
    // PlayAddAnimation（同步方法）依赖 card.Pile 定位 NCard；RemoveFromCurrentPile 会将 card.Pile 置为 null
    PileType oldPile = card.Pile?.Type ?? PileType.None;

    // 动画在 RemoveFromCurrentPile 之前执行（FindOnTable 依赖 Pile.Type）
    PromisePileAnimator.PlayAddAnimation(card);

    card.RemoveFromCurrentPile();
    pile.AddLast(card);

    // pile.AddLast 后 IsInPromisePile 为 true，GlobalMovePatch 能正确推断 newPile = PromisePile
    _ = Hook.AfterCardChangedPiles(card.Owner.RunState, card.CombatState, card, oldPile, null);

    MainFile.Logger.Info($"[PromisePile] '{card.Title}' → promise pile (count={pile.Count})");
    OnCardEntered?.Invoke(card);
    if (card is KarenBaseCardModel karenCard)
        _ = karenCard.OnAddedToPromisePile();

    // 更新 Power
    _ = UpdatePowerAsync(card.Owner);
}
```

- [ ] **Step 3：修改 DrawFromPromisePileAsync**

在 `DrawFromPromisePileAsync` 方法中，找到：

```csharp
    await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top);

    // 更新 Power
    await UpdatePowerAsync(player);
```

替换为：

```csharp
    // SuppressOnce 抑制 CardPileCmd.Add 内部触发的错误事件（oldPile=None），
    // 之后手动调 Hook 并传入正确的 oldPile = PromisePile
    GlobalMovePatch.SuppressOnce.Add(card);
    await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top);
    await Hook.AfterCardChangedPiles(card.Owner.RunState, card.CombatState, card, KarenCustomEnum.PromisePile, null);

    // 更新 Power
    await UpdatePowerAsync(player);
```

- [ ] **Step 4：修改 DiscardAllAsync**

在 `DiscardAllAsync` 方法的循环体内，找到：

```csharp
            await CardPileCmd.Add(card, PileType.Discard);
```

替换为：

```csharp
            GlobalMovePatch.SuppressOnce.Add(card);
            await CardPileCmd.Add(card, PileType.Discard);
            await Hook.AfterCardChangedPiles(card.Owner.RunState, card.CombatState, card, KarenCustomEnum.PromisePile, null);
```

- [ ] **Step 5：更新 ClearPromisePile 注释**

找到 `ClearPromisePile` 方法的 `<summary>` 注释，替换为：

```csharp
    /// <summary>
    /// 清空玩家的约定牌堆，将所有卡牌从 CombatState 注销。
    /// 不触发任何扳机，仅清理用。战斗结束时调用；
    /// 此时游戏状态正在销毁，不应触发订阅者逻辑。
    /// 战斗实例被注销后，DeckVersion 仍在 Player.Deck，下场战斗正常使用。
    /// </summary>
```

- [ ] **Step 6：编译确认无错误**

```bash
cd "D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren" && dotnet build 2>&1 | tail -5
```

期望：`Build succeeded.`

- [ ] **Step 7：提交**

```bash
git add src/Core/PromisePileSystem/PromisePileManager.cs
git commit -m "feat(promise-pile): trigger Hook.AfterCardChangedPiles on enter/leave"
```

---

## 验证说明（无自动化测试框架，需游戏内验证）

启动游戏，使用华恋角色进入战斗：

1. **进入约定牌堆**：使用「坠落」将手牌放入约定牌堆 → 日志应出现 `[GlobalMove]` 或订阅者触发（若有）
2. **从约定牌堆摸牌**：触发约定摸牌效果 → 确认无崩溃，卡牌正常进入手牌
3. **约定牌堆弃置**：使用会调用 `DiscardAllAsync` 的卡牌 → 确认无崩溃，卡牌正常进入弃牌堆
4. **战斗结束**：确认 `ClearPromisePile` 正常清空，无异常

若 `GlobalMoveSystem.OnCardMoved` 尚无订阅者，可临时在 `MainFile.cs` 或任意初始化点添加调试订阅：

```csharp
GlobalMoveSystem.OnCardMoved += (card, from, to, _) =>
    MainFile.Logger.Info($"[GlobalMove] {card.Title}: {from} → {to}");
```
