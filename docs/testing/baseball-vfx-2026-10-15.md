# 棒球：蓄势、缝线球飞行与真实命中

## 范围与实现

按用户确认的 [ImageGen 概念图](../design/rare-vfx/baseball-2026-10-15/concept-v1.png) 实现前三格，不加入第四格保留图标环。原图与完整提示词存放在同一目录。

- 0–0.15 秒：角色前方的短白金挥击弧，外沿带红色棒球缝线。
- 0.15–0.35 秒：白色球体带两条红缝线旋转，沿浅弧线飞往真实目标，后接渐细的白金轨迹。
- 真实命中时：原生 `AttackCommand.WithHitVfxNode` 创建目标处的短闪、碎光和散开的缝线弧。此回调同时结束球体，避免独立计时伪造命中。
- Fast 使用 0.7 倍时间；Instant / NonInteractive 不创建视觉节点。球体、轨迹、命中节点均归属于本次攻击，没有全局持有的出牌实例。
- 当前双方 `Visuals` 的 Bounds 经完整画布变换定位，兼容临时缩放、翻转、偏移和旋转；无 Bounds 时回退交互框。失去战斗/角色时取消飞行，命中保留短尾，换房间直接清理。
- 程序绘制球体、缝线、渐变光与固定数量的碎光，不新增大幅贴图、帧序列、常驻效果或第三方 VFX 依赖。命中复用游戏原有 `blunt_attack.mp3`，与命中回调同步。

卡牌仍为 3 费、24／32 伤害；命中后依次施加原来的四项 2 回合保留能力。没有更改伤害计算、Power 类型、层数或升级值。

## 定向检查与证据

制作工作树 `../worktrees/karen-baseball-vfx-20261015`，分支 `codex/karen-baseball-vfx-20261015`，从 main `5311a28` 开始，功能提交 `d72b5a5`。2026-10-15 用户确认合并后快进集成 main；证据归档到主目录 `artifacts/baseball-vfx-20261015/`，最终双版本包另存该目录的 `package/`。不归档隔离游戏副本；共享开发环境未修改。main 原有概念图和提示词经 SHA-256 核对与任务副本一致，保留原内容并纳入版本管理。

最终包已完成双版本实机专项。本轮仅做与棒球有关的单人专项，不运行完整矩阵、多人或第三方 Mod 共存测试。

| 项目 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 双版本最终构建 | 通过，25 个原有警告、0 错误 | 通过，25 个原有警告、0 错误 |
| 普通／升级伤害 | 24／32，通过 | 24／32，通过 |
| 四项保留能力 | 首次各 2，连续第二次各 4，通过 | 首次各 2，连续第二次各 4，通过 |
| Fast／Instant | Fast 缩时且完整命中；Instant 无自定义视觉，规则正常 | 通过，同左 |
| 缩至 65%＋翻转、目标偏移＋旋转 | 通过，特效 unit 为 0.63375 | 通过，特效 unit 为 0.63375 |
| 最大命中定位误差 | 0.0001221 像素 | 0.0001221 像素 |
| 普通两次、Fast、变换、击杀的节点清理 | 10／10 释放；新战斗无残留 | 10／10 释放；新战斗无残留 |
| `karen_check_hand` | 12／12 通过 | 12／12 通过 |
| 最终日志异常 | 0 | 0 |
| 正常出牌录制实际采集帧率 | 29.17 fps | 29.95 fps |
| 前台／恢复窗口采样 | 均为 0 | 均为 0 |

已查看两版合计 22 张前后 MCP 截图，以及实录中蓄势、飞球、命中和最终字幕画面。两段 6.3 秒预览均为原速，有游戏进程音轨；仅为预览调整音量，不改变游戏资源或混入其他音源。最终录制 SHA-256 与本次双版本包一致，见 `baseball-summary.json` 和各版 `runtime-verification.json`。

证据根目录：[artifacts/baseball-vfx-20261015](../../artifacts/baseball-vfx-20261015)。其中 `stable`／`beta` 保存正常实录、规则状态、逐帧观察、前后截图及日志；`baseball-fast`、`baseball-instant`、`baseball-pose`、`baseball-lethal` 是各版目录内的专项子目录。临时测试 DLL 与配置已从隔离游戏中移除，测试游戏进程已结束。

- [正式版有声预览](../../artifacts/baseball-vfx-20261015/preview/stable/Karen-Baseball-stable.mp4)
- [测试版有声预览](../../artifacts/baseball-vfx-20261015/preview/beta/Karen-Baseball-beta.mp4)
- [机器可读结果](../../artifacts/baseball-vfx-20261015/baseball-summary.json)

复现入口：

```powershell
./tools/build_dual_branch.ps1
# 从现有独立开发环境复制游戏与用户存档；工作树中可用 --source-dev 指定来源。
python tools/vfx/catalog/prepare.py stable --output artifacts/baseball-vfx-20261015 --source-dev <开发环境的artifacts/dev-game> --port 18647
python tools/vfx/catalog/prepare.py beta --output artifacts/baseball-vfx-20261015 --source-dev <开发环境的artifacts/dev-game> --port 18648
python tools/vfx/catalog/record.py stable --output artifacts/baseball-vfx-20261015 --scenes baseball --port 18647
python tools/vfx/catalog/record.py beta --output artifacts/baseball-vfx-20261015 --scenes baseball --port 18648
python tools/vfx/catalog/verify_runtime.py --output artifacts/baseball-vfx-20261015 --branch stable --scenes baseball
python tools/vfx/catalog/verify_runtime.py --output artifacts/baseball-vfx-20261015 --branch beta --scenes baseball
```

`baseball.py` 使用真实出牌验证 24／32 伤害、四种保留能力的实际层数、连续出牌、Fast／Instant、变换定位和击杀后的新战斗；`BaseballProbe.cs` 逐帧观察节点定位、命中回调与退出。探针只装入隔离副本，不随 Karen 发布。录像只包含正常出牌，其他情况只保存状态与 MCP 截图。

首轮观察发现临时缩放没有更新原生交互框，已改用画面 Bounds。早期击杀测试中的 `IntegrationWatch.Single()` 异常来自依赖在场约定塔的旧测试观察器；在击杀前停用该观察器，棒球自己的观察器继续记录清理。早期证据保留于 `v1-stable`、`v2-stable`，不作为最终验收结果。
