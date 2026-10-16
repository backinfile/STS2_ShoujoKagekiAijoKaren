# 嫉妒特效

## Original user input

> [$worktree-task](C:\Users\17575\\.codex\skills\worktree-task\SKILL.md) 为嫉妒这张牌制作特效，读取 D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/docs/19.创意工坊特效Mod解包与制作流程.md 你先搜搜相关官方和周边参考资料，然后用imagegen生成特效表现预览图，然后参考这个预览图开始实现，最后录制效果成视频给我看看

## Brief task plan

- 原始检出：main，bcfe75e9db3ffdd659a8334b0085c985e00e8be1，工作区干净。
- 任务分支：codex/jealousy-vfx；工作树：../worktrees/STS2_ShoujoKagekiAijoKaren/jealousy-vfx。
- 查阅项目 VFX 流程、嫉妒卡牌与官方/周边资料，记录来源。
- 按 2026-10-16 修订生成攻击动作预览，实现后撤蓄势、身体突进、银槌挥击、命中回位，删除奖励演出。
- 双版本构建及针对性实机检查，通过 KarenSTS2MCP 录制视频并检查清理、速度模式和规则结果。
- 整理证据并合并为一个任务提交；同步 main 后，等待用户明确确认再合并。

## Historical v1 result (2026-10-15, 已停用)

- 已完成官方／授权周边和卡图参考记录，使用内置 imagegen 生成三段预览，并按图实现青绿弧光、金色星扣、命中碎光及闪耀耗尽奖励确认。
- 改动主体：`KarenJealousy.cs`、`NKarenJealousyVfx.cs` 与绘制分部；新增临时探针、专项场景和视频封装脚本。
- 正式版与测试版构建均通过（各 25 警告、0 错误）；两版普通／升级伤害 15／20，奖励 0／2，Fast、Instant、变换定位专项通过。双版本击杀与下一战清理通过。每版 13 个自定义节点全部释放，9 次手牌检查通过；成功轮次日志无新异常。早期截图／Vulkan 内存分配失败与处理保留在测试记录中。
- 原速双版本有声视频：`artifacts/jealousy-reviewed-20261015/preview/Karen-Jealousy-showcase.mp4`；原片及实际非静音音轨已核查。特效没有后期合成。
- 最终记录：`docs/testing/jealousy-vfx-2026-10-15.md`；参考与提示词见 `docs/design/rare-vfx/jealousy-source-selection.md` 和 `jealousy-2026-10-15/`。
- 未执行全面单人／多人矩阵及第三方共存测试。测试均在任务隔离游戏副本中进行，共享开发环境和原始 checkout 未修改。
- 实现、媒体和双版本专项已完成。功能提交 `5372733`；已同步 main 的 `86abd19`，唯一录制入口冲突保留双方意图。同步后双版本包重建通过、重新录制两版普通／升级出牌，两版击杀专项再次通过。嫉妒运行代码经 diff 核对未因同步变化。合并回 main 仍须用户明确确认，任务分支与工作树保留。

## Follow-up user input (2026-10-16)

> 重新制作，更侧重攻击这个动作，闪耀耗尽的奖励不用制作特效

## Revision plan

- 保留已核实的真昼武器造型与青绿配色，重新生成以攻击动作优先的 ImageGen 预览。
- 将星扣投射物替换为蓄势、身体突进、银色棱面槌挥击、命中冲击和回位；只借用角色 Sprite2D 的可叠加 Offset，不改变生物位置或玩法模型，退出时归还本次偏移。
- 删除嫉妒专属的奖励演出和确认音效，保留原有遗物奖励逻辑。
- 两版定向实测，重录原速实机视频；旧版设计与证据标为历史方案。

## Result and changes

- 已完成攻击动作重制。当前设计为 `concept-attack-v2.png`，完整内置 imagegen 提示词为 `prompt-attack-v2.txt`；旧版星扣投射物和奖励效果已停用。
- `KarenJealousy` 保留原有伤害与奖励命令，删除专属奖励演出入口；VFX 改为银灰棱面槌、局部青绿挥击弧、角色短后撤与突进、最多三张纹理残影，真实命中后收势回位。
- 通过 `Sprite2D.Offset` 的自有可叠加分量实现身体动作，不修改生物位置或原生动画 Position 轨；节点退出时减回偏移。确认音效从奖励中删除，重击声接到真实命中。
- 双版本构建通过（各 25 警告、0 错误）；两版普通／升级 15／20、Fast、Instant、缩放翻转、攻击中途移除视觉后的偏移恢复均已实测通过，奖励逻辑正确且没有奖励视觉节点。正式版击杀和下一场清理通过；测试版击杀将随同步后的包单独检查。
- 原检出仍为 main，任务期间已被其他工作推进；未跟踪的 `docs/20.卡片特效制作经验提示词.md` 属于原检出其他工作，原样保留，不进入本任务。
- 当前证据：`artifacts/jealousy-attack-reviewed-20261016/`；本轮记录：`docs/testing/jealousy-attack-vfx-2026-10-16.md`。双版本原速有声片已录，等待同步包重录与最后复核。
- 仅做相关单人专项，未做 full／多人。仍待用户明确确认合并回 main，保留工作树与任务分支。
