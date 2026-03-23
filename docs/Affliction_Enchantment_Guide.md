# Affliction（诅咒/缺陷）与 Enchantment（附魔）系统文档

## 概述

在《Slay the Spire 2》（STS2）中，卡牌可以携带两种特殊状态：**Affliction**（诅咒/缺陷）和 **Enchantment**（附魔）。

- **Affliction**：负面效果，通常由敌人施加给玩家的卡牌
- **Enchantment**：正面效果，通常通过游戏内机制（如事件、遗物）给卡牌添加

## 核心类结构

### 基类

| 类 | 文件路径 | 说明 |
|---|---------|------|
| `AfflictionModel` | `src/Core/Models/AfflictionModel.cs` | Affliction 基类 |
| `EnchantmentModel` | `src/Core/Models/EnchantmentModel.cs` | Enchantment 基类 |

### CardModel 中的属性

在 `CardModel` 中，这两个状态作为可空属性存在：

```csharp
public EnchantmentModel? Enchantment { get; private set; }  // 第512行
public AfflictionModel? Affliction { get; private set; }    // 第514行
```

相关事件：
```csharp
event Action? AfflictionChanged;   // Affliction 变化时触发
event Action? EnchantmentChanged;  // Enchantment 变化时触发
```

---

## Affliction（诅咒/缺陷）

### 定义

Affliction 是一种**负面**卡牌状态，通常由敌人施加，给卡牌带来负面效果。

### 核心特性

| 特性 | 说明 |
|-----|------|
| 单卡唯一 | 一张卡只能有一个 Affliction |
| 不可叠加不同类型 | 不同类型 Affliction 不能共存 |
| 可叠加同类型 | 如果 `IsStackable = true`，同类型可以叠加数值 |
| 可影响 Unplayable 卡 | 默认可以，可通过 `CanAfflictUnplayableCards` 控制 |

### AfflictionModel 关键属性

```csharp
public abstract class AfflictionModel : AbstractModel
{
    public const string locTable = "afflictions";  // 本地化表名

    public int Amount { get; set; }  // 数值（可叠加时有用）
    public CardModel Card { get; set; }  // 所属的卡牌
    public CombatState CombatState => Card.CombatState;  // 战斗状态

    public virtual bool CanAfflictUnplayableCards => true;  // 是否可施加于无法打出卡
    public virtual bool IsStackable => false;  // 是否可叠加
    public virtual bool HasExtraCardText => false;  // 是否在卡面显示额外文字
}
```

### 内置 Affliction 列表

| 类名 | 效果 | 叠加 | 特殊 |
|-----|------|-----|------|
| `Ringing` | 鸣响（打出时伤害自己） | 是 | 有额外卡面文字 |
| `Hexed` | 被诅咒（打出时施加虚弱给自己） | 否 | 检测 HexPower |
| `Smog` | 烟雾（打出时获得负面效果） | 否 | - |
| `Entangled` | 纠缠（打出时无法打出） | 否 | - |
| `Bound` | 绑定（打出时费用增加） | 否 | - |
| `Galvanized` | 电镀（打出时获得电击） | 是 | - |

### 应用 Affliction

```csharp
// 方法1：泛型方式（推荐）
await CardCmd.Afflict<Ringing>(card, amount);

// 方法2：实例方式
var affliction = ModelDb.Affliction<Ringing>().ToMutable();
await CardCmd.Afflict(affliction, card, amount);

// 批量施加并预览
await CardCmd.AfflictAndPreview<Ringing>(cards, amount, CardPreviewStyle.HorizontalLayout);
```

### 清除 Affliction

```csharp
CardCmd.ClearAffliction(card);
```

### 创建自定义 Affliction

```csharp
public sealed class MyAffliction : AfflictionModel
{
    public override bool IsStackable => true;  // 可叠加
    public override bool HasExtraCardText => true;  // 显示额外文字

    public override async Task OnPlay(PlayerChoiceContext choiceContext, Creature? target)
    {
        // 卡牌打出时触发的效果
        await base.OnPlay(choiceContext, target);
    }
}
```

---

## Enchantment（附魔）

### 定义

Enchantment 是一种**正面**卡牌状态，通常通过游戏机制（如事件、遗物、药水）给卡牌添加增益效果。

### 核心特性

| 特性 | 说明 |
|-----|------|
| 单卡唯一 | 一张卡只能有一个 Enchantment |
| 可叠加同类型 | 如果 `IsStackable = true`，同类型可以叠加数值 |
| 影响卡牌数值 | 可以修改伤害、格挡、能量费用等 |
| 可存档 | Enchantment 会被保存到存档中 |

### EnchantmentModel 关键属性

```csharp
public abstract class EnchantmentModel : AbstractModel
{
    public const string locTable = "enchantments";  // 本地化表名

    public int Amount { get; set; }  // 数值
    public CardModel Card { get; set; }  // 所属的卡牌
    public DynamicVarSet DynamicVars { get; }  // 动态变量系统
    public EnchantmentStatus Status { get; set; }  // 状态（Normal/Disabled）

    public virtual bool ShowAmount => false;  // 是否显示数值
    public virtual bool IsStackable => false;  // 是否可叠加
    public virtual bool HasExtraCardText => false;  // 是否有额外卡面文字
    public virtual bool ShouldGlowGold => false;  // 是否金色高亮
    public virtual bool ShouldGlowRed => false;  // 是否红色高亮

    // 附魔对卡牌数值的修改
    public virtual decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props) => 0m;
    public virtual decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props) => 1m;
    public virtual decimal EnchantBlockAdditive(decimal originalBlock, ValueProp props) => 0m;
    public virtual decimal EnchantBlockMultiplicative(decimal originalBlock, ValueProp props) => 1m;
    public virtual int EnchantPlayCount(int originalPlayCount) => originalPlayCount;
}
```

### 内置 Enchantment 列表

| 类名 | 效果 | 可叠加 | 限定卡类型 |
|-----|------|-------|-----------|
| `Sharp` | 伤害+X | 是 | 攻击牌 |
| `Swift` | 打出后抽X张牌 | 是 | 无 |
| `Corrupted` | 伤害+50%，但对自己造成2伤害 | 否 | 攻击牌 |
| `Glam` | 本战斗第一次打出时额外打出一次 | 否 | 无 |
| `Imbued` | 第一回合开始时自动打出 | 否 | 技能牌 |
| `SlumberingEssence` | 在手牌中时费用-1 | 否 | 无 |
| `Clone` | 打出时复制一张到手牌 | 是 | 无 |
| `SoulsPower` | 每有1层获得+1伤害 | 是 | 攻击牌 |
| `Spiral` | 打出后回到抽牌堆顶部 | 否 | 无 |
| `Sown` | 打出时抽X张牌 | 是 | 无 |
| `Favored` | 费用-1 | 是 | 无 |
| `Adroit` | 打出时获得X敏捷 | 是 | 无 |
| `Vigorous` | 打出时获得X力量 | 是 | 无 |

### 应用 Enchantment

```csharp
// 方法1：泛型方式（推荐）
CardCmd.Enchant<Sharp>(card, amount);

// 方法2：实例方式
var enchantment = ModelDb.Enchantment<Sharp>().ToMutable();
CardCmd.Enchant(enchantment, card, amount);
```

### 清除 Enchantment

```csharp
CardCmd.ClearEnchantment(card);
```

### 创建自定义 Enchantment

```csharp
public sealed class MyEnchantment : EnchantmentModel
{
    public override bool IsStackable => true;
    public override bool ShowAmount => true;

    // 修改伤害
    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        if (!props.IsPoweredAttack()) return 0m;
        return Amount;  // 伤害+Amount
    }

    // 打出时触发
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        // 自定义效果
    }
}
```

---

## 关键区别对比

| 特性 | Affliction | Enchantment |
|-----|-----------|-------------|
| **性质** | 负面效果 | 正面效果 |
| **来源** | 通常敌人施加 | 通常事件/遗物/药水施加 |
| **存档** | ❌ 不存档（战斗结束消失） | ✅ 存档（永久保留） |
| **数值修改** | 通常不修改 | 可以修改伤害/格挡/费用等 |
| **卡面高亮** | 无 | 可金色/红色高亮 |
| **额外文字** | 可以显示 | 可以显示 |
| **图标** | Overlay 覆盖层 | Icon 图标 |
| **状态系统** | 无 | 有（Normal/Disabled） |

---

## 存档与复制

### 存档行为

- **Enchantment**：会被保存到 `SerializableCard` 中，加载时自动恢复
  ```csharp
  // CardModel.ToSerializable()
  Enchantment = Enchantment?.ToSerializable(),

  // CardModel.FromSerializable()
  if (save.Enchantment != null)
  {
      cardModel.EnchantInternal(EnchantmentModel.FromSerializable(save.Enchantment), ...);
  }
  ```

- **Affliction**：**不会**被保存，战斗结束时自动清除

### 复制卡牌时的行为

在 `DeepCloneFields()` 中，两者都会被正确复制：

```csharp
if (Enchantment != null)
{
    EnchantmentModel enchantmentModel = (EnchantmentModel)Enchantment.ClonePreservingMutability();
    Enchantment = null;
    EnchantInternal(enchantmentModel, enchantmentModel.Amount);
}
if (Affliction != null)
{
    AfflictionModel afflictionModel = (AfflictionModel)Affliction.ClonePreservingMutability();
    Affliction = null;
    AfflictInternal(afflictionModel, afflictionModel.Amount);
}
```

---

## 本地化

### Affliction 本地化键

```json
{
  "ringing.title": "鸣响",
  "ringing.description": "这张卡打出时，对自己造成 {Amount} 点伤害。",
  "ringing.extraCardText": "鸣响 {Amount}。"
}
```

### Enchantment 本地化键

```json
{
  "sharp.title": "锋利",
  "sharp.description": "伤害 +{Amount}。",
  "sharp.extraCardText": "锋利 {Amount}。"
}
```

### 资源路径

- **Affliction Overlay**: `scenes/cards/overlays/afflictions/{id}.tscn`
- **Enchantment Icon**: `images/enchantments/{id}.png`

---

## 卡牌打出流程中的触发

在 `CardModel.OnPlayWrapper()` 中，Affliction 和 Enchantment 的触发顺序：

```csharp
// 1. 卡牌自身效果
await OnPlay(choiceContext, cardPlay);

// 2. Enchantment 效果
if (Enchantment != null)
{
    await Enchantment.OnPlay(choiceContext, cardPlay);
}

// 3. Affliction 效果
if (Affliction != null)
{
    await Affliction.OnPlay(choiceContext, target);
}
```

---

## Hook 接口

### Affliction 相关 Hook

```csharp
// 在 CardCmd.Afflict 中检查
Hook.ShouldAfflict(combatState, card, affliction)
```

### Enchantment 相关 Hook

在 `EnchantmentModel.CanEnchant()` 中有基础检查，但没有专门的 Hook。

---

## 常见问题

### Q: 一张卡可以同时有 Affliction 和 Enchantment 吗？
**A:** 可以，两者互不影响。

### Q: 如何检测卡牌是否有 Affliction/Enchantment？
**A:** 直接检查属性：
```csharp
if (card.Affliction != null) { ... }
if (card.Enchantment != null) { ... }
```

### Q: 如何获取 Affliction/Enchantment 的数值？
**A:** 通过 Amount 属性：
```csharp
int afflictionAmount = card.Affliction?.Amount ?? 0;
int enchantmentAmount = card.Enchantment?.Amount ?? 0;
```

### Q: Affliction 在战斗结束后会怎样？
**A:** 会自动清除，不会保留到下一个战斗。

### Q: Enchantment 会被复制到新卡吗？
**A:** 会，在 `DeepCloneFields()` 和存档/读档时都会保留。

### Q: 能否在战斗外给卡牌添加 Enchantment？
**A:** 可以，Enchantment 支持战斗外应用。

### Q: 能否在战斗外给卡牌添加 Affliction？
**A:** 技术上可以，但由于 Affliction 不存档，意义不大。
