# main 特效整理与共存验证：2026-10-16

基线为 main `782a71e`。这次按当前生效代码整理了 [53 组表现](../21.特效总览与兼容约定.md)，覆盖角色、约定／空虚、闪耀、专属攻击、增益、选牌、遗物、牌堆 UI、影片和音乐。partial 文件、辅助粒子和旧兼容壳类合并到各自的效果组；历史设计与旧实现不当作当前并存效果。

## 修正

### 祖先隐藏／释放后仍压暗其他效果

`KarenVfxComposition.EventWeight` 原先只检查注册节点自己的 `Visible` 和删除标记。注册节点仍显示、但其父容器隐藏或排队释放时，该节点会继续参与其他效果的压暗计算。

回归在真实游戏场景树内创建父容器和两个 cue，验证隐藏、重现、拆树、祖先排队释放、所属角色、全舞台作用域、最强值与自我排除。修改前 `hiddenAncestor`、`queuedAncestor` 失败。现在使用 `IsVisibleInTree()`，并沿父链检查排队释放；其余权重和规则保持原有定义。

实现：[KarenVfxComposition.cs](../../src/Core/PromisePileSystem/Vfx/KarenVfxComposition.cs)。

### 旧音乐淡出回调越过过场／新音乐／退出边界

旧 `StopAndRestoreGameMusic` 开始淡出时将 `_currentPlayer` 置空，后续 `StopImmediate` 无法再找到这个仍在播放的旧播放器。淡出完成仍无条件请求恢复原版音乐：过场可能被 BGM 打断，新旧音乐可能短暂叠加，退出也可能留下恢复任务。重复 Stop 和换房恢复兜底还可能在淡出尚未结束时请求恢复。

现在显式持有淡出播放器与 Tween，并将恢复请求代次绑定到淡出。新音乐、过场和退出杀旧 Tween、停止旧播放器、撤销旧回调；正常淡出结束后释放并恢复原版音乐；重复停止和恢复兜底在淡出期间等待原拥有者完成。

探针调用真实 `CombatBgmReplacementManager`、真实 `AudioStreamPlayer` 和 Tween，等待超过 `.35s` 淡出时间，检查播放器状态、恢复请求代次及待恢复标志。不是用伪造回调或源码字符串代替运行时验证。测试样本增益设为零，避免探针本身发声；正常演出录像另保留游戏进程音频。

修改前以下六项失败：`cutsceneStopsFadingSound`、`cutsceneKeepsMusicSuppressed`、`cutsceneRejectsStaleRestore`、`replacementStopsFadingSound`、`replacementRejectsStaleRestore`、`runExitRejectsStaleRestore`。

实现：[CombatBgmReplacementManager.cs](../../src/Core/Audio/CombatBgmReplacementManager.cs)。

## 工具与复跑

新增 [CoexistenceCmd](../../tools/vfx/catalog/CoexistenceCmd.cs)、[共存回归场景](../../tools/vfx/catalog/coexistence.py)、[近期特效叠加场景](../../tools/vfx/catalog/recent_stack.py)。它们只属于临时测试 Mod，正式项目排除 `tools/**` 编译。

先构建包，再顺序准备两版探针：

```powershell
pwsh -NoProfile -File tools/build_dual_branch.ps1
python tools/vfx/catalog/prepare.py stable --output artifacts/vfx-main-coexistence-20261016/after
python tools/vfx/catalog/prepare.py beta --output artifacts/vfx-main-coexistence-20261016/after
```

对全新复跑换一个 output，避免覆盖历史证据。每版依次运行；`<branch>` 取 `stable` 或 `beta`：

```powershell
python -u tools/vfx/catalog/record.py <branch> --output artifacts/vfx-main-coexistence-20261016/after --scenes coexistence-regressions,compatibility-regressions,recent-stack-normal,recent-stack-void,newstack-normal,newstack-void,stage-lifecycle,normal-moves,void-moves,lastword
python tools/vfx/catalog/verify_runtime.py --branch <branch> --output artifacts/vfx-main-coexistence-20261016/after --scenes coexistence-regressions,compatibility-regressions,recent-stack-normal,recent-stack-void,newstack-normal,newstack-void,stage-lifecycle,normal-moves,void-moves,lastword
```

本机 Python 的 WindowsApps 别名在沙箱内不可执行，实际使用 bundled runtime 的 Python 运行上述工具。MegaDot 导出和本机 MCP 访问使用获准的沙箱外执行；最初一次导出因编辑器目录／.NET 访问失败，首次沙箱内 MCP 尝试也受网络权限限制，这些均不作为兼容结论。失败记录保留于本次 artifacts。

红灯命令已实际运行：

```text
record.py stable --output artifacts/vfx-main-coexistence-20261016/repro --scenes coexistence-regressions
AssertionError: composition.hiddenAncestor, composition.queuedAncestor,
  music.cutsceneStopsFadingSound, music.cutsceneKeepsMusicSuppressed,
  music.cutsceneRejectsStaleRestore, music.replacementStopsFadingSound,
  music.replacementRejectsStaleRestore, music.runExitRejectsStaleRestore
```

## 结果

原始记录位于 `artifacts/vfx-main-coexistence-20261016/`，修复前为 `repro/`，修复后为 `after/`。每个实际录制保存最终 DLL／PCK 哈希，`runtime-verification.json` 核对实际运行包、手牌、注册清理、持有作用域及日志。

| 项目 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 新增可见性／权重回归 | 11 项通过 | 11 项通过 |
| 新增音乐所有权／中断回归 | 9 项通过 | 9 项通过 |
| 旧音效总线／卡牌池缓存回归 | 通过 | 通过 |
| 最近九类效果 × 四种常驻，普通／空虚 | 两组通过，真实出牌全部完成 | 两组通过，真实出牌全部完成 |
| 双幕 Starlight × 四种常驻，普通／空虚 | 两组通过，真实重播索引 `[0,1,2]` | 两组通过，真实重播索引 `[0,1,2]` |
| 取消／换房／Instant、普通／空虚移牌、台词过场 | 通过 | 通过 |
| 实际场景数 | 10 | 10 |
| 手牌模型与可见节点检查 | 101 次通过 | 101 次通过 |
| 注册、持有状态、背景及 HUD 清理 | 44 次通过 | 44 次通过 |
| 第二幕持有与作用域 | 4 次通过 | 4 次通过 |
| 日志 ERROR／异常 | 无 | 无 |
| 前台／窗口恢复监控 | 均为 0 | 均为 0 |

日志中的 `Asset not cached` 警告和调试战斗夹具的 `cannot find music path: event:/music/act1_b_boss_waterfall_giant` 另行保留。本轮音乐回归验证播放器归属及恢复调用时序，不据静音探针宣称已经主观试听原版 BGM 或完成音量混音验收。

两版构建均为 0 错误，保留既有警告。最终 20 段原始录像核对了实际 DLL／PCK 哈希；已查看两版 MCP 截图检查页各 5 张，以及主要攻击／第二幕运动采样页各 3 张。原始影片、状态、动作及核查 JSON 保留在 artifacts；本轮没有重新制作全效果合辑。

最终包已更新到本项目两套独立开发环境，各核对 5 个 Karen 文件；正式版 8 个、测试版 6 个其他 Mod 文件哈希保持一致。旧包位于本批 `deployment-backup/`，部署收据为 `deployment.json`。隔离测试进程均已退出，临时部署的测试 DLL／manifest 已移除；Steam 安装目录和其他 Mod 项目的游戏目录未改动。

近期叠加用例在燃烧、过去与未来、觉醒、再生产上依次真实打出 Position 0、激情、骄傲、嫉妒、背靠背、永不遗忘的闪耀，并完成 Why 手牌选择、唤醒选牌和命运交换；普通／空虚分别覆盖。Position 0 的真实 Power 存在时允许一个常驻 cue，临时演出结束后其他注册必须消失，换房后全部归零。选牌和交换的 MCP 截图放在录像外保存，动画结束逐次执行 `karen_check_hand`，同时核对模型身份和可见牌。

初轮近期组合的核查拒绝了缺少 `KAREN_EXCHANGE_FATE` 完成记录的结果：再生产可补出十张牌，丝流结束并不表示整次原生交换结束，而脚本已经换房。该轮证据保留在 `timing-baseline/stable/`。修正测试等待条件为真实 `CardPlaysFinished` 后，重新保存操作后截图、手牌结果并复跑这两个组合；没有为此修改游戏移牌规则。

上述专项阶段未运行全面单人／多人矩阵、真实多人传递场景及第三方工坊 Mod 组合。玩家随后要求完整矩阵及多次打出／跨玩家边界，追加执行情况见 [完整矩阵与边界复测](full-matrix-vfx-boundaries-2026-10-16.md)。选角影片、Splash 和商店／休息站场景完成入口审阅，没有重跑各自完整交互流程。历史 49 段合辑保持原历史含义，不冒充包含本次全部近期效果的新合辑。
