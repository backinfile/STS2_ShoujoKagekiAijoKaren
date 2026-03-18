# STS2 Mod 卡牌与关键字实现完全指南

基于项目内文档综合整理：
- `STS2_Keyword_And_CardField_Guide.md` - 关键字和动态字段系统
- `BaseLib_StS2_Documentation.md` - BaseLib使用文档
- `Harmony_CSharp_Documentation.md` - Harmony补丁文档
- 项目实际代码 (`StrikeKaren.cs`, `Karen.cs`, `KarenCardPool.cs`)

---

## 目录

1. [卡牌基础实现](#一卡牌基础实现)
2. [关键字系统](#二关键字系统)
3. [卡牌效果扩展](#三卡牌效果扩展)
4. [高级卡牌效果](#四高级卡牌效果)
5. [Harmony增强卡牌](#五harmony增强卡牌)
6. [完整示例](#六完整示例)

---

## 一、卡牌基础实现

### 1.1 最简卡牌结构（参考 StrikeKaren.cs）

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

public sealed class StrikeKaren() : CardModel(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    // ===== 1. 定义标签（如 Strike）=====
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    // ===== 2. 定义动态变量（伤害/格挡等）=====
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    // ===== 3. 实现卡牌效果 =====
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    // ===== 4. 实现升级逻辑 =====
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
```

### 1.2 卡牌构造函数参数

```csharp
public CardModel(
    int baseCost,           // 基础能量消耗
    CardType type,          // 卡牌类型：Attack/Skill/Power
    CardRarity rarity,      // 稀有度：Basic/Common/Uncommon/Rare
    TargetType target,      // 目标类型
    bool showInCardLibrary = true  // 是否显示在图鉴
)
```

**CardType 类型：**
- `CardType.Attack` - 攻击牌
- `CardType.Skill` - 技能牌
- `CardType.Power` - 能力牌

**CardRarity 稀有度：**
- `CardRarity.Basic` - 基础牌（初始卡组）
- `CardRarity.Common` - 普通
- `CardRarity.Uncommon` - 罕见
- `CardRarity.Rare` - 稀有

**TargetType 目标类型：**
- `TargetType.AnyEnemy` - 任意敌人
- `TargetType.AllEnemy` - 所有敌人
- `TargetType.Self` - 自己
- `TargetType.None` - 无目标

### 1.3 动态变量（DynamicVar）系统

```csharp
// 基础变量
protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
{
    new DamageVar(10m, ValueProp.Move),      // 伤害
    new BlockVar(5m, ValueProp.Move),        // 格挡
    new ExtraDamageVar(3m),                   // 额外伤害
    new HealVar(5m),                          // 治疗
};
```

**使用动态变量：**

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    // 获取数值（使用 BaseValue）
    int damage = (int)DynamicVars.Damage.BaseValue;
    int block = (int)DynamicVars.Block.BaseValue;

    // 造成伤害
    await DamageCmd.Attack(damage)
        .FromCard(this)
        .Targeting(cardPlay.Target!)
        .Execute(choiceContext);

    // 获得格挡
    await BlockCmd.BlockFor(block)
        .FromCard(this)
        .Targeting(Owner.Creature)
        .Execute(choiceContext);
}
```

**升级动态变量：**

```csharp
protected override void OnUpgrade()
{
    // 增加基础值
    DynamicVars.Damage.UpgradeValueBy(4m);  // +4伤害
    DynamicVars.Block.UpgradeValueBy(2m);   // +2格挡
}
```

---

## 二、关键字系统

### 2.1 使用现有关键字

```csharp
public sealed class MyCard : CardModel
{
    // 定义关键字
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Exhaust, CardKeyword.Innate };

    public MyCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }
}
```

**可用关键字（CardKeyword）：**
- `Exhaust` - 消耗（使用后移除）
- `Ethereal` - 虚无（回合结束消耗）
- `Innate` - 固有（开局在手牌）
- `Unplayable` - 无法打出
- `Retain` - 保留（保留到下一回合）
- `Sly` - 狡猾（不消耗能量）
- `Eternal` - 永恒

### 2.2 实现自定义关键字（需要Harmony补丁）

由于 `CardKeyword` 是枚举，无法直接扩展。需要通过其他方式实现：

#### 方法1：使用卡牌标签（CardTag）+ 补丁

```csharp
// 步骤1：定义标签
public static class MyCardTags
{
    public static readonly CardTag Recursive = new("Recursive");
}

// 步骤2：卡牌使用标签
public sealed class RecursiveCard : CardModel
{
    protected override HashSet<CardTag> CanonicalTags =>
        new() { MyCardTags.Recursive };
}

// 步骤3：Harmony补丁实现关键字效果
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class RecursiveKeywordPatch
{
    static void Postfix(CardModel __instance)
    {
        if (__instance.Tags.Contains(MyCardTags.Recursive))
        {
            // 实现"递归"效果：将一张复制加入弃牌堆
            // ...
        }
    }
}
```

#### 方法2：使用 SpireField 追踪状态

```csharp
using BaseLib.Utils;

// 定义字段追踪"冻结"状态（类似关键字）
public static class FrozenKeyword
{
    private static readonly SpireField<CardModel, bool> _isFrozen = new(() => false);

    public static bool IsFrozen(this CardModel card) => _isFrozen.Get(card);
    public static void SetFrozen(this CardModel card, bool frozen) => _isFrozen.Set(card, frozen);
}

// 在卡牌中使用
public sealed class IceCard : CardModel
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 给目标卡牌添加"冻结"效果
        var targetCard = ...;
        targetCard.SetFrozen(true);
    }
}

// 补丁处理"冻结"效果
[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlay))]
public static class FrozenEffectPatch
{
    static void Postfix(CardModel __instance, ref bool __result)
    {
        if (__instance.IsFrozen())
        {
            __result = false; // 冻结的牌无法打出
        }
    }
}
```

### 2.3 在卡牌描述中显示自定义关键字

```json
// localization/eng/cards.json
{
  "ice-card": {
    "title": "Ice Shard",
    "description": "Deal {Damage} damage. Apply [FrozenKeyword] to a card in your hand."
  },
  "frozen-keyword": {
    "title": "Frozen",
    "description": "Frozen cards cannot be played."
  }
}
```

---

## 三、卡牌效果扩展

### 3.1 常见卡牌效果模式

#### 多段伤害

```csharp
public sealed class MultiStrike : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(3m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 攻击3次
        for (int i = 0; i < 3; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);
        }
    }
}
```

#### AOE（范围伤害）

```csharp
public sealed class Whirlwind : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 对所有敌人造成伤害
        foreach (var enemy in Owner.PlayerCombatState.Enemies)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(enemy)
                .Execute(choiceContext);
        }
    }
}
```

#### 条件效果

```csharp
public sealed class FinishingBlow : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new ExtraDamageVar(10m)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int damage = (int)DynamicVars.Damage.BaseValue;

        // 如果目标血量低于25%，造成额外伤害
        if (cardPlay.Target!.Health <= cardPlay.Target.MaxHealth * 0.25f)
        {
            damage += (int)DynamicVars.ExtraDamage.BaseValue;
        }

        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }
}
```

#### 抽牌效果

```csharp
public sealed class QuickDraw : CardModel
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 抽2张牌
        await CardPileCmd.Draw(Owner, 2).Execute(choiceContext);
    }
}
```

#### 获得能量

```csharp
public sealed class Energize : CardModel
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得2点能量
        await EnergyCmd.Gain(Owner, 2).Execute(choiceContext);
    }
}
```

### 3.2 使用 CalculatedVar 实现动态数值

```csharp
public sealed class PerfectedStrike : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(6m),      // 基础伤害
        new ExtraDamageVar(2m),           // 每把打击牌+2
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, target) =>
            // 计算卡组中打击牌数量
            card.Owner.PlayerCombatState.AllCards.Count(c => c.Tags.Contains(CardTag.Strike))
        )
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 使用计算后的伤害值
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }
}
```

对应的本地化文本：

```json
{
  "perfected-strike": {
    "description": "Deal {CalculatedDamage} damage. Deals {ExtraDamage} additional damage for each Strike in your deck."
  }
}
```

### 3.3 卡牌效果链（链式调用）

```csharp
public sealed class ComplexCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new BlockVar(5m, ValueProp.Move)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 获得格挡
        await BlockCmd.BlockFor(DynamicVars.Block.BaseValue)
            .FromCard(this)
            .Targeting(Owner.Creature)
            .Execute(choiceContext);

        // 3. 抽牌
        await CardPileCmd.Draw(Owner, 1).Execute(choiceContext);

        // 4. 获得能量
        await EnergyCmd.Gain(Owner, 1).Execute(choiceContext);

        // 5. 添加卡牌到手牌
        var newCard = ModelDb.Card<SomeCard>().ToMutable(Owner);
        await CardPileCmd.Add(newCard, Owner.PlayerCombatState.Hand).Execute(choiceContext);
    }
}
```

---

## 四、高级卡牌效果

### 4.1 自定义 DynamicVar（用于复杂数值）

```csharp
// 创建自定义变量：每有一张手牌就+1伤害
public class HandSizeDamageVar : DynamicVar
{
    public HandSizeDamageVar(decimal baseValue) : base("HandSizeDamage", baseValue)
    {
    }

    public override void UpdateCardPreview(
        CardModel card,
        CardPreviewMode previewMode,
        Creature? target,
        bool runGlobalHooks)
    {
        // 基础值 + 手牌数量
        int handSize = card.Owner?.PlayerCombatState?.Hand.Cards.Count ?? 0;
        PreviewValue = BaseValue + handSize;
    }
}

// 在卡牌中使用
public sealed class HandBlade : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HandSizeDamageVar(5m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars["HandSizeDamage"].BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .Execute(choiceContext);
    }
}
```

### 4.2 在描述中显示额外信息

```csharp
public sealed class InfoCard : CardModel
{
    protected override void AddExtraArgsToDescription(LocString description)
    {
        // 添加自定义变量到描述
        description.Add("HandSize", Owner?.PlayerCombatState?.Hand.Cards.Count ?? 0);
        description.Add("StrikesInDeck",
            Owner?.PlayerCombatState?.AllCards.Count(c => c.Tags.Contains(CardTag.Strike)) ?? 0);

        // 添加布尔值控制条件显示
        description.Add("HasBuff", Owner?.Creature?.HasPower<StrengthPower>() ?? false);
    }
}
```

对应的本地化：

```json
{
  "info-card": {
    "description": "Deal {Damage} damage. You have {HandSize} cards in hand. {if:HasBuff}\nYou have Strength!{/if:HasBuff}"
  }
}
```

### 4.3 卡牌状态追踪（跨回合）

```csharp
using BaseLib.Utils;

public sealed class GrowingCard : CardModel
{
    // 使用 SpireField 追踪每张卡的播放次数
    private static readonly SpireField<CardModel, int> _playCount = new(() => 0);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = _playCount.Get(this);

        // 每次播放增加伤害
        int damage = 5 + count * 2;
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .Execute(choiceContext);

        // 增加计数
        _playCount.Set(this, count + 1);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("PlayCount", _playCount.Get(this));
    }
}
```

---

## 五、Harmony增强卡牌

### 5.1 全局修改卡牌行为

```csharp
using HarmonyLib;

// 修改所有攻击牌的伤害
[HarmonyPatch(typeof(DamageCmd), nameof(DamageCmd.Attack))]
public static class GlobalDamageBoostPatch
{
    static void Prefix(ref decimal amount, DamageCmd __instance)
    {
        // 检查是否来自卡牌
        if (__instance.Source is CardModel card)
        {
            // 如果玩家有某种遗物，增加伤害
            if (card.Owner?.HasRelic<MyDamageRelic>() == true)
            {
                amount *= 1.5m;
            }
        }
    }
}
```

### 5.2 修改卡牌可玩性

```csharp
[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlay))]
public static class CustomCanPlayPatch
{
    static void Postfix(CardModel __instance, ref bool __result)
    {
        // 自定义条件：某些卡牌只能在特定条件下打出
        if (__instance is MyConditionalCard &&
            __instance.Owner?.PlayerCombatState?.Hand.Cards.Count < 3)
        {
            __result = false; // 手牌少于3张时无法打出
        }
    }
}
```

### 5.3 卡牌打出后效果

```csharp
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class PostPlayEffectPatch
{
    static void Postfix(CardModel __instance)
    {
        // 所有带有特定标签的卡牌打出后触发效果
        if (__instance.Tags.Contains(MyCardTags.Chain))
        {
            // 连锁效果：抽一张牌
            CardPileCmd.Draw(__instance.Owner, 1).Execute(__instance.Owner.RunState);
        }
    }
}
```

---

## 六、完整示例

### 6.1 完整的自定义卡牌类

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
///  Karen的招牌攻击： radiant slash
///  造成中等伤害，如果手牌数>5则造成额外伤害
/// </summary>
public sealed class RadiantSlash : CardModel
{
    // ===== 1. 标签和关键字 =====
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    // 无特殊关键字
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    // ===== 2. 动态变量 =====
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),      // 基础伤害
        new ExtraDamageVar(5m)                    // 额外伤害（手牌>5时）
    };

    // ===== 3. 构造函数 =====
    public RadiantSlash() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy
    )
    {
    }

    // ===== 4. 卡牌效果 =====
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 计算伤害
        int damage = (int)DynamicVars.Damage.BaseValue;
        int handSize = Owner.PlayerCombatState.Hand.Cards.Count;

        // 条件：手牌>5时增加伤害
        if (handSize > 5)
        {
            damage += (int)DynamicVars.ExtraDamage.BaseValue;
        }

        // 造成伤害（带特效）
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .WithHitVfxNode(target => NSlashVfx.Create(target))
            .Execute(choiceContext);

        // 抽一张牌（作为奖励）
        await CardPileCmd.Draw(Owner, 1).Execute(choiceContext);
    }

    // ===== 5. 升级 =====
    protected override void OnUpgrade()
    {
        // 基础伤害 +3，额外伤害 +2
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.ExtraDamage.UpgradeValueBy(2m);
    }

    // ===== 6. 描述增强 =====
    protected override void AddExtraArgsToDescription(LocString description)
    {
        int handSize = Owner?.PlayerCombatState?.Hand.Cards.Count ?? 0;
        description.Add("CurrentHandSize", handSize);
        description.Add("BonusActive", handSize > 5);
    }
}
```

### 6.2 对应的本地化文件

```json
// localization/eng/cards.json
{
  "radiant-slash": {
    "title": "Radiant Slash",
    "description": "Deal {Damage} damage. Draw 1 card. If you have more than 5 cards in hand, deal {ExtraDamage} more damage.",
    "selectionScreenPrompt": "Select a target for Radiant Slash."
  },
  "radiant-slash-plus": {
    "title": "Radiant Slash+",
    "description": "Deal {Damage} damage. Draw 1 card. If you have more than 5 cards in hand, deal {ExtraDamage} more damage."
  }
}
```

### 6.3 添加到卡牌池

```csharp
// KarenCardPool.cs
public sealed class KarenCardPool : CardPoolModel
{
    public override string Title => "karen";
    public override string EnergyColorName => "karen";
    public override string CardFrameMaterialPath => "card_frame_purple";
    public override Color DeckEntryCardColor => new("3EB3ED");
    public override Color EnergyOutlineColor => new("1D5673");
    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards()
    {
        return
        [
            ModelDb.Card<StrikeKaren>(),
            ModelDb.Card<DefendKaren>(),
            ModelDb.Card<RadiantSlash>(),  // 添加新卡牌
            // ... 其他卡牌
        ];
    }

    protected override IEnumerable<CardModel> FilterThroughEpochs(UnlockState unlockState, IEnumerable<CardModel> cards)
    {
        return cards.ToList();
    }
}
```

---

## 七、快速参考

### 7.1 常用 DamageCmd 参数

```csharp
await DamageCmd.Attack(damage)
    .FromCard(this)                    // 来源（用于遗物触发）
    .Targeting(target)                 // 目标
    .WithHitFx("vfx/...")              // 击中特效
    .WithHitSfx("sfx/...")             // 击中音效
    .WithHitVfxNode(t => new Vfx())   // 自定义视觉效果
    .WithHitCount(3)                   // 多段伤害次数
    .Execute(choiceContext);
```

### 7.2 常用 BlockCmd 参数

```csharp
await BlockCmd.BlockFor(block)
    .FromCard(this)
    .Targeting(Owner.Creature)         // 对自己使用
    .Execute(choiceContext);
```

### 7.3 卡牌操作

```csharp
// 抽牌
await CardPileCmd.Draw(Owner, count).Execute(choiceContext);

// 添加卡牌到手牌
var card = ModelDb.Card<MyCard>().ToMutable(Owner);
await CardPileCmd.Add(card, Owner.PlayerCombatState.Hand).Execute(choiceContext);

// 添加卡牌到弃牌堆
await CardPileCmd.Add(card, Owner.PlayerCombatState.Discard).Execute(choiceContext);

// 获得能量
await EnergyCmd.Gain(Owner, amount).Execute(choiceContext);
```

### 7.4 检查游戏状态

```csharp
// 战斗中
bool inCombat = CombatManager.Instance.IsInProgress;

// 当前回合数
int turn = CombatManager.Instance.CurrentTurn;

// 手牌数
int handSize = Owner.PlayerCombatState.Hand.Cards.Count;

// 检查是否有某种能力
bool hasStrength = Owner.Creature.HasPower<StrengthPower>();
int strengthAmount = Owner.Creature.GetPower<StrengthPower>()?.Amount ?? 0;

// 检查遗物
bool hasRelic = Owner.HasRelic<MyRelic>();
```

---

## 参考资源

- **项目文档**：`docs/STS2_Keyword_And_CardField_Guide.md`
- **BaseLib文档**：`docs/BaseLib_StS2_Documentation.md`
- **Harmony文档**：`docs/Harmony_CSharp_Documentation.md`
- **BaseLib Wiki**: https://alchyr.github.io/BaseLib-Wiki/
- **Mod 模板**: https://github.com/Alchyr/ModTemplate-StS2
