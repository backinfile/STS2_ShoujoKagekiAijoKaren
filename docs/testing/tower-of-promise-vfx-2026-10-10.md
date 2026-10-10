# 约定之塔 A 方案与约定移牌拖拽预览

## 行为与范围

- 《约定之塔》拖住时：持续点亮两侧塔腿、上半塔轮廓和各层灯光，三组金色流光循环，观景台保持环光与星光。
- 打出时：沿塔腿向上蓄光，逐层点亮；取出的实际卡牌带细金色尾迹，抵达手牌位置时短暂描边闪光。
- 普通约定移牌牌拖住时：持续点亮塔腿与塔台边缘，在常亮灯带上循环流光。放入向塔台汇聚，取出向下流动，双向牌交替显示；亮度和覆盖范围低于专属塔牌，但明显区别于常驻灯光。
- 对应牌打出后，亮光保持到整张卡牌结算完成，覆盖选牌等待、重复打出、打出后 Hook 和结果牌堆迁移；取消拖拽或结算完成后淡出。选牌界面的原生暗幕仍会正常压暗背景。
- 点击塔身可打开约定牌堆，虚无模式打开对应抽牌堆。命中区域跟随塔的轮廓与缩放，透明角落不拦截点击；拖牌、选目标期间不会误开，查看时不改变牌堆。
- 原生鼠标拖拽与控制器选牌共用 `NPlayerHand.StartCardPlay` 和 `NCardPlay.Cleanup`。只是指针悬停在手牌上不会启动此预览。直接观察原生生命周期，避免短小的 tracker 通知方法被游戏预编译代码内联后绕过补丁。
- 普通牌包括直接移牌、生成到约定堆、打出后进入约定堆、通过选项执行的约定移牌；不包含能力牌、只按约定数量计算伤害的牌、只在被放入／移出时触发的牌。
- 《唤醒》只有升级后且非虚无模式提供约定取牌选项，因此只有此条件下显示预览。

覆盖表由 `PromiseTransferPreviews.ForCard` 集中维护，不依赖宽泛的 `PromisePileRelated` 标签或本地化文案。

| 类型 | 模型 |
| --- | --- |
| 放入 | KarenFall、KarenWhy、KarenOurPromise、KarenMeetAgain、KarenYesterglow、KarenNoHesitate、KarenStarGuide、KarenCourageStrike、KarenDodge、KarenLanding、KarenNewSituation、KarenNewDay、KarenRevueDuet、KarenRun、KarenStretching |
| 取出 | KarenPromiseDraw、KarenOnStage、KarenDance、KarenBridge、KarenWakeUp（上述条件） |
| 双向 | KarenWhosPromise、KarenExchangeFate、KarenRapid |
| 专属 | KarenTowerOfPromise |

## 实现约束

沿用原生移牌、牌堆、抽牌次序与手牌布局。特效节点独立挂在战斗 UI，观察 `NCard.Body` 的实际变换，不改卡牌位置、不重设父节点、不向池化卡牌添加子节点。到达手牌位置、卡牌身份改变、离树或战斗结束时清理；另有最长存活时间保护。

蓄光等待使用原生 Normal / Fast / Instant 速度设置。空牌堆或手满时仅给短星光反馈，不创建虚假的取牌尾迹。虚无模式跟随实际抽牌堆图标作为出牌位置。拖拽效果仅为本地预览；远端仍沿用原生手牌隐藏规则。

## 复测入口

以下入口按改动范围选择；完整 `full` 矩阵仅在玩家要求或推送创意工坊前执行，遵循 `AGENTS.md` 的测试范围规定。

```powershell
pwsh -File tools/build_dual_branch.ps1
pwsh -File tools/test_dev_matrix.ps1 -Suite full -UpdateMcp -LoopbackOnly
pwsh -File tools/test_promise_visuals.ps1 -Environment stable -Record
pwsh -File tools/test_promise_visuals.ps1 -Environment beta -Record
pwsh -File tools/test_tower_recall.ps1 -Environment stable
pwsh -File tools/test_tower_recall.ps1 -Environment beta
pwsh -File tools/test_tower_recall.ps1 -Environment stable -InteractionOnly
pwsh -File tools/test_tower_recall.ps1 -Environment beta -InteractionOnly
pwsh -File tools/test_tower_recall.ps1 -Environment stable -ResolutionOnly
pwsh -File tools/test_tower_recall.ps1 -Environment beta -ResolutionOnly
```

`test_tower_recall.ps1` 使用临时测试插件，通过 MCP 开发者命令调用原生 `NPlayerHand.StartCardPlay`、取消与提交路径。它验证每张覆盖牌与反例、取消清理、升级条件、正常／空来源／近满手牌／虚无取牌，保存截图、录像及每项断言。测试插件不进入发布包。此方式覆盖原生交互生命周期，不代表人工鼠标或手柄设备操作测试。

## 本次结果

环境：正式版 v0.107.1、测试版 v0.111.0；均从独立用户目录以 Karen 进入战斗。MCP 来自当前本地 `D:\Github\STS2_Mcp` 源码，未使用 Computer Use。所有输出保存在工作树的 `artifacts/`，未改动 Steam 或其他 Mod 项目的游戏目录。

| 项目 | 正式版 | 测试版 |
| --- | --- | --- |
| 双分支包构建 | 通过，0 错误 | 通过，0 错误 |
| 每张主动移牌牌拖拽、取消、能力牌及纯伤害反例、唤醒升级条件 | 55 项通过 | 55 项通过 |
| Normal：塔预览与 3 张取牌、空来源、近满手牌、虚无来源 | 8 项通过 | 8 项通过 |
| Fast：上述四组塔预览／取牌 | 8 项通过 | 8 项通过 |
| 《坠落》→《约定之塔》与池化卡牌复用专项 | 通过，防御计数 3→2→3 | 通过，防御计数 2→1→2 |
| 移牌截图与 `karen_check_hand` | 通过 | 通过 |
| full 矩阵（含单人及同版本联机） | 18 项通过 | 18 项通过 |
| 人工鼠标／手柄设备操作、Instant 模式 | 未测；测试夹具走原生交互生命周期 | 未测；测试夹具走原生交互生命周期 |

塔专项通过逐帧观察真实特效节点确认：正常及虚无各 3 张、近满手牌 1 张，全部到达目标位置；空来源 0 张；等待动画结束后活动节点与预览透明度均为 0。两版专项日志未出现异常。录像保留原生音频。

证据目录（相对本工作树）：

- 正式版首次完整拖拽：`artifacts/tower-recall/20261010-133816-stable/checks.json`。
- 测试版首次完整拖拽：`artifacts/tower-recall/20261010-133827-beta/checks.json`。
- Normal 塔专项：`artifacts/tower-recall/20261010-134025-stable/`、`artifacts/tower-recall/20261010-134036-beta/`。
- Fast 塔专项：`artifacts/tower-recall/20261010-134449-stable/`、`artifacts/tower-recall/20261010-134459-beta/`。
- 坠落专项：`artifacts/test-promise-visuals/20261010-134141-stable-77195e/`、`artifacts/test-promise-visuals/20261010-133212-beta-5a61d6/`。
- 完整矩阵：`artifacts/test-matrix/20261010-132834-72acdb/`。
- 同步后两版各 63 项全部通过：`artifacts/tower-recall/20261010-134712-stable/`、`artifacts/tower-recall/20261010-134723-beta/`。逐组查看了正常、近满手牌及虚无移牌前后截图，两版日志均无错误或异常。
- 同步后实机短片：`artifacts/tower-of-promise-vfx/tower-A-in-game.mp4`。

期间同步了本地 `main` 的 `d2d6981`（塔身常驻灯光等更新），同步后重新构建两版并执行完整拖拽／Normal 塔专项。矩阵启动较早，验证的是相同的取牌与移牌实现；最后的拖拽入口修正及新常驻灯光由同步后的专项覆盖。

完整矩阵最终为 **36 通过、0 失败**，含双 Karen、Karen 主机／原版客户端、原版主机／Karen 客户端、双原版对照，以及闪耀耗尽、额外重播、全局移牌、约定移牌和回合结束同步。跨版本联机不在本次范围内。

## 拖拽亮度与塔身点击反馈修订

旧版回放中，两种预览在拖住 1 秒、6 秒时都保持激活，但光点亮度经过多次衰减，细小尾迹被常驻灯光淹没。现在以常亮灯带为底，流光按拖拽开始时间连续循环，呼吸亮度下限保持在 84%；取消或整张卡牌结算完成后淡出。点击塔腿的旧版定点操作则未打开任何牌堆界面，已增加与绘制坐标一致的多边形 Control 命中区域，并复用角色点击查看流程。

可重复入口为 `tools/test_tower_recall.ps1 -InteractionOnly`（独立预览副本另传 `-GameDirectory` 和 `-McpPort`）。基线 `artifacts/tower-recall/20261010-141907-stable/` 在 `tower-opens-promise-pile` 断言失败；修订后正式版 `20261010-142604-stable`、测试版 `20261010-142629-beta` 均 **15 项通过**。覆盖持续拖拽、取消清理、拖后打出及三张牌实际抵达、手牌模型/节点一致、透明角落、拖牌期间点击、正常/空约定堆和虚无抽牌堆查看。截图已人工核对。点击由 Godot Viewport 输入路由注入，不直接调用查看方法；仍不等同于人工鼠标设备测试。

本次反馈修订仅执行构建与上述专项，未重跑全面矩阵；上节的 36 项结果对应此前版本。

随后同步本地 `main` 的 `7b951ab`，使预览及打出时的平台灯光复用新平台轮廓。最终包重新构建后，正式版 `artifacts/tower-recall/20261010-142954-stable/`、测试版 `artifacts/tower-recall/20261010-143004-beta/` 再次各 **15 项通过**，两端日志无错误或异常。查看了持续 6 秒时的预览、打出前后手牌以及点击打开牌堆的截图。

## 持续到结算完成与实机录像

用户复看后要求对应牌打出过程中继续发光。旧生命周期仅观察拖拽，`NCardPlay.Cleanup` 在异步结算结束前就清除了预览；基线 `artifacts/tower-recall/20261010-151317-stable/` 的《坠落》停在选牌界面时，`alpha=0`，`fall-lit-through-choice` 失败。

新实现给完整 `CardModel.OnPlayWrapper` 加独立可释放的视觉作用域。它等待原任务完成，并在 `finally` 释放；嵌套打牌各自持有作用域，战斗清理会清空全部作用域。未修改卡牌效果、选择结果或原生移牌逻辑。

复测命令使用 `-ResolutionOnly -GameDirectory artifacts/tower-of-promise-vfx/preview-stable/game -McpPort 15727 -ReuseFixture`，测试版对应 `preview-beta` 与 `15728`。`-ReuseFixture` 仅用于本任务预览副本中已保留的测试插件，复测后继续保留插件。最终两版各 **6 项通过**：选牌等待持续亮、结算结束淡出、坠落手牌一致、四张牌从塔实际抵达、塔后手牌一致、塔身点击查看。原生 Play 牌堆中的每帧透明度最小值均为 1；正式版坠落 205 帧、测试版 204 帧，塔均为 83 帧。两端日志无错误或异常，移牌前后截图已查看。

- 正式版：`artifacts/tower-recall/20261010-151546-stable/`。
- 测试版：`artifacts/tower-recall/20261010-151546-beta/`。
- 展示视频：正式版目录中的 `resolution-stable.mp4`，18.27 秒，1280×720、30 fps、H.264/AAC。已核对连续画面及原生游戏进程音轨，平均音量 -46 dB、峰值 -24.6 dB；未采集麦克风或其他进程音频。

本轮只重新构建双版本并执行结算专项，没有重跑完整矩阵。上述视频基于 `7b951ab`。用户批准合并后，分支同步到 `7256d6b`，纳入塔身流光延伸至塔尖及经过平台时的遮挡改动，再执行双版本构建与结算专项。

合并前最终验证：`artifacts/tower-recall/20261010-152334-stable/` 和 `artifacts/tower-recall/20261010-152334-beta/` 均 **6 项通过**，构建无错误，日志无错误或异常；已查看两版移牌前后与等待选牌截图。视频、测试证据及已验证的包在清理工作树前保留到主目录 `artifacts/`，其中包位于 `artifacts/tower-of-promise-vfx/review-package/`，不覆盖现有开发环境。
