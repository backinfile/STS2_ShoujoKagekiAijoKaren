# 约定牌堆与闪耀耗尽牌堆查看特效

## Original user input

> $worktree-task 为查看约定牌堆和闪耀耗尽牌堆制作特效，不要太华丽，读取 D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/docs/19.创意工坊特效Mod解包与制作流程.md 你先搜搜相关官方和周边参考资料，然后用imagegen生成特效表现预览图，然后参考这个预览图开始实现，最后录制效果成视频给我看看

## Brief task plan

- Original checkout: repository root; integration target: `main` at `86abd1940cfbe517cfc9895849500076e21bdcfd`; original checkout clean.
- Task branch: `codex/karen-pile-view-vfx-20261015`; worktree: `../worktrees/karen-pile-view-vfx-20261015`.
- Read project VFX research and pile view code; research official Godot, Revue Starlight, and related mod references.
- Generate an imagegen concept preview, then implement restrained, view-owned pile opening/closing details using native rendering.
- Build both supported variants and run targeted MCP checks in isolated test environments; record actual gameplay video and save evidence.
- Consolidate task changes and documentation into one commit, synchronize local `main`, and request the skill-required explicit approval immediately before merge.

## Result and changes

实现、针对性实测与视频交付已完成；等待 `worktree-task` 要求的合并确认。原工作区 `main` 初始无未提交改动，集成前仍保持干净。任务分支和工作树保留供审阅。

### 行为与文件

- `src/ShoujoKagekiAijoKaren/GodotScripts/Node/NKarenPileViewVfx.cs`（含 Godot UID）：单个查看界面子 Control，约定显示红金细线和双星，耗尽显示空心星、断续短弧及开场下落余烬。两种开场均只有六颗小星点。
- `src/Core/PromisePileSystem/PromisePileManager.cs`、`src/Core/ShineSystem/ShinePileManager.cs`：分别在现有原生查看入口挂载装饰。
- 普通 0.64 秒、快速 0.32 秒；瞬时直接显示静态终态。开场结束停止 `_Process`，静态绘制由 Godot 缓存。仅响应自身尺寸变化，无全局订阅、Tween、独立任务或额外贴图。
- 装饰在原生 Background 之后、CardGrid 之前，忽略鼠标且不参与焦点，卡牌及原生返回按钮继续使用原有流程。原生界面关闭或替换时直接释放所属子树。
- 实机发现第一行卡牌高于概念图，最终将星标移至参考分辨率 y=112，位于顶栏与第一行之间；最终双版截图已人工查看。
- `tools/vfx/pile-view/`：临时测试指令、专项场景、证据核查与原速视频合成；通过 `tools/vfx/catalog/Showcase.csproj` 和 `record.py --scenes pile-view` 显式选择，不加入默认全览。
- `record.py --renderer opengl3` 提供隔离进程的低显存 Compatibility 选项，默认 Vulkan 保持不变。
- [参考与 imagegen 提示词规格](references.md)记录官方、工坊与 Godot 来源，以及概念到实现的对应关系。

### Verification

| 项目 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 完整双版本包构建 | 通过 | 通过 |
| 空堆、有牌查看 | 通过 | 通过 |
| 普通 / 快速 / 瞬时 | 通过 | 通过 |
| 原生返回、开场中关闭、连续开关、替换 | 通过 | 通过 |
| 原版牌堆无新增装饰 | 通过 | 通过 |
| 查看前后真实牌堆身份与顺序 | 一致 | 一致 |
| MCP 手牌模型/可见节点检查 | 24 次通过 | 24 次通过 |
| 查看节点释放检查 | 19 次通过 | 19 次通过 |
| 最终日志异常错误 | 0 | 0 |
| 窗口抢焦点/恢复显示样本 | 0 | 0 |
| 原生录像捕获速率 | 24.90 fps | 25.24 fps |

- 闪耀耗尽牌通过实际打出最后一点闪耀的《闪耀打击》产生；入堆前后保存截图，动画后核对 `karen_check_hand`。约定测试使用生成的真实牌堆模型。
- 初始 Vulkan 重启遇到显存分配失败；另有一次构建遇到系统内存不足，重试完成。最终双版专项录像使用 OpenGL 3 Compatibility，不能据此声称完成最终 Vulkan 实测。
- 测试发送的 1600×900、1100×800 设置请求没有改变隐藏 guard 下实际 1280×720 视口，**不计入尺寸适配实测**。原始 `results.json` 的旧 `resize` 标签仅代表设置请求和界面操作断言；最终 `verification.json` 已剔除此项并记录实际 PNG 尺寸。
- 未进行全面单人机制矩阵或多人测试；这是纯查看界面改动的双版本单人专项。`git diff --check` 通过。

### Deliverables

全部产物位于本任务工作树；本地录像、构建包和原始证据按项目惯例留在 Git 忽略目录。

- imagegen 概念图：`output/pile-view-vfx/concept.png`，内置工具生成；未作为游戏贴图使用。
- 双版实机展示：`output/pile-view-vfx/pile-view-showcase.mp4`，约 12.7 秒、1280×768、H.264/AAC、30 fps 编码；原游戏区域为 1280×720，字幕在下方附加区域，保留原速与原声。已从两个版本段提取成片帧人工检查。
- 录像含 MCP 补帧；实际捕获速率见表，不声称每个编码帧都是独立捕获。
- 完整证据：`artifacts/pile-view-vfx-20261015/{stable,beta}/pile-view/`，含空堆/有牌/三个速度的截图、动作及牌堆状态、手牌检查、完整原始录像和捕获构建哈希；各版日志位于上一级。
- 自动核查：`artifacts/pile-view-vfx-20261015/verification.json`；剪辑元数据：同目录 `showcase-metadata.json`。
- 最终包：`artifacts/ShoujoKagekiAijoKaren/`。
- 正式版 DLL SHA-256：`045f99d880f436221154ce2922d47c0a350cc40d536979921e594b599f6fa074`。
- 测试版 DLL SHA-256：`07ab5a4599da044ee6045a1a9470165c7510e6191edde39f5dd1cf18b87ee3a9`。
- 成片 SHA-256：`b2a566c00e890f98d659469554fd0160123087a6a06e23dda41d5d47f23be604`。

合并获批后应先将预览、成片和最终证据复制回原仓库的对应忽略目录，再清理本任务工作树，避免丢失本地媒体。
