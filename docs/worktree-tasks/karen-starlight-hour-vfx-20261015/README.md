# 《星光闪耀之时》特效

## Original user input

[$worktree-task](C:\Users\17575\\.codex\skills\worktree-task\SKILL.md) 为星光闪耀之时这张牌制作特效，读取 D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/docs/19.创意工坊特效Mod解包与制作流程.md 你先搜搜相关官方和周边参考资料，然后用imagegen生成特效表现预览图，然后参考这个预览图开始实现，最后录制效果成视频给我看看

### Follow-up user input（2026-10-15）

合并

## Brief task plan

- 原始 checkout：项目根目录；分支 `main`；HEAD `c0c51dc43fdadd744613d6e39350cb974a533c8c`；开始时工作区干净。
- 工作树：`../worktrees/karen-starlight-hour-vfx-20261015`；分支 `codex/karen-starlight-hour-vfx-20261015`。
- 阅读第 19 篇和 KarenStar 的真实自动出牌/耗尽/奖励流程，检索官方及授权周边参考，生成 imagegen 分镜。
- 按预览实现双星聚光、剩余闪耀星轨与逐次兑现，沿用原生游戏结算；处理加速、退场和并发归属。
- 在独立游戏副本做针对性双版本验证，MCP 截图与手牌节点核对，录制有声视频并检查成片。
- 更新记录，合为一个任务提交，同步原分支；展示视频和验证结果后请求合回确认。

## Result and changes

已完成官方/授权周边检索、内置 imagegen 四格预览、特效实现和约 19.62 秒有声实机视频。

- `KarenStar.cs`：原生命令顺序保留，加入短起势和作用域；逐次反馈读取真实 `BeforeCardPlayed`；奖励完成才确认。
- `NKarenStarVfx.cs`：大小四角星、双追光、红蓝细丝、有限星轨和实际卡牌落点；Fast/Instant、清场和退树清理；最多 12 个显示槽，规则次数不设上限。
- `ShoujoKagekiAijoKaren/audio/karen_star_*.wav`：三段原创新合成音效及 Godot 导入配置。
- `tools/vfx/star/`：可复现的音效生成、独立双版本夹具/验证、录像包装。
- [参考与预览](design.md)、[验证记录与限制](validation.md)、[机器可读结果](verification-summary.json)、[imagegen 提示词](imagegen-prompt.txt)、[预览图](preview.png)。

两版分别完成 11 类单人定向检查，涵盖 1/3/4/9/14 次、不同来源牌堆、重播叠加、速度、中断、不可打出和清场；核心规则/表现断言通过。测试版连续开发命令跳战斗有原生 NCard 重复回池诊断，未充分定位；清场夹具和一次 MCP 采集失败等原始记录均保留，未将日志描述为全绿。完整多人矩阵未测。视频检查有游戏进程音轨、非静音采样与连续画面，未后期添加特效。

任务代码、文档和预览已合为一个任务提交。用户于 2026-10-15 明确确认合并；集成目标为原 `main`，采用快进合并。集成前原工作区干净，视频及证据已另存会话交付目录；验证提交被 `main` 包含且两端树一致后，清理本次任务分支与工作树。
