# 《命运的齿轮》速度模式专项

## 结论与改动

以 main `386aa5a` 为基线。Instant 原本会立即完成规则等待，却照常创建约1秒的施放齿轮和约0.62秒的回合触发齿轮；基线选牌画面可同时看到3个齿轮节点，机械短声也仍播放。播放至0.30秒切到 Instant 后，旧节点还残留约0.65秒。

本次保留透明分层齿轮、角色后方的绘制顺序与既有机械声，只修表现生命周期：

- 管理器在调用和延迟创建时都检查 Instant／非交互模式。
- 节点在入树及逐帧处理中检查同一条件，切换后隐藏、退出，并由既有 `_ExitTree` 清理所属声音。
- Normal／Fast 施放1秒、触发0.62秒保持；卡牌1秒与 Power 0.65秒的原生等待不变。游戏本体的 `Cmd.CustomScaledWait` 在 Instant 跳过等待；不以本机节点是否结束决定结算。
- 真实选牌、约定／空虚牌堆路由、能力叠层和自动出牌不变。未改图片、粒子、卡牌或 Power 规则。

生产文件：[管理器](../../src/Core/PromisePileSystem/Vfx/KarenGeerVfxManager.cs)、[齿轮节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenGeerVfx.cs)。

## 验证与证据

使用 KarenSTS2MCP，在任务私有的正式版 v0.107.1／测试版 v0.111.0 游戏及用户目录执行。只做相关单人专项，未运行全面单人／多人矩阵。临时 Probe 仅布置牌堆、观察节点／音效和切换原生速度偏好；施放、结束回合、选牌由真实 MCP 动作驱动，自动打出以原生 CardPlay 完成事件及实际扣血核对。

最终结果：修改后的两版各12项通过，各42次 `karen_check_hand` 核对一致；基线4项记录另有15次手牌核对。真实自动出牌与伤害核对通过，所有齿轮节点退出、所属声音计数归零，角色位置／视觉位置／Sprite偏移及Z序误差均为0。选牌完成后额外等待1.1秒弃牌尾动画，再保存最终截图和检查手牌；此等待只在测试工具内，不进入游戏规则。

| 场景 | 正式版 | 测试版 | 目标 |
| --- | --- | --- | --- |
| Normal／Fast／Instant 真实施放、下一回合选牌、自动打出 | 通过 | 通过 | 普通／快速各3节点，瞬时0节点；自动打击1次、伤害6 |
| 播放0.30秒切 Instant | 通过 | 通过 | 两层施放退出；声音无残留；能力仍应用 |
| 空虚模式三种速度 | 通过 | 通过 | 从真实抽牌堆选牌并自动打出；伤害6 |
| 空来源牌堆 | 通过 | 通过 | 仅施放2节点，无回合齿轮／选牌／自动出牌 |
| 两层能力 | 通过 | 通过 | 两次选择、两次自动打出、伤害12，合计6节点 |
| 延迟创建前切 Instant | 通过 | 通过 | 0节点、0声音 |
| VFX中断／切房 | 通过 | 通过 | 旧节点和所属声音退出，无手牌视觉残留 |

播放中切到 Instant 的退出延迟：基线约654毫秒，修改后正式版6毫秒、测试版5毫秒。Normal／Fast 选牌出现时齿轮可见计数为0；Instant 从创建到选择均为0节点，真实打击仍自动打出1次、造成6点伤害。两层能力为2次选择、2次自动打出与12点伤害。

构建双版本与 loader 均0错误；运行日志：基线／正式版无匹配异常，测试版在 Common 预载期有1次原生 `ERROR: Invalid Task ID`（`worker_thread_pool.cpp:418`），其后预载完成，12个实测场景完成。该条完整保留并限定为启动期已知诊断，未忽略场景期异常。未独立运行非交互模拟或同版本多人矩阵。

证据保存在[任务目录](../worktree-tasks/geer-speed-vfx-2026-10-17/README.md)：

- [28.4秒实机前后对比](../worktree-tasks/geer-speed-vfx-2026-10-17/evidence/Karen-Geer-speed-before-after.mp4)、[画面审核页](../worktree-tasks/geer-speed-vfx-2026-10-17/evidence/video-review.jpg)。
- [自动核验结果](../worktree-tasks/geer-speed-vfx-2026-10-17/evidence/verification.json)、[剪辑与包指纹](../worktree-tasks/geer-speed-vfx-2026-10-17/evidence/manifest.json)。
- [基线观测](../worktree-tasks/geer-speed-vfx-2026-10-17/evidence/before-stable-observations.json)、[正式版观测](../worktree-tasks/geer-speed-vfx-2026-10-17/evidence/after-stable-observations.json)、[测试版观测](../worktree-tasks/geer-speed-vfx-2026-10-17/evidence/after-beta-observations.json)包括场景状态、真实 CardPlay、全部99次手牌检查及窗口监视。MCP图集和完整运行日志位于同一目录，已逐项查看移牌前后图集。

录像按真实动作标记对齐，以1×播放；短侧只延长末帧，声音仅取修改后游戏进程。Normal／Fast 章节保留用于核对节拍，主要变化在 Instant 和中途切换章节。片中能量和牌堆由专项夹具布置，不代表正常开局。输出1920×660／30fps，实际抓帧约27.8～29.9fps，存在重复帧，MCP记录0丢帧；这不是游戏性能测量。

复现命令（在任务工作树，隔离环境须先准备）：

```powershell
python tools/vfx/catalog/record.py stable --output artifacts/geer-speed-20261017/before --scenes geer-speed-baseline --port 20817 --package artifacts/geer-speed-20261017/before-package
python tools/vfx/catalog/record.py stable --output artifacts/geer-speed-20261017/after --scenes geer-speed-after,geer-speed-checks --port 20817 --package artifacts/geer-speed-20261017/after-package
python tools/vfx/catalog/record.py beta --output artifacts/geer-speed-20261017/after --scenes geer-speed-after,geer-speed-checks --port 20818 --package artifacts/geer-speed-20261017/after-package
python tools/vfx/catalog/verify_geer_speed.py --output artifacts/geer-speed-20261017
python tools/vfx/catalog/compare_suitability.py --output artifacts/geer-speed-20261017 --geer-speed
```

原始包、完整动作、运行日志、MCP前后截图及录像已于2026-10-18归档到主检出 `artifacts/geer-speed-20261017/`（1234文件、约440.2MiB，逐文件SHA-256一致；排除可重建的隔离游戏／用户目录及目录链接），不可拿旧包重新录制覆盖已有证据。部署未写入 Steam 或共享开发环境。并行维护中的 main 未提交文件另行保留。

第一轮效果验证已通过；因图集发现原生出牌结束后弃牌飞行尚有尾段，补等尾动画后重录基线和两版最终录像。旧轮次保留在 `artifacts/geer-speed-20261017/before-first-pass`、`after-first-pass`、`comparison-first-pass`，未混入最终交付。初次临时Probe构建缺少命名空间，补齐后才录基线；生产包构建未失败。
