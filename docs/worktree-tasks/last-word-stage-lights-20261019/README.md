# 最后的台词：顶部扇形舞台追光

## Original user input

```text
按1 修改看看
```

相关设计要求（原文）：

```text
最后的台词可用提示需要加大突出，因为符合条件的时候，手里只有这张牌了，你看看能不能额外设计点合适的特效，先用imagegen生成试试
不要，主要改为屏幕边缘特效吧，给出三份方案
T字换灯光（模拟舞台的感觉）
```

追加反馈（原文）：

```text
原来的T字还是要的
你现在这个光的颜色跟参考图不对
```

## Brief task plan

- 从捕获的 main（84ab49f）建立本任务隔离工作树，保留原工作区 74 个未提交/未跟踪文件状态；此前两个特效工作树不变。
- 实装选定的“顶部扇形追光”：上方两角投下粉白、玫红、绯红的舞台灯束，中央战场及 UI 保持清晰。
- 保留卡牌周围原有 T 粒子提示，并叠加屏幕边缘追光；仅在本地玩家《最后的台词》真实可用时显示，同屏只显示一套，失去条件、出牌和战斗结束时及时淡出或清理。
- 构建正式版、测试版，针对提示出现、持续、失效、重新出现及出牌清理进行 MCP 验证；录制游戏内效果供审阅，不运行全面测试矩阵。
- 整理为一个任务提交并同步本地 main，合回前依照 worktree-task 请求明确确认。

## Result and changes

已完成实现、两版针对性实测和校色后的对比视频；尚未合回 main，等待用户确认。

- 原牌边 T 字完整保留：原 t01–t04 纹理、生成间隔、粒子数量、缩放、颜色、速度、寿命与旋转参数不变。只补充本地玩家、可用条件失效、战斗结束及换房间时的回收。
- 新增屏幕两侧顶部三束追光。顶部粉白灯芯向下渐变为玫红，绯红雾光衬底；透明混色避免蓝色场景把光束叠成淡紫。缓慢摆动、轻微呼吸与少量光尘，中央区域透明，保护顶栏、能量球、结束回合按钮。
- 屏幕追光为单例，原生手牌刷新和悬停不叠加、不重播；淡入 0.48 秒、失效淡出 0.24 秒，战斗结束或切房间立即清理。追光只有一个绘制子节点。
- 卡牌规则、最后的台词影片与实际出牌 T 字演出、永不遗忘的闪耀均未更改；卡牌、影片和实际出牌特效与起始提交的来源哈希一致。

主要实现：
- src/Core/Patches/KarenLastWordPlayableVfxPatch.cs
- src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenLastWordPlayableVfx.cs
- src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenLastWordStageLightsVfx.cs
- materials/vfx/karen_last_word_stage_lights.gdshader

### 验证

正式版 v0.107.1、测试版 v0.111.0 均已在任务专属隔离游戏执行 KarenSTS2MCP 验证，端口分别为 22927 / 22928。未写共享开发游戏或 Steam 目录，未使用 Computer Use。两版均使用同步本地 main 后的最终生产包。

| 项目 | 正式版 | 测试版 |
| --- | --- | --- |
| 构建与资源导出 | 通过 | 通过 |
| 20 项提示状态：不满足、满足、抽牌/弃牌/手牌阻断与恢复、重复卡、移除重建、重复刷新、奖励、虚无模式、切房间 | 通过 | 通过 |
| T 字与追光同时显示，失效均回收、追光不叠加 | 通过 | 通过 |
| 原生悬停与真实出牌、终幕影片、999 伤害、奖励界面 | 通过 | 通过 |
| MCP 移牌前后截图及 25 次手牌模型/可见节点一致性检查 | 通过 | 通过 |
| 有声录像、采帧时钟审核、编码丢帧 0 | 通过 | 通过 |
| 原生 1920×1080 视口及 UI 遮罩 | 通过 | 通过 |
| 实际 4:3 视口 | 未测：守卫保留 16:9 视口 | 未测：守卫保留 16:9 视口 |
| 多人和全面矩阵 | 未测：本次只做相关专项 | 未测：本次只做相关专项 |

复核 81 张 MCP 截图，见 mcp-contact-01.jpg 至 mcp-contact-06.jpg。两版均未抢前台。正式版日志无 ERROR/Exception；测试版预加载阶段有 2 条已知 Invalid Task ID，均在 Common 资源预加载完成前，专项操作期间无后续异常。详细状态、采样与日志见 [verification.json](verification.json) 和 [mcp-evidence.tar.gz](mcp-evidence.tar.gz)。包哈希见 [build-files.json](build-files.json)。

### 视频

- [修改前后左右对比](LastWord-stage-lights-before-after.mp4)：10.13 秒，1920×668，左侧原 T 字、右侧原 T 字＋校色后追光。
- [修改后完整演示](LastWord-stage-lights-showcase.mp4)：23.46 秒，1280×784，含常驻提示、悬停、真实出牌和原终幕演出。
- 均为 1× 原速、保留完整游戏画面及游戏进程音频，说明条位于原画面之外；对比只截去较长一侧的尾部。完整解码和非静音音轨检查通过，对比无超过 1 秒的静帧；完整演示仅在末尾奖励界面有 1.33 秒静止。
- [方案参考](selected-concept.png)、[当前实机](stage-lights-in-game.png)、[对比帧](comparison-frame.png)、[视频阶段抽查](video-frame-check.jpg)、[成片元数据](videos.json)。

复现流程见 tools/vfx/last-word-stage-lights/README.md。测试插件只用于隔离游戏，不进入生产包。

### 分支与原工作区

起始目标：main，起始 HEAD：84ab49fca7128e9b0338669eea0f66163a7f6cb6。
工作树：../worktrees/STS2_ShoujoKagekiAijoKaren/last-word-stage-lights-20261019。
任务分支：codex/last-word-stage-lights-20261019。
任务实现提交：eb5eddb（随后同步最新本地 main 并在同步提交中保存最终验证和媒体）。

本任务未写原工作区。执行期间，其他工作完成提交，main 前进到 15fddc37b56514164982045cfac16ab8b4ff8b63；本任务已将该 main 合入任务分支。同步前后 main 的 HEAD 和剩余 4 个未跟踪文件的路径、状态、SHA256 完全一致，见 original-state-synchronization.json 与 original-state-final.json。没有远程操作。原工作区保留，任务工作树和分支保留，合回前按 worktree-task 等待明确确认。
