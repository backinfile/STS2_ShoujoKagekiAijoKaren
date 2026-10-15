# 嫉妒：青绿弧光与金色星扣

## 设计与实现

按 [官方与周边参考记录](../design/rare-vfx/jealousy-source-selection.md) 和 [ImageGen 三段预览](../design/rare-vfx/jealousy-2026-10-15/concept-v1.png) 实现。完整内置 imagegen 提示词与概念图已纳入版本管理。概念图只用于特效造型；游戏使用原有 Karen 角色。

- 攻击前 0.42 秒，青绿弧光、细金线及星扣从真实角色沿曲线到达目标。
- 原生 `AttackCommand.WithHitVfxNode` 触发目标处斜切光、五角星金属扣、金白短闪与 18 个有限碎光，并终止飞行物。命中使用游戏原生 `slash_attack.mp3`。
- 原有 `OnShineExhausted` 奖励循环成功后才播放奖励确认：1／2 颗星扣在所属角色头肩上方升起、金线绕环、尾光消散；复用已有 `karen_star_resolve.wav`，走游戏 SFX 总线。
- Fast 时间乘 0.7；Instant / NonInteractive 不创建自定义视觉。没有全局停时、滤镜或移动角色。
- 源与目标的真实 Bounds 使用完整画布变换；节点归当前战斗。取消、失去主人、换房间和完成时释放；击杀留下短命中余光，然后清理。
- 卡牌费用、15／20 伤害、闪耀上限 9、奖励 1／2 份、原生闪耀移牌流程均保留。奖励是追加战后遗物奖励，没有提前发放遗物。

实录复核后，切光由较细的首版加宽，命中星扣放大，奖励星扣上移，减少与已有增益光效的重叠。没有改动生成预览图。

## 针对性验证

仅覆盖本次特效的双版本单人专项；未执行全面单人／多人矩阵或第三方 Mod 共存验证。所有游戏操作、截图和录屏均通过 KarenSTS2MCP；未使用 Computer Use，未修改共享开发游戏、其他项目或 Steam 安装。

最终证据根目录：`artifacts/jealousy-reviewed-20261015/`。制作工作树：`../worktrees/STS2_ShoujoKagekiAijoKaren/jealousy-vfx`，分支 `codex/jealousy-vfx`；开始时 main 为 `bcfe75e` 且工作区干净。

最终双版本结果待补入本节。

早期证据分别保存在 `artifacts/jealousy-vfx-20261015/` 与 `artifacts/jealousy-final-20261015/`。正式版曾两次在 MCP `CaptureScreenshot → Texture2D.GetImage()` 报原生内存分配失败，随后 HTTP 连接中断；当时还存在其他项目的多个游戏进程。未据此断言具体根因，也没有停止其他项目进程。改为减少重复初始截图、将击杀与换房间拆成独立进程后复测；早期失败日志不作为最终通过证据。

打包导出日志另含 Banana 类型读取和外部主题引用诊断；PCK 写出完成，卡牌的最终效果以双版本实际出牌证据核查，未把导出日志当作运行兼容结论。

## 复现与交付

```powershell
./tools/build_dual_branch.ps1
python tools/vfx/catalog/prepare.py stable --output artifacts/jealousy-reviewed-20261015 --source-dev <共享开发环境的artifacts/dev-game> --port 18727
python tools/vfx/catalog/prepare.py beta --output artifacts/jealousy-reviewed-20261015 --source-dev <共享开发环境的artifacts/dev-game> --port 18728
# 两版串行执行；core 后先保留日志和窗口采样，再单独启动 lethal。
python tools/vfx/catalog/record.py stable --output artifacts/jealousy-reviewed-20261015 --scenes jealousy,jealousy-checks --port 18727
# 复制 stable/godot.log 到根目录 stable-run-core-godot.log，窗口采样另存 stable-window-core.json。
python tools/vfx/catalog/record.py stable --output artifacts/jealousy-reviewed-20261015 --scenes jealousy-lethal --port 18727
# beta 用同样流程，端口换成 18728。
python tools/vfx/catalog/verify_runtime.py --output artifacts/jealousy-reviewed-20261015 --branch stable --scenes jealousy
python tools/vfx/catalog/verify_runtime.py --output artifacts/jealousy-reviewed-20261015 --branch beta --scenes jealousy
python tools/vfx/catalog/package_jealousy.py --source artifacts/jealousy-reviewed-20261015
```

测试探针 `JealousyProbe.cs` 只放入隔离游戏，观察原生实际出牌、真实战后 `RelicReward` 列表、视觉节点时序／定位／退出。普通与升级牌连续播放，检查伤害、普通牌不误发奖励，以及升级耗尽后两份奖励。另检 Fast、Instant、缩放翻转与目标偏移旋转、击杀、下一场战斗与 `karen_check_hand`。

成片只使用 MCP 采集的原始游戏画面与游戏进程声音，在原画面之外添加底部说明条、按测得峰值调整播放音量并拼接两版；不加速、不在后期添加特效。原片、采集元数据、录制构建哈希与最终音轨检查均保留。

- [双版本原速有声预览](../../artifacts/jealousy-reviewed-20261015/preview/Karen-Jealousy-showcase.mp4)
- [正式版单段](../../artifacts/jealousy-reviewed-20261015/preview/Karen-Jealousy-stable.mp4)
- [测试版单段](../../artifacts/jealousy-reviewed-20261015/preview/Karen-Jealousy-beta.mp4)
- [任务记录](../worktree-tasks/jealousy-vfx/README.md)
