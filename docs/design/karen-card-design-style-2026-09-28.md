# Karen 卡牌风格分析与设计指导草案

日期：2026-09-28。通读范围：当前 Cards 下115个具体卡牌类（包含正式池、基础、Ancient、暂时移出池、诅咒、实体Token、选项Token），中文描述、升级与关键字，并追读相关Power、约定存取、闪耀耗尽/恢复、保留力量和奖励逻辑。以下是从现有作品归纳的设计倾向，不冒充作者已确认的设计宣言；未据此改动卡牌，也没有对平衡性作实测结论。

## 核心判断

Karen的鲜明之处是：牌的位置、寿命和资源的失效时机都可以被玩家经营。玩家不断决定什么现在使用、什么先放起来、什么应该用尽、什么值得留下来；卡牌通过少量共用规则互相改变价值。

她的组合深度主要来自牌与牌的关系。单张牌往往短而明确，整套牌允许很大的变化幅度。闪耀、约定、临时力量、保留和禁用遗物虽然形式不同，却共同围绕资源的去留与转化展开。

## 为什么前一轮15张草案不贴合

1. 先拿原版的伤害/防御规格，再附加一次存牌，没有先识别Karen已有的操作语言。相同前缀和角色名不能代替机制身份。
2. 默认要求所有牌都有保底、都有次数上限。现有《背靠背》《下一个舞台》《最后的台词》恰恰允许空转、难启动或极端条件，以换取特别的使用方式。并非每张都应当成为通用好牌。
3. 加入“X至少2”“约定至少3”“队友本回合第一次”等条件，却没有说明阈值带来的独特选择。现有设计更多直接操作牌堆、寿命、抽取范围、保留规则。
4. 把多人理解成额外伤害或给队友格挡，忽略《歌唱吧舞蹈吧互相争斗吧》把同一张有寿命的牌在队友牌组间传递，《一起闪耀吧》把Karen的闪耀奖励分享出去。
5. 《幕间整备》大体是放大《坠落》/《为什么要离开？我的朋友》的数字；《还不能倒下》又在已有《以觉悟的名义》的力量—攻击—格挡联系旁边添直接换算。它们可能可用，但尚未证明值得占据稀有名额。
6. 拟名多是“守住舞台”“共同战斗”这样的泛化台词，未从具体卡图、动作、日常物品或已有故事关系出发。现有食物、练习、舞台终端各有不同语气，不适合全部改写成同一种英雄宣言。

## 做得好的结构

### 1. 普通牌在完成战斗职责时教会玩家使用机制

《坠落》1费8格挡并放入1张牌；《重逢》1费5格挡、抽2、放1；《双人舞》伤害后先取再放；《昨日夜空》从弃牌堆准备下一次取牌。这些牌让初期的进攻和防守自然形成约定存量。

《勇气打击》造成群伤，把《侧身》存入约定；《观察情况》提供格挡，把《三明治》存入约定。玩家今天做的事，会准备另一种之后可用的动作。衍生牌不是直接兑现成固定附加数值：取牌方式、持有时机、牌堆数量和入堆收益都能改变它的意义。

简单牌有明确存在价值。《闪耀打击》《体操练习》《迈向那个舞台》承担高效率基础功能；不需要每张普通牌都再加一个独立机制。

### 2. 存放和取出本身都有价值，且目的彼此有冲突

| 现有牌 | 实际规则 | 带来的选择 |
|---|---|---|
| 背靠背 | 按约定库存数量×4造成伤害，升级×6 | 有时应先攻击再取牌 |
| 舞台正在等待着 | 清空约定获得能量 | 有时应尽快取空，和囤积的目标不同 |
| 开拓星辰 | 放入时群体虚弱；离开时群体易伤 | 牌的位置决定防守阶段与进攻阶段 |
| 神圣星辰 | 每次被放入约定，战斗内费用减1 | 暂缓打出可以换来更便宜的后续循环 |
| Banana什锦 | 现在打出获得8临时力量；留在约定至回合结束则变成8永久力量的banana晚餐 | 消耗当前机会换未来收益；变化后仍需取出并支付打牌费用 |
| 命运交换之日 | 交换手牌和约定；升级去掉消耗 | 改变整个手牌内容，升级让一次性调度变为可重复操作 |

这类冲突比“牌堆有3张所以多几点伤害”更有内容：玩家需要决定何时放、放哪张、何时取，而非单纯追求把条件图标点亮。

### 3. 一张牌可以靠被处理而发挥作用

《开拓星辰》《添麻烦了》《下一个舞台》都没有通常意义上的打出收益。玩家要用其他牌操作它们。《旋转》在移动中成长，《招架》在手牌流动中增值。

它们把存取牌从单纯的整理手牌工具变成战斗动作。同一张《重逢》在不同构筑里可以是基础防御、启动移牌触发、给后续攻击降费、保存变化牌的工具。这是复用既有动作产生深度的好例子。

不能因此推导“所有新牌都要被动触发”：主动操作牌和被操作牌都需要，且新玩家需要容易理解的入口。

### 4. 闪耀把单场战斗连接到整局构筑

《武术练习》平时防御、耗尽回血；《嫉妒》平时攻击、耗尽得遗物；《永不遗忘的闪耀》耗尽给多次闪耀牌奖励；《耀眼的阳光》提供回归牌组的选择。卡牌退出本身可以成为值得争取的结果。

更好的部分在于，不同牌对同一资源持相反态度：《舞蹈练习》利用仍然拥有的闪耀总量，《我们犯下的罪过》利用已经耗尽的不同牌种，《Starlight第二幕》让闪耀牌额外打出后强制耗尽，《星光闪耀之时》把唯一闪耀牌的剩余次数集中兑现。

这让“省着用”和“尽快用完”都可能成立。永久删牌也不只有损失：它能改变后续抽牌质量，同时留下历史收益。

需要注意实际规则：闪耀是跨战斗寿命，但当前代码中进入消耗堆会恢复当前牌闪耀；CardCmd.Upgrade路径也会恢复。因此不能把每张“消耗＋闪耀”的牌粗略按固定若干场后退出计算，更不能将消耗与闪耀耗尽当成同一件事。新增牌若改变这些路径，必须先弄清它是在保护寿命还是加速耗尽。

### 5. 强度经常受获取路径、位置和时间限制

《三明治》《侧身》《光辉》效果很简洁，但通常先生成在约定里；《光辉》不能只看成免费的2能量，需要连同《两人的梦想》的投入和后续取牌一起评估。

《RevueDuet》《行动》《拉伸》打出后进入约定，回到手中所需的路径是它们的使用成本与组合接口。《摘星》《星罪》用本场战斗的遗物能力换即时资源；升级主要减少禁用成本。《骄傲》用开场能量、易伤和虚无造成压力，换整局成长。

这些限制容易在实际操作中感知。它们能支撑较大胆的收益，同时让玩家有机会改变成本。不过现有系统并不自动保证平衡：循环后如果这些成本全部消失，仍须测试。

### 6. 稀有牌中有较多能改变整副牌用途的设计

《世界上最空虚的人》让约定替代抽牌堆；《命运舞台的再生产》提供无限续演；《燃烧吧燃烧吧》让约定取牌升级并消耗；《门票》提供每回合取牌入口；《最后的台词》把整理主要循环牌堆变成终结条件。

这些牌得到后，会改变对很多旧牌的评价。相比之下，把已有格挡加大并再附一次存牌，可能只改变一次出牌效率。

这不是“稀有都必须改规则”的硬要求。《激情》《投下燃料》《Banana蛋糕》等也以直接且明确的收益成立。关键是稀有牌是否提供值得记住的机会或取舍。

### 7. 升级经常改变使用权限或操作范围

《唤醒》升级扩大可选牌堆；《下一个舞台》升级扩大触发范围；《命运交换之日》《约定之塔》升级去掉消耗；《水族馆》《过去与未来》《门票》升级获得固有；《觉醒形态》升级去掉虚无；《摘星》《星罪》升级降低禁用数量。

这些升级使玩家重新决定何时拿、何时打、如何留牌。《勇气打击》《武术练习》等采用普通加数值升级也合理，没有必要把所有升级都做成新规则。

### 8. 主题可以通过操作过程体现

“约定”对应先存放、未来兑现；“重逢”对应重新拿回；“闪耀”对应高强度使用、耗尽、再生产；食物牌围绕临时力量、保存和等待变化；“最后的台词”对应清理主要循环牌堆后的一击。这是依据牌名与机制的解释，不声称对应原作的某个具体镜头。

多人现有样本仅两张，不能据此定绝对规范；但这两张都分享Karen自己的资源、寿命或奖励，身份比普通的“队友伤害增加”更清楚。

## 可遵守的设计指导

| 指导 | 实际要回答的问题 | 边界 |
|---|---|---|
| 先确定新决定，再填效果和数字 | 玩家拿到此牌后，会在哪个具体时刻换一种做法？ | 基础效率牌也可以成立，但需证明职责或体验有区别 |
| 优先复用已有状态 | 位置、剩余闪耀、耗尽历史、临时力量、库存、保留时长够不够表达？ | 不禁止新机制；新增计数须有现有变量表达不了的意义 |
| 明确主要操作对象 | 这张牌是被打出、被存放、被取出、被耗尽，还是改变别的牌？ | 一张牌可以组合多个动作，但应服务同一个可描述的用途 |
| 把连接成本算进去 | 衍生牌在哪里生成？怎样拿到？需要何种牌与能量？ | 不把生成到手牌与生成到约定视作等价 |
| 为不同资源状态保留理由 | 它鼓励囤积、清空、保留、耗尽中的哪一种？ | 避免所有新牌都奖励同一个最强循环 |
| 不强制所有牌有保底 | 前置条件不满足时是否可以弱、甚至无效？ | 难用应换来明确上限或特别用途，不能只有卡手 |
| 先找自然成本，再决定限次 | 能量、寿命、取牌、消耗、准备时间能否承担约束？ | 确有退化循环时仍可限次；不能为了风格放任死循环 |
| 稀有提供值得记住的机会 | 它改变旧牌价值、整局取舍或关键回合打法了吗？ | 不要求所有稀有都是引擎或复杂能力 |
| 升级强化身份 | 扩大范围、改善时机、减轻代价、增加次数，哪种最合适？ | 加数值仍是合法且常常更清晰的选择 |
| 规则写清，推演留给玩家 | 消耗/耗尽、选择/抽取、当前战斗/永久、张数/牌种能否区分？ | 简洁不能省掉决定性的规则信息 |
| 多人共享自己的玩法 | 这张牌让队友参与Karen什么机制或取舍？ | 通用援护牌可以有，但不应成为所有多人牌的唯一模板 |
| 名字在最后与机制、画面一起核对 | 这个动作是否真的符合选定的物品、台词或场景？ | 不凭空编造原作出处；不强求每张名字都宏大 |

## 一张新牌的评审模板

1. 一句话核心：这张牌允许玩家做什么以前做不到、或不值得做的事？
2. 主要对象：操作哪个现有资源、哪张牌、哪个牌堆或哪个时机？
3. 现有近邻：最像哪两张Karen牌？为何不是它们的数字变体？
4. 不同伙伴：至少列出几种已有牌如何改变它的用法，而非只列伤害相加。
5. 机会成本：为得到收益，玩家必须放弃什么当前或未来机会？
6. 生命周期：当前回合、当前战斗、整局分别会留下什么变化？
7. 升级：更强的是哪个核心体验？是否不小心改成另一张牌？
8. 模式边界：进入约定替代抽牌堆、无限续演、燃烧、重播后，语义是否仍成立？
9. 表述：第一眼能否理解核心？是否需要额外记一个无关计数？
10. 名额：是否符合当前确定的总数、稀有度、单人/多人约束？

原版参考牌用于校准费用、获取频率与大致输出范围。判断是不是Karen的牌，应以本卡池内部关系为主。X费罕见攻击、2费稀有技能只是目前候选规格，不能反过来要求把任意新想法塞进模板。

## 值得保留，但不能当成已验证平衡的部分

- 自由存取、0费牌、抽牌回能、移牌收益、去消耗升级相互叠加，可能让一个低成本循环压过囤积与等待的选择。需对局验证，不能仅凭代码宣布存在或不存在无限。
- 整局收益牌会改变奖励和遗物经济；如果拖延战斗成为最优选择，可能损害节奏。检查触发次数、耗尽和生成限制，但不据此直接削弱。
- 模式牌的优点是改变规则，也因此需要公开清楚的规则。《世界上最空虚的人》影响抽取与存放的意义；闪耀进入消耗堆恢复、升级恢复也不宜让玩家只能靠试错发现。
- 稀有度不是纯粹的复杂度分层；卡池里有简单稀有和复杂罕见。不要为了追求统一，把现有好用的基础牌都改复杂。
- 《续写》的描述只写“打出所有耗尽的续写”，但当前代码对耗尽列表使用逐次缩短的多轮遍历；不能只按描述理解为每张恰好一次。这里只记录理解边界，不将实现差异推断成设计意图或直接修改。

## 后续设计的起点

暂停此前15张草案。先找现有规则下值得增加的选择，再筛选符合剩余配额的方案；即便最终仍是X费攻击或2费防御，也应能说明它如何改变Karen已有牌的使用，而不仅是给原版效果追加一行约定文本。

## 阅读覆盖清单

以下按实际具体卡牌类记录；选项牌计入阅读覆盖，不计入正式卡池数量。池外模型不作为正式构筑的可获得证据。

| 目录 | 模型／中文名 | 当前登记状态 |
|---|---|---|
| ancient | [为什么要离开？我的朋友](../../src/Core/Models/Cards/ancient/KarenWhy.cs) | KarenCardPool |
| basic | [防御](../../src/Core/Models/Cards/basic/KarenDefend.cs) | KarenCardPool |
| basic | [坠落](../../src/Core/Models/Cards/basic/KarenFall.cs) | KarenCardPool |
| basic | [不再遥不可及](../../src/Core/Models/Cards/basic/KarenPromiseDraw.cs) | KarenCardPool |
| basic | [闪耀打击](../../src/Core/Models/Cards/basic/KarenShineStrike.cs) | KarenCardPool |
| basic | [打击](../../src/Core/Models/Cards/basic/KarenStrike.cs) | KarenCardPool |
| curses | [困意](../../src/Core/Models/Cards/curses/KarenSleepy.cs) | Token／选项／特殊模型 |
| curses | [存在于舞台的理由](../../src/Core/Models/Cards/curses/KarenStageReason.cs) | Token／选项／特殊模型 |
| globalMove | [下一个舞台](../../src/Core/Models/Cards/globalMove/KarenNextStage.cs) | KarenCardPool |
| globalMove | [旋转](../../src/Core/Models/Cards/globalMove/KarenSpin.cs) | KarenCardPool |
| globalMove | [唤醒](../../src/Core/Models/Cards/globalMove/KarenWakeUp.cs) | KarenCardPool |
| neutral | [Banana蛋糕](../../src/Core/Models/Cards/neutral/KarenBananaCake.cs) | KarenCardPool |
| neutral | [燃烧吧燃烧吧](../../src/Core/Models/Cards/neutral/KarenBurn.cs) | KarenCardPool |
| neutral | [以觉悟的名义](../../src/Core/Models/Cards/neutral/KarenConsciousness.cs) | KarenCardPool |
| neutral | [添麻烦了](../../src/Core/Models/Cards/neutral/KarenCry.cs) | KarenCardPool |
| neutral | [觉醒形态](../../src/Core/Models/Cards/neutral/KarenForm.cs) | KarenCardPool |
| neutral | [命运的齿轮](../../src/Core/Models/Cards/neutral/KarenGeer.cs) | KarenCardPool |
| neutral | [最后的台词](../../src/Core/Models/Cards/neutral/KarenLastWord.cs) | KarenCardPool |
| neutral | [约定的那天](../../src/Core/Models/Cards/neutral/KarenOldPlace.cs) | KarenCardPool |
| neutral | [招架](../../src/Core/Models/Cards/neutral/KarenParry.cs) | KarenCardPool |
| neutral | [披萨](../../src/Core/Models/Cards/neutral/KarenPizza.cs) | 暂时移出池 |
| neutral | [Position 0](../../src/Core/Models/Cards/neutral/KarenPosition0.cs) | KarenCardPool |
| promisePile | [水族馆](../../src/Core/Models/Cards/promisePile/KarenAquariumCard.cs) | KarenCardPool |
| promisePile | [背靠背](../../src/Core/Models/Cards/promisePile/KarenBackToBackCard.cs) | KarenCardPool |
| promisePile | [Banana午餐](../../src/Core/Models/Cards/promisePile/KarenBananaLunch.cs) | KarenCardPool |
| promisePile | [棒球](../../src/Core/Models/Cards/promisePile/KarenBaseball.cs) | KarenCardPool |
| promisePile | [约定之塔桥](../../src/Core/Models/Cards/promisePile/KarenBridge.cs) | KarenCardPool |
| promisePile | [勇气打击](../../src/Core/Models/Cards/promisePile/KarenCourageStrike.cs) | KarenCardPool |
| promisePile | [舞动](../../src/Core/Models/Cards/promisePile/KarenDance.cs) | KarenCardPool |
| promisePile | [闪避](../../src/Core/Models/Cards/promisePile/KarenDodge.cs) | KarenCardPool |
| promisePile | [命运交换之日](../../src/Core/Models/Cards/promisePile/KarenExchangeFate.cs) | KarenCardPool |
| promisePile | [神圣星辰](../../src/Core/Models/Cards/promisePile/KarenHolyStar.cs) | KarenCardPool |
| promisePile | [落地](../../src/Core/Models/Cards/promisePile/KarenLanding.cs) | KarenCardPool |
| promisePile | [信封](../../src/Core/Models/Cards/promisePile/KarenLetterCard.cs) | KarenCardPool |
| promisePile | [重逢](../../src/Core/Models/Cards/promisePile/KarenMeetAgain.cs) | KarenCardPool |
| promisePile | [新的一天](../../src/Core/Models/Cards/promisePile/KarenNewDay.cs) | KarenCardPool |
| promisePile | [观察情况](../../src/Core/Models/Cards/promisePile/KarenNewSituation.cs) | KarenCardPool |
| promisePile | [不再犹豫](../../src/Core/Models/Cards/promisePile/KarenNoHesitate.cs) | KarenCardPool |
| promisePile | [命运与本能](../../src/Core/Models/Cards/promisePile/KarenOnStage.cs) | KarenCardPool |
| promisePile | [开拓星辰](../../src/Core/Models/Cards/promisePile/KarenOpenUpStars.cs) | KarenCardPool |
| promisePile | [我们的约定](../../src/Core/Models/Cards/promisePile/KarenOurPromise.cs) | KarenCardPool |
| promisePile | [过去与未来](../../src/Core/Models/Cards/promisePile/KarenPastAndFuture.cs) | KarenCardPool |
| promisePile | [极速下降](../../src/Core/Models/Cards/promisePile/KarenRapid.cs) | KarenCardPool |
| promisePile | [RevueDuet](../../src/Core/Models/Cards/promisePile/KarenRevueDuet.cs) | KarenCardPool |
| promisePile | [行动](../../src/Core/Models/Cards/promisePile/KarenRun.cs) | KarenCardPool |
| promisePile | [Banana什锦](../../src/Core/Models/Cards/promisePile/KarenSmallSnack.cs) | KarenCardPool |
| promisePile | [舞台正在等待着](../../src/Core/Models/Cards/promisePile/KarenStageIsWaiting.cs) | KarenCardPool |
| promisePile | [命运舞台的再生产](../../src/Core/Models/Cards/promisePile/KarenStageReproduce.cs) | KarenCardPool |
| promisePile | [拉伸](../../src/Core/Models/Cards/promisePile/KarenStretching.cs) | KarenCardPool |
| promisePile | [门票](../../src/Core/Models/Cards/promisePile/KarenTicket.cs) | KarenCardPool |
| promisePile | [东京塔下](../../src/Core/Models/Cards/promisePile/KarenUnderTower.cs) | KarenCardPool |
| promisePile | [世界上最空虚的人](../../src/Core/Models/Cards/promisePile/KarenVoid.cs) | KarenCardPool |
| promisePile | [两人的梦想](../../src/Core/Models/Cards/promisePile/KarenWeKnow.cs) | KarenCardPool |
| promisePile | [双人舞](../../src/Core/Models/Cards/promisePile/KarenWhosPromise.cs) | KarenCardPool |
| promisePile | [昨日夜空](../../src/Core/Models/Cards/promisePile/KarenYesterglow.cs) | KarenCardPool |
| relic | [骄傲](../../src/Core/Models/Cards/relic/KarenArrogant.cs) | KarenCardPool |
| relic | [Banana费南雪](../../src/Core/Models/Cards/relic/KarenFinancier.cs) | KarenCardPool |
| relic | ["宽恕"](../../src/Core/Models/Cards/relic/KarenForgive.cs) | 暂时移出池 |
| relic | [嫉妒](../../src/Core/Models/Cards/relic/KarenJealousy.cs) | KarenCardPool |
| relic | [激情](../../src/Core/Models/Cards/relic/KarenPassion.cs) | KarenCardPool |
| relic | [摘星](../../src/Core/Models/Cards/relic/KarenPickStar.cs) | KarenCardPool |
| relic | [星罪](../../src/Core/Models/Cards/relic/KarenStarCrime.cs) | KarenCardPool |
| shine | [我们犯下的罪过](../../src/Core/Models/Cards/shine/KarenCarryingGuilt.cs) | KarenCardPool |
| shine | [蓄力打击](../../src/Core/Models/Cards/shine/KarenChargeStrike.cs) | KarenCardPool |
| shine | [永无结束的命运舞台](../../src/Core/Models/Cards/shine/KarenContinue02.cs) | KarenCardPool |
| shine | [出场](../../src/Core/Models/Cards/shine/KarenDebut.cs) | KarenCardPool |
| shine | [水分补充](../../src/Core/Models/Cards/shine/KarenDrinkWater.cs) | KarenCardPool |
| shine | [投下燃料](../../src/Core/Models/Cards/shine/KarenDropFuel.cs) | KarenCardPool |
| shine | [歌唱吧舞蹈吧互相争斗吧](../../src/Core/Models/Cards/shine/KarenFightRelay.cs) | KarenCardPool |
| shine | [NONNON哒哟](../../src/Core/Models/Cards/shine/KarenNonon.cs) | KarenCardPool |
| shine | [土豆](../../src/Core/Models/Cards/shine/KarenPotato.cs) | KarenCardPool |
| shine | [武术练习](../../src/Core/Models/Cards/shine/KarenPractice.cs) | KarenCardPool |
| shine | [舞蹈练习](../../src/Core/Models/Cards/shine/KarenPractice2.cs) | KarenCardPool |
| shine | [体操练习](../../src/Core/Models/Cards/shine/KarenPractice3.cs) | KarenCardPool |
| shine | [准备完成](../../src/Core/Models/Cards/shine/KarenReady.cs) | KarenCardPool |
| shine | [续写](../../src/Core/Models/Cards/shine/KarenRewrite.cs) | KarenCardPool |
| shine | [用你的闪耀贯穿我吧](../../src/Core/Models/Cards/shine/KarenShineStrikeBarrage.cs) | KarenCardPool |
| shine | [永不遗忘的闪耀](../../src/Core/Models/Cards/shine/KarenShineTogether.cs) | KarenCardPool |
| shine | [一起闪耀吧](../../src/Core/Models/Cards/shine/KarenShineTogetherAll.cs) | KarenCardPool |
| shine | [星光闪耀之时](../../src/Core/Models/Cards/shine/KarenStar.cs) | KarenCardPool |
| shine | [星星串起了我们的友谊](../../src/Core/Models/Cards/shine/KarenStarFriend.cs) | KarenCardPool |
| shine | [星光指引](../../src/Core/Models/Cards/shine/KarenStarGuide.cs) | KarenCardPool |
| shine | [Starlight第二幕](../../src/Core/Models/Cards/shine/KarenStarlight02Card.cs) | KarenCardPool |
| shine | [Starlight第三幕](../../src/Core/Models/Cards/shine/KarenStarlight03Card.cs) | KarenCardPool |
| shine | [Starlight第一幕](../../src/Core/Models/Cards/shine/KarenStarlightCard.cs) | KarenCardPool |
| shine | [耀眼的阳光](../../src/Core/Models/Cards/shine/KarenSunlight.cs) | KarenCardPool |
| shine | [上挑](../../src/Core/Models/Cards/shine/KarenSwordUp.cs) | KarenCardPool |
| shine | [迈向那个舞台](../../src/Core/Models/Cards/shine/KarenToTheStage.cs) | KarenCardPool |
| strength | [Banana松饼](../../src/Core/Models/Cards/strength/KarenBananaMuffin.cs) | KarenCardPool |
| strength | [一起吃饭吧](../../src/Core/Models/Cards/strength/KarenEatTogether.cs) | KarenCardPool |
| token | [banana晚餐](../../src/Core/Models/Cards/token/KarenBanana.cs) | Token／选项／特殊模型 |
| token | [对峙](../../src/Core/Models/Cards/token/KarenConfront.cs) | Token／选项／特殊模型 |
| token | [续演](../../src/Core/Models/Cards/token/KarenContinue.cs) | Token／选项／特殊模型 |
| token | [回击](../../src/Core/Models/Cards/token/KarenCounter.cs) | Token／选项／特殊模型 |
| token | [空壳](../../src/Core/Models/Cards/token/KarenEmptyShell.cs) | Token／选项／特殊模型 |
| token | [三明治](../../src/Core/Models/Cards/token/KarenSandwitch.cs) | Token／选项／特殊模型 |
| token | [闪耀的再生产](../../src/Core/Models/Cards/token/KarenShineReproduce.cs) | Token／选项／特殊模型 |
| token | [侧身](../../src/Core/Models/Cards/token/KarenSideways.cs) | Token／选项／特殊模型 |
| token | [约定之塔](../../src/Core/Models/Cards/token/KarenTowerOfPromise.cs) | Token／选项／特殊模型 |
| token | [光辉](../../src/Core/Models/Cards/token/KarenWeKonwToken.cs) | Token／选项／特殊模型 |
| options | [弃牌堆](../../src/Core/Models/Cards/token/options/KarenAwakePotionDiscardPileOption.cs) | Token／选项／特殊模型 |
| options | [抽牌堆](../../src/Core/Models/Cards/token/options/KarenAwakePotionDrawPileOption.cs) | Token／选项／特殊模型 |
| options | [约定牌堆](../../src/Core/Models/Cards/token/options/KarenAwakePotionPromisePileOption.cs) | Token／选项／特殊模型 |
| options | [弃牌堆](../../src/Core/Models/Cards/token/options/KarenNoHesitateDiscardPileOption.cs) | Token／选项／特殊模型 |
| options | [抽牌堆](../../src/Core/Models/Cards/token/options/KarenNoHesitateDrawPileOption.cs) | Token／选项／特殊模型 |
| options | [手牌](../../src/Core/Models/Cards/token/options/KarenNoHesitateHandOption.cs) | Token／选项／特殊模型 |
| options | [保留格挡](../../src/Core/Models/Cards/token/options/KarenOldPlaceRetainBlockOption.cs) | Token／选项／特殊模型 |
| options | [保留能量](../../src/Core/Models/Cards/token/options/KarenOldPlaceRetainEnergyOption.cs) | Token／选项／特殊模型 |
| options | [保留手牌](../../src/Core/Models/Cards/token/options/KarenOldPlaceRetainHandOption.cs) | Token／选项／特殊模型 |
| options | [保留力量](../../src/Core/Models/Cards/token/options/KarenOldPlaceRetainStrengthOption.cs) | Token／选项／特殊模型 |
| options | [放入](../../src/Core/Models/Cards/token/options/KarenRapidPutOption.cs) | Token／选项／特殊模型 |
| options | [取回](../../src/Core/Models/Cards/token/options/KarenRapidTakeOption.cs) | Token／选项／特殊模型 |
| options | [弃牌堆](../../src/Core/Models/Cards/token/options/KarenWakeUpDiscardPileOption.cs) | Token／选项／特殊模型 |
| options | [抽牌堆](../../src/Core/Models/Cards/token/options/KarenWakeUpDrawPileOption.cs) | Token／选项／特殊模型 |
| options | [约定牌堆](../../src/Core/Models/Cards/token/options/KarenWakeUpPromisePileOption.cs) | Token／选项／特殊模型 |

具体模型合计：115。
