# 创意工坊发布前功能与产物校验：2026-11-16

本轮 Karen 1.0.0 / Core 1.9.5 候选的完整功能矩阵与发布产物校验通过。正式版实测 v0.107.1，测试版实测 v0.113.1；测试版实现基于 v0.113.0 编译。工坊上传尚未执行。

## 候选与环境

- Karen 起点：`7c96793d0ee2f7f39b9872d2949c2493bcd8c939`；Core 起点：`97a67a46d94c53f8cbaf70793d7aadbe5fa9235e`。
- BaseLib 3.4.8，当前 KarenSTS2MCP 源码分别针对两版构建；独立用户数据、日志、MCP 端口和本机联机端口。没有使用共享 Steam 存档或其他项目游戏目录。
- 同机其他测试占用了默认端口和显存，本轮功能矩阵改用独立端口及 OpenGL compatibility。逻辑测试隐藏启动，约定截图专项使用隔离桌面；静音配置为 `silent_test`。窗口监控没有发现抢焦点。
- Core 两实现及入口构建零错误、零警告；Karen 两实现零错误、各 23 条既有警告，入口零错误、零警告。Godot 资源导出完成。

## 功能结果

| 项目 | 结果 | 检查内容 |
| --- | --- | --- |
| 完整双版本矩阵 | 36/36 通过 | 单人开局、清场耗尽；每版四种角色主客组合；双方出牌与回合同步；全局移牌、约定移入取回、回合末转化 |
| 审查回归 | 8/8 通过 | 双版四种角色组合；升级/降级和副本、额外重播、单次闪耀预算、转交与拒收、能量、远端 UI、遗物清理 |
| 当前与旧字段存档 | 8/8 通过 | 每版 current、legacy-core、legacy-karen、mixed；当前格式各两轮退出/重启，其余各一轮；成长字段、耗尽档案、空壳、重新获取和同步 |
| 丢档对照 | 通过 | 故意清除档案，测试确实以 SL changed archive 失败，证明断言能识别用户报告的问题 |
| 测试版真实出牌 SL | 3/3 通过 | 实际打出闪耀打击耗尽、原生房间自动保存及退主菜单续玩、进程重启、强制关闭恢复和同场战斗回滚 |
| 单人约定移牌 | 2/2 通过 | 坠落→约定之塔→取回后出牌、13 张牌批量存取、四种模式切换；截图与只读手牌检查 |
| Core 离线检查 | 10 组通过 | Foundation、API 551 项兼容、Bootstrap、旧模型编译、双版 Resolution/Observation、生命周期、选牌 |
| Core 游戏内回归 | 双版本通过 | 舞台理由旧 ID/三语/旧字段/重播/耗尽重新获取；无节点、释放节点、奖励原生序列化、无主预览 |
| Loader / 手牌审计工具 | 各 8 项通过 | 装载策略与可见手牌检查 |
| 本地化 | 通过 | 10 表、457 条中文、914 条翻译、10,968 次原生 SmartFormat 分支/复数渲染 |
| 截图及日志 | 通过 | 已查看 200 张截图的联系表，并复核移牌截图；144 份游戏日志没有非预期 ERROR |
| 最终发布包 | 通过 | 18 文件 SHA-256、双环境部署一致、Core 内嵌一致、两 PCK 的 1,114 个条目 MD5、SDK 严格校验、封面与依赖 |

已保存的耗尽卡在退出、重启和强制关闭后仍可查看。刚在当前战斗耗尽的卡随战斗回滚：档案数量 0→1→0，永久卡组 10→9→10，闪耀恢复到战斗起点 9/9。该回滚没有造成永久丢牌。

## 本轮修正

- 为中英日三语补齐 `KAREN.shortTitle`、`KAREN.bestiaryQuote`、`SEA_GLASS.KAREN.title`，共 9 条；清理已迁移到 Core 的两条旧源哈希，刷新新增源哈希。
- 更新说明的随包 Core 版本从 1.9.4 改为 1.9.5。
- 测试工具支持独立 MCP/联机端口、可选 GL 渲染和 Python 路径；客户端复用原生文件硬链接，Mod 和配置单独复制。默认 Vulkan 和默认端口保持不变。
- 未修改玩法 C#。最终六份本地化 JSON 的每个字符串与已测 PCK 完全一致；格式整理不影响包内文案。

## 逐项完整矩阵

| 用例 | 状态 |
| --- | --- |
| `smoke-stable` | 通过 |
| `smoke-beta` | 通过 |
| `solo-stable` | 通过 |
| `solo-beta` | 通过 |
| `mp-stable-KAREN-KAREN` | 通过 |
| `mp-stable-KAREN-IRONCLAD` | 通过 |
| `mp-stable-IRONCLAD-KAREN` | 通过 |
| `mp-stable-IRONCLAD-IRONCLAD` | 通过 |
| `mp-beta-KAREN-KAREN` | 通过 |
| `mp-beta-KAREN-IRONCLAD` | 通过 |
| `mp-beta-IRONCLAD-KAREN` | 通过 |
| `mp-beta-IRONCLAD-IRONCLAD` | 通过 |
| `mp-stable-KAREN-KAREN-shine-host` | 通过 |
| `mp-stable-KAREN-KAREN-shine-client` | 通过 |
| `mp-stable-KAREN-IRONCLAD-shine-host` | 通过 |
| `mp-stable-IRONCLAD-KAREN-shine-client` | 通过 |
| `mp-stable-KAREN-IRONCLAD-global-move` | 通过 |
| `mp-stable-IRONCLAD-KAREN-global-move` | 通过 |
| `mp-beta-KAREN-KAREN-shine-host` | 通过 |
| `mp-beta-KAREN-KAREN-shine-client` | 通过 |
| `mp-beta-KAREN-IRONCLAD-shine-host` | 通过 |
| `mp-beta-IRONCLAD-KAREN-shine-client` | 通过 |
| `mp-beta-KAREN-IRONCLAD-global-move` | 通过 |
| `mp-beta-IRONCLAD-KAREN-global-move` | 通过 |
| `mp-stable-KAREN-KAREN-promise` | 通过 |
| `mp-stable-KAREN-IRONCLAD-promise` | 通过 |
| `mp-stable-IRONCLAD-KAREN-promise` | 通过 |
| `mp-beta-KAREN-KAREN-promise` | 通过 |
| `mp-beta-KAREN-IRONCLAD-promise` | 通过 |
| `mp-beta-IRONCLAD-KAREN-promise` | 通过 |
| `mp-stable-KAREN-KAREN-promise-turn-end` | 通过 |
| `mp-stable-KAREN-IRONCLAD-promise-turn-end` | 通过 |
| `mp-stable-IRONCLAD-KAREN-promise-turn-end` | 通过 |
| `mp-beta-KAREN-KAREN-promise-turn-end` | 通过 |
| `mp-beta-KAREN-IRONCLAD-promise-turn-end` | 通过 |
| `mp-beta-IRONCLAD-KAREN-promise-turn-end` | 通过 |

## 逐项审查回归

| 用例 | 状态 |
| --- | --- |
| `stable-KAREN-IRONCLAD-review-regression` | 通过 |
| `stable-IRONCLAD-KAREN-review-regression` | 通过 |
| `stable-IRONCLAD-IRONCLAD-review-regression` | 通过 |
| `beta-KAREN-KAREN-review-regression` | 通过 |
| `beta-KAREN-IRONCLAD-review-regression` | 通过 |
| `beta-IRONCLAD-KAREN-review-regression` | 通过 |
| `beta-IRONCLAD-IRONCLAD-review-regression` | 通过 |
| `stable-KAREN-KAREN-review-regression` | 通过 |

## 逐项存档恢复

| 用例 | 状态 |
| --- | --- |
| `stable-current` | 通过 |
| `stable-legacy-core` | 通过 |
| `stable-legacy-karen` | 通过 |
| `stable-mixed` | 通过 |
| `beta-current` | 通过 |
| `beta-legacy-core` | 通过 |
| `beta-legacy-karen` | 通过 |
| `beta-mixed` | 通过 |
| `beta-negative-control` | 通过（成功检出故意丢档） |

## 重试与保留事项

- 第一轮默认端口被其他任务占用，磁盘复制空间不足；第二轮 Vulkan 遇到显存不足。两轮基础设施失败保留原始日志，调整隔离端口、硬链接及 GL 后完成上述 36 项。
- 审查回归首次正式版 Karen/Karen 在原生大厅握手 10 秒超时，未进入功能断言；独立重跑全部 18 个两端阶段通过。其余七组首次通过。
- 原始批量报告的 smoke-beta 文本仍显示编译基线 v0.113.0；实际运行版本由 release_info、游戏日志和包部署审计确认为 v0.113.1。
- 既有 OBS-046 的原始 37 Mod `RitsuBoundary effects=0` 组合没有重建，仍待原组合复测。已有 NativeBoundary 修复和合成转发回归通过；本轮不把该指纹标成已修复。详见 Core 的 error-watch-20261116 任务记录。
- 按用户选择，特效覆盖、首播/重复播放和帧耗时专项未执行。本轮没有 APK 目标。

## 发布包指纹

所有 18 个文件的完整清单保存在本地证据 `package-audit.json` 和 `final-summary.json`。

| 文件 | SHA-256 |
| --- | --- |
| `ShoujoKagekiAijoCore/core.bundle` | `438f555930618aebb85cbe85a321a377c79166221623455a475ba19a3fb36ebc` |
| `ShoujoKagekiAijoCore/ShoujoKagekiAijoCore.dll` | `298c016576c15bfec483bd30c57ad0f9d9a30693be632414d29356fb4aa2ff9b` |
| `ShoujoKagekiAijoCore/ShoujoKagekiAijoCore.json` | `02735c3fafc5fb6339697fb9977ca08f4ca3e97a67b6f5664a220e7faac52fd1` |
| `ShoujoKagekiAijoKaren/core.package.lock` | `04bc2fa26315f4ad02c1aa2ed345cfa8093b55e56a125b491c88602fb2cdfcc1` |
| `ShoujoKagekiAijoKaren/core.requirements` | `f15e5c31f7ea779551f180b5c281a03c03ada4f88c5eaffc1a92127f1e811ebb` |
| `ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.dll` | `5aee49eef725ca3144433d9986949f61778e7611a6d996427ec2f304a010d664` |
| `ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.json` | `4e957f31f8dfbc5a29b86a0dd802a15f971e55e25baafaeda8fbe9703c4cb803` |
| `ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck` | `5da057236b3275a6a931896fb2b71e59b0255592076f1dc7059305a59318e5c4` |
| `ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.Beta.dll` | `a78cfbf935955dd9aa6f0f19ba1c5dfe472b1986dda18f1f7078be979725cd92` |
| `ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.Stable.dll` | `deb69fecb68fcf18d9765fb59067bcdea3c095e33ef32e201d86fdd1395899b2` |
| `ShoujoKagekiAijoCore/sdk/BundleValidation.cs` | `8a08d43552c2fd3f3f905ed29213d4272c089ebdd2e8015134179e419c06e27e` |
| `ShoujoKagekiAijoCore/sdk/CharacterLoader.cs` | `f10e98f4c0172cc5db9d2a8a43d2a89658c95dc882bbecfb1f61d43c8a98e5ff` |
| `ShoujoKagekiAijoCore/sdk/pack-character.ps1` | `3b66e7c4af869f96c26c153349f7fb109e7f51e0dc87688748e28eed9b5704b9` |
| `ShoujoKagekiAijoCore/sdk/validate-package.ps1` | `76cb398a3f980ec4a25b3f945e127580a79526b5ff4008ecdef397ec637abb4d` |
| `ShoujoKagekiAijoCore/resources/1.9.5/Core.Resources.pck` | `d2b15c522c951a9a4caba2b6b2aa6376c94cc87603da79f68903e5998b66642e` |
| `ShoujoKagekiAijoCore/lib/1.9.5/api/ShoujoKagekiAijoCore.Api.V1.dll` | `1f78b18522d58effde2eb83925ec0356f6b2f0bda89dd150a7d77156b7cf8455` |
| `ShoujoKagekiAijoCore/lib/1.9.5/beta/ShoujoKagekiAijoCore.Runtime.Beta.dll` | `caede3d7523409fa45142abfc5e0dde54a5b31f4b561c06ae8b7b9904b2d49bf` |
| `ShoujoKagekiAijoCore/lib/1.9.5/stable/ShoujoKagekiAijoCore.Runtime.Stable.dll` | `a97d78fde0aa820f5924d0bc637e57dddc08b65ba0ef18032a5ae8364c24eb93` |

## 证据索引

Karen 本地证据根：`out/tmp/workshop-full-test-20261116/`；Core 使用同名证据根。日志、存档、截图和私有运行材料保存在 Git 忽略目录。

- 完整矩阵：`artifacts/test-matrix/20261116-081730-bad48b/report.json`。
- 审查回归：`artifacts/test-matrix/review-20261116-084803/`，正式版双 Karen 重试 `artifacts/test-matrix/review-stable-retry-20261116/`；最终汇总 `review-summary.json`。
- 约定截图：`artifacts/test-promise-visuals/20261116-091323-stable-b7ba07/`、`artifacts/test-promise-visuals/20261116-091453-beta-aa5242/`。
- Core：`persistence-summary.json`、`persistence/`、`published-native-1131-final/`、`published-force-restart-1131/`、`published-battle-rollback-1131/`、`offline-final-checks.json`。
- 复核：`matrix-log-audit.json`、`additional-log-audit.json`、三个 `*-visual-manifest.json`、`promise-summary.json`、`final-hash-check.json`。
- 上传工作目录：`upload-workspace/`；封面 91,191 字节、BaseLib 工坊依赖及 18 文件内容已核对。未执行上传。
- 收尾归档位置：各原仓库 `artifacts/task-deliverables/workshop-full-test-20261116/`。工作树删除前保留候选上传目录及原始证据压缩包。
