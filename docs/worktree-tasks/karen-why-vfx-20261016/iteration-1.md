# 为什么要离开？我的朋友 — 特效任务

## Original user input

> [$worktree-task](C:\Users\17575\\.codex\skills\worktree-task\SKILL.md) 为“为什么离开，我的朋友”这张牌制作特效，读取 D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/docs/19.创意工坊特效Mod解包与制作流程.md 你先搜搜相关官方和周边参考资料，然后用imagegen生成特效表现预览图，然后参考这个预览图开始实现，最后录制效果成视频给我看看

## Brief task plan

1. 阅读项目制作流程、卡牌和约定牌堆接口；搜索官方及周边视觉参考并记录来源。
2. 使用内置 imagegen 生成表现预览图，按预览实现与真实格挡、移牌结果相连的特效。
3. 在隔离游戏进程通过 KarenSTS2MCP 做针对性验证，保留移牌前后截图与 karen_check_hand，录制视频并检查成片。
4. 汇总验证、提交一个任务 commit，与最新本地 main 同步；展示结果后按 worktree-task 要求请求合并确认。

## Result and changes

已完成实现、Imagegen 表现预览、双版本针对性单人实测和约 18 秒有声游戏录像。任务提交后同步最新本地 `main`，等待用户明确确认合并；工作树和任务分支保留。

- 设计与来源：[references-and-design.md](references-and-design.md)，完整生成提示词：[imagegen-prompt.txt](imagegen-prompt.txt)。
- 预览：[preview.png](preview.png)；成片：[showcase.mp4](showcase.mp4)；验证汇总：[verification.json](verification.json)。三者随任务提交保留。
- `src/Core/Models/Cards/ancient/KarenWhy.cs`：真实格挡结算后确认守护弧，保持原生选牌与移牌，`finally` 结束演出。
- `src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenWhyVfx.cs` 与 `materials/vfx/karen_why_window.gdshader`：夕阳窗格、双灯分离、守护弧、航路与选牌牵引；所有者归属、快速/瞬时模式及节点/音效清理。
- `PromisePileCmd.AddFromHand` / `PromisePileManager.AddFromPileAsync`：增加默认为空的只读选牌观察回调，同时透传虚空路由。不改变其他调用者的牌堆行为。
- `tools/vfx/why/`：隔离测试 fixture、MCP 原生打牌/选择、录制、视频封装和截图复核脚本。临时测试 DLL 不进入发布包。

### 验证结果

| 案例 | v0.107.1 | v0.111.0 |
| --- | --- | --- |
| 基础版：9 格挡、2 张牌 | 通过 | 通过 |
| 选择 0 张，不产生牵引或虚假入堆确认 | 通过 | 通过 |
| 升级版：12 格挡、3 张牌 | 通过 | 通过 |
| 快速模式 | 通过 | 通过 |
| 瞬时模式：跳过装饰、规则结算一致 | 通过 | 通过 |
| 虚空模式：2 张牌进入抽牌堆 | 通过 | 通过 |
| 无其他手牌 | 通过 | 通过 |
| 同玩家连续施放 | 通过 | 通过 |
| 切换战斗后的节点及图层清理 | 通过 | 通过 |

双版本构建通过（既有项目警告 25 条），测试 fixture 构建无警告。18 项均用原生 MCP 打牌与选择；操作前后保存并查看 MCP 截图，执行 `karen_check_hand`，模型/可见卡牌一致、无多余牌堆图标；观察器确认所有特效及独立图层退出。两版日志无 `ERROR`、`SCRIPT ERROR`、`SHADER ERROR` 或异常。

视频为 MCP 视口与游戏进程音频实录，只加画面外标题栏并拼接，未在后期补特效。内容包含正式版基础、升级版与测试版虚空模式；H.264/AAC，30fps，总长 17.854 秒。音频均值 -46.7dB、峰值 -22.5dB，非静音无削波；三段 4fps 抽样全部帧不同，未检出超过 1 秒的近似静帧。已检查连续帧拼图与前后截图。

详细原始证据位于工作树 `artifacts/why-vfx-20261016/`（被 Git 忽略），包括每例前后截图、状态、手牌验证、动作日志、两端 Godot 日志、构建哈希与原始录制。测试使用隔离副本和端口 19067/19068，仅终止本任务自己启动的进程，未部署到共享开发游戏或 Steam 目录。

本任务未做多人实测或完整测试矩阵，不能据此宣布全面多人兼容。最终验收仍需用户审阅表现并确认合并。

### 同步及交付状态

- 任务实现提交：`469d212`。
- 在工作树中合入最新本地 `main`：`b222b16d66a8f4e4345d9a591ec4501dc880461f`，保留上游牌堆查看特效；无冲突。
- 同步后重新构建两版安装包，并分别重跑基础版真实施放、移牌、前后截图、`karen_check_hand` 和清理检查，均通过；最终日志无运行异常。
- 同步复核曾暴露测试 fixture 共用 `obj` 导致串版引用的问题，已按版本隔离 restore/编译缓存；同时归档旧日志后再等待新进程 MCP 加载。失败日志保留在 `artifacts/why-vfx-synced-20261016/`，修复后的两版复核通过。变化仅在测试工具，不改变录制时的卡牌表现。
- 原目录仍在 `main`，HEAD 与本次同步目标相同。期间新增的未跟踪文件 `docs/20.卡牌特效制作步骤提示词.md` 正在被并行工作更新，最终检查发现其哈希相对同步前快照发生变化。本任务未写入该文件，保留外部编辑，不据旧哈希覆盖它；同步前状态保存在 `artifacts/why-vfx-20261016/original-before-integration.json`。合并前需重新核查主目录当前状态。
- 未执行合并回 `main`、共享游戏部署、推送或工作树清理；等待用户明确合并确认。

## Original checkout record

- Original branch: `main`
- Original HEAD: `c9ca472cc599eca1fc92da8c0c1fdd3736fb01c4`
- Original status: clean (`git status --short` empty)
- Worktree: `../worktrees/karen-why-vfx-20261016`
- Task branch: `codex/karen-why-vfx-20261016`

## Follow-up user input

> 完全不知所谓

> 你重做吧，现在的线条特效太简陋了

重做重点：用 Imagegen 绘制的透明美术、流动光幕/绸缎、光尘与选牌包裹替换细线主体。原版视觉已被用户否决，前文验收状态与录像只代表第一版；保留历史，停止合并，重新实机录制和审阅。
