# 全量审查问题修复与验证（2026-09-24）

本次修复对应全量审查中的 7 项问题。以正式版 v0.107.1、测试版 v0.111.0 的本地程序集反编译结果及实际安装的 BaseLib 为依据。

| 项目 | 修复与边界 |
| --- | --- |
| 禁用遗物的战后清理 | 在 `Hook.AfterCombatEnd` 创建监听器快照前恢复遗物，使原生流程发送一次战后回调；不手工重放回调。 |
| 保留能量的多人计数 | 仅拥有者的 `AfterEnergyReset` 消耗层数，其他玩家重置能量不影响它。 |
| 接力牌重播导致重复永久转移 | 只在 `PlayIndex == PlayCount - 1` 时转移；接收失败时将战斗原牌保留在弃牌堆，避开预先计算的 `None` 结果。 |
| 永久副本遗漏附魔效果 | 两种永久复制共用重建逻辑，按原生反序列化顺序应用附魔、执行 `ModifyCard`、逐级升级并 `FinalizeUpgradeInternal`；不复制战斗临时附魔禁用状态。两种复制原有闪耀保留策略不变。 |
| 《永无结束的命运舞台》降级错误 | 遵循原生“全部降级”语义：原生重置数值后，将自定义升级计数归零。 |
| 非 Karen 约定牌堆不触发回合末 | 按实际回合结束参与者分发，不再按角色 ID 排除原版角色。 |
| 远端 Void 污染本机牌堆 UI | 图标与数量命令必须传所属玩家，由命令入口统一检查 `LocalContext.IsMe`；所有调用点带入正确玩家。 |

## 可重复执行的验证

在已准备好隔离双版本游戏的工作树内执行：

```powershell
pwsh -NoProfile -File tools/build_dual_branch.ps1
pwsh -NoProfile -File tools/test_dev_matrix.ps1 -Suite full -UpdateMcp
pwsh -NoProfile -File tools/test_review_regressions.ps1
pwsh -NoProfile -File tools/test_promise_visuals.ps1 -Environment stable
pwsh -NoProfile -File tools/test_promise_visuals.ps1 -Environment beta
```

`full` 增加了 Karen／原版角色互换主客机的约定牌堆回合末回归。专项测试插件位于 `tools/review-regression`，只临时安装到测试游戏；正式 Mod 工程不编译这些文件。

专项用例包含 0–3 次升级、全部降级和再次升级、原生序列化恢复、Steady 附魔永久副本、三次播放只转移一份、两位玩家保留能量层数、强制拒绝接收后原牌保留、远端 UI 命令及模式切换、禁用 CentennialPuzzle 后的真实战斗结束清理。转移拒绝使用测试插件的确定性故障注入；遗物使用已消耗标志模拟本场已触发状态。

## 仅本机联机测试

`tools/update_dev_mcp.ps1 -LoopbackOnly` 在 MCP **构建副本**中添加测试用传输补丁，使 ENet 主机和客户端绑定 `127.0.0.1`。不改 MCP 源仓库、正式 Mod、系统防火墙或管理员权限。专项运行器默认启用它，并验证主机 UDP 33771 实际绑定地址。需要局域网联机时，重新运行不带该选项的 MCP 构建安装。

## 本次结果

| 验证 | 结果 | 记录 |
| --- | --- | --- |
| 稳定版／测试版构建及共用 PCK 导出 | 通过；两版实现各 20 条既有警告、0 错误 | `artifacts/ShoujoKagekiAijoKaren/` |
| 双版本完整矩阵 | 36 通过，0 用例失败 | `artifacts/test-matrix/20260924-072335-42943f/report.json` |
| 修复专项，两版 × Karen/铁甲与铁甲/Karen | 4 通过，0 用例失败；每组 models、relay、energy、rejection、ui、relic 均在双端断言 | `artifacts/test-matrix/review-20260924-075102/report.json` |
| 正式版单人移牌视觉 | 通过，防御数量 2 → 1 → 2；取回后可继续打出，手牌模型／节点一致 | `artifacts/test-promise-visuals/20260924-075439-stable-5eb58c/report.json` |
| 测试版单人移牌视觉 | 通过，防御数量 3 → 2 → 3；取回后可继续打出，手牌模型／节点一致 | `artifacts/test-promise-visuals/20260924-075439-beta-705a14/report.json` |

已查看双版本联机移入／抽回、接力成功／拒绝及单人移牌前后的 MCP 截图。完整矩阵不包含接力牌专项；接力的最终失败回退边界由最后构建的专项 DLL 实测覆盖。卡牌读档一致性通过原生 `ToSerializable` / `FromSerializable` 验证，不等同于全局存档断线恢复测试。本次不改变存档格式或重连流程，未另测断线恢复和跨版本联机。

最终包与测试游戏安装的实现 DLL SHA-256 一致：

- Stable：`D1B6FA56B87CB43838E2C4ECCE69B812A21A966340722A2A78FDE8C7E17FDFEE`
- Beta：`8E040A9CE5FAB4AA655F95D24D4687551D064443AF35EA370044D0EB0AA38DC9`

### 日志限制及环境准备失败

没有发现本次机制的 `[ERROR]`、专项断言失败或联机校验不一致。测试版引擎仍偶发输出 `ERROR: Invalid Task ID`（`worker_thread_pool.cpp:418`）；本次完整矩阵及专项日志共 11 条，测试版单人视觉日志 1 条。修复前的 `20260924-063609-a7d5d2` 基线日志已有 17 条同类记录，包含原版／原版对照组。这项既有引擎日志未在本任务处理，不能宣称日志完全无错误。

最初专项被回环地址格式误判挡住：Windows 显示 `::ffff:127.0.0.1`。现已按 IP 地址语义识别，并等待前一进程退出、端口释放。后续复制临时游戏时出现磁盘空间不足，改用既有客户端副本；这些环境准备失败保留在 `artifacts/review-regressions.log`、`artifacts/review-regressions-2.log`，最终通过结果为 `artifacts/review-regressions-3.log`。实际查看的主机 UDP 33771 和客户端 UDP 51175 均为回环地址，无须提权或调整防火墙。

客户端游戏缓存默认保留在 `artifacts/test-matrix/review-client-cache`，后续运行复用；可用 `-ClientGamesRoot` 指向工作树内已有测试副本。本次复用了 `artifacts/test-matrix/review-20260924-074616`。系统自动审批拒绝递归清理游戏副本，故保留了失败运行副本，不再重复删除；两套开发环境主游戏中的临时测试插件已按运行器流程移除；缓存客户端保留测试配置。
