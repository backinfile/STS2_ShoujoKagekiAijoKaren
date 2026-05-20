# STS2 能力系统开发指南

## 概述

Power（能力）是《Slay the Spire 2》中影响生物状态的 Buff/Debuff 机制，包含 271 种能力。所有能力继承自 `PowerModel`，位于 `MegaCrit.Sts2.Core.Models`。

---

## 基础架构

### 核心枚举

**PowerType（能力类型）** - `src/Core/Entities/Powers/PowerType.cs`
```csharp
public enum PowerType
{
    None,   // 无
    Buff,   // 增益
    Debuff  // 减益
}
```

**PowerStackType（叠加类型）** - `src/Core/Entities/Powers/PowerStackType.cs`
```csharp
public enum PowerStackType
{
    None,    // 不可叠加（如 Artifact）
    Counter, // 数值叠加（默认）
    Single   // 单一层数
}
```

---

## PowerModel 核心属性

### 抽象/虚属性（必须/可选覆盖）

| 属性 | 类型 | 说明 |
|------|------|------|
| `Type` | `PowerType` | **必须**，Buff 或 Debuff |
| `StackType` | `PowerStackType` | **必须**，叠加方式 |
| `AllowNegative` | `bool` | 是否允许负值（默认 false） |
| `IsInstanced` | `bool` | 是否每次独立实例（默认 false） |
| `Amount` | `int` | 当前数值（叠加值） |
| `DisplayAmount` | `int` | 显示数值（可覆盖） |
| `AmountLabelColor` | `Color` | 数值标签颜色 |
| `ShouldScaleInMultiplayer` | `bool` | 多人游戏是否缩放 |

### 只读属性

| 属性 | 说明 |
|------|------|
| `Title` | 本地化标题（`powers/{Id}.title`） |
| `Description` | 本地化描述（`powers/{Id}.description`） |
| `SmartDescription` | 智能描述（支持变量） |
| `IconPath` | 图标路径（自动） |
| `BigIcon` | 大图标（自动） |
| `Owner` | 拥有者生物 |
| `CombatState` | 当前战斗状态 |
| `Applier` | 施加者生物 |
| `Target` | 目标生物 |
| `DynamicVars` | 动态变量集合 |

---

## 常用 Hook 方法（按分类）

### 战斗生命周期

```csharp
// 战斗开始
public virtual Task BeforeCombatStart()
public virtual Task BeforeCombatStartLate()

// 回合相关
public virtual Task BeforeSideTurnStart(PlayerChoiceContext ctx, CombatSide side, CombatState state)
public virtual Task AfterSideTurnStart(CombatSide side, CombatState state)
public virtual Task BeforeTurnEndVeryEarly(PlayerChoiceContext ctx, CombatSide side)
public virtual Task BeforeTurnEndEarly(PlayerChoiceContext ctx, CombatSide side)
public virtual Task BeforeTurnEnd(PlayerChoiceContext ctx, CombatSide side)
public virtual Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)

// 战斗结束
public virtual Task AfterCombatEnd(CombatRoom room)
public virtual Task AfterCombatVictory(CombatRoom room)
public virtual Task AfterCombatVictoryEarly(CombatRoom room)
```

### 卡牌相关

```csharp
public virtual Task BeforeCardPlayed(CardPlay cardPlay)
public virtual Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
public virtual Task AfterCardPlayedLate(PlayerChoiceContext ctx, CardPlay cardPlay)

public virtual Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
public virtual Task AfterCardDrawnEarly(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)

public virtual Task AfterCardDiscarded(PlayerChoiceContext ctx, CardModel card)
public virtual Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)
public virtual Task AfterCardRetained(CardModel card)
```

### 伤害与格挡

```csharp
public virtual Task BeforeAttack(AttackCommand command)
public virtual Task AfterAttack(AttackCommand command)

public virtual Task BeforeDamageReceived(PlayerChoiceContext ctx, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
public virtual Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
public virtual Task AfterDamageGiven(PlayerChoiceContext ctx, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)

public virtual Task BeforeBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
public virtual Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
public virtual Task AfterBlockBroken(Creature creature)
public virtual Task AfterBlockCleared(Creature creature)
```

### 能力相关

```csharp
public virtual Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
public virtual Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)

public virtual Task AfterModifyingPowerAmountReceived(PowerModel power)
public virtual Task AfterModifyingPowerAmountGiven(PowerModel power)
```

### 其他常用 Hook

```csharp
public virtual Task BeforePotionUsed(PotionModel potion, Creature? target)
public virtual Task AfterPotionUsed(PotionModel potion, Creature? target)

public virtual Task AfterGoldGained(Player player)
public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta)

public virtual Task AfterCreatureAddedToCombat(Creature creature)
public virtual Task BeforeDeath(Creature creature)
public virtual Task AfterDeath(PlayerChoiceContext ctx, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
```

### 数值修改器（Modify 系列）

```csharp
// 伤害修改
public virtual decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)

// 格挡修改
public virtual decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
public virtual decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)

// 其他修改
public virtual int ModifyAttackHitCount(AttackCommand attack, int hitCount)
public virtual int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
```

---

## PowerCmd 命令类

位置：`src/Core/Commands/PowerCmd.cs`

### 主要方法

```csharp
// 施加能力（泛型）
public static async Task<T?> Apply<T>(Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false) where T : PowerModel

// 施加能力（实例）
public static async Task Apply(PowerModel power, Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false)

// 施加给多个目标
public static async Task<IReadOnlyList<T>> Apply<T>(IEnumerable<Creature> targets, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false) where T : PowerModel

// 修改数值
public static async Task<int> ModifyAmount(PowerModel power, decimal offset, Creature? applier, CardModel? cardSource, bool silent = false)

// 设置数值
public static async Task<T?> SetAmount<T>(Creature target, decimal amount, Creature? applier, CardModel? cardSource) where T : PowerModel

// 移除能力
public static async Task Remove(PowerModel? power)
public static async Task Remove<T>(Creature creature) where T : PowerModel

// 减少1层（用于持续回合）
public static async Task Decrement(PowerModel power)

// 持续回合递减
public static async Task TickDownDuration(PowerModel power)
```

---

## 完整示例

### 示例1：力量（StrengthPower）- 简单伤害加成

```csharp
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class StrengthPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => true;  // 允许负值（力量降低）

    // 伤害加成：每次攻击增加 Amount 点伤害
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 必须是自己攻击，且是力量攻击
        if (base.Owner != dealer)
            return 0m;
        if (!props.IsPoweredAttack())
            return 0m;
        return base.Amount;
    }
}
```

### 示例2：中毒（PoisonPower）- 回合触发伤害

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class PoisonPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

    // 计算触发次数（受加速器影响）
    private int TriggerCount
    {
        get
        {
            IEnumerable<Creature> source = from c in base.Owner.CombatState.GetOpponentsOf(base.Owner)
                where c.IsAlive
                select c;
            return Math.Min(base.Amount, 1 + source.Sum((Creature a) => a.GetPowerAmount<AccelerantPower>()));
        }
    }

    // 回合开始时触发伤害
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != base.Owner.Side)
            return;

        int iterations = TriggerCount;
        for (int i = 0; i < iterations; i++)
        {
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), base.Owner, base.Amount, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
            if (base.Owner.IsAlive)
            {
                await PowerCmd.Decrement(this);  // 伤害后递减
            }
        }
    }
}
```

### 示例3：残影（AfterimagePower）- 使用内部数据

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class AfterimagePower : PowerModel
{
    // 内部数据类存储状态
    private class Data
    {
        public readonly Dictionary<CardModel, int> amountsForPlayedCards = new();
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 显示格挡悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new global::_003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.Static(StaticHoverTip.Block));

    // 初始化内部数据
    protected override object InitInternalData() => new Data();

    // 打牌前记录当前层数
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != base.Owner)
            return Task.CompletedTask;
        GetInternalData<Data>().amountsForPlayedCards.Add(cardPlay.Card, base.Amount);
        return Task.CompletedTask;
    }

    // 打牌后获得格挡
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == base.Owner &&
            GetInternalData<Data>().amountsForPlayedCards.Remove(cardPlay.Card, out var value) &&
            value > 0)
        {
            await CreatureCmd.GainBlock(base.Owner, value, ValueProp.Unpowered, null, fast: true);
        }
    }
}
```

### 示例4：约定牌堆计数（DisplayAmount 覆盖）- 数值为0不消失

```csharp
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace YourMod.Powers;

/// <summary>
/// 显示约定牌堆卡牌数量，数值可以为0但 Power 持续存在
/// </summary>
public sealed class PromisePileCounterPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    // Single 类型：Amount 始终为 1，不会自动消失
    public override PowerStackType StackType => PowerStackType.Single;

    // 内部数据存储真实数值
    private class Data
    {
        public int RealCount;
    }

    protected override object InitInternalData() => new Data();

    // 覆盖显示数值，UI 显示的是这个值
    public override int DisplayAmount => GetInternalData<Data>().RealCount;

    // 外部调用更新数值
    public void SetRealCount(int count)
    {
        GetInternalData<Data>().RealCount = count;
        base.RefreshDescription();  // 刷新 UI
    }
}
```

**使用方式：**
```csharp
// 应用 Power（Amount=1 确保存在）
await PowerCmd.Apply<PromisePileCounterPower>(creature, 1, creature, null);

// 更新显示数值（可以为0）
if (creature.HasPower<PromisePileCounterPower>(out var power)
    && power is PromisePileCounterPower counter)
{
    counter.SetRealCount(0);  // 显示 0，但 Power 不会消失
}
```

---

## 创建自定义能力步骤

### 1. 创建能力类

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace YourMod.Powers;

public sealed class MyCustomPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
```

### 2. 添加本地化

**英文** `localization/eng/powers.json`:
```json
{
    "MyCustomPower.title": "My Power",
    "MyCustomPower.description": "Gain {Block} [gold]Block[/gold].",
    "MyCustomPower.smartDescription": "Gain {Block} [gold]Block[/gold]."
}
```

**中文** `localization/zhs/powers.json`:
```json
{
    "MyCustomPower.title": "我的能力",
    "MyCustomPower.description": "获得{Block}点[gold]格挡[/gold]。",
    "MyCustomPower.smartDescription": "获得{Block}点[gold]格挡[/gold]。"
}
```

### 3. 添加图标

- 图标路径：`images/powers/mycustompower.png`（小图标，用于图集）
- 大图标：`images/powers/mycustompower.png`（战斗UI显示）

### 4. 使用能力

```csharp
// 在卡牌效果中应用能力
public override async Task Use(PlayerChoiceContext ctx, Creature target)
{
    // 给目标施加 3 层虚弱
    await PowerCmd.Apply<WeakPower>(target, 3, Player.Creature, this);

    // 给自己施加 2 层力量
    await PowerCmd.Apply<StrengthPower>(Player.Creature, 2, Player.Creature, this);
}
```

---

## 重要提示

1. **能力叠加**：Counter 类型会自动叠加，Single 类型不叠加，None 类型重复应用会失败
2. **负值支持**：设置 `AllowNegative = true` 允许负值（如力量降低）
3. **实例模式**：设置 `IsInstanced = true` 每次应用都创建新实例（用于独立计时）
4. **内部数据**：使用 `InitInternalData()` 和 `GetInternalData<T>()` 存储复杂状态
5. **动画等待**：修改数值后会自动等待，长时间操作后需手动调用 `Cmd.Wait(0.5f)`
6. **事件触发**：`Flash()` 方法触发能力图标闪烁效果
7. **数值为0时不消失**：使用 `StackType.Single` + 内部数据 + 覆盖 `DisplayAmount` 实现
   - 参考：`KarenPromisePilePower` - 显示数值可以为0，但 Power 持续存在

---

## 常用内置能力列表

| 能力 | 类名 | 效果 |
|------|------|------|
| 力量 | `StrengthPower` | 增加攻击伤害 |
| 敏捷 | `DexterityPower` | 增加格挡值 |
| 虚弱 | `WeakPower` | 降低攻击伤害 |
| 脆弱 | `VulnerablePower` | 增加受到的攻击伤害 |
| 中毒 | `PoisonPower` | 回合开始造成伤害 |
| 格挡 | `BlockPower` | 抵挡伤害（内部） |
| 金属化 | `MetallicizePower` | 回合结束获得格挡 |
| 再生 | `RegeneratePower` | 回合结束恢复生命 |
| 神器 | `ArtifactPower` | 抵消下一次 Debuff |
