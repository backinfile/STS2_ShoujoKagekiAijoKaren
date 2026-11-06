# Karen 七件新增遗物

2026-10-11 实装。[图文清单](karen-seven-relics.html)展示全部七件的图标、稀有度、效果与触发规则。保留现有两版初始发夹，角色遗物池共九件；禁用占位遗物不计入。

2026-10-22调整：**小小星为稀有，打出《约定之塔》获得3点临时力量；华恋的水壶保留临时力量1回合。** 星形发饰仍为罕见，开战加入《光辉》。

## 代码接入

七件遗物位于 `src/Core/Models/Relics/`，均加入 `KarenRelicPool.GenerateAllRelics()`，使用原生遗物稀有度与生成流程。`KarenBaseRelic` 只统一图标路径和本地化机制提示，不更改原有初始遗物。

- `KarenWaterBottleRelic`：监听正向临时力量增加，每战一次，应用现有 `KarenRetainTmpStrengthPower` 一层。普通力量与临时力量降低不会触发。临时力量跨过一次回合结束，再在下一次回合结束时消失。
- `KarenCookieRelic`：拾起时按普通、罕见、稀有三个桶分别抽取闪耀牌，各加入牌组一张。沿用现有闪耀奖励的可用卡池限制和 `Owner.PlayerRng.Rewards`。
- `KarenStarHairclipRelic`：`BeforeCombatStart` 在约定牌堆初始化后加入一张未升级的 `KarenWeKownToken`（《光辉》）。
- `KarenBaseballCatRelic`：通过 `IShineExhaustionListener` 接收实际闪耀耗尽通知；本场战斗按 `ModelId` 去重，升级版仍是同名。记录集合在克隆时独立复制，每战开始清空。沿用 `ExtraRewardCmd.AddShineCardReward` 发放奖励。
- `KarenMrWhiteRelic`：在拥有者第一回合的 `BeforeSideTurnEnd` 中调用现有可选手牌选择流程，范围为零到全部手牌，早于常规弃牌。约定牌堆的模式行为沿用原命令。
- `KarenLittleStarRelic`：每次实际打出《约定之塔》后获得3点临时力量；升级版及额外重播同样触发。`KarenLittleStarTempStrengthPower` 继承原生临时力量，以小小星为来源；水壶可保留此效果。
- `KarenStarTiaraRelic`：拥有者每回合开始时先调用现有禁用命令，再获得 1 能量；不以成功禁用数量作为能量条件。禁用自身后，本次结算完成，之后直到战后恢复都不再触发。

`ShinePileManager.MoveToShinePile` 保留进入前的闪耀身份，在耗尽卡牌的自身效果之后、移出战斗之前通知拥有者当前持有的遗物。不能用 `!CombatManager.IsEnding` 过滤遗物通知，否则可能漏掉结束战斗的一次耗尽。普通消耗不会进入此通知流程。

## 素材与文本

七件遗物按用户反馈统一采用本体手绘色面。当前皇冠为V8，其余六件沿用V7。**星之皇冠**保留原作六个卷环、八处白色饰件及纵向粉色宝石，收窄两侧、提高主体高度，并增强金色线条、深色轮廓和手绘色块；改善顶部栏缩小后的辨识度。名称和描述同步调整，去掉“然后”。[V8皇冠改图记录](karen-star-crown-art-v8.md)保留前后对照和游戏截图。

[素材记录](karen-seven-relics-assets.json)列出原作来源、当前母稿SHA-256、提示词及实际主体范围。[当前生成清单](assets/relic-style-v8/generation-manifest.json)引用V8皇冠和V7其余六张母稿；V6、V7历史稿另行保留。运行`python tools/prepare_relic_assets.py`可复现游戏资源。[V7图片复核](karen-seven-relics-art-v7.md)保留此前原作对照和本地滤波验收证据。

按本体 `relic_atlas` 的实际尺寸分别打包：顶部栏小图与白色高亮轮廓为 **85×85**，主体最长边 **79px**；独立详情大图为 **256×256**，主体最长边 **246px**。保持纵横比、裁掉透明空白并按 alpha 重心微调，避免旧版 216/256 留白使图标偏小。`KarenBaseRelic.BigIconPath` 单独使用 `_big.png`，不再将小图放大。[画风与缩放核对](karen-seven-relics-style-v6.md)记录测量与实测。

中、英、日文各增加 title / description / flavor；机制提示复用已有关键词本地化。《光辉》和《约定之塔》名称与现有卡牌一致。

## 验证

2026-10-22：小小星收益由5点改为3点；本次完成双版本编译及三语动态变量静态核对，未做实机复测。下方2026-10-12实测记录对应旧5点收益。

2026-10-12机制调整后，只进行用户要求的针对性验证：两版各14项机制断言通过，检查小小星普通／升级／重播触发、临时力量到期及水壶1回合联动、每战一次和下场重置；38张截图已查看，手牌模型与节点核查通过。未重新运行全面或多人矩阵。[本次数值调整验证](../testing/karen-seven-relics-balance-2026-10-12.md)与下列旧数值的历史结果分开记录。

V6 资源调整前，正式版 v0.107.1 和测试版 v0.111.0 均完成七件遗物单人实测，以及用户要求的全面测试：基础双版本单人／同版本多人矩阵 36 项、七遗物联机专项 6 项、双版本《坠落》→《约定之塔》画面专项 2 项，共 44 项通过。多人专项覆盖 Karen/Karen、Karen/原版、原版/Karen；基础矩阵另有原版/原版对照。移牌保存并查看前后截图，动画结束后执行 `karen_check_hand`。[逐项测试记录](../testing/karen-seven-relics-2026-10-11.md)包含实测结果和未测范围。V6 只调整图稿、资源尺寸及详情图路径，另在两版游戏完成顶部栏、七件详情页、原版对照和战斗画面的针对性实测，共保存24张原生截图；未重复全面矩阵。

V7造型与滤波修正后，再次完成两版原生顶部栏、七件详情、原版对照和战斗手牌针对性实测，24张新截图均已查看，日志没有运行错误。本次仍未改变机制或重复全面矩阵。

测试辅助 Mod 位于 `tools/relic-regression/`，不进入发行程序集或 PCK。独立测试环境只在工作树的 `artifacts/` 内，不部署到 Steam 安装目录或主工作区的开发游戏。
