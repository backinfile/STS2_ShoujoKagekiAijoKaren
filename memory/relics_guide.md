# STS2 遗物系统开发指南

## 概述

Relic（遗物）是《Slay the Spire 2》中的被动道具系统，一局游戏中持续生效。游戏包含 290 种遗物，继承自 `RelicModel`，位于 `MegaCrit.Sts2.Core.Models`。

---

## 基础架构

### RelicRarity（遗物稀有度）

位置：`src/Core/Entities/Relics/RelicRarity.cs`

```csharp
public enum RelicRarity
{
    None,    // 无
    Starter, // 初始遗物（燃烧之血等）
    Common,  // 普通
    Uncommon,// 罕见
    Rare,    // 稀有
    Shop,    // 商店专属
    Event,   // 事件专属
    Ancient  // 上古者（Boss）
}
```

### RelicStatus（遗物状态）

位置：`src/Core/Entities/Relics/RelicStatus.cs`

```csharp
public enum RelicStatus
{
    Normal,   // 正常
    Active,   // 激活（脉冲效果）
    Disabled  // 禁用（使用后）
}
```

---

## RelicModel 核心属性

### 抽象/虚属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Rarity` | `RelicRarity` | **必须**，遗物稀有度 |
| `IconPath` | `string` | 图标路径（自动，可覆盖） |
| `PackedIconPath` | `string` | 打包图集图标路径 |
| `BigIconPath` | `string` | 大图标路径（可覆盖） |
| `IsUsedUp` | `bool` | 是否已用完（默认 false） |
| `HasUponPickupEffect` | `bool` | 是否有拾取时效果 |
| `SpawnsPets` | `bool` | 是否生成宠物 |
| `IsStackable` | `bool` | 是否可堆叠 |
| `ShowCounter` | `bool` | 是否显示计数器 |
| `DisplayAmount` | `int` | 显示数值 |
| `FlashSfx` | `string` | 激活音效 |
| `ShouldFlashOnPlayer` | `bool` | 是否在玩家身上显示闪光 |
| `MerchantCost` | `int` | 商店价格（根据稀有度默认） |

### 只读属性

| 属性 | 说明 |
|------|------|
| `Title` | 本地化标题（`relics/{Id}.title`） |
| `Description` | 本地化描述 |
| `DynamicDescription` | 带动态变量的描述 |
| `Flavor` | 风味文本 |
| `Icon` | 图标纹理 |
| `BigIcon` | 大图标纹理 |
| `Owner` | 拥有者玩家 |
| `Pool` | 所属遗物池 |
| `IsTradable` | 是否可交易 |
| `IsWax` | 是否是蜡像（变形） |
| `IsMelted` | 是否已融化 |
| `DynamicVars` | 动态变量集合 |
| `Status` | 当前状态 |
| `HoverTip` | 悬浮提示 |

---

## 常用 Hook 方法

遗物继承 `AbstractModel` 的所有 Hook 方法，以下是常用的：

### 战斗相关

```csharp
// 战斗开始
public virtual Task BeforeCombatStart()
public virtual Task AfterSideTurnStart(CombatSide side, CombatState combatState)

// 回合相关
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

public virtual Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
public virtual Task AfterCardDiscarded(PlayerChoiceContext ctx, CardModel card)
public virtual Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)
```

### 伤害与格挡

```csharp
public virtual Task BeforeDamageReceived(PlayerChoiceContext ctx, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
public virtual Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
public virtual Task AfterDamageGiven(PlayerChoiceContext ctx, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)

public virtual Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
public virtual Task AfterBlockBroken(Creature creature)
```

### 遗物生命周期

```csharp
// 获得遗物
public virtual Task AfterObtained()

// 移除遗物
public virtual Task AfterRemoved()
```

### 房间与事件

```csharp
public virtual Task BeforeRoomEntered(AbstractRoom room)
public virtual Task AfterRoomEntered(AbstractRoom room)

public virtual Task AfterRestSiteHeal(Player player, bool isMimicked)
public virtual Task AfterRestSiteSmith(Player player)

public virtual Task AfterGoldGained(Player player)
public virtual Task AfterItemPurchased(Player player, MerchantEntry item, int goldSpent)
```

### 数值修改器

```csharp
public virtual decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
public virtual decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
public virtual decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
```

---

## RelicCmd 命令类

位置：`src/Core/Commands/RelicCmd.cs`

### 主要方法

```csharp
// 获取遗物（泛型）
public static async Task<T> Obtain<T>(Player player) where T : RelicModel

// 获取遗物（实例）
public static async Task<RelicModel> Obtain(RelicModel relic, Player player, int index = -1)

// 移除遗物
public static async Task Remove(RelicModel relic)

// 替换遗物
public static async Task<RelicModel> Replace(RelicModel original, RelicModel replace)

// 融化遗物（上古者惩罚）
public static async Task Melt(RelicModel relic)
```

---

## 完整示例

### 示例1：燃烧之血（BurningBlood）- 战斗胜利恢复

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class BurningBlood : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    // 定义动态变量（治疗量）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new HealVar(6m));

    // 战斗胜利后恢复生命
    public override async Task AfterCombatVictory(CombatRoom _)
    {
        if (!base.Owner.Creature.IsDead)
        {
            Flash();  // 触发遗物闪光
            await CreatureCmd.Heal(base.Owner.Creature, base.DynamicVars.Heal.BaseValue);
        }
    }
}
```

### 示例2：黑血（BlackBlood）- 升级版燃烧之血

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class BlackBlood : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new HealVar(12m));

    public override async Task AfterCombatVictory(CombatRoom _)
    {
        if (!base.Owner.Creature.IsDead)
        {
            Flash();
            await CreatureCmd.Heal(base.Owner.Creature, base.DynamicVars.Heal.BaseValue);
        }
    }
}
```

### 示例3：锚（Anchor）- 战斗开始获得格挡

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class Anchor : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new BlockVar(10m, ValueProp.Unpowered));

    // 显示格挡悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new global::_003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.Static(StaticHoverTip.Block));

    // 战斗开始时获得格挡
    public override async Task BeforeCombatStart()
    {
        Flash();
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, null);
    }
}
```

### 示例4：赤牛（Akabeko）- 首回合给予能力

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class Akabeko : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // 给予活力能力
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new PowerVar<VigorPower>(8m));

    // 显示活力能力悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new global::_003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromPower<VigorPower>());

    // 首回合给予活力
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Creature.Side && combatState.RoundNumber <= 1)
        {
            Flash();
            await PowerCmd.Apply<VigorPower>(
                base.Owner.Creature,
                base.DynamicVars["VigorPower"].IntValue,
                base.Owner.Creature,
                null
            );
        }
    }
}
```

---

## 创建自定义遗物步骤

### 1. 创建遗物类

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Relics;
using System.Threading.Tasks;

namespace YourMod.Relics;

public sealed class MyCustomRelic : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task BeforeCombatStart()
    {
        // 遗物效果
    }
}
```

### 2. 添加本地化

**英文** `localization/eng/relics.json`:
```json
{
    "MyCustomRelic.title": "My Relic",
    "MyCustomRelic.description": "At the start of combat, gain [blue]10[/blue] [gold]Block[/gold].",
    "MyCustomRelic.flavor": "A mysterious relic."
}
```

**中文** `localization/zhs/relics.json`:
```json
{
    "MyCustomRelic.title": "我的遗物",
    "MyCustomRelic.description": "战斗开始时，获得[blue]10[/blue]点[gold]格挡[/gold]。",
    "MyCustomRelic.flavor": "一个神秘的遗物。"
}
```

### 3. 添加图标

- 小图标：`images/relics/mycustomrelic.png`（64x64）
- 大图标：`images/relics/mycustomrelic.png`（256x256）

图标会自动从以下路径加载：
- 标准：`res://images/relics/{lowercase_name}.png`
- Beta：`res://images/relics/beta/{lowercase_name}.png`

### 4. 使用遗物

```csharp
// 在Mod初始化中注册遗物池（如需）
// 或使用控制台命令测试
public static async Task GiveRelic(Player player)
{
    await RelicCmd.Obtain<MyCustomRelic>(player);
}
```

---

## 重要提示

1. **Flash() 方法**：遗物生效时调用 `Flash()` 触发视觉反馈和音效
2. **动态变量**：使用 `CanonicalVars` 定义可升级的数值（如 `new HealVar(6m)`）
3. **悬浮提示**：使用 `ExtraHoverTips` 添加关联能力或关键词的提示
4. **战斗范围**：遗物 Hook 在非战斗状态（如地图）也会触发，需检查 `CombatManager.Instance.IsInProgress`
5. **稀有度限制**：`Starter` 和 `Ancient` 稀有度的遗物不会在正常遗物池中出现

---

## 内置遗物分类参考

| 类型 | 示例 | 触发时机 |
|------|------|----------|
| 初始遗物 | BurningBlood, BlackBlood | 战斗胜利恢复 |
| 战斗开始 | Anchor, BagOfMarbles | BeforeCombatStart |
| 回合触发 | HornCleat, IceCream | AfterSideTurnStart |
| 伤害加成 | Akabeko, Vajra | ModifyDamageAdditive |
| 格挡加成 | Orichalcum, FossilizedHelix | AfterBlockGained |
| 卡牌相关 | DeadBranch, Charon'sAshes | AfterCardPlayed/Exhausted |
| 金币相关 | GoldenIdol, MawBank | AfterGoldGained |
| 商店相关 | MembershipCard, SmilingMask | AfterItemPurchased |
| 休息点 | DreamCatcher, RegalPillow | AfterRestSiteHeal/Smith |
| 特殊 | EctoPlasm, BustedCrown | 全局修改器 |

---

## 动态变量类型

遗物常用动态变量（`src/Core/Localization/DynamicVars/`）：

```csharp
// 治疗
new HealVar(6m)

// 格挡
new BlockVar(10m, ValueProp.Unpowered)

// 伤害
new DamageVar(8m, ValueProp.Powered)

// 能力层数
new PowerVar<StrengthPower>(2m)

// 能量
new EnergyVar(1m)

// 金币
new GoldVar(100m)

// 抽牌
new DrawVar(2m)
```
