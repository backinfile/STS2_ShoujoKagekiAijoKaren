# 约定之塔与角色变换

## 原因与修复

塔节点挂在 `NCreature`，原先只复制 `VfxSpawnPosition`。原版缩小修改的是子节点 `Visuals.Scale`，因此人物缩到 50% 后塔仍是 100%，塔脚落到血条下面。测试版修复前实测：人物缩放 0.5、塔缩放 1，飞入锚点误差约 83.104。

现在每帧从 `Visuals.GlobalTransform` 的轴长度计算塔的尺寸，从原版 VFX 标记计算中心；不继承负缩放或旋转，塔与计数始终正向。计数跟随引线位置但保留字号。飞入锚点读取时刷新位置；卡牌拖尾保存世界坐标，角色在飞行期间变换不会拉伸历史轨迹。

原版 `ShrinkPower` 通过 `NCreature.ScaleTo(0.5, ...)` 缩小；原版 `SurroundedPower` 翻转 `Body.Scale.X`。测试插件分别走这些相同的视觉变换入口，并额外覆盖整个 Visuals 翻转、旋转、非等比缩放和位移。

## 可重复测试

先构建并安装当前双版本包。测试用插件仅临时部署，不进入发布包：

```powershell
pwsh -NoProfile -File tools/test_promise_transforms.ps1 -Environment stable
pwsh -NoProfile -File tools/test_promise_transforms.ps1 -Environment beta
```

如正式版被正在游玩的进程占用，可以复制游戏并配置独立 MCP 端口，再传 `-GameDirectory <副本/game> -McpPort <端口>`。不能停止用户正在进行的战斗来腾出端口。

测试依次保存正常、50% 缩小、150% 放大、Body 翻转、缩小加翻转、Visuals 翻转、旋转、非等比缩放、位移与恢复的 MCP 截图，检查塔的正向基底、缩放、中心和飞入锚点。然后在缩小加翻转状态执行《坠落》→《约定之塔》，并核对 `karen_check_hand`。`-AllowPlacementFailure` 用于一次收集多个失败姿态，最终报告仍应失败。

## 本次结果

- 修复前：`test_promise_visuals.ps1 -Environment beta -TransformChecks -AllowPlacementFailure`，缩小、放大、缩小加翻转、非等比缩放均由断言捕获；缩小截图复现用户反馈。
- 正式版 v0.107.1：全部姿态坐标检查通过；缩小加翻转时存入、抽回、取回后继续打牌通过；已查看前后截图。手牌中目标牌数量 4→3→4，模型与可见节点一致。
- 测试版 v0.111.0：相同检查通过，目标牌数量 4→3→4；已查看截图。
- 双版本专项日志未发现 `ERROR` 或异常。

本地证据（均在本工作树 `artifacts/`，不提交二进制截图）：

- 修复前：`test-promise-visuals/20260927-075354-beta-e229e1/`
- 正式版：`test-promise-visuals/20260927-075628-stable-a2dbb4/`
- 测试版：`test-promise-visuals/20260927-075614-beta-8e9785/`
- 构建：`transform-build.log`

完整矩阵按用户「先不需要做完整测试」的指示中止，不能记为完整通过。中止前已完成 29 项，均通过：双版本启动、单人闪耀耗尽、两版各四种基本联机组合、两版闪耀耗尽与全局移牌专项，以及正式版三组和测试版两组约定牌堆存取。已查看双 Karen 联机存取前后截图。测试版原版主机＋Karen 客户端的存取用例中断，回合结束转换用例未执行。

矩阵记录：`artifacts/test-matrix/20260927-075846-b9c987/`，输出 `artifacts/transform-matrix.log`。为保留用户正在玩的正式版进程，临时测试脚本使用正式版隔离副本和端口 15727，其余测试流程保持原套件；使用本轮开始前已构建的当前 MCP（150c63f，loopback only），未覆盖正在运行环境的 MCP。测试进程已停止，用户原游戏进程保留。

## 部署状态

改动位于 `codex/promise-constellation` 工作树，发布包为该工作树的 `artifacts/ShoujoKagekiAijoKaren/`。测试版开发环境和正式版隔离测试副本已安装；原正在运行的正式版战斗保持原进程，尚未加载新 DLL。

2026-09-28 合并清理：上述为验证当天的部署状态。工作树的构建包、截图、测试日志和隔离存档归档到主仓库 `artifacts/archive/promise-constellation-20260928/`；上文相对于工作树 `artifacts/` 的证据路径均可在该归档根目录下找到。清理工作树不代表重新部署主仓库的开发游戏。
