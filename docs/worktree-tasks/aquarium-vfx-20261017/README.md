# 水族馆：特效制作

## Original user input

```text
可以，合并，然后下一个
```

上下文：用户确认上一张贯穿特效并要求继续下一张；按先前推荐顺序制作《水族馆》。上一任务已快进合入 main，试用和验证材料已归档。

后续反馈（本轮需落实）：

```text
水族馆的特效不要常驻
```

合回确认（2026-10-18）：

```text
合并
```

## Brief task plan

- 从本次开始时捕获的 main 建立 `../worktrees/STS2_ShoujoKagekiAijoKaren/aquarium-vfx-20261017`；起点与原有未跟踪状态见 `original-state.json`。
- 水族馆是 1 费能力，升级固有；每回合抽牌后选择手牌放入约定牌堆。按最新反馈取消常驻，只制作有限时长的施放水光、真实回合触发涟漪与选牌收束，不改变抽牌或入塔规则。
- 查看卡图和现有能力生命周期；制作接近实机的概念预览，以局部 Shader、水光和独立粒子实现。
- 在本工作树隔离双版本游戏进行针对性检查：施放／叠层、跨回合抽选牌、满手／空牌堆、空虚、Fast／Instant、移除／换场和组合层级；保存 MCP 前后截图、手牌核查和原速有声视频，不运行 full。
- 整理为一个任务提交，同步本地 main；完成可审阅结果后再请求本张合回确认。

## Result and changes

已按最新反馈取消常驻，用户于 2026-10-18 明确确认合回 `main`。当前视频：`aquarium-transient-synced-recordings.mp4`；本轮证据：`evidence/transient/`。旧 `aquarium-recordings.mp4` 与 `concept.png` 属于首版常驻方案，已被本轮取代。

- 删除水母和水面 Shader 的基础常驻亮度、循环气泡，以及进入房间自动恢复特效的 Hook。
- 水母／水光仅在施放与回合真实触发时短暂显示，约 1–1.5 秒淡出；真实抽牌的卡位水纹、实际移牌完成后的气泡收束同样限时播放。
- 等待原生选牌时，仅保留不可见的卡位观察作用域，所有水母、水光、水纹和粒子都会按时消失；完成／取消该作用域后回收全部特效节点。普通闲置时没有水族馆特效节点或附加显示层。
- 保留原生抽牌、选牌、入塔和空虚移牌规则，无额外演出等待。Fast 加速短暂演出，Instant 不创建特效。移除能力、切换战斗、手动清除演出时及时清理。
- 造型和两段水声沿用首版；本轮是生命周期修订，未重新生成图片。

### 本轮针对性验证

- 正式版 v0.107.1、测试版 v0.111.0 各 10 项：普通连续三个真实回合、叠层、空虚、Fast、Instant、空牌堆、选牌中清除演出、选牌时切换 Instant、选牌时移除能力、换战斗。
- 施放后检查能力仍存在且特效节点／附加层为 0；选牌界面保留约 3 秒，检查显示、发射及循环粒子数为 0；每次原生移牌完成后检查全部特效节点释放。验证不同触发之间能重新创建短暂特效，叠层不生成重复主体。
- MCP 保存并人工查看移牌前／后、施放到期和等待选牌截图；每次动画结束后执行 `karen_check_hand`。当前包哈希、结果、手牌核查、原始压缩日志与原速有声录像统计见本轮证据。
- 本轮共 20 项场景、48 次手牌核查通过；四份最终日志均无新增战斗错误。同步前修订版的 30.19 秒视频保留原速与游戏进程声音，实际采帧 29.71–29.82 fps、0 丢帧，非静音音轨平均 -41.1 至 -41.0 dB、峰值 -18.2 至 -17.2 dB。
- 双版本构建均 27 个既有警告、0 错误。未重新运行与本次生命周期无关的完整矩阵、多人或创意工坊发布。
- 本轮测试版场景检查启动时，在 Common 资源预加载完成前出现 6 条既有 `Invalid Task ID`，录像进程启动没有该提示；逐条按实际日志行号与战斗错误区分，不宣称已修复。

### 重跑本轮

复用本工作树的独立游戏与 MCP（正式版 20727、测试版 20728）；初次准备方式仍见下方历史流程。先运行 `tools/build_dual_branch.ps1`，然后：

```text
python tools/vfx/aquarium/verify.py stable --cases normal,stack,void,fast,instant,empty,cancel,switch,remove,room --run transient --capture --record ""
python tools/vfx/aquarium/verify.py beta --cases normal,stack,void,fast,instant,empty,cancel,switch,remove,room --run transient --capture --record ""
python tools/vfx/aquarium/verify.py stable --cases showcase --run transient-recordings --record showcase
python tools/vfx/aquarium/verify.py beta --cases showcase --run transient-recordings --record showcase
python tools/vfx/aquarium/package_video.py --run transient-recordings
python tools/vfx/aquarium/review_frames.py --run transient
python tools/vfx/aquarium/summarize.py --run transient
```

### 同步最新 main

本轮 20 项检查基于修订后水族馆源码完成。随后同步本地 main `386aa5a63b7ef19c08c85161967ca55a10eac3c7`，带入能量图标与约定之塔悬停说明；水族馆生产代码在同步中没有变化。水族馆生产代码与本轮实现提交 `d45a79ad5f07d61a6e7da90ab4b0e625ad34740e` 一致。

同步后重新导入资源和构建双版本包，分别补查真实施放、等待到期、原生抽牌、慢选牌与入塔收尾，并重新录制原速有声视频。同步前的 20 项完整针对性结果保留为原哈希证据，不将其冒充为新 PCK 的全量复测。最终组合包的动作、哈希与日志在 `evidence/transient-synced/`；双版本补查通过，4 次手牌核查通过，两份最终日志均无错误。当前视频 30.35 秒，实际采帧 29.18–29.89 fps、0 丢帧，非静音音轨平均 -41.3 至 -41.2 dB、峰值 -19.1 至 -15.4 dB。测试版首次录屏质量检查因丢 6 帧拒绝，重录后通过，保留首次统计于 `recording-retake/`。

### 首版历史实现与验证（常驻方案已取消）

- `KarenAquariumPower` 接入能力增加／叠层、回合开始、移除和战斗房间恢复；继续使用原生抽牌、选牌和移牌流程，没有增加演出等待。
- 新增 `NKarenAquariumVfx`：三只 Shader 水母与局部折光水面作为常驻主体；施放提亮，回合涟漪，真实抽到卡牌的局部水纹，以及实际完成移动后的气泡收束。独立 CPU 粒子负责气泡；水母不覆盖面部，不改卡牌材质、透明度或父节点。
- `PromisePileCmd.AddFromHand`、`PromisePileManager.AddFromPileAsync` 和空虚分支返回本次已完成移动的模型列表，原有调用仍可直接 await，目标牌堆及选牌规则保持原样。水族馆以该结果确认移动，不按能力层数虚构动画数量。
- 增加两段可复现合成水声与两个 Shader；来源取舍、imagegen 概念和完整提示词见 `references.md`、`concept.png`、`concept-prompt.txt`。
- 快速模式加速局部演出；瞬时模式不创建常驻效果。能力被移除、角色死亡、切换战斗、演出节点被清除时释放所拥有的后景与手牌层；选牌任务用 finally 结束作用域。
- 测试工具位于 `tools/vfx/aquarium/`，仅部署于本工作树隔离游戏，不进入正式 Mod 包；真实触发通过 MCP 打牌与结束回合，不手动调用水族馆回合 Hook。

### 验证与证据

- 起始基线与最终双版本包编译均为 27 个既有警告、0 错误；测试插件不进入生产构建。
- 正式版 v0.107.1、测试版 v0.111.0 各 14 项针对性场景：普通（连续三个真实回合）、叠层、满手、空牌堆、仅一张牌、空虚、Fast、Instant、移除、中途清除演出、选择时切换 Instant、换战斗、缩放翻转位移、燃烧＋过去与未来＋舞台正在等待着组合。另补查两版升级卡和四段录像中的原生触发。
- 实机 MCP 截图、前后状态、每回合 `karen_check_hand`、被动观测、构建哈希和压缩日志归档到 `evidence/`。截图已通过前后对照图人工查看，普通场景三个回合分别查看；手牌模型数、可见节点、残留牌堆图标及燃烧覆盖层分别核对。
- 首轮动作断言通过后，完整日志复核发现正式版连续强制跳战斗的原生抽牌异常；已保留原始日志并修正测试脚本等待房间实例改变和实际出牌阶段，详见 `evidence/incidents/README.md`。正式版全部 14 项复测通过，最终日志无该异常；四组最终日志审计均无新增战斗错误，详情见 `evidence/summary.json`。
- 测试版首次启动仍记录已有 `ERROR: Invalid Task ID`，位置在 Common 资源预加载完成前；与战斗错误分别记录，不宣称已修复。
- 交付 `aquarium-recordings.mp4`：四段原速、游戏进程有声实录，字幕在 1280×720 游戏视口之外。录像覆盖正式版施放／持续与回合触发、两层、空虚以及测试版同效果；四段实际采帧 29.29–29.85 fps、0 丢帧，音轨平均 -39.9 至 -39.4 dB、峰值 -19.4 至 -15.8 dB；补充静帧检测未发现超过 1 秒的近似静帧。共完成 72 次手牌节点核查。
- 本次没有执行 full、多人矩阵、创意工坊推送或存档中断恢复专项；视觉坐标与清理检查不能替代这些兼容性结论。


### 初次准备隔离游戏

工作树中运行 Godot `--headless --editor --import --path .`，再运行 `tools/build_dual_branch.ps1`。用 `tools/vfx/catalog/prepare.py stable` 和 `beta`，输出 `artifacts/aquarium`，源指向捕获的原始检出的 `artifacts/dev-game`，分别设置端口 20727／20728、`--skip-build`。MCP DLL 从上一任务已构建归档复制；重新构建 MCP 可使用 `tools/update_dev_mcp.ps1`，仅部署到本次隔离游戏。

### 集成状态

上一张贯穿特效已按用户明确确认合入 main（`3b4990147edc4d2f1ca151ae9b8e52c9753a94e1`），原工作树／分支已清理，本张使用独立工作树。原始 main 的 4 个未跟踪文件保留且不纳入本任务。本张实现与用户反馈整理为一个任务提交，随后合入最新本地 main 并完成同步验证；用户已于 2026-10-18 发送“合并”确认，采用快进合回。四个原有未跟踪文件已逐一对比 SHA-256，未纳入本提交。


### 合回与清理核对（2026-10-18）

- 合回前 main 在检查期间新增独立齿轮 Instant 模式修复 `cccf906998f771328410077693676c3ab47284a8`，已再次同步；水族馆的命令、能力、节点与 Shader 和已实测版本逐文件一致，双 API 集成编译各 27 个既有警告、0 错误。本次集成未追加完整矩阵或新的兼容性结论。
- main 当前另有 6 个已跟踪修改和 6 个未跟踪文件，与本任务合回路径没有重叠；记录在 `pre-merge-state.json`，合回后再次核对。最初 4 个未跟踪文件的内容哈希也保持一致。
- 原始实机材料、测试插件、MCP 与已验证包已归档到 `artifacts/vfx-archive/aquarium-vfx-20261017`，1420 个文件逐一比对哈希一致；概要见 `archive-summary.json`。包对应已验证视频的 main 386aa5a，归档用于重现验证，不代表额外部署。
- 确认 main 包含本任务、提交树一致、未提交文件未被覆盖后，清理本任务的 `aquarium-vfx-20261017` 工作树及 `codex/aquarium-vfx-20261017` 分支；其他工作树与分支保留。
