# STS2 英文卡牌描述本地化规范

基于 `D:\claudeProj\sts2\localization\eng\cards.json`（STS2 官方）整理。

---

## 一、JSON 键名格式

```json
"CARD_KEY.title": "Card Name",
"CARD_KEY.description": "Effect text.",
"CARD_KEY.selectionScreenPrompt": "Choose a card."
```

- 键名全大写 + 下划线，与卡牌类名的全大写形式一致
- 三种字段：`title`（牌名）、`description`（描述）、`selectionScreenPrompt`（选牌弹窗提示，可选）

---

## 二、基础文字单位

| 用途 | 英文格式 | 示例 |
|------|----------|------|
| 伤害值 | `{Damage:diff()} damage` | `Deal {Damage:diff()} damage.` |
| 格挡值 | `{Block:diff()} [gold]Block[/gold]` | `Gain {Block:diff()} [gold]Block[/gold].` |
| 状态层数 | `{XPower:diff()} [gold]Name[/gold]` | `Apply {WeakPower:diff()} [gold]Weak[/gold].` |
| 卡牌数量 | `{Cards:diff()} {Cards:plural:card\|cards}` | `Draw {Cards:diff()} {Cards:plural:card\|cards}.` |
| 能量图标（变量） | `{Energy:energyIcons()}` | `Gain {Energy:energyIcons()}.` |
| 费用标注（文中 0 费） | `0{energyPrefix:energyIcons(1)}` | `It costs 0 {energyPrefix:energyIcons(1)} this combat.` |
| 星点图标 | `{Stars:starIcons()}` | `Gain {Stars:starIcons()}.` |
| 单个星点符号（引用） | `{singleStarIcon}` | `Whenever you spend {singleStarIcon}, ...` |

---

## 三、牌堆名称（均用 `[gold]` 高亮）

| 牌堆 | 英文 |
|------|------|
| 手牌 | `[gold]Hand[/gold]` |
| 抽牌堆 | `[gold]Draw Pile[/gold]` |
| 弃牌堆 | `[gold]Discard Pile[/gold]` |
| 消耗牌堆 | `[gold]Exhaust Pile[/gold]` |
| 约定牌堆（自定义） | `[gold]Promise Pile[/gold]` |

---

## 四、常见关键词（均用 `[gold]` 高亮）

| 关键词 | 英文 |
|--------|------|
| 格挡 | `[gold]Block[/gold]` |
| 力量 | `[gold]Strength[/gold]` |
| 敏捷 | `[gold]Dexterity[/gold]` |
| 集中 | `[gold]Focus[/gold]` |
| 活力 | `[gold]Vigor[/gold]` |
| 覆甲 | `[gold]Plating[/gold]` |
| 易伤 | `[gold]Vulnerable[/gold]` |
| 虚弱 | `[gold]Weak[/gold]` |
| 中毒 | `[gold]Poison[/gold]` |
| 灾厄 | `[gold]Doom[/gold]` |
| 消耗（动词） | `[gold]Exhaust[/gold]` |
| 消耗状态（已消耗） | `[gold]Exhausted[/gold]` |
| 升级（动词） | `[gold]Upgrade[/gold]` |
| 变化（动词） | `[gold]Transform[/gold]` |
| 保留（动词/关键词） | `[gold]Retain[/gold]` |
| 生成充能球 | `[gold]Channel[/gold]` |
| 激发充能球 | `[gold]Evoke[/gold]` |
| 斩杀 | `[gold]Fatal[/gold]` |
| 铸造 | `[gold]Forge[/gold]` |
| 无实体 | `[gold]Intangible[/gold]` |
| 虚无 | `[gold]Ethereal[/gold]` |
| 重放 | `[gold]Replay[/gold]` |
| 奇巧 | `[gold]Sly[/gold]` |
| 召唤 | `[gold]Summon[/gold]` |
| 固有 | `[gold]Innate[/gold]`（不在描述中显示，卡牌属性决定） |
| 闪耀（本 Mod 自定义） | `[gold]Shine[/gold]` |

---

## 五、固定动作短语

### 5.1 伤害

```
Deal {Damage:diff()} damage.
Deal {Damage:diff()} damage to ALL enemies.
Deal {Damage:diff()} damage twice.
Deal {Damage:diff()} damage {Repeat:diff()} {Repeat:plural:time|times}.
Deal {Damage:diff()} damage to ALL enemies {Repeat:diff()} {Repeat:plural:time|times}.
Deal {CalculatedDamage:diff()} damage.      （配合条件加成使用）
```

注意：全体 / 多段均用 **ALL**（大写）强调全体。

### 5.2 格挡

```
Gain {Block:diff()} [gold]Block[/gold].
Give another player {Block:diff()} [gold]Block[/gold].
Gain [gold]Block[/gold] equal to your [gold]Block[/gold].
Double your [gold]Block[/gold].
[gold]Block[/gold] is not removed at the start of your turn.
[gold]Block[/gold] is not removed at the start of your next turn.
```

### 5.3 抽牌 / 弃牌

```
Draw {Cards:diff()} {Cards:plural:card|cards}.
Draw 1 card.
Discard 1 card.
Discard {Cards:diff()} {Cards:plural:card|cards}.
Discard your [gold]Hand[/gold].
Put a card from your [gold]Discard Pile[/gold] into your [gold]Hand[/gold].
Put a card from your [gold]Discard Pile[/gold] on top of your [gold]Draw Pile[/gold].
Put {Cards:diff()} cards from your [gold]Draw Pile[/gold] into your [gold]Hand[/gold].
```

### 5.4 能量

```
Gain {Energy:energyIcons()}.
Next turn, gain {Energy:energyIcons()}.
Double your Energy.
```

### 5.5 施加负面状态（给敌人）

```
Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].
Apply {WeakPower:diff()} [gold]Weak[/gold].
Apply {PoisonPower:diff()} [gold]Poison[/gold].
Apply {DoomPower:diff()} [gold]Doom[/gold].
Apply {WeakPower:diff()} [gold]Weak[/gold] and [gold]Vulnerable[/gold] to ALL enemies.
Enemy loses {StrengthLoss:diff()} [gold]Strength[/gold] this turn.
ALL enemies lose {StrengthLoss:diff()} [gold]Strength[/gold] this turn.
```

### 5.6 施加增益（给自己）

```
Gain {StrengthPower:diff()} [gold]Strength[/gold].
Gain {DexterityPower:diff()} [gold]Dexterity[/gold].
Gain {FocusPower:diff()} [gold]Focus[/gold].
Gain {StrengthPower:diff()} [gold]Strength[/gold] this turn.
Gain {DexterityPower:diff()} [gold]Dexterity[/gold] this turn.
```

### 5.7 生命相关

```
Lose {HpLoss:diff()} HP.
Take {Damage:diff()} damage.              （自身受伤时用 take）
Raise your Max HP by {MaxHp:diff()}.
Lose {MaxHp:diff()} Max HP.
```

### 5.8 卡牌操作

```
[gold]Exhaust[/gold] 1 card.
[gold]Exhaust[/gold] your [gold]Hand[/gold].
[gold]Upgrade[/gold] {Cards:diff()} random {Cards:plural:card|cards} in your [gold]Discard Pile[/gold].
[gold]Upgrade[/gold] {IfUpgraded:show:ALL cards|a card} in your [gold]Hand[/gold].
Add a copy of this card into your [gold]Discard Pile[/gold].
Return this card to your [gold]Hand[/gold].
[gold]Retain[/gold] your [gold]Hand[/gold] this turn.
Permanently increase this card's [gold]Block[/gold] by {Increase:diff()}.
```

### 5.9 充能球操作

```
[gold]Channel[/gold] 1 [gold]Lightning[/gold].
[gold]Channel[/gold] 1 [gold]Frost[/gold].
[gold]Channel[/gold] 1 [gold]Dark[/gold].
[gold]Channel[/gold] 1 [gold]Plasma[/gold].
[gold]Channel[/gold] 1 [gold]Glass[/gold].
[gold]Evoke[/gold] your rightmost Orb twice.
[gold]Evoke[/gold] your leftmost Orb.
Trigger the passive ability of your rightmost Orb.
```

---

## 六、触发条件句型

### 6.1 时间触发

```
At the start of your turn, ...
At the end of your turn, ...
Next turn, ...
This turn, ...
This combat, ...
At the start of your next turn, ...
```

### 6.2 事件触发

```
Whenever you play a card, ...
Whenever you play an Attack, ...
Whenever you play a Skill, ...
Whenever you draw a card, ...
Whenever you gain [gold]Block[/gold], ...
Whenever you are attacked, ...
Whenever a card is [gold]Exhausted[/gold], ...
Whenever an Attack deals unblocked damage, ...
```

### 6.3 条件判断

```
If ..., ...
If [gold]Fatal[/gold], ...
If the enemy is [gold]Vulnerable[/gold], ...
If you have [gold]Exhausted[/gold] a card this turn, ...
If this is in your [gold]Hand[/gold], ...
Can only be played if ...
```

### 6.4 计数触发

```
Deals {ExtraDamage:diff()} additional damage for each card in your [gold]Exhaust Pile[/gold].
Deals {ExtraDamage:diff()} additional damage for each [gold]Vulnerable[/gold] on the enemy.
Deals {ExtraDamage:diff()} additional damage for ALL your cards containing "Strike".
Gain {StrengthPerVulnerable:diff()} [gold]Strength[/gold] for each [gold]Vulnerable[/gold] on the enemy.
```

### 6.5 回合结束手牌留存惩罚

固定模板：
```
At the end of your turn, if this is in your [gold]Hand[/gold], take {Damage:diff()} damage.
At the end of your turn, if this is in your [gold]Hand[/gold], lose {HpLoss:diff()} HP.
At the end of your turn, if this is in your [gold]Hand[/gold], gain {WeakPower:diff()} [gold]Weak[/gold].
At the end of your turn, if this is in your [gold]Hand[/gold], lose {Gold:diff()} [gold]Gold[/gold].
```

### 6.6 实战显示（InCombat）

战斗中括号内显示实际计算值（仅战斗时可见）：

```
{InCombat:\n(Hits {CalculatedHits:diff()} {CalculatedHits:plural:time|times})|}
{InCombat:\n(Deals {CalculatedDamage:diff()} damage)|}
{InCombat:\n(Gain {CalculatedBlock:diff()} [gold]Block[/gold])|}
{InCombat:\n(Draw {CalculatedCards:diff()} {CalculatedCards:plural:card|cards})|}
```

---

## 七、升级差异句型

```
{IfUpgraded:show:升级后文本|未升级文本}
```

**注意**：升级后文本在前，未升级文本在后（与中文版相同）。

常见用例：

```
[gold]Upgrade[/gold] {IfUpgraded:show:ALL cards|a card} in your [gold]Hand[/gold].
This {IfUpgraded:show:combat|turn}, ...
Add a [gold]{IfUpgraded:show:Soul+|Soul}[/gold] into your [gold]Draw Pile[/gold].
Trigger the passive ability of your rightmost Orb{IfUpgraded:show: 2 times|}.
{IfUpgraded:show:X+1|X}
```

注意：`{IfUpgraded:show:text|}`（无升级版本时后半部分为空）也是合法写法。

---

## 八、费用相关

### 8.1 文中标注 0 费

```
It costs 0 {energyPrefix:energyIcons(1)} this combat.
It's free to play this turn.
ALL cards in your [gold]Hand[/gold] are free to play this turn.
Skills cost 0 {energyPrefix:energyIcons(1)}.
Reduce the cost of ALL cards in your [gold]Hand[/gold] to 1 this {IfUpgraded:show:combat|turn}.
Add a random Skill into your [gold]Hand[/gold]. It's free to play this turn.
Add 1 random card into your [gold]Hand[/gold]. It costs 0 {energyPrefix:energyIcons(1)} this turn.
```

### 8.2 X 费卡

```
Deal {Damage:diff()} damage X times.
Play the top X{IfUpgraded:show:+1} cards of your [gold]Draw Pile[/gold].
[gold]Evoke[/gold] your rightmost Orb {IfUpgraded:show:X+1|X} times.
```

### 8.3 费用变化

```
Reduce this card's cost by 1.
Reduce this card's cost to 0 {energyPrefix:energyIcons(1)}.
Increase the cost of this card by 1.
```

---

## 九、复数语法（plural）

格式：`{VarName:plural:singular|plural}`

常见示例：

```
{Cards:plural:card|cards}
{Repeat:plural:time|times}
{Turns:plural:turn|turns}
{Combats:plural:combat|combats}
{OrbSlots:plural:Slot|Slots}
{CalculatedHits:plural:time|times}
```

特殊：值为 1 时显示单数，其余显示复数。也可用条件形式：
```
{Cards:plural:It gains|They gain} [gold]Ethereal[/gold].
{BufferPower:plural:time|{BufferPower:diff()} times}
```

---

## 十、打出条件（特殊限制句）

放在描述**第一行**：

```
Can only be played if every card in your [gold]Hand[/gold] is an Attack.
Can only be played if there are no cards in your [gold]Draw Pile[/gold].
Can only be played if you have {Cards:diff()} or more {Cards:plural:card|cards} in your [gold]Exhaust Pile[/gold].
```

---

## 十一、多段攻击写法

| 效果 | 英文写法 |
|------|----------|
| 固定两次 | `Deal {Damage:diff()} damage twice.` |
| 固定 N 次 | `Deal {Damage:diff()} damage {Repeat:diff()} {Repeat:plural:time|times}.` |
| 全体 N 次 | `Deal {Damage:diff()} damage to ALL enemies {Repeat:diff()} {Repeat:plural:time|times}.` |
| X 次 | `Deal {Damage:diff()} damage X times.` |

---

## 十二、加成伤害模板

```
Deal {CalculatedDamage:diff()} damage.
Deals {ExtraDamage:diff()} additional damage for each card in your [gold]Exhaust Pile[/gold].
Deals {ExtraDamage:diff()} additional damage for each [gold]Vulnerable[/gold] on the enemy.
Deals {ExtraDamage:diff()} additional damage for each other Attack you've played this turn.
Deals {ExtraDamage:diff()} additional damage for each card discarded this turn.
Deals {ExtraDamage:diff()} additional damage for ALL your cards that have a {singleStarIcon} cost.
```

---

## 十三、选牌界面提示（selectionScreenPrompt）

简洁祈使句，通常以动词开头，**句末加句号**（部分官方条目省略，本 Mod 统一加句号）：

```
Choose a card to put on top of your Draw Pile.
Choose a card to put back in your Hand.
Choose {Amount} {Amount:plural:card|cards} to put into your [gold]Hand[/gold]
Choose a card to Copy.
Choose a Card.
Choose a card to add [gold]Sly[/gold] to.
Choose cards to [gold]Transform[/gold].
```

---

## 十四、约定牌堆操作（PromisePile，自定义机制）

### 14.1 放入约定牌堆

```
Place {Cards:diff()} {Cards:plural:card|cards} from your [gold]Hand[/gold] into your [gold]Promise Pile[/gold].
After being played, this card enters your [gold]Promise Pile[/gold].
```

### 14.2 从约定牌堆取出

```
Draw {Cards:diff()} {Cards:plural:card|cards} from your [gold]Promise Pile[/gold].
```

---

## 十五、全局格式规范总结

1. **句末加句号**`.`，每行效果独立成句
2. **换行用** `\n`，多效果卡每条效果单独一行
3. **数值与单词之间加空格**：`Gain {Block:diff()} [gold]Block[/gold].`（英文版保留空格）
4. **能量图标不加 "energy" 单位文字**：`Gain {Energy:energyIcons()}.`（不写 "Gain N energy"）
5. **状态层数直接跟关键词**：`Apply {WeakPower:diff()} [gold]Weak[/gold].`（无 "stacks of"）
6. **关键词统一金色高亮** `[gold]...[/gold]`
7. **"ALL" 大写**表示全体：`ALL enemies`, `ALL cards`, `ALL allies`
8. **"this turn" vs "this combat" vs 无限制** 要准确区分：
   - `this turn` = 当前这个行动轮
   - `this combat` = 当前这场战斗
   - 无限制（永久效果）= 不加时间限定词
9. **HP 大写**：`Lose {HpLoss:diff()} HP.`（不是 "Hp" 或 "hp"）
10. **"take X damage"** 用于自身受伤；**"deal X damage"** 用于对敌人造成伤害
11. **"Apply"** 用于施加状态；**"Gain"** 用于自身获得增益
12. **敌方失去力量**用 "Enemy loses" / "ALL enemies lose"（不用 "Apply -X Strength"）
13. **`{energyPrefix:energyIcons(1)}`** 表示 1 费图标（用于说明某牌的费用为 0 时，写作 `0{energyPrefix:energyIcons(1)}`）
