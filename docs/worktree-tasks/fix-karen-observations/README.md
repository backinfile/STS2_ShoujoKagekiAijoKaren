# 华恋报错候选定向修复

## Original user input

> 创建一个新对话，使用 $worktree-task 来处理需要处理的问题。

> 请使用 $worktree-task 技能处理华恋 Mod 仍需处理的三个候选问题。先完整阅读 $worktree-task 和仓库 AGENTS.md，再按技能建立独立工作树并开展排查、最小修复和相关验证。

交接范围为 OBS-009 约定牌堆定位兼容、OBS-010 花奖励联机索引越界、OBS-013 选角背景宿主节点查找失败。按调用栈核对责任边界，先建立捕获具体失败的最小复现，再实施必要修复。其他 Mod 自身问题和可忽略问题直接标为“忽略，不处理”。保留既有编号、人工忽略原因和修复记录。

原检出的 docs/待修复问题.md 和 artifacts/error-watch/2026-11-06/ 下的 details.json、items.json、review.json，以及 artifacts/error-watch/state.json、triage-policy.json 为只读交接输入。历史详情可参考 artifacts/telemetry-review-2026-11-05/error-details.json。原始日志和身份信息留在忽略目录，Git 文档使用脱敏摘要。

新增修改与验证产物放在工作树。运行行为使用 KarenSTS2MCP，移牌验证保存并查看前后截图，动画结束执行 karen_check_hand。遵循最小相关测试范围。完成任务文档、提交前隐私检查和单一任务提交，合并和清理分别按技能征求确认。授权范围为排查、修复、定向验证和任务提交。

## Brief task plan

1. 固定起点并保护原检出未提交清单，建立独立工作树。
2. 读取交接证据、原生方法及调用入口，逐项建立具体失败复现并核对责任边界。
3. 实施最小修复，验证双版本单人、花奖励同版本双端及真实约定移牌路径。
4. 维护完整问题清单副本，完成隐私检查和单一任务提交，同步 main 后征求合并确认。

起点分支为 main，提交为 e8730c8625ef3a13194b39ad3b83b0bf37994ade。任务分支为 codex/fix-karen-observations，工作树位于 ../worktrees/STS2_ShoujoKagekiAijoKaren/fix-karen-observations。

原检出唯一未提交状态为未跟踪的 docs/待修复问题.md，SHA-256 为 9c209695ad99fc79cf7cea1ee84ee60ee7f0edfe05828f31c13ce07feff56f20。更新后的完整副本保存在本目录的 issues.md，便于合并时保留原检出文件。

## Result and changes

### 责任边界和修复

- OBS-009 的首个业务帧来自华恋定位补丁。普通 Play 牌堆无需访问 Owner，旧补丁读取 canonical 模型的 Owner 后抛异常。现仅对 PromisePile／Draw 计算塔定位，并在读取 Owner 前检查 IsMutable。校验 525 的三条关联记录继续保留。最小复现使用原版 canonical StrikeIronclad，Valencina 0.10.3 原动作链保留上线核验。
- OBS-010 已复现华恋嵌套奖励的同步边界错误。原生 SelectLocalReward 对顶层列表做 IndexOf，子项结果为 -1，经 8 位编码变为 255。现先用原生 RewardsSetSynchronizer 选择父奖励，再用 PlayerChoiceSynchronizer 同步子选项。父奖励由后端标记完成，未选项由两端模型层记录跳过，UI 负责移除按钮。等待中的本地选择复用任务，完成失败后允许重试。
- OBS-013 已复现原生锁定角色选择清空背景后触发同一宿主缺失诊断。选角补丁检查 IsLocked，恢复信息面板背景并停止视频。解锁后的首次及重复选择继续在 Karen 背景 TextureRect 内挂载视频。原报告未记录锁定状态及完整节点树，其他 UI 改写仍待原玩家组合核验。

生产改动位于以下文件。

- src/Core/PromisePileSystem/Patches/NCardFindOnTablePatch.cs
- src/Core/Models/Relics/KarenFlowerRelic.cs
- src/Core/Patches/FlowerRewardSelectionPatch.cs 及 Godot UID
- src/Core/Patches/FlowerRewardUiPatch.cs
- src/Core/Patches/NCharacterSelectKarenVideoPatch.cs

回归夹具位于 tools/observation-regression/，独立测试 DLL 仅安装在忽略目录内的测试游戏。完整问题清单副本见 issues.md，原有 27 项忽略结论和 OBS-008 修复记录均保留。

### 最小复现

在 e8730c8 源码基线上构建测试版实现后，运行以下命令。

```powershell
python tools/observation-regression/run.py beta --label baseline-3
```

结果为三个具体失败和一个正常对照。

```text
fail:locked:Failed to find Karen character select background host node
pass:unlocked
fail:flower:invalid reward index -1, wire byte 255
fail:canonical:Canonical model of type MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad used in incorrect place.
```

基线构建及旧版结果保存在 artifacts/baseline/ 和 artifacts/observation-regression/beta/baseline-3/。基线阶段复用原检出已有资源包，最终回归改用从本工作树导出的完整任务包。

### 最终验证

| 验证 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 最终双版本包构建 | 通过 | 通过 |
| canonical 出牌区定位，锁定／解锁选角，父子奖励完成状态 | 4 项通过 | 4 项通过 |
| Karen＋Karen 同版本联机花奖励 | 通过 | 通过 |
| 主机选首项、客户端选次项，自然生成的花奖励 | 通过 | 通过 |
| 嵌套奖励跳过，返回外层奖励栈 | 通过 | 通过 |
| 离房跳过剩余奖励，未选项历史各记录一次 | 通过 | 通过 |
| 两端遗物、奖励完成状态、历史、奖励 ID 及实际报文索引 | 一致，索引有效 | 一致，索引有效 |
| 《坠落》→《约定之塔》截图与手牌一致性 | 通过，已查看截图 | 通过，已查看截图 |

《坠落》前、移入约定后、取回后，两版手牌模型／可见节点数均为 7／7、5／5、6／6。missing、unexpected、unexpectedPileIcons 均为空。正式版被选防御数量为 1→0→1，测试版为 2→1→2。

联机嵌套夹具显式构造包含大锅的花奖励以覆盖子奖励栈。花朵的自然生成规则仍排除商店遗物，普通花奖励另用两组自然生成掉落验证。

最终证据保存在以下忽略目录。

- artifacts/observation-regression/stable/final-1/
- artifacts/observation-regression/beta/final-2/
- artifacts/observation-multiplayer/stable/final-2/
- artifacts/observation-multiplayer/beta/final-5/
- artifacts/test-promise-visuals/20261106-014657-stable-365b1a/
- artifacts/test-promise-visuals/20261106-014857-beta-1a58e3/
- artifacts/build-package.log

单人和联机回归入口分别为 tools/observation-regression/run.py 与 run_multiplayer.py，均以 stable／beta 为参数。先用 tools/prepare_relic_test_games.ps1 准备隔离游戏，构建最终任务包和对应 Variant 的 ObservationRegression.csproj，将测试 DLL 与合法清单安装到隔离游戏，MCP 使用 tools/update_dev_mcp.ps1 的 LoopbackOnly 构建。脚本为每次运行创建独立用户数据并在私有桌面静音启动，结束后关闭所持有的测试进程。

移牌验证执行 tools/test_promise_visuals.ps1，Environment 分别为 stable、beta，通过忽略目录的私有桌面包装器运行。端口均为任务专用端口。

联机测试在花奖励准备前出现的引擎 Invalid Task ID 和原生 RunLocationTargetedMessageBuffer 启动阶段诊断单独存档，按外部／通用诊断忽略。花奖励准备至进入下一房间期间检查全部错误，最终定向运行阶段通过。测试部署中额外复制的 source-info.json 已移除，随后重新完成两版联机检查。

### 最终包哈希

完整任务包位于 artifacts/ShoujoKagekiAijoKaren/，版本为 v0.1.8。包仅包含加载器、清单、资源包和两版实现。

| 文件 | SHA-256 |
| --- | --- |
| ShoujoKagekiAijoKaren.dll | 3bec58bd940042366c994cc696ef803cce26412e42af2033fd24348a4b449ce6 |
| ShoujoKagekiAijoKaren.json | 5fdcbacc7fd0c38343b48e251e2631e7e3cf2181adea881b6e16a6b7766ef7e4 |
| ShoujoKagekiAijoKaren.pck | 0874ac8d639a76ed26bc3113ba2fd6f71d53a6a920cfccfdb70bb1b3a41f7c5a |
| lib/ShoujoKagekiAijoKaren.Stable.dll | 7ff7ddf700a1412193f423de646911abd1a83ca355dfe4f9ca368d8a80fa4353 |
| lib/ShoujoKagekiAijoKaren.Beta.dll | 77f85da2d768318f890ab70c907739a4e798101346424d3036ba4a919fafd2b9 |

### 审查及剩余事项

- 已审阅 2,382 份原已跟踪文档，未发现敏感信息或本机绝对路径。完整 Git 工作树隐私检查扫描 6,351 个原已跟踪文件，结果为零发现。完整暂存索引检查 tools/check_git_privacy.py --staged 扫描 6,359 个文件，结果为零发现。新增文档在内的 2,384 份已跟踪文档也完成本机绝对路径检查，结果为零发现。
- 原检出 main、HEAD 及未跟踪清单哈希已复核一致。任务提交及同步完成后按 $worktree-task 单独征求合并确认，工作树清理另行确认。
- 未测项目为原玩家完整混合 Mod 组合、Valencina 0.10.3 原动作和校验 525 完整链路、OBS-013 原 UI 改写组合、完整单人／多人矩阵、特效专项、Replay 及 Android。
