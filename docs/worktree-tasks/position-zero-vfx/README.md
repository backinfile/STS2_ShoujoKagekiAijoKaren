# Position 0 特效

## Original user input

[$worktree-task](C:\Users\17575\\.codex\skills\worktree-task\SKILL.md) 为Position 0这张牌制作特效，读取 D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/docs/19.创意工坊特效Mod解包与制作流程.md 你先搜搜相关官方和周边参考资料，然后用imagegen生成特效表现预览图，然后参考这个预览图开始实现，最后录制效果成视频给我看看

后续输入（原文）：

打出特效固定；触发获得格挡时，要求改为灯光特效（和打出的不一样）；当还有剩余的触发机会时（就是还有攻击牌次数），增加一个常驻特效和攻击牌的hover特效

## Brief task plan

- 沿用初版官方/授权来源与 imagegen 构图，固定原暖金打出时序。
- 将格挡确认改成冷白交叉扫灯；按真实剩余攻击机会控制常驻脚灯与攻击牌 hover。
- 分别使用隔离正式版/测试版和 KarenSTS2MCP 验证、截图、录像、检查音轨与清理。
- 保存一个修订提交，同步本地 main 后等待明确合并确认。

## Result and changes

已按后续要求完成，尚未合并回 `main`。

- **固定打出**：保持原暖金双聚光、金尘与起势；`DrawStage`、`Beam` 相对 `724dfa2` 完全一致。hover 和格挡确认不重启打出演出。
- **格挡触发**：移除敌人回流轨迹与护弧。实际格挡正增长后，播放约 0.76 秒的冷白交叉侧灯与地光短闪，区别于暖金打出。
- **剩余机会常驻**：原有层数减 `AttackCounter` 大于零、且所属玩家处于可出牌阶段时，保留低亮 T 与两侧轻呼吸脚灯。次数耗尽后熄灭；下回合原生计数重置后恢复。
- **攻击牌 hover**：只对本地真实攻击手牌显示，脚灯加强，卡框上缘增加小 T 与双灯光标。技能牌不显示。移开、打出、次数耗尽、回池/更换模型、能力移除和结束时清除；每个 Power 最多一份卡牌提示，不改原生高亮或输入。
- 普通/快速/瞬时沿用统一表现开关；瞬时模式无自定义常驻、hover 或触发节点。伤害、格挡命令和原有攻击计数规则保持不变。

## Verification

两版各通过 **27 项针对性实测断言**，完整证据见 [light-revision-verification.json](light-revision-verification.json)。

| 验证 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 双版本编译与打包、打出函数保持固定 | 通过 | 通过 |
| 正常 6 格挡确认、次数耗尽、下回合恢复 | 通过 | 通过 |
| 技能无 hover、攻击 hover/移开/重复不叠加 | 通过 | 通过 |
| 耗尽后攻击无提示、下回合 hover 恢复 | 通过 | 通过 |
| 叠加、多段、快速/瞬时、零伤害不误确认 | 通过 | 通过 |
| 带 hover 移除能力、战斗结束无残留 | 通过 | 通过 |
| 手牌模型/可见节点、日志、连续帧、游戏进程音轨 | 通过 | 通过 |
| 完整单人/多人矩阵 | 未测：本次为专项验证 | 未测：本次为专项验证 |

最终日志无 `[ERROR]`/`ERROR:`。录像捕获分别约 28.59/28.70 fps，FFmpeg 未检出超过 1 秒的冻结，音轨非静音；DLL/PCK SHA-256 与当前打包产物一致。录像与测试通过真实出牌、原生 hover、自然换回合运行，观察夹具不直接触发特效或修改攻击计数。重复 hover 检查按实际节点类型计数，避免 Godot 自动重命名导致漏数。

额外未实测：同版本联机、断线/存档恢复、死亡路径、极端尺寸与多种常驻能力组合。对应身份校验与清理路径已实现，不将静态检查作为这些实测的通过结论。

## Deliverables

- [新版 28 秒双版本对照视频](position-zero-light-revision.mp4)：1280×720、30 fps，包含固定打出、常驻、hover、触发灯及耗尽；保留游戏进程音轨并增益 8 dB，无外加音频。
- [最新来源与分镜](references.md)、[初版 imagegen 预览](preview.png)、[内置 imagegen 完整提示词](imagegen-prompt.txt)。初版预览中的回流护弧已按后续输入替换成灯光；预览不作为运行纹理。
- 保留 [初版视频](position-zero-showcase.mp4) 和 [初版验证](verification.json) 便于对照。
- 当前原始录像、逐动作 JSON、前后截图与日志：工作树 `artifacts/position-zero-light-final/`。工具入口：`tools/vfx/position-zero/README.md`。未部署 Steam 或共享开发环境。

## Integration state

任务分支 `codex/position-zero-vfx` 已包含此前初版任务提交 `9c9f4fa` 及同步提交 `724dfa2`，本次修订提交为 `f0454de`。随后已无冲突同步本地最新 `main@4f3cc84`（另一任务的 Back to Back 特效与记录）。同步后两版 ExportRelease 编译均通过（0 错误，各 26 条警告），本次四个运行代码文件与修订提交完全一致，未因无关主线更新重复完整实机专项。录像/27 项结果来自本次修订实现。原主目录的 HEAD 与未跟踪文档哈希保持一致，待用户明确确认后才能合并回原目录。任务分支和工作树保留。

## Baseline

- Original checkout: repository root; branch `main`; revision `c9ca472cc599eca1fc92da8c0c1fdd3736fb01c4`.
- Original checkout was clean; no active Git operation. A later untracked document from other work is preserved by hash.
- Task checkout: `../worktrees/STS2_ShoujoKagekiAijoKaren/position-zero-vfx`; branch `codex/position-zero-vfx`.
