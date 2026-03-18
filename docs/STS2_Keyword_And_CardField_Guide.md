# STS2 关键字系统与卡牌动态字段实现指南

## 目录

1. [关键字系统 (Keyword System)](#一关键字系统-keyword-system)
2. [卡牌动态字段系统](#二卡牌动态字段系统)
3. [使用 DynamicVar 实现关键字系统](#三使用-dynamicvar-实现关键字系统)
4. [完整示例：实现一个自定义卡牌](#五完整示例实现一个自定义卡牌)
5. [常见问题](#六常见问题)
6. [参考资源](#七参考资源)

---

## 一、关键字系统 (Keyword System)

### 1.1 核心文件

| 文件 | 路径 | 作用 |
|------|------|------|
| `CardKeyword.cs` | `MegaCrit.Sts2.Core.Entities.Cards` | 定义关键字枚举 |
| `Keywords.cs` | `MegaCrit.Sts2.GameInfo.Objects` | 关键字配置数据类 |
| `CardKeywordExtensions.cs` | `MegaCrit.Sts2.Core.Entities.Cards` | 关键字扩展方法 |

### 1.2 定义新关键字

#### 步骤 1: 在枚举中添加关键字

```csharp
// File: MegaCrit.Sts2.Core.Entities.Cards/CardKeyword.cs
namespace MegaCrit.Sts2.Core.Entities.Cards;

public enum CardKeyword
{
    None,
    Exhaust,
    Ethereal,
    Innate,
    Unplayable,
    Retain,
    Sly,
    Eternal,
    // 添加你的新关键字
    MyCustomKeyword
}
```

#### 步骤 2: 添加本地化配置

```csharp
// File: MegaCrit.Sts2.Core.Entities.Cards/CardKeywordExtensions.cs
internal static class CardKeywordExtensions
{
    // 扩展现有方法即可，不需要修改代码
    // 系统会自动根据枚举名称生成本地化键
    // 例如 MyCustomKeyword -> "my-custom-keyword"
}
```

#### 步骤 3: 添加本地化文本

在 `localization/eng/` 目录下的 `cards.json` 或 `keywords.json` 中添加:

```json
{
  "my-custom-keyword": {
    "title": "MyKeyword",
    "description": "This is what my keyword does."
  }
}
```

#### 步骤 4: 在卡牌中使用关键字

```csharp
public sealed class MyCard : CardModel
{
    // 重写 CanonicalKeywords 属性
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.MyCustomKeyword };

    public MyCard()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }
}
```

### 1.3 关键字的动态判断

某些关键字可能需要运行时判断（如 Retain 可以被临时赋予）:

```csharp
// 在 CardModel 中，ShouldRetainThisTurn 属性会检查：
// 1. 卡牌是否有 Retain 关键字
// 2. 或者是否有 HasSingleTurnRetain 标记

// 给卡牌临时添加 Retain 效果
public void GrantTemporaryRetain()
{
    HasSingleTurnRetain = true;
}
```

---

## 二、卡牌动态字段系统

### 2.1 核心概念

STS2 使用 `DynamicVar` 系统来管理卡牌上可变化的数值字段（如伤害、格挡等）。

#### 核心类:

| 类 | 作用 |
|----|------|
| `DynamicVar` | 动态变量基类，存储 BaseValue, EnchantedValue, PreviewValue |
| `DynamicVarSet` | 动态变量集合，管理卡牌的所有动态变量 |
| `DamageVar` | 伤害变量示例 |
| `BlockVar` | 格挡变量示例 |
| `CalculatedVar` | 计算型变量基类，支持基于其他变量的复杂计算 |

### 2.2 内置动态变量类型

```csharp
// 基础数值变量
public class DamageVar : DynamicVar           // 伤害
public class BlockVar : DynamicVar            // 格挡
public class ExtraDamageVar : DynamicVar      // 额外伤害
public class EnergyVar : DynamicVar           // 能量

// 计算型变量（基于其他变量计算）
public class CalculatedDamageVar : CalculatedVar  // 计算伤害
public class CalculatedBlockVar : CalculatedVar   // 计算格挡

// 其他特殊变量
public class PowerVar<T> : DynamicVar         // 能力值引用
public class CardsVar : DynamicVar            // 卡牌数量
public class GoldVar : DynamicVar             // 金币
public class HealVar : DynamicVar             // 治疗
```

### 2.3 为卡牌添加动态字段

#### 基础示例 - 简单伤害卡牌

```csharp
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

public sealed class MyAttackCard : CardModel
{
    // 定义标签
    protected override HashSet<CardTag> CanonicalTags =>
        new HashSet<CardTag> { CardTag.Strike };

    // 定义动态变量 - 初始伤害值为6
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DamageVar(6m, ValueProp.Move) };

    public MyAttackCard()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 使用 DynamicVars.Damage.BaseValue 获取当前伤害值
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升级时增加3点伤害
        base.DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
```

#### 进阶示例 - 多个动态变量

```csharp
public sealed class MyComplexCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),           // 基础伤害
        new BlockVar(5m, ValueProp.Move),            // 基础格挡
        new ExtraDamageVar(2m)                        // 额外伤害（用于计算）
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 造成伤害
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .Execute(choiceContext);

        // 获得格挡
        await BlockCmd.BlockFor(base.DynamicVars.Block.BaseValue)
            .FromCard(this)
            .Targeting(Owner.Creature)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars.Block.UpgradeValueBy(2m);
    }
}
```

#### 高级示例 - 计算型变量

```csharp
public sealed class PerfectedStrike : CardModel
{
    protected override HashSet<CardTag> CanonicalTags =>
        new HashSet<CardTag> { CardTag.Strike };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(6m),      // 基础伤害值
        new ExtraDamageVar(2m),           // 每把打击牌增加的额外伤害
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, target) =>
            // 计算卡组中打击牌的数量
            card.Owner.PlayerCombatState.AllCards.Count(c => c.Tags.Contains(CardTag.Strike))
        )
    };

    public PerfectedStrike()
        : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 使用计算后的伤害值
        await DamageCmd.Attack(base.DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }
}
```

### 2.4 创建自定义动态变量

#### 继承 DynamicVar 创建简单变量

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

public class CustomCountVar : DynamicVar
{
    public const string defaultName = "CustomCount";

    public CustomCountVar(decimal value)
        : base("CustomCount", value)
    {
    }

    // 重写此方法以在卡牌预览时更新数值
    public override void UpdateCardPreview(
        CardModel card,
        CardPreviewMode previewMode,
        Creature? target,
        bool runGlobalHooks)
    {
        // 基础值
        decimal value = base.BaseValue;

        // 处理附魔效果
        EnchantmentModel enchantment = card.Enchantment;
        if (enchantment != null)
        {
            // 应用附魔加成
            value = enchantment.ApplyToValue(value);
            if (!card.IsEnchantmentPreview)
            {
                base.EnchantedValue = value;
            }
        }

        // 设置预览值（用于UI显示）
        base.PreviewValue = value;
    }
}
```

#### 继承 CalculatedVar 创建计算型变量

```csharp
public class MyCalculatedVar : CalculatedVar
{
    public const string defaultName = "MyCalculated";

    public MyCalculatedVar()
        : base("MyCalculated")
    {
    }

    public override void UpdateCardPreview(
        CardModel card,
        CardPreviewMode previewMode,
        Creature? target,
        bool runGlobalHooks)
    {
        // 计算最终值
        base.PreviewValue = Calculate(target);
    }

    protected override DynamicVar GetBaseVar()
    {
        // 指定基础变量
        return ((CardModel)_owner).DynamicVars.CalculationBase;
    }

    protected override DynamicVar GetExtraVar()
    {
        // 指定额外变量
        return ((CardModel)_owner).DynamicVars.ExtraDamage;
    }
}
```

### 2.5 动态变量与描述文本

#### 描述文本中的变量替换

卡牌描述使用 SmartFormat 库进行动态文本替换。

**本地化文件示例** (`cards.json`):

```json
{
  "my-attack-card": {
    "title": "My Attack",
    "description": "Deal {Damage} damage. Gain {Block} Block."
  }
}
```

**系统会自动将 DynamicVar 添加到描述中**:

```csharp
// CardModel.GetDescriptionForPile() 中的逻辑:
private string GetDescriptionForPile(PileType pileType, DescriptionPreviewType previewType, Creature? target = null)
{
    LocString description = Description;
    DynamicVars.AddTo(description);  // 自动添加所有动态变量
    // ... 其他处理
    return description.GetFormattedText();
}
```

#### 条件显示与格式化

```json
{
  "my-conditional-card": {
    "title": "Conditional Card",
    "description": "Deal {Damage} damage.{if:IsUpgraded}\nUpgrade: Deal {ExtraDamage} more damage.{/if:IsUpgraded}"
  }
}
```

#### 在代码中添加额外描述参数

```csharp
protected override void AddExtraArgsToDescription(LocString description)
{
    // 添加自定义变量
    description.Add("CustomValue", 42);
    description.Add("IsEmpowered", IsEmpowered);

    // 添加其他LocString
    description.Add("EffectText", new LocString("cards", "my-effect-text"));
}
```

### 2.6 动态变量的生命周期

```csharp
// 1. 卡牌创建时 - 初始化 CanonicalVars
DynamicVars = new DynamicVarSet(CanonicalVars);
DynamicVars.InitializeWithOwner(this);

// 2. 卡牌升级时 - 更新基础值
protected override void OnUpgrade()
{
    base.DynamicVars.Damage.UpgradeValueBy(3m);  // BaseValue += 3
}

// 3. 卡牌克隆时 - 复制变量
_dynamicVars = DynamicVars.Clone(this);

// 4. 战斗预览时 - 计算预览值
public void UpdateDynamicVarPreview(CardPreviewMode previewMode, Creature? target, DynamicVarSet dynamicVarSet)
{
    foreach (DynamicVar value in dynamicVarSet.Values)
    {
        value.UpdateCardPreview(this, previewMode, target, runGlobalHooks);
    }
}
```

### 2.7 动态变量的三个值

每个 `DynamicVar` 有三个重要的数值属性:

| 属性 | 说明 | 用途 |
|------|------|------|
| `BaseValue` | 基础值（升级后） | 卡牌的基础数值 |
| `EnchantedValue` | 附魔后的值 | 应用附魔加成后的值 |
| `PreviewValue` | 预览值 | 最终显示给玩家的数值（包含所有Buff/Debuff） |

```csharp
public class DynamicVar
{
    public decimal BaseValue { get; set; }       // 6 -> 9 (升级后)
    public decimal EnchantedValue { get; protected set; }  // 9 -> 11 (附魔+2)
    public decimal PreviewValue { get; set; }    // 11 -> 13 (力量+2)

    public int IntValue => (int)BaseValue;       // 用于卡牌逻辑计算
}
```

---

## 三、使用 DynamicVar 实现关键字系统

除了游戏内置的关键字（通过 `CardKeyword` 枚举），你还可以使用 `DynamicVar` 实现**带数值的可变化关键字**，如 "Shine 9"、"Ice 3" 等。

### 3.1 为什么要用 DynamicVar 实现关键字？

| 实现方式 | 适用场景 | 特点 |
|---------|---------|------|
| `CardKeyword` 枚举 | 无数值关键字（如 Exhaust、Innate） | 游戏内置，简单标记 |
| `SpireField` | 临时状态追踪 | 外部附加数据，不随卡牌克隆自动保留 |
| `DynamicVar` | **带数值的可变关键字** | 随卡牌生命周期管理，自动克隆，自动显示 |

**DynamicVar 的优势：**
- ✅ 自动处理卡牌克隆（升级、复制等）
- ✅ 自动显示在卡牌描述中
- ✅ 支持升级逻辑
- ✅ 与游戏数值系统无缝集成

### 3.2 创建关键字 DynamicVar

```csharp
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

/// <summary>
/// Shine 动态变量 - 用于"闪耀"关键字
/// 每次打出后减少，到0时卡牌被移除
/// </summary>
public class ShineVar : DynamicVar
{
    public const string DefaultName = "Shine";

    public ShineVar(decimal initialValue)
        : base(DefaultName, initialValue)
    {
    }

    // 可选：自定义预览更新逻辑
    public override void UpdateCardPreview(
        CardModel card,
        CardPreviewMode previewMode,
        Creature? target,
        bool runGlobalHooks)
    {
        // Shine 值不受附魔影响，直接显示基础值
        base.EnchantedValue = base.BaseValue;
        base.PreviewValue = base.BaseValue;
    }
}
```

### 3.3 在卡牌中使用关键字变量

```csharp
public sealed class ShineStrike : CardModel
{
    // 定义动态变量 - 包含 Shine 值
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(9m, ValueProp.Move),    // 伤害
        new ShineVar(9m)                       // Shine 关键字值
    };

    public ShineStrike()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .Execute(choiceContext);

        // 处理 Shine 关键字逻辑
        await HandleShine();
    }

    private async Task HandleShine()
    {
        // 获取当前 Shine 值
        int currentShine = (int)DynamicVars.Shine.BaseValue;

        // 减少 Shine 值
        currentShine--;
        DynamicVars.Shine.SetBaseValue(currentShine);

        // 如果 Shine 归零，移除卡牌
        if (currentShine <= 0)
        {
            await RemoveCardFromGame();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        // Shine 值通常不随升级改变
    }
}
```

### 3.4 DynamicVar 关键 API

```csharp
// 获取/设置基础值
DynamicVars.Shine.BaseValue = 5m;
int shine = (int)DynamicVars.Shine.BaseValue;

// 设置基础值（内部使用）
DynamicVars.Shine.SetBaseValue(5m);

// 获取当前值（用于显示）
decimal preview = DynamicVars.Shine.PreviewValue;

// 检查是否存在
bool hasShine = DynamicVars.Contains("Shine");
```

### 3.5 处理卡牌克隆

DynamicVar 会自动处理卡牌克隆，但需要注意：

```csharp
// 当卡牌被克隆时（如升级、复制）：
// 1. CardModel.Clone() 会调用 DynamicVarSet.Clone()
// 2. 每个 DynamicVar 的 Clone() 方法会被调用
// 3. 新卡牌获得独立的 DynamicVar 副本

// 如果你需要自定义克隆逻辑：
public class CustomShineVar : DynamicVar
{
    public override DynamicVar Clone()
    {
        // 创建新的实例，复制当前值
        var clone = new CustomShineVar(BaseValue);
        return clone;
    }
}
```

### 3.6 与 Harmony 补丁结合

如果需要在打出后统一处理关键字逻辑：

```csharp
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class ShineKeywordPatch
{
    static void Postfix(CardModel __instance)
    {
        // 检查卡牌是否有 Shine 变量
        if (!__instance.DynamicVars.Contains("Shine")) return;

        // 获取并减少 Shine 值
        var shineVar = __instance.DynamicVars.Shine;
        int newValue = (int)shineVar.BaseValue - 1;
        shineVar.SetBaseValue(newValue);

        if (newValue <= 0)
        {
            // Shine 耗尽，移除卡牌
            __instance.RemoveFromCurrentPile();
            __instance.RemoveFromState();
        }
    }
}
```

### 3.7 完整的关键字系统架构

```csharp
// ============ 1. 定义 DynamicVar ============
public class ShineVar : DynamicVar
{
    public const string DefaultName = "Shine";
    public ShineVar(decimal value) : base(DefaultName, value) { }
}

// ============ 2. Harmony 补丁处理逻辑 ============
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class ShineKeywordSystem
{
    static void Postfix(CardModel __instance)
    {
        if (!__instance.DynamicVars.Contains(ShineVar.DefaultName)) return;

        // 延迟执行，等待卡牌进入弃牌堆
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);

            int current = (int)__instance.DynamicVars.Shine.BaseValue - 1;
            __instance.DynamicVars.Shine.SetBaseValue(current);

            if (current <= 0)
            {
                __instance.RemoveFromCurrentPile();
                __instance.RemoveFromState();
            }
        });
    }
}

// ============ 3. 卡牌使用关键字 ============
public sealed class ShineCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(9m, ValueProp.Move),
        new ShineVar(9m)  // 只需添加这一行
    };

    // ... 其他代码
}
```

---

## 五、完整示例：实现一个自定义卡牌

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyMod.Cards;

public sealed class AdaptiveBlade : CardModel
{
    // ===== 1. 定义标签 =====
    protected override HashSet<CardTag> CanonicalTags =>
        new HashSet<CardTag> { CardTag.Strike };

    // ===== 2. 定义关键字 =====
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Sly };  // 狡猾：不消耗能量

    // ===== 3. 定义动态变量 =====
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(5m, ValueProp.Move),
        new ExtraDamageVar(3m)
    };

    // ===== 4. 构造函数 =====
    public AdaptiveBlade()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // ===== 5. 实现卡牌效果 =====
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 使用 BaseValue 获取当前数值
        int damage = (int)base.DynamicVars.Damage.BaseValue;

        // 如果有额外条件，增加伤害
        if (Owner.PlayerCombatState.Hand.Cards.Count > 5)
        {
            damage += (int)base.DynamicVars.ExtraDamage.BaseValue;
        }

        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    // ===== 6. 实现升级逻辑 =====
    protected override void OnUpgrade()
    {
        // 基础伤害 +2
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        // 额外伤害 +1
        base.DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }

    // ===== 7. (可选) 添加额外描述参数 =====
    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("HandSize", Owner?.PlayerCombatState?.Hand.Cards.Count ?? 0);
        description.Add("BonusActive", Owner?.PlayerCombatState?.Hand.Cards.Count > 5 ?? false);
    }
}
```

**对应的本地化文件** (`cards.json`):

```json
{
  "adaptive-blade": {
    "title": "Adaptive Blade",
    "description": "Deal {Damage} damage. If you have more than 5 cards in hand, deal {ExtraDamage} more damage."
  },
  "adaptive-blade-plus": {
    "title": "Adaptive Blade+",
    "description": "Deal {Damage} damage. If you have more than 5 cards in hand, deal {ExtraDamage} more damage."
  }
}
```

---

## 六、常见问题

### Q1: 如何让卡牌描述显示升级后的数值对比？

A: 系统会自动处理。`DynamicVar.WasJustUpgraded` 标记会触发高亮显示，绿色表示数值增加。

### Q2: 如何实现类似"造成X点伤害，X为你手牌数量"的效果？

A: 使用 `CalculatedVar` 或 `CalculatedDamageVar`:

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
{
    new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, target) =>
        card.Owner.PlayerCombatState.Hand.Cards.Count
    )
};
```

### Q3: 如何存储临时的状态（例如"这张牌本回合已使用过"）？

A: 在卡牌类中添加普通字段：

```csharp
public sealed class MyCard : CardModel
{
    private bool _wasUsedThisTurn;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_wasUsedThisTurn)
        {
            // 额外效果
        }
        _wasUsedThisTurn = true;
        // ...
    }

    // 回合结束时重置
    public void EndOfTurnReset()
    {
        _wasUsedThisTurn = false;
    }
}
```

### Q4: DynamicVar 和 CardInfo 中的 base_damage 有什么关系？

A: `CardInfo.BaseDamage` 是配置数据，用于卡牌设计工具。实际运行时，卡牌使用 `DynamicVar.BaseValue`。

```csharp
// CardInfo 用于配置
public class CardInfo
{
    public int BaseDamage { get; init; }  // 设计时数值
}

// CardModel 用于运行时
public class CardModel
{
    protected virtual IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DamageVar(6m, ValueProp.Move) };  // 运行时数值
}
```

---

## 七、参考资源

- **关键字枚举**: `MegaCrit.Sts2.Core.Entities.Cards/CardKeyword.cs`
- **动态变量基类**: `MegaCrit.Sts2.Core.Localization.DynamicVars/DynamicVar.cs`
- **动态变量集合**: `MegaCrit.Sts2.Core.Localization.DynamicVars/DynamicVarSet.cs`
- **卡牌模型**: `MegaCrit.Sts2.Core.Models/CardModel.cs`
- **计算型变量**: `MegaCrit.Sts2.Core.Localization.DynamicVars/CalculatedVar.cs`
- **简单卡牌示例**: `MegaCrit.Sts2.Core.Models.Cards/StrikeIronclad.cs`
- **复杂卡牌示例**: `MegaCrit.Sts2.Core.Models.Cards/PerfectedStrike.cs`
