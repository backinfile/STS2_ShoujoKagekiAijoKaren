# OBS-032 极速下降选牌同步

## Original user input

“创建一个对话 用 skill worktree-task 进行修复”。

“请使用 $worktree-task 技能修复 OBS-032，《极速下降》选择放牌后发生的选牌同步索引越界。”先建立捕获具体失败的最小复现，再实施最小修复和相关验证。沿用问题清单中的 27 项忽略结论及 OBS-009、OBS-010、OBS-013 原组合上线核验状态。

原检出的问题清单与忽略目录证据作为只读输入，任务修改和验证产物放在工作树内。构建使用官方 Core 发行包，游戏验证使用 KarenSTS2MCP。移牌验证保存并查看前后截图，动画结束执行 karen_check_hand。提交前检查隐私，合并与清理分别等待确认。

## Brief task plan

- 固定起始 main / bfd5c997378dd2d021b68610b4a5632237664684，工作树为 ../worktrees/STS2_ShoujoKagekiAijoKaren/fix-obs-032-choice-context，任务分支为 codex/fix-obs-032-choice-context。
- 原检出仅有未跟踪 docs/待修复问题.md，SHA256 为 8ac027e43db5bb116b30a32dbd65a676a3f1cf13c4a073433db77a239b0b0c74。复制清单到本目录 issues.md 后维护，原件保持原状。
- 构建最小反馈回路，核对嵌套选项、选牌同步和取消时序，按证据定位根因。
- 按复现证据区分责任边界。若属于华恋则实施最小修复，若属于外部或原生通用问题则沿用用户归属规则标记忽略。记录相关构建、实际移牌及 Android 原组合状态。
- 更新任务记录与问题清单，完成隐私检查和单个任务提交，同步 main 后请求合并确认。

## Result and changes

### 结论与处理

OBS-032 按用户既有归属规则标记为“忽略，不处理”。失败条件已定位到原生 PlayerChoiceSynchronizer 接收到不属于其玩家集合的 Player 实例。RunState.GetPlayerSlotIndex(Player) 返回 -1，GetChoiceId 只检查索引上界，随后访问 choiceIds[-1] 并抛出报告中的 ArgumentOutOfRangeException。

CardSelectCmd.FromHand 在等待 UI 前使用全局 RunManager.PlayerChoiceSynchronizer 预留编号，等待完成后再次读取全局同步器。若等待期间换成基于新玩家实例创建的同步器，旧选择会把旧 Player 与旧 choiceId 提交给新同步器。原报告没有记录重载、退局或玩家集合变化的操作，具体触发来源仍待原 Android 组合核验。现有证据不能归因到 QuickSlAndRerollStart、RitsuLib 或 SpeedX 中的某一项。

《极速下降》的两层选择在当前实现中依次预留 0、1 两个编号，正常完成时所属玩家槽位均为 0。CardPileCmdEx.SelectOption 顺序等待原生选择和 DoOption，PromiseTransferResolutionPatch 仅在 finally 释放表现作用域。原版《生存者》在完全排除 Karen、Core、BaseLib 和 RitsuLib 的隔离进程中也重现相同失败，最小条件为等待中的原生手牌选择与不再包含旧玩家实例的新同步器。

本次交付为问题归属记录、完整清单副本和独立诊断入口。生产玩法代码保持当前版本。既有 27 项忽略结论继续保留，OBS-032 追加后共 28 项。OBS-009、OBS-010、OBS-013 的原组合上线核验状态保持原记录。

### 变更文件

- docs/worktree-tasks/fix-obs-032-choice-context/README.md 保存任务、根因和验证。
- docs/worktree-tasks/fix-obs-032-choice-context/issues.md 保存原清单副本及 OBS-032 处理结论。
- tools/rapid-choice-regression/RapidChoiceRegression.csproj 构建隔离诊断 DLL。
- tools/rapid-choice-regression/RapidChoiceFixture.cs 注入存档反序列化产生的同步器、记录玩家槽位，并提供原生退局及只读手牌对照。
- tools/rapid-choice-regression/run.py 使用 KarenSTS2MCP 驱动真实出牌、两层选择、截图与手牌校验。vanilla 模式只加载 MCP 和诊断夹具，以铁甲战士和原版《生存者》作对照。

夹具只安装到 artifacts 内的隔离测试游戏，诊断日志使用 [OBS032] 前缀。原始反编译输出、日志、存档和截图保存在忽略目录，Git 文档保留脱敏结论。

### 可捕获具体失败的复现

先通过 tools/prepare_relic_test_games.ps1 准备本工作树的隔离游戏，使用 SHOUJO_CORE_PACKAGE 指向官方 Core 发行目录并执行 tools/build_dual_branch.ps1，安装工作树产物。通过 tools/update_dev_mcp.ps1 -LoopbackOnly 构建对应游戏版本的当前 MCP。编译夹具示例如下。

```powershell
dotnet build tools/rapid-choice-regression/RapidChoiceRegression.csproj -p:Variant=Beta -o artifacts/rapid-choice-fixture/Beta
```

以下命令已经执行，捕获与报告一致的失败。

```powershell
python tools/rapid-choice-regression/run.py beta --label red-rapid-replaced --transition replace
```

```text
beforeSync currentSlot=0 choices=
pendingSync currentSlot=0 choices=2
replacement slot=-1 sameNetId=True
sync slot=0 id=0 count=1 runMatches=True
sync slot=-1 id=1 count=0 runMatches=False
OBS-032 ArgumentOutOfRangeException in PlayerChoiceSynchronizer.GetChoiceId
exit 1
```

关键堆栈为 GetChoiceId → SyncLocalChoice → CardSelectCmd.FromHand → PromisePileManager.AddFromPileAsync → PromisePileCmd.AddFromHand → KarenRapidPutOption.DoOption → CardPileCmdEx.SelectOption → KarenRapid.OnPlay。ValidateChoiceId 被运行时内联时不单独出现在栈上。

原版最小组合复现命令如下，同样以退出码 1 捕获具体异常。

```powershell
python tools/rapid-choice-regression/run.py beta --label vanilla-replaced --card native --transition replace --vanilla
```

该命令的选择只有一层，预留编号为 0，新同步器对旧 Player 返回槽位 -1，堆栈包含原版 Survivor.OnPlay。此复现为有意注入同步器替换的失败条件，未重放原 Android 玩家在报错前的实际操作。

### 验证版本与产物

Windows 正常流程基线为游戏正式版 v0.107.1／测试版 v0.111.0、Karen v0.2.0、Core 1.0.0 和 BaseLib v3.4.7。原版最小组合只加载 MCP 与诊断夹具。原 Android 报告为 Karen v0.1.8，保留版本和环境差异。

基线包由本工作树构建，SHA256 如下。

| 包内文件 | SHA256 |
| --- | --- |
| ShoujoKagekiAijoKaren.pck | 4d4db75668eb0240d1ad09bcb8e1fbfb7a7d9f6005ed439b728c1aca6c762c82 |
| lib/ShoujoKagekiAijoKaren.Stable.dll | 999f768e2e1e8b12eaaaa630c4ff7133c0edbe4d325226f4f6b338112c4a98a6 |
| lib/ShoujoKagekiAijoKaren.Beta.dll | 42e97dda638de100a546e57a0cb7f7a72f305ab4cd2435216484dd3816f88d90 |

### 实际验证

| 项目 | 结果 | 证据 |
| --- | --- | --- |
| 官方 Core 包校验、双版本 Karen 最终基线包构建 | 通过 | artifacts/baseline/build.log |
| 测试版《极速下降》正常选择放牌，两次独立运行 | 通过，编号 0→1，手牌模型及可见节点均为 6→3 | artifacts/rapid-choice-regression/beta/baseline-normal-4/ 与 baseline-normal-5/ |
| 正式版《极速下降》正常选择放牌 | 通过，编号 0→1，手牌模型及可见节点均为 6→3 | artifacts/rapid-choice-regression/stable/final-normal/ |
| 双版本移牌截图与动画后 karen_check_hand | 通过，已查看前后截图，missing／unexpected／unexpectedPileIcons 均为空，约定牌堆为 0→2 | 对应正常流程目录的 before.png、after.png、手牌 JSON |
| 测试版原版《生存者》正常选择，Karen 环境 | 通过，预留编号 0，手牌模型及可见节点均为 6→4 | artifacts/rapid-choice-regression/beta/native-normal/ |
| 测试版《极速下降》等待选牌期间替换同步器 | 捕获目标异常，退出码 1 | artifacts/rapid-choice-regression/beta/red-rapid-replaced/ |
| 测试版原版《生存者》等待选牌期间替换同步器，Karen 环境 | 捕获目标异常，退出码 1 | artifacts/rapid-choice-regression/beta/red-native-replaced/ |
| 测试版铁甲战士＋原版《生存者》，仅 MCP 与诊断夹具 | 捕获目标异常，退出码 1；已查看选牌截图与实际加载清单 | artifacts/rapid-choice-regression/beta/vanilla-replaced/ |
| 测试版最小原版组合正常选择 | 通过，编号 0，手牌模型及可见节点均为 6→4 | artifacts/rapid-choice-regression/beta/vanilla-normal/ |
| 测试版《极速下降》手牌选择期间正常退回主菜单 | 最终定向复核通过，原同步器槽位保持 0，目标异常未出现；通用引擎 Invalid Task ID 诊断单独记录，首次脚本曾因此判失败 | artifacts/rapid-choice-regression/beta/rapid-leave/ 与 final-leave/ |
| 原 Android v0.111.0／Karen v0.1.8 完整 Mod 组合 | 未测，原报告缺触发操作与可重放存档，本次验证运行于 Windows 隔离游戏 | 原检出 artifacts/error-watch/2026-11-07/details.json |

正常流程卡牌数量检查覆盖已打出的《极速下降》进入消耗堆和两张手牌进入约定牌堆。人工截图显示约定数为 2。诊断负例仅证明失败条件及责任边界，保留目标异常作为预期红色信号。

### 提交与集成状态

全工作树隐私检查扫描 5,499 个原已跟踪文件，零发现。完整暂存索引隐私检查扫描 5,504 个文件，零发现。570 份已跟踪文档完成敏感信息及本机绝对路径审阅，零发现。原清单逐行一致性检查确认 27 项忽略结论及三项已修复候选行原文保留。任务结果已达到可审阅状态，合并与清理分别等待确认。原目录仍为 main / bfd5c997378dd2d021b68610b4a5632237664684，未跟踪清单 SHA256 与开工记录一致。更新清单保存在本任务的 issues.md，合并时保留原检出原有文件状态。
