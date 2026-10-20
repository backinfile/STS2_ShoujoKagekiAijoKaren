# 特效版本选择与分支清理（2026-10-20）

按逐项对比后的选择整理 main，不整体合并旧细化分支：

| 部分 | 最终采用 |
| --- | --- |
| Position 0 卡顶标记 | 直接删除 `NKarenPositionZeroCardHoverVfx` 及其创建、持有、释放代码；悬停仍反馈到脚灯，保留施放暖光、次数与格挡冷光 |
| 燃烧手牌 | `72bee9c` 的 10/18 细化：火花初始数量与发射频率降低，烟雾减弱，减少卡顶发射 |
| Why | `72bee9c` 的 10/18 细化：保留入场冲击，0.65～1.70 秒间将暖色角部曝光降至 44%，流光继续 |
| 最后的台词 | 保留 main 的 10/20 实现，包括均匀发射、随机大小、漂移、舞台追光与余辉；相关 29 个已跟踪文件逐字节一致 |

燃烧与 Why 两个文件与 `72bee9c5569482e84c5a517625c38553e2dd2737` 完全一致；最后的台词以本轮开始前的 main `159b201feba61987e02bd8ee33f9bd24e3c5f18f` 为基准核对。入场音效移除、Form BGM 所有权修复和耗尽原地收拢均沿用当前 main。

## 针对性验证

双版本构建成功。正式版 v0.107.1 与测试版 v0.111.0 使用隔离目录及独立端口 18749／18750，经 KarenSTS2MCP 实测四段真实单人流程，未运行完整单人／多人矩阵。

`tools/vfx/catalog/refinement_scenes.py` 覆盖燃烧取牌后实际 overlay、临界闪耀共存、Position 0 技能与两种攻击牌悬停、真实格挡与冷光、次数耗尽、自然换回合恢复、悬停时移除能力、Why 原生选牌持有及确认移牌、最后的台词真实可用状态与悬停。观察器不直接创建或改写特效。旧 Position 0 专项及全览录制脚本同步改为要求卡顶标记数量为零。

复现前先构建最新双版本包；准备与录制必须依次执行，不在准备完成前启动录制。给两版分别准备新目录，再运行：

```powershell
python tools/vfx/catalog/record.py stable --output artifacts/vfx-accepted-refinement-20261020 --scenes refinement-burn,refinement-position,refinement-why,refinement-lastword --port 18749 --audit-capture
python tools/vfx/catalog/record.py beta --output artifacts/vfx-accepted-refinement-20261020 --scenes refinement-burn,refinement-position,refinement-why,refinement-lastword --port 18750 --audit-capture
```

两版各段移牌前后 MCP 截图、`karen_check_hand`、实际节点参数、录制包哈希、日志和采帧时钟检查保存在 `artifacts/vfx-accepted-refinement-20261020/`。`source-verification.json` 核对来源，`stable/runtime-verification.json` 与 `beta/runtime-verification.json` 核对真实状态、手牌、日志及包版本。

结果：两版各 17 次手牌模型／节点核查全部一致，各四项场景通过，日志无意外错误；采帧时钟 PTS 严格递增。正式版最低采帧率 28.74 fps、累计 1 个编码丢帧，测试版最低 28.32 fps、零丢帧。最终四段视频 53.02 秒，1280×816、30 fps、双声道；全片解码与实际音轨 Form 检查通过。两套开发环境均已安装并逐文件核对最终包哈希，见 `installed-verification.json`。仅观察 DLL 首次编译出现一次测试代码的 `Node.Visible` 类型错误，修正后重新编译通过；游戏实测没有该错误。

[四段最终效果视频](../../artifacts/vfx-accepted-refinement-20261020/videos/Karen-accepted-refinement-2026-10-20.mp4) 保留原速和真实游戏音量，下方逐段说明；`delivery-summary.json` 记录成片哈希、解码及 Form 音轨检查。历史预览与对比视频保持为当时版本的证据。

## 清理范围

清理 25 个本地分支：已合并的 `codex/*` 和旧 `worktree-gear-redesign`，以及已取代的 Banana、Ticket 设计、本次细化和两条仅测试／录制分支。每条删除的本地分支先保存为 `refs/codex-history/cleanup-20261020/<原分支名>`，保留旧实现及录制证据的可恢复入口。远端仅删除已合并的 `codex/drop-fuel-vfx` 与 `codex/last-word-vfx`。

保留仍有独立未合并功能的 `codex/eventremix-fit-tags`、`codex/karen-character-animation`、`codex/last-word-curtain-20261011`，以及 `main`、`dev`、`release/*`。清理计划、提交 SHA 和实际结果保存在交付目录的 `cleanup-plan.json`、`cleanup-result.json`；可用 `git branch <原分支名> refs/codex-history/cleanup-20261020/<原分支名>` 恢复。

Karen 与 MCP 仓库均只剩各自 main 的工作目录，没有额外注册的 worktree；项目约定的 worktree 目录为空。历史 `_task-records` 为文档证据目录，保留。
