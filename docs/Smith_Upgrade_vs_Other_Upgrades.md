# 火堆升级与其他升级的区分分析

## 结论

**不能直接区分**火堆升级和其他地方的升级。游戏本体的 `CardCmd.Upgrade` 方法没有提供升级来源的参数。

---

## 核心代码分析

### 1. CardCmd.Upgrade 方法签名

```csharp
// src/Core/Commands/CardCmd.cs:190-244
public static void Upgrade(CardModel card, CardPreviewStyle style = CardPreviewStyle.HorizontalLayout)
{
    if (CombatManager.Instance.IsEnding) return;

    foreach (CardModel card in cards)
    {
        if (!card.IsUpgradable) continue;

        // 记录到历史（如果是牌组中的卡）
        CardPile pile = card.Pile;
        if (pile != null && pile.Type == PileType.Deck)
        {
            card.Owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(card.Owner.NetId).UpgradedCards.Add(card.Id);
        }

        card.UpgradeInternal();
        card.FinalizeUpgradeInternal();
        // ...
    }
}
```

**关键问题**：方法没有 `source` 或 `context` 参数来标识升级来源。

---

### 2. 火堆升级流程

```csharp
// src/Core/Entities/RestSite/SmithRestSiteOption.cs:57-74
public override async Task<bool> OnSelect()
{
    CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, SmithCount);
    prefs.Cancelable = true;
    prefs.RequireManualConfirmation = true;

    // 1. 选择要升级的卡牌
    _selection = await CardSelectCmd.FromDeckForUpgrade(base.Owner, prefs);
    if (!_selection.Any()) return false;

    // 2. 升级每张选中的卡牌
    foreach (CardModel item in _selection)
    {
        CardCmd.Upgrade(item, CardPreviewStyle.None);
    }

    // 3. 触发火堆锻造后扳机
    await Hook.AfterRestSiteSmith(base.Owner.RunState, base.Owner);
    return true;
}
```

---

### 3. 可用的 Hook

```csharp
// src/Core/Hooks/Hook.cs:762-769
public static async Task AfterRestSiteSmith(IRunState runState, Player player)
{
    foreach (AbstractModel model in runState.IterateHookListeners(null))
    {
        await model.AfterRestSiteSmith(player);
        model.InvokeExecutionFinished();
    }
}
```

**局限性**：
- 只在火堆升级完成后触发
- 不传递升级的卡牌列表（`_selection` 是私有的）
- 只通知"火堆锻造发生了"，不告诉"哪些卡被升级了"

---

### 4. 历史记录

唯一记录升级信息的地方：

```csharp
// CardCmd.cs:210
if (pile.Type == PileType.Deck)
{
    card.Owner.RunState.CurrentMapPointHistoryEntry
        ?.GetEntry(card.Owner.NetId)
        .UpgradedCards.Add(card.Id);  // 只记录卡ID，不记录升级来源
}
```

`UpgradedCards` 是一个简单的 `List<string>`，不包含上下文信息。

---

## 解决方案

### 方案1：Patch SmithRestSiteOption.OnSelect（推荐）

```csharp
[HarmonyPatch(typeof(SmithRestSiteOption), nameof(SmithRestSiteOption.OnSelect))]
public static class SmithRestSiteOption_Patch
{
    public static bool IsSmithUpgrading = false;

    static void Prefix()
    {
        IsSmithUpgrading = true;
    }

    static void Postfix()
    {
        IsSmithUpgrading = false;
    }
}

// 然后在需要检测的地方
if (SmithRestSiteOption_Patch.IsSmithUpgrading)
{
    // 这是火堆升级
}
```

**优点**：简单直接，准确率高
**缺点**：依赖 Harmony Patch，如果其他 Mod 也 Patch 可能有冲突

---

### 方案2：检测当前房间类型

```csharp
// 火堆升级只发生在 RestSiteRoom
if (RunManager.Instance?.CurrentRoom is RestSiteRoom)
{
    // 很可能是火堆升级
}
```

**优点**：不需要 Patch
**缺点**：不精确，火堆房间可能有其他升级途径（如事件）

---

### 方案3：Patch CardCmd.Upgrade 并检测调用栈

```csharp
[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Upgrade), typeof(CardModel), typeof(CardPreviewStyle))]
public static class CardCmd_Upgrade_Patch
{
    static void Prefix(CardModel card)
    {
        // 检查调用栈
        var stackTrace = new System.Diagnostics.StackTrace();
        bool isFromSmith = stackTrace.GetFrames()
            .Any(f => f.GetMethod().DeclaringType == typeof(SmithRestSiteOption));
    }
}
```

**优点**：准确
**缺点**：性能开销大，代码脆弱

---

### 方案4：配合 AfterRestSiteSmith Hook

```csharp
public class YourRelic : RelicModel
{
    private bool _smithUpgradeInProgress = false;

    public override async Task AfterRestSiteSmith(Player player)
    {
        _smithUpgradeInProgress = true;
        // 等待一帧让升级完成
        await Cmd.Wait(0);
        _smithUpgradeInProgress = false;
    }
}
```

**优点**：使用官方 Hook
**缺点**：有竞态条件，不精确

---

## 建议

对于需要区分升级来源的卡牌/遗物效果，**推荐方案1**（Patch `SmithRestSiteOption.OnSelect`）：

1. 在 Prefix 中设置静态标志
2. 在 Postfix 中清除标志
3. 在卡牌升级逻辑中检测该标志

这是 STS2 Mod 开发中处理此类问题的常见模式。

---

## 附录：升级预览（Upgrade Preview）Patch 点

如果需要在升级预览界面显示自定义信息，可以 Patch 以下位置。

### 升级预览类型枚举

```csharp
// src/Core/Entities/Cards/CardUpgradePreviewType.cs
public enum CardUpgradePreviewType
{
    None,
    Deck,   // 牌组中的升级预览（火堆等）
    Combat  // 战斗中的升级预览
}
```

### 1. 战斗中手牌升级预览

**Patch 点**：`NPlayerHand.SelectCardInUpgradeMode`

```csharp
// src/Core/Nodes/Combat/NPlayerHand.cs:726-740
private void SelectCardInUpgradeMode(NHandCardHolder holder)
{
    CardModel model = holder.CardNode.Model;
    if (_selectedCards.Count != 0)
    {
        NCard nCard = NCard.Create(_selectedCards.Last());
        nCard.GlobalPosition = _upgradePreview.SelectedCardPosition;
        DeselectCard(nCard);
    }
    _selectedCards.Add(model);
    _upgradePreviewContainer.Visible = true;
    _upgradePreview.Card = model;  // 触发 NUpgradePreview.Reload()
    RemoveCardHolder(holder);
    RefreshSelectModeConfirmButton();
}
```

**实际设置 Preview 类型的地方**：`NUpgradePreview.Reload()`

```csharp
// src/Core/Nodes/Cards/NUpgradePreview.cs:43-66
private void Reload()
{
    RemoveExistingCards();
    _arrows.Visible = Card != null;
    if (Card != null)
    {
        // 左侧：原始卡牌
        NPreviewCardHolder nPreviewCardHolder = NPreviewCardHolder.Create(NCard.Create(Card), ...);
        _before.AddChildSafely(nPreviewCardHolder);

        // 右侧：升级后的卡牌（克隆并升级）
        CardModel cardModel = Card.CardScope.CloneCard(Card);
        cardModel.UpgradeInternal();
        cardModel.UpgradePreviewType = (!Card.Pile.IsCombatPile)
            ? CardUpgradePreviewType.Deck   // 非战斗牌堆
            : CardUpgradePreviewType.Combat; // 战斗牌堆

        NPreviewCardHolder nPreviewCardHolder2 = NPreviewCardHolder.Create(NCard.Create(cardModel), ...);
        nPreviewCardHolder2.CardNode.ShowUpgradePreview();
    }
}
```

---

### 2. 牌组选择升级预览（火堆）

**Patch 点**：`NDeckUpgradeSelectScreen.OnCardClicked`

```csharp
// src/Core/Nodes/Screens/CardSelection/NDeckUpgradeSelectScreen.cs:142-188
protected override void OnCardClicked(CardModel card)
{
    if (_selectedCards.Add(card))
    {
        _grid.HighlightCard(card);
        if (UseSingleSelection)  // 单选（火堆通常是单选）
        {
            // 显示单个升级预览
            _upgradeSinglePreviewContainer.Visible = true;
            _singlePreview.Card = card;  // 触发 NUpgradePreview.Reload()
            // ...
        }
        else  // 多选
        {
            _upgradeMultiPreviewContainer.Visible = true;
            foreach (CardModel selectedCard in _selectedCards)
            {
                CardModel cardModel = _runState.CloneCard(selectedCard);
                cardModel.UpgradeInternal();
                cardModel.UpgradePreviewType = CardUpgradePreviewType.Deck;  // 明确设置为 Deck
                NCard nCard = NCard.Create(cardModel);
                nCard.ShowUpgradePreview();
            }
        }
    }
}
```

---

### 3. CardModel 的 UpgradePreviewType 属性

```csharp
// src/Core/Models/CardModel.cs:633-648
public CardUpgradePreviewType UpgradePreviewType
{
    get => _upgradePreviewType;
    set
    {
        AssertMutable();
        if (!value.IsPreview() && _upgradePreviewType.IsPreview())
        {
            throw new InvalidOperationException("A card cannot go to from being upgrade preview. Consider making a new card model instead.");
        }
        _upgradePreviewType = value;
    }
}
```

**用途**：
- 在卡牌描述中检测当前是否是升级预览模式
- 用于 `CombatState` 属性的计算：

```csharp
// src/Core/Models/CardModel.cs:799-810
public CombatState? CombatState
{
    get
    {
        CardPile pile = Pile;
        if ((pile != null && pile.IsCombatPile) || UpgradePreviewType == CardUpgradePreviewType.Combat)
        {
            return _owner?.Creature.CombatState;
        }
        return null;
    }
}
```

---

### 推荐的 Preview Patch 方案

如果需要在升级预览时显示自定义信息（如 Karen 的 Shine 值预览）：

```csharp
[HarmonyPatch(typeof(NUpgradePreview), nameof(NUpgradePreview.Reload))]
public static class NUpgradePreview_Patch
{
    static void Postfix(NUpgradePreview __instance)
    {
        if (__instance.Card == null) return;

        // 检测是否是火堆/牌组升级预览
        bool isDeckPreview = !__instance.Card.Pile?.IsCombatPile ?? true;

        // 或者检测克隆卡的 UpgradePreviewType
        // var clonedCard = __instance.GetNode<NUpgradePreview>("%After").GetChildOrNull<NPreviewCardHolder>(0)?.CardNode?.Model;
        // if (clonedCard?.UpgradePreviewType == CardUpgradePreviewType.Deck)
    }
}
```

**关键总结**：
- 战斗中手牌升级：`NPlayerHand` 模式为 `UpgradeSelect`，通过 `NUpgradePreview` 显示
- 火堆/牌组升级：`NDeckUpgradeSelectScreen` 显示，单选用 `NUpgradePreview`，多选直接创建 `NCard`
- 两者都会设置克隆卡的 `UpgradePreviewType` 属性
