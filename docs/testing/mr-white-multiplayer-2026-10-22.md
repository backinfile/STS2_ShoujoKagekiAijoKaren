# Mr. White 双版本联机专项

日期：2026-10-22。

> 历史记录：本页验证的是改动前的首回合效果。用户随后要求改为前两回合，现行实现与复测见 [两回合效果记录](mr-white-two-turn-2026-10-22.md)。

## 范围与结果

结论：36/36 项真实联机专项通过，未复现 Mr. White 本体故障，未修改其正式实现。

| 游戏版本 | Karen 主机＋原版客户端 | 原版主机＋Karen 客户端 | Karen＋Karen |
| --- | --- | --- | --- |
| 正式版 v0.107.1 | 6/6 通过 | 6/6 通过 | 6/6 通过 |
| 测试版 v0.111.0 | 6/6 通过 | 6/6 通过 | 6/6 通过 |

正式版 v0.107.1 与测试版 v0.111.0，各覆盖 Karen 主机＋原版客户端、原版主机＋Karen 客户端、Karen＋Karen 三组；原版角色使用 Ironclad。每组运行以下六种场景，不等同于全 Mod 的全面测试矩阵。

| 场景 | 核对内容 |
| --- | --- |
| 部分选择 | 持有者先就绪，取消后重新就绪；队友仍可出牌；双方结束后，各持有者选两张 |
| 零张确认 | 反向提交结束回合；不选牌直接确认，原手牌按正常规则处理 |
| 全选 | 每个持有者将全部可选手牌放入自己的约定牌堆 |
| 空手牌 | 持有者为空手牌时不弹无效选择，也不阻塞回合 |
| 保留／虚无全选 | 生成普通、保留、虚无各一张，确认均可先存入约定牌堆 |
| 保留／虚无零选 | 不存入；保留牌继续保留，虚无牌按原生规则消耗 |

每个场景均等待动画结束后保存双端游戏状态和截图，执行只读 `karen_check_hand`，并比较两端所有玩家的手牌、约定牌堆、遗物状态；继续到第三回合检查不重复触发。六个连续战斗同时覆盖下一场重置。

## 时序解释

原版 `EndPlayerTurnAction` 只标记准备结束回合，允许 `UndoEndPlayerTurnAction` 撤销；全员准备好后才进入 `CombatManager.EndPlayerTurnPhaseOneInternal`，执行 `Hook.BeforeTurnEnd` 与 `BeforeSideTurnEnd`。因此，单个玩家先点“结束回合”时不立即触发 Mr. White，与原生回合结束规则一致。

首次探针错误地要求“持有者先就绪即弹选牌”，在正式版主机、客户端都返回 `state=monster, round=1, hand=5`。核对原生流程后撤销此错误断言；这两次不计作遗物缺陷或最终有效测试失败。

## 日志与画面验收

- 共 216 份手牌节点诊断，模型数与可见数相等，`missing`、`unexpected`、`unexpectedPileIcons` 全为空。
- 保存 251 张截图；六组双端移牌前后联系表已逐组目视查看，没有卡牌节点残留或错位。
- 12 份日志中，正式版无错误；测试版在 5 份启动日志中合计出现 7 条 `ERROR: Invalid Task ID`：主机持有组 3 条、客户端持有组 1 条、双方持有组 3 条。全部早于 Common 预加载完成和首条开发命令，紧随来源均为 `wait_for_task_completion (core/object/worker_thread_pool.cpp:418)`。
- 该形态在[既有预加载记录](bridge-v11-reference-angle-2026-10-10.md)中也有记录，根因仍未确认。本次不把启动日志称为零错误，也没有宣称修复引擎问题。
- 战斗操作阶段未发现错误、状态校验不一致或手牌诊断失败。`Test-MrWhiteLogs` 已在上述完整日志上执行，只单列版本、阶段、文本和来源均完全匹配的启动诊断，其他错误仍判失败。
- 当前任务源码双版本编译均 0 错误、28 条既有警告；测试夹具双版本均 0 警告、0 错误。测试进程全部关闭，独立数据和原始证据保留。

## 测试入口与隔离

- `tools/dev_matrix_case.ps1 -MrWhiteRegression`：使用原有联机开局与安全停止流程。
- `tools/relic-regression/mr-white-actions.ps1`：六种场景及双端断言。
- `tools/relic-regression/MrWhiteProbeCmd.cs`：仅测试 DLL 中的手牌准备、只读全玩家状态、原生撤销动作入口；不进入正式 Mod。
- `tools/relic-regression/run-white.ps1`：顺序运行两版三种角色组合；默认全部重跑，`-ReuseStableHost` 仅显式复用本次已有正式版主机证据。
- 双端使用独立游戏路径、Mod 配置、用户目录、日志和 MCP 端口；引擎只读文件通过硬链接复用，Mod 和存档均为独立副本。主机传输限制在回环地址。
- 专用 MCP 端口：正式版主机／客户端 15767／15769，测试版 15768／15770；联机 UDP 33771。先在各自日志确认 `[Karen STS2 MCP]` 已加载再访问 HTTP。
- 各游戏加载当前任务源码构建的对应 Stable／Beta 程序集；共享资源 PCK 沿用开发包。皇冠能量标记本轮仅静态校验，没有借此声称完成其游戏内渲染验收。

MCP 的 `undo_end_turn` 适配器会因已就绪后的 `PlayerActionsDisabled` 拒绝撤销。夹具 `white_undo` 改为请求与原生按钮相同的 `UndoEndPlayerTurnAction`，仍经原生动作同步队列执行；未修改 MCP 源码或遗物行为。该接口限制与 Mr. White 本体分开记录。

## 证据

- 正式版主机：`artifacts/test-matrix/mr-white-boundaries-v2-stable-host/`。
- 其他五组及汇总：`artifacts/test-matrix/mr-white-mp-20261022/`。
- 各组 `mr-white/<case>/results.json`、逐端 `*-hand.json`、移牌前后 PNG、选牌 JSON 与游戏日志。
- 逐组双端画面联系表：汇总目录下 `review/`。

## 限制

本专项针对正常同版本联机和本遗物的选择边界；未覆盖断线重连、存档恢复、额外回合、Void 模式、第三方 Mod 干预或四人局。没有把这些未测范围表述为已通过。
