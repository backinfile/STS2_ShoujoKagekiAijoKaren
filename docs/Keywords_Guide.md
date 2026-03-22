# STS2 关键词完整参考文档

> 数据来源：游戏反编译代码 `D:\claudeProj\sts2\`（v0.99.1）
> 本文档涵盖卡牌关键词、战斗状态、充能球、牌组术语及游戏机制术语。

---

## 一、卡牌关键词（CardKeyword 枚举）

定义于 `src/Core/Entities/Cards/CardKeyword.cs`，显示顺序由 `CardKeywordOrder.cs` 控制。

### 显示在描述文字之前（beforeDescription）

| 中文 | 英文 | 本地化键 | 效果描述 |
|------|------|---------|---------|
| 虚无 | Ethereal | `ETHEREAL` | 若这张牌在回合结束时留在手牌，则将其**消耗**。（与消耗关键词联动：自动附加消耗提示） |
| 奇巧 | Sly | `SLY` | 若这张牌在回合结束前从手牌被弃掉，则免费将其打出。 |
| 保留 | Retain | `RETAIN` | 回合结束时，保留的牌不会被弃掉。 |
| 固有 | Innate | `INNATE` | 每场战斗开始时这张牌会出现在手牌（强制起手）。 |
| 不能被打出 | Unplayable | `UNPLAYABLE` | 无法被打出。打出时跳过执行，直接移入结果牌堆。 |

### 显示在描述文字之后（afterDescription）

| 中文 | 英文 | 本地化键 | 效果描述 |
|------|------|---------|---------|
| 消耗 | Exhaust | `EXHAUST` | 战斗结束前移除（进入消耗堆）。打出后直接消耗。 |
| 永恒 | Eternal | `ETERNAL` | 无法从牌组中移除或变化（`IsRemovable = false`）。诅咒牌常用。 |

### CardKeyword API（Mod 开发使用）

```csharp
// 静态声明（覆写 CanonicalKeywords）
public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust, CardKeyword.Innate };

// 升级时动态添加/移除
protected override void OnUpgrade() { AddKeyword(CardKeyword.Innate); }
protected override void OnUpgrade() { RemoveKeyword(CardKeyword.Exhaust); }

// 运行时命令
CardCmd.ApplyKeyword(card, CardKeyword.Sly);
CardCmd.RemoveKeyword(card, CardKeyword.Exhaust);
CardCmd.ApplySingleTurnSly(card);   // 临时本回合 Sly
CardCmd.Exhaust(ctx, card);          // 手动触发消耗

// 悬浮提示（不真正拥有关键词，只展示说明）
protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    new[] { HoverTipFactory.FromKeyword(CardKeyword.Sly) };
```

---

## 二、战斗状态（Power/Buff/Debuff）

### 通用增益（玩家/敌人均可拥有）

| 中文 | Power 类名 | 描述 |
|------|-----------|------|
| **力量** | `StrengthPower` | 增加/减少攻击造成的伤害值。可为负数（削弱力量）。 |
| **敏捷** | `DexterityPower` | 增加/减少从卡牌获得的格挡值。可为负数。 |
| **集中** | `FocusPower` | 增加/减少充能球的被动和激发效果数值。 |
| **格挡** | （不是 Power，是实体属性）| 在下个回合前阻挡伤害。回合开始时归零（除非有壁垒/残影等效果）。 |
| **人工制品** | `ArtifactPower` | 免疫下 N 次负面效果（层数=免疫次数）。 |
| **活力** | `VigorPower` | 下一张攻击牌造成额外伤害，之后消耗。 |
| **荆棘** | `ThornsPower` | 受到攻击时对攻击者造成反击伤害。 |

### 负面状态（Debuff）

| 中文 | Power 类名 | 描述 | 触发时机 |
|------|-----------|------|---------|
| **虚弱** | `WeakPower` | 造成的攻击伤害减少 25%。 | 持续 N 回合 |
| **易伤** | `VulnerablePower` | 受到的攻击伤害增加 50%。 | 持续 N 回合 |
| **脆弱** | `FrailPower` | 从卡牌获得的格挡值减少 25%。 | 持续 N 回合 |
| **中毒** | `PoisonPower` | 回合开始时失去等量生命，然后中毒层数 -1。 | 每回合衰减 |
| **灾厄** | `DoomPower` | 回合结束时若生命值 ≤ 灾厄层数则立即死亡。 | 回合结束判定 |
| **击晕** | `StunPower` | 敌人下一回合无法行动。 | 一次性 |
| **混乱** | `ConfusedPower` | 卡牌抽取时耗能随机变为 0~3。 | 持续 |
| **无实体** | `IntangiblePower` | 受到的所有伤害和效果最多为 1（持续 N 回合）。 |  持续 N 回合 |

### 特殊增益（玩家专属）

| 中文 | Power 类名 | 描述 |
|------|-----------|------|
| **壁垒** | `BarricadePower` | 格挡不在回合开始时消失。 |
| **残影** | `BlurPower` | 下 N 回合开始时格挡不消失。 |
| **覆甲** | `MetallicizePower`（推测）| 每回合结束时获得固定格挡。 |
| **重放** | `ReplayPower`（动态/静态）| 这张牌额外打出 N 次。（显示在卡牌描述末尾） |
| **爆发** | `BurstPower` | 本回合下 N 张技能牌多打出一次。 |
| **斩杀** | — | 特殊触发词：当这张牌杀死一名非爪牙敌人时触发效果。（不是 Power，是卡牌效果修饰词） |

---

## 三、充能球（Orbs）

充能球为缺陷角色的核心机制。每个充能球具有**被动效果**（每回合结束触发）和**激发效果**（手动激发或栏位满时自动触发）。

| 中文 | 英文 | 类名 | 被动效果 | 激发效果 |
|------|------|------|---------|---------|
| **冰霜** | Frost | `FrostOrb` | 回合结束时获得 N 点格挡 | 获得 N 点格挡 |
| **闪电** | Lightning | `LightningOrb` | 回合结束时对随机敌人造成 N 点伤害 | 对随机敌人造成 N 点伤害 |
| **黑暗** | Dark | `DarkOrb` | 回合结束时伤害值 +N（累积增加） | 对生命值最少的敌人造成累积伤害 |
| **等离子** | Plasma | `PlasmaOrb` | 回合开始时获得 1 点能量（不受集中影响） | 获得 2 点能量 |
| **玻璃** | Glass | `GlassOrb` | 回合结束时对所有敌人造成 N 点伤害，然后数值 -1 | 对所有敌人造成 N 点伤害 |

### 充能球相关机制词

| 中文 | 说明 |
|------|------|
| **生成**（Channel） | 将充能球放入第一个空栏位；栏位满时自动激发最左侧球。 |
| **激发**（Evoke） | 消耗最右侧充能球并触发其激发效果。 |
| **充能球** | 充能球的通称/空栏位。 |

---

## 四、牌组系统术语

| 中文 | 英文 | 说明 |
|------|------|------|
| **牌组** | Deck | 你在战斗中使用的所有卡牌。 |
| **手牌** | Hand | 当前在手中的卡牌。 |
| **抽牌堆** | Draw Pile | 每回合从此处抽牌；抽完后将弃牌堆洗入。 |
| **弃牌堆** | Discard Pile | 打出或弃掉的卡牌；抽牌堆空时洗回抽牌堆。 |
| **消耗牌堆** | Exhaust Pile | 被消耗的卡牌；战斗结束前不回到循环。 |

---

## 五、特定卡牌/召唤物

这些词在卡牌描述中以 `[gold]` 标注，指代具体的卡牌或单位：

| 中文 | 类型 | 说明 |
|------|------|------|
| **小刀** | 卡牌 | 由猎奇/精准等效果生成的 0 费攻击牌。 |
| **灵魂** | 卡牌类型 | 特定来源生成的卡牌，与吞噬生命等 Power 联动。 |
| **燃料** | 卡牌 | 特定效果生成的卡牌。 |
| **巨石** | 卡牌 | 特定效果生成的卡牌。 |
| **伤口** | 状态牌 | 诅咒类状态牌（Unplayable，耗尽时无效果）。 |
| **灼伤** | 状态牌 | 回合结束时对自身造成伤害，然后消耗。 |
| **黏液** | 状态牌/卡牌 | 特定敌人掉落或生成的状态牌。 |
| **碎屑** | 状态牌 | 无效果的消耗牌。 |
| **君王之剑** | 卡牌 | 铸造（Forge）机制创建的专属攻击牌，每次铸造增加额外伤害。 |
| **奥斯提** | 召唤物 | 储君角色的专属随从；`召唤` 命令以指定生命值创建或增强它。 |

---

## 六、游戏机制词

| 中文 | 英文 | 说明 |
|------|------|------|
| **升级** | Upgrade | 将一张卡牌升级（强化效果）。 |
| **变化** | Transform | 将一张卡牌变为随机稀有度的其他卡牌。 |
| **铸造** | Forge | 每场战斗第一次铸造时将君王之剑加入手牌，并为其增加伤害。 |
| **召唤** | Summon | 以指定生命值创建/强化奥斯提随从。 |
| **斩杀** | Fatal | 触发词：这张牌杀死一名非爪牙敌人时触发额外效果。 |
| **重放** | Replay | 将这张牌额外打出 N 次（显示在卡牌描述末尾）。 |
| **稀有** | Rare | 卡牌稀有度：稀有（金色边框）。 |
| **攻击牌** | Attack | 卡牌类型：攻击牌（红色）。 |
| **状态** | Status | 卡牌类型：通常不能被打出或效果单一的状态牌。 |
| **金币** | Gold | 游戏货币，用于商店购买。 |
| **最大生命值** | Max HP | 生命上限。 |
| **休息处** | Rest Site | 地图节点：可回复生命或升级卡牌。 |

---

## 七、卡牌描述中出现的 IfUpgraded 专属词

以下词语以 `[gold]{IfUpgraded:show:X+|X}[/gold]` 格式出现，代表特定角色/效果生成的卡牌，升级版在名称后加 `+`：

- **仆从俯冲**、**仆从打击**、**仆从捐躯**（召唤物关联卡牌）
- **小刀+**、**巨石+**、**灵魂+**、**燃料+**

---

## 八、在 Mod 卡牌描述中引用关键词

参考 `docs/ZhsCardLocalization_Guide.md` 和 `docs/EngCardLocalization_Guide.md`。

```json
// 常见格式示例
"消耗。" → "[gold]消耗[/gold]。"
"虚无。" → "[gold]虚无[/gold]。"
"保留。" → "[gold]保留[/gold]。"
"固有。" → "[gold]固有[/gold]。"
"奇巧。" → "[gold]奇巧[/gold]。"

// 卡牌描述中的 Power 词
"获得{Block:diff()}点[gold]格挡[/gold]。"
"造成{Damage:diff()}点伤害。给予敌人[blue]{WeakPower}[/blue]层[gold]虚弱[/gold]。"
"给予敌人[blue]{VulnerablePower}[/blue]层[gold]易伤[/gold]。"
"获得[blue]{StrengthPower}[/blue]点[gold]力量[/gold]。"
```

> **注意**：`DynamicVar` 中 Power 词用完整类名（如 `{StrengthPower:diff()}`），但在描述文字中显示的人类可读名称用 `[gold]力量[/gold]`。两者相互独立。

---

## 附：关键文件路径

| 文件 | 用途 |
|------|------|
| `D:\claudeProj\sts2\src\Core\Entities\Cards\CardKeyword.cs` | CardKeyword 枚举定义 |
| `D:\claudeProj\sts2\src\Core\Entities\Cards\CardKeywordExtensions.cs` | 本地化/文本扩展方法 |
| `D:\claudeProj\sts2\src\Core\Entities\Cards\CardKeywordOrder.cs` | 关键词显示顺序 |
| `D:\claudeProj\sts2\localization\zhs\card_keywords.json` | 中文关键词本地化 |
| `D:\claudeProj\sts2\localization\eng\card_keywords.json` | 英文关键词本地化 |
| `D:\claudeProj\sts2\localization\zhs\powers.json` | 中文 Power 本地化 |
| `D:\claudeProj\sts2\localization\zhs\static_hover_tips.json` | 中文机制词悬浮提示 |
| `D:\claudeProj\sts2\localization\zhs\orbs.json` | 中文充能球本地化 |
| `D:\claudeProj\sts2\localization\zhs\gameplay_ui.json` | 中文 UI/格挡/能量等术语 |
