# STS2 JSON 文本格式化标签参考

> 来源：游戏反编译代码 `D:\claudeProj\sts2\`（v0.99.1）
> 实现位置：`src/Core/RichTextTags/`、`addons/mega_text/MegaRichTextLabel.cs`

---

## 一、颜色标签（自定义特效）

| 标签 | 颜色 | 十六进制 | 典型用途 |
|------|------|---------|---------|
| `[gold]...[/gold]` | 金色 | `#EFC851` | **关键词**、卡牌术语（最常用） |
| `[blue]...[/blue]` | 天蓝 | `#87CEEB` | 数值、升级前后变化量 |
| `[green]...[/green]` | 绿色 | `#7FFF00` | 回血/增益数值 |
| `[red]...[/red]` | 红色 | `#FF5555` | 受伤/减益/flavor 文本 |
| `[orange]...[/orange]` | 橙色 | `#FFA518` | NPC 名字、特殊强调 |
| `[purple]...[/purple]` | 紫色 | `#EE82EE` | 神秘效果、附魔名 |
| `[aqua]...[/aqua]` | 青绿 | `#2AEBBE` | 特殊名词（如 Flaw、Pollen） |
| `[pink]...[/pink]` | 粉色 | `#FF78A0` | 角色名、解锁条件 |

**注意**：`[gold]` 有特殊逻辑——若字符颜色已是 `green`，则跳过覆盖（允许绿色在金色标签内保留）。

---

## 二、动画/特效标签（自定义特效）

| 标签 | 效果 | 参数 |
|------|------|------|
| `[jitter]...[/jitter]` | 字符随机抖动（Perlin 噪声） | 无 |
| `[sine]...[/sine]` | 字符上下正弦波动 | 无 |
| `[thinky_dots]...[/thinky_dots]` | 字符依次跳动（思考气泡感） | 无 |
| `[fade_in]...[/fade_in]` | 字符逐渐淡入 | `speed`（默认4.0）、`tick`（默认0.01） |
| `[fly_in]...[/fly_in]` | 字符从偏移处飞入+旋转淡入 | `offset_x`、`offset_y` |

> `[fade_in]` 和 `[fly_in]` 仅通过 C# 代码写入对话框，不出现在 JSON 本地化文件中。
> 动效标签受玩家设置「文字特效」开关控制（`[jitter]` 和颜色标签不受此限制）。

---

## 三、Godot 原生 BBCode 标签

| 标签 | 效果 | 备注 |
|------|------|------|
| `[b]...[/b]` | **粗体** | 事件描述、菜单标题 |
| `[i]...[/i]` | *斜体* | 叙述旁白、角色动作描述 |
| `[center]...[/center]` | 居中对齐 | UI 布局 |
| `[font_size=N]...[/font_size]` | 指定字号 | 叙事大字如 `[i][font_size=22]...[/font_size][/i]` |
| `[rainbow freq=F sat=S val=V]...[/rainbow]` | 彩虹渐变动画 | `freq`频率、`sat`饱和度、`val`亮度 |
| `[shake]...[/shake]` | 字符随机震动 | 极少使用 |

---

## 四、嵌套组合示例

```
[jitter][red]YOU ARE WEAK!![/red][/jitter]
[sine][rainbow freq=0.3 sat=0.8 val=1]shifting chaos[/rainbow][/sine]
[b][jitter]CLANG! CLANG!!![/jitter][/b]
[purple][sine]"SHARE KNOWLEDGE???"[/sine][/purple]
[center][blue]{PlayerName}[/blue] [gold]{Character}[/gold][/center]
[green][sine]Green Byrd[/sine][/green]
```

---

## 五、各文件类型使用规律

| 文件 | 主要标签 |
|------|---------|
| `cards.json` | `[gold]`（关键词），极少其他颜色 |
| `relics.json` | `[gold]`、`[blue]`（数值）、`[green]`（回血）、`[red]`（flavor）、`[purple]`（附魔） |
| `powers.json` | `[gold]`、`[blue]` |
| `potions.json` | `[gold]`、`[blue]`、`[green]` |
| `events.json` | 全部标签，动效标签最多（`[jitter]`、`[sine]`、`[rainbow]`） |
| `ancients.json` | `[i][font_size=22]`（旁白）、`[thinky_dots]`（内心独白） |
| `characters.json` | `[pink]`（角色名）、`[red]`（Ascension 说明） |

---

## 六、Mod 开发规范

**卡牌描述**中的标准约定（来自 `CardKeywordExtensions.cs`）：
```
[gold]关键词[/gold]
```

**推荐用法**：
- 关键词/术语 → `[gold]`
- 数值（伤害/格挡量等） → `[blue]`（升级时用 `{Value:diff()}` 配合）
- 回复/增益数值 → `[green]`
- flavor 文本 / 负面说明 → `[red]`

**卡牌描述不建议使用动效标签**（`[jitter]`、`[sine]` 等），这些仅用于事件叙事文字。
