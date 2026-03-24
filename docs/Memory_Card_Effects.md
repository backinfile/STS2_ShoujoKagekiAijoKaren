# STS2 卡牌效果系统速查

> 详细文档见 `docs/CardEffects_Guide.md`

## 架构要点

- **无独立 ICardEffect 接口**，效果写在 `CardModel.OnPlay()` 中
- **注册机制**：纯反射，无需手动注册。类名 Slugify 为 JSON key（`KarenStrike` → `KAREN_STRIKE`）
- **源码路径**：`D:\claudeProj\sts2\src\Core\Models\Cards\`（~584 张卡牌参考）

## CardModel 构造函数参数

```csharp
protected CardModel(int cost, CardType type, CardRarity rarity, TargetType targetType)

// CardType:    Attack / Skill / Power / Curse / Status
// CardRarity:  Basic / Common / Uncommon / Rare / Special
// TargetType:  Self / AnyEnemy / AllEnemies / None
```

## DynamicVar 速查

| 类 | 文本占位符 | 用途 |
|----|-----------|------|
| `DamageVar(decimal, ValueProp.Move)` | `{Damage:diff()}` | 攻击伤害 |
| `BlockVar(decimal, ValueProp.Move)` | `{Block:diff()}` | 格挡值 |
| `CardsVar(int)` | `{Cards:diff()}` | 抽牌/操作数量 |
| `PowerVar<T>(decimal)` | `{T类名:diff()}` | 能力层数 |
| `IntVar(string, decimal)` | `{自定义名:diff()}` | 通用整数 |
| `HealVar / MaxHpVar / GoldVar` | `{Heal/MaxHp/Gold:diff()}` | 特殊数值 |

- 多个 DynamicVar：使用 `new DynamicVar[] { ... }` 而不是 `ReadOnlySingleElementList`
- 升级：`base.DynamicVars.Damage.UpgradeValueBy(3m)`
- 通过 Name 访问：`base.DynamicVars["StrengthPower"].BaseValue`

## Cmd 类速查

```csharp
// 攻击（单体）
await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
    .FromCard(this).Targeting(cardPlay.Target)
    .WithHitFx("vfx/vfx_attack_slash").Execute(ctx);

// 攻击（AoE）
await DamageCmd.Attack(damage).FromCard(this)
    .TargetingAllOpponents(base.CombatState).Execute(ctx);

// 格挡
await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

// 抽牌
await CardPileCmd.Draw(ctx, count, base.Owner);

// 施加能力
await PowerCmd.Apply<StrengthPower>(target, amount, applier, this);

// 弃牌/消耗
await CardCmd.Discard(ctx, card);
await CardCmd.Exhaust(ctx, card);

// 升级
CardCmd.Upgrade(card);

// 选牌交互
var cards = await CardSelectCmd.FromHandForDiscard(ctx, base.Owner,
    new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), null, this);
```

## Power 钩子速查（override 方法）

```csharp
// 回合开始/结束（async Task）
AfterSideTurnStart(CombatSide side, CombatState)   // side == Owner.Side 判断
AfterTurnEnd(PlayerChoiceContext, CombatSide)       // Duration 在此 TickDownDuration

// 伤害/格挡修改（同步，返回 decimal）
ModifyDamageAdditive(target, amount, ValueProp, dealer, card)    // Strength 用此
ModifyDamageMultiplicative(target, amount, ValueProp, dealer, card)  // Vulnerable 用此
ModifyBlockAdditive(target, amount, ValueProp, cardPlay)          // Dexterity 用此

// 卡牌事件（async Task）
AfterCardPlayed(PlayerChoiceContext, CardPlay)
AfterCardDrawn(PlayerChoiceContext, CardModel, bool)

// 工具
protected void Flash()   // 图标闪烁动画
```

## CardKeyword 枚举

```csharp
CardKeyword.Exhaust / Ethereal / Innate / Retain / Unplayable / Sly / Eternal
```

## AoE 目标获取

```csharp
base.CombatState.GetOpponentsOf(base.Owner.Creature)  // 所有对手
base.CombatState.PlayerCreatures                       // 所有玩家生物
base.CombatState.EnemyCreatures                        // 所有敌方生物
```

## 常用特效路径

```
"vfx/vfx_attack_slash"           — 普通斩击
"vfx/vfx_attack_blunt"           — 钝击
"vfx/vfx_flying_slash"           — 飞斩
"vfx/vfx_giant_horizontal_slash" — 横扫大招
```
