# Karen 特效全览录制

[2026-10-16 main 共存专项](../../../docs/testing/vfx-coexistence-2026-10-16.md) 增加 `coexistence-regressions`：11 项实际 Godot 可见性／权重检查及 9 项真实音乐淡出中断检查；旧包会在祖先隐藏、祖先排队释放及旧音乐恢复回调上失败。`recent-stack-normal,recent-stack-void` 将 Position 0、激情、骄傲、嫉妒、背靠背、永不遗忘的闪耀、Why、唤醒和命运交换放在四种旧常驻表现上真实出牌，并在换房后检查注册清零。选牌和交换的 MCP 前后截图放在录制之外保存。当前全量入口清单见 [特效总览](../../../docs/21.特效总览与兼容约定.md)，历史 49 段合辑不代表包含这些近期演出。

同日完整矩阵后的接力专项捕获了 NPower 场景 Ready 但 Model 未绑定时的异常。`coexistence-regressions` 另含 `vfx_coexist power-anchor` 的十项原生节点检查，覆盖未绑定占位、精确模型、空目标、隐藏恢复、脱树与排队释放；激情、骄傲、星光及舞台图标共用安全锚点入口。最终包与初轮包的区别及实际复测范围见 [完整矩阵与边界记录](../../../docs/testing/full-matrix-vfx-boundaries-2026-10-16.md)。

[棒球专项](../../../docs/testing/baseball-vfx-2026-10-15.md) 使用显式 `--scenes baseball`：正常录像只录普通版／升级版，随后在录像外验证 Fast、Instant、缩放翻转、目标偏移旋转及击杀清理。`BaseballProbe` 观察真实命中回调、完整画布定位与节点退出；检查工具不随正式包发布。该专项未加入默认完整目录。

当前完整目录见 [2026-10-14 记录](../../../docs/testing/vfx-catalog-2026-10-14.md)：49 段，增加 Banana、门票 B、梦想、光辉、双幕 Starlight 和新旧组合。`record.py` 默认录全部当前场景；合成与核查使用 `--catalog current`，旧 32 段数据仍用默认 legacy。`verify_runtime.py --branch stable --full-catalog` 核查当前所有原片的最终包哈希、手牌、生命周期与真实重播，beta 不带 full 参数核查四组兼容专项。

[2026-10-15 兼容审查](../../../docs/testing/vfx-compatibility-review-2026-10-15.md) 补充音效总线路由、原生卡牌对象池和 8 组双版本专项。`verify_runtime.py --scenes <逗号分隔场景>` 可核查相应子集；`compatibility-regressions` 会调用 `vfx_review audio`／`card-cache`，旧包应明确失败。Showcase 编译中间目录已按 Variant 分开，避免 Stable／Beta 增量构建串用 DLL。像素级卡框检查只在 `revision_probe watch-border` 或 `shine-ambient*` 场景启用，避免无关场景重复读回大型图集。

通过 KarenSTS2MCP 录制真实游戏画面、游戏进程音频、选牌与移牌。运行环境保持后台渲染，监控是否抢占前台。不会使用 Computer Use。

前提：`artifacts/dev-game/<branch>/game` 快照与 BaseLib、当前 KarenSTS2MCP 已准备，`tools/build_dual_branch.ps1` 已产出双版本包，本机有 Python、.NET 9、ffmpeg 和 ffprobe。输入用户目录需要有默认用户 1 的 Karen 可继续单人存档；`--user-source` 可指定另一份这样的目录。准备阶段复制存档、Mod 和配置，只对只读游戏本体使用硬链接。端口为 18637 / 18638。

```powershell
pwsh -File tools/build_dual_branch.ps1
python tools/vfx/catalog/prepare.py stable --output artifacts/vfx-catalog-20261012
python -u tools/vfx/catalog/record.py stable --output artifacts/vfx-catalog-20261012
python tools/vfx/catalog/assemble.py --output artifacts/vfx-catalog-20261012
python tools/vfx/catalog/verify.py --output artifacts/vfx-catalog-20261012
```

已准备的目录不要再次运行 `prepare.py`；用 `record.py --scenes tower,normal-moves` 单独复录对应目录即可。`tools/launch_vfx_recording.ps1` 包装准备和录制步骤；`tools/record_gear_vfx.sh` 仅录齿轮两种模式。

在 worktree 中可用 `prepare.py --source-dev <主目录/artifacts/dev-game>` 读取游戏快照；`--port` 可覆盖测试端口，准备和录制必须传相同端口。必须先完成 Mod 构建再准备探针，两步不要并行。`record.py --package <包目录>` 可录制封存的基线包；`--speed fast` 验证原生快速模式。每批使用独立 `--output`，避免覆盖旧录像、日志或存档。

2026-10-13 优化专项使用 `before/stable`、`after/stable`、`after/beta`、`fast/stable`、`fast/beta` 五批证据：普通模式九组（guilt、fuel、replay、nonon、normal-star-guide、void-star-guide、allstack-normal、allstack-void、geer），快速模式三组（guilt、replay、nonon）。在共同父目录运行以下命令生成与核查对比片：

```powershell
python tools/vfx/catalog/compare.py --output artifacts/vfx-polish-20261013
python tools/vfx/catalog/verify_polish.py --output artifacts/vfx-polish-20261013
```

对比片按每次 MCP 动作标记对齐，左右保持 1× 原速，短片段末帧停留；默认保留右侧原声，齿轮第一次施放另提供两遍原声 A/B。核查脚本验证真实手牌节点、库存身份、伤害/降力量/回能/闪耀耗尽、基线与优化版最终牌堆及媒体哈希；日志错误单列，需人工审阅。

连续重播必须等原生三次 `CardPlaysFinished` 后再检查静止手牌；约定牌堆没有活动飞行不代表原生抽牌已经完成。`record.py --scenes replay-timing --speed fast` 逐步保存能量、真实打出历史、模型与可见手牌节点，可配合 `--package` 对照旧包。

`assemble.py` 的 `ENTRIES` 明确列出 32 段顺序与标题。输出下方增加 96 像素标题区，显示编号、标题、动作阶段和版本；保留原速、完整手牌画面与音频，并嵌入 MP4 章节。`manifest.json` 保存每段原始视频哈希和捕获数据，`chapters.txt` 是文本索引，`verification.json` 汇总媒体、手牌和音频检查。

状态相关的塔预览专项可分别运行：

```powershell
python -u tools/vfx/catalog/record.py stable --output artifacts/vfx-catalog-20261012 --scenes compatibility
python -u tools/vfx/catalog/record.py beta --output artifacts/vfx-catalog-20261012 --scenes compatibility
```

准备 beta 时需另运行 `prepare.py beta`，不要混用游戏 DLL 或用户目录。专项验证新的一天、普通／空虚取牌、升级条件、消耗／闪耀反例和结算期间灯光，保存操作前后截图并执行 `karen_check_hand`。预览不符会使脚本返回失败。

`ShowcaseCmd`、`guard/` 和引用的 `tools/promise-transform/` 仅作为临时测试 Mod，录制结束后删除部署副本。发布包不包含这些插件。原始录屏、截图、日志及隔离游戏均留在被 Git 忽略的 `artifacts/`。复录一批前先归档该批次的 `godot.log` 与 `window-monitor.json`，它们会在下一次会话结束时更新。

删牌准备默认等待 2.2 秒再换场，以免测试版尚未完成的原生删牌回池与新房间清理重叠。`--scenes setup-control --setup-wait 0` 可复现这一夹具时机对照，常规录制不要关闭等待。预加载期间的历史 `Invalid Task ID` 仍需记录，不以出牌检查通过替代日志审阅。

这套全览的实录环境与覆盖范围以 `docs/testing/vfx-catalog-2026-10-12.md` 为准，不代表执行过完整单人／多人测试矩阵。

塔退场与抽牌堆图标接替的专项：

```powershell
python -u tools/vfx/catalog/record.py stable --output artifacts/void-tower-handoff-20261012 --scenes void-tower-handoff
python -u tools/vfx/catalog/record.py beta --output artifacts/void-tower-handoff-20261012 --scenes void-tower-handoff
```

该专项单独录制正常转换、已有燃烧／光带的转换、空虚后点燃、下一场恢复四组场景；按实际节点归属核对图标层，检查塔淡出、隐藏点击区域、空库存、重复打出、取牌、塔桥和再生产。`void-tower-checks.json` 保存逐项结果，移牌操作额外保留 MCP 截图与 `karen_check_hand` 结果。准备方法和隔离环境前提同上；详见 `docs/testing/void-tower-handoff-2026-10-12.md`。

向下扫光、塔脚传光与图标重组的专项：

```powershell
python -u tools/vfx/catalog/record.py stable --output artifacts/void-card-beam-20261012 --scenes void-tower-timing
python -u tools/vfx/catalog/record.py beta --output artifacts/void-card-beam-20261012 --scenes void-tower-timing
python tools/vfx/catalog/verify_void_projection.py --output artifacts/void-card-beam-20261012 --branch stable
python tools/vfx/catalog/verify_void_projection.py --output artifacts/void-card-beam-20261012 --branch beta
python tools/vfx/catalog/assemble_void_timing.py --output artifacts/void-card-beam-20261012
```

该专项逐帧读取实际塔和图标透明度、卡牌星星位置和亮度、光束路径，分别录制约定牌堆有 3 张牌与无牌，检查扫描期间塔保持完整、扫描结束后淡出、图标先隐藏再重现，以及库存转换与手牌一致性。有牌时额外核对星星不会提前飞到图标，传递阶段保持可见并位于对应光束上且不超过光头；无牌时真实库存星星应始终为零。`void-tower-timing-checks.json` 保存时序和轨迹结果，`tower-timeline.json` 保存逐帧数据；导出两个独立的编号／标题视频及两段合辑，保留游戏原声与原速。首次运行前仍需按上述前提分别准备两版隔离环境。

`verify_void_projection.py` 从固定 1280×720 测试画面的塔脚光束区域、图标上方区域读取实际暖白像素，要求两个区域各至少 3 帧出现光线；无牌场景能避免用真实库存星星掩盖丢失的图标重组特效。也可用 `--video <原始录像>` 核查单段旧录像。区域与阈值仅用于这套固定布局，仍须查看连续帧和 MCP 前后截图；不能代替不同分辨率或角色位置的视觉审阅。
