# 七件新增遗物验证与全面测试

> 历史记录：下列测试及91.6秒视频对应2026-10-11的小小星8格挡、水壶2回合。2026-10-12已改为小小星5点临时力量、水壶1回合，最新结果见[本次数值调整针对性验证](karen-seven-relics-balance-2026-10-12.md)。旧全面测试不能代替新数值的验证。

日期：2026-10-11。正式版 v0.107.1、测试版 v0.111.0。初始针对性单人验证使用工作树内独立的 mods、用户目录、日志及 MCP 端口 15657 / 15658。用户随后要求全面测试；基础矩阵 36 项、七遗物联机专项 6 项、双版本移牌画面专项 2 项，共 44 项全部通过。各测试使用隔离进程、用户目录及日志，测试完成后关闭本次创建的游戏进程。

## 已通过

下列项目在两版游戏中分别通过：

1. **遗物池、资源与文本**：池中九件遗物；新增普通 1、罕见 2、稀有 3、商店 1。七件图标、轮廓可加载，中文名称、描述变量及附加机制提示可解析；模型 ID 的序列化往返通过。
2. **饼干**：通过真实 `RelicCmd.Obtain` 获得后，牌组增加三张闪耀牌，普通、罕见、稀有各一张。
3. **星形发饰**：战斗开始时约定牌堆只有一张未升级《光辉》；下一场重新加入。
4. **水壶触发条件**：普通力量、临时力量降低不触发；首次正向临时力量触发保留力量 2 层；再次获得不叠加。回合 2 保留层数为 1，回合 3 为 0、原临时力量仍在；回合 4 临时力量已到期。之后再获得临时力量不触发水壶，下一场重置。
5. **小小星**：真实自动打出带一次额外重播的《约定之塔》，共获得 16 格挡；《光辉》从约定牌堆进入手牌，模型与节点一致。
6. **棒球猫**：两张同名《闪耀打击》耗尽只增加一个奖励；另一名称《水分补充》耗尽再增加一个；普通《光辉》消耗不计数。克隆计数集合互不影响，下一场战斗清零。
7. **结束战斗的耗尽**：将最后一个敌人设为 1 HP，通过原生自动打出闪耀为 1 的《闪耀打击》，击杀并耗尽后计数为 1，奖励列表出现一项 `special_card`，实际奖励界面可见。
8. **Mr. White**：第一回合结束、常规弃牌前出现选择；部分选择两张、零张和全选五张均通过。牌堆分别得到所选数量，未选手牌按正常回合流程处理；后续回合不重复弹出。
9. **星之王冠自禁用**：位于最右侧时禁用自身，当回合能量 4/3；下一回合不再触发，能量 3/3。战斗结束恢复；下一场重新触发。
10. **星之王冠禁用其他遗物**：王冠右侧放置草莓，首回合先禁用草莓并获得能量，下一回合禁用王冠自身并获得能量。
11. **移牌可视状态**：保存并查看《约定之塔》取回、Mr. White 部分选择及全选的操作前后截图；动画结束后 `karen_check_hand` 的 `missing`、`unexpected`、`unexpectedPileIcons` 均为空，手牌模型数与可见节点数相同。
12. **清单 HTML**：七行、七图；稀有筛选为三件，效果搜索和无结果状态正常，规则可展开。1440px 桌面和 390px 手机宽度无横向溢出，浏览器错误为零。

双版本发行程序集构建及共享 PCK 导出通过，C# 实现各 0 错误、24 条已有警告；初始基线也是 24 条警告。

测试版游戏日志无 `[ERROR]`、脚本错误或异常。正式版首次测试的辅助 Mod 清单使用旧依赖格式，启动时有一条弃用错误日志；已改成带最低版本的依赖对象。正式版游戏行为测试未出现运行异常。首次完成击杀的控制台夹具漏掉动作层的 `CheckWinCondition`，已通过原生命令补结算并验证奖励，夹具随后补齐该调用；没有因此修改遗物行为。

## 证据与复现

### 全面测试结果

| 范围 | 结果 | 原始报告 |
| --- | --- | --- |
| 基础 `full` 矩阵：双版本启动、单人、同版本四种主客角色组合、闪耀耗尽、全局移牌、约定牌堆、回合结束转换 | 36/36 通过 | `artifacts/test-matrix/20261011-044908-5c6948/report.json` |
| 七遗物联机：两版分别 Karen/Karen、Karen/原版、原版/Karen，共八个 Karen 玩家 | 6/6 通过 | `artifacts/test-matrix/seven-relics-20261011-051458/report.json` |
| 正式版《坠落》→《约定之塔》→取回牌继续打出 | 通过 | `artifacts/test-promise-visuals/20261011-052407-stable-fea3a9/report.json` |
| 测试版相同移牌画面专项 | 通过 | `artifacts/test-promise-visuals/20261011-052506-beta-3782e5/report.json` |

基础矩阵使用主机端口 15627 / 15628、客户端 15629 / 15630。客户端游戏副本保留供七遗物专项复用；每个用例仍使用自己的用户目录、日志和玩家标识。基础多人包含原版/原版对照；没有跨版本联机用例。

七遗物专项的卡牌通过 MCP 原生 `play_card` 打出，双方轮流触发。每个 Karen 玩家分别验证：饼干三种稀有度、开战一张未升级《光辉》、水壶四回合持续与每战一次、约定之塔带一次额外重播共 16 格挡、棒球猫同名基础/升级版去重与异名各一项奖励、Mr. White 首回合实际选择两张、王冠禁用自身当回合获得能量并于后续停止触发。双方均验证每次命令的实际玩家 NetId，比较手牌、牌组、能力、格挡、能量、遗物计数、约定牌堆和额外奖励，确认属于另一个玩家的状态没有被该次出牌改动。战斗结束时每个 Karen 获得两项闪耀牌奖励，原版角色没有这些奖励。

所有移牌用例保存操作前后截图并经人工查看；动画结束后逐端运行只读 `karen_check_hand`。新增多人专项及两版单人画面专项中模型数与可见卡牌节点数一致，`missing`、`unexpected`、`unexpectedPileIcons` 为空。双版本画面专项分别四次检查通过。星形发饰 V5 二维图标已在两版实际画面中核对。基础矩阵、七遗物专项、双版本移牌专项及录像合计 81 份最终游戏日志未发现 `[ERROR]`、脚本错误或未处理异常。

七遗物联机第一次尝试的测试脚本错误地把 `--clientId` 当成联机 NetId，实际遗物断言已通过但驱动没有匹配返回值；修正驱动为读取实际 `Player.NetId` 后重新完整运行。旧尝试保存在 `seven-relics-20261011-051216`，未计入最终通过结果，没有为该问题修改遗物实现。

```powershell
pwsh -NoProfile -File tools/test_dev_matrix.ps1 -Suite full -UpdateMcp -LoopbackOnly -KeepClientGames
pwsh -NoProfile -File tools/relic-regression/run-mp.ps1 -ClientGamesRoot artifacts/test-matrix/20261011-044908-5c6948
pwsh -NoProfile -File tools/test_promise_visuals.ps1 -Environment stable -ClientId 9700
pwsh -NoProfile -File tools/test_promise_visuals.ps1 -Environment beta -ClientId 9702
```

`-ClientGamesRoot` 应指向包含 `stable-client/game` 和 `beta-client/game` 的基础矩阵运行目录。辅助插件仅部署到这些隔离游戏，不进入发行包。

### 初始针对性验证

原始 MCP 响应、前后截图和日志在 `artifacts/relic-regression/stable/`、`artifacts/relic-regression/beta/`。测试夹具通过原生命令、卡牌播放与回合流程验证行为，不替换遗物实现。`setup-result.json`、`start-result.json`、`tower-result.json`、`cat-result.json`、`water2/3/4-result.json`、`reset-result.json`、`finisher-result.json` 记录断言结果；`white-none-result.json` 和 `white-all-result.json` 记录约定牌堆内容。

在准备好独立开发游戏后：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/build_dual_branch.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/prepare_relic_test_games.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/relic-regression/launch.ps1 -Branch stable
python tools/relic-regression/drive.py stable start
python tools/relic-regression/drive.py stable mechanics
python tools/relic-regression/drive.py stable turns
python tools/relic-regression/drive.py stable continue
python tools/relic-regression/drive.py stable finish
python tools/relic-regression/drive.py stable white-boundaries
```

测试版将 `stable` 替换为 `beta`。夹具会改动测试战局，应仅在独立目录的新存档中运行。每次从主菜单新开 Karen；重跑前使用新的隔离用户目录，或在测试游戏内放弃旧局。查看截图后检查各版日志，并关闭由启动记录标识的测试进程。

### 七段合并演示视频

正式版 v0.107.1 原生 MCP 录像，1280×720、30 FPS；完整保留游戏画面，在下方增加 80 像素中文说明与遗物图标，成片 1280×800、91.6 秒。七段依次为饼干、星形发饰、水壶、小小星、Mr. White、棒球猫、星之王冠，并写入七个章节。只有游戏进程音频，没有录制麦克风或其他应用；成片音轨非静音，峰值 -22.8 dB。录像后恢复原来的静音设置并关闭该隔离进程。

饼干段显示真实获得的普通／罕见／稀有牌预览；棒球猫段通过 MCP 原生出牌击杀最后一个敌人，在奖励界面显示新增闪耀牌；王冠段覆盖草莓被禁用、王冠自身被禁用、第三回合能量恢复为 3。选牌界面和各段触发后的画面已从视频抽帧查看，十一份 `karen_check_hand` 响应均匹配。原始片段、录制元数据、状态、字幕源及成片校验位于 `artifacts/relic-video/`；成片文件为 `Karen-seven-relics-effects.mp4`，校验为 `verification.json`。

```powershell
pwsh -NoProfile -File tools/relic-regression/launch.ps1 -Branch stable -UserRoot "$PWD/artifacts/relic-video/session" -ProfileId 9701
py tools/relic-regression/record.py start
# 按顺序分别运行 cookie、hairclip、water、star、white、cat、tiara。
py tools/relic-regression/assemble_video.py
```

录制工具使用临时夹具准备遗物和战局，实际出牌、选牌、回合结束及奖励仍走游戏原生流程；没有改动遗物生产代码。Windows 打包应用的原始录像可能落在被系统虚拟化的 `LocalApplicationData/KarenSTS2MCP/recordings`，工具只复制本次 MCP 返回的那个录像文件到任务产物目录。

## V6 图片风格与缩放复核

全面机制测试与上方合并视频完成后，用户要求对照本体统一七件图片的画风，并调整大小缩放。七张图稿已全部重绘为 V6；顶部栏图标和轮廓为85×85，详情图为独立256×256，分别按主体最长边79px与246px打包，保持原始纵横比。星形发饰继续保持二维。

| 版本 | 七件资源尺寸／加载 | 顶部栏及详情页 | 战斗手牌 | 日志 |
| --- | --- | --- | --- | --- |
| 正式版 v0.107.1 | 通过：small／outline为85×85，big为256×256 | 通过：七件与血瓶／金刚杵／项圈原版对照 | 通过：模型5／可见节点5，无异常节点 | 无运行错误 |
| 测试版 v0.111.0 | 同上，通过 | 同上，通过 | 通过：模型与可见节点一致，无异常节点 | 无运行错误 |

本次通过MCP在独立用户目录实测，每版保存地图顶部栏、七件详情页、三件原版详情对照及战斗顶部栏，共24张原生截图，逐张查看；路径为 `artifacts/relic-art-native-v6/{stable,beta}/`。测试辅助命令 `relic_art` 只用于打开游戏原生详情页和检查实际纹理，未进入发行包。资源改动后双版本发行构建成功，测试辅助Mod两版构建均0警告、0错误。[画风与缩放核对](../design/karen-seven-relics-style-v6.md)包含测量依据。

本次不改变七遗物行为，不重复此前44项全面机制矩阵。原合并演示视频保留V5外观，作为机制触发记录；V6阶段外观以该阶段原生截图为准。当前外观已更新为下文V7。

## V7 本地去摩尔纹与王冠造型复核

使用已有本地Mi-Ripple 0.1.0检查七张1254×1254母稿，关闭该工具的重生成。四张检出孤立周期分量的原稿采用滤波；小小星默认滤波在轮廓窗口超限，降低到25%修正量后通过验证。饼干、星形发饰未检出可处理周期纹，像素保留。王冠用内置ImageGen参照TV第11话原画面重绘，恢复细冠架、小白色饰件、两颗内卷环圆饰和下方小菱饰；新稿通过本地纹理检查。原始alpha、镂空和物件轮廓均保留，尺寸规则沿用85×85/256×256。

| 版本 | 资源与原生画面 | 战斗手牌 | 日志 |
| --- | --- | --- | --- |
| 正式版 v0.107.1 | 通过：七件small/outline/big、顶部栏、七件详情、三件本体对照 | 模型5/可见5，无异常节点 | 无运行错误 |
| 测试版 v0.111.0 | 同上，通过 | 模型5/可见5，无异常节点 | 无运行错误 |

再次通过MCP在独立ProfileId 9221/9222实测并查看24张新截图，产物位于 `artifacts/relic-art-native-v7/{stable,beta}/`；结束后恢复设置并关闭本任务两个进程。双版本发行包构建及测试辅助Mod构建通过。Godot导出退出时仍有既有的编辑器程序集重载告警，两版实际游戏均加载此发行包并通过上述验证。[V7图片复核](../design/karen-seven-relics-art-v7.md)保留动画原图对照和诊断、验收、滤波侧记及XML轨迹。

本次没有修改遗物机制，不重复全面矩阵，也不重录历史触发视频。当前外观以V7截图及[更新后的清单](../design/karen-seven-relics.html)为准。

## 未测与边界

- **断线恢复、多人特殊额外回合：未测。** 已完成常规同版本多人矩阵与七遗物专项；本次没有修改存档或断线恢复流程，不把这些常规测试当作恢复结论。
- **英文／日文完整界面：未测。** 两种语言的新增键与 JSON 格式已核对；实际游戏画面使用中文。
- **长期自然掉落频率、平衡性：未测。** 已检查遗物池登记、稀有度、实际获得流程，不据此推断长期分布。
- 星之王冠没有“禁用成功才给能量”的条件；现有规则下它自身也可被禁用，正常情况下不会形成“仍生效但没有可禁用遗物”的状态。未人工改动禁用白名单制造该边界。
