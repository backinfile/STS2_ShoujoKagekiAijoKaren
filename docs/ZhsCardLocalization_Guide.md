# STS2 中文卡牌描述本地化规范

基于以下两个来源整理，**当两者规范冲突时，以 STS2 官方规范为准**：
- `D:\claudeProj\sts2\localization\zhs\cards.json`（STS2 官方）
- `D:\Github\STS_ShoujoKageki\…\ShoujoKageki-Card-Strings.json`（STS1 少女歌剧 Mod）

---

## 一、基础文字单位

### 数值单位

| 用途 | 中文格式 | 示例 |
|------|----------|------|
| 伤害/格挡/HP/力量等"点"类 | `N点XXX` | `造成{Damage:diff()}点伤害` |
| 层叠状态（易伤/虚弱/中毒等） | `N层[gold]XXX[/gold]` | `给予{WeakPower:diff()}层[gold]虚弱[/gold]` |
| 卡牌数量 | `N张牌` | `抽{Cards:diff()}张牌` |
| 能量 | `{Energy:energyIcons()}` | `获得{Energy:energyIcons()}` |
| 充能球数量 | `N个[gold]XXX[/gold]充能球` | `[gold]生成[/gold]1个[gold]闪电[/gold]充能球` |

---

## 二、牌堆名称（均用 `[gold]` 高亮）

| 英文 | 中文 |
|------|------|
| Hand | `[gold]手牌[/gold]` |
| Draw Pile | `[gold]抽牌堆[/gold]` |
| Discard Pile | `[gold]弃牌堆[/gold]` |
| Exhaust Pile | `[gold]消耗牌堆[/gold]` |
| Promise Pile（自定义） | `[gold]约定牌堆[/gold]` |

---

## 三、常见关键词（均用 `[gold]` 高亮）

| 关键词 | 格式 |
|--------|------|
| 格挡 | `[gold]格挡[/gold]` |
| 力量 | `[gold]力量[/gold]` |
| 敏捷 | `[gold]敏捷[/gold]` |
| 集中 | `[gold]集中[/gold]` |
| 活力 | `[gold]活力[/gold]` |
| 易伤 | `[gold]易伤[/gold]` |
| 虚弱 | `[gold]虚弱[/gold]` |
| 中毒 | `[gold]中毒[/gold]` |
| 灾厄 | `[gold]灾厄[/gold]` |
| 消耗（动词，如"消耗1张牌"） | `[gold]消耗[/gold]` |
| 升级（动词） | `[gold]升级[/gold]` |
| 变化（动词） | `[gold]变化[/gold]` |
| 保留（动词，如"在本回合保留手牌"） | `[gold]保留[/gold]` |
| 生成（充能球） | `[gold]生成[/gold]` |
| 激发（充能球） | `[gold]激发[/gold]` |
| 斩杀 | `[gold]斩杀[/gold]` |
| 铸造 | `[gold]铸造[/gold]` |
| 无实体 | `[gold]无实体[/gold]` |
| 覆甲 | `[gold]覆甲[/gold]` |
| 虚无（关键词） | `[gold]虚无[/gold]` |
| 重放 | `[gold]重放[/gold]` |
| 奇巧 | `[gold]奇巧[/gold]` |
| 召唤 | `[gold]召唤[/gold]` |
| 固有（Innate） | `[gold]固有[/gold]` |
| 闪耀（Shine，本 Mod 自定义） | `[gold]闪耀[/gold]` |

---

## 四、固定动作短语

### 4.1 伤害

```
造成{Damage:diff()}点伤害。
对所有敌人造成{Damage:diff()}点伤害。
造成{Damage:diff()}点伤害两次。
造成{Damage:diff()}点伤害{Repeat:diff()}次。
随机对敌人造成{Damage:diff()}点伤害{Repeat:diff()}次。
造成{CalculatedDamage:diff()}点伤害。   （配合条件加成使用）
```

### 4.2 格挡

```
获得{Block:diff()}点[gold]格挡[/gold]。
给予另一名玩家{Block:diff()}点[gold]格挡[/gold]。
获得等量于...的[gold]格挡[/gold]。
将你当前的[gold]格挡[/gold]翻倍。
```

### 4.3 抽牌 / 弃牌

```
抽{Cards:diff()}张牌。
抽1张牌。
丢弃1张牌。
丢弃{Cards:diff()}张牌。
丢弃所有[gold]手牌[/gold]。
从[gold]抽牌堆[/gold]中选择{Cards:diff()}张牌放入你的[gold]手牌[/gold]。
从[gold]弃牌堆[/gold]中选择{Cards:diff()}张牌放入你的[gold]手牌[/gold]。
从[gold]抽牌堆[/gold]、[gold]弃牌堆[/gold]，或[gold]手牌[/gold]中选择{Cards:diff()}张牌...
从...中选择任意数量的牌放入你的[gold]手牌[/gold]。
```

### 4.4 能量

```
获得{Energy:energyIcons()}。
在下个回合获得{Energy:energyIcons()}。
将你的能量翻倍。
```

### 4.5 施加负面状态（给敌人）

```
给予{VulnerablePower:diff()}层[gold]易伤[/gold]。
给予{WeakPower:diff()}层[gold]虚弱[/gold]。
给予{PoisonPower:diff()}层[gold]中毒[/gold]。
给予{DoomPower:diff()}层[gold]灾厄[/gold]。
给予所有敌人{Power:diff()}层[gold]虚弱[/gold]和[gold]易伤[/gold]。
```

### 4.6 施加增益（给自己）

```
获得{StrengthPower:diff()}点[gold]力量[/gold]。
获得{DexterityPower:diff()}点[gold]敏捷[/gold]。
获得{FocusPower:diff()}点[gold]集中[/gold]。
在本回合获得{StrengthPower:diff()}点[gold]力量[/gold]。
```

### 4.7 失去生命 / 受伤 / 回复

```
失去{HpLoss:diff()}点生命。
受到{Damage:diff()}点伤害。
永久获得{MaxHp:diff()}点最大生命值。
失去{MaxHp:diff()}点最大生命。
回复{Heal:diff()}点生命。
```

### 4.8 卡牌操作

```
[gold]消耗[/gold]1张牌。
[gold]消耗[/gold]你的[gold]手牌[/gold]中随机一张...。
[gold]消耗[/gold]所有[gold]手牌[/gold]。
随机[gold]升级[/gold]你[gold]弃牌堆[/gold]中的{Cards:diff()}张牌。
[gold]升级[/gold]你[gold]手牌[/gold]中的{IfUpgraded:show:所有牌|一张牌}。
将一张此牌的复制品加入你的[gold]弃牌堆[/gold]。
将你在本回合打出的下一张牌放置到你的[gold]抽牌堆[/gold]顶部。
将[gold]弃牌堆[/gold]中的一张牌放到[gold]抽牌堆[/gold]顶部。
将[gold]弃牌堆[/gold]中的一张牌放入你的[gold]手牌[/gold]。
将此牌返回你的[gold]手牌[/gold]。
在本回合[gold]保留[/gold]你的[gold]手牌[/gold]。
打出后进入[gold]约定牌堆[/gold]。
复制所有[gold]手牌[/gold]到[gold]约定牌堆[/gold]。
从[gold]约定牌堆[/gold]抽出的牌将被[gold]升级[/gold]。
能被多次[gold]升级[/gold]。
```

### 4.9 充能球操作

```
[gold]生成[/gold]1个[gold]闪电[/gold]充能球。
[gold]生成[/gold]1个[gold]冰霜[/gold]充能球。
[gold]生成[/gold]1个[gold]黑暗[/gold]充能球。
[gold]生成[/gold]1个[gold]等离子[/gold]充能球。
[gold]生成[/gold]1个[gold]玻璃[/gold]充能球。
[gold]激发[/gold]你最右侧的充能球两次。
[gold]激发[/gold]你最左侧的充能球。
[gold]激发[/gold]所有充能球。
```

---

## 五、触发条件句型

### 5.1 时间触发

```
在你的回合开始时，...              （每回合起始，限定为你的回合）
在你的回合结束时，...              （每回合结束）
每场战斗开始时，...                （每场战斗开局一次）
战斗结束时，...                    （战斗结束后一次）
在下个回合...
在本回合...
在本场战斗中...
在本局游戏中...
每回合...
每当你打出一张...时，...
每当你抽到...时，...
每当你给予...时，...
每当有一张牌被[gold]消耗[/gold]时，...
此牌在牌堆之间移动时，...          （每次在任意牌堆间移动都触发）
被放入[gold]约定牌堆[/gold]时，...  （进入约定牌堆时触发）
从[gold]约定牌堆[/gold]中抽出此牌时，...
[gold]闪耀[/gold]耗尽时，...        （Shine 值归零/耗尽时触发）
```

### 5.2 条件判断

```
如果...，则...
只有在...才能被打出。
如果这张牌在你的[gold]手牌[/gold]中，...    （常用于回合结束惩罚）
如果该敌人有[gold]易伤[/gold]状态，则...
如果你在本回合...，则...
如果[gold]约定牌堆[/gold]不为空，...
如果[gold]约定牌堆[/gold]中有牌，...
不能被打出。                                  （纯触发型卡牌，无法主动出牌）
```

### 5.3 计数触发

```
你的[gold]手牌[/gold]中每有一张...，...
敌人身上每有一层[gold]易伤[/gold]，就...
你在本回合中每打出过一张...，...
你每打出一张...，都...
本场战斗中每打出过一张...，此牌就...一次
```

### 5.4 斩杀触发

```
[gold]斩杀[/gold]时，...
```

### 5.5 实战显示（InCombat）

战斗中在括号内显示实际计算值（仅战斗时可见）：

```
{InCombat:\n（命中{CalculatedHits:diff()}次）|}
{InCombat:\n（造成{CalculatedDamage:diff()}点伤害）|}
{InCombat:\n（获得{CalculatedBlock:diff()}点[gold]格挡[/gold]）|}
{InCombat:\n（抽{CalculatedCards:diff()}张牌）|}
```

---

## 六、升级差异句型

```
{IfUpgraded:show:升级后文本|未升级文本}
```

常见用例：

```
[gold]升级[/gold]你[gold]手牌[/gold]中的{IfUpgraded:show:所有牌|一张牌}。
在这{IfUpgraded:show:场战斗|个回合}，...
将一张[gold]{IfUpgraded:show:灵魂+|灵魂}[/gold]放入...
```

---

## 七、费用相关

0费标注格式（内嵌在描述中）：

```
这张牌在本回合的耗能为0{energyPrefix:energyIcons(1)}。
这张牌在本回合内可以免费打出。
这张牌在本回合免费打出。
其耗能为0{energyPrefix:energyIcons(1)}。
耗能降低至1。
将...的耗能减少{energyPrefix:energyIcons(1)}。
```

X费卡（X倍效果）：

```
造成{Damage:diff()}点伤害X次。
[gold]生成[/gold]{IfUpgraded:show:X+1|X}个[gold]闪电[/gold]充能球。
```

---

## 八、选牌界面提示（selectionScreenPrompt）

选牌提示格式简洁，通常以"选择"开头：

```
选择一张牌放到你的抽牌堆顶。
选择一张牌加入你的手牌。
选择{Amount}张牌加入你的[gold]手牌[/gold]。
选择最多{Cards:diff()}张牌[gold]消耗[/gold]。
选择一张攻击牌加入你的手牌。
选择一张技能牌加入你的手牌。
选择一张牌[gold]消耗[/gold]。
选择要[gold]变化[/gold]的牌。
选择要添加[gold]重放[/gold]的卡牌。
选择要添加[gold]奇巧[/gold]的卡牌。
选择要添加[gold]虚无[/gold]的卡牌。
选择要[gold]消耗[/gold]的牌。
选择要放到你抽牌堆顶的牌
选择一张牌复制。
```

注意：selectionScreenPrompt **有时不加句号**（不一致，参照原文）。

---

## 九、多段攻击写法

| 效果 | 中文写法 |
|------|----------|
| 固定N次 | `造成{Damage:diff()}点伤害两次。` / `{Repeat:diff()}次` |
| 随机N次 | `随机对敌人造成{Damage:diff()}点伤害{Repeat:diff()}次。` |
| 当前N段 | `当前每有一个[gold]充能球[/gold]，造成{Damage:diff()}点伤害。` |
| X次 | `造成{Damage:diff()}点伤害X次。` |

---

## 十、加成伤害模板

```
造成{CalculatedDamage:diff()}点伤害。
...每有一层[gold]易伤[/gold]就额外造成{ExtraDamage:diff()}点伤害。
你的[gold]消耗牌堆[/gold]中每有一张牌，伤害增加{ExtraDamage:diff()}。
本回合其他玩家每攻击过一次该敌人，该牌造成的伤害就额外增加{ExtraDamage:diff()}点。
你在本回合中每打出过一张其他攻击牌，这张牌的伤害就提升{ExtraDamage:diff()}点。
```

---

## 十一、回合结束惩罚（手牌留存惩罚）

固定模板：

```
在你的回合结束时，如果这张牌在你的[gold]手牌[/gold]中，你受到{Damage:diff()}点伤害。
在你的回合结束时，如果这张牌在你的[gold]手牌[/gold]中，则失去{HpLoss:diff()}点生命。
在你的回合结束时，如果这张牌在你的[gold]手牌[/gold]中，则失去{Gold:diff()}[gold]金币[/gold]。
在你的回合结束时，如果这张牌在你的[gold]手牌[/gold]中，获得{WeakPower:diff()}层[gold]虚弱[/gold]。
```

---

## 十二、约定牌堆操作（PromisePile，自定义机制）

约定牌堆是本 Mod 的核心自定义机制，以下是所有相关描述句型：

### 12.1 放入约定牌堆

```
将{Cards:diff()}张牌放入[gold]约定牌堆[/gold]。
将{Cards:diff()}张[gold]手牌[/gold]放入[gold]约定牌堆[/gold]。
将{Cards:diff()}张*牌名放入[gold]约定牌堆[/gold]。
打出后进入[gold]约定牌堆[/gold]。
此牌和你打出的下{Repeat:diff()}张牌进入[gold]约定牌堆[/gold]。
```

### 12.2 从约定牌堆取出

```
从[gold]约定牌堆[/gold]中抽{Cards:diff()}张牌。
从[gold]约定牌堆[/gold]抽牌直到[gold]手牌[/gold]上限。
从[gold]约定牌堆[/gold]中选择{Cards:diff()}张牌加入你的[gold]手牌[/gold]。
从[gold]约定牌堆[/gold]中选择任意数量的牌放入你的[gold]手牌[/gold]。
弃置其余牌。                         （搭配选取时使用）
```

### 12.3 约定牌堆状态触发

```
[gold]约定牌堆[/gold]被清空时，...
[gold]约定牌堆[/gold]之外只有这张牌时才能打出。
如果[gold]约定牌堆[/gold]不为空，...
[gold]约定牌堆[/gold]中每有{N}张牌，...
```

### 12.4 约定牌堆内卡牌行为

```
从[gold]约定牌堆[/gold]抽出的牌将被[gold]升级[/gold]，且被打出时[gold]消耗[/gold]。
被放入[gold]约定牌堆[/gold]时，在本场战斗中耗能减少{energyPrefix:energyIcons(1)}。
回合开始时如果在[gold]约定牌堆[/gold]中，获得{Energy:energyIcons()}。
```

### 12.5 复制手牌到约定牌堆

```
复制所有[gold]手牌[/gold]到[gold]约定牌堆[/gold]。
[gold]消耗[/gold][gold]约定牌堆[/gold]中的所有牌，每[gold]消耗[/gold]一张，放入{Cards:diff()}张随机卡牌。
```

---

## 十三、打出条件（特殊限制句）

放在描述的**第一行**，描述打出此牌的前提条件：

```
不能被打出。                                          （纯触发型/标记型）
只有当你的[gold]抽牌堆[/gold]中没有牌时才能打出。
[gold]手牌[/gold]满时才能打出。
[gold]手牌[/gold]中的每一张牌都是攻击牌时才能打出。
[gold]约定牌堆[/gold]之外只有这张牌时才能打出。
只有在你的[gold]消耗牌堆[/gold]拥有大于等于{Cards:diff()}张牌的时候才能被打出。
```

条件描述（`EXTENDED_DESCRIPTION`）用于在无法打出时的说明，如：

```
"这张牌不能被打出。"
"约定牌堆中卡牌数量不足。"
```

---

## 十四、永久自增效果（战斗内/跨战斗）

```
这张牌每被打出一次，在本场战斗中其攻击次数增加1。
每打出一次，这张牌在本局游戏中的[gold]格挡[/gold]值永久增加{Increase:diff()}点。
在本场战斗中每回合结束时如果在[gold]约定牌堆[/gold]中，此牌伤害增加{Damage:diff()}。
```

---

## 十五、全局规范总结

1. **句末加句号** `。`，每行效果独立成句
2. **换行**用 `\n`，多效果卡每条效果单独一行
3. **数值与中文字符之间不加空格**：`获得{Block:diff()}点[gold]格挡[/gold]。`
4. **能量图标不加"点能量"单位**：`获得{Energy:energyIcons()}。` 而非"获得N点能量"
5. **状态层数用"层"**，不用"点"：`给予3层[gold]易伤[/gold]`
6. **关键词统一金色高亮** `[gold]...[/gold]`
7. **动词"给予"用于施加负面效果**；**"获得"用于自身获益**
8. **"本回合"** vs **"本场战斗"** vs **"本局游戏"** 要准确区分：
   - 本回合 = 当前这个行动轮
   - 本场战斗 = 当前这场战斗（出了战斗房间即重置）
   - 本局游戏 = 整个游戏存档周期（永久）
9. **"在你的回合开始时"** = at the start of YOUR turn（非"每回合开始"）
10. **HP 回复也用"点"**：`回复{Heal:diff()}点生命。`（不要省略"点"）
11. **触发时间词区分**：
    - `在你的回合开始时，` — 每回合起始（最常用）
    - `每场战斗开始时，` — 战斗开局一次
    - `战斗结束时，` — 离开战斗时一次
12. **卡牌关键词自动显示，禁止写入描述**：`消耗`（Exhaust）、`保留`（Retain）、`固有`（Innate）等卡牌关键词由游戏自动展示为 badge，**不需要也不应该**在 `description` 末尾追加 `[gold]消耗[/gold]。` 之类的文本。`消耗`/`保留` 作为**动词效果**（如"消耗1张手牌"）仍然正常写入。

---

## 附录：STS1 Mod 格式差异对照（冲突以 STS2 为准）

以下 STS1 写法**不应**在本 Mod 中使用：

| STS1 写法（不用） | STS2 写法（使用） |
|------------------|------------------|
| `NL` 换行 | `\n` |
| `!D!` / `!B!` / `!M!` 数值占位符 | `{Damage:diff()}` / `{Block:diff()}` / `{N:diff()}` |
| `[E]` 能量图标 | `{Energy:energyIcons()}` |
| 关键词不加标签：`消耗` / `保留` | 作为**动词**时用 `[gold]消耗[/gold]` / `[gold]保留[/gold]`；作为**卡牌关键词 badge** 则完全不写入描述（STS2 自动显示） |
| 数值前后加空格：`获得 !B! 点 格挡 。` | 无空格：`获得{Block:diff()}点[gold]格挡[/gold]。` |
| `回合结束时，` | `在你的回合结束时，` |
| `回复 !M! 生命`（无"点"） | `回复{Heal:diff()}点生命。` |
| 句末用 ` 。`（前置空格） | 句末用 `。`（无空格） |
