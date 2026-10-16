# Position 0 特效

## Original user input

[$worktree-task](C:\Users\17575\\.codex\skills\worktree-task\SKILL.md) 为Position 0这张牌制作特效，读取 D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/docs/19.创意工坊特效Mod解包与制作流程.md 你先搜搜相关官方和周边参考资料，然后用imagegen生成特效表现预览图，然后参考这个预览图开始实现，最后录制效果成视频给我看看

后续输入（原文）：

打出特效固定；触发获得格挡时，要求改为灯光特效（和打出的不一样）；当还有剩余的触发机会时（就是还有攻击牌次数），增加一个常驻特效和攻击牌的hover特效

后续输入（原文）：

脚底为啥看起来像是有一道横线，有点怪，修改下

合并授权（原文）：

合并吧

## Brief task plan

- 沿用初版官方/授权来源与 imagegen 构图，固定原暖金打出时序。
- 将格挡确认改成冷白交叉扫灯；按真实剩余攻击机会控制常驻脚灯与攻击牌 hover。
- 分别使用隔离正式版/测试版和 KarenSTS2MCP 验证、截图、录像、检查音轨与清理。
- 保存修订提交，同步本地 main，按用户明确授权快进合并并清理任务工作树。

## Result and changes

已按后续要求完成；用户已明确授权合并回 `main`。

- **固定打出**：保持原暖金双聚光、金尘与起势；`DrawStage`、`Beam` 相对 `724dfa2` 完全一致。hover 和格挡确认不重启打出演出。按最新反馈去掉脚底地面 T，避免横杠像第二根血条。
- **格挡触发**：移除敌人回流轨迹与护弧。实际格挡正增长后，播放约 0.76 秒的冷白交叉侧灯与地光短闪，区别于暖金打出。
- **剩余机会常驻**：原有层数减 `AttackCounter` 大于零、且所属玩家处于可出牌阶段时，显示柔和地光与两侧轻呼吸脚灯，地面不再绘制硬横线/竖线。次数耗尽后熄灭；下回合原生计数重置后恢复。
- **攻击牌 hover**：只对本地真实攻击手牌显示，脚灯加强，卡框上缘增加小 T 与双灯光标。技能牌不显示。移开、打出、次数耗尽、回池/更换模型、能力移除和结束时清除；每个 Power 最多一份卡牌提示，不改原生高亮或输入。
- 普通/快速/瞬时沿用统一表现开关；瞬时模式无自定义常驻、hover 或触发节点。伤害、格挡命令和原有攻击计数规则保持不变。

## Verification

脚底修正只删除地面 `Tape` 线条及其绘制方法，保留双聚光、侧灯和 hover 的实现及规则。针对视觉反馈使用 `--normal-only` 在两版各复测 13 项（正常打出、技能/攻击 hover、重复/移开、触发、耗尽、自然换回合恢复、移除），重新检查前后 MCP 截图与录像。证据见 [soft-floor-verification.json](soft-floor-verification.json)。

上一轮机制修订两版各通过 **27 项针对性实测断言**，完整证据保留于 [light-revision-verification.json](light-revision-verification.json)，其覆盖范围如下；本次未重复叠加/多段、快速/瞬时、零伤害和带 hover 战斗结束等未改动部分。

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

各轮最终日志及录像指标分别保存在对应验证 JSON 中。录像与测试通过真实出牌、原生 hover、自然换回合运行，观察夹具不直接触发特效或修改攻击计数。重复 hover 检查按实际节点类型计数，避免 Godot 自动重命名导致漏数。

额外未实测：同版本联机、断线/存档恢复、死亡路径、极端尺寸与多种常驻能力组合。对应身份校验与清理路径已实现，不将静态检查作为这些实测的通过结论。

## Deliverables

- [最新：去掉脚底横线的 28 秒双版本视频](position-zero-soft-floor.mp4)：1280×720、30 fps，保留双聚光、常驻脚灯、hover、触发灯及耗尽；只有地面线条被去除。
- [上一轮灯光/hover 修订视频](position-zero-light-revision.mp4)：保留用于对照。录像保留游戏进程音轨并增益 8 dB，无外加音频。
- [最新来源与分镜](references.md)、[初版 imagegen 预览](preview.png)、[内置 imagegen 完整提示词](imagegen-prompt.txt)。初版预览中的回流护弧已按后续输入替换成灯光；预览不作为运行纹理。
- 保留 [初版视频](position-zero-showcase.mp4) 和 [初版验证](verification.json) 便于对照。
- 原始录像、逐动作 JSON、前后截图与日志已归档到主目录 `artifacts/position-zero-evidence-20261016/`，包含最新 `position-zero-soft-floor/`、上一轮 `position-zero-light-final/`、初版 `position-zero-delivery/`、构建日志及打包输出；269 个文件逐个核对 SHA256。隔离游戏副本为可重建测试环境，不归档。工具入口：`tools/vfx/position-zero/README.md`。未部署 Steam 或共享开发环境。

## Integration state

本轮去掉脚底硬线的任务提交为 `90012f9`。随后已无冲突同步本地最新 `main@05ea593`（另一任务的 Passion 特效及记录），同步后两版 ExportRelease 编译均通过（0 错误，各 26 条警告）。本次 Position 0 运行代码与任务提交完全一致，13 项实机验证和录像来自这份实现；未因无关主线更新重复完整专项。

此前初版/灯光修订提交及验证均保留。2026-10-16 用户以“合并吧”明确授权将最终修正并入捕获的原分支 `main`。合并前再次核对：主目录仍为 `main@05ea593`，无进行中的 Git 操作，任务分支包含该主线；原未跟踪文档 `docs/20.卡牌特效制作步骤提示词.md` 的 SHA256 仍为 `2396f33473560138071ed7f7d42315f16d98422ab9e8ba9a44ee405a686e6272`。集成采用 fast-forward；合并后核对两端提交树、任务提交包含关系及原文档哈希，再清理本任务工作树和分支。最终媒体与记录随 Git 保留，原始证据归档见上。

## Baseline

- Original checkout: repository root; branch `main`; revision `c9ca472cc599eca1fc92da8c0c1fdd3736fb01c4`.
- Original checkout was clean; no active Git operation. A later untracked document from other work is preserved by hash.
- Task checkout: `../worktrees/STS2_ShoujoKagekiAijoKaren/position-zero-vfx`; branch `codex/position-zero-vfx`.
