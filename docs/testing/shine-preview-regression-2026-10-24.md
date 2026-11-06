# 模板牌闪耀预览回归（2026-10-24）

## 原因与修复

原生转化界面每 0.2 秒切换候选模板牌。模板牌不是可变战斗实例，读取其 `CardModel.Owner` 会触发 `CanonicalModelException`；`Owner?.` 无法跳过属性 getter 中的断言。

`ShineVfx.WillDeplete` 先检查 `IsMutable`，模板牌返回 false，保留普通闪耀表现。真实实例继续使用现有阈值、主动耗尽、星光02、复制牌及永恒判断。

## 定向验证

修复前，在实际游戏模型上执行 `shine_vfx_regression`：58 项检查中，25 种模板牌的 50 项检查失败；8 项真实实例边界检查通过。

| 检查 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 修复后 58 项模型检查 | 通过 | 通过 |
| 原生转化预览 20 次采样 | 通过，4 次含闪耀候选 | 模板异常为 0，8 次含闪耀候选；另有下述资源错误 |
| 预览 `CanonicalModelException` | 0 | 0 |
| 实际打出：闪耀 2→1，进入弃牌堆 | 通过 | 通过 |
| 实际打出：闪耀 1→0，预警并耗尽 | 通过 | 通过 |
| 星光02：剩余闪耀大于 1 时预警并耗尽 | 通过 | 通过 |
| 移牌前后 MCP 截图及动画后 `karen_check_hand` | 6 组通过 | 6 组通过 |
| 编译 | 通过 | 通过 |

测试使用独立用户目录，通过 MCP 操作。启动前配置最小化、禁止抢焦点和静音；接口确认 `muted=true`、`silent_test=true`、`minimized_startup_guard=true`。测试结束前按进程枚举窗口，游戏窗口均为最小化状态。

测试夹具位于 `tools/promise-transform/ShineVfxRegressionCmd.cs`，随 `KarenPromiseTransformTests` 构建，不包含在正式 Mod 中。最小化测试通过环境变量 `KAREN_SHINE_VFX_REGRESSION=1` 启用夹具后台绘制，确保原生加载和预览能够产生实际渲染帧。加载夹具、进入无星光02的战局后执行 MCP 命令 `shine_vfx_regression`。

`tools/vfx/first-use/catalog.py --check` 通过：70 种特效、37 个共享资源所有者、164 项资源。

原始日志、模型结果、战斗结果、截图和 DLL 哈希保存在忽略目录 `artifacts/shine-preview-fix-20261024/`。前次进阶1完整战局证据保留在 `artifacts/karen-a1-20261024/`。

## 单独记录的问题与范围

测试版另有资源加载错误：`images/ui/flower/chain.png`、`chain.tres`、`chest_cradle.png`、`chest_cradle.tres` 及 `materials/vfx/past_future_trace.gdshader`。日志涉及原生资源加载及 `KarenVfxWarmup`，本次模板牌回归的 58 项检查和实际出牌检查均通过，但不据此宣称测试版资源兼容性完整通过。这组资源问题待单独排查。

本次未运行完整单人/多人矩阵、首次播放性能专项或发布校验，也未上传创意工坊。
