# Karen 中英日本地化

中文 `ShoujoKagekiAijoKaren/localization/zhs` 为语义来源；`eng`、`jpn` 分别直接对照中文维护。
当前三语各有 10 个 JSON、403 个键：原有 398 条，另将设置页导出弹窗的 5 条硬编码文本移入本地化。
空描述（困意、存在于舞台的理由、空壳等）按中文保留，不填入猜测的效果。

## 术语表

| 中文 | English | 日本語 |
| --- | --- | --- |
| 爱城华恋 | Karen Aijo | 愛城華恋 |
| 闪耀 | Shine | キラめき |
| 永恒闪耀 | Eternal Shine | 永遠のキラめき |
| 闪耀耗尽 | Shine runs out / depleted Shine | キラめきが尽きる／キラめき枯渇 |
| 约定牌堆 | Promise Pile | 約束の山札 |
| 闪耀牌奖励 | Shine Card Reward | キラめきカード報酬 |
| 临时力量 | Temporary Strength | 一時的な筋力 |
| 保留力量 | Retain Strength | 筋力保留 |
| 禁用遗物 | Disable Relic | レリック無効化 |
| 衍生牌 | Token Card | 生成カード |
| 力量 | Strength | 筋力 |
| 格挡 | Block | ブロック |
| 虚弱 | Weak | 脱力 |
| 易伤 | Vulnerable | 弱体 |
| 消耗 | Exhaust | 廃棄 |
| 保留 | Retain | 保留 |
| 斩杀 | Fatal | リーサル |
| 升级 | Upgrade | アップグレード |
| 抽牌堆 | draw pile | 山札 |
| 弃牌堆 | discard pile | 捨て札 |
| 手牌 | hand | 手札 |
| 牌组 | deck | デッキ |
| 约定之塔 | Tower of Promise | 約束の塔 |
| 续演 | Encore | 続演 |
| 光辉 | Radiance | 輝き |
| 红茶 | Black Tea | 紅茶 |

- **闪耀耗尽不是 Exhaust／廃棄**：前者通常永久移除牌组中的牌，后者只在本场战斗内移除。英文不再用 exhausted 指代闪耀耗尽。
- 英文数量使用游戏原生 `{Cards:plural:card|cards}`，不使用 `card(s)`。允许添加仅用于语法的 `plural`，但必须引用中文已有变量。
- 日语数值描述沿用本体风格，如 `{Damage:diff()}ダメージを与える。`、`{Block:diff()}[gold]ブロック[/gold]を得る。`。
- 日语跨牌堆移动使用「カード置き場の間を移動」，涵盖手札、山札、捨て札与约定牌堆。
- 卡名、台词保持少女歌剧的舞台意象；「ばなな」用于相关食物系列。没有完整的官方台词对照表，长卡名目前是中文语义基础上的日语译稿，不宣称逐字恢复原作。
- 同名卡牌、Power、生成牌引用必须同步修改。内部 ID、资源文件名不随译名改变。
- 设置弹窗的状态、结果、确认文字已本地化；错误详情保留底层工具或异常返回的原文，日志及开发导出的固定元数据标签不在本轮翻译范围。

## 已核对的本地依据

- `D:/Github/spire-codex/extraction/raw/src/Core/Localization/LocManager.cs`：`ja` 映射为 `jpn`，加载 `PluralLocalizationFormatter` 等格式化器。
- 同目录项目的 `src/Core/Modding/ModManager.cs`：按 `res://{mod.manifest.id}/localization/{language}/{file}` 加载表，不需要增加语言注册补丁。
- 本体 `localization/jpn/{cards,powers,potions,card_keywords,gameplay_ui}.json`：原版日语术语。
- 本项目 `KarenCarryingGuilt.cs`：耗尽牌按不同 ID 计数。
- 本项目 `KarenConsciousness.cs`：格挡计算使用 `TotalDamage + OverkillDamage`；英文删除旧译额外添加的 “unblocked” 限制。
- 本项目 `KarenStar.cs`、`KarenFightRelay.cs`：区分闪耀耗尽、自动重播与转移牌组。

## 校验

在仓库根目录使用 PowerShell 7：

```powershell
./tools/validate_localization.ps1
./tools/validate_localization.ps1 -SmartFormatPath 'D:/App/Stream/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/SmartFormat.dll'
```

另使用隔离副本验证校验器能够拒绝缺键、未知变量、括号损坏、条件分支缺失、BBCode 错配、中文源文变更、JSON 重复键共 7 类故障。

基础检查覆盖 UTF-8、JSON 重复键、三语文件/键集合、空值、变量及格式化器、分支数量、BBCode、英文中文残留与 `(s)`、中文源文本哈希。
可选检查使用游戏的 SmartFormat 库解析原始文本；以数值 0/1/2 × 升级前后 × 战斗内外执行 9,672 次英日文本格式化。
格式化检查仅在内存中的副本里把 `diff()` 和能量图标替换为普通数字，把 `show` 替换为等价布尔选择；不能代替游戏内动态变量和渲染验收。

本轮 C# 编译在隔离 worktree 验证；为处理原项目 DLL 相对路径，构建时临时将两处 HintPath 指向本机游戏，构建后逐字节恢复 csproj。未修改项目依赖配置。最终编译 0 错误，19 条警告与改动前基线相同。

## 后续中文变更

1. 修改中文后，同时核对同键英日文本；发现中文和代码不一致时先确认实际效果。
2. 核对术语、卡牌和 Power 之间的名称引用、升级分支、临时/永久及回合范围。
3. 仅在两种翻译均已复核后，更新 `source-hashes.json` 中该条中文值的 SHA-256（UTF-8，无 BOM，不包含 JSON 引号）。不要用批量刷新哈希掩盖待翻译条目。
4. 运行校验脚本，再按下面的清单检查游戏内显示。

单条哈希更新示例：

```powershell
$file = 'cards.json'
$key = 'KAREN_STRIKE.description'
$source = Get-Content -Raw -Encoding UTF8 "ShoujoKagekiAijoKaren/localization/zhs/$file" | ConvertFrom-Json -AsHashtable
$hashes = Get-Content -Raw -Encoding UTF8 'docs/localization/source-hashes.json' | ConvertFrom-Json -AsHashtable
$hashes["$file/$key"] = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($source[$key]))).ToLowerInvariant()
$hashes | ConvertTo-Json | Set-Content -Encoding utf8NoBOM 'docs/localization/source-hashes.json'
```

## 游戏内验收

2026-09-08 已打包安装并完成英文、日语各一场真实战斗，发现并修复切换语言后关键字提示沿用旧语言的问题。实测范围与结果见 [实战记录](playtest-2026-09-08.md)。以下项目尚需完整覆盖：

- 角色选择、设置页按钮及导出成功/失败弹窗；长日语标题是否溢出、缺字。
- 卡牌库的原版/升级版：迈向那个舞台、舞蹈练习、唤醒、下一个舞台。
- 战斗条件文本：背靠背、我们犯下的罪过、续写、Position 0；检查 0/1/多层状态。
- 闪耀及约定牌堆悬浮提示、临时力量、保留力量、禁用遗物的拼接文本。
- 药水选牌、生成牌名称、遗物描述、闪耀耗尽历史页。

验收重点是字体、换行、实际动态数值与图标；静态格式化检查无法验证这些显示结果。
