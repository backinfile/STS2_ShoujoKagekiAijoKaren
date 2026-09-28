# 全卡牌特效清单（2026-09-28）

与 [分析报告](card-vfx-audit-2026-09-28.md) 配套。基线 `fab37b9`，共 115 个具体卡牌类；按当前代码调用链整理，并非逐卡实机验收。卡名使用当前中文本地化，类名用于区分同名选项。

“共用流程”指接入相应系统，实际视觉仍受本地玩家、模式、来源牌堆和播放速度限制；没有专属 VFX 不代表没有原生反馈。“闪耀：是”指本类初始化闪耀值，不涵盖游戏中后来添加的闪耀。Power 列保留实现类名，具体持续演出与风险见分析报告。建议列为后续投入方向，不代表所有条目都有缺陷。

## 当前卡池：攻击牌（27）

| 卡牌 / 源码 | 当前表现接线 | 闪耀 | 分析建议 |
| --- | --- | --- | --- |
| [不再遥不可及](../../src/Core/Models/Cards/basic/KarenPromiseDraw.cs)<br>`KarenPromiseDraw` | 原生斩击+伤害；约定牌堆共用流程（Draw） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [闪耀打击](../../src/Core/Models/Cards/basic/KarenShineStrike.cs)<br>`KarenShineStrike` | 原生斩击+伤害 | 是 | 复用攻击家族，按轻/重/多段调整节奏 |
| [打击](../../src/Core/Models/Cards/basic/KarenStrike.cs)<br>`KarenStrike` | 原生斩击+伤害 | — | 复用攻击家族，按轻/重/多段调整节奏 |
| [旋转](../../src/Core/Models/Cards/globalMove/KarenSpin.cs)<br>`KarenSpin` | 原生斩击+伤害 | — | P3：伤害成长时补短促数值提示 |
| [以觉悟的名义](../../src/Core/Models/Cards/neutral/KarenConsciousness.cs)<br>`KarenConsciousness` | 原生斩击+伤害；格挡 | 是 | 复用攻击家族，按轻/重/多段调整节奏 |
| [最后的台词](../../src/Core/Models/Cards/neutral/KarenLastWord.cs)<br>`KarenLastWord` | 可打出牌框提示、视频/文字粒子与声音、伤害 | — | P1：发射源 owner；P2：Instant 与离场清理 |
| [背靠背](../../src/Core/Models/Cards/promisePile/KarenBackToBackCard.cs)<br>`KarenBackToBackCard` | 原生斩击+伤害 | — | P3：突出高伤害时的重击感 |
| [棒球](../../src/Core/Models/Cards/promisePile/KarenBaseball.cs)<br>`KarenBaseball` | 原生斩击+伤害；Power 应用/图标（RetainHandPower、KarenRetainEnergyPower、BlurPower、KarenRetainTmpStrengthPower） | — | P3：命中表现可更贴合棒球主题 |
| [勇气打击](../../src/Core/Models/Cards/promisePile/KarenCourageStrike.cs)<br>`KarenCourageStrike` | 原生斩击+伤害；约定牌堆共用流程（AddToken） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [舞动](../../src/Core/Models/Cards/promisePile/KarenDance.cs)<br>`KarenDance` | 原生斩击+伤害；约定牌堆共用流程（SelectedToHand） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [神圣星辰](../../src/Core/Models/Cards/promisePile/KarenHolyStar.cs)<br>`KarenHolyStar` | 原生斩击+伤害 | — | P3：入塔降费提示；两段攻击节奏 |
| [落地](../../src/Core/Models/Cards/promisePile/KarenLanding.cs)<br>`KarenLanding` | 原生斩击+伤害；约定牌堆共用流程（AddToken） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [RevueDuet](../../src/Core/Models/Cards/promisePile/KarenRevueDuet.cs)<br>`KarenRevueDuet` | 原生斩击+伤害；约定牌堆共用流程（Add） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [双人舞](../../src/Core/Models/Cards/promisePile/KarenWhosPromise.cs)<br>`KarenWhosPromise` | 原生斩击+伤害；约定牌堆共用流程（AddFromHand / Draw） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [昨日夜空](../../src/Core/Models/Cards/promisePile/KarenYesterglow.cs)<br>`KarenYesterglow` | 原生斩击+伤害；约定牌堆共用流程（AddFromDiscard） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [嫉妒](../../src/Core/Models/Cards/relic/KarenJealousy.cs)<br>`KarenJealousy` | 原生斩击+伤害；额外遗物奖励/标记 | 是 | 核对战后奖励与当前标记的可理解性 |
| [我们犯下的罪过](../../src/Core/Models/Cards/shine/KarenCarryingGuilt.cs)<br>`KarenCarryingGuilt` | 目标聚焦、舞台灯与声音；延后伤害 | — | P2：游戏时间等待、目标变形定位 |
| [蓄力打击](../../src/Core/Models/Cards/shine/KarenChargeStrike.cs)<br>`KarenChargeStrike` | 原生斩击+伤害；Power 应用/图标（KarenChargeStrikeStrengthDownPower） | 是 | 复用攻击家族，按轻/重/多段调整节奏 |
| [永无结束的命运舞台](../../src/Core/Models/Cards/shine/KarenContinue02.cs)<br>`KarenContinue02` | 原生斩击+伤害 | 是 | 复用攻击家族，按轻/重/多段调整节奏 |
| [出场](../../src/Core/Models/Cards/shine/KarenDebut.cs)<br>`KarenDebut` | 原生斩击+伤害 | 是 | 复用攻击家族，按轻/重/多段调整节奏 |
| [NONNON哒哟](../../src/Core/Models/Cards/shine/KarenNonon.cs)<br>`KarenNonon` | 大横斩+伤害；Power 应用/图标（KarenNononStrengthDownPower） | 是 | 已有大横斩差异，保留 |
| [续写](../../src/Core/Models/Cards/shine/KarenRewrite.cs)<br>`KarenRewrite` | 原生斩击+伤害；自动出牌 | 是 | 复用攻击家族，按轻/重/多段调整节奏 |
| [用你的闪耀贯穿我吧](../../src/Core/Models/Cards/shine/KarenShineStrikeBarrage.cs)<br>`KarenShineStrikeBarrage` | 原生斩击+伤害 | 是 | 复用攻击家族，按轻/重/多段调整节奏 |
| [永不遗忘的闪耀](../../src/Core/Models/Cards/shine/KarenShineTogether.cs)<br>`KarenShineTogether` | 原生斩击+伤害；额外闪耀牌奖励/标记 | 是 | 核对战后奖励与当前标记的可理解性 |
| [星星串起了我们的友谊](../../src/Core/Models/Cards/shine/KarenStarFriend.cs)<br>`KarenStarFriend` | 原生斩击+伤害；额外闪耀牌奖励/标记 | 是 | 核对战后奖励与当前标记的可理解性 |
| [耀眼的阳光](../../src/Core/Models/Cards/shine/KarenSunlight.cs)<br>`KarenSunlight` | 原生斩击+伤害；牌堆加入预览；奖励选择；选项/选牌及结果移牌 | 是 | 复用攻击家族，按轻/重/多段调整节奏 |
| [上挑](../../src/Core/Models/Cards/shine/KarenSwordUp.cs)<br>`KarenSwordUp` | 原生斩击+伤害；Power 应用/图标（WeakPower、VulnerablePower） | 是 | 复用攻击家族，按轻/重/多段调整节奏 |

## 当前卡池：技能牌（39）

| 卡牌 / 源码 | 当前表现接线 | 闪耀 | 分析建议 |
| --- | --- | --- | --- |
| [为什么要离开？我的朋友](../../src/Core/Models/Cards/ancient/KarenWhy.cs)<br>`KarenWhy` | 格挡；约定牌堆共用流程（AddFromHand） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [防御](../../src/Core/Models/Cards/basic/KarenDefend.cs)<br>`KarenDefend` | 格挡 | — | 原生反馈为主，按需补轻量主题提示 |
| [坠落](../../src/Core/Models/Cards/basic/KarenFall.cs)<br>`KarenFall` | 格挡；约定牌堆共用流程（AddFromHand） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [下一个舞台](../../src/Core/Models/Cards/globalMove/KarenNextStage.cs)<br>`KarenNextStage` | 抽牌 | — | 原生反馈为主，按需补轻量主题提示 |
| [唤醒](../../src/Core/Models/Cards/globalMove/KarenWakeUp.cs)<br>`KarenWakeUp` | 选项/选牌及结果移牌 | — | 原生反馈为主，按需补轻量主题提示 |
| [Banana蛋糕](../../src/Core/Models/Cards/neutral/KarenBananaCake.cs)<br>`KarenBananaCake` | 药水获取 | — | 原生反馈为主，按需补轻量主题提示 |
| [添麻烦了](../../src/Core/Models/Cards/neutral/KarenCry.cs)<br>`KarenCry` | 格挡 | — | 原生反馈为主，按需补轻量主题提示 |
| [约定的那天](../../src/Core/Models/Cards/neutral/KarenOldPlace.cs)<br>`KarenOldPlace` | 格挡；选项/选牌及结果移牌 | — | 原生反馈为主，按需补轻量主题提示 |
| [招架](../../src/Core/Models/Cards/neutral/KarenParry.cs)<br>`KarenParry` | 格挡 | — | P3：格挡成长时补短促数值提示 |
| [约定之塔桥](../../src/Core/Models/Cards/promisePile/KarenBridge.cs)<br>`KarenBridge` | 约定牌堆共用流程（AutoPlayFromPromisePile） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [闪避](../../src/Core/Models/Cards/promisePile/KarenDodge.cs)<br>`KarenDodge` | 格挡；约定牌堆共用流程（AddToken） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [命运交换之日](../../src/Core/Models/Cards/promisePile/KarenExchangeFate.cs)<br>`KarenExchangeFate` | 约定牌堆共用流程（SwitchHand） | — | 优先验证交换前后模型与可见牌一致 |
| [重逢](../../src/Core/Models/Cards/promisePile/KarenMeetAgain.cs)<br>`KarenMeetAgain` | 格挡；抽牌；约定牌堆共用流程（AddFromHand） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [新的一天](../../src/Core/Models/Cards/promisePile/KarenNewDay.cs)<br>`KarenNewDay` | 格挡；Power 应用/图标（KarenNewDayPower）；约定牌堆共用流程（Add） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [观察情况](../../src/Core/Models/Cards/promisePile/KarenNewSituation.cs)<br>`KarenNewSituation` | 格挡；约定牌堆共用流程（AddToken） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [不再犹豫](../../src/Core/Models/Cards/promisePile/KarenNoHesitate.cs)<br>`KarenNoHesitate` | 选项/选牌及结果移牌 | 是 | 原生反馈为主，按需补轻量主题提示 |
| [命运与本能](../../src/Core/Models/Cards/promisePile/KarenOnStage.cs)<br>`KarenOnStage` | 能量；约定牌堆共用流程（Draw） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [开拓星辰](../../src/Core/Models/Cards/promisePile/KarenOpenUpStars.cs)<br>`KarenOpenUpStars` | Power 应用/图标（WeakPower、VulnerablePower） | — | 原生反馈为主，按需补轻量主题提示 |
| [我们的约定](../../src/Core/Models/Cards/promisePile/KarenOurPromise.cs)<br>`KarenOurPromise` | 抽牌；约定牌堆共用流程（AddFromHand） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [极速下降](../../src/Core/Models/Cards/promisePile/KarenRapid.cs)<br>`KarenRapid` | 选项/选牌及结果移牌 | — | 原生反馈为主，按需补轻量主题提示 |
| [行动](../../src/Core/Models/Cards/promisePile/KarenRun.cs)<br>`KarenRun` | 抽牌；约定牌堆共用流程（Add） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [拉伸](../../src/Core/Models/Cards/promisePile/KarenStretching.cs)<br>`KarenStretching` | 格挡；约定牌堆共用流程（Add） | — | 复用塔演出；关注来源、落点和批量节奏 |
| [东京塔下](../../src/Core/Models/Cards/promisePile/KarenUnderTower.cs)<br>`KarenUnderTower` | 格挡；生成牌 | — | 原生反馈为主，按需补轻量主题提示 |
| [激情](../../src/Core/Models/Cards/relic/KarenPassion.cs)<br>`KarenPassion` | 额外遗物奖励/标记 | 是 | 核对战后奖励与当前标记的可理解性 |
| [摘星](../../src/Core/Models/Cards/relic/KarenPickStar.cs)<br>`KarenPickStar` | 能量；遗物目标星光预告+吸收 | — | 保留遗物预告与吸收，核对目标归属 |
| [星罪](../../src/Core/Models/Cards/relic/KarenStarCrime.cs)<br>`KarenStarCrime` | 抽牌；遗物目标星光预告+吸收 | — | 保留遗物预告与吸收，核对目标归属 |
| [水分补充](../../src/Core/Models/Cards/shine/KarenDrinkWater.cs)<br>`KarenDrinkWater` | Power 应用/图标（BufferPower） | 是 | 原生反馈为主，按需补轻量主题提示 |
| [投下燃料](../../src/Core/Models/Cards/shine/KarenDropFuel.cs)<br>`KarenDropFuel` | 燃料专属 VFX；抽牌、能量反馈 | 是 | 保留；核对重播和离场清理 |
| [歌唱吧舞蹈吧互相争斗吧](../../src/Core/Models/Cards/shine/KarenFightRelay.cs)<br>`KarenFightRelay` | 牌堆加入预览；额外遗物奖励/标记 | 是 | 已有牌堆加入预览；可补传出/接收者确认 |
| [土豆](../../src/Core/Models/Cards/shine/KarenPotato.cs)<br>`KarenPotato` | 治疗 | 是 | 原生反馈为主，按需补轻量主题提示 |
| [武术练习](../../src/Core/Models/Cards/shine/KarenPractice.cs)<br>`KarenPractice` | 格挡；治疗 | 是 | 原生反馈为主，按需补轻量主题提示 |
| [舞蹈练习](../../src/Core/Models/Cards/shine/KarenPractice2.cs)<br>`KarenPractice2` | 格挡 | — | 原生反馈为主，按需补轻量主题提示 |
| [体操练习](../../src/Core/Models/Cards/shine/KarenPractice3.cs)<br>`KarenPractice3` | 格挡 | 是 | 原生反馈为主，按需补轻量主题提示 |
| [准备完成](../../src/Core/Models/Cards/shine/KarenReady.cs)<br>`KarenReady` | 抽牌；Power 应用/图标（StrengthPower） | 是 | 原生反馈为主，按需补轻量主题提示 |
| [星光闪耀之时](../../src/Core/Models/Cards/shine/KarenStar.cs)<br>`KarenStar` | 自动出牌；额外闪耀牌奖励/标记 | — | 核对战后奖励与当前标记的可理解性 |
| [星光指引](../../src/Core/Models/Cards/shine/KarenStarGuide.cs)<br>`KarenStarGuide` | 金边与群牌飞行；跳过常规入塔视觉 | — | P2：最终收束接到塔锚点 |
| [迈向那个舞台](../../src/Core/Models/Cards/shine/KarenToTheStage.cs)<br>`KarenToTheStage` | 抽牌；能量 | 是 | 原生反馈为主，按需补轻量主题提示 |
| [Banana松饼](../../src/Core/Models/Cards/strength/KarenBananaMuffin.cs)<br>`KarenBananaMuffin` | 抽牌；Power 应用/图标（KarenBananaMuffinTempStrengthPower） | — | 原生反馈为主，按需补轻量主题提示 |
| [一起吃饭吧](../../src/Core/Models/Cards/strength/KarenEatTogether.cs)<br>`KarenEatTogether` | Power 应用/图标（KarenEatTogetherTempStrengthPower、KarenRetainTmpStrengthPower） | — | 原生反馈为主，按需补轻量主题提示 |

## 当前卡池：能力牌（20）

| 卡牌 / 源码 | 当前表现接线 | 闪耀 | 分析建议 |
| --- | --- | --- | --- |
| [燃烧吧燃烧吧](../../src/Core/Models/Cards/neutral/KarenBurn.cs)<br>`KarenBurn` | 燃烧模式、持续粒子、塔模式反馈 | — | P1：人物尺寸与生成范围 |
| [觉醒形态](../../src/Core/Models/Cards/neutral/KarenForm.cs)<br>`KarenForm` | 觉醒风场、Power 与 BGM | — | P2：状态应用/移除/恢复统一管理 |
| [命运的齿轮](../../src/Core/Models/Cards/neutral/KarenGeer.cs)<br>`KarenGeer` | Power 应用/图标（KarenGeerPower） | — | 保留应用反馈；关注后续触发提示 |
| [Position 0](../../src/Core/Models/Cards/neutral/KarenPosition0.cs)<br>`KarenPosition0` | Power 应用/图标（KarenPosition0Power） | — | 保留应用反馈；关注后续触发提示 |
| [水族馆](../../src/Core/Models/Cards/promisePile/KarenAquariumCard.cs)<br>`KarenAquariumCard` | Power 应用/图标（KarenAquariumPower） | — | 保留应用反馈；关注后续触发提示 |
| [Banana午餐](../../src/Core/Models/Cards/promisePile/KarenBananaLunch.cs)<br>`KarenBananaLunch` | Power 应用/图标（KarenBananaLunchPower） | — | 保留应用反馈；关注后续触发提示 |
| [信封](../../src/Core/Models/Cards/promisePile/KarenLetterCard.cs)<br>`KarenLetterCard` | Power 应用/图标（KarenLetterPower） | — | 保留应用反馈；关注后续触发提示 |
| [过去与未来](../../src/Core/Models/Cards/promisePile/KarenPastAndFuture.cs)<br>`KarenPastAndFuture` | 模式光环、脉冲与塔模式反馈 | — | P1：多人实例归属；声音接线待决定 |
| [Banana什锦](../../src/Core/Models/Cards/promisePile/KarenSmallSnack.cs)<br>`KarenSmallSnack` | 变牌；升级；Power 应用/图标（KarenSmallSnackTempStrengthPower） | — | 保留应用反馈；关注后续触发提示 |
| [舞台正在等待着](../../src/Core/Models/Cards/promisePile/KarenStageIsWaiting.cs)<br>`KarenStageIsWaiting` | Power 应用/图标（KarenStageIsWaitingPower） | — | 保留应用反馈；关注后续触发提示 |
| [命运舞台的再生产](../../src/Core/Models/Cards/promisePile/KarenStageReproduce.cs)<br>`KarenStageReproduce` | 约定牌堆共用流程（EnterMode） | — | 保留应用反馈；关注后续触发提示 |
| [门票](../../src/Core/Models/Cards/promisePile/KarenTicket.cs)<br>`KarenTicket` | Power 应用/图标（KarenTicketPower） | — | 保留应用反馈；关注后续触发提示 |
| [世界上最空虚的人](../../src/Core/Models/Cards/promisePile/KarenVoid.cs)<br>`KarenVoid` | 原生消耗 VFX 编排、批量移牌与虚无模式 | — | 保留；定向核对移牌后的模型/画面 |
| [两人的梦想](../../src/Core/Models/Cards/promisePile/KarenWeKnow.cs)<br>`KarenWeKown` | Power 应用/图标（KarenWeKownPower） | — | 保留应用反馈；关注后续触发提示 |
| [骄傲](../../src/Core/Models/Cards/relic/KarenArrogant.cs)<br>`KarenArrogant` | Power 应用/图标（VulnerablePower）；额外遗物奖励/标记 | — | 保留应用反馈；关注后续触发提示 |
| [Banana费南雪](../../src/Core/Models/Cards/relic/KarenFinancier.cs)<br>`KarenFinancier` | Power 应用/图标（StrengthPower、KarenFinancierPower） | — | 保留应用反馈；关注后续触发提示 |
| [一起闪耀吧](../../src/Core/Models/Cards/shine/KarenShineTogetherAll.cs)<br>`KarenShineTogetherAll` | 额外闪耀牌奖励/标记 | — | 保留应用反馈；关注后续触发提示 |
| [Starlight第二幕](../../src/Core/Models/Cards/shine/KarenStarlight02Card.cs)<br>`KarenStarlight02Card` | Power 应用/图标（KarenStarlight02Power） | — | 保留应用反馈；关注后续触发提示 |
| [Starlight第三幕](../../src/Core/Models/Cards/shine/KarenStarlight03Card.cs)<br>`KarenStarlight03Card` | 额外闪耀牌奖励/标记 | — | 保留应用反馈；关注后续触发提示 |
| [Starlight第一幕](../../src/Core/Models/Cards/shine/KarenStarlightCard.cs)<br>`KarenStarlightCard` | Power 应用/图标（KarenStarlightPower） | — | 保留应用反馈；关注后续触发提示 |

## 衍生牌（10）

| 卡牌 / 源码 | 当前表现接线 | 闪耀 | 分析建议 |
| --- | --- | --- | --- |
| [banana晚餐](../../src/Core/Models/Cards/token/KarenBanana.cs)<br>`KarenBanana` | Power 应用/图标（StrengthPower） | — | 保留应用反馈；关注后续触发提示 |
| [对峙](../../src/Core/Models/Cards/token/KarenConfront.cs)<br>`KarenConfront` | 原生斩击+伤害 | — | 复用攻击家族，按轻/重/多段调整节奏 |
| [续演](../../src/Core/Models/Cards/token/KarenContinue.cs)<br>`KarenContinue` | 原生斩击+伤害 | — | 复用攻击家族，按轻/重/多段调整节奏 |
| [回击](../../src/Core/Models/Cards/token/KarenCounter.cs)<br>`KarenCounter` | 原生斩击+伤害 | — | 复用攻击家族，按轻/重/多段调整节奏 |
| [空壳](../../src/Core/Models/Cards/token/KarenEmptyShell.cs)<br>`KarenEmptyShell` | 占位衍生牌；无主动演出 | — | 保留安静的占位语义 |
| [三明治](../../src/Core/Models/Cards/token/KarenSandwitch.cs)<br>`KarenSandwitch` | Power 应用/图标（KarenSandwitchTempStrengthPower） | — | 原生反馈为主，按需补轻量主题提示 |
| [闪耀的再生产](../../src/Core/Models/Cards/token/KarenShineReproduce.cs)<br>`KarenShineReproduce` | 牌堆加入预览；选项/选牌及结果移牌 | 是 | 保留选择/移牌预览，核对变换前后清晰度 |
| [侧身](../../src/Core/Models/Cards/token/KarenSideways.cs)<br>`KarenSideways` | 格挡 | — | 原生反馈为主，按需补轻量主题提示 |
| [约定之塔](../../src/Core/Models/Cards/token/KarenTowerOfPromise.cs)<br>`KarenTowerOfPromise` | 约定牌堆共用流程（DrawToFullHand） | — | 优先验证批量取回与手牌节点一致 |
| [光辉](../../src/Core/Models/Cards/token/KarenWeKonwToken.cs)<br>`KarenWeKownToken` | 能量 | — | 原生反馈为主，按需补轻量主题提示 |

## 选项牌（15）

| 卡牌 / 源码 | 当前表现接线 | 闪耀 | 分析建议 |
| --- | --- | --- | --- |
| [弃牌堆](../../src/Core/Models/Cards/token/options/KarenAwakePotionDiscardPileOption.cs)<br>`KarenAwakePotionDiscardPileOption` | 选项/选牌及结果移牌 | — | 保留选项确认，不额外叠加大型演出 |
| [抽牌堆](../../src/Core/Models/Cards/token/options/KarenAwakePotionDrawPileOption.cs)<br>`KarenAwakePotionDrawPileOption` | 选项/选牌及结果移牌 | — | 保留选项确认，不额外叠加大型演出 |
| [约定牌堆](../../src/Core/Models/Cards/token/options/KarenAwakePotionPromisePileOption.cs)<br>`KarenAwakePotionPromisePileOption` | 选项/选牌及结果移牌 | — | 保留选项确认，不额外叠加大型演出 |
| [弃牌堆](../../src/Core/Models/Cards/token/options/KarenNoHesitateDiscardPileOption.cs)<br>`KarenNoHesitateDiscardPileOption` | 约定牌堆共用流程（AddFromDiscard） | — | 保留选项确认，不额外叠加大型演出 |
| [抽牌堆](../../src/Core/Models/Cards/token/options/KarenNoHesitateDrawPileOption.cs)<br>`KarenNoHesitateDrawPileOption` | 约定牌堆共用流程（AddFromDraw） | — | 保留选项确认，不额外叠加大型演出 |
| [手牌](../../src/Core/Models/Cards/token/options/KarenNoHesitateHandOption.cs)<br>`KarenNoHesitateHandOption` | 约定牌堆共用流程（AddFromHand） | — | 保留选项确认，不额外叠加大型演出 |
| [保留格挡](../../src/Core/Models/Cards/token/options/KarenOldPlaceRetainBlockOption.cs)<br>`KarenOldPlaceRetainBlockOption` | Power 应用/图标（BlurPower） | — | 保留选项确认，不额外叠加大型演出 |
| [保留能量](../../src/Core/Models/Cards/token/options/KarenOldPlaceRetainEnergyOption.cs)<br>`KarenOldPlaceRetainEnergyOption` | Power 应用/图标（KarenRetainEnergyPower） | — | 保留选项确认，不额外叠加大型演出 |
| [保留手牌](../../src/Core/Models/Cards/token/options/KarenOldPlaceRetainHandOption.cs)<br>`KarenOldPlaceRetainHandOption` | Power 应用/图标（RetainHandPower） | — | 保留选项确认，不额外叠加大型演出 |
| [保留力量](../../src/Core/Models/Cards/token/options/KarenOldPlaceRetainStrengthOption.cs)<br>`KarenOldPlaceRetainStrengthOption` | Power 应用/图标（KarenRetainTmpStrengthPower） | — | 保留选项确认，不额外叠加大型演出 |
| [放入](../../src/Core/Models/Cards/token/options/KarenRapidPutOption.cs)<br>`KarenRapidPutOption` | 约定牌堆共用流程（AddFromHand） | — | 保留选项确认，不额外叠加大型演出 |
| [取回](../../src/Core/Models/Cards/token/options/KarenRapidTakeOption.cs)<br>`KarenRapidTakeOption` | 约定牌堆共用流程（SelectedToHand） | — | 保留选项确认，不额外叠加大型演出 |
| [弃牌堆](../../src/Core/Models/Cards/token/options/KarenWakeUpDiscardPileOption.cs)<br>`KarenWakeUpDiscardPileOption` | 选项/选牌及结果移牌 | — | 保留选项确认，不额外叠加大型演出 |
| [抽牌堆](../../src/Core/Models/Cards/token/options/KarenWakeUpDrawPileOption.cs)<br>`KarenWakeUpDrawPileOption` | 选项/选牌及结果移牌 | — | 保留选项确认，不额外叠加大型演出 |
| [约定牌堆](../../src/Core/Models/Cards/token/options/KarenWakeUpPromisePileOption.cs)<br>`KarenWakeUpPromisePileOption` | 约定牌堆共用流程（SelectedToHand） | — | 保留选项确认，不额外叠加大型演出 |

## 诅咒（2）

| 卡牌 / 源码 | 当前表现接线 | 闪耀 | 分析建议 |
| --- | --- | --- | --- |
| [困意](../../src/Core/Models/Cards/curses/KarenSleepy.cs)<br>`KarenSleepy` | 诅咒；无主动 OnPlay 演出 | — | 不补主动动画，保留诅咒状态表达 |
| [存在于舞台的理由](../../src/Core/Models/Cards/curses/KarenStageReason.cs)<br>`KarenStageReason` | 诅咒；无主动 OnPlay 演出 | 是 | 不补主动动画，保留诅咒状态表达 |

## 未加入当前卡池（2）

| 卡牌 / 源码 | 当前表现接线 | 闪耀 | 分析建议 |
| --- | --- | --- | --- |
| [披萨](../../src/Core/Models/Cards/neutral/KarenPizza.cs)<br>`KarenPizza` | Power 应用/图标（StrengthPower） | — | 未入池，暂缓新增资源 |
| ["宽恕"](../../src/Core/Models/Cards/relic/KarenForgive.cs)<br>`KarenForgive` | 原生斩击+伤害；遗物目标星光预告+吸收 | — | 未入池；已有遗物预告/吸收，暂缓新增资源 |
