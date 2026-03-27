# Slay the Spire 2 - Core Models 效果代码参考

> 本文档整理自 `D:\claudeProj\sts2\src\Core\Models` 目录下的反编译代码，用于指导编写新卡牌、新能力、新遗物和新药水。

---

## 目录

1. [卡牌 (CardModel)](#1-卡牌-cardmodel)
2. [能力 (PowerModel)](#2-能力-powermodel)
3. [遗物 (RelicModel)](#3-遗物-relicmodel)
4. [药水 (PotionModel)](#4-药水-potionmodel)
5. [DynamicVar 系统](#5-dynamicvar-系统)
6. [Command API 汇总](#6-command-api-汇总)
7. [Hook/事件系统](#7-hookevent-系统)
8. [代码模板](#8-代码模板)

---

## 1. 卡牌 (CardModel)

### 1.1 基础结构

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class MyCard : CardModel
{
    // 构造函数：费用、类型、稀有度、目标类型
    public MyCard()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 动态变量定义
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(8m, ValueProp.Move),
        new BlockVar(5m, ValueProp.Move)
    };

    // 悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<VulnerablePower>()
    };

    // 关键词（显示在卡牌底部）
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        CardKeyword.Exhaust
    };

    // 打出效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 实现效果
    }

    // 升级效果
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
```

### 1.2 构造函数参数

| 参数 | 类型 | 说明 |
|------|------|------|
| `energyCost` | `int` | 基础能量消耗 |
| `type` | `CardType` | 卡牌类型：Attack, Skill, Power, Status, Curse, Quest |
| `rarity` | `CardRarity` | 稀有度：Basic, Common, Uncommon, Rare, Special, Curse, Status, Event, Quest, Ancient |
| `targetType` | `TargetType` | 目标类型 |

**TargetType 枚举值：**
- `None` - 无目标
- `Self` - 自身
- `AnyEnemy` - 任意敌人
- `AllEnemies` - 所有敌人
- `RandomEnemy` - 随机敌人
- `FrontEnemy` - 前方敌人
- `BackEnemy` - 后方敌人

### 1.3 常用属性覆盖

```csharp
// 提供格挡（影响格挡悬浮提示显示）
public override bool GainsBlock => true;

// 多重攻击（影响视觉表现）
public override int BaseHitCount => 2;

// 无限升级
public override int MaxUpgradeLevel => int.MaxValue;

// 强制保留（如 Iron Wave）
public override bool Retain => true;
```

### 1.4 攻击卡牌示例

```csharp
public sealed class Bash : CardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<VulnerablePower>()
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<VulnerablePower>(2m)
    };

    public Bash()
        : base(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 攻击命令链式调用
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);

        // 应用易伤
        await PowerCmd.Apply<VulnerablePower>(
            cardPlay.Target,
            base.DynamicVars.Vulnerable.BaseValue,
            base.Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars.Vulnerable.UpgradeValueBy(1m);
    }
}
```

### 1.5 技能/防御卡牌示例

```csharp
public sealed class Armaments : CardModel
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new BlockVar(5m, ValueProp.Move)
    };

    public Armaments()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        // 升级手牌
        if (base.IsUpgraded)
        {
            // 升级所有可升级的手牌
            foreach (CardModel item in PileType.Hand.GetPile(base.Owner).Cards.Where(c => c.IsUpgradable))
            {
                CardCmd.Upgrade(item);
            }
        }
        else
        {
            // 选择一张手牌升级
            CardModel cardModel = await CardSelectCmd.FromHandForUpgrade(choiceContext, base.Owner, this);
            if (cardModel != null)
            {
                CardCmd.Upgrade(cardModel);
            }
        }
    }
}
```

### 1.6 能力卡牌示例

```csharp
public sealed class Inflame : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new PowerVar<StrengthPower>(2m)
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<StrengthPower>()
    };

    protected override IEnumerable<string> ExtraRunAssetPaths => NGroundFireVfx.AssetPaths;

    public Inflame()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NPowerUpVfx.CreateNormal(base.Owner.Creature);
        await PowerCmd.Apply<StrengthPower>(
            base.Owner.Creature,
            base.DynamicVars["StrengthPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    public override async Task OnEnqueuePlayVfx(Creature? target)
    {
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(base.Owner.Creature));
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["StrengthPower"].UpgradeValueBy(1m);
    }
}
```

### 1.7 群体效果卡牌

```csharp
public sealed class Shockwave : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("Power", 3m)
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        CardKeyword.Exhaust
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    };

    public Shockwave()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        VfxCmd.PlayOnCreatureCenter(base.Owner.Creature, "vfx/vfx_flying_slash");

        int amount = base.DynamicVars["Power"].IntValue;
        foreach (Creature enemy in base.CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<WeakPower>(enemy, amount, base.Owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(enemy, amount, base.Owner.Creature, this);
        }
    }
}
```

### 1.8 生成其他卡牌

```csharp
public sealed class BladeDance : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new CardsVar(3)
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromCard<Shiv>()
    };

    public BladeDance()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        for (int i = 0; i < base.DynamicVars.Cards.IntValue; i++)
        {
            await Shiv.CreateInHand(base.Owner, base.CombatState);
            await Cmd.Wait(0.1f);
        }
    }
}

// Shiv 卡牌中的静态工厂方法
public static async Task<CardModel?> CreateInHand(Player owner, CombatState combatState)
{
    if (CombatManager.Instance.IsOverOrEnding)
    {
        return null;
    }

    CardModel shiv = combatState.CreateCard<Shiv>(owner);
    await CardPileCmd.AddGeneratedCardToCombat(shiv, PileType.Hand, addedByPlayer: true);
    return shiv;
}
```

---

## 2. 能力 (PowerModel)

### 2.1 基础结构

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class MyPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;  // Buff/Debuff

    public override PowerStackType StackType => PowerStackType.Counter;  // Counter/Single

    public override bool AllowNegative => true;  // 允许负值（如力量）

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("MyValue", 1.5m)
    };
}
```

### 2.2 PowerType 与 StackType

| 属性 | 可选值 | 说明 |
|------|--------|------|
| `Type` | `Buff`, `Debuff` | 能力类型，影响视觉效果和交互 |
| `StackType` | `Counter`, `Single` | Counter叠加层数，Single只存在有无 |

### 2.3 伤害修改能力

```csharp
// 力量 - 加法修改
public sealed class StrengthPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => true;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (base.Owner != dealer) return 0m;
        if (!props.IsPoweredAttack()) return 0m;
        return base.Amount;  // 返回加法值
    }
}

// 易伤 - 乘法修改
public sealed class VulnerablePower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("DamageIncrease", 1.5m)  // 50%增伤
    };

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner) return 1m;
        if (!props.IsPoweredAttack()) return 1m;

        decimal multiplier = base.DynamicVars["DamageIncrease"].BaseValue;
        return multiplier;  // 返回乘数
    }

    // 回合结束减少
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Enemy)
        {
            await PowerCmd.TickDownDuration(this);
        }
    }
}

// 虚弱 - 伤害减少
public sealed class WeakPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("DamageDecrease", 0.75m)  // 25%减伤
    };

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != base.Owner) return 1m;
        if (!props.IsPoweredAttack()) return 1m;

        return base.DynamicVars["DamageDecrease"].BaseValue;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Enemy)
        {
            await PowerCmd.TickDownDuration(this);
        }
    }
}
```

### 2.4 格挡修改能力

```csharp
// 脆弱
public sealed class FrailPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (base.Owner != target) return 1m;
        if (!props.IsPoweredCardOrMonsterMoveBlock()) return 1m;
        return 0.75m;  // 格挡减少25%
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Enemy)
        {
            await PowerCmd.TickDownDuration(this);
        }
    }
}
```

### 2.5 特殊效果能力

```csharp
//  artifact - 抵消debuff
public sealed class ArtifactPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldScaleInMultiplayer => true;

    // 尝试修改收到的能力值
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? _, out decimal modifiedAmount)
    {
        if (target != base.Owner)
        {
            modifiedAmount = amount;
            return false;
        }
        if (canonicalPower.GetTypeForAmount(amount) != PowerType.Debuff)
        {
            modifiedAmount = amount;
            return false;
        }
        if (!canonicalPower.IsVisible)
        {
            modifiedAmount = amount;
            return false;
        }

        modifiedAmount = 0m;  // 抵消debuff
        return true;
    }

    // 抵消后减少artifact层数
    public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        await PowerCmd.Decrement(this);
    }
}

// Barricade - 保留格挡
public sealed class BarricadePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.Static(StaticHoverTip.Block)
    };

    // 阻止清除格挡
    public override bool ShouldClearBlock(Creature creature)
    {
        if (base.Owner != creature) return true;
        return false;  // 不清理自己的格挡
    }
}
```

### 2.6 回合触发能力

```csharp
// 毒
public sealed class PoisonPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

    // 回合开始时触发
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != base.Owner.Side) return;

        int iterations = TriggerCount;
        for (int i = 0; i < iterations; i++)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                base.Owner,
                base.Amount,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null,
                null
            );

            if (base.Owner.IsAlive)
            {
                await PowerCmd.Decrement(this);
            }
        }
    }
}
```

### 2.7 Power 生命周期钩子

```csharp
public override Task AfterApplied(Creature? applier, CardModel? cardSource)
{
    // 能力被应用后
    return Task.CompletedTask;
}

public override Task AfterModifyingPowerAmountReceived(PowerModel power)
{
    // 修改其他能力后
    return Task.CompletedTask;
}

public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
{
    // 某一方回合开始时
}

public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
{
    // 某一方回合结束时
}
```

---

## 3. 遗物 (RelicModel)

### 3.1 基础结构

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class MyRelic : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new HealVar(6m)
    };

    // 战斗胜利后
    public override async Task AfterCombatVictory(CombatRoom _)
    {
        if (!base.Owner.Creature.IsDead)
        {
            Flash();  // 遗物闪烁
            await CreatureCmd.Heal(base.Owner.Creature, base.DynamicVars.Heal.BaseValue);
        }
    }
}
```

### 3.2 RelicRarity 稀有度

- `Starter` - 初始遗物
- `Common` - 普通
- `Uncommon` - 稀有
- `Rare` - 罕见
- `Special` - 特殊
- `Boss` - Boss遗物
- `Shop` - 商店遗物

### 3.3 常用遗物钩子

```csharp
// 战斗相关
public override Task AfterCombatVictory(CombatRoom room)
public override Task OnVictoryFlash(CombatRoom room)  // 胜利闪烁时
public override Task OnPlayerApplyEnemyPower(Creature target, PowerModel power)
public override Task OnPlayerPlayAttackCard(CardModel card, Creature target)
public override Task OnPlayerPlaySkillCard(CardModel card)
public override Task OnPlayerPlayPowerCard(CardModel card)

// 房间/地图相关
public override Task OnEnterRoom(Room room)
public override Task OnEnterRestSite()
public override Task OnLeaveRestSite()

// 抽牌/手牌
public override Task OnDrawCard(CardModel card)
public override void ModifyDrawnCard(CardModel card)  // 修改抽到的牌

// 药水
public override void ModifyPotionCount(ref int count)

// 能量
public override Task OnGainEnergy(int amount)

// 受到伤害/治疗
public override Task OnPlayerDamageTaken(int damageAmount, Creature? source)
public override Task OnPlayerHeal(int healAmount)
public override void ModifyIncomingDamage(ref int damage, Creature? source)

// 金币
public override void ModifyGoldGain(ref int goldGain)

// 洗牌
public override Task OnShuffleDrawPile()

// 精英/Boss遭遇
public override Task OnEnterEliteCombat()
public override Task OnEnterBossCombat()

// 拾取时
public override Task OnPickup()
```

### 3.4 遗物示例

```csharp
// Burning Blood - 战斗结束回血
public sealed class BurningBlood : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new HealVar(6m)
    };

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

---

## 4. 药水 (PotionModel)

### 4.1 基础结构

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Potions;

public sealed class FirePotion : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;  // Anywhere/CombatOnly
    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(20m, ValueProp.Unpowered)  // 无视力量
    };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        PotionModel.AssertValidForTargetedPotion(target);

        // 视觉特效
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(target));

        // 造成伤害
        await CreatureCmd.Damage(
            choiceContext,
            target,
            base.DynamicVars.Damage.BaseValue,
            base.DynamicVars.Damage.Props,  // Unpowered
            base.Owner.Creature,
            null
        );
    }
}
```

### 4.2 PotionUsage

- `Anywhere` - 可在任何地方使用（包括战斗外）
- `CombatOnly` - 仅战斗中

---

## 5. DynamicVar 系统

### 5.1 变量类型

| 变量类 | 用途 | 示例 |
|--------|------|------|
| `DamageVar` | 伤害值 | `new DamageVar(8m, ValueProp.Move)` |
| `BlockVar` | 格挡值 | `new BlockVar(5m, ValueProp.Move)` |
| `PowerVar<T>` | 能力层数 | `new PowerVar<StrengthPower>(2m)` |
| `EnergyVar` | 能量值 | `new EnergyVar(2)` |
| `CardsVar` | 卡牌数量 | `new CardsVar(3)` |
| `HpLossVar` | 失去生命 | `new HpLossVar(6m)` |
| `HealVar` | 治疗值 | `new HealVar(6m)` |
| `DynamicVar` | 通用值 | `new DynamicVar("Key", 1.5m)` |
| `StringVar` | 字符串 | `new StringVar("ApplierName")` |

### 5.2 ValueProp 属性

用于标记伤害/格挡的属性：

```csharp
ValueProp.Move           // 动作（触发相关遗物/能力）
ValueProp.Unpowered      // 无视力量
ValueProp.Unblockable    // 无视格挡
ValueProp.Orb            // 球体伤害
```

### 5.3 自定义计算变量

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => new[]
{
    new DamageVar(4m, ValueProp.Move),
    new CalculationBaseVar(0m),
    new CalculationExtraVar(1m),
    new CalculatedVar("FanOfKnivesAmount").WithMultiplier((CardModel card, Creature? _) =>
        (card != null && card.IsMutable && card.Owner != null)
            ? card.Owner.Creature.GetPowerAmount<FanOfKnivesPower>()
            : 0)
};
```

### 5.4 升级变量

```csharp
protected override void OnUpgrade()
{
    base.DynamicVars.Damage.UpgradeValueBy(2m);      // 伤害+2
    base.DynamicVars.Block.UpgradeValueBy(3m);       // 格挡+3
    base.DynamicVars.Cards.UpgradeValueBy(1m);       // 卡牌数+1
    base.DynamicVars["CustomKey"].UpgradeValueBy(1m); // 自定义+1
}
```

---

## 6. Command API 汇总

### 6.1 伤害相关

```csharp
// 基础攻击
await DamageCmd.Attack(damageAmount)
    .FromCard(this)
    .Targeting(target)
    .Execute(choiceContext);

// 带特效的攻击
await DamageCmd.Attack(damageAmount)
    .FromCard(this)
    .Targeting(target)
    .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
    .Execute(choiceContext);

// 自定义VFX的攻击
await DamageCmd.Attack(damageAmount)
    .FromCard(this)
    .Targeting(target)
    .WithHitVfxNode((Creature t) => NShivThrowVfx.Create(base.Owner.Creature, t, Colors.Green))
    .Execute(choiceContext);

// 多目标攻击
await DamageCmd.Attack(damageAmount)
    .FromCard(this)
    .TargetingAllOpponents(combatState)
    .Execute(choiceContext);

// 带动画的攻击
await DamageCmd.Attack(damageAmount)
    .FromCard(this)
    .Targeting(target)
    .WithAttackerAnim("Shiv", 0.2f)
    .Execute(choiceContext);

// 直接伤害（无卡牌来源）
await CreatureCmd.Damage(choiceContext, target, amount, ValueProp.Move, cardSource);
await CreatureCmd.Damage(choiceContext, target, amount, ValueProp.Unblockable | ValueProp.Unpowered, this);
```

### 6.2 格挡相关

```csharp
// 获得格挡
await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

// 直接数值
await CreatureCmd.GainBlock(base.Owner.Creature, 10m, cardPlay);
```

### 6.3 能力相关

```csharp
// 应用能力
await PowerCmd.Apply<StrengthPower>(target, amount, applier, cardSource);
await PowerCmd.Apply<StrengthPower>(target, base.DynamicVars.StrengthPower.BaseValue, base.Owner.Creature, this);

// 减少能力层数
await PowerCmd.Decrement(power);

// 能力持续时间减少
await PowerCmd.TickDownDuration(power);

// 获取能力数量
int amount = creature.GetPowerAmount<StrengthPower>();
bool hasPower = creature.HasPower<StrengthPower>();
T power = creature.GetPower<T>();
```

### 6.4 抽牌相关

```csharp
// 抽牌
await CardPileCmd.Draw(choiceContext, count, player);

// 从抽牌堆抽取特定卡牌
CardModel card = await CardPileCmd.DrawSpecific(cardFromDrawPile, player);
```

### 6.5 卡牌操作

```csharp
// 创建战斗中的卡牌
CardModel newCard = combatState.CreateCard<Shiv>(owner);

// 添加生成的卡牌到战斗
await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true);

// 升级卡牌
CardCmd.Upgrade(cardModel);

// 获取牌堆
List<CardModel> handCards = PileType.Hand.GetPile(player).Cards.ToList();
```

### 6.6 选牌界面

```csharp
// 从手牌选择一张
CardModel card = await CardSelectCmd.FromHand(choiceContext, player, cardSource);

// 从手牌选择用于丢弃
CardModel card = await CardSelectCmd.FromHandForDiscard(choiceContext, player, cardSource);

// 从手牌选择用于升级
CardModel card = await CardSelectCmd.FromHandForUpgrade(choiceContext, player, cardSource);

// 从手牌选择用于消耗
CardModel card = await CardSelectCmd.FromHandForExhaust(choiceContext, player, cardSource);

// 从弃牌堆选择
IEnumerable<CardModel> cards = await CardSelectCmd.FromDiscard(choiceContext, player, count, cardSource);

// 从抽牌堆选择
IEnumerable<CardModel> cards = await CardSelectCmd.FromDraw(choiceContext, player, count, cardSource);

// 从所有牌堆选择
IEnumerable<CardModel> cards = await CardSelectCmd.FromAllPiles(choiceContext, player, count, cardSource);

// 从任意来源选择
IEnumerable<CardModel> cards = await CardSelectCmd.FromGrid(choiceContext, cards, player, prefs);
```

### 6.7 能量相关

```csharp
// 获得能量
await PlayerCmd.GainEnergy(amount, player);
```

### 6.8 治疗相关

```csharp
// 治疗
await CreatureCmd.Heal(creature, amount);

// fire-and-forget 模式（无context时）
_ = CreatureCmd.Heal(creature, amount);
```

### 6.9 动画相关

```csharp
// 触发动画
await CreatureCmd.TriggerAnim(creature, "Cast", delay);
await CreatureCmd.TriggerAnim(base.Owner.Creature, "Shiv", 0.2f);

// 等待
await Cmd.Wait(0.1f);

// 缩放等待
await Cmd.CustomScaledWait(0.1f, 0.25f);
```

### 6.10 视觉特效

```csharp
// 全屏特效
VfxCmd.PlayFullScreenInCombat("vfx/vfx_adrenaline");

// 在角色中心播放
VfxCmd.PlayOnCreatureCenter(creature, "vfx/vfx_flying_slash");

// 创建特效节点
NPowerUpVfx.CreateNormal(creature);
NGroundFireVfx.Create(creature);
NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfxNode);
```

---

## 7. Hook/Event 系统

### 7.1 Hook 类型

```csharp
// 修改伤害
Hook.ModifyDamage(runState, combatState, target, dealer, baseDamage, props, cardSource, hookType, previewMode, out sources);

// 修改格挡
Hook.ModifyBlock(runState, combatState, target, baseBlock, props, cardSource, hookType);

// 卡牌移动
Hook.AfterCardChangedPiles(card, fromPile, toPile, source);
```

### 7.2 卡牌移动监听

```csharp
// 在全局系统中监听
GlobalMoveSystem.OnCardMoved += (card, from, to, source) =>
{
    // 处理卡牌移动
};
```

---

## 8. 代码模板

### 8.1 攻击卡牌模板

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace YourNamespace.Cards;

public sealed class YourAttackCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(10m, ValueProp.Move)
    };

    public YourAttackCard()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
```

### 8.2 防御卡牌模板

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace YourNamespace.Cards;

public sealed class YourDefendCard : CardModel
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new BlockVar(8m, ValueProp.Move)
    };

    public YourDefendCard()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(3m);
    }
}
```

### 8.3 能力卡牌模板

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YourNamespace.Cards;

public sealed class YourPowerCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new PowerVar<YourPower>(2m)
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<YourPower>()
    };

    public YourPowerCard()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<YourPower>(
            base.Owner.Creature,
            base.DynamicVars.YourPower.BaseValue,
            base.Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.YourPower.UpgradeValueBy(1m);
    }
}
```

### 8.4 能力模板

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace YourNamespace.Powers;

public sealed class YourPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;  // 或 Debuff
    public override PowerStackType StackType => PowerStackType.Counter;  // 或 Single

    // 可选：允许负值
    public override bool AllowNegative => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("Key", 1m)
    };

    // 根据需要覆盖以下方法：

    // 修改伤害（加法）
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return 0m;
    }

    // 修改伤害（乘法）
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return 1m;
    }

    // 修改格挡
    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        return 1m;
    }

    // 回合结束
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Enemy)
        {
            await PowerCmd.TickDownDuration(this);
        }
    }
}
```

### 8.5 遗物模板

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace YourNamespace.Relics;

public sealed class YourRelic : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new HealVar(6m)
    };

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (!base.Owner.Creature.IsDead)
        {
            Flash();
            await CreatureCmd.Heal(base.Owner.Creature, base.DynamicVars.Heal.BaseValue);
        }
    }
}
```

### 8.6 药水模板

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace YourNamespace.Potions;

public sealed class YourPotion : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(20m, ValueProp.Unpowered)
    };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        PotionModel.AssertValidForTargetedPotion(target);
        await CreatureCmd.Damage(
            choiceContext,
            target,
            base.DynamicVars.Damage.BaseValue,
            base.DynamicVars.Damage.Props,
            base.Owner.Creature,
            null
        );
    }
}
```

---

## 附录：常用命名空间

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
```

---

> 文档生成时间：2026-03-28
> 基于 Slay the Spire 2 v0.99.1 反编译代码
