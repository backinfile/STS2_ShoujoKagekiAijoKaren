# Slay the Spire 2 Mod API 文档

## 目录
- [CardModel API](#cardmodel-api)
- [PowerModel API](#powermodel-api)
- [RelicModel API](#relicmodel-api)
- [Commands API](#commands-api)
- [DynamicVar API](#dynamicvar-api)
- [Factory API](#factory-api)

---

## CardModel API

### 基础属性

```csharp
public abstract class CardModel
{
    // 构造函数
    protected CardModel(
        int cost,              // 费用
        CardType type,         // 类型
        CardRarity rarity,     // 稀有度
        TargetType targetType, // 目标类型
        bool upgradable        // 可升级
    )

    // 核心属性
    CardEntity Owner           // 卡牌所有者
    CombatState CombatState   // 战斗状态
    DynamicVarCollection DynamicVars  // 动态变量
}
```

### 枚举类型

```csharp
enum CardType {
    Attack = 1,   // 攻击
    Skill = 2,    // 技能
    Power = 3     // 能力
}

enum CardRarity {
    Basic = 1,      // 基础
    Common = 2,     // 普通
    Uncommon = 3,   // 罕见
    Rare = 4        // 稀有
}

enum TargetType {
    None = 0,           // 无目标
    Self = 1,           // 自己
    SingleEnemy = 2,    // 单个敌人
    AllEnemies = 3      // 所有敌人
}
```

### 重写方法

```csharp
// 定义卡牌变量
protected override IEnumerable<DynamicVar> CanonicalVars

// 额外提示信息
protected override IEnumerable<IHoverTip> ExtraHoverTips

// 卡牌打出时
protected override async Task OnPlay(
    PlayerChoiceContext choiceContext,
    CardPlay cardPlay)

// 升级时
protected override void OnUpgrade()

// 是否可以打出
protected override bool CanPlay(CardPlay cardPlay)
```

---

## PowerModel API

### 基础属性

```csharp
public abstract class PowerModel
{
    Creature Owner        // 能力所有者
    int Amount           // 层数

    // 必须重写
    public abstract PowerType Type
    public abstract PowerStackType StackType
}
```

### 枚举类型

```csharp
enum PowerType {
    Buff = 1,    // 增益
    Debuff = 2   // 减益
}

enum PowerStackType {
    None = 0,     // 不叠加
    Add = 1,      // 叠加
    Replace = 2   // 替换
}
```

### 钩子方法

```csharp
// === 回合相关 ===
async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
async Task AfterPlayerTurnEnd(PlayerChoiceContext ctx, Player player)
async Task AfterEnemyTurnStart(PlayerChoiceContext ctx, Creature enemy)
async Task AfterEnemyTurnEnd(PlayerChoiceContext ctx, Creature enemy)

// === 卡牌相关 ===
async Task AfterCardPlayed(PlayerChoiceContext ctx, CardModel card)
async Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool fromPlay)
async Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card)

// === 伤害相关 ===
decimal ModifyDamageDealt(Creature target, decimal damage, DamageInfo info)
decimal ModifyDamageTaken(Creature source, decimal damage, DamageInfo info)
async Task AfterDamageDealt(PlayerChoiceContext ctx, Creature target, decimal damage)
async Task AfterDamageTaken(PlayerChoiceContext ctx, Creature source, decimal damage)

// === 属性修改 ===
decimal ModifyMaxEnergy(Player player, decimal amount)
decimal ModifyBlock(Creature target, decimal block)
decimal ModifyCardCost(CardModel card, int cost)

// === 其他 ===
void Flash()  // 闪烁效果
```

---

## RelicModel API

### 基础属性

```csharp
public abstract class RelicModel
{
    Player Owner  // 遗物所有者

    // 必须重写
    public abstract RelicRarity Rarity
}
```

### 枚举类型

```csharp
enum RelicRarity {
    Common = 1,     // 普通
    Uncommon = 2,   // 罕见
    Rare = 4,       // 稀有
    Boss = 8,       // Boss
    Special = 16    // 特殊
}
```

### 钩子方法

```csharp
// 与PowerModel类似的钩子
async Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool fromPlay)
async Task AfterCardPlayed(PlayerChoiceContext ctx, CardModel card)
async Task AfterCombatStart(PlayerChoiceContext ctx)
async Task AfterCombatEnd(PlayerChoiceContext ctx)

void Flash()  // 闪烁效果
```

---

## Commands API

### DamageCmd - 伤害命令

```csharp
// 基础攻击
DamageCmd.Attack(decimal damage)
    .FromCard(CardModel card)
    .Targeting(Creature target)
    .TargetingAllOpponents(CombatState state)
    .WithHitFx(string fxPath)
    .Execute(PlayerChoiceContext ctx)

// 真实伤害
DamageCmd.Damage(decimal damage)
    .FromPower(PowerModel power)
    .Targeting(Creature target)
    .Execute(PlayerChoiceContext ctx)
```

### BlockCmd - 格挡命令

```csharp
BlockCmd.GainBlock(decimal block)
    .FromCard(CardModel card)
    .Targeting(Creature target)
    .Execute(PlayerChoiceContext ctx)
```

### PowerCmd - 能力命令

```csharp
// 施加能力
await PowerCmd.Apply<TPower>(
    Creature target,
    decimal amount,
    Creature source,
    CardModel card,
    bool showEffect = true)

// 移除能力
await PowerCmd.Remove<TPower>(
    Creature target,
    decimal amount)
```

### CardPileCmd - 卡牌堆命令

```csharp
// 抽牌
await CardPileCmd.DrawCards(int amount)

// 弃牌
await CardPileCmd.DiscardCards(IEnumerable<CardModel> cards)

// 消耗
await CardPileCmd.ExhaustCards(IEnumerable<CardModel> cards)

// 添加生成的卡牌
await CardPileCmd.AddGeneratedCardToCombat(
    CardModel card,
    PileType pile,
    bool showReward,
    CardPilePosition position)
```

### CreatureCmd - 生物命令

```csharp
// 播放动画
await CreatureCmd.TriggerAnim(
    Creature creature,
    string animName,
    float delay)
```

### 枚举类型

```csharp
enum PileType {
    DrawPile = 0,      // 抽牌堆
    Hand = 2,          // 手牌
    DiscardPile = 1,   // 弃牌堆
    ExhaustPile = 3    // 消耗堆
}

enum CardPilePosition {
    Top = 0,      // 顶部
    Bottom = 1,   // 底部
    Random = 2    // 随机
}
```

---

## DynamicVar API

### 变量类型

```csharp
// 伤害变量
new DamageVar(decimal baseValue, ValueProp prop)
new DamageVar(string key, decimal baseValue, ValueProp prop)

// 格挡变量
new BlockVar(decimal baseValue)

// 能力变量
new PowerVar<TPower>(decimal baseValue)

// 能量变量
new EnergyVar(int baseValue)

// 通用数值
new MagicNumberVar(decimal baseValue)
```

### ValueProp 枚举

```csharp
enum ValueProp {
    Attack = 8,        // 攻击伤害
    TrueDamage = 6,    // 真实伤害
    Block = 0          // 格挡
}
```

### 变量操作

```csharp
DynamicVar var = DynamicVars.Damage;

decimal value = var.BaseValue;      // 基础值
decimal current = var.Value;        // 当前值

var.UpgradeValueBy(decimal amount)  // 升级时增加
```

### 访问变量

```csharp
// 通过属性访问（需要定义）
DynamicVars.Damage
DynamicVars.Block
DynamicVars.Vulnerable

// 通过键访问
DynamicVars["SelfDamage"]
```

---

## Factory API

### CardFactory

```csharp
// 获取随机卡牌
CardModel card = CardFactory.GetDistinctForCombat(
    Player owner,
    IEnumerable<CardModel> pool,
    int count,
    Random rng).First();
```

### HoverTipFactory

```csharp
// 从能力创建提示
HoverTipFactory.FromPower<TPower>()

// 从关键词创建提示
HoverTipFactory.FromKeyword(CardKeyword keyword)

// 能量提示
HoverTipFactory.ForEnergy(PowerModel power)
```

### 关键词枚举

```csharp
enum CardKeyword {
    Exhaust = 1,      // 消耗
    Ethereal = 2,     // 虚无
    Innate = 3,       // 固有
    Retain = 4        // 保留
}
```

---

## 常用模式

### 1. 造成伤害并施加能力

```csharp
protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
{
    // 造成伤害
    await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
        .FromCard(this)
        .Targeting(play.Target)
        .Execute(ctx);

    // 施加能力
    await PowerCmd.Apply<WeakPower>(
        play.Target,
        DynamicVars.Weak.BaseValue,
        Owner.Creature,
        this);
}
```

### 2. 获得格挡并抽牌

```csharp
protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
{
    await BlockCmd.GainBlock(DynamicVars.Block.BaseValue)
        .FromCard(this)
        .Targeting(Owner.Creature)
        .Execute(ctx);

    await CardPileCmd.DrawCards(2);
}
```

### 3. 回合开始触发效果

```csharp
public override async Task AfterPlayerTurnStart(
    PlayerChoiceContext ctx,
    Player player)
{
    if (player != Owner.Player)
        return;

    Flash();

    await DamageCmd.Damage(Amount)
        .FromPower(this)
        .TargetingAllOpponents(player.CombatState)
        .Execute(ctx);
}
```

### 4. 修改属性值

```csharp
public override decimal ModifyDamageDealt(
    Creature target,
    decimal damage,
    DamageInfo info)
{
    if (info.Source != Owner)
        return damage;

    return damage * 1.5m;  // 增加50%伤害
}
```

---

## 注意事项

1. **所有游戏逻辑必须使用 async/await**
2. **decimal 类型用于数值计算**
3. **检查 Owner 避免影响其他玩家**
4. **使用 Flash() 提供视觉反馈**
5. **命令链式调用必须以 Execute() 结束**
