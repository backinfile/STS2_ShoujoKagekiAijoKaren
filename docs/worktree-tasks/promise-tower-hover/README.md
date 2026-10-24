# Promise tower hover

## Original user input

$worktree-task（本机参考：`<user-home>\\.codex\skills\worktree-task\SKILL.md`）  鼠标移动到约定牌堆的塔上时，鼠标不要变，同时需要展示约定牌堆tips

后续用户输入（原文）：

录制视频我看看效果

合并

## Brief task plan

- Starting checkout: repository root; target branch: `main`; starting revision: `3b4990147edc4d2f1ca151ae9b8e52c9753a94e1`.
- Task branch: `codex/promise-tower-hover`; worktree: `../worktrees/STS2_ShoujoKagekiAijoKaren/promise-tower-hover`.
- Preserve the four existing untracked files in the original checkout; they are excluded from this task.
- Inspect tower hover, cursor and PromisePile tooltip behavior; retain the current cursor and reuse the existing localized PromisePile tips.
- Build and perform focused verification. Consolidate the change and this record into one commit, synchronize with local `main`, then request explicit confirmation before merging back.

## Result and changes

已完成实现、针对性验证和效果录像；用户已明确确认合并到 `main`。

### 修改

- `src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenPromiseStarNode.Interaction.cs`：塔命中区域使用 `Arrow`，保留游戏原有自定义光标，不再切换为系统手形。
- 鼠标进入塔区域时，通过角色原生 `ShowHoverTips` 展示 `KarenPromisePilePower.HoverTips`，复用本地化说明、牌堆内容和当前模式提示。
- 悬停期间随战斗状态变化刷新；移开鼠标或塔节点离开场景时关闭提示，并解除状态订阅。保留原有点击查看牌堆的行为及原生提示屏蔽逻辑。

### 验证

| 验证项目 | stable v0.107.1 | beta v0.111.0 |
| --- | --- | --- |
| `ExportRelease` 编译，使用各自参考程序集 | 通过，0 错误、27 个既有警告 | 通过，0 错误、27 个既有警告 |
| 游戏内定向检查 | 13/13 通过 | 13/13 通过 |
| 悬停、移开、当前模式截图人工查看 | 通过 | 通过 |
| 完成测试的游戏日志检查 | 无 ERROR / Exception | 2 条既有启动 WorkerThreadPool Invalid Task ID；无意外运行错误 |

13 项检查包括：默认光标；通过视口注入鼠标移动，确认实际悬停控件为 `PromiseTowerHitbox`、光标仍为 `Arrow` 且显示约定牌堆提示；移开后提示和订阅清理；四次重复进入时各只有一个订阅；原生 targeting 提示屏蔽开关；悬停中进入燃烧模式并刷新提示；点击打开牌堆；能力移除后提示消失；新战斗中的提示；塔节点释放后提示和订阅清理。

验证仅使用 KarenSTS2MCP 和临时开发探针，在本工作树 `artifacts/tower-hover/` 内创建的游戏副本、独立用户数据和独立端口运行。没有修改主目录的开发环境或存档。验证脚本和探针属于忽略的临时产物，不进入正式 Mod。首次准备阶段遇到旧存档流程和等待条件问题，改用新存档并修正验证脚本后，两版均完整通过；上表仅采用完整通过的运行结果。

日志按 `tools/vfx/six-card/audit_log.py` 既有规则核查：测试版两条 WorkerThreadPool 诊断均在 Common 资源预载完成前，启动后和交互期间没有意外错误。`log-audit.json` 保留了诊断行号。

未运行全面单人/多人测试矩阵，本任务不作全面兼容性结论。验证证据位于 `artifacts/tower-hover/{stable,beta}/evidence/`，包含结果、动作、游戏日志及 MCP 截图。

### 效果录像

按用户后续请求录制正式版实机演示：`artifacts/tower-hover/stable/recording/promise-tower-hover.mp4`，13.3 秒、1280×720、30fps，包含游戏进程音轨。连续两次移入／移开约定之塔，展示提示出现／消失。

MCP 的视口录像不采集系统光标，因此临时开发探针用原生 `NCursorManager._lastSetCursor` 图片和实际输入位置显示录像光标；未改动正式 Mod。951 个采样帧的实际 `Input.GetCurrentCursorShape()` 全部为 `Arrow`。录像捕获约 29.96fps，399 个输出帧，1 个重复帧。已查看成片抽帧对照并核对 H.264、AAC 音轨、时长和非静音音量。

视频 SHA-256：`1d555ac5a50f2cd1a9b71db6877ba416a3bf3c1715436a96068c5ab90fa52efd`。视频另复制为聊天中的独立交付文件，避免后续清理工作树时丢失。

### 集成与原有工作保护

目标始终为任务开始时捕获的 `main`。用户已通过后续输入“合并”明确授权集成。集成方式为快进合并，校验提交树和原有文件后清理本次工作树及任务分支。

工作树清理前，已把两版验证证据、录像、探针源码和复现脚本复制到主目录 `artifacts/tower-hover/`，并核对证据文件 SHA-256 一致。独立游戏副本及临时用户数据不保留。

原主目录的四个未跟踪文件未纳入任务，并核对 SHA-256 保持原样：

- `docs/20.卡牌特效制作步骤提示词.md`
- `tools/clean_generated_artifacts.ps1`
- `tools/preview_cleanup.ps1`
- `tools/test_preview_cleanup.ps1`