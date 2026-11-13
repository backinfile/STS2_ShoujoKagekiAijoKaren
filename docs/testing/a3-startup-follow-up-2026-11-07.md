# 进阶 3 续玩问题后续：启动预热与预载

2026-11-07。原始记录位于 `artifacts/testing/a3-resume-20261107/`，本次定向诊断与回归证据位于 `artifacts/testing/a3-issues-20261107/`。

后续按用户要求，已清理本次对战的 52 个原始、剪辑及副本视频和 4 个临时音频，合计 4,901,304,609 字节。日志、截图及既有验证结果保留，删除清单见本次证据目录的 `recording-cleanup.json`。

## A3-01：已修复后台窗口下的预热停顿

`KarenVfxWarmup.Prepare` 原先等待两次全局 `FramePostDraw`。窗口隐藏或最小化时，自动渲染不能持续推进，预热会停在等待绘制处。本次在旧实现的实际启动流程中复现：前四批完成后最小化窗口，第五批一直等待，35 秒超时且主菜单未完成；初始隐藏启动另有 120 秒超时样本。

原进阶 3 日志中的 `VFX warmup batch did not draw` 及 68,119.2ms 失败记录保留。本轮直接复现的是后台绘制停顿，未再次捕获同一零绘制异常。

修改 `src/Core/Vfx/KarenVfxWarmup.cs`：

- 每批等待主线程 `ProcessFrame` 后调用 `RenderingServer.ForceDraw(false)`，主动绘制离屏视口。
- 两帧完成后调用 `RenderingServer.ForceSync()`，再读取该视口的真实绘制计数；零绘制仍然判为失败。
- 每次准备重置 `DrawnBatches` 和 `DrawCalls`，避免失败重试时累计上一次的计数。

临时计时日志已从生产代码删除。资源、倒地素材及卡牌规则没有改动。

## 最终源码定向验证

版本：Karen v0.2.0、Core 1.0.1、BaseLib 3.4.7；正式版 v0.107.1 / MegaDot m.12，测试版 v0.111.0 / MegaDot m.14。均使用 Vulkan、静音和隔离桌面。双版本构建均为 0 错误、32 条警告。

| 场景 | 结果 | 预热耗时 | 批次 / 绘制调用 | 主菜单耗时 |
| --- | --- | --- | --- | --- |
| 旧实现，预热中最小化 | 失败：35 秒超时 | 未完成 | 已完成 4 批 | 未完成 |
| 最终测试版，同一最小化夹具 | 通过 | 1,697.6ms | 30 / 597 | 7,989ms |
| 最终测试版，从隐藏窗口启动并最小化 | 通过 | 1,725.8ms | 30 / 597 | 8,617ms |
| 最终正式版，从隐藏窗口启动并最小化 | 通过 | 1,707.7ms | 30 / 597 | 7,709ms |

三个最终样本均确认：311 个预热资源有效、临时视口已释放，重复 `EnsureReady` 立即复用原结果且没有新增绘制；检查时窗口状态为 `Minimized`。重复调用分别耗时 0.1478ms、0.1610ms、0.1368ms。三个样本均无错误日志，未复现 `Invalid Task ID`。

本次只覆盖启动预热。完整特效专项、单人战斗和多人矩阵未执行；本次改动不涉及规则或移牌。

最终分支 DLL SHA-256：

| 产物 | SHA-256 |
| --- | --- |
| `final-build/ShoujoKagekiAijoKaren.Stable.dll` | `7A77621477A6C2157A3E665B7D78A3FC817FEA919372964D9DA5C0CE943641C1` |
| `final-beta-build/ShoujoKagekiAijoKaren.Beta.dll` | `B609C729D1763D80E215BE9372387D00EFFC2CAB953481D0F6AA206782569F4D` |

上表路径相对本次证据目录。源码与受测包完整哈希见该目录的 `evidence.json`。受测 PCK 沿用原进阶 3 包，代码修改通过替换对应分支 DLL 验证。最终整理的双版本包位于 `fixed-package/`，不含 MCP 或诊断插件。

## A3-02：Invalid Task ID 仍待修复

已确认它不要求加载 Karen、Core 或 BaseLib：仅加载原进阶 3 使用的 MCP DLL，等待原生 Common 预载完成，即可间歇复现 `wait_for_task_completion (core/object/worker_thread_pool.cpp:418)`。复现过程中没有发游戏动作，也没有预先查询游戏状态。

| 对照 | 本次观察 |
| --- | --- |
| 原始完整 Mod 组合 | Common 阶段 2 条错误，随后完成预载 |
| 仅 MCP | 有 1 条错误样本，也有正常样本 |
| 仅 MCP＋资源加载跟踪 | 有 3 条错误样本，随后完成 792 个资源的预载 |
| 无 Mod | 本次 1 次启动无错误 |
| 仅加载空诊断 Mod | 本次 1 次启动无错误 |
| MCP 关闭静音桥接 | 2 次启动无错误；开启桥接也出现过正常启动，不能据此认定它是根因 |
| MCP，资源加载并发由 128 改为 1 | 本次无错误，Common 耗时由约 4 秒增至 26,637ms |

串行对照支持继续调查并发加载时序，但单次成功不足以确认具体竞态，且启动开销显著，因此未加入生产补丁。仍需取得出错时的原生加载调用栈及共享依赖路径，区分 MCP 与引擎交互、递归资源加载或渲染任务等待。当前不能将此项标为已修复或断言完全与 Mod 无关。

上游曾修复递归资源加载中重复等待任务 ID 的同名报错，相关入口为 [Godot PR #104060](https://github.com/godotengine/godot/pull/104060)。该修复早于当前引擎版本，不能直接当作本次故障的根因或修复方案。

## A3-03：误报已撤销

用户已确认 `images/packed/karen_corpse.png` 就是华恋倒地素材。原续玩报告、JSON 及其生成脚本已纠正，不替换此素材。

## 复现入口与证据

最小化回归使用实际游戏启动和真实资源绘制，夹具仅在第 40 次预览节点添加后最小化窗口，并检查预热状态、资源有效性、复用及清理：

```powershell
python artifacts/testing/a3-issues-20261107/startup_probe.py --source <隔离游戏源> --mode full --name <新的证据目录名> --dll <待测BetaDLL> --minimize-batch --stop-show-after-minimize --verify-warmup --timeout 35
```

旧实现结果为 `old-minimize-control/result.json`；最终结果为 `final-beta-paired/result.json`、`final-beta-verified/result.json`、`final-stable-verified/result.json`，各目录保存完整日志与检查结果。诊断脚本、旧实现计时 DLL、反编译摘录及测试插件保留在已忽略的本次证据目录，不进入发行包。

初次正式版夹具误用了测试版 MCP DLL，产生 `StartRunLobbyPlayer` 类型缺失；该夹具无效。正式版已改用针对 v0.107.1 构建的 MCP 并完成上述最终回归。最初双版本共用构建输出目录导致测试版产物被清理，随后改用独立输出目录，最终测试均使用上表哈希对应 DLL。
