# Slay the Spire 2 Mod制作指南

基于 sts1to2card mod 的反编译分析

## 目录
1. [项目结构](#项目结构)
2. [开发环境](#开发环境)
3. [核心概念](#核心概念)
4. [卡牌制作](#卡牌制作)
5. [能力制作](#能力制作)
6. [遗物制作](#遗物制作)
7. [本地化](#本地化)

---

## 项目结构

```
sts1to2card/
├── sts1to2card.dll          # 编译后的mod主文件
├── sts1to2card.pck          # Godot资源包（图片、音效等）
├── mod_manifest.json        # mod元数据
└── README.md
```

### 源代码结构（推荐）
```
src/
├── red/                     # 战士（红色）相关
│   ├── cards/              # 卡牌
│   ├── powers/             # 能力
│   └── vfx/                # 视觉效果
├── green/                   # 猎人（绿色）相关
│   ├── cards/
│   └── powers/
├── relics/                  # 遗物
│   └── shared/             # 通用遗物
└── colorless/              # 无色卡牌
```

---

## 开发环境

### 必需工具
- **.NET 9.0 SDK** - STS2使用.NET 9
- **Godot 4.x** - 游戏引擎
- **C# IDE** - Visual Studio / Rider / VS Code

### 项目配置

创建 `.csproj` 文件：
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>
</Project>
```

### 核心命名空间
```csharp
using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
```

---

## 核心概念

### 1. CardModel - 卡牌基类
所有卡牌继承自 `CardModel`

### 2. PowerModel - 能力基类
所有能力（buff/debuff）继承自 `PowerModel`

### 3. RelicModel - 遗物基类
所有遗物继承自 `RelicModel`

### 4. 异步执行
游戏使用 `async/await` 模式处理所有游戏逻辑

---

## 卡牌制作

### 基础攻击卡示例

```csharp
public sealed class RedCleave : CardModel
{
    // 定义卡牌变量（伤害值等）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] {
            new DamageVar(8m, ValueProp.Attack)
        };

    // 构造函数：费用、类型、稀有度、目标类型、是否可升级
    public RedCleave()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            targetType: TargetType.AllEnemies,
            upgradable: true)
    {
    }

    // 卡牌使用时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    // 升级效果
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
```

### 技能卡示例（施加能力）

```csharp
public sealed class RedBerserk : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] {
            new PowerVar<VulnerablePower>(2m),
            new EnergyVar(1)
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[] {
            HoverTipFactory.FromPower<VulnerablePower>(),
            this.EnergyHoverTip
        };

    public RedBerserk()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放施法动画
        await CreatureCmd.TriggerAnim(
            Owner.Creature,
            "Cast",
            Owner.Character.CastAnimDelay);

        // 给自己施加易伤
        await PowerCmd.Apply<VulnerablePower>(
            Owner.Creature,
            DynamicVars.Vulnerable.BaseValue,
            Owner.Creature,
            this);

        // 给自己施加狂暴能力（增加能量）
        await PowerCmd.Apply<RedBerserkPower>(
            Owner.Creature,
            DynamicVars.Energy.BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Vulnerable.UpgradeValueBy(-1m);
    }
}
```

### 卡牌参数说明

**构造函数参数：**
- `cost` - 费用（0-3或-2表示X费用）
- `type` - 卡牌类型
  - `CardType.Attack` - 攻击
  - `CardType.Skill` - 技能
  - `CardType.Power` - 能力
- `rarity` - 稀有度
  - `CardRarity.Basic` - 基础
  - `CardRarity.Common` - 普通
  - `CardRarity.Uncommon` - 罕见
  - `CardRarity.Rare` - 稀有
- `targetType` - 目标类型
  - `TargetType.Self` - 自己
  - `TargetType.SingleEnemy` - 单个敌人
  - `TargetType.AllEnemies` - 所有敌人
  - `TargetType.None` - 无目标
- `upgradable` - 是否可升级

**DynamicVar 类型：**
- `DamageVar` - 伤害值
- `BlockVar` - 格挡值
- `PowerVar<T>` - 能力层数
- `EnergyVar` - 能量
- `MagicNumberVar` - 通用数值

---

## 能力制作

### 简单能力示例（修改属性）

```csharp
public sealed class RedBerserkPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Add;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[] {
            HoverTipFactory.ForEnergy(this)
        };

    // 修改最大能量
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != Owner.Player)
            return amount;

        return amount + Amount;
    }
}
```

### 复杂能力示例（回合触发）

```csharp
public sealed class RedBrutalityPower : PowerModel
{
    private const string SelfDamageKey = "SelfDamage";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Add;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> {
            new DamageVar(SelfDamageKey, 1m, ValueProp.TrueDamage)
        };

    // 玩家回合开始时触发
    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner.Player)
            return;

        Flash();

        // 抽Amount张牌
        await CardPileCmd.DrawCards(Amount);

        // 对自己造成1点真实伤害
        await DamageCmd.Damage(DynamicVars[SelfDamageKey].Value)
            .FromPower(this)
            .Targeting(Owner.Creature)
            .Execute(choiceContext);
    }
}
```

### 能力类型

**PowerType:**
- `Buff` - 增益
- `Debuff` - 减益

**PowerStackType:**
- `Add` - 叠加
- `Replace` - 替换
- `None` - 不叠加

### 常用钩子方法

```csharp
// 回合相关
AfterPlayerTurnStart()      // 玩家回合开始后
AfterPlayerTurnEnd()        // 玩家回合结束后
AfterEnemyTurnStart()       // 敌人回合开始后

// 卡牌相关
AfterCardPlayed()           // 卡牌打出后
AfterCardExhausted()        // 卡牌消耗后
AfterCardDrawn()            // 卡牌抽取后

// 伤害相关
ModifyDamageDealt()         // 修改造成的伤害
ModifyDamageTaken()         // 修改受到的伤害
AfterDamageDealt()          // 造成伤害后
AfterDamageTaken()          // 受到伤害后

// 属性修改
ModifyMaxEnergy()           // 修改最大能量
ModifyBlock()               // 修改格挡
```

---

## 遗物制作

### 基础遗物示例

```csharp
public sealed class SharedDeadBranch : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[] {
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
        };

    // 卡牌消耗后触发
    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool _)
    {
        if (card.Owner != Owner)
            return;

        Flash();

        // 随机生成一张卡牌
        CardModel randomCard = CardFactory
            .GetDistinctForCombat(
                Owner,
                Owner.Character.CardPool.GetUnlockedCards(
                    Owner.UnlockState,
                    Owner.RunState.CardMultiplayerConstraint),
                1,
                Owner.RunState.Rng.CombatCardGeneration)
            .First();

        // 添加到手牌
        await CardPileCmd.AddGeneratedCardToCombat(
            randomCard,
            PileType.Hand,
            showReward: true,
            CardPilePosition.Top);
    }
}
```

### 遗物稀有度

```csharp
RelicRarity.Common      // 普通
RelicRarity.Uncommon    // 罕见
RelicRarity.Rare        // 稀有
RelicRarity.Boss        // Boss遗物
RelicRarity.Special     // 特殊
```

---

## 本地化

### 文件结构
本地化文件通常放在 `.pck` 资源包中

### 本地化键命名规范
```
卡牌: CARD_[ID]_NAME / CARD_[ID]_DESC
能力: POWER_[ID]_NAME / POWER_[ID]_DESC
遗物: RELIC_[ID]_NAME / RELIC_[ID]_DESC
```

### 动态变量
在描述文本中使用 `{变量名}` 引用DynamicVar：
```
造成 {Damage} 点伤害
获得 {Block} 点格挡
施加 {Vulnerable} 层易伤
```

---

## 常用命令（Commands）

### DamageCmd - 伤害命令
```csharp
await DamageCmd.Attack(damage)
    .FromCard(this)
    .Targeting(target)
    .WithHitFx("vfx/slash")
    .Execute(choiceContext);
```

### BlockCmd - 格挡命令
```csharp
await BlockCmd.GainBlock(block)
    .FromCard(this)
    .Targeting(Owner.Creature)
    .Execute(choiceContext);
```

### PowerCmd - 能力命令
```csharp
await PowerCmd.Apply<PowerType>(
    target,
    amount,
    source,
    this);
```

### CardPileCmd - 卡牌堆命令
```csharp
await CardPileCmd.DrawCards(amount);
await CardPileCmd.DiscardCards(cards);
await CardPileCmd.ExhaustCards(cards);
```

### CreatureCmd - 生物命令
```csharp
await CreatureCmd.TriggerAnim(creature, "Cast", delay);
```

---

## 调试技巧

1. **使用GD.Print()输出日志**
```csharp
GD.Print($"卡牌 {GetType().Name} 被打出");
```

2. **检查游戏日志**
日志位置：`%AppData%/SlayTheSpire2/logs/`

3. **热重载**
修改代码后重新编译DLL，游戏可能需要重启

---

## 发布Mod

### 1. 编译项目
```bash
dotnet build -c Release
```

### 2. 准备文件
- `YourMod.dll` - 编译后的DLL
- `YourMod.pck` - Godot资源包（可选）
- `mod_manifest.json` - Mod元数据

### 3. mod_manifest.json 示例
```json
{
  "name": "Your Mod Name",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "Mod description",
  "game_version": "0.98.3"
}
```

### 4. 安装位置
将mod文件放入：
```
Steam/steamapps/common/SlayTheSpire2/mods/YourMod/
```

---

## 参考资源

- [官方Mod教程](https://github.com/GlitchedReme/SlayTheSpire2ModdingTutorials)
- [STS2学习项目](https://github.com/rayinls/STS2_Learner)
- [sts1to2card源码](https://github.com/rayinls/sts-1-to-2-card/)

---

## 常见问题

**Q: Mod不加载？**
A: 检查.NET版本是否为9.0，检查DLL是否在正确目录

**Q: 卡牌没有图片？**
A: 需要创建.pck资源包并包含图片资源

**Q: 如何测试Mod？**
A: 启动游戏，在主菜单查看Mod是否加载，进入游戏测试功能

**Q: 能力不生效？**
A: 检查PowerType和钩子方法是否正确实现

---

## 版本兼容性

- 当前分析基于 **STS2 v0.98.3**
- 使用 **.NET 9.0**
- 需要 **Godot 4.x** 引擎支持

**注意：** 游戏更新可能导致API变化，需要相应更新Mod代码。
