# 卡牌复制（Clone）完整指南

> 基于 v0.99.1 反编译代码

---

## 核心 API

### `CardModel.CreateClone()`

战斗中复制卡牌的标准方法。

```csharp
public CardModel CreateClone()
```

- **前提**：卡牌必须在战斗牌堆中（Hand/Draw/Discard/Play/Exhaust），或 `Pile == null`
- 若卡在非战斗牌堆（如 `PileType.Deck`）会抛 `InvalidOperationException`
- 自动设置 `clone._cloneOf = this`（追踪克隆来源）
- 自动选择 Scope：战斗中→`CombatState`，否则→`RunState`
- **自动复制升级状态和附魔**（见下方详细说明）

### `CardModel.CreateDupe()`

用于自动打出的临时副本（如 `HistoryCourse` 遗物）。

```csharp
public CardModel CreateDupe()
```

- 在 `CreateClone()` 基础上设 `IsDupe = true`
- **自动移除 `CardKeyword.Exhaust`**
- 若对副本调用 `CreateDupe()`，会追溯到原卡再克隆

### `RunState.CloneCard(CardModel mutableCard)`

战斗外（营地等）复制卡牌，强制在 RunState 作用域下注册。

```csharp
public CardModel CloneCard(CardModel mutableCard)
```

- 无校验（可用于 `PileType.Deck` 中的卡）
- 不设置 `_cloneOf`
- 克隆卡的 `Owner` 通过 `MemberwiseClone` 自动继承原卡

---

## 底层机制：`ClonePreservingMutability`

`CreateClone()`和`RunState.CloneCard()`底层都使用`AbstractModel.ClonePreservingMutability()`方法：

```csharp
public AbstractModel ClonePreservingMutability()
{
    if (!IsMutable)
    {
        return this;  // 不可变对象直接返回自身
    }
    return MutableClone();
}
```

### 关键特性

| 特性 | 行为 |
|------|------|
| **不可变对象** (`IsCanonical`) | 直接返回自身引用（无需克隆，线程安全） |
| **可变对象** (`IsMutable`) | 调用`MutableClone()`进行完整深拷贝 |

### `MutableClone()`流程

```csharp
public AbstractModel MutableClone()
{
    // 1. 浅拷贝
    AbstractModel clone = (AbstractModel)MemberwiseClone();

    // 2. 标记为可变
    clone.IsMutable = true;

    // 3. 深拷贝特定字段（子类实现）
    clone.DeepCloneFields();

    // 4. 清理事件监听
    clone.AfterCloned();

    return clone;
}
```

### 为什么需要这个设计

1. **性能优化**：不可变对象（如基础CardModel定义）无需复制，直接共享引用
2. **状态安全**：可变对象（战斗中的卡牌实例）必须深拷贝，避免修改互相影响
3. **一致性**：Enchantment/Affliction等复杂对象通过`DeepCloneFields()`正确复制

---

## 两者对比

| | `CreateClone()` | `owner.RunState.CloneCard()` |
|---|---|---|
| 底层实现 | 相同（`ClonePreservingMutability` + `AddCard`） | 相同 |
| 适用场景 | 战斗中 | 战斗外（营地等） |
| 卡在 Deck 时调用 | 抛异常 | 正常 |
| `_cloneOf` 追踪 | 设置 | 不设置 |
| Scope 选择 | 自动 | 强制 RunState |

---

## `CreateClone()` 复制内容详解

`CreateClone()` 内部调用 `CardScope.CloneCard(this)`，会完整复制以下状态：

| 属性 | 复制方式 | 说明 |
|------|----------|------|
| `CurrentUpgradeLevel` | `MemberwiseClone` 浅拷贝 | 升级等级完全保留（如 +2 卡牌克隆后仍是 +2） |
| `Enchantment` | `ClonePreservingMutability()` + `EnchantInternal()` | 附魔状态和数值完整复制 |
| `Affliction` | `ClonePreservingMutability()` + `AfflictInternal()` | 诅咒状态和数值完整复制 |
| `Keywords` | HashSet 深拷贝 | 所有关键词保留 |
| `DynamicVars` | `DynamicVars.Clone(this)` | 动态变量（伤害/格挡等）完整复制 |
| `EnergyCost` | `_energyCost?.Clone(this)` | 费用状态保留 |
| `_temporaryStarCosts` | `ToList()` 新列表 | 临时星耗保留 |

### 代码实现参考

来自 `CardModel.DeepCloneFields()`（第910-933行）：

```csharp
protected override void DeepCloneFields()
{
    // Keywords / DynamicVars / EnergyCost / StarCosts 深拷贝
    _keywords = new HashSet<CardKeyword>(Keywords);
    _dynamicVars = DynamicVars.Clone(this);
    _energyCost = _energyCost?.Clone(this);
    _temporaryStarCosts = _temporaryStarCosts.ToList();

    // Enchantment 深拷贝并重新绑定
    if (Enchantment != null)
    {
        EnchantmentModel enchantmentModel = (EnchantmentModel)Enchantment.ClonePreservingMutability();
        Enchantment = null;
        EnchantInternal(enchantmentModel, enchantmentModel.Amount);
    }

    // Affliction 深拷贝并重新绑定
    if (Affliction != null)
    {
        AfflictionModel afflictionModel = (AfflictionModel)Affliction.ClonePreservingMutability();
        Affliction = null;
        AfflictInternal(afflictionModel, afflictionModel.Amount);
    }
}
```

### 使用示例：复制带升级和附魔的卡牌

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    // 选择手牌中的一张卡（可能是升级的，可能有附魔）
    CardModel selection = (await CardSelectCmd.FromHand(
        prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
        context: choiceContext,
        player: Owner
    )).FirstOrDefault();

    if (selection != null)
    {
        // CreateClone 会自动复制：
        // - 升级状态（如打击+2）
        // - 附魔（如强化+3）
        // - 动态变量（伤害/格挡数值）
        CardModel clone = selection.CreateClone();

        // 加入手牌
        await CardPileCmd.AddGeneratedCardToCombat(clone, PileType.Hand, addedByPlayer: true);
    }
}
```

---

## 加入战斗牌堆（战斗中）

```csharp
// 复制到手牌
CardModel clone = originalCard.CreateClone();
await CardPileCmd.AddGeneratedCardToCombat(clone, PileType.Hand, addedByPlayer: true);

// 复制到抽牌堆顶
CardModel clone = originalCard.CreateClone();
await CardPileCmd.AddGeneratedCardToCombat(clone, PileType.Draw, addedByPlayer: true,
    position: CardPilePosition.Top);
```

---

## 加入牌组（战斗外 / 永久）

来自 `CloneRestSiteOption.cs` 的官方写法：

```csharp
// 1. 克隆（Owner 自动继承）
CardModel clone = owner.RunState.CloneCard(originalCard);

// 2. 加入牌组
CardPileAddResult result = await CardPileCmd.Add(clone, PileType.Deck);

// 3. 展示预览动画（fire-and-forget，不需要 await）
CardCmd.PreviewCardPileAdd(result, 1.2f, CardPreviewStyle.MessyLayout);
```

战斗中效果要永久复制卡牌进牌组（跨战斗保留），同样用此模式：

```csharp
// 在 OnPlay 中
CardModel clone = Owner.RunState.CloneCard(this);
CardPileAddResult result = await CardPileCmd.Add(clone, PileType.Deck);
CardCmd.PreviewCardPileAdd(result, 1.2f, CardPreviewStyle.MessyLayout);
```

---

## 丢弃不需要的克隆

克隆后如果决定不使用，根据当前所在牌堆分三种情况处理：

| 克隆卡当前状态 | 处理方法 |
|---|---|
| 未加入任何堆（`Pile == null`） | `clone.RemoveFromState()` |
| 在战斗牌堆（Hand/Draw/Discard/Exhaust） | `await CardPileCmd.RemoveFromCombat(clone)` |
| 在牌组（`PileType.Deck`） | `await CardPileCmd.RemoveFromDeck(clone)` |

**注意：三种方法不能混用**，`RemoveFromCombat` 和 `RemoveFromDeck` 开头都会检查 `Pile` 类型，不符合则抛异常。

### 情况一：未加入任何堆

```csharp
clone.RemoveFromState();
// 内部：RemoveFromCurrentPile()（Pile==null 时 no-op）
//       + HasBeenRemovedFromState = true（停止接收 Hook）
```

### 情况二：已加入战斗牌堆

```csharp
await CardPileCmd.RemoveFromCombat(clone);              // 播放消除动画 + 触发 Hook
await CardPileCmd.RemoveFromCombat(clone, skipVisuals: true);  // 无动画版
```

### 情况三：已加入牌组

```csharp
await CardPileCmd.RemoveFromDeck(clone);                // 播放消除动画 + 触发 BeforeCardRemoved
await CardPileCmd.RemoveFromDeck(clone, showPreview: false);   // 无动画版
```

---

## 动画说明

`CardPileCmd.Add` **不自动播放**"展示给玩家"的动画。需要手动调用：

```csharp
// 单张
CardCmd.PreviewCardPileAdd(CardPileAddResult result, float time = 1.2f, CardPreviewStyle style)

// 多张
CardCmd.PreviewCardPileAdd(IReadOnlyList<CardPileAddResult> results, float time, CardPreviewStyle style)
```

### `CardPreviewStyle` 可选值

| 值 | 效果 | 适用场景 |
|---|---|---|
| `HorizontalLayout` | 横排（默认） | 少量卡牌（≤5张） |
| `MessyLayout` | 散乱堆叠 | 营地克隆，多张 |
| `GridLayout` | 网格 | 大量卡牌 |
| `EventLayout` | 事件界面布局 | 事件奖励 |
| `None` | 不显示 | 后台操作 |

---

## Karen Mod 特殊注意

### Shine 值不会自动复制

如需克隆卡携带 Shine 值，需手动同步：

```csharp
CardModel clone = originalCard.CreateClone(); // 或 RunState.CloneCard
int shine = ShinePileManager.GetShine(originalCard);
if (shine > 0) ShinePileManager.SetShine(clone, shine);
```

### 复制进牌组的卡无需加入 KarenCardPool

`CardPileCmd.Add(PileType.Deck)` 直接操作 `RunState._allCards`，不经过 `ModelDb.AllCards`，不会触发 Pool 崩溃。只有 **初始卡组（StartingDeck）** 和 **卡池奖励** 才需要同时加入 `KarenCardPool.GenerateAllCards()`。

---

## 游戏内使用复制机制的卡/遗物

| 类 | 复制方式 | 目标堆 |
|---|---|---|
| `Anger` | `CreateClone()` | Discard |
| `DualWield` | `CreateClone()` | Hand |
| `NightmarePower` | `CreateClone()` | Hand（每回合开始） |
| `JugglingPower` | `CreateClone()` | Hand（第3张攻击后） |
| `BurningSticks`（遗物） | `CreateClone()` | Hand |
| `HistoryCourse`（遗物） | `CreateDupe()` + `AutoPlay` | 自动打出 |
| `CloneRestSiteOption`（营地） | `RunState.CloneCard()` | Deck |
| `EggRelicHelper` | `RunState.CloneCard()` + `Upgrade()` | Deck |
