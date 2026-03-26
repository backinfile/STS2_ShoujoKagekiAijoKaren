# Karen 能力牌实现要求文档

## 概述

本文档记录Karen（爱城华恋）能力牌（Power Card）的实现规范，包括卡牌结构、Power扳机机制、命名规范等。

---

## 文件结构

### 1. 能力牌（CardModel）

**路径**: `src/Core/Models/Cards/shine/`

**命名规范**: `Karen{Name}Card.cs`

**必须继承**: `KarenBaseCardModel`

**基础结构**:
```csharp
public sealed class KarenXxxCard() : KarenBaseCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(3m, ValueProp.Move)  // 或 RepeatVar/DamageVar 等
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 给自己添加对应的Power
        await PowerCmd.Apply<KarenXxxPower>(
            Owner.Creature,
            DynamicVars.Block.BaseValue,  // 或具体数值
            Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
```

---

### 2. Power效果（PowerModel）

**路径**: `src/Core/Models/Powers/`

**命名规范**: `Karen{Name}Power.cs`

**继承选择**:
- 普通Power: 继承 `PowerModel`
- 约定牌堆相关Power: 继承 `KarenBasePower`（提供 `OnCardAddedToPromisePile` 和 `OnCardRemovedFromPromisePile` 虚函数）

---

## 已实现的能力牌

### 1. 信（Letter）- KarenLetterCard / KarenLetterPower

| 属性 | 值 |
|------|-----|
| 费用 | 1 |
| 稀有度 | 罕见 (Uncommon) |
| 类型 | Power |
| 目标 | 自己 |

**效果**: 每当你将一张卡牌放入约定牌堆，获得 X 点格挡。

**Power实现要点**:
- 继承 `KarenBasePower`
- 重写 `OnCardAddedToPromisePile(CardModel card)`
- 使用 `BlockCmd.GainBlock()` 获得格挡
- 调用 `Flash()` 触发Power闪烁

---

### 2. 星光（第二幕）- KarenStarlightCard / KarenStarlightPower

| 属性 | 值 |
|------|-----|
| 费用 | 1 |
| 稀有度 | 罕见 (Uncommon) |
| 类型 | Power |
| 目标 | 自己 |

**效果**: 打出闪耀牌时，额外打出 M 次，然后耗尽该牌的闪耀值。

**Power实现要点**:
- 继承 `PowerModel`
- 重写 `AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)`
- 检测 `card.IsShineCard()` 和 `card.HasShine()`
- 使用 `card.CreateClone()` 复制卡牌
- 手动复制闪耀值: `clone.SetShineMax()` 和 `clone.SetShineCurrent()`
- 使用 `CardCmd.Play()` 打出克隆牌
- 耗尽原牌闪耀值: `card.SetShineCurrent(0)`

---

### 3. 星光（第三幕）- KarenStarlight03Card / KarenStarlight03Power

| 属性 | 值 |
|------|-----|
| 费用 | 1 |
| 稀有度 | 稀有 (Rare) |
| 类型 | Power |
| 目标 | 自己 |

**效果**: 获得闪耀牌奖励（参考星星串起了我们的友谊）。

**Power实现要点**:
- 继承 `PowerModel`
- 静态方法 `AddShineCardReward(Player player)`
- 使用 `ShineManager.GetAllShineCards()` 获取所有闪耀牌
- 使用 `TakeRandom()` 随机选取
- 使用 `combatRoom.AddExtraReward()` 添加奖励

---

## 关键API参考

### 检测闪耀牌

```csharp
// 是否是闪耀牌（有Shine关键字）
card.IsShineCard()

// 是否有闪耀值（>0）
card.HasShine()

// 获取/设置闪耀值
int shine = card.GetShineValue();
card.SetShineCurrent(value);
card.SetShineMax(value);
```

### Power扳机（Hook）

```csharp
// 卡牌打出后（PowerModel基类虚函数）
public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)

// 约定牌堆相关（KarenBasePower虚函数）
public virtual Task OnCardAddedToPromisePile(CardModel card)
public virtual Task OnCardRemovedFromPromisePile(CardModel card)
```

### 创建卡牌奖励

```csharp
// 随机获取闪耀牌
var shineCard = ShineManager.GetAllShineCards()
    .Where(c => c is not KarenStarFriend)  // 排除特定卡牌
    .TakeRandom(1, player.PlayerRng.Rewards);

// 添加战斗奖励
if (player?.RunState?.CurrentRoom is CombatRoom combatRoom)
{
    combatRoom.AddExtraReward(player, new CardReward(shineCard, CardCreationSource.Encounter, player));
}
```

### 克隆卡牌（打出复制）

```csharp
// 创建战斗中的克隆
CardModel clone = originalCard.CreateClone();

// 复制闪耀值（CreateClone不自动复制SpireField）
clone.SetShineMax(originalCard.GetShineMaxValue());
clone.SetShineCurrent(originalCard.GetShineValue());

// 打出克隆牌
await CardCmd.Play(choiceContext, clone, target, AutoPlayType.Effect);
```

---

## 命名规范

| 类型 | 命名格式 | 示例 |
|------|----------|------|
| 卡牌 | Karen{Name}Card | KarenLetterCard |
| Power | Karen{Name}Power | KarenLetterPower |
| 文件路径 | Cards/shine/ 和 Powers/ | - |

---

## 待办清单（实现新能力牌）

- [ ] 将卡牌加入 `KarenCardPool.GenerateAllCards()`
- [ ] 添加中英文本地化（Card-Strings.json）
- [ ] 添加Power图标（images/powers/）
- [ ] 添加卡牌图片（images/cards/）

---

## 参考文件

- `KarenBaseCardModel.cs` - 卡牌基类
- `KarenBasePower.cs` - Power基类（约定牌堆扳机）
- `ShineExtension.cs` - 闪耀值操作扩展方法
- `ShineManager.cs` - 闪耀牌管理
- `KarenStarFriend.cs` - 参考实现（闪耀牌奖励）
