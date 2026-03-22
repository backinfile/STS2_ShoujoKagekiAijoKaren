# PromisePile Cmd 扩展 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为约定牌堆系统新增三个 Cmd：从弃牌堆/抽牌堆选牌放入约定牌堆，以及将约定牌堆全部牌弃置到弃牌堆。

**Architecture:** 在 `PromisePileManager` 中添加两个内部异步方法（`AddFromPileAsync`、`DiscardAllAsync`），再在 `PromisePileCmd` 中暴露对外接口。选牌使用游戏原生 `CardSelectCmd.FromSimpleGrid`；`minSelect == maxSelect` 时单张自动确认，多张显示确认按钮。

**Tech Stack:** C# .NET 9.0，Godot 4.5.1（MegaDot），Harmony，BaseLib，STS2 原生 API（`CardSelectCmd`、`CardPileCmd`、`CardSelectorPrefs`、`PileType`）

---

## 文件修改清单

| 操作 | 路径 |
|------|------|
| 修改 | `src/Core/PromisePileSystem/PromisePileManager.cs` |
| 修改 | `src/Core/PromisePileSystem/Commands/PromisePileCmd.cs` |

---

### Task 1：在 PromisePileManager 中新增 `AddFromPileAsync`

**Files:**
- Modify: `src/Core/PromisePileSystem/PromisePileManager.cs`

#### 背景知识
- `PileType.Discard.GetPile(player)` 返回 `CardPile`，其 `.Cards` 为当前牌堆所有牌的列表（只读快照）
- `CardSelectCmd.FromSimpleGrid(ctx, cardList, player, prefs)` 返回 `Task<IReadOnlyList<CardModel>>`
- `CardSelectorPrefs(LocString prompt, int minSelect, int? maxSelect)` — `minSelect == maxSelect` 时 `RequireManualConfirmation=false`，选满 N 张后**自动确认**（无需手动点按钮）；1 张时单击即确认
- `FromSimpleGrid` 返回 `Task<IEnumerable<CardModel>>`，用 `var` 接收兼容
- 选完后对每张牌调用已有的 `AddToPromisePile(card)`，该方法内部已处理 `RemoveFromCurrentPile` + 动画尝试（弃/抽牌堆的牌没有 NCard on table，动画静默跳过）

- [ ] **Step 1：添加 using 语句**

在 `PromisePileManager.cs` 顶部新增：
```csharp
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;
```

- [ ] **Step 2：实现 `AddFromPileAsync`**

在 `PromisePileManager.cs` 的 `ClearPromisePile` 方法之后插入：

```csharp
/// <summary>
/// 从指定牌堆（弃牌堆或抽牌堆）让玩家选择最多 count 张牌放入约定牌堆。
/// 若牌堆为空或实际可选数为 0 则直接返回。
/// minSelect == maxSelect，1 张自动确认，多张需点确认按钮。
/// </summary>
public static async Task AddFromPileAsync(
    PlayerChoiceContext ctx, Player player, PileType pileType, int count, LocString prompt)
{
    if (player == null) return;

    var cardPile = pileType.GetPile(player);
    // 拍快照：避免选牌过程中列表变动
    var cards = cardPile.Cards.ToList();
    if (cards.Count == 0) return;

    int selectCount = Math.Min(count, cards.Count);
    var prefs = new CardSelectorPrefs(prompt, selectCount, selectCount);

    var selected = await CardSelectCmd.FromSimpleGrid(ctx, cards, player, prefs);
    if (selected == null) return;

    foreach (var card in selected)
        AddToPromisePile(card);
}
```

- [ ] **Step 3：编译确认无报错**

在 Godot 编辑器或 `dotnet build` 中确认编译通过，无 CS 错误。

- [ ] **Step 4：Commit**

```bash
git add src/Core/PromisePileSystem/PromisePileManager.cs
git commit -m "feat(promise-pile): add AddFromPileAsync to PromisePileManager"
```

---

### Task 2：在 PromisePileManager 中新增 `DiscardAllAsync`

**Files:**
- Modify: `src/Core/PromisePileSystem/PromisePileManager.cs`

#### 背景知识
- 约定牌堆是虚拟的 `LinkedList<CardModel>`，牌进入时已调用 `RemoveFromCurrentPile()`，不属于任何真实 PileType
- `CardPileCmd.Add(card, PileType.Discard)` 可将"游离"的牌加入弃牌堆，无需事先 RemoveFromCurrentPile
- `ctx` 参数沿游戏约定传入，与 `DrawFromPromisePileAsync` 保持接口一致

- [ ] **Step 1：实现 `DiscardAllAsync`**

在 `AddFromPileAsync` 方法之后插入：

```csharp
/// <summary>
/// 将约定牌堆中的所有牌依次弃置到弃牌堆（FIFO 顺序）。
/// 约定牌堆为空时直接返回。
/// </summary>
public static async Task DiscardAllAsync(PlayerChoiceContext ctx, Player player)
{
    if (player == null) return;

    var pile = GetPromisePile(player);
    if (pile.Count == 0) return;

    while (pile.Count > 0)
    {
        var card = pile.First!.Value;
        pile.RemoveFirst();
        MainFile.Logger.Info($"[PromisePile] '{card.Title}' ← promise pile → discard");
        OnCardLeft?.Invoke(card);
        await CardPileCmd.Add(card, PileType.Discard);
    }

    await UpdatePowerAsync(player);
}
```

- [ ] **Step 2：编译确认无报错**

- [ ] **Step 3：Commit**

```bash
git add src/Core/PromisePileSystem/PromisePileManager.cs
git commit -m "feat(promise-pile): add DiscardAllAsync to PromisePileManager"
```

---

### Task 3：在 PromisePileCmd 中暴露三个新 Cmd

**Files:**
- Modify: `src/Core/PromisePileSystem/Commands/PromisePileCmd.cs`

- [ ] **Step 1：添加 using 语句**

在 `PromisePileCmd.cs` 顶部新增：
```csharp
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
```

- [ ] **Step 2：新增三个公开方法**

在文件末尾（`}` 闭合前）插入：

```csharp
/// <summary>
/// 从弃牌堆让玩家选择最多 count 张牌放入约定牌堆。
/// prompt 通常传调用方卡牌的 SelectionScreenPrompt。
/// </summary>
public static Task AddFromDiscard(
    PlayerChoiceContext ctx, Player player, int count, LocString prompt)
    => PromisePileManager.AddFromPileAsync(ctx, player, PileType.Discard, count, prompt);

/// <summary>
/// 从抽牌堆让玩家选择最多 count 张牌放入约定牌堆。
/// prompt 通常传调用方卡牌的 SelectionScreenPrompt。
/// </summary>
public static Task AddFromDraw(
    PlayerChoiceContext ctx, Player player, int count, LocString prompt)
    => PromisePileManager.AddFromPileAsync(ctx, player, PileType.Draw, count, prompt);

/// <summary>
/// 将约定牌堆中所有牌弃置到弃牌堆（FIFO 顺序）。
/// </summary>
public static Task DiscardAll(PlayerChoiceContext ctx, Player player)
    => PromisePileManager.DiscardAllAsync(ctx, player);
```

- [ ] **Step 3：编译确认无报错**

- [ ] **Step 4：Commit**

```bash
git add src/Core/PromisePileSystem/Commands/PromisePileCmd.cs
git commit -m "feat(promise-pile): expose AddFromDiscard/AddFromDraw/DiscardAll in PromisePileCmd"
```

---

### Task 4：游戏内验证

无自动化测试框架，通过游戏内临时卡牌手动验证。

- [ ] **Step 1：验证 AddFromDiscard**

在任意现有卡牌的 `Play` 方法中临时加入：
```csharp
await PromisePileCmd.AddFromDiscard(ctx, base.Owner, 2, base.SelectionScreenPrompt);
```
进入战斗，先打出几张牌让它们进入弃牌堆，再打出该卡。

预期：弹出选牌界面，显示弃牌堆中的牌；选满 2 张（或弃牌堆只有 1 张时自动确认）后，所选牌从弃牌堆消失，进入约定牌堆（Power 数值更新）。

- [ ] **Step 2：验证 AddFromDraw**

同上，改为：
```csharp
await PromisePileCmd.AddFromDraw(ctx, base.Owner, 1, base.SelectionScreenPrompt);
```
预期：弹出选牌界面，显示抽牌堆中的牌；选中后牌从抽牌堆消失，进入约定牌堆。

- [ ] **Step 3：验证 DiscardAll**

在约定牌堆中放入 2-3 张牌后，触发：
```csharp
await PromisePileCmd.DiscardAll(ctx, base.Owner);
```
预期：约定牌堆清空（Power 数值归 0），对应张数的牌出现在弃牌堆中，日志打印每张牌的转移记录。

- [ ] **Step 4：移除临时测试代码，Commit**

```bash
git add -p   # 仅提交清理后的正式代码
git commit -m "test: verify promise pile cmd extensions in-game (cleanup)"
```
