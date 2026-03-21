# STS2 卡牌效果系统完整参考

> 基于游戏源码 `D:\claudeProj\sts2\`（v0.99.1）分析整理，供 Mod 开发参考。

---

## 一、架构总览

STS2 **没有独立的 ICardEffect 接口**。卡牌效果通过以下方式实现：

```
CardModel（抽象基类）
  ├── CanonicalVars     → 声明数值参数（DynamicVar）
  ├── CanonicalTags     → 声明标签（Strike/Defend 等）
  ├── CanonicalKeywords → 声明关键词（Exhaust/Ethereal 等）
  ├── OnPlay()          → 效果逻辑，调用静态 Cmd 类执行
  └── OnUpgrade()       → 升级时修改 DynamicVar 值
```

效果执行器（静态 Cmd 类）：

| Cmd 类 | 用途 |
|--------|------|
| `DamageCmd` | 攻击/伤害 |
| `CreatureCmd` | 格挡/治疗/血量 |
| `CardPileCmd` | 抽牌/洗牌/移动到牌堆 |
| `CardCmd` | 弃牌/消耗/升级/变形/自动打出 |
| `PowerCmd` | 施加/修改/移除能力 |
| `CardSelectCmd` | 玩家选牌交互 |
| `VfxCmd / SfxCmd` | 特效/音效 |

---

## 二、CardModel 关键方法

```csharp
// 构造函数
protected CardModel(
    int canonicalEnergyCost,
    CardType type,         // Attack / Skill / Power / Curse / Status
    CardRarity rarity,     // Basic / Common / Uncommon / Rare / Special
    TargetType targetType, // Self / AnyEnemy / AllEnemies / None
    bool shouldShowInCardLibrary = true)

// 必须 override 的方法
protected override IEnumerable<DynamicVar> CanonicalVars { get; }
protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
protected override void OnUpgrade()

// 可选 override
protected override HashSet<CardTag> CanonicalTags { get; }    // CardTag.Strike / Defend
protected override IEnumerable<CardKeyword> CanonicalKeywords { get; }
protected override IEnumerable<IHoverTip> ExtraHoverTips { get; }

// 特殊属性
public override bool GainsBlock => true;      // 声明该卡会给予格挡（UI 提示）
protected override bool HasEnergyCostX => true; // X 费卡
```

### CardPlay 对象

```csharp
cardPlay.Target    // 目标 Creature（TargetType.AnyEnemy 时必须非 null）
cardPlay.Source    // 来源（通常是 Player）
```

### 常用基类属性

```csharp
base.Owner         // 持有该卡的 Player
base.Owner.Creature // 玩家生物
base.CombatState   // 战斗状态（访问所有生物）
base.DynamicVars   // DynamicVarSet，访问参数
base.IsUpgraded    // 是否已升级
base.Keywords      // 当前关键词集合
```

---

## 三、DynamicVar 系统（数值参数）

DynamicVar 是卡牌数值的声明和容器，同时驱动 UI 显示。

### 所有 DynamicVar 类型

| 类 | 默认 Name | 构造示例 | 文本占位符 |
|----|-----------|---------|------------|
| `DamageVar(decimal, ValueProp)` | `"Damage"` | `new DamageVar(6m, ValueProp.Move)` | `{Damage:diff()}` |
| `BlockVar(decimal, ValueProp)` | `"Block"` | `new BlockVar(5m, ValueProp.Move)` | `{Block:diff()}` |
| `CardsVar(int)` | `"Cards"` | `new CardsVar(3)` | `{Cards:diff()}` |
| `PowerVar<T>(decimal)` | `T的类名` | `new PowerVar<StrengthPower>(2m)` | `{StrengthPower:diff()}` |
| `IntVar(string, decimal)` | 自定义 | `new IntVar("MyVal", 3m)` | `{MyVal:diff()}` |
| `HealVar(decimal)` | `"Heal"` | `new HealVar(6m)` | `{Heal:diff()}` |
| `MaxHpVar(decimal)` | `"MaxHp"` | `new MaxHpVar(10m)` | `{MaxHp:diff()}` |
| `GoldVar(decimal)` | `"Gold"` | `new GoldVar(30m)` | `{Gold:diff()}` |
| `EnergyVar(decimal)` | `"Energy"` | `new EnergyVar(1m)` | `{Energy:diff()}` |
| `RepeatVar(int)` | `"Repeat"` | `new RepeatVar(3)` | `{Repeat:diff()}` |

### ValueProp 枚举（伤害/格挡修改标志）

```csharp
[Flags]
public enum ValueProp
{
    Unblockable  = 2,   // 无视格挡（HP直伤）
    Unpowered    = 4,   // 不受力量/弱化等加成影响
    Move         = 8,   // 受力量/弱化等正常修改（普通攻击/格挡）
    SkipHurtAnim = 16   // 跳过受击动画
}
```

### DynamicVar API

```csharp
// 声明（在 CanonicalVars 中）
new DamageVar(6m, ValueProp.Move)

// 访问（在 OnPlay/OnUpgrade 中）
base.DynamicVars.Damage.BaseValue      // decimal，当前基础值
base.DynamicVars.Block.BaseValue
base.DynamicVars.Cards.BaseValue       // int
base.DynamicVars["StrengthPower"].BaseValue  // 通过 Name 字符串访问

// 升级
base.DynamicVars.Damage.UpgradeValueBy(3m)   // 升级加值
base.DynamicVars.Block.UpgradeValueBy(3m)
base.DynamicVars.Cards.UpgradeValueBy(1m)

// 多个 DynamicVar 的声明（用数组而不是 ReadOnlySingleElementList）
protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
{
    new DamageVar(8m, ValueProp.Move),
    new PowerVar<VulnerablePower>(2m)
};
```

---

## 四、Cmd 类 API 完整参考

### 4.1 DamageCmd（攻击）

```csharp
// 构建攻击命令（流式 API）
await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
    .FromCard(this)                              // 来源为卡牌（设置 Attacker、触发 FromCard 动画）
    .Targeting(cardPlay.Target)                  // 单体目标
    // .TargetingAllOpponents(base.CombatState) // AoE 全体敌方
    // .TargetingRandomOpponents(state, count)  // 随机 N 个敌人
    .WithHitFx("vfx/vfx_attack_slash")          // 命中特效（路径）
    // .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3") // 带音效
    .WithHitCount(3)                             // 连击 N 次
    .Execute(choiceContext);

// 常用特效路径
// "vfx/vfx_attack_slash"     — 普通斩击
// "vfx/vfx_attack_blunt"     — 钝击
// "vfx/vfx_flying_slash"     — 飞斩
// "vfx/vfx_giant_horizontal_slash" — 横扫
```

### 4.2 CreatureCmd（生物操作）

```csharp
// 获得格挡
await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

// 治疗
await CreatureCmd.Heal(creature, amount);

// 最大 HP 操作
await CreatureCmd.GainMaxHp(creature, amount);
await CreatureCmd.LoseMaxHp(choiceContext, creature, amount, isFromCard: true);

// 直接伤害（绕过 AttackCommand，用于特殊机制）
await CreatureCmd.Damage(choiceContext, target, damageVar, dealer);
```

### 4.3 CardPileCmd（牌堆操作）

```csharp
// 抽牌
await CardPileCmd.Draw(choiceContext, count, base.Owner);

// 洗牌（弃牌堆回抽牌堆）
await CardPileCmd.Shuffle(choiceContext, player);

// 将卡移入指定牌堆
await CardPileCmd.Add(card, PileType.Discard);
await CardPileCmd.Add(card, PileType.Exhaust);
await CardPileCmd.Add(card, PileType.Hand);
await CardPileCmd.Add(card, PileType.DrawPile);

// 从战斗中移除（不影响永久卡组）
await CardPileCmd.RemoveFromCombat(card);

// 从永久卡组删除
await CardPileCmd.RemoveFromDeck(card);
```

### 4.4 CardCmd（卡牌操作）

```csharp
// 弃牌
await CardCmd.Discard(choiceContext, card);

// 消耗
await CardCmd.Exhaust(choiceContext, card);

// 升级
CardCmd.Upgrade(card);

// 自动打出（不消耗费用）
await CardCmd.AutoPlay(choiceContext, card, target);

// 变形为另一种卡
await CardCmd.TransformTo<TargetCardType>(originalCard);

// 施加词缀（Affliction）
await CardCmd.Afflict<SomeAffliction>(card, amount);

// 施加强化（Enchantment）
CardCmd.Enchant<SomeEnchantment>(card, amount);
```

### 4.5 PowerCmd（能力）

```csharp
// 给单个目标施加能力
await PowerCmd.Apply<StrengthPower>(target, amount, applier, cardSource);
await PowerCmd.Apply<VulnerablePower>(target, 2m, base.Owner.Creature, this);

// 给多个目标施加能力
await PowerCmd.Apply<PoisonPower>(
    base.CombatState.GetOpponentsOf(base.Owner.Creature),
    3m, base.Owner.Creature, this);

// 修改现有能力数值
await PowerCmd.ModifyAmount(power, -1, applier, cardSource);

// 移除能力
await PowerCmd.Remove<PoisonPower>(creature);
await PowerCmd.Remove(powerModel);

// Duration 递减（回合结束调用）
await PowerCmd.TickDownDuration(power);
```

### 4.6 CardSelectCmd（玩家选牌交互）

```csharp
// 从手牌选择弃牌
IEnumerable<CardModel> cards = await CardSelectCmd.FromHandForDiscard(
    choiceContext, base.Owner,
    new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, count),
    null, this);

// 从手牌选择升级
CardModel? chosen = await CardSelectCmd.FromHandForUpgrade(choiceContext, base.Owner, this);

// 从特定牌堆选择
// ... 参考 CardSelectCmd 的其他静态方法
```

---

## 五、卡牌执行流程（OnPlayWrapper）

```
玩家拖卡 → TryManualPlay
  → EnqueueManualPlay → PlayCardAction 入队
    → CardModel.OnPlayWrapper() 执行：

1. CardPileCmd.Add → 卡牌移入 Play 区
2. 计算结果牌堆（Discard / Exhaust / None）
3. Hook.ModifyCardPlayCount → 决定打出次数（Echo Form 等遗物在此修改）
4. [循环 playCount 次]:
   a. Hook.BeforeCardPlayed(combatState, cardPlay)
   b. OnPlay(choiceContext, cardPlay)       ← 子类实现的效果
   c. Enchantment.OnPlay(...)              ← 附魔效果（如 Gilded）
   d. Affliction.OnPlay(...)               ← 词缀效果
   e. Hook.AfterCardPlayed(...)            ← 遗物/能力响应
5. 移动到结果牌堆
6. 费用/星币事后清理
```

### 触发时机汇总

| 时机 | 触发方式 |
|------|----------|
| 打出卡牌 | `OnPlay()` override |
| 打出前 | `Hook.BeforeCardPlayed` |
| 打出后 | `Hook.AfterCardPlayed` |
| 抽牌时 | `Hook.AfterCardDrawn` |
| 弃牌时 | `Hook.AfterCardDiscarded` |
| 消耗时 | `Hook.AfterCardExhausted` |
| 回合结束留手 | `OnTurnEndInHand()` override |
| 升级时 | `OnUpgrade()` override |
| 牌堆变化 | `Hook.AfterCardChangedPiles` |

---

## 六、关键词系统

### CardKeyword 枚举

```csharp
public enum CardKeyword
{
    None, Exhaust, Ethereal, Innate, Unplayable, Retain, Sly, Eternal
}
```

### 在卡牌中声明

```csharp
// 静态声明（在 CanonicalKeywords 中）
protected override IEnumerable<CardKeyword> CanonicalKeywords
    => new[] { CardKeyword.Exhaust };

// 动态操作
card.AddKeyword(CardKeyword.Retain);
card.RemoveKeyword(CardKeyword.Exhaust);

// 检查
if (Keywords.Contains(CardKeyword.Ethereal)) { ... }
```

### 悬浮提示（ExtraHoverTips）

```csharp
protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
{
    HoverTipFactory.FromPower<VulnerablePower>(),  // 能力关键词提示
    HoverTipFactory.FromKeyword(CardKeyword.Exhaust) // 内置关键词提示
};
```

---

## 七、Power 系统

### PowerModel 关键属性

```csharp
public abstract PowerType Type { get; }       // Buff / Debuff
public abstract PowerStackType StackType { get; }  // Counter（叠加）/ Duration（持续回合）
public int Amount { get; }                    // 当前数值
public Creature Owner { get; }                // 持有者
public bool AllowNegative { get; }            // 是否允许负值（Strength 为 true）
```

### PowerModel 钩子方法（override 实现逻辑）

**回合类（async Task）：**
```csharp
AfterSideTurnStart(CombatSide side, CombatState)     // 某方回合开始
AfterPlayerTurnStart(PlayerChoiceContext, Player)     // 玩家回合开始
BeforeTurnEnd(PlayerChoiceContext, CombatSide)        // 回合结束前
AfterTurnEnd(PlayerChoiceContext, CombatSide)         // 回合结束后（Duration 在此递减）
```

**卡牌类（async Task）：**
```csharp
BeforeCardPlayed(CardPlay)
AfterCardPlayed(PlayerChoiceContext, CardPlay)
AfterCardDrawn(PlayerChoiceContext, CardModel, bool)
AfterCardDiscarded(PlayerChoiceContext, CardModel)
AfterCardExhausted(PlayerChoiceContext, CardModel, bool)
```

**伤害/格挡类（同步，返回修改值）：**
```csharp
decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp, Creature? dealer, CardModel?)
decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp, Creature? dealer, CardModel?)
decimal ModifyBlockAdditive(Creature? target, decimal amount, ValueProp, CardPlay?)
decimal ModifyBlockMultiplicative(Creature? target, decimal amount, ValueProp, CardPlay?)
int     ModifyHandDraw(Player, int drawCount)
```

**能力生命周期：**
```csharp
BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel?)
AfterApplied(Creature? applier, CardModel?)
AfterRemoved(Creature oldOwner)
```

**工具方法：**
```csharp
protected void Flash()   // 触发能力图标闪烁动画
```

---

## 八、典型卡牌完整示例

### 8.1 基础攻击

```csharp
public sealed class KarenStrike : CardModel
{
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Strike };
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DamageVar(6m, ValueProp.Move) };

    public KarenStrike() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);
    }

    protected override void OnUpgrade() => base.DynamicVars.Damage.UpgradeValueBy(3m);
}
```

**JSON（`localization/eng/cards.json`）：**
```json
"KAREN_STRIKE.title": "Karen Strike",
"KAREN_STRIKE.description": "Deal {Damage:diff()} damage."
```

### 8.2 基础防御

```csharp
public sealed class KarenDefend : CardModel
{
    public override bool GainsBlock => true;
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Defend };
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new BlockVar(5m, ValueProp.Move) };

    public KarenDefend() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
        => await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

    protected override void OnUpgrade() => base.DynamicVars.Block.UpgradeValueBy(3m);
}
```

### 8.3 攻击 + 施加能力（多 DynamicVar）

```csharp
// 类似 Bash：攻击并施加弱化
public sealed class KarenPerformance : CardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<WeakPower>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<WeakPower>(2m)
    };

    public KarenPerformance() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);
        await PowerCmd.Apply<WeakPower>(cardPlay.Target,
            base.DynamicVars["WeakPower"].BaseValue, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars["WeakPower"].UpgradeValueBy(1m);
    }
}
```

**JSON：**
```json
"KAREN_PERFORMANCE.title": "Karen Performance",
"KAREN_PERFORMANCE.description": "Deal {Damage:diff()} damage.\nApply {WeakPower:diff()} [gold]Weak[/gold]."
```

### 8.4 抽牌 + 弃牌（选牌交互）

```csharp
public sealed class StageCall : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new CardsVar(3) };

    public StageCall() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(ctx, base.DynamicVars.Cards.BaseValue, base.Owner);
        var toDiscard = (await CardSelectCmd.FromHandForDiscard(
            ctx, base.Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1),
            null, this)).FirstOrDefault();
        if (toDiscard != null)
            await CardCmd.Discard(ctx, toDiscard);
    }

    protected override void OnUpgrade() => base.DynamicVars.Cards.UpgradeValueBy(1m);
}
```

### 8.5 升级改变行为（IsUpgraded 条件分支）

```csharp
protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
{
    await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
    if (base.IsUpgraded)
    {
        // 升级后：升级手牌所有卡
        foreach (var card in PileType.Hand.GetPile(base.Owner).Cards.Where(c => c.IsUpgradable))
            CardCmd.Upgrade(card);
    }
    else
    {
        // 未升级：选一张升级
        var chosen = await CardSelectCmd.FromHandForUpgrade(ctx, base.Owner, this);
        if (chosen != null) CardCmd.Upgrade(chosen);
    }
}
```

### 8.6 Exhaust 卡

```csharp
protected override IEnumerable<CardKeyword> CanonicalKeywords
    => new[] { CardKeyword.Exhaust };

// OnPlay 结束后卡牌自动消耗（由 CanonicalKeywords 驱动，无需手动 CardCmd.Exhaust）
```

### 8.7 Power 卡（每回合触发的能力）

```csharp
public sealed class StarPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 每回合开始：力量 +Amount
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Side)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null);
        }
    }
}

// 对应的卡牌
public sealed class StarlightPower : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<StarPower>(2m) };

    public StarlightPower() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        NPowerUpVfx.CreateNormal(base.Owner.Creature);
        await PowerCmd.Apply<StarPower>(
            base.Owner.Creature,
            base.DynamicVars["StarPower"].BaseValue,
            base.Owner.Creature, this);
    }
}
```

---

## 九、Mod 自定义卡牌步骤

1. **创建 C# 类**，继承 `CardModel`，命名约定决定 JSON key：
   - `MyCard` → `MY_CARD`
   - `KarenShineStrike` → `KAREN_SHINE_STRIKE`

2. **实现构造函数**，传入费用/类型/稀有度/目标类型

3. **声明 `CanonicalVars`**（数值参数，驱动 UI 显示）

4. **实现 `OnPlay`**，调用 Cmd 类执行效果

5. **实现 `OnUpgrade`**，修改 DynamicVar 值

6. **添加本地化 JSON**（`localization/eng/cards.json` 和 `localization/zhs/cards.json`）：
   ```json
   "MY_CARD.title": "My Card",
   "MY_CARD.description": "Deal {Damage:diff()} damage."
   ```

7. **无需注册**，游戏启动时反射自动发现所有 `CardModel` 子类

### 本地化文本动态数值绑定规则

```
{DynamicVar名称:diff()}   → 显示数值，升级后会用颜色区分变化
{Damage:diff()}           → 对应 DamageVar（默认名 "Damage"）
{Block:diff()}            → 对应 BlockVar
{Cards:diff()}            → 对应 CardsVar
{WeakPower:diff()}        → 对应 PowerVar<WeakPower>
{IfUpgraded:show:A|B}    → 条件文本（升级前显示 B，升级后显示 A）
```

---

## 十、AoE 多目标实现

```csharp
// 攻击所有对手
await DamageCmd.Attack(damage)
    .FromCard(this)
    .TargetingAllOpponents(base.CombatState)
    .Execute(ctx);

// 给所有对手施加能力
await PowerCmd.Apply<WeakPower>(
    base.CombatState.GetOpponentsOf(base.Owner.Creature),
    2m, base.Owner.Creature, this);

// 获取目标列表
base.CombatState.GetOpponentsOf(base.Owner.Creature)  // 所有对手
base.CombatState.PlayerCreatures                       // 所有玩家生物
base.CombatState.EnemyCreatures                        // 所有敌方生物
```

---

## 参考文件（游戏源码）

- `D:\claudeProj\sts2\src\Core\Models\CardModel.cs`
- `D:\claudeProj\sts2\src\Core\Models\PowerModel.cs`
- `D:\claudeProj\sts2\src\Core\Models\AbstractModel.cs`（所有钩子方法签名）
- `D:\claudeProj\sts2\src\Core\Commands\DamageCmd.cs`
- `D:\claudeProj\sts2\src\Core\Commands\Builders\AttackCommand.cs`
- `D:\claudeProj\sts2\src\Core\Commands\CreatureCmd.cs`
- `D:\claudeProj\sts2\src\Core\Commands\CardPileCmd.cs`
- `D:\claudeProj\sts2\src\Core\Commands\PowerCmd.cs`
- `D:\claudeProj\sts2\src\Core\Commands\CardCmd.cs`
- `D:\claudeProj\sts2\src\Core\Commands\CardSelectCmd.cs`
- `D:\claudeProj\sts2\src\Core\Localization\DynamicVars\` （所有 DynamicVar 类型）
- `D:\claudeProj\sts2\src\Core\Entities\Cards\CardKeyword.cs`
- `D:\claudeProj\sts2\src\Core\Models\Models\Cards\StrikeIronclad.cs`（典型攻击牌）
- `D:\claudeProj\sts2\src\Core\Models\Models\Cards\Bash.cs`（多 DynamicVar）
- `D:\claudeProj\sts2\src\Core\Models\Models\Cards\Acrobatics.cs`（选牌交互）
- `D:\claudeProj\sts2\src\Core\Models\Models\Cards\Whirlwind.cs`（X费 AoE）
- `D:\claudeProj\sts2\src\Core\Models\ModelDb.cs`（反射注册机制）
