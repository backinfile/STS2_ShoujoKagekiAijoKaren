# CardPileCmd.Add 方法分析

## 文件位置
`D:\claudeProj\sts2\src\Core\Commands\CardPileCmd.cs`

## 方法概述
`CardPileCmd.Add` 是将卡牌添加到指定牌堆的核心命令。它处理卡牌从一个牌堆移动到另一个牌堆的完整流程，包括状态校验、视觉动画和事件触发。

---

## 方法签名

```csharp
// 基础重载
public static async Task<CardPileAddResult> Add(
    CardModel card,
    PileType newPileType,
    CardPilePosition position = CardPilePosition.Bottom,
    AbstractModel? source = null,
    bool skipVisuals = false)

// CardPile 版本
public static async Task<CardPileAddResult> Add(
    CardModel card,
    CardPile newPile,
    ...)

// 批量版本
public static async Task<IReadOnlyList<CardPileAddResult>> Add(
    IEnumerable<CardModel> cards,
    CardPile newPile,
    CardPilePosition position = CardPilePosition.Bottom,
    AbstractModel? source = null,
    bool skipVisuals = false)
```

### 参数说明

| 参数 | 类型 | 说明 |
|------|------|------|
| `card(s)` | `CardModel` / `IEnumerable<CardModel>` | 要添加的卡牌（单张或批量） |
| `newPileType` | `PileType` | 目标牌堆类型枚举 |
| `newPile` | `CardPile` | 目标牌堆实例 |
| `position` | `CardPilePosition` | 插入位置：`Bottom`(-1), `Top`(0), `Random` |
| `source` | `AbstractModel?` | 触发此操作的来源模型（用于事件追踪） |
| `skipVisuals` | `bool` | 是否跳过视觉动画 |

### 返回值
`CardPileAddResult` 包含：
- `success` - 是否成功添加
- `cardAdded` - 实际添加的卡牌（可能被 Hook 修改）
- `oldPile` - 原始牌堆
- `modifyingModels` - 修改此卡牌的模型列表

---

## 执行流程图

```
Add(cards, newPile, ...)
    │
    ▼
[前置校验阶段]
    ├── 检查卡牌是否有 Owner
    ├── 检查战斗状态（战斗结束则返回失败）
    ├── 检查每张卡牌的有效性：
    │   ├── IsInCombat && CombatState == null || IsDead → 失败
    │   ├── HasBeenRemovedFromState → 抛异常
    │   ├── 目标为 Deck 但不在 RunState → 抛异常
    │   ├── 目标为战斗堆但不在 CombatState → 抛异常
    │   └── UpgradePreviewType.IsPreview() → 抛异常
    │
    ▼
[Deck 特殊处理]
    └── 如果是添加到牌组：
        ├── 调用 Hook.ShouldAddToDeck
        ├── 若被阻止：执行 preventer.AfterAddToDeckPrevented
        └── 若成功：记录到历史 (CardsGained), 设置 FloorAddedToDeck
    │
    ▼
[准备阶段]
    ├── 确定 owningPlayer
    ├── 检查所有卡牌是否属于同一玩家
    └── 检查目标牌堆是否为战斗堆且战斗进行中
    │
    ▼
[批量处理每张卡牌]
    │
    ├── 检查手牌上限（满10张则重定向到 Discard）
    ├── 确定是否需要创建 NCard 节点
    ├── 调用 RemoveFromCurrentPile() 从旧堆移除
    ├── 调用 targetPile.AddInternal() 添加到新堆
    └── 触发 Hook.AfterCardEnteredCombat（如果是新进入战斗）
    │
    ▼
[视觉动画阶段]
    ├── 为需要动画的卡牌创建 Tween
    ├── 根据目标牌堆类型执行不同动画：
    │   ├── Exhaust → 变灰 + 消失特效
    │   ├── Hand → 插值移动到 Hand 区域
    │   ├── Play → 插值移动到 Play 区域
    │   └── Draw/Discard/Deck → 飞牌特效 (NCardFlyVfx / NCardFlyShuffleVfx)
    └── 播放 Tween 并等待完成
    │
    ▼
[事件触发阶段]
    └── 对每张成功添加的卡牌：
        └── 触发 Hook.AfterCardChangedPiles(runState, combatState, card, oldPile.Type, source)
```

---

## 关键实现细节

### 1. 手牌上限处理

```csharp
// 第362-364行：手牌满时重定向到弃牌堆
bool isFullHandAdd = targetPile.Cards.Count >= 10;
if (isFullHandAdd)
{
    targetPile = CardPile.Get(PileType.Discard, card.Owner);
}
```

### 2. 插入位置处理

```csharp
// 第428-434行
int insertIndex = position switch
{
    CardPilePosition.Bottom => -1,  // 列表末尾
    CardPilePosition.Top => 0,      // 列表开头
    CardPilePosition.Random => rng.Next(targetPile.Cards.Count + 1),
    _ => throw new ArgumentOutOfRangeException()
};
targetPile.AddInternal(card2, insertIndex);
```

### 3. 视觉动画逻辑

| 目标牌堆 | 动画效果 |
|---------|---------|
| `Exhaust` | 先移动到 Play 区域 → 等待 → 播放 Exhaust 特效 → 变灰色 → 销毁 |
| `Hand` | 插值移动到 Hand 区域 → 执行 `handNode.Add(cardNode2)` |
| `Play` | 插值移动到 Play 容器 → 缩放至 0.8f |
| `Draw/Discard` | `NCardFlyShuffleVfx` 飞牌效果 |
| `Deck` | `NCardFlyVfx` 飞向顶部栏轨迹容器 |

### 4. 本地玩家判断

```csharp
bool owningPlayerIsLocal = LocalContext.IsMe(owningPlayer);

// 非本地玩家的卡牌动画处理（第486-498行）
if (!owningPlayerIsLocal && targetPile.Type != PileType.Play)
{
    // 移动到抽牌堆/弃牌堆/牌组时：向下移动25px，变灰，然后销毁
    tween.Parallel().TweenProperty(cardNode2, "position", cardNode2.Position + Vector2.Down * 25f, ...);
    tween.Parallel().TweenProperty(cardNode2, "modulate", StsColors.exhaustGray, ...);
    tween.Chain().TweenCallback(Callable.From(cardNode2.QueueFreeSafely));
}
```

---

## 重要校验规则

| 校验条件 | 失败结果 |
|---------|---------|
| `card.Owner == null` | 抛 `InvalidOperationException` |
| 战斗已结束 + 目标为战斗堆 | 返回 `success = false` |
| `HasBeenRemovedFromState` | 抛异常（提示需先加回 State） |
| 添加到 Deck 但不在 RunState | 抛异常 |
| 添加到战斗堆但不在 CombatState | 抛异常 |
| `UpgradePreviewType.IsPreview()` | 抛异常（预览卡不能加入牌堆） |
| 批量添加时卡牌属于不同 Owner | 抛异常 |

---

## 相关 Hook 事件

```csharp
// 添加到牌组前拦截
Hook.ShouldAddToDeck(runState, card, out AbstractModel preventer)

// 添加被阻止后回调
preventer.AfterAddToDeckPrevented(card)

// 卡牌首次进入战斗（oldPile == null 且目标为战斗堆）
Hook.AfterCardEnteredCombat(combatState, card)

// 卡牌移动完成后（重要：所有移动都会触发）
Hook.AfterCardChangedPiles(runState, combatState, card, oldPileType, source)
```

---

## 与约定牌堆系统的关联

在 Karen Mod 中，`PromisePileCmd` 调用了 `CardPileCmd.Add`：

```csharp
// PromisePileCmd.Draw 方法
public static async Task Draw(...)
{
    // ... 从约定牌堆取出卡牌 ...

    // 使用 CardPileCmd.Add 添加到手牌
    await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom, null, skipVisuals: true);

    // 手动触发 GlobalMoveSystem 事件
    await GlobalMoveSystem.OnCardMoved?.Invoke(card, PromisePile, PileType.Hand, source);
}
```

**注意**：约定牌堆是虚拟牌堆，不使用标准的 Add 流程，而是手动管理并触发相关事件。

---

## 性能优化点

1. **FastMode 支持**：所有动画时长根据 `SaveManager.Instance.PrefsSave.FastMode` 调整
   - `Instant`: 0.01f
   - `Fast`: 0.2f
   - 默认: 0.3f-0.5f

2. **批量处理**：单张卡牌也包装为列表统一处理，减少代码重复

3. **延迟播放**：Shuffle 时使用 `waitTimeAccumulator` 优化帧率

---

## 代码行号参考

| 功能 | 行号 |
|------|------|
| 主要 Add 方法（批量） | 231-583 |
| 单张卡牌重载 | 217-220 |
| PileType 重载 | 208-215 |
| AddGeneratedCardToCombat | 172-206 |
| 视觉动画逻辑 | 473-573 |
| Hook.AfterCardChangedPiles 触发 | 574-581 |
| Draw 方法 | 717-777 |
| Shuffle 方法 | 779-834 |

---

*分析日期：2026-03-29*
*游戏版本：Slay the Spire 2 v0.99.1*
