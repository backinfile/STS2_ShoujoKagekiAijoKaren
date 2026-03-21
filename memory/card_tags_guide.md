# STS2 卡牌 Tag 系统开发指南

## 概述

CardTag（卡牌标签）是《Slay the Spire 2》中用于分类和标记卡牌的系统，位于 `MegaCrit.Sts2.Core.Entities.Cards` 命名空间。Tag 主要用于：
- 卡牌分类识别（打击/防御/ Shiv 等）
- 遗物/能力效果触发条件
- 卡牌效果计算（如完美打击）
- 附魔条件筛选

---

## CardTag 枚举定义

**文件位置**: `src/Core/Entities/Cards/CardTag.cs`

```csharp
namespace MegaCrit.Sts2.Core.Entities.Cards;

public enum CardTag
{
    None,      // 无标签
    Strike,    // 打击（所有角色的打击牌）
    Defend,    // 防御（所有角色的防御牌）
    Minion,    // 仆从（死灵术士召唤物）
    OstyAttack,// Osty 攻击（Osty 专属攻击牌）
    Shiv       // Shiv（匕首，刺客代币）
}
```

---

## CardModel 中的 Tag 属性

### 定义 Tag

在卡牌类中通过 `CanonicalTags` 属性定义标签：

```csharp
public sealed class StrikeIronclad : CardModel
{
    // 定义卡牌标签
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Strike };
}
```

### 多个 Tag

一张卡牌可以有多个标签：

```csharp
public sealed class MinionStrike : CardModel
{
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag>
    {
        CardTag.Strike,
        CardTag.Minion
    };
}
```

### 获取 Tag

```csharp
// 获取卡牌的所有标签
IEnumerable<CardTag> tags = card.Tags;

// 检查是否有特定标签
bool isStrike = card.Tags.Contains(CardTag.Strike);

// 基础打击/防御检测（用于某些遗物效果）
bool isBasicStrikeOrDefend = card.IsBasicStrikeOrDefend;  // 检查是否是基础打击或防御
```

---

## 各 Tag 详细说明

### Strike（打击）

**用途**: 标记所有"打击"类卡牌

**自带此 Tag 的卡牌**:
- `StrikeIronclad` / `StrikeSilent` / `StrikeDefect` / `StrikeRegent` / `StrikeNecrobinder`（各角色基础打击）
- `TwinStrike`, `PommelStrike`, `PerfectedStrike`, `MeteorStrike`, `ShiningStrike` 等
- `MinionStrike`（同时有 Minion 标签）
- `AdaptiveStrike`, `AshenStrike`, `BlightStrike`, `FocusedStrike`, `LeadingStrike` 等

**典型使用场景**:
```csharp
// 遗物：打击木偶（StrikeDummy）- 打击牌伤害+3
public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
{
    if (!cardSource.Tags.Contains(CardTag.Strike))
        return 0m;
    return base.DynamicVars["ExtraDamage"].BaseValue;
}

// 卡牌：完美打击（PerfectedStrike）- 伤害基于卡组中打击牌数量
new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
    (CardModel card, Creature? _) =>
        card.Owner.PlayerCombatState.AllCards.Count((CardModel c) => c.Tags.Contains(CardTag.Strike))
);
```

---

### Defend（防御）

**用途**: 标记所有"防御"类卡牌

**自带此 Tag 的卡牌**:
- `DefendIronclad` / `DefendSilent` / `DefendDefect` / `DefendRegent` / `DefendNecrobinder`（各角色基础防御）
- `UltimateDefend` 等

**典型使用场景**:
```csharp
// 遗物：筛选基础防御牌
CardModel defendCard = character.CardPool.AllCards.First(
    (CardModel c) => c.Rarity == CardRarity.Basic && c.Tags.Contains(CardTag.Defend)
);

// 能力：FastenPower - 只给防御牌添加效果
public override decimal ModifyBlockAdditive(Creature creature, decimal amount, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
{
    if (cardSource == null || !cardSource.Tags.Contains(CardTag.Defend))
        return 0m;
    return base.DynamicVars.Block.BaseValue;
}
```

---

### Shiv（匕首）

**用途**: 标记刺客的 Shiv 代币卡牌

**自带此 Tag 的卡牌**:
- `Shiv`（基础 Shiv 牌）

**典型使用场景**:
```csharp
// 能力：精准（AccuracyPower）- Shiv 伤害增加
public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? card)
{
    if (!card.Tags.Contains(CardTag.Shiv))
        return 0m;
    return base.Amount;
}

// 遗物：螺旋飞镖（HelicalDart）- 打出 Shiv 时获得敏捷
public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
{
    if (cardPlay.Card.Tags.Contains(CardTag.Shiv))
    {
        Flash();
        await PowerCmd.Apply<HelicalDartPower>(base.Owner.Creature, base.DynamicVars.Dexterity.IntValue, base.Owner.Creature, null);
    }
}

// 能力：幻影之刃（PhantomBladesPower）- 给所有 Shiv 添加保留
public override Task AfterApplied(Creature? applier, CardModel? cardSource)
{
    foreach (CardModel item in base.Owner.Player.PlayerCombatState.AllCards.Where(
        (CardModel c) => c.Tags.Contains(CardTag.Shiv)))
    {
        CardCmd.ApplyKeyword(item, CardKeyword.Retain);
    }
    return Task.CompletedTask;
}
```

---

### Minion（仆从）

**用途**: 标记死灵术士的仆从牌

**自带此 Tag 的卡牌**:
- `MinionStrike`（同时有 Strike 标签）
- `MinionSacrifice`
- `MinionDiveBomb`

**典型使用场景**:
```csharp
// 遗物：维特鲁威仆从（VitruvianMinion）- 仆从牌效果增强
public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
{
    if (!cardSource.Tags.Contains(CardTag.Minion))
        return 0m;
    return base.DynamicVars["ExtraDamage"].BaseValue;
}
```

---

### OstyAttack（Osty 攻击）

**用途**: 标记 Osty 的专属攻击牌

**自带此 Tag 的卡牌**:
- `BoneShards`
- `Fetch`
- `Flatten`
- `HighFive`
- `Poke`
- `Protector`
- `Rattle`
- `RightHandHand`
- `SicEm`
- `Snap`
- `Squeeze`
- `SweepingGaze`
- `Unleash`

**典型使用场景**:
```csharp
// 卡牌：挤压（Squeeze）- 伤害基于其他 Osty 攻击牌数量
new CalculatedDamageVar(ValueProp.Move).FromOsty().WithMultiplier(
    (CardModel card, Creature? _) =>
        card.Owner.PlayerCombatState.AllCards.Count(
            (CardModel c) => c.Tags.Contains(CardTag.OstyAttack) && c != card
        )
);
```

---

## 完整使用示例

### 在卡牌中定义 Tag

```csharp
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace YourMod.Cards;

public sealed class MyCustomStrike : CardModel
{
    // 定义此卡牌为打击牌
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Strike };

    public MyCustomStrike()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }
}
```

### 在遗物中检测 Tag

```csharp
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.ValueProps;

namespace YourMod.Relics;

public sealed class MyStrikeRelic : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // 打出打击牌时触发效果
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Tags.Contains(CardTag.Strike))
        {
            Flash();
            // 执行效果...
        }
    }

    // 增加打击牌伤害
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (cardSource?.Tags.Contains(CardTag.Strike) == true)
        {
            return 2m;  // 打击牌+2伤害
        }
        return 0m;
    }
}
```

### 在能力中检测 Tag

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace YourMod.Powers;

public sealed class MyShivPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 只增加 Shiv 的伤害
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? card)
    {
        if (base.Owner != dealer)
            return 0m;
        if (!props.IsPoweredAttack())
            return 0m;
        if (card == null || !card.Tags.Contains(CardTag.Shiv))
            return 0m;
        return base.Amount;
    }
}
```

### 计算卡组中特定 Tag 的卡牌数量

```csharp
// 计算卡组中打击牌数量
int strikeCount = player.PlayerCombatState.AllCards.Count(
    (CardModel c) => c.Tags.Contains(CardTag.Strike)
);

// 计算 Shiv 数量（包括消耗堆）
int shivCount = PileType.Exhaust.GetPile(player).Cards.Count(
    (CardModel c) => c.Tags.Contains(CardTag.Shiv)
);

// 筛选特定 Tag 的卡牌
List<CardModel> strikes = player.Deck.Cards.Where(
    (CardModel c) => c.Tags.Contains(CardTag.Strike)
).ToList();
```

### 在附魔中使用 Tag 筛选

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;

namespace YourMod.Enchantments;

public sealed class StrikeEnchantment : EnchantmentModel
{
    // 只能附魔给打击牌
    public override bool CanEnchant(CardModel c)
    {
        return base.CanEnchant(c) && c.Tags.Contains(CardTag.Strike);
    }
}
```

### 在卡牌选择中使用 Tag 过滤

```csharp
// 从卡组中选择打击牌
List<CardModel> strikes = (await CardSelectCmd.FromDeckForRemoval(
    prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2),
    player: base.Owner,
    filter: (CardModel c) => c.Tags.Contains(CardTag.Strike)
)).ToList();
```

---

## 内置使用 Tag 的遗物/能力汇总

### 遗物

| 遗物 | 检测 Tag | 效果 |
|------|----------|------|
| `StrikeDummy` | Strike | 打击伤害+3 |
| `VitruvianMinion` | Minion | 仆从伤害和格挡+3 |
| `HelicalDart` | Shiv | 打出 Shiv 获得敏捷 |
| `GhostSeed` | Strike/Defend | 对基础打击/防御牌特殊处理 |
| `NutritiousSoup` | Strike | 升级基础打击牌时自动附魔 |
| `LargeCapsule` | Strike/Defend | 获取角色基础打击/防御牌 |
| `LeafyPoultice` | Strike/Defend | 变换打击/防御牌 |

### 能力

| 能力 | 检测 Tag | 效果 |
|------|----------|------|
| `AccuracyPower` | Shiv | Shiv 伤害增加 |
| `PhantomBladesPower` | Shiv | Shiv 获得保留，首张小刀伤害增加 |
| `HellraiserPower` | Strike | 打出打击牌时抽牌 |
| `FastenPower` | Defend | 防御牌格挡增加 |

### 卡牌

| 卡牌 | 使用 Tag | 效果 |
|------|----------|------|
| `PerfectedStrike` | Strike | 伤害基于卡组打击牌数量 |
| `KnifeTrap` | Shiv | 基于消耗堆 Shiv 数量造成伤害 |
| `Squeeze` | OstyAttack | 伤害基于其他 Osty 攻击牌数量 |

### 附魔

| 附魔 | 检测 Tag | 效果 |
|------|----------|------|
| `Spiral` | Strike/Defend | 只能附魔给基础打击/防御牌 |
| `Goopy` | Defend | 只能附魔给防御牌 |

---

## 创建自定义 Tag 的建议

当前游戏内置 5 个 Tag，**Mod 无法添加新的 CardTag 枚举值**（因为游戏代码固定），但可以通过以下方式实现类似效果：

### 方案1：使用 CardKeyword

如果需要在卡牌上显示标签，使用 `CardKeyword`：

```csharp
public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Summon };
```

### 方案2：使用 CardType 配合特定逻辑

```csharp
// 检测特定卡牌类型
if (card.Type == CardType.Attack && card.Rarity == CardRarity.Token)
{
    // 处理代币攻击牌
}
```

### 方案3：通过卡牌 ID 前缀/后缀识别

```csharp
// 自定义识别逻辑
bool IsMySpecialCard(CardModel card)
{
    return card.Id.Entry.StartsWith("MyMod_Special_");
}
```

---

## 重要提示

1. **Tag 是只读的**: `Tags` 属性返回的是 `IEnumerable<CardTag>`，运行时无法动态添加/移除
2. **Tag 在定义时确定**: 通过 `CanonicalTags` 在卡牌类定义时设置
3. **多 Tag 支持**: 一张卡牌可以有多个 Tag（如 `MinionStrike` 同时有 Strike 和 Minion）
4. **基础检测**: `IsBasicStrikeOrDefend` 属性同时检查 `Basic` 稀有度和 `Strike`/`Defend` Tag
5. **性能考虑**: 使用 `HashSet<CardTag>` 存储 Tag，Contains 检查是 O(1) 复杂度
