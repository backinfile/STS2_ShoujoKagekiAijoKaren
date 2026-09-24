# v0.107.1 / v0.111.0 移牌与核心接口核查

本次直接用 `ilspycmd -t <类型全名> <游戏 sts2.dll>` 反编译两套独立开发环境中的实际程序集，重点检查 Karen 使用的移牌、选牌、出牌结果、攻击和回合 Hook。反编译结果保留在本机 `artifacts/debug-tower/decompiled/{stable,beta}/`，不提交游戏源码。本记录不是游戏全部 API 的完整差异清单。

| 游戏 | `sts2.dll` SHA-256 |
| --- | --- |
| 正式版 v0.107.1 | `A1F9E653F1E28E4076558FEE1E60D218619CB7E057B887C6417F62C62C6D7A52` |
| 测试版 v0.111.0 | `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9` |

## 本次确认并修复：约定之塔抽回后缺少手牌节点

最小复现是《坠落》选中防御牌，随后打出《约定之塔》。旧代码中模型回到手牌，但手牌没有对应的可见节点。仅检查 MCP 的 `player.hand` 或 `DrawFromPromisePileAsync` 日志会误报通过。

添加只读命令 `karen_check_hand` 后，在修复玩法代码前运行了：

```powershell
pwsh -NoProfile -File artifacts/debug-karenfall/repro.ps1 -Environment beta -ClientId 66102 -SelectedId KAREN_DEFEND -Visual
```

得到错误：`modelCount=6, visualCount=5, missing=[KAREN_DEFEND]`，进程退出码为 1。截图也确认缺少防御牌。

根因有两部分：

1. 测试版 `CardPileCmd.Add` 先改变模型牌堆，再调用 `GetTweenForCardsChangingPiles`，并向 `NCard.FindOnTable` 传入旧牌堆。测试版本体使用 `overridePile ?? card.Pile?.Type` 来查找来源节点。Karen 的前缀必须同样按来源牌堆判断，不能只看已经改变的 `card.Pile`。
2. 旧兼容补丁把从约定牌堆移出的操作强制改成 `skipVisuals=true`，跳过了创建卡牌节点及 `NPlayerHand.Add`。这不仅关闭动画，也会导致模型与手牌 UI 不一致。

修复移除了强制跳过视觉的逻辑，并让测试版查找补丁只在有效来源牌堆为 PromisePile 时返回空节点。正常移牌流程由游戏创建新节点、播放动画并接入手牌。正式版保留原来的查找顺序。

## 核查结论

| 边界 | 测试版变化 | Karen 当前处理 / 结论 |
| --- | --- | --- |
| `CardPileCmd.Add(IEnumerable<CardModel>, CardPile, ...)` | 新增 `isChangingOwners`；模型与视觉移动拆分 | 两处 Harmony 补丁及闪耀 reverse patch 已区分签名并传递参数；本次修复约定牌堆的视觉路径 |
| `NCard.FindOnTable` | 显式旧牌堆优先于模型当前牌堆 | 本次同步前缀语义，同时覆盖移入和移出约定牌堆 |
| `GetTweenForCardsChangingPiles` | 测试版集中创建、移动卡牌节点；移入手牌调用 `NPlayerHand.Add` | 使用本体流程，不再通过 `skipVisuals` 绕开自定义牌堆 |
| 出牌结果位置 | tuple / `GetResultPileTypeForCardPlay` 改为 `CardLocation` / `GetResultLocationForCardPlay` | 闪耀、新的一天、接力和虚空已有条件编译分支；`CardLocation` 包含玩家信息，后续改动必须保留其归属 |
| 回合结束 | `AfterTurnEnd` 改为 `AfterSideTurnEnd`，提供参与者集合 | 现有补丁已切换入口；参与者语义仍值得专项检查，见下方 |
| 选牌上下文 | `SignalPlayerChoiceBegun` 新增 `Player` 参数；本体增加本地测试选择器支持 | `CardSelectCmdEx` 已传入玩家；复制本体选牌流程的代码应持续核对，不应仅凭编译认定回放/测试选择器行为一致 |
| 全局移牌 / 额外重播 | 本次核查的 `AfterCardChangedPiles`、`ModifyCardPlayCount` 入口签名可继续使用 | 继续通过实际战斗矩阵检查行为；本次没有改动它们 |
| `AttackCommand.FromCard` | 新增 `CardPlay?`，并传到伤害修正 Hook | 当前兼容扩展传 `null`，只完成了签名适配；有实际 `CardPlay` 的出牌攻击应改为传递真实上下文，需另行补充伤害修正回归 |

## 仍需专项验证的项目

- `AttackCommandCompat.FromCard(card)` 会丢失测试版新增的出牌上下文。`AttackCommand.Execute` 将此值传给 `CreatureCmd.Damage`，最终进入 `Hook.ModifyDamage`；依赖本次出牌的伤害修正可能与原版牌不同。应优先迁移 `OnPlay` 中的攻击调用；非出牌触发的攻击可继续传 `null`。本次未将此风险宣称为已修复，也没有把所有攻击卡一起改写。
- 约定牌堆回合结束补丁目前只取第一个 Karen 玩家。该逻辑并非此次测试版新增，但双 Karen 或测试版参与者集合不完整时值得专项验证；《约定之塔》取回回归不能证明所有回合末扳机都正确。

## 持续回归入口

```powershell
pwsh -NoProfile -File tools/test_promise_visuals.ps1 -Environment stable
pwsh -NoProfile -File tools/test_promise_visuals.ps1 -Environment beta
pwsh -NoProfile -File tools/test_dev_matrix.ps1 -Suite promise
```

单人脚本使用隔离用户目录，启动后通过 MCP 最小化，保存《坠落》前后、《约定之塔》后及打出取回卡牌后的截图和状态；每次移牌后执行 `karen_check_hand`，同时检查模型数量。多人约定牌堆用例覆盖两版各自的 Karen/Karen、Karen/原版角色、原版角色/Karen，检查双端可见手牌与模型一致。跨版本联机不纳入本次验证。

只读命令应在选牌及移牌动画结束后调用；它比较本地手牌模型和可见卡牌节点，不能替代其他 UI 动画或网络同步语义检查。

## 本次验证结果

- 双版本实现、入口及 PCK 构建成功，已安装到两套独立开发环境；安装 DLL 与构建包的 SHA-256 一致。
- 两版单人专项均通过，防御牌数量均为 `3 → 2 → 3`，抽回后可以打出，三个阶段的手牌节点检查均通过。已查看抽回和再次打出后的截图，并核对日志。
  - 正式版：`artifacts/test-promise-visuals/20260924-055556-stable-407aef/`
  - 测试版：`artifacts/test-promise-visuals/20260924-055612-beta-bfeb7a/`
- 完整矩阵 **30 通过、0 失败**：`artifacts/test-matrix/20260924-055659-1869d3/report.json`。包括双版本启动、单人闪耀、八组基础联机、十二组闪耀/全局移牌及六组约定牌堆专项。六组约定牌堆均通过模型数量恢复、双端手牌节点匹配和第 2 回合检查。
- 已查看正式版联机客户端，以及测试版双 Karen 主客机和两种混合角色组合的抽回截图。新截图在矩阵运行目录的 `logs/*-after-tower.png`。
- Steam 创意工坊发布包本次未更新；上述验证对应本项目独立开发环境中的修复包。攻击 `CardPlay` 上下文与多 Karen 回合末参与者逻辑仍保留为上文的专项核查项。

## v0.1.3 后续兼容修复

上文“仍需专项验证”的两项已在后续变更中处理：

- 所有 31 个从 `OnPlay` 发起攻击的卡牌文件改用 `FromPlayedCard(this, cardPlay)`。正式版兼容层调用旧版 `AttackCommand.FromCard(card)`；测试版调用 `AttackCommand.FromCard(card, cardPlay)`。对构建后的测试版 DLL 反编译核查，`KarenStrike.OnPlay` 传递了 `cardPlay`，兼容层也继续将其传入游戏本体的攻击命令。
- 约定牌堆回合末补丁按 Hook 的 `participants` 集合遍历本回合结束的每位 Karen，不再只处理第一位。新增只读命令 `karen_check_promise_pile`，联机测试让两位 Karen 各把《小零食》放入约定牌堆，下一回合分别确认牌已变成《香蕉》且《小零食》不再留在牌堆中。

双版本专项运行 `pwsh -NoProfile -File tools/test_dev_matrix.ps1 -Suite turn-end`，结果为 **2 通过、0 失败**，记录在 `artifacts/test-matrix/20260924-063326-56fd8b/report.json`。前一轮约定牌堆回归共 7 通过、1 失败；失败是测试脚本在测试版客户端《坠落》动作完成前检查牌堆，未到回合末断言。脚本现等待移牌实际完成后再检查，专项复测两版均通过。

最终 v0.1.3 包运行 `pwsh -NoProfile -File tools/test_dev_matrix.ps1 -Suite full -UpdateMcp`，结果 **32 通过、0 失败**，记录在 `artifacts/test-matrix/20260924-063609-a7d5d2/report.json`。正式版与测试版双 Karen 的《坠落》前后及《约定之塔》抽回截图已逐张查看：移入后手牌减少一张，抽回后可见手牌恢复；脚本同时调用 `karen_check_hand` 核对模型与节点。两版双 Karen 回合末的《小零食》→《香蕉》专项再次通过。正式版与测试版独立环境安装的 DLL、PCK 和最终包 SHA-256 一致。
