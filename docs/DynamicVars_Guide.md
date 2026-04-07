# STS2 卡牌动态变量（DynamicVar）完整指南

## 一、DynamicVar 体系概览

卡牌描述中的 `{变量名:格式化方法()}` 占位符由 `DynamicVar` 系统驱动。

### 核心 API

```csharp
// 在 CanonicalVars 中声明（告知系统该卡牌有哪些变量）
protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
{
    new DamageVar(8m, ValueProp.Move),
    new BlockVar(5m, ValueProp.Move),
};

// 在 OnUpgrade 中升级
protected override void OnUpgrade()
{
    base.DynamicVars.Damage.UpgradeValueBy(2m);   // 伤害 +2
    base.EnergyCost.UpgradeBy(-1);                 // 费用 -1（注意：不是 DynamicVar）
}

// 在 OnPlay 中使用
await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).Execute(ctx);
await PowerCmd.Apply<WeakPower>(target, base.DynamicVars.Weak.BaseValue, ...);
```

**访问方式**：
- 强类型属性：`base.DynamicVars.Damage` / `.Block` / `.Cards` / `.Energy` 等
- 自定义 key：`base.DynamicVars["MyKey"]`

---

## 二、所有 DynamicVar 子类速查

| C# 类 | JSON 占位符名 | 用途 | 构造示例 |
|---|---|---|---|
| `DamageVar` | `Damage` | 攻击伤害值 | `new DamageVar(8m, ValueProp.Move)` |
| `BlockVar` | `Block` | 格挡值 | `new BlockVar(5m, ValueProp.Move)` |
| `CardsVar` | `Cards` | 抽/弃/添加牌数 | `new CardsVar(2)` |
| `EnergyVar` | `Energy` | 能量值 | `new EnergyVar(1)` |
| `RepeatVar` | `Repeat` | 重复次数 | `new RepeatVar(3)` |
| `MaxHpVar` | `MaxHp` | 最大 HP 变化量 | `new MaxHpVar(10m)` |
| `HpLossVar` | `HpLoss` | 失去的生命值 | `new HpLossVar(3m)` |
| `HealVar` | `Heal` | 回复的生命值 | `new HealVar(8m)` |
| `GoldVar` | `Gold` | 金币数 | `new GoldVar(50)` |
| `ForgeVar` | `Forge` | 铸造值 | `new ForgeVar(1)` |
| `StarsVar` | `Stars` | 星辰数 | `new StarsVar(2)` |
| `SummonVar` | `Summon` | 召唤量 | `new SummonVar(1m)` |
| `ExtraDamageVar` | `ExtraDamage` | 额外伤害（配合 CalculatedDamage） | `new ExtraDamageVar(3m)` |
| `CalculationBaseVar` | `CalculationBase` | 计算型变量基础值 | `new CalculationBaseVar(6m)` |
| `CalculationExtraVar` | `CalculationExtra` | 计算型变量每次叠加量 | `new CalculationExtraVar(2m)` |
| `CalculatedDamageVar` | `CalculatedDamage` | 运行时动态计算的伤害 | `new CalculatedDamageVar(ValueProp.Move)` |
| `CalculatedBlockVar` | `CalculatedBlock` | 运行时动态计算的格挡 | `new CalculatedBlockVar(ValueProp.Move)` |
| `CalculatedVar` | 自定义名 | 通用动态计算变量 | `new CalculatedVar("Hits")` |
| `PowerVar<T>` | T 的类名 | 施加某 Power 的层数 | `new PowerVar<WeakPower>(2m)` |
| `IfUpgradedVar` | `IfUpgraded` | 升级条件文本控制 | 自动生成，无需手动创建 |
| `IntVar` | 自定义名 | 通用整数 | `new IntVar("N", 5m)` |
| `DynamicVar`（基类） | 自定义名 | 通用数字 | `new DynamicVar("MyKey", 3m)` |

### PowerVar 名称规则

JSON 中的键名等于泛型参数 T 的类名：

| C# 写法 | JSON 占位符 |
|---|---|
| `PowerVar<StrengthPower>` / `base.DynamicVars.Strength` | `{StrengthPower:diff()}` |
| `PowerVar<DexterityPower>` / `base.DynamicVars.Dexterity` | `{DexterityPower:diff()}` |
| `PowerVar<VulnerablePower>` / `base.DynamicVars.Vulnerable` | `{VulnerablePower:diff()}` |
| `PowerVar<WeakPower>` / `base.DynamicVars.Weak` | `{WeakPower:diff()}` |
| `PowerVar<PoisonPower>` / `base.DynamicVars.Poison` | `{PoisonPower:diff()}` |
| `PowerVar<DoomPower>` / `base.DynamicVars.Doom` | `{DoomPower:diff()}` |
| `PowerVar<FocusPower>` | `{FocusPower:diff()}` |

---

## 三、格式化方法（修饰符）

JSON 占位符格式：`{变量名:格式化方法()}`

### `diff()` — 高亮差异（最常用）

```json
"{Damage:diff()}"
```

- 正常状态：显示 `PreviewValue`，若有 Power 加成则绿色/红色高亮
- 升级预览时：绿色高亮显示升级后的新值
- **几乎所有数值型变量都用此方法**

```json
"BASH.description": "造成{Damage:diff()}点伤害。\n给予{VulnerablePower:diff()}层[gold]易伤[/gold]。"
"BACKFLIP.description": "获得{Block:diff()}点[gold]格挡[/gold]。\n抽2张牌。"
```

---

### `inverseDiff()` — 反向高亮差异

```json
"{StrengthPower:inverseDiff()}"
```

与 `diff()` 相同但方向相反，用于"失去力量"等数值越大越负面的情况。

```json
"FRIENDSHIP.description": "失去{StrengthPower:inverseDiff()}点[gold]力量[/gold]。"
```

---

### `energyIcons()` — 显示能量图标

```json
"{Energy:energyIcons()}"
```

将数值转换为当前角色能量图标（1~3 个显示图标叠放，4+ 显示"数字+图标"）。

```json
"ADRENALINE.description": "获得{Energy:energyIcons()}。\n抽2张牌。"
"BLOODLETTING.description": "失去{HpLoss:diff()}点生命。\n获得{Energy:energyIcons()}。"
```

**固定数量图标**（不依赖变量）：
```json
"CORRUPTION.description": "技能牌消耗变为0{energyPrefix:energyIcons(1)}。"
```
`energyPrefix` 配合 `energyIcons(N)` 可显示固定 N 个能量图标。

---

### `starIcons()` — 显示星辰图标

```json
"{Stars:starIcons()}"
```

```json
"BIG_BANG.description": "获得{Stars:starIcons()}。"
```

---

### `show:A|B` — 升级条件文本

```json
"{IfUpgraded:show:升级后文本|普通文本}"
```

- 正常状态：显示 `|` 后的文本
- 已升级：显示 `|` 前的文本
- 升级预览：以绿色显示 `|` 前的文本
- `|` 后可为空（只在升级后显示额外内容）

```json
"ARMAMENTS.description": "获得{Block:diff()}点格挡。\n升级你手牌中的{IfUpgraded:show:所有牌|一张牌}。"
"ENLIGHTENMENT.description": "在这{IfUpgraded:show:场战斗|个回合}，..."
"DARKNESS.description": "...触发所有黑暗充能球的被动{IfUpgraded:show:两次|}。"
"CASCADE.description": "打出你抽牌堆顶部的X{IfUpgraded:show:+1}张牌。"
```

---

### `choose(N):A|B` — 数值条件选择

```json
"{Var:choose(阈值):等于阈值时的文本|不等于时的文本}"
```

当变量值等于阈值时显示 A，否则显示 B（B 中可嵌套其他变量）。

```json
"BURST.description": "在这个回合，你打出的下{Skills:choose(1):一|{Skills:diff()}}张技能牌会被额外打出一次。"
```

---

### `percentMore()` / `percentLess()` — 百分比格式

```json
"{CrueltyPower:percentMore()}"   // 1.5m → 显示 "50"（表示 +50%）
"{SomeVar:percentLess()}"        // 0.5m → 显示 "50"（表示 -50%）
```

---

### `abs()` — 绝对值

```json
"{SomeVar:abs()}"
```

显示数值的绝对值（去掉负号）。

---

## 四、特殊条件块

### `{InCombat:文本|}` — 战斗中才显示

```json
"BODY_SLAM.description": "造成你当前格挡值的伤害。{InCombat:\n（造成{CalculatedDamage:diff()}点伤害）|}"
"BARRAGE.description": "当前每有一个充能球，造成{Damage:diff()}点伤害。{InCombat:\n（命中{CalculatedHits:diff()}次）|}"
```

`|` 后面是非战斗时显示的内容（通常为空）。

### `{IsTargeting:文本|}` — 瞄准时才显示

```json
"NO_ESCAPE.description": "给予{CalculationBase:diff()}层灾厄...{IsTargeting:\n（给予{CalculatedDoom:diff()}层灾厄）|}"
```

### `{singleStarIcon}` — 静态星辰图标

不是 DynamicVar，是直接渲染一个星辰图标。

---

## 五、计算型变量（CalculatedVar）

用于运行时动态计算的值（如"伤害等于当前格挡值"、"每有一张消耗牌伤害+3"）。

### 核心公式

```
最终值 = CalculationBase + ExtraDamage × multiplierCalc(card, target)
```

### 使用流程

```csharp
// 示例：AshenStrike — 伤害 = 6 + 3 × 消耗牌数
protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
{
    new CalculationBaseVar(6m),        // 基础伤害 6
    new ExtraDamageVar(3m),            // 每张消耗牌 +3
    new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
        (CardModel card, Creature? _) => PileType.Exhaust.GetPile(card.Owner).Cards.Count
    )
};
```

```json
"ASHEN_STRIKE.description": "造成{CalculatedDamage:diff()}点伤害。\n你的消耗牌堆中每有一张牌，伤害增加{ExtraDamage:diff()}。"
```

```csharp
// 示例：BodySlam — 伤害 = 当前格挡值
new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
    (CardModel card, Creature? _) => card.Owner.Creature.Block
)
// CalculationBase=0, ExtraDamage=1 → 伤害 = 格挡值
```

### CalculatedBlockVar — 动态计算格挡

与 `CalculatedDamageVar` 类似，用于格挡值在运行时动态计算的场景。

```csharp
// 示例：KarenPractice2 — 格挡 = 所有牌的闪耀值之和
protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
{
    new CalculationBaseVar(0m),        // 基础格挡 0
    new CalculationExtraVar(1m),       // 每点闪耀值 +1
    new CalculatedBlockVar(ValueProp.Move).WithMultiplier(
        (CardModel card, Creature? _) =>
        {
            var combatState = card.Owner.PlayerCombatState;
            if (combatState == null) return 0m;

            return combatState.DrawPile.Cards
                .Concat(combatState.DiscardPile.Cards)
                .Concat(combatState.Hand.Cards)
                .Concat(combatState.ExhaustPile.Cards)
                .OfType<KarenBaseCardModel>()
                .Sum(c => c.GetShineValueRounded());
        }
    )
};

protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    var blockAmount = (int)DynamicVars.CalculatedBlock.Calculate(cardPlay.Target);
    await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, cardPlay);
}

protected override void OnUpgrade()
{
    DynamicVars.CalculationBase.UpgradeValueBy(1);  // 升级后每张 +1 格挡
}
```

```json
"KAREN_PRACTICE_2.description": "获得{CalculatedBlock:diff()}点[gold]格挡[/gold]。"
```

### 自定义 CalculatedVar

```csharp
// 自定义名称的计算变量
var hitsVar = new CalculatedVar("CalculatedHits").WithMultiplier(
    (CardModel card, Creature? _) => card.Owner.OrbSlots.Count(o => o is LightningOrb)
);
```

---

## 六、ValueProp 枚举

`DamageVar`、`BlockVar`、`CalculatedDamageVar` 构造时必须传入。

```csharp
[Flags]
public enum ValueProp
{
    Unblockable = 2,       // 无视格挡（中毒、自伤等）
    Unpowered   = 4,       // 不受力量/敏捷加成
    Move        = 8,       // 受力量/敏捷加成（普通攻击和格挡均用此）
    SkipHurtAnim = 0x10    // 跳过受击动画
}
```

**重要**：`ValueProp` 是 `[Flags]` 枚举，**没有** `.None`，零值写 `(ValueProp)0`。

| 场景 | ValueProp |
|---|---|
| 普通攻击 | `ValueProp.Move` |
| 普通格挡 | `ValueProp.Move` |
| 自伤（如 Bloodletting） | `ValueProp.Unblockable \| ValueProp.Unpowered \| ValueProp.Move` |
| 不受加成的固定伤害 | `ValueProp.Unpowered` |

---

## 七、X 费卡牌

X 费卡**不用 DynamicVar** 存 X 值，通过特殊属性处理：

```csharp
protected override bool HasEnergyCostX => true;

protected override async Task OnPlay(...)
{
    int x = ResolveEnergyXValue();   // 获取 X 的实际值
    await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
        .WithHitCount(x)
        .Execute(ctx);
}
```

JSON 中 X 直接写字面量 `X`：
```json
"ERADICATE.description": "造成{Damage:diff()}点伤害X次。"
"CASCADE.description": "打出你抽牌堆顶部的X{IfUpgraded:show:+1}张牌。"
```

---

## 八、变量名速查表

```
{Block:diff()}                  格挡值
{Damage:diff()}                 伤害值
{Cards:diff()}                  牌数
{Energy:energyIcons()}          能量（图标形式）
{Repeat:diff()}                 重复次数
{MaxHp:diff()}                  最大 HP 变化量
{HpLoss:diff()}                 失去的生命值
{Heal:diff()}                   回复的生命值
{Gold:diff()}                   金币数
{Stars:starIcons()}             星辰（图标形式）
{Forge:diff()}                  铸造值
{Summon:diff()}                 召唤量

{StrengthPower:diff()}          力量层数
{DexterityPower:diff()}         敏捷层数
{VulnerablePower:diff()}        易伤层数
{WeakPower:diff()}              虚弱层数
{PoisonPower:diff()}            中毒层数
{DoomPower:diff()}              灾厄层数
{FocusPower:diff()}             集中层数

{ExtraDamage:diff()}            额外伤害（配合 CalculatedDamage）
{CalculationBase:diff()}        计算基础值
{CalculationExtra:diff()}       计算叠加量
{CalculatedDamage:diff()}       动态计算最终伤害
{CalculatedBlock:diff()}        动态计算最终格挡

{IfUpgraded:show:A|B}           升级条件文本（A=升级后，B=普通）
{InCombat:文本|}                战斗中显示的额外文本
{IsTargeting:文本|}             瞄准目标时显示的额外文本
{energyPrefix:energyIcons(N)}   固定 N 个能量图标
{singleStarIcon}                单个星辰图标（静态）
```

---

## 九、Karen Mod 新卡标准模板

```csharp
// KarenXxxCard.cs
public class KarenXxxCard : CardModel
{
    public KarenXxxCard() : base(
        name: "KarenXxx",
        color: CardColor.Purple,
        rarity: CardRarity.Common,
        type: CardType.Attack,
        cost: 1)
    { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),     // {Damage:diff()}
        new PowerVar<WeakPower>(2m),             // {WeakPower:diff()}
        // new DynamicVar("MyKey", 3m),          // 自定义: {MyKey:diff()}
    };

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);
        base.DynamicVars.Weak.UpgradeValueBy(1m);
        // base.EnergyCost.UpgradeBy(-1);        // 费用 -1
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .Execute(ctx, cardPlay.Target);
        await PowerCmd.Apply<WeakPower>(
            cardPlay.Target, base.DynamicVars.Weak.BaseValue, this, ctx);
    }
}
```

```json
// localization/zhs/cards.json
"KAREN_XXX.description": "造成{Damage:diff()}点伤害。\n给予{WeakPower:diff()}层[gold]虚弱[/gold]。"
"KAREN_XXX.upgrade": "伤害提升至{Damage:diff()}点，虚弱提升至{WeakPower:diff()}层。"

// localization/eng/cards.json
"KAREN_XXX.description": "Deal {Damage:diff()} damage.\nApply {WeakPower:diff()} [gold]Weak[/gold]."
```

> **中文格式规范**：变量占位符与中文字符之间**不加空格**，如 `获得{Block:diff()}点[gold]格挡[/gold]。`
