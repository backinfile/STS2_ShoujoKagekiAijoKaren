# STS2 本体「黏糊 (Goopy)」附魔实现调研

## 一、概述

| 项目 | 内容 |
|------|------|
| 中文名 | 黏糊 |
| 英文名 | Goopy |
| 键名 | `GOOPY` |
| 可附魔对象 | **仅 `Defend` 标签的卡牌** |
| 核心效果 | 1. 给卡牌添加 `消耗 (Exhaust)` 关键词<br>2. 每次打出后，该牌的**格挡值永久 +1** |

---

## 二、核心实现

### 2.1 附魔类

**文件**：`src/Core/Models/Enchantments/Goopy.cs`

```csharp
public sealed class Goopy : EnchantmentModel
{
    public override bool HasExtraCardText => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        new global::_003C_003Ez__ReadOnlySingleElementList<IHoverTip>(
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust));

    // 只能给带有 Defend 标签的牌附魔
    public override bool CanEnchant(CardModel card)
    {
        if (base.CanEnchant(card))
        {
            return card.Tags.Contains(CardTag.Defend);
        }
        return false;
    }

    // 附魔时添加 Exhaust 关键词
    protected override void OnEnchant()
    {
        base.Card.AddKeyword(CardKeyword.Exhaust);
    }

    // 打出后：Amount++，同步到 DeckVersion
    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card != base.Card)
        {
            return Task.CompletedTask;
        }
        base.Amount++;
        if (base.Card.DeckVersion != null)
        {
            base.Card.DeckVersion.Enchantment.Amount++;
        }
        return Task.CompletedTask;
    }

    // 格挡加成：返回 Amount - 1
    public override decimal EnchantBlockAdditive(decimal originalBlock, ValueProp props)
    {
        if (!props.IsPoweredCardOrMonsterMoveBlock())
        {
            return 0m;
        }
        return base.Amount - 1;
    }
}
```

#### 关键机制解析

| 方法 | 作用 |
|------|------|
| `CanEnchant` | 限制附魔范围：必须是 `CardTag.Defend` 卡牌。基类已排除诅咒/状态/不可打出等牌。 |
| `OnEnchant` | 附魔生效时调用，给卡牌添加 `CardKeyword.Exhaust`。 |
| `AfterCardPlayed` | 卡牌打出后触发，`Amount` 永久 +1，并同步到 `DeckVersion`（确保跨战斗持久化）。 |
| `EnchantBlockAdditive` | 在计算格挡时附加 `Amount - 1` 的额外格挡。注意初始 `Amount` 通常为 1，所以第一次打出时不加格挡，第二次开始 +1、+2…… |

> **为什么 `EnchantBlockAdditive` 返回 `Amount - 1`？**
> 
> 因为 `AfterCardPlayed` 是在**打出后**增加 `Amount`。初始附魔时 `Amount = 1`：
> - 第 1 次打出 → 格挡加成 `1 - 1 = 0` → 打出后 `Amount` 变为 2
> - 第 2 次打出 → 格挡加成 `2 - 1 = 1` → 打出后 `Amount` 变为 3
> - 第 3 次打出 → 格挡加成 `3 - 1 = 2`
>
> 这是一个「越打越强」的线性成长附魔。

#### 加成调用链

格挡计算在以下两处调用 `EnchantmentModel.EnchantBlockAdditive`：
- `BlockVar.cs:34` —— 动态变量计算格挡值
- `CalculatedBlockVar.cs:28, 48` —— 预览/实际计算格挡

---

### 2.2 附魔基类关键接口

**文件**：`src/Core/Models/EnchantmentModel.cs`

开发自定义附魔时，需要 override 的关键虚方法：

```csharp
public abstract class EnchantmentModel : AbstractModel
{
    public virtual bool HasExtraCardText => false;          // 是否显示额外卡牌文本
    public virtual bool CanEnchant(CardModel card) { ... }  // 附魔 eligibility
    protected virtual void OnEnchant() { }                   // 附魔生效时的修改
    public virtual Task AfterCardPlayed(...) { ... }         // 打出后触发
    public virtual decimal EnchantBlockAdditive(...) { ... } // 格挡附加
    public virtual decimal EnchantDamageAdditive(...) { ... }// 伤害附加
    // ... 还有其他 EnchantXXX 方法
}
```

---

## 三、视觉特效

### 3.1 特效节点

**文件**：`src/Core/Nodes/Vfx/NGoopyImpactVfx.cs`

```csharp
public partial class NGoopyImpactVfx : Node2D
{
    public static readonly string scenePath = SceneHelper.GetScenePath("vfx/vfx_goopy_impact");

    // 在 Creature 身上播放（默认绿色）
    public static NGoopyImpactVfx? Create(Creature creature)
    {
        ...
        return Create(nCreature.VfxSpawnPosition, Colors.Green);
    }

    // 在指定位置播放，可改颜色
    public static NGoopyImpactVfx? Create(Vector2 targetCenterPosition, Color tint)
    {
        ...
        nGoopyImpactVfx.ModulateParticles(tint);
        return nGoopyImpactVfx;
    }
}
```

- `scenePath` 指向 `vfx/vfx_goopy_impact.tscn`
- 特效默认颜色为 **绿色 (`Colors.Green`)**
- 在 `_Ready` 中启动粒子动画，3.5 秒后自动 `QueueFree`

### 3.2 预加载注册

**文件**：`src/Core/Commands/VfxCmd.cs:103`

```csharp
NGoopyImpactVfx.scenePath,  // 在 preload 列表中注册
```

---

## 四、使用 Goopy 特效的相关卡牌

「黏糊」特效不仅是附魔的专属 VFX，还被以下卡牌复用：

### 4.1 Slimed（黏液/黏稠）

**文件**：`src/Core/Models/Cards/Slimed.cs`

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    NGoopyImpactVfx nGoopyImpactVfx = NGoopyImpactVfx.Create(base.Owner.Creature);
    if (nGoopyImpactVfx != null)
    {
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nGoopyImpactVfx);
    }
    await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
}
```

- 1 费 Status 牌，打出时给自己播放绿色 Goopy 特效，然后抽 1 张牌。

### 4.2 GunkUp（黏液喷射/污秽打击）

**文件**：`src/Core/Models/Cards/GunkUp.cs`

```csharp
await DamageCmd.Attack(...)
    .Targeting(cardPlay.Target)
    .WithHitVfxNode(NGoopyImpactVfx.Create)
    .Execute(choiceContext);

CardModel card = base.CombatState.CreateCard<Slimed>(base.Owner);
CardCmd.PreviewCardPileAdd(
    await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, addedByPlayer: true)
);
```

- 1 费 Common 攻击牌，攻击敌人时附带绿色 Goopy 命中特效，并将一张 `Slimed` 加入弃牌堆。

---

## 五、本地化键

**文件**：`localization/zhs/enchantments.json`

```json
{
  "GOOPY.title": "黏糊",
  "GOOPY.description": "这张牌获得[gold]消耗[/gold]。当被打出时，这张牌的[gold]格挡[/gold]值永久增加[blue]1[/blue]点。",
  "GOOPY.extraCardText": "这张牌的[gold]格挡[/gold]值永久增加[blue]1[/blue]点。"
}
```

---

## 六、开发借鉴要点

1. **附魔范围限制**：通过 override `CanEnchant` + 检查 `card.Tags` 实现。
2. **持久化数值增长**：在 `AfterCardPlayed` 中修改 `base.Amount`，并同步 `base.Card.DeckVersion.Enchantment.Amount`。
3. **修改卡牌关键词**：在 `OnEnchant` 中直接调用 `base.Card.AddKeyword(...)`。
4. **修改卡牌数值**：通过 override `EnchantBlockAdditive` / `EnchantDamageAdditive` 等钩子实现，而不是直接改 `DynamicVars`。
5. **VFX 复用**：`NGoopyImpactVfx` 的 `Create(Creature)` 和 `Create(Vector2, Color)` 两种工厂方法，既可供附魔调用，也可供卡牌调用。
