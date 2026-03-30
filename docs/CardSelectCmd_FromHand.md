# CardSelectCmd.FromHand 手牌选择系统分析

> 本文档分析《Slay the Spire 2》游戏本体代码中从手牌选择卡牌的核心机制。

## 1. 方法概述

`CardSelectCmd.FromHand` 是游戏战斗中用于让玩家从手牌选择指定数量卡牌的核心命令方法。

### 1.1 方法签名

```csharp
public static async Task<IEnumerable<CardModel>> FromHand(
    PlayerChoiceContext context,      // 玩家选择上下文
    Player player,                    // 目标玩家
    CardSelectorPrefs prefs,          // 选择器偏好设置
    Func<CardModel, bool>? filter,    // 可选：卡牌过滤条件
    AbstractModel source              // 触发选择的来源（如卡牌/遗物）
)
```

### 1.2 命名空间

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
```

---

## 2. 执行流程详解

### 2.1 整体流程图

```
┌─────────────────────────────────────────────────────────────┐
│  FromHand 执行流程                                           │
├─────────────────────────────────────────────────────────────┤
│  1. 检查战斗状态                                             │
│     └─ 如果战斗已结束，返回空列表                             │
│                                                             │
│  2. 本地玩家预处理                                           │
│     └─ 取消所有正在进行的卡牌播放                             │
│                                                             │
│  3. 获取可选手牌列表                                         │
│     └─ 从 PileType.Hand 获取并应用 filter                    │
│                                                             │
│  4. 快速返回判断                                             │
│     ├─ 列表为空 → 返回空列表                                 │
│     ├─ 无需确认且数量≤最小选择数 → 直接返回全部               │
│     └─ 有测试选择器 → 使用测试选择器                          │
│                                                             │
│  5. 进入UI选择模式                                           │
│     ├─ 预约选择ID（多人同步用）                               │
│     ├─ 发送选择开始信号                                       │
│     ├─ 调用 NPlayerHand.SelectCards()                        │
│     ├─ 同步选择结果（本地）/ 等待远程选择（联机）              │
│     └─ 发送选择结束信号                                       │
│                                                             │
│  6. 返回选择的卡牌列表                                        │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 核心代码流程

```csharp
public static async Task<IEnumerable<CardModel>> FromHand(
    PlayerChoiceContext context, Player player,
    CardSelectorPrefs prefs, Func<CardModel, bool>? filter, AbstractModel source)
{
    // 1. 战斗结束检查
    if (CombatManager.Instance.IsOverOrEnding)
        return Array.Empty<CardModel>();

    // 2. 本地玩家预处理
    if (ShouldSelectLocalCard(player))
        NPlayerHand.Instance?.CancelAllCardPlay();

    // 3. 获取手牌并过滤
    List<CardModel> list = PileType.Hand
        .GetPile(player)
        .Cards
        .Where(filter ?? (_ => true))
        .ToList();

    // 4. 快速返回路径
    if (list.Count == 0) return list;
    if (!prefs.RequireManualConfirmation && list.Count <= prefs.MinSelect) return list;
    if (Selector != null)  // 测试模式
        return await Selector.GetSelectedCards(list, prefs.MinSelect, prefs.MaxSelect);

    // 5. UI选择流程
    uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
    await context.SignalPlayerChoiceBegun(PlayerChoiceOptions.CancelPlayCardActions);

    IEnumerable<CardModel> result;
    if (ShouldSelectLocalCard(player))
    {
        // 本地玩家：打开UI选择
        result = await NCombatRoom.Instance.Ui.Hand.SelectCards(prefs, filter, source);
        RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(player, choiceId,
            PlayerChoiceResult.FromMutableCombatCards(result));
    }
    else
    {
        // 远程玩家：等待同步
        result = (await RunManager.Instance.PlayerChoiceSynchronizer
            .WaitForRemoteChoice(player, choiceId)).AsCombatCards();
    }

    await context.SignalPlayerChoiceEnded();
    return result;
}
```

---

## 3. CardSelectorPrefs 配置详解

`CardSelectorPrefs` 是一个 `struct`，用于配置卡牌选择器的行为。

### 3.1 属性说明

| 属性 | 类型 | 说明 |
|------|------|------|
| `Prompt` | `LocString` | 选择界面顶部显示的提示文本 |
| `MinSelect` | `int` | 最少需要选择的卡牌数量 |
| `MaxSelect` | `int` | 最多可以选择的卡牌数量 |
| `RequireManualConfirmation` | `bool` | 是否需要手动点击确认按钮 |
| `Cancelable` | `bool` | 是否允许取消选择 |
| `UnpoweredPreviews` | `bool` | 是否显示无能力加成的预览 |
| `PretendCardsCanBePlayed` | `bool` | 是否假装卡牌可打出（用于视觉效果） |
| `ShouldGlowGold` | `Func<CardModel, bool>?` | 自定义哪些卡牌应该显示金色高亮 |

### 3.2 预设提示文本

```csharp
public static LocString TransformSelectionPrompt => new LocString("card_selection", "TO_TRANSFORM");
public static LocString ExhaustSelectionPrompt  => new LocString("card_selection", "TO_EXHAUST");
public static LocString RemoveSelectionPrompt   => new LocString("card_selection", "TO_REMOVE");
public static LocString EnchantSelectionPrompt  => new LocString("card_selection", "TO_ENCHANT");
public static LocString DiscardSelectionPrompt  => new LocString("card_selection", "TO_DISCARD");
public static LocString UpgradeSelectionPrompt  => new LocString("card_selection", "TO_UPGRADE");
```

### 3.3 构造函数

```csharp
// 固定数量选择（Min = Max）
public CardSelectorPrefs(LocString prompt, int selectCount)

// 范围数量选择
public CardSelectorPrefs(LocString prompt, int minCount, int maxCount)
```

**注意**：当 `MinSelect >= 0` 且 `MinSelect != MaxSelect` 时，`RequireManualConfirmation` 自动设为 `true`。

---

## 4. UI 层实现：NPlayerHand.SelectCards

### 4.1 方法签名

```csharp
public async Task<IEnumerable<CardModel>> SelectCards(
    CardSelectorPrefs prefs,
    Func<CardModel, bool>? filter,
    AbstractModel? source,
    Mode mode = Mode.SimpleSelect
)
```

### 4.2 选择模式（Mode）

| 模式 | 说明 |
|------|------|
| `Mode.Play` | 正常打牌模式 |
| `Mode.SimpleSelect` | 简单选择模式（多选） |
| `Mode.UpgradeSelect` | 升级选择模式（单选+预览） |

### 4.3 UI 选择流程

```csharp
public async Task<IEnumerable<CardModel>> SelectCards(...)
{
    // 1. 取消当前正在进行的卡牌播放
    CancelAllCardPlay();

    // 2. 显示背景遮罩（渐变淡入）
    _selectModeBackstop.Visible = true;
    // Tween 动画: self_modulate:a 0 -> 1 (0.2s)

    // 3. 设置选择模式
    CurrentMode = mode;
    _currentSelectionFilter = filter;
    _prefs = prefs;

    // 4. 设置UI状态
    NCombatRoom.Instance.RestrictControllerNavigation(Array.Empty<Control>());
    _selectionCompletionSource = new TaskCompletionSource<IEnumerable<CardModel>>();
    _selectionHeader.Visible = true;
    _selectionHeader.Text = "[center]" + prefs.Prompt.GetFormattedText() + "[/center]";
    PeekButton.Enable();

    // 5. 更新卡牌可见性（根据filter）
    UpdateSelectModeCardVisibility();
    RefreshSelectModeConfirmButton();

    // 6. 等待玩家完成选择
    IEnumerable<CardModel> result = await _selectionCompletionSource.Task;

    // 7. 清理并返回
    AfterCardsSelected(source);
    return result;
}
```

### 4.4 选择确认逻辑

```csharp
// 点击卡牌时（SimpleSelect 模式）
private void SelectCardInSimpleMode(NHandCardHolder holder)
{
    // 已达最大选择数时，自动取消最后一个选择
    if (_selectedCards.Count >= _prefs.MaxSelect)
        _selectedHandCardContainer.DeselectCard(_selectedCards.Last());

    _selectedCards.Add(holder.CardNode.Model);
    _selectedHandCardContainer.Add(holder);  // 移动到选中区域
    RemoveCardHolder(holder);
    RefreshSelectModeConfirmButton();
}

// 确认按钮点击
private void OnSelectModeConfirmButtonPressed(NButton _)
{
    _selectionCompletionSource.SetResult(_selectedCards.ToList());
}

// 检查是否已选够
private void CheckIfSelectionComplete()
{
    if (_selectedCards.Count >= _prefs.MaxSelect)
        _selectionCompletionSource.SetResult(_selectedCards.ToList());
}
```

### 4.5 确认按钮状态

```csharp
private void RefreshSelectModeConfirmButton()
{
    int count = _selectedCards.Count;
    // 只有在 [Min, Max] 范围内才启用确认按钮
    if (count >= _prefs.MinSelect && count <= _prefs.MaxSelect)
        _selectModeConfirmButton.Enable();
    else
        _selectModeConfirmButton.Disable();
}
```

---

## 5. 使用示例

### 5.1 基础用法：从手牌选择1张牌

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    // 选择1张牌（最少1张，最多1张）
    var prefs = new CardSelectorPrefs(
        new LocString("my_mod", "SELECT_CARD_TO_EXHAUST"),
        1, 1
    );

    IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
        choiceContext,
        cardPlay.Player,
        prefs,
        filter: null,  // 不过滤，所有手牌都可选
        source: this
    );

    CardModel targetCard = selected.FirstOrDefault();
    if (targetCard != null)
    {
        // 处理选中的卡牌...
    }
}
```

### 5.2 过滤特定卡牌

```csharp
// 只选择可升级的卡牌
IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
    choiceContext,
    player,
    prefs,
    filter: c => c.IsUpgradable,
    source: this
);
```

### 5.3 使用预设提示

```csharp
// 使用游戏内置的"选择要弃置的牌"提示
var prefs = new CardSelectorPrefs(
    CardSelectorPrefs.DiscardSelectionPrompt,
    1, 2  // 选择1-2张
);
```

### 5.4 快捷方法：FromHandForDiscard

游戏提供了一个专门用于弃牌的选择方法：

```csharp
public static async Task<IEnumerable<CardModel>> FromHandForDiscard(
    PlayerChoiceContext context,
    Player player,
    CardSelectorPrefs prefs,
    Func<CardModel, bool>? filter,
    AbstractModel source
)
{
    // 设置金色高亮：标记"灵巧(Sly)"卡牌
    prefs.ShouldGlowGold = c => c.IsSlyThisTurn &&
        (c.CanPlay(out _, out _) || reason.HasResourceCostReason());

    return await FromHand(context, player, prefs, filter, source);
}
```

### 5.5 单卡升级选择

```csharp
// FromHandForUpgrade 专门用于选择一张牌进行升级
public static async Task<CardModel?> FromHandForUpgrade(...)
{
    // 内部使用 Mode.UpgradeSelect 模式
    result = await NCombatRoom.Instance.Ui.Hand.SelectCards(
        new CardSelectorPrefs(new LocString("gameplay_ui", "CHOOSE_CARD_UPGRADE_HEADER"), 1),
        c => c.IsUpgradable,
        source,
        NPlayerHand.Mode.UpgradeSelect  // 升级模式
    );
}
```

---

## 6. 相关方法对比

| 方法 | 用途 | 数据来源 | 特殊功能 |
|------|------|----------|----------|
| `FromHand` | 从手牌选择 | `PileType.Hand` | 基础方法 |
| `FromHandForDiscard` | 弃牌选择 | `PileType.Hand` | 金色高亮Sly卡牌 |
| `FromHandForUpgrade` | 升级选择 | `PileType.Hand` | 单选+升级预览 |
| `FromSimpleGrid` | 通用选择 | 传入卡牌列表 | 网格布局 |
| `FromDeckForUpgrade` | 牌组升级 | `PileType.Deck` | 只显示可升级 |
| `FromDeckForRemoval` | 移除卡牌 | `PileType.Deck` | 只显示可移除 |
| `FromDeckForEnchantment` | 附魔选择 | `PileType.Deck` | 附魔预览 |

---

## 7. 多人同步机制

### 7.1 本地玩家流程

```csharp
if (ShouldSelectLocalCard(player))
{
    // 打开UI让玩家选择
    result = await NCombatRoom.Instance.Ui.Hand.SelectCards(prefs, filter, source);
    // 同步选择结果给远端
    RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
        player, choiceId, PlayerChoiceResult.FromMutableCombatCards(result));
}
```

### 7.2 远程玩家流程

```csharp
else
{
    // 等待远程玩家的选择结果
    result = (await RunManager.Instance.PlayerChoiceSynchronizer
        .WaitForRemoteChoice(player, choiceId)).AsCombatCards();
}
```

---

## 8. 注意事项

1. **战斗结束检查**：`FromHand` 会在战斗结束时立即返回空列表，避免在战斗结束后还尝试选择卡牌。

2. **取消播放**：进入选择模式前会自动取消当前正在进行的卡牌播放（`CancelAllCardPlay`）。

3. **空列表处理**：如果过滤后的可选项为空，方法会立即返回空列表而不打开UI。

4. **快速返回**：如果 `RequireManualConfirmation = false` 且可选项数量 ≤ `MinSelect`，会直接返回所有选项。

5. **测试模式**：通过 `CardSelectCmd.UseSelector` 可以注入测试用的选择器，绕过UI直接返回结果。

6. **确认按钮**：只有当选择数量在 `[MinSelect, MaxSelect]` 范围内时，确认按钮才会启用。

7. **最大选择自动确认**：当选择的卡牌数量达到 `MaxSelect` 时，会自动完成选择（无需点击确认）。

---

## 9. 文件位置

```
D:\claudeProj\sts2\src\Core\Commands\CardSelectCmd.cs          # 主要命令
D:\claudeProj\sts2\src\Core\CardSelection\CardSelectorPrefs.cs  # 配置结构体
D:\claudeProj\sts2\src\Core\Nodes\Combat\NPlayerHand.cs         # UI实现
```
