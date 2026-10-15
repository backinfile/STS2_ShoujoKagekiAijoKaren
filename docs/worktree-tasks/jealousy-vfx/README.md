# 嫉妒特效

## Original user input

> [$worktree-task](C:\Users\17575\\.codex\skills\worktree-task\SKILL.md) 为嫉妒这张牌制作特效，读取 D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/docs/19.创意工坊特效Mod解包与制作流程.md 你先搜搜相关官方和周边参考资料，然后用imagegen生成特效表现预览图，然后参考这个预览图开始实现，最后录制效果成视频给我看看

## Brief task plan

- 原始检出：main，bcfe75e9db3ffdd659a8334b0085c985e00e8be1，工作区干净。
- 任务分支：codex/jealousy-vfx；工作树：../worktrees/STS2_ShoujoKagekiAijoKaren/jealousy-vfx。
- 查阅项目 VFX 流程、嫉妒卡牌与官方/周边资料，记录来源。
- 使用内置 imagegen 生成表现预览，依据预览实现真实攻击与闪耀耗尽反馈。
- 双版本构建及针对性实机检查，通过 KarenSTS2MCP 录制视频并检查清理、速度模式和规则结果。
- 整理证据并合并为一个任务提交；同步 main 后，等待用户明确确认再合并。

## Result and changes

- 已完成官方／授权周边和卡图参考记录，使用内置 imagegen 生成三段预览，并按图实现青绿弧光、金色星扣、命中碎光及闪耀耗尽奖励确认。
- 改动主体：`KarenJealousy.cs`、`NKarenJealousyVfx.cs` 与绘制分部；新增临时探针、专项场景和视频封装脚本。
- 正式版与测试版构建均通过（各 25 警告、0 错误）；两版普通／升级伤害 15／20，奖励 0／2，Fast、Instant、变换定位专项通过。正式版击杀与下一战清理通过。测试版首版击杀通过，最终视觉修订包的击杀复测因 Vulkan 缓冲分配失败中断，待单独复测。
- 原速双版本有声视频：`artifacts/jealousy-reviewed-20261015/preview/Karen-Jealousy-showcase.mp4`；原片及实际非静音音轨已核查。特效没有后期合成。
- 最终记录：`docs/testing/jealousy-vfx-2026-10-15.md`；参考与提示词见 `docs/design/rare-vfx/jealousy-source-selection.md` 和 `jealousy-2026-10-15/`。
- 未执行全面单人／多人矩阵及第三方共存测试。测试均在任务隔离游戏副本中进行，共享开发环境和原始 checkout 未修改。
- 实现与媒体已完成；正在同步 main 并完成最后的资源受限复测。合并回 main 仍须用户明确确认，任务分支与工作树保留。
