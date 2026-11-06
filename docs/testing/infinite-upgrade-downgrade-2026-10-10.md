# 无限升级牌降级修复（2026-10-10）

## 问题与原因

《永无结束的命运舞台》（`KarenContinue02`）升至 +3 后，降级一次直接变成 +0，伤害从 27 回到 12。正式版 v0.107.1、测试版 v0.111.0 均通过实际游戏内命令复现。

用户确认触发场景是镜子、旺戈等事件。这两个事件直接调用 `CardCmd.Downgrade()`；专项中的 +3 用例将牌放入永久牌组后走同一命令分支。

两版原生 `CardModel.DowngradeInternal()` 都将 `CurrentUpgradeLevel` 设为 0，并重建基础数值，然后调用 `AfterDowngraded()`。此前该牌的回调只将 `_upgradeCount` 清零，采用的是原生“全部降级”语义。2026-09-24 的审查修复记录及旧测试也采用了这个假设；本次明确该牌的游戏效果降级应当每次只减一级。

首次复现：

```powershell
pwsh -NoProfile -File tools/test_card_downgrade.ps1
```

两版均输出：`single downgrade: expected +2, actual +0, damage=12`。报告：`artifacts/test-matrix/downgrade-20261010-003802/report.json`。扩展为等级与伤害联合断言后再次复现：`expected +2/21 damage, actual +0/12 damage`，见 `artifacts/test-matrix/downgrade-20261010-004048/report.json`。

## 修改

- `KarenContinue02.AfterDowngraded()`：原生重置后重建剩余的 N−1 次升级，并逐次 `FinalizeUpgradeInternal()`；等级、伤害、成长计数及升级预览标记保持一致。不通过 `CardCmd.Upgrade()` 重建，避免降级顺带补满闪耀。
- `KarenContinueDampenPatch`：原生临时降级能力 `DampenPower` 对该牌只记录一级待恢复升级，避免移除效果后把原来的 N 级再次全加上。
- `KarenContinueBasePreviewPatch`：查看卡牌界面的基础版预览仍完整清除副本的升级，实际卡牌不受影响。
- 上述两个调用点补丁仅对 `KarenContinue02` 改变行为，普通卡牌保持原生语义。补丁验证预期调用点数量，避免游戏 API 变化后静默失效。

没有添加新的存档字段或网络状态；剩余等级继续使用原生序列化。

## 自动化覆盖

`tools/review-regression/ReviewFixtureCmd.cs` 的 `downgrade` 用例包含：

- 初始等级 0、1、2、3、10、100，逐级降至 0，同时检查伤害公式。
- 每次降级后再次升级、调用原生内部降级、序列化恢复及永久副本。
- 降级不补满闪耀，伤害变量无残留升级预览标记。
- 普通铁甲打击的降级对照。
- 真实 `PowerCmd.Apply<DampenPower>()` / `PowerCmd.Remove()` 生命周期；临时降级期间额外升级后，效果结束仍只补回一级。
- 保留附魔、永久零费附魔在降级与序列化恢复后仍生效。
- 实际 `NInspectCardScreen` 取消升级勾选后，展示 +0 基础版，原牌保持 +3。

`tools/test_card_downgrade.ps1` 在独立用户目录启动两版 Karen 单人战斗，临时安装测试插件，验证 MCP 加载记录后执行用例，结束时清理插件与测试进程。`tools/test_review_regressions.ps1` 覆盖两版各四种主客角色组合；主机、客户端分别发起降级专项，并在两个进程中断言结果。新增的 `-Headless` 仅适用于资源受限时的模型与同步验证；报告会明确标记画面未测，不替代移牌截图验收。

## 验证结果

| 检查 | 结果与证据 |
| --- | --- |
| 双版本构建及共享 PCK 导出 | 通过；每版实现 23 条编译警告、0 错误，日志 `artifacts/downgrade-build.log` |
| 双版本单人降级专项 | 通过；最终用例包含永久牌组中的事件命令分支，两版均 `pass:downgrade`，原生与托管日志均未发现错误。`artifacts/test-matrix/downgrade-20261010-011856/report.json` |
| 双版本完整矩阵，首次运行 | 启动与单人战斗 4 项通过；正式版联机资源预加载发生原生内存分配失败，停止后续重复崩溃。`artifacts/test-matrix/20261010-004341-7be334/report-interrupted.json` |
| 双版本同版本联机专项，无渲染模式 | 八种组合、16 端的 `models` 与 `downgrade` 断言均通过，无托管错误；多份日志有原生资源初始化错误，不能记作八组完整通过。详见下文。 |
| 正式版单人约定牌堆与画面 | 通过；防御数量 2 → 1 → 2，手牌节点检查通过，已查看 MCP 前后截图。`artifacts/test-promise-visuals/20261010-005205-stable-bd5878/report.json` |
| 测试版单人约定牌堆与画面 | 通过；防御数量 3 → 2 → 3，手牌节点检查通过，已查看 MCP 前后截图，日志未发现错误。`artifacts/test-promise-visuals/20261010-012048-beta-769d20/report.json` |
| 双版本完整矩阵，正常渲染重试 | 34 项通过、2 项失败；失败为测试版双 Karen 的地图等待超时，以及测试版铁甲主机＋Karen 客户端的闪耀用例结果解析失败。`artifacts/test-matrix/20261010-005341-496299/report.json` |
| 上述两项单独复跑 | 两项功能断言均通过；资源读取错误未重现，两个主机日志仍各有一次预加载 `Invalid Task ID`。`artifacts/test-matrix/downgrade-reruns-20261010-011645/report.json`、同目录 `log-audit.json` |

正常渲染矩阵覆盖两版各四种主客角色组合的出牌与结束回合，并覆盖闪耀耗尽、全局移牌、约定牌堆、回合结束转换。已查看两版各含 Karen 组合的 MCP 移牌与回合前后截图；动画结束后的 `karen_check_hand` 检查通过。合并复跑后，36 个矩阵项目的功能断言均得到通过结果，但不能因此宣称全部日志无错误。

补充日志审计见 `artifacts/test-matrix/20261010-005341-496299/native-log-audit.json`：64 份联机日志中，9 份包含共 44 条原生错误。结果解析失败的那组主机还包含 35 条资源读取/解析错误、4 条托管 VFX 加载错误，不能只归因于测试脚本；该组复跑时这些错误未重现。其余 9 条原生错误为预加载阶段的 `worker_thread_pool.cpp: Invalid Task ID`，包括原版双铁甲对照组。此问题未在本次降级修复中处理。

无渲染专项记录位于 `artifacts/test-matrix/review-20261010-004721/`。`report.json` 的正式版四组返回通过，测试版四组因原生日志检查失败；补充审计 `assertions-and-log-audit.json` 发现正式版两组也有原生错误，因此只认可全部降级断言通过。原生错误出现在预加载阶段，包括 dummy renderer 的纹理/RID 初始化及 `worker_thread_pool.cpp` 的 `Invalid Task ID`；均在降级用例执行前发生。无渲染运行没有取得移牌画面证据，不计作视觉验收。

镜子/旺戈的验证使用永久牌组卡牌及事件调用的同一个 `CardCmd.Downgrade()` 分支，没有逐页操作两个事件界面。序列化恢复使用真实 `ToSerializable()` / `FromSerializable()`，不等同于全局存档恢复或断线重连测试。本次不更改存档格式及断线处理；未另测断线恢复或跨版本联机。

## 部署与收尾

修复后的双版本 DLL 与共享 PCK 已安装到本项目 `artifacts/dev-game/stable/game/mods` 和 `artifacts/dev-game/beta/game/mods`。联机测试使用 `-LoopbackOnly`；测试结束后通过 `tools/update_dev_mcp.ps1 -OutputRoot artifacts/mcp-default-after-downgrade` 恢复常规 MCP 构建，测试用的本机连接限制不保留在开发环境中。临时降级测试插件已卸载，测试游戏进程已结束。

首次中断测试生成的两份客户端游戏副本共约 5.8 GiB，仍保留在 `artifacts/test-matrix/20261010-004341-7be334/`。清理操作在获得用户明确授权后仍被自动审批拒绝，返回 `blocked by policy`，未提供更具体原因。后续复跑复用了这些副本；正常渲染完整矩阵创建的另一组副本已由测试脚本清理，测试日志和报告均保留。
