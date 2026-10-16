# 激情特效任务

## Original user input

[$worktree-task](C:\Users\17575\\.codex\skills\worktree-task\SKILL.md) 为激情这张牌制作特效，读取 D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/docs/19.创意工坊特效Mod解包与制作流程.md 你先搜搜相关官方和周边参考资料，然后用imagegen生成特效表现预览图，然后参考这个预览图开始实现，最后录制效果成视频给我看看

## Brief task plan

原始计划保留如下；2026-10-16 的重做以下方用户跟进为准。

- 起始分支 main，起始提交 bcfe75e9db3ffdd659a8334b0085c985e00e8be1，起始工作区干净。
- 隔离分支 codex/karen-passion-vfx-20261015，工作区 ../worktrees/karen-passion-vfx-20261015。
- 阅读第 19 章、卡牌、奖励命令和既有 VFX；搜索官方角色、第一话及官方周边。
- 内置 ImageGen 生成红缎折星、奖励封套、烫金封印、缩入真实 Power 位置的分镜。
- 在真实奖励登记之后接入程序化缎带与封套；快速模式限时，瞬时模式跳过；战斗结束清理。
- 隔离开发环境中实测并通过 KarenSTS2MCP 录制有声视频，复核画面、奖励和节点清理。
- 汇总为一条提交，同步 main，呈现结果后按技能要求征求合并确认。

## Follow-up user input (2026-10-16)

为 获得 “激情buff（战斗结束时获得一个额外遗物）”专门制作特效， 你重新为激情这个卡牌制作特效，不要复杂的信封动画

本轮重做计划：废弃信封/折缎大演出，改为真实 `KarenPassionPower` 正向增加时的约一秒短提示。放大一次已有 Buff 图标，配少量金红星屑，收向真实状态图标并显示本次增量。兼顾首次、叠层、非本卡来源、负向变化不触发、快速/瞬时和清理，并重新录制视频。复用本工作树，修改前已同步 main `0bddccc`；主工作区干净。

## Result and changes

2026-10-16 重做完成。**当前方案仅为获得/叠层激情 Buff 的约一秒反馈；信封、封套、折缎、烫印和对应旧音效均已移除。** 上一轮 ImageGen 预览及提示词保留为被替换方案的历史资料，不代表当前实装。

### 实现

- `KarenPassion.OnPlay` 恢复为只调用原有 `ExtraRewardCmd.AddRelicReward`。不改变费用、闪耀、随机数、奖励登记或战后发放。
- `KarenPassionPower.AfterPowerAmountChanged` 仅在 `power == this`、本次实际正向增加至少 1 层时播放。首次获得、再次叠层和其他来源增加均走真实 Power 钩子；减少自己或增加其他能力不触发。
- `NKarenPassionVfx` 在角色肩侧短暂显出已有 Buff 图标，配 12 枚稀疏金红星屑，沿短弧收向同一 Power 实例的真实图标，图标旁显示本次 `+N`。无新增道具、信封、全屏遮罩或规则等待。
- 普通单次约 1.05 秒，Fast 约 0.65 秒，Instant 跳过。每个实际 Power 仅持有一个节点，连续增量追加短脉冲，最多保留 3 枚；三次出牌仍登记三份奖励。
- Power 消失、死亡、战斗结束和节点退树时清理；只使用绝对显示尺寸与画布变换。图标纹理以独立 CacheMode.Ignore 实例持有并释放，避免跨战斗使用被游戏资源缓存释放的纹理。
- 新增一段 0.32 秒原创增益提示音及确定性生成脚本，删除旧丝带/印章音效。

### 实机专项验证

正式版 v0.107.1 与测试版 v0.111.0 各 9 项，共 18 项通过：

| 专项 | 正式版 | 测试版 |
| --- | --- | --- |
| 激情真实出牌首次获得、图标落点与释放 | 通过 | 通过 |
| 连续 3 张、Power/奖励各 +3、单节点合并 | 通过 | 通过 |
| Fast 约 0.65 秒 | 通过 | 通过 |
| Instant 不创建节点 | 通过 | 通过 |
| 演出中战斗提前结束清理 | 通过 | 通过 |
| 战后额外遗物奖励实际出现 | 通过 | 通过 |
| 原生 PowerCmd 单独增加 2 层，触发 +2 | 通过 | 通过 |
| 原生 PowerCmd 减少 1 层，不播放获得反馈 | 通过 | 通过 |
| 已有激情时获得力量，不误触发 | 通过 | 通过 |

使用复制的独立开发环境、用户数据和 Mods，MCP 端口 18767/18768；全部游戏操作通过 KarenSTS2MCP，未使用 Computer Use。Direct 测试只施加真实 Power，不代发遗物奖励，且不混入正常玩法展示。卡牌测试仍检查真实奖励预约数及当前遗物列表不变。

前后 MCP 截图/状态和 `karen_check_hand` 已保存；出牌结束模型/可见节点一致，无遗留牌堆图标。所有观察到的节点坐标有限、图标纹理有效、终点误差为零、最终释放；两版日志审计 `unexpectedErrors` 为空。当前版本双分支安装包构建和 Python 脚本编译检查通过，基线已有的 25 条编译警告不属于本次新增。

首轮重做发现缓存图标在跨战斗后被释放。改为特效独立持有纹理后，两版完整重跑通过，原错误日志保留于 `artifacts/passion-buff-review/first-attempt-texture.log`，未计作通过证据。

本次为单卡/单 Buff 专项，未运行完整单人/多人测试矩阵或联机测试，未部署主开发环境、Steam 或创意工坊。

### 交付

- 新视频：`artifacts/passion-buff-review/showcase/passion-showcase.mp4`，约 11 秒、1280×720、30 fps，实际游戏原速，含首次获得、连续叠层和战后遗物奖励。
- 游戏进程音频，经测得原始峰值后增益 13.3 dB，最终峰值 −12.1 dB；没有外部音乐或人声。已检查编码后关键连续帧、章节文字、真实 Buff 落点及奖励画面。
- 两版证据：`artifacts/passion-buff-review/stable/` 与 `beta/` 下的 `results.json`、`log-audit.json`、前后 PNG/状态、手牌检查、真实增量、节点寿命、奖励及构建哈希。
- 视频检查：`artifacts/passion-buff-review/showcase/validation.json`、`chapters.json`、`audio-check.txt`；首次过期纹理录制已被修复后实录替换，旧信封录像保留在另一历史目录 `artifacts/passion-review/`。
- 本地忽略产物不随 Git 克隆提供；合并清理前须归档新视频与验证证据。

### 集成状态

重做前已同步 main `0bddccc`。本轮提交和最终 main 同步正在收尾；原始集成目标仍为 main。用户尚未确认合并，保留任务分支与工作树。主工作区后来新增的未跟踪文件 `docs/20.卡牌特效制作步骤提示词.md` 保持未动，不进入本任务。
