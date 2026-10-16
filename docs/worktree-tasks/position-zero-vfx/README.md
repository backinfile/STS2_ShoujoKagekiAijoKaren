# Position 0 特效

## Original user input

[$worktree-task](C:\Users\17575\\.codex\skills\worktree-task\SKILL.md) 为Position 0这张牌制作特效，读取 D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/docs/19.创意工坊特效Mod解包与制作流程.md 你先搜搜相关官方和周边参考资料，然后用imagegen生成特效表现预览图，然后参考这个预览图开始实现，最后录制效果成视频给我看看

## Brief task plan

- 阅读制作流程和 Position 0 卡牌/Power；搜集官方视觉与周边参考。
- 使用内置 imagegen 生成演出分镜，记录提示词。
- 按真实 Power 应用和格挡获得事件制作定位标记、聚光与确认反馈，支持快速/瞬时和清理。
- 在隔离游戏中做双版本针对性验证，使用 KarenSTS2MCP 录制视频并检查日志、画面、音轨。
- 整理一个任务提交，同步 main，等待用户明确确认后合并。

## Result and changes

已完成官方/授权参考检索、内置 imagegen 预览、实时特效实现及双版本针对性实测；待用户明确确认后合并到 `main`。

- 新增 `NKarenPositionZeroVfx`：T 形舞台定位带、双束暖光、低亮常驻地光、实际格挡确认后的短回流与护弧。普通混合保留红色实体，标记位于鞋底与血条之间。
- `KarenPosition0Power` 仅增加表现接线，保持现有攻击计数与格挡命令。正向层数变化重播起势；不增加格挡时不确认。每个 Power 复用同一节点，不改变全局时间、伤害与随机数。
- 快速模式缩短演出，瞬时/无交互模式跳过。能力移除、死亡、战斗结束、换房间、退树和瞬时切换释放节点、音效与合成权重。
- `tools/vfx/position-zero/` 提供观察夹具、隔离 MCP 实测、录像校验与剪辑脚本。未部署到 Steam 或共享开发环境。

| 验证 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 双版本打包/编译 | 通过 | 通过 |
| 正常施放、真实获得 6 格挡、第二击不误触发、自然下一回合重置 | 通过 | 通过 |
| 两层叠加、双重打击两次确认、单一常驻节点 | 通过 | 通过 |
| 快速、瞬时、零伤害不误确认 | 通过 | 通过 |
| 移除能力、起势期间结束战斗、合成权重恢复 | 通过 | 通过 |
| `karen_check_hand` 模型/可见节点一致 | 通过 | 通过 |
| 日志、录像连续帧、游戏进程音轨 | 通过 | 通过 |
| 全面单人/多人矩阵 | 未测：本次为专项验证 | 未测：本次为专项验证 |

两个版本分别通过 15 项运行断言，最终日志无 `[ERROR]`/`ERROR:`。实测录像捕获约 29 fps，FFmpeg 未检出超过 1 秒的冻结；音轨真实非静音。汇总见 [verification.json](verification.json)。额外未测：联机、断线/存档恢复、死亡路径、极端尺寸与其他常驻能力组合；相应清理和恢复路径已实现，不能以静态检查代替这些实测。

## Deliverables

- [20 秒双版本实机对照视频](position-zero-showcase.mp4)：1280×720 / 30 fps，实际游戏画面，保留游戏进程音频并增益 8 dB，无外加音频。
- [imagegen 演出预览](preview.png)、[完整提示词](imagegen-prompt.txt)、[来源与分镜](references.md)。使用内置 imagegen，预览不作为运行纹理。
- 原始录像、逐动作 JSON、前后截图与日志在工作树 `artifacts/position-zero-delivery/`；这些是本地证据，不加入安装包。

开发中修正了录制夹具进入战斗过早的竞态、等待 Power 实际生效的时序，以及两版同时录像造成的截图内存压力；最终记录来自依次运行的完整专项。画面修正包括避免原始后景遮挡、T 与血条重叠、叠光冲白。

工作期间 `main` 已由其他任务推进到 `b222b16`，另出现一份未跟踪文档。该文档未触碰，内容 SHA-256 已在本地安全检查中记录。任务先提交自身改动，再在此工作树同步最新本地 `main`；合并回原工作树仍须另行确认。

同步结果：已无冲突合入本地 `main@b222b16`，同步后正式版与测试版 ExportRelease 编译均通过（各 25 条现有警告，0 错误），`git diff --check` 通过。此次两个运行代码文件与任务提交 `9c9f4fa` 完全一致；上述实机录像和 30 项运行断言来自该任务提交的同一份实现，同步后没有重复完整专项或完整矩阵。原始主目录仍在 `main@b222b16`，未跟踪文档内容校验一致。任务分支和工作树保留，尚未合并回主目录。

## Baseline

- Original checkout: repository root; branch `main`; revision `c9ca472cc599eca1fc92da8c0c1fdd3736fb01c4`.
- Original worktree was clean; no active Git operation.
- Task checkout: `../worktrees/STS2_ShoujoKagekiAijoKaren/position-zero-vfx`; branch `codex/position-zero-vfx`.
