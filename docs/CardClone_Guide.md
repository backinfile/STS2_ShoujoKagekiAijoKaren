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

## 两者对比

| | `CreateClone()` | `owner.RunState.CloneCard()` |
|---|---|---|
| 底层实现 | 相同（`ClonePreservingMutability` + `AddCard`） | 相同 |
| 适用场景 | 战斗中 | 战斗外（营地等） |
| 卡在 Deck 时调用 | 抛异常 | 正常 |
| `_cloneOf` 追踪 | 设置 | 不设置 |
| Scope 选择 | 自动 | 强制 RunState |

---

## 克隆后的深拷贝内容

`ClonePreservingMutability()` 会深拷贝以下字段：

| 字段 | 处理方式 |
|---|---|
| `_keywords` | HashSet 深拷贝 |
| `_dynamicVars` | `DynamicVars.Clone(this)` |
| `_energyCost` | `_energyCost?.Clone(this)` |
| `_temporaryStarCosts` | `ToList()` 新列表 |
| `Enchantment` / `Affliction` | 各自 `ClonePreservingMutability()` |

**不会**自动复制：`SpireField` 数据（如 Shine 值）——这是外部 Dictionary，需手动同步。

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
