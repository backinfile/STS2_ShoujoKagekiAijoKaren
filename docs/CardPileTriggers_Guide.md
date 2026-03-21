# STS2 卡牌牌堆移动触发器完整指南

> 基于游戏源码 v0.99.1 分析，供 Mod 开发参考

---

## 一、PileType 枚举（牌堆类型）

| 值 | 含义 |
|----|------|
| `PileType.Deck` | 牌库（战斗外） |
| `PileType.Draw` | 抽牌堆（战斗中） |
| `PileType.Hand` | 手牌 |
| `PileType.Play` | 打出区（临时，打牌时短暂停留） |
| `PileType.Discard` | 弃牌堆 |
| `PileType.Exhaust` | 消耗牌堆 |
| `PileType.None` | 不在任何牌堆（移出战斗 / 新生成未入堆） |

---

## 二、各牌堆移动触发器详解

### 1. 抽牌（Draw）—— 进入手牌

| Hook 名称 | 方法签名 | 触发时机 |
|-----------|----------|----------|
| `Hook.ShouldDraw` | `bool ShouldDraw(Player player, bool fromHandDraw)` | 每次 Draw **之前**，返回 `false` 可阻止 |
| `Hook.AfterPreventingDraw` | `Task AfterPreventingDraw()` | 抽牌被阻止后 |
| `Hook.BeforeHandDraw` | `Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, CombatState state)` | 回合开始抽手牌前（Early 版先触发） |
| `Hook.BeforeHandDrawLate` | 同上 | 回合开始抽手牌前（Late 版后触发） |
| `Hook.AfterCardDrawnEarly` | `Task AfterCardDrawnEarly(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)` | 每张牌进入手牌后（Early，优先触发） |
| `Hook.AfterCardDrawn` | `Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)` | 每张牌进入手牌后（Normal） |
| `card.InvokeDrawn()` | C# event（无参） | 卡牌自身级别，每次被抽到时 |

> **`fromHandDraw`**：`true` = 回合开始正常抽牌；`false` = 效果触发的额外抽牌。

---

### 2. 打出牌（Play）—— 手牌 → Play 区 → 目标堆

| Hook 名称 | 方法签名 | 触发时机 |
|-----------|----------|----------|
| `Hook.ShouldPlay` | `bool ShouldPlay(CardModel card, AutoPlayType type)` | 打牌之前，返回 `false` 可阻止 |
| `Hook.BeforeCardAutoPlayed` | `Task BeforeCardAutoPlayed(CardModel card, Creature? target, AutoPlayType type)` | 自动打牌（Autoplay）之前 |
| `Hook.BeforeCardPlayed` | `Task BeforeCardPlayed(CardPlay cardPlay)` | 每次打牌效果执行前 |
| `Hook.AfterCardPlayed` | `Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)` | 每次打牌效果执行后（Early） |
| `Hook.AfterCardPlayedLate` | 同上 | 每次打牌效果执行后（Late） |
| `Hook.ModifyCardPlayResultPileTypeAndPosition` | `(PileType, CardPilePosition) Modify(CardModel card, bool isAutoPlay, ResourceInfo res, PileType pile, CardPilePosition pos)` | 修改打完后去向（可改为 Exhaust / 置顶等） |
| `card.Played?.Invoke()` | C# event | 打牌流程最末尾 |

---

### 3. 弃牌（Discard）—— 进入弃牌堆

| Hook 名称 | 方法签名 | 触发时机 |
|-----------|----------|----------|
| `Hook.ShouldFlush` | `bool ShouldFlush(Player player)` | 回合结束弃手牌之前，返回 `false` 可阻止 |
| `Hook.BeforeFlush` | `Task BeforeFlush(PlayerChoiceContext ctx, Player player)` | 回合结束弃手牌前（Early） |
| `Hook.BeforeFlushLate` | 同上 | 回合结束弃手牌前（Late） |
| `Hook.AfterCardDiscarded` | `Task AfterCardDiscarded(PlayerChoiceContext ctx, CardModel card)` | 主动/效果触发的弃牌完成后 |
| `Hook.AfterCardRetained` | `Task AfterCardRetained(CardModel card)` | 回合结束时，有 Retain 的牌**留**在手中时 |

> **注意**：回合结束自动弃手牌（Flush）**不触发** `AfterCardDiscarded`，只触发 `AfterCardChangedPiles`。

---

### 4. 消耗（Exhaust）—— 进入消耗牌堆

| Hook 名称 | 方法签名 | 触发时机 |
|-----------|----------|----------|
| `Hook.ShouldEtherealTrigger` | `bool ShouldEtherealTrigger(CardModel card)` | 判断以太关键词是否触发消耗 |
| `Hook.AfterCardExhausted` | `Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)` | 消耗完成后 |

> **`causedByEthereal`**：`true` = 以太关键词自动消耗；`false` = 效果主动消耗。

---

### 5. 洗牌（Shuffle）—— 弃牌堆 → 抽牌堆

| Hook 名称 | 方法签名 | 触发时机 |
|-----------|----------|----------|
| `Hook.ModifyShuffleOrder` | `void ModifyShuffleOrder(Player player, List<CardModel> cards, bool isInitialShuffle)` | 洗牌前，可修改顺序（同步方法） |
| `Hook.AfterShuffle` | `Task AfterShuffle(PlayerChoiceContext ctx, Player shuffler)` | 全部牌移入抽牌堆后，触发一次 |

> 洗牌时每张牌从弃牌堆移入抽牌堆，**每张**都会触发 `AfterCardChangedPiles(Discard→Draw)`，但只有一次 `AfterShuffle`。

---

### 6. 通用牌堆变更（Universal）

**`Hook.AfterCardChangedPiles`** / **`Hook.AfterCardChangedPilesLate`**

所有 `CardPileCmd.Add()` 调用完成后均会触发，是最广泛的牌堆监听器。

```csharp
Task AfterCardChangedPiles(
    CardModel card,
    PileType oldPileType,   // 移动前的堆（PileType.None = 新生成）
    AbstractModel? source   // 触发移动的来源对象，可为 null
)
```

**覆盖的移动路径**：

| 移动路径 | 来源场景 |
|---------|---------|
| None → Draw/Hand/Discard/Exhaust | 新生成的战斗牌进入某堆 |
| Draw → Hand | 抽牌 |
| Hand → Play | 手动打牌 |
| Play / Hand → Discard | 打牌后弃牌 / 主动弃牌 |
| 任意 → Exhaust | 消耗 |
| Discard → Draw | 洗牌（每张单独触发） |
| 任意 → 任意 | 技能/遗物效果移牌 |

---

### 7. 战斗外进出（进入/离开战斗）

| Hook 名称 | 方法签名 | 触发时机 |
|-----------|----------|----------|
| `Hook.AfterCardEnteredCombat` | `Task AfterCardEnteredCombat(CardModel card)` | 牌首次被加入战斗某牌堆（`oldPile == null`） |
| `Hook.AfterCardGeneratedForCombat` | `Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)` | 战斗中生成新牌时 |
| `Hook.BeforeCardRemoved` | `Task BeforeCardRemoved(CardModel card)` | 从牌库永久删除牌之前 |

---

## 三、各场景触发时序

### 场景 A：回合开始抽牌

```
Hook.BeforeHandDraw（Early → Late）
└─ 对每张抽到的牌：
    CardPileCmd.Add(card, Hand)
    → Hook.AfterCardChangedPiles（Draw → Hand）
    → Hook.AfterCardDrawnEarly
    → Hook.AfterCardDrawn
    → card.InvokeDrawn()
```

### 场景 B：手动打出一张牌

```
Hook.ShouldPlay
CardPileCmd.AddDuringManualCardPlay（Hand → Play）
  → Hook.AfterCardChangedPiles（Hand → Play）
Hook.BeforeCardPlayed
CardModel.OnPlay（卡牌效果）
Hook.AfterCardPlayed → AfterCardPlayedLate
// 打完后根据 ResultPile：
  → 弃牌：CardPileCmd.Add(Discard) → Hook.AfterCardChangedPiles（Play → Discard）
  → 消耗：CardCmd.Exhaust → Hook.AfterCardChangedPiles + Hook.AfterCardExhausted
card.Played?.Invoke()
```

### 场景 C：主动弃牌（CardCmd.Discard）

```
CardPileCmd.Add(card, Discard)
  → Hook.AfterCardChangedPiles（Hand → Discard）
Hook.AfterCardDiscarded
```

### 场景 D：消耗（CardCmd.Exhaust）

```
CardPileCmd.Add(card, Exhaust)
  → Hook.AfterCardChangedPiles（旧堆 → Exhaust）
Hook.AfterCardExhausted（causedByEthereal: true/false）
```

### 场景 E：回合结束弃手牌（Flush）

```
Hook.ShouldFlush
Hook.BeforeFlush → BeforeFlushLate
└─ 对无 Retain 的手牌：
    CardPileCmd.Add(card, Discard)
    → Hook.AfterCardChangedPiles（Hand → Discard）
    // 注意：不触发 AfterCardDiscarded
└─ 对有 Retain 的手牌：
    Hook.AfterCardRetained
```

### 场景 F：洗牌（Shuffle）

```
Hook.ModifyShuffleOrder（同步修改顺序）
└─ 对弃牌堆每张牌：
    CardPileCmd.Add(card, Draw)
    → Hook.AfterCardChangedPiles（Discard → Draw）
Hook.AfterShuffle（整体结束后触发一次）
```

---

## 四、Mod 开发要点

### 在 PowerModel / RelicModel 中 override

```csharp
// 监听消耗（适合 Shine 耗尽检查）
public override async Task AfterCardExhausted(
    PlayerChoiceContext choiceContext,
    CardModel card,
    bool causedByEthereal)
{
    // 只处理属于本玩家的牌
    if (card.Owner?.Creature != Owner) return;
    // ...
}

// 监听所有牌堆变化
public override async Task AfterCardChangedPiles(
    CardModel card,
    PileType oldPileType,
    AbstractModel? source)
{
    if (card.Pile?.Type == PileType.Discard && oldPileType == PileType.Hand)
    {
        // 手牌 → 弃牌堆
    }
}
```

### 关键区分

| 场景 | 推荐 Hook |
|------|-----------|
| 监听任意牌堆变化 | `AfterCardChangedPiles` |
| 监听抽牌（含额外信息） | `AfterCardDrawn`（有 `fromHandDraw` 参数） |
| 监听主动弃牌（不含回合结束） | `AfterCardDiscarded` |
| 监听回合结束弃手牌 | `AfterCardChangedPiles`（oldPile=Hand, newPile=Discard） |
| 监听消耗（含以太区分） | `AfterCardExhausted`（有 `causedByEthereal` 参数） |
| 修改打完后去向 | `ModifyCardPlayResultPileTypeAndPosition` |
| 阻止抽/弃/以太触发 | `ShouldDraw` / `ShouldFlush` / `ShouldEtherealTrigger` |

### Shine 系统建议

Shine 归零时将牌从战斗中移除，推荐使用 `AfterCardPlayed` 之后检查 Shine 值，而非依赖消耗触发器（因为 Shine 耗尽是主动从战斗移除，不走标准 Exhaust 流程）。
