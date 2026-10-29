# Karen 卡牌特效、节奏与性能逐卡审阅

日期：2026-10-29。基线：main `6a7a9bdc36bb1b33c6eb1b54212d3f0100d5e781`。范围为当前已提交源码；原目录中已有的文档与遥测改动保持原状。

**先完成基础牌专属表现，再优化高频连锁、常驻伤痕的数量和等待策略。** 当前多张重点牌已有专属演出，约定牌堆、闪耀、舞台与收益系统也提供了完整接线。逐卡建议优先沿用这些入口。

本次逐张阅读了 116 个具体卡牌类的实现，核对中文名、卡池注册、直接特效调用，以及相关 Power、Patch 和共享表现入口。以下为源码审阅；游戏内观感、帧耗时和联机运行结果列为待定向实测。**P1/P2/P3 表示优化投入顺序**：P1 优先验证与设计，P2 局部改进，P3 保持或低成本完善；“暂缓”用于已移出主卡池的牌。

后续要求已纳入：[分批完成计划与基础牌设计](card-vfx-rollout-plan-2026-10-29.md)。基础五张牌提升为第一批必做；《打击》《防御》《闪耀打击》计划分别使用独立演出入口。

## 1. 优先处理的事项

### 1.1 伤痕数量与查询成本随命中累计

适用：《永不遗忘的闪耀》。

源码：[NKarenUnforgottenShineVfx](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenUnforgottenShineVfx.cs)。

- 已有整场共用组与 CanvasGroup 透明度预算，保留这个结构。
- `AddCut()` 每次把一个新 Sprite 加入 `_retained`；新鲜闪光另有淡出列表。
- `PickHitPoint()` 遍历 `_retained.GetChildren()` 建立邻居列表，再进行 8 次候选采样。当前实现中未见保留伤痕的数量上限。
- 保留对象数量随命中增长；连续命中的累计扫描工作也随之增加。**这是源码可定位的增长路径**，实际帧耗时仍待运行采样。

最小优化方向：为目标或整场设置固定伤痕预算，优先复用旧 Sprite，位置查询只访问有界集合；旧痕用短淡出退场。保留叠加后统一透明度和新命中的亮点。先做有界集合，再按观感决定是否需要累积纹理。

### 1.2 高频自动出牌按整串控制装饰预算

适用：《续写》《星光闪耀之时》《约定之塔桥》《续演》及第二幕联动。

源码：[KarenRewrite](../../src/Core/Models/Cards/shine/KarenRewrite.cs)、[KarenStar](../../src/Core/Models/Cards/shine/KarenStar.cs)、[NKarenContinueVfx](../../src/ShoujoKagekiAijoKaren/GodotScripts/Vfx/NKarenContinueVfx.cs)、[KarenStarlightStage](../../src/Core/Vfx/KarenStarlightStage.cs)。

- 《续写》有 n 张符合条件的耗尽牌时，代码生成 `n(n+1)/2` 次衍生播放；n=5 为 15 次，n=10 为 55 次。这是规则规模，视觉可单独设预算。
- 《续演》每次从原生飞刀创建表现，复制多个粒子 ProcessMaterial、建立 ShaderMaterial，并增加火屑子层；共享调色与曲线已缓存。
- 《星光闪耀之时》已有一个整串 `Playback`、真实 CardPlay 事件队列与 `.55` 秒脉冲淘汰；优化应扩展这套作用域。
- 第二幕已按排队/执行/耗尽状态持有共享舞台；继续保留当前所有权。

优化方向：整串只完整播放一次准备，中间保留真实每击的局部接触，末次或规则奖励时做收束；共享不可变材质、粒子模板和小顶点缓冲，动态参数留在实例。可复用小节点优先在本次演出内复用，再评估跨战斗池化的收益。

### 1.3 固定等待先按规则时钟分类

适用：《命运的齿轮》《投下燃料》《约定之塔桥》《约定之塔》。

| 入口 | 源码中配置的等待 | 当前观察 |
| --- | --- | --- |
| [KarenGeer](../../src/Core/Models/Cards/neutral/KarenGeer.cs) | 施放 1.00 秒 | PlayCast 可因本地显示条件跳过，命令等待仍执行 |
| [KarenGeerPower](../../src/Core/Models/Powers/KarenGeerPower.cs) | 每层触发 0.65 秒 | 源码明确以固定原生时序保持选择与本地渲染独立 |
| [KarenDropFuel](../../src/Core/Models/Cards/shine/KarenDropFuel.cs) | 普通/Fast 均 0.80 秒 | 这是已确定的等拍设计；工厂返回空仍走同一规则等待 |
| [KarenBridge](../../src/Core/Models/Cards/promisePile/KarenBridge.cs) | Fast 0.30 / 普通 0.48 秒 | 条件为有可用牌，与是否建立表现分别判断 |
| [KarenTowerOfPromise](../../src/Core/Models/Cards/token/KarenTowerOfPromise.cs) | Fast 0.18 / 普通 0.48 秒 | 条件为能补牌，与回收表现是否建立分别判断 |

表中是传入 `Cmd.CustomScaledWait` 的参数；实际经过时间还受游戏时钟与运行模式影响。

优化顺序：先核对哪些等待承担结算/联机节拍，哪些属于纯起势；规则等待使用参与方一致的速度策略，画面按本地质量独立简化。当前即可先减少尾部重叠、隐藏层绘制和高频实例分配。需要调整规则节拍时，再安排双方不同质量配置的同版本定向联机验证。

### 1.4 最后的台词：质量降级与规则等待分别核查

源码：[KarenLastWord](../../src/Core/Models/Cards/neutral/KarenLastWord.cs)、[NKarenLastWordVideoVfx](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenLastWordVideoVfx.cs)。

当前低档改播文字后，视频入口仍返回 true，保留 6.7 秒长段所有权；缺视频时使用 0.7 秒文字段。完整/低档已显式考虑相同的长段等待。关闭档则由卡牌外层条件跳过该段。

先验证参与方显示档位或资源条件不同时的出牌节拍，再决定统一等待策略。视频、文字和轻量替代共享“何时命中”的语义点，各自保留独立的绘制与释放。保留音乐交接、真实攻击结算和关闭档原生反馈。

### 1.5 批量移牌继续以真实卡面为核心

适用：《星光指引》《世界上最空虚的人》《约定之塔》《命运交换之日》以及所有入塔/取回牌。

当前已提供真实卡面、库存星、批量交接、空虚锚点与完成通知。优化集中到共享路径：复用轨迹缓冲、同批只做一次大面积起势、按可见对象给装饰预算。卡牌模型、真实节点、完成任务继续由当前命令负责。

验收重点：动画前后截图、查看实际绘制、结束后 `karen_check_hand`；普通/空虚、低/关闭、满手/空塔按本次涉及的分支定向覆盖。

### 1.6 当前已具备的性能与生命周期基础

| 已有能力 | 本次采用方式 |
| --- | --- |
| [KarenVfxQuality](../../src/Core/Vfx/KarenVfxQuality.cs)：低档约四分之一粒子/装饰采样，重绘最高20帧 | 新优化接当前预算；对固定 Sprite 和保留对象另设数量预算 |
| [KarenVfxWarmup](../../src/Core/Vfx/KarenVfxWarmup.cs)：共享资源准备与真实绘制 | 继续登记资源，首次使用专项按需求验证 |
| [KarenVfxComposition](../../src/Core/PromisePileSystem/Vfx/KarenVfxComposition.cs)：常驻与短cue亮度协调 | 保留统一合成，先减大面积重叠 |
| [AttackCommandCompat](../../src/Core/Compat/AttackCommandCompat.cs)：关闭档补原生刀光 | 沿用原生反馈回退 |
| [ShineVfx](../../src/Core/ShineSystem/ShineVfx.cs)：模型身份、overlay与回池清理 | 保留；局部卡缘方案接入这里 |
| [KarenPowerVfxAnchor](../../src/Core/Vfx/KarenPowerVfxAnchor.cs)：实际Power图标定位 | 收益确认复用当前锚点 |

## 2. 视觉家族与制作方法

瓦伦希娜研究中可迁移的三点：用命中事件联系画面和结算、宽形状/细亮芯/散点分层、卡缘绑定原生轮廓与高亮节拍。华恋已有大量程序化特效，新增变化可先用少量共享资源完成。

| 家族 | 覆盖方式 | 制作重点 |
| --- | --- | --- |
| 轻击/重击/多段/群攻 | 基础攻击与部分普通牌复用；重点稀有牌保留专属主形 | 起势一次、实际每击接触、末击收束；尺寸和亮度封顶 |
| 入塔/取回/交换 | 所有约定牌堆相关牌 | 实际卡面与库存星连续交接，批次共享轨迹材质 |
| 格挡/力量/治疗/回能 | 普通收益牌与选项 | 真实结果的小图标确认，常用牌保持即时 |
| 临界/耗尽/成长 | 闪耀牌、成长牌 | 现有SDF/轮廓与身份管理，静态几何缓存 |
| 模式/舞台 | 燃烧、觉醒、再生产、空虚、第二幕 | 一次建立，事件局部增强，退出统一释放 |
| 选择 | 唤醒、Why、不再犹豫、极速下降等 | 原生卡牌与按钮在上方，装饰忽略输入，布局变化时更新缓存 |

以下逐卡表的“当前表现”包含共享机制和 Power 驱动的间接表现；建议列是下一步设计方向。P3 的保留项同样是逐卡审阅结论。

## 3. 逐卡清单



### 3.1 基础与入门（5）

| 卡牌 / 源码 | 优先级 | 当前表现 | 优化建议 |
| --- | --- | --- | --- |
| [防御](../../src/Core/Models/Cards/basic/KarenDefend.cs)<br>`KarenDefend` | P1 | 原生格挡反馈。 | 第一批实现专属入口NKarenDefendVfx：角色迎敌侧升起三片相扣的弧形护幕，细金边和低透明玫红内面，中心以小盾菱形收束。 实际格挡确认后播放，规则即时结算。 |
| [坠落](../../src/Core/Models/Cards/basic/KarenFall.cs)<br>`KarenFall` | P1 | 先格挡，再选手牌入塔，走共用移牌。 | 第一批实现专属入口NKarenFallVfx：短护幕由上向下压稳，完成选牌后向塔收拢一段下落弧线，沿真实卡面入塔。 按真实命中或移牌确认，复用共享工具。 |
| [不再遥不可及](../../src/Core/Models/Cards/basic/KarenPromiseDraw.cs)<br>`KarenPromiseDraw` | P1 | 原生斩击后从塔取2/4张。 | 第一批实现专属入口NKarenPromiseDrawVfx：短斜切接触后，塔向真实归手卡发出一束弯折回光，突出攻击引回卡牌的关系。 按真实命中或移牌确认，复用共享工具。 |
| [闪耀打击](../../src/Core/Models/Cards/basic/KarenShineStrike.cs)<br>`KarenShineStrike` | P1 | 9点原生斩击，闪耀9；卡缘与耗尽由共享系统负责。 | 第一批实现专属入口NKarenShineStrikeVfx：更细的白金直刃，两侧短金轨在接触处闭成四角星，随后只剩短刃尾。 按真实命中或移牌确认，复用共享工具。 |
| [打击](../../src/Core/Models/Cards/basic/KarenStrike.cs)<br>`KarenStrike` | P1 | 6点原生斩击。 | 第一批实现专属入口NKarenStrikeVfx：短直突刺，珊瑚红窄刃带配象牙白刃尖，目标近侧出现单个菱形接触点。 按真实命中或移牌确认，复用共享工具。 |

### 3.2 闪耀（26）

| 卡牌 / 源码 | 优先级 | 当前表现 | 优化建议 |
| --- | --- | --- | --- |
| [我们犯下的罪过](../../src/Core/Models/Cards/shine/KarenCarryingGuilt.cs)<br>`KarenCarryingGuilt` | P2 | 目标悬停聚焦、舞台灯、按既定时间命中。 | 保留分轮灯与悬停语言；缓存固定灯几何，目标变形更新锚点，重播共享聚焦层。 |
| [蓄力打击](../../src/Core/Models/Cards/shine/KarenChargeStrike.cs)<br>`KarenChargeStrike` | P3 | 原生单击后给敌方临时减力量。 | 轻型蓄力亮芯与末端图标确认共用攻击家族；前摇保持短，别把每次重播都做满起势。 |
| [永无结束的命运舞台](../../src/Core/Models/Cards/shine/KarenContinue02.cs)<br>`KarenContinue02` | P2 | 无限升级成长，原生斩击，闪耀与降级逻辑已有专门处理。 | 根据伤害/升级做封顶的宽度与亮度映射；高数值仍保持单个短接触。 |
| [出场](../../src/Core/Models/Cards/shine/KarenDebut.cs)<br>`KarenDebut` | P2 | 全敌3/4击，原生逐击斩击。 | 核对多段是否重复起势；一份群攻准备配逐击局部刀光，末击收束更清楚。 |
| [水分补充](../../src/Core/Models/Cards/shine/KarenDrinkWater.cs)<br>`KarenDrinkWater` | P3 | 施加1层缓冲，闪耀3，消耗。 | 保留原生护罩和闪耀卡缘；可补短弧面但不增加规则等待。 |
| [投下燃料](../../src/Core/Models/Cards/shine/KarenDropFuel.cs)<br>`KarenDropFuel` | P1 | 卡面蓄势/点燃后等0.8秒给抽牌和能量，普通/Fast同拍。 | 保留当前已定节拍作为基线；先核查重播、关闭档和主客机等待策略，再压缩可独立缩短的尾部。 |
| [歌唱吧舞蹈吧互相争斗吧](../../src/Core/Models/Cards/shine/KarenFightRelay.cs)<br>`KarenFightRelay` | P2 | 联机传递真实卡副本后播放传递线，末次耗尽登记奖励。 | 保留每传出者重叠限制和本地发声；重点看多个参与者同帧的HUD线条遮挡与真实接收者锚点。 |
| [NONNON哒哟](../../src/Core/Models/Cards/shine/KarenNonon.cs)<br>`KarenNonon` | P2 | 0.66秒大横斩一次群攻，再群体临时减力量。 | 保留大横斩主形；敌人数增加只添加局部命中/图标确认，大面积光幕只保留一份。 |
| [土豆](../../src/Core/Models/Cards/shine/KarenPotato.cs)<br>`KarenPotato` | P3 | 真实治疗，闪耀3。 | 使用原生治疗数字与短暖光即可；满血时按实际治疗量收束。 |
| [武术练习](../../src/Core/Models/Cards/shine/KarenPractice.cs)<br>`KarenPractice` | P3 | 格挡；闪耀耗尽时治疗。 | 常规打出保持轻盾形，耗尽治疗共用回血确认，分开表达两种发生时点。 |
| [舞蹈练习](../../src/Core/Models/Cards/shine/KarenPractice2.cs)<br>`KarenPractice2` | P3 | 全牌剩余闪耀总量换算格挡。 | 保留原生格挡；可从闪耀计数做一次汇聚至盾形，粒子数量封顶，避免逐牌建节点。 |
| [体操练习](../../src/Core/Models/Cards/shine/KarenPractice3.cs)<br>`KarenPractice3` | P3 | 较高格挡，闪耀9。 | 与武术练习共用盾形模板，通过大小/包络区分，装饰预算封顶。 |
| [准备完成](../../src/Core/Models/Cards/shine/KarenReady.cs)<br>`KarenReady` | P3 | 力量后抽1张，闪耀6。 | 用单次能力图标和真实卡面确认；保留0费牌的即时感。 |
| [续写](../../src/Core/Models/Cards/shine/KarenRewrite.cs)<br>`KarenRewrite` | P1 | 原生攻击后按三角数量克隆并自动打出耗尽区同名牌。 | 连锁内复用轻击材质、一次准备与有界尾迹；克隆/击数维持规则，优先观察大n峰值。 |
| [用你的闪耀贯穿我吧](../../src/Core/Models/Cards/shine/KarenShineStrikeBarrage.cs)<br>`KarenShineStrikeBarrage` | P2 | 一次准备、4/5次贯穿，0.12秒击间隔，finally完成。 | 保留真实逐击和穿过完整目标边界；减少每击散点/材质分配，末击增强一次。 |
| [永不遗忘的闪耀](../../src/Core/Models/Cards/shine/KarenShineTogether.cs)<br>`KarenShineTogether` | P1 | 命中留下战斗常驻伤痕，CanvasGroup统一透明度，耗尽登记奖励。 | 给每目标/全战斗保留伤痕设上限，复用Sprite或淘汰旧痕；位置邻居查询改为有界集合。 |
| [一起闪耀吧](../../src/Core/Models/Cards/shine/KarenShineTogetherAll.cs)<br>`KarenShineTogetherAll` | P2 | 联机收益短演出，逐个真实玩家登记闪耀奖励。 | 共享施放层；每接收者只做对应确认点，保持静音约定与去重。 |
| [星光闪耀之时](../../src/Core/Models/Cards/shine/KarenStar.cs)<br>`KarenStar` | P1 | 聚集后自动重播唯一闪耀牌，按真实CardPlay发脉冲，最后确认奖励。 | 整串共用当前Playback；限制脉冲/装饰重叠，并让目标查询在发现第二张时提前结束。 |
| [星星串起了我们的友谊](../../src/Core/Models/Cards/shine/KarenStarFriend.cs)<br>`KarenStarFriend` | P2 | 原生单击，真正致命时才登记闪耀奖励。 | 加入短小致命星光确认，挂实际WasTargetKilled结果；普通击保持轻量。 |
| [星光指引](../../src/Core/Models/Cards/shine/KarenStarGuide.cs)<br>`KarenStarGuide` | P1 | 收集手/抽/弃中的闪耀牌，定制收束后批量入塔并刷新数量。 | 优先核对批量真实卡面和skipVisuals路径；统一轨迹缓冲，装饰采样有界，保持每模型一颗库存星。 |
| [Starlight第二幕](../../src/Core/Models/Cards/shine/KarenStarlight02Card.cs)<br>`KarenStarlight02Card` | P2 | 施放短演出与Power确认；KarenStarlightStage持有整次闪耀牌出牌舞台。 | 沿用整次任务持有；只优化持有期重绘和顶点复用，保留重播/耗尽后的最后释放。 |
| [Starlight第三幕](../../src/Core/Models/Cards/shine/KarenStarlight03Card.cs)<br>`KarenStarlight03Card` | P3 | 直接登记闪耀牌奖励，当前无长专属演出。 | 共用奖励图标短确认即可；与第一/二幕用结果形状区分。 |
| [Starlight第一幕](../../src/Core/Models/Cards/shine/KarenStarlightCard.cs)<br>`KarenStarlightCard` | P2 | 第一幕短星轨给Power；打闪耀牌后确认真实抽牌。 | 保留七星半圆与静音；抽多张共用主轨，单张只增加终点确认。 |
| [耀眼的阳光](../../src/Core/Models/Cards/shine/KarenSunlight.cs)<br>`KarenSunlight` | P2 | 原生斩击；耗尽后按战斗状态选择回牌组或登记奖励。 | 优化耗尽→选择→真实回牌组的衔接；保留原生选择，复用小型结果确认。 |
| [上挑](../../src/Core/Models/Cards/shine/KarenSwordUp.cs)<br>`KarenSwordUp` | P2 | 原生单击后虚弱和易伤。 | 一条向上挑击主形，两个减益只脉冲真实图标；复用小刀光材质。 |
| [迈向那个舞台](../../src/Core/Models/Cards/shine/KarenToTheStage.cs)<br>`KarenToTheStage` | P3 | 回能3，升级再抽1张，闪耀3。 | 保留快节奏回能；升级抽牌从能量落点自然接原生卡面，不新增固定等待。 |

### 3.3 约定牌堆（33）

| 卡牌 / 源码 | 优先级 | 当前表现 | 优化建议 |
| --- | --- | --- | --- |
| [水族馆](../../src/Core/Models/Cards/promisePile/KarenAquariumCard.cs)<br>`KarenAquariumCard` | P2 | Power有常驻水族馆、回合抽牌及入塔选择阶段。 | 保留抽取→选择→入塔阶段；相邻同类回合触发复用水面和边界，批量卡只增加交接层。 |
| [背靠背](../../src/Core/Models/Cards/promisePile/KarenBackToBackCard.cs)<br>`KarenBackToBackCard` | P2 | 0.34秒准备后撞击，强度取塔内牌数。 | 保留现有接近/接触结构；按数量封顶亮度和散点，接近层与命中层共用一次性缓冲。 |
| [Banana午餐](../../src/Core/Models/Cards/promisePile/KarenBananaLunch.cs)<br>`KarenBananaLunch` | P3 | Power在回合末向塔生成三明治。 | 沿用原生Power闪烁和生成牌入塔；大量生成共用一圈出生光。 |
| [棒球](../../src/Core/Models/Cards/promisePile/KarenBaseball.cs)<br>`KarenBaseball` | P2 | 0.35秒球路接触，命中后施加四种保留效果。 | 保留球路；四个能力各有真实图标确认，合并为一次角色局部收尾，避免四轮大演出。 |
| [约定之塔桥](../../src/Core/Models/Cards/promisePile/KarenBridge.cs)<br>`KarenBridge` | P1 | 塔桥作用域与逐张聚光；有牌时固定前摇Fast0.30/普通0.48秒。 | 保留整串一次塔桥；高X只保留逐张接触闪光，统一速度策略核查关闭档前摇。 |
| [勇气打击](../../src/Core/Models/Cards/promisePile/KarenCourageStrike.cs)<br>`KarenCourageStrike` | P2 | 群体斩击后生成侧身入塔。 | 共享一次横向起势，每个实际命中保留局部反馈；生成侧身的出生点落在塔边。 |
| [舞动](../../src/Core/Models/Cards/promisePile/KarenDance.cs)<br>`KarenDance` | P2 | 斩击后从塔选择2/4张回手。 | 将视觉重点放在选择与归手；刀光保持短，升级批量共享归手光带。 |
| [闪避](../../src/Core/Models/Cards/promisePile/KarenDodge.cs)<br>`KarenDodge` | P3 | 格挡后生成对峙入塔。 | 保留轻量盾形和真实生成牌；可让入塔星短偏转以表达闪避，无新增等待。 |
| [命运交换之日](../../src/Core/Models/Cards/promisePile/KarenExchangeFate.cs)<br>`KarenExchangeFate` | P2 | 两组卡牌快照、交叉标记等待、实际SwitchHand，finally收尾。 | 保留已有交叉事件；卡量增加时共享丝带网格并限制装饰采样，真实卡面逐张交接。 |
| [神圣星辰](../../src/Core/Models/Cards/promisePile/KarenHolyStar.cs)<br>`KarenHolyStar` | P2 | 原生两击；每次入塔降费。 | 第二击仅加短收束，入塔后用费用角标短亮表达降费；两段共用一份准备。 |
| [落地](../../src/Core/Models/Cards/promisePile/KarenLanding.cs)<br>`KarenLanding` | P2 | 群体斩击后生成回击入塔。 | 可共用一次低位冲击起势，命中各敌人局部确认；落地与勇气打击用方向区分。 |
| [信封](../../src/Core/Models/Cards/promisePile/KarenLetterCard.cs)<br>`KarenLetterCard` | P2 | Power每张入塔时给格挡并Flash。 | 同批入塔只强化一次图标/盾形，卡牌自身入塔尾迹保留；与添麻烦了共用视觉预算。 |
| [重逢](../../src/Core/Models/Cards/promisePile/KarenMeetAgain.cs)<br>`KarenMeetAgain` | P2 | 格挡→抽2张→选1张入塔。 | 保留操作顺序；共享选牌起势与塔落点，让刚抽到的真实卡面自然接入选择。 |
| [新的一天](../../src/Core/Models/Cards/promisePile/KarenNewDay.cs)<br>`KarenNewDay` | P3 | 格挡、施加下一次牌入塔能力，自身入塔。 | 保留自身入塔和下一次入塔的区别；使用同一结果交接层，不为每步重复起幕。 |
| [观察情况](../../src/Core/Models/Cards/promisePile/KarenNewSituation.cs)<br>`KarenNewSituation` | P3 | 格挡后生成1/2张三明治入塔。 | 加强真实生成卡短亮即可；升级两张共享一次出生光。 |
| [不再犹豫](../../src/Core/Models/Cards/promisePile/KarenNoHesitate.cs)<br>`KarenNoHesitate` | P2 | 选择手牌/抽牌/弃牌来源，再选牌入塔，空虚时来源减少。 | 来源选项保持简洁；选中后复用入塔家族，并按实际模式更新锚点。 |
| [命运与本能](../../src/Core/Models/Cards/promisePile/KarenOnStage.cs)<br>`KarenOnStage` | P2 | 双人短演出，逐张取塔牌，最后确认能量。 | 保留逐张取回与能量分开确认；取牌批次共用准备，末张和能量到达后统一收尾。 |
| [开拓星辰](../../src/Core/Models/Cards/promisePile/KarenOpenUpStars.cs)<br>`KarenOpenUpStars` | P2 | 入塔施加群体虚弱，出塔施加群体易伤。 | 只在对应Hook成功后给目标图标小脉冲；入/出方向色区分，避免打出时提前演出。 |
| [我们的约定](../../src/Core/Models/Cards/promisePile/KarenOurPromise.cs)<br>`KarenOurPromise` | P2 | 抽3/4张，再选2张入塔。 | 共享抽牌/选择的背景层；升级后让实际新增卡进入原生选择，不另复制装饰卡面。 |
| [过去与未来](../../src/Core/Models/Cards/promisePile/KarenPastAndFuture.cs)<br>`KarenPastAndFuture` | P2 | 模式维持穿身环，临时力量及抽牌有共享表现。 | 保留已确定的右向左等大穿身环；多次触发用限量实例/同帧脉冲，位置跟随角色。 |
| [极速下降](../../src/Core/Models/Cards/promisePile/KarenRapid.cs)<br>`KarenRapid` | P2 | 保留、消耗；选择放入或取回。 | 两分支用现有入塔/回手方向表达；共用一次选择框，取消及时清除预览。 |
| [RevueDuet](../../src/Core/Models/Cards/promisePile/KarenRevueDuet.cs)<br>`KarenRevueDuet` | P2 | 0费斩击后自身入塔，排除新的一天改流。 | 按轻击家族处理；刀光结束和真实自身入塔相接，重复自动打出只保留接触与落点。 |
| [行动](../../src/Core/Models/Cards/promisePile/KarenRun.cs)<br>`KarenRun` | P2 | 抽牌后自身入塔。 | 保持即时抽牌，牌面入塔直接接原生打出卡；批量连锁共用背景光路。 |
| [Banana什锦](../../src/Core/Models/Cards/promisePile/KarenSmallSnack.cs)<br>`KarenSmallSnack` | P2 | 施放临时力量；在塔回合末变成晚餐并确认生成卡。 | 保留变形源卡身份与新卡确认；多张什锦同时变化时复用锅/蒸汽模板并控制重叠。 |
| [舞台正在等待着](../../src/Core/Models/Cards/promisePile/KarenStageIsWaiting.cs)<br>`KarenStageIsWaiting` | P2 | Power维持幕布、牌堆状态和能量结果；收益脉冲/音已简化。 | 延续当前简化：施放保留起幕，回能只强调真实图标，静态幕布降低重绘。 |
| [命运舞台的再生产](../../src/Core/Models/Cards/promisePile/KarenStageReproduce.cs)<br>`KarenStageReproduce` | P2 | 进入无限增援模式后播放塔脚走灯与生成续演。 | 一次建立常驻灯组；后续补牌仅触发对应灯/星，预算随可见装饰而非累计卡数增长。 |
| [拉伸](../../src/Core/Models/Cards/promisePile/KarenStretching.cs)<br>`KarenStretching` | P3 | 格挡后自身入塔。 | 保留较稳的防御节拍；自身入塔复用共用收束，零独立全屏层。 |
| [门票](../../src/Core/Models/Cards/promisePile/KarenTicket.cs)<br>`KarenTicket` | P2 | 补丁起票，等待短cue后给Power；回合开始生成约定之塔。 | 保留补丁单一施放入口，回合生成只做短交接；票根到能力图标的结果保持同一节点身份。 |
| [东京塔下](../../src/Core/Models/Cards/promisePile/KarenUnderTower.cs)<br>`KarenUnderTower` | P3 | 格挡后真实生成约定之塔入手。 | 先盾形再卡面入手即可；重用原生生成卡预览和塔主题边缘。 |
| [世界上最空虚的人](../../src/Core/Models/Cards/promisePile/KarenVoid.cs)<br>`KarenVoid` | P1 | 删除抽牌堆、旧塔退场、约定牌批量转到抽牌堆，skipVisuals依质量档。 | 优先核查各档的真实卡面交接与完成释放；大牌堆用单次扫光、共享变形与有界装饰。 |
| [两人的梦想](../../src/Core/Models/Cards/promisePile/KarenWeKnow.cs)<br>`KarenWeKown` | P2 | 梦想短演出给Power，回合末生成光辉并等待交付。 | 保留已有静音与短演出；多张光辉以同一生成批次共享星轨，完成仍核对真实模型。 |
| [双人舞](../../src/Core/Models/Cards/promisePile/KarenWhosPromise.cs)<br>`KarenWhosPromise` | P2 | 攻击→取塔牌→选手牌入塔。 | 突出一出一入的方向交换；两段复用共用轨迹材质，选择期间只留轻背景。 |
| [昨日夜空](../../src/Core/Models/Cards/promisePile/KarenYesterglow.cs)<br>`KarenYesterglow` | P2 | 攻击后选择弃牌入塔，升级增加选择数量。 | 来源落在弃牌图标与选中卡面之间；升级批量共用一次起势和落点脉冲。 |

### 3.4 通用与模式（10）

| 卡牌 / 源码 | 优先级 | 当前表现 | 优化建议 |
| --- | --- | --- | --- |
| [Banana蛋糕](../../src/Core/Models/Cards/neutral/KarenBananaCake.cs)<br>`KarenBananaCake` | P2 | 蛋糕短演出、药水来源登记、按获取成功确认。 | 保留无附加音约定；药水槽已满时用结束包络收束，材质/粒子模板和晚餐共用。 |
| [燃烧吧燃烧吧](../../src/Core/Models/Cards/neutral/KarenBurn.cs)<br>`KarenBurn` | P2 | 进入Burn模式，表现由塔/卡缘共享系统驱动。 | 保留现有火焰形状；将常驻火焰更新、入塔反馈和抽出牌缘分别计预算，低档优先保留轮廓。 |
| [以觉悟的名义](../../src/Core/Models/Cards/neutral/KarenConsciousness.cs)<br>`KarenConsciousness` | P2 | 斩击后按实际伤害结果获得格挡。 | 用目标接触后到角色盾形的短局部回流表达转换；确认格挡后收尾，长度固定上限。 |
| [添麻烦了](../../src/Core/Models/Cards/neutral/KarenCry.cs)<br>`KarenCry` | P2 | OnPlay为空，每次全局移牌给格挡。 | 高频收益提示合并同帧显示，规则仍逐次结算；与信封同时触发时复用盾形脉冲。 |
| [觉醒形态](../../src/Core/Models/Cards/neutral/KarenForm.cs)<br>`KarenForm` | P2 | Power维持风场、抽牌脉冲与战斗音乐。 | 保留已确认风场与音乐交接；静止阶段降低装饰采样，抽牌脉冲共用当前节点。 |
| [命运的齿轮](../../src/Core/Models/Cards/neutral/KarenGeer.cs)<br>`KarenGeer` | P1 | 施放固定等1秒；回合触发每层等0.65秒并选牌。 | 先统一游戏速度下的等待策略，再调整起势/尾部；减少同时绘制的背景与角色齿轮重叠。 |
| [最后的台词](../../src/Core/Models/Cards/neutral/KarenLastWord.cs)<br>`KarenLastWord` | P1 | 本地视频优先；低档文字沿用6.7秒，缺视频文字段0.7秒。 | 先核查主客机与不同本地质量档的规则等待一致性；视频和文字共用语义命中点，画面单独降级。 |
| [约定的那天](../../src/Core/Models/Cards/neutral/KarenOldPlace.cs)<br>`KarenOldPlace` | P3 | 格挡后选择保留格挡/能量/力量/手牌。 | 保持原生选择；四项用原有图标和选中脉冲表达，统一复用选择装饰。 |
| [招架](../../src/Core/Models/Cards/neutral/KarenParry.cs)<br>`KarenParry` | P2 | 在手牌期间随进出手牌增长格挡，打出/回合末重置。 | 一次移牌批次内合并数值脉冲；重置时仅清数值强调，避免额外全屏反馈。 |
| [Position 0](../../src/Core/Models/Cards/neutral/KarenPosition0.cs)<br>`KarenPosition0` | P2 | Power维持暖光、脚灯、格挡确认、卡顶T标记。 | 保留标记位置和现有身份校验；静态T几何缓存，多段攻击的脚灯脉冲设重叠预算。 |

### 3.5 遗物与禁用遗物（7）

| 卡牌 / 源码 | 优先级 | 当前表现 | 优化建议 |
| --- | --- | --- | --- |
| [骄傲](../../src/Core/Models/Cards/relic/KarenArrogant.cs)<br>`KarenArrogant` | P2 | 舞台释放后按实际易伤和奖励分别确认，异常Abort。 | 保留两段真实确认；背景灯与奖励亮点分预算，和常驻舞台合成时限制大面积亮度。 |
| [Banana费南雪](../../src/Core/Models/Cards/relic/KarenFinancier.cs)<br>`KarenFinancier` | P3 | 力量和临时力量保留能力。 | 共用小型力量图标反馈，触发次数变化时只更新数量；保持低开销。 |
| [嫉妒](../../src/Core/Models/Cards/relic/KarenJealousy.cs)<br>`KarenJealousy` | P2 | 无攻击动画，按专属接触时点播放方形/抽取反馈，耗尽登记遗物。 | 保留当前方形冲击语言；集中优化接触层复用与耗尽标记，避免每次攻击完整演出战后奖励。 |
| [激情](../../src/Core/Models/Cards/relic/KarenPassion.cs)<br>`KarenPassion` | P2 | 施放粒子后登记遗物，能力获得另有确认。 | 保留两段独立生命周期与每玩家上限；共享材质，收益只在真实确认时强调。 |
| [摘星](../../src/Core/Models/Cards/relic/KarenPickStar.cs)<br>`KarenPickStar` | P2 | 遗物禁用吸收落到能量图标，再实际回能。 | 按整个禁用批次共享吸收准备，能量显示以实际增量为准；使用当前图标锚点。 |
| [双星](../../src/Core/Models/Cards/relic/KarenShinePrice.cs)<br>`KarenShinePrice` | P2 | X个遗物形成成对双星，逐次真实命中发射，BeforeDamage等接触。 | 保留成对节拍和局部接触；高X限制同时尾迹/星对缓存，远端用实际目标附近的回退锚点。 |
| [星罪](../../src/Core/Models/Cards/relic/KarenStarCrime.cs)<br>`KarenStarCrime` | P2 | 遗物吸收落抽牌图标，再实际抽3张。 | 抽牌批次共用吸收和落点，先图标接触再抽牌；保持真实卡面交接。 |

### 3.6 临时力量（2）

| 卡牌 / 源码 | 优先级 | 当前表现 | 优化建议 |
| --- | --- | --- | --- |
| [Banana松饼](../../src/Core/Models/Cards/strength/KarenBananaMuffin.cs)<br>`KarenBananaMuffin` | P3 | 临时力量后抽牌，原生能力/抽牌反馈。 | 保持即时；可共用小型力量图标确认，临时与永久收益用颜色/寿命区分。 |
| [一起吃饭吧](../../src/Core/Models/Cards/strength/KarenEatTogether.cs)<br>`KarenEatTogether` | P3 | 临时力量和保留临时力量两个能力。 | 复用能力图标脉冲；把两个确认合成同次短动作，保持两个规则施加入口。 |

### 3.7 全局移牌（3）

| 卡牌 / 源码 | 优先级 | 当前表现 | 优化建议 |
| --- | --- | --- | --- |
| [下一个舞台](../../src/Core/Models/Cards/globalMove/KarenNextStage.cs)<br>`KarenNextStage` | P2 | 入塔/离塔或升级后的全局移牌触发抽牌。 | 把高频抽牌归入同一移牌批次的短反馈；原生逐次抽牌规则保留，视觉只标最后的落点。 |
| [旋转](../../src/Core/Models/Cards/globalMove/KarenSpin.cs)<br>`KarenSpin` | P2 | 原生斩击；每次移牌增长伤害并写Info日志。 | 数字成长用同帧一次短脉冲；逐移牌日志可改调试级，重击大小用封顶映射。 |
| [唤醒](../../src/Core/Models/Cards/globalMove/KarenWakeUp.cs)<br>`KarenWakeUp` | P2 | 选择幕帘、各来源选项与真实归手尾迹；Instant保留选择反馈。 | 保留当前Instant约定；缓存选牌边界，批量回手共享装饰层，取消时快速退幕。 |

### 3.8 Ancient（1）

| 卡牌 / 源码 | 优先级 | 当前表现 | 优化建议 |
| --- | --- | --- | --- |
| [为什么要离开？我的朋友](../../src/Core/Models/Cards/ancient/KarenWhy.cs)<br>`KarenWhy` | P2 | 格挡后在入塔选牌期间挂边缘流光，finally结束。 | 沿用当前选牌层级；窗口缩放/Peek时更新边界，静止时缓存装饰几何。 |

### 3.9 衍生牌（10）

| 卡牌 / 源码 | 优先级 | 当前表现 | 优化建议 |
| --- | --- | --- | --- |
| [banana晚餐](../../src/Core/Models/Cards/token/KarenBanana.cs)<br>`KarenBanana` | P2 | 晚餐演出后永久力量，按真实增量确认。 | 和什锦共用美术/材质，通过结尾持久亮点区分永久收益，保留现有晚餐音。 |
| [对峙](../../src/Core/Models/Cards/token/KarenConfront.cs)<br>`KarenConfront` | P2 | 单敌3/4击原生斩击，消耗。 | 一份短准备配三拍接触，与勇气打击生成链共用主题资源。 |
| [续演](../../src/Core/Models/Cards/token/KarenContinue.cs)<br>`KarenContinue` | P1 | 每次命中实例化原生飞刀并复制多份粒子材质、添加火屑。 | 高频自动打出优先复用不可变材质和粒子模板；按低档/连锁预算保留刀体与接触。 |
| [回击](../../src/Core/Models/Cards/token/KarenCounter.cs)<br>`KarenCounter` | P2 | 全敌固定3击原生斩击，消耗。 | 共用群攻起势与逐击局部反馈；敌人多时限制同时散点，原生击数保留。 |
| [空壳](../../src/Core/Models/Cards/token/KarenEmptyShell.cs)<br>`KarenEmptyShell` | P3 | 0费消耗占位牌，无额外OnPlay效果。 | 保留简洁卡面和原生消耗；使用静态低亮轮廓区分占位。 |
| [三明治](../../src/Core/Models/Cards/token/KarenSandwitch.cs)<br>`KarenSandwitch` | P3 | 施加临时力量后消耗。 | 共用力量图标短确认，和晚餐用短寿命/图标区分。 |
| [闪耀的再生产](../../src/Core/Models/Cards/token/KarenShineReproduce.cs)<br>`KarenShineReproduce` | P2 | 克隆耗尽区候选供选择，再生成牌组副本与手牌。 | 复用选择与生成卡表现；候选网格只用静态卡缘，选定后的真实双去向分开确认。 |
| [侧身](../../src/Core/Models/Cards/token/KarenSideways.cs)<br>`KarenSideways` | P3 | 0费格挡6/12后消耗。 | 保留即时盾形；作为轻防御模板，避免为生成牌额外起幕。 |
| [约定之塔](../../src/Core/Models/Cards/token/KarenTowerOfPromise.cs)<br>`KarenTowerOfPromise` | P1 | 塔回收作用域，能抽时固定Fast0.18/普通0.48秒，再补满手牌。 | 保留整批一次塔起势；降低大批量拖尾开销，核对满手/空塔/关闭档的统一等待与交接。 |
| [光辉](../../src/Core/Models/Cards/token/KarenWeKonwToken.cs)<br>`KarenWeKownToken` | P2 | 光辉演出回能，确认增量后等待能量离开卡面。 | 保留真实卡面身份交接；多次光辉共用回能材质、减少尾部重叠，保持当前静音。 |

### 3.10 来源与保留选项（15）

| 卡牌 / 源码 | 优先级 | 当前表现 | 优化建议 |
| --- | --- | --- | --- |
| [弃牌堆](../../src/Core/Models/Cards/token/options/KarenAwakePotionDiscardPileOption.cs)<br>`KarenAwakePotionDiscardPileOption` | P3 | 药水弃牌堆选择动作。 | 复用药水选择和真实归手；本选项保持静态图标/名称。 |
| [抽牌堆](../../src/Core/Models/Cards/token/options/KarenAwakePotionDrawPileOption.cs)<br>`KarenAwakePotionDrawPileOption` | P3 | 药水抽牌堆选择动作。 | 复用抽牌堆选择，选项只保留来源区别；生成候选时共享装饰层。 |
| [约定牌堆](../../src/Core/Models/Cards/token/options/KarenAwakePotionPromisePileOption.cs)<br>`KarenAwakePotionPromisePileOption` | P3 | 药水约定牌堆选择动作。 | 复用塔选牌与归手轨迹，实际模式锚点由现有命令提供。 |
| [弃牌堆](../../src/Core/Models/Cards/token/options/KarenNoHesitateDiscardPileOption.cs)<br>`KarenNoHesitateDiscardPileOption` | P2 | 从弃牌堆选2/3张入塔。 | 选中真实卡面才起入塔光，批次共用尾迹材质和一次塔落点。 |
| [抽牌堆](../../src/Core/Models/Cards/token/options/KarenNoHesitateDrawPileOption.cs)<br>`KarenNoHesitateDrawPileOption` | P2 | 从抽牌堆选2/3张入塔。 | 沿用原生网格；选中后用来源图标到塔的方向，保持模型身份。 |
| [手牌](../../src/Core/Models/Cards/token/options/KarenNoHesitateHandOption.cs)<br>`KarenNoHesitateHandOption` | P2 | 从手牌选2/3张入塔。 | 保留原生手牌选择，批量同帧共用起势，避免每张重复背景闪光。 |
| [保留格挡](../../src/Core/Models/Cards/token/options/KarenOldPlaceRetainBlockOption.cs)<br>`KarenOldPlaceRetainBlockOption` | P3 | 选择后施加BlurPower保留格挡。 | 格挡图标短确认即可，复用选择结束包络。 |
| [保留能量](../../src/Core/Models/Cards/token/options/KarenOldPlaceRetainEnergyOption.cs)<br>`KarenOldPlaceRetainEnergyOption` | P3 | 选择后施加保留能量Power。 | 能量图标短确认，与实际能量增加分开表达。 |
| [保留手牌](../../src/Core/Models/Cards/token/options/KarenOldPlaceRetainHandOption.cs)<br>`KarenOldPlaceRetainHandOption` | P3 | 选择后施加保留手牌Power。 | 手牌边缘短静光即可，避免改变卡牌输入或原生高亮所有权。 |
| [保留力量](../../src/Core/Models/Cards/token/options/KarenOldPlaceRetainStrengthOption.cs)<br>`KarenOldPlaceRetainStrengthOption` | P3 | 选择后施加保留临时力量Power。 | 力量能力图标短确认，复用现有临时力量主题。 |
| [放入](../../src/Core/Models/Cards/token/options/KarenRapidPutOption.cs)<br>`KarenRapidPutOption` | P2 | 选2/3张手牌入塔。 | 复用不再犹豫手牌分支；本选项不单独新建演出系统。 |
| [取回](../../src/Core/Models/Cards/token/options/KarenRapidTakeOption.cs)<br>`KarenRapidTakeOption` | P2 | 选2/3张塔牌回手。 | 复用舞动回手分支，保持同样的选中/落点表达。 |
| [弃牌堆](../../src/Core/Models/Cards/token/options/KarenWakeUpDiscardPileOption.cs)<br>`KarenWakeUpDiscardPileOption` | P2 | 弃牌堆选2张，Observe真实回手。 | 沿用父级幕帘；选中后共用归手尾迹，输入结束立即放开背景持有。 |
| [抽牌堆](../../src/Core/Models/Cards/token/options/KarenWakeUpDrawPileOption.cs)<br>`KarenWakeUpDrawPileOption` | P2 | 抽牌堆选2张，Observe真实回手。 | 与未升级唤醒共用全部表现；缓存布局，来源图标独立定位。 |
| [约定牌堆](../../src/Core/Models/Cards/token/options/KarenWakeUpPromisePileOption.cs)<br>`KarenWakeUpPromisePileOption` | P2 | 约定牌堆选2张，Observe真实回手。 | 保留塔库存星到真实卡面的交接，空虚模式复用正确来源锚点。 |

### 3.11 诅咒（2）

| 卡牌 / 源码 | 优先级 | 当前表现 | 优化建议 |
| --- | --- | --- | --- |
| [困意](../../src/Core/Models/Cards/curses/KarenSleepy.cs)<br>`KarenSleepy` | P3 | 不可打出、虚无、永久，依赖原生卡牌状态。 | 保留原生状态反馈；虚无离场复用共用清理，卡面仅静态区分。 |
| [存在于舞台的理由](../../src/Core/Models/Cards/curses/KarenStageReason.cs)<br>`KarenStageReason` | P3 | 可打出的诅咒，闪耀3，无额外OnPlay规则。 | 强调闪耀计数与耗尽入口即可，沿用共享卡缘和耗尽，保持低装饰量。 |

### 3.12 暂时移出主卡池（2）

| 卡牌 / 源码 | 优先级 | 当前表现 | 优化建议 |
| --- | --- | --- | --- |
| [披萨](../../src/Core/Models/Cards/neutral/KarenPizza.cs)<br>`KarenPizza` | 暂缓 | 暂时移出主卡池；自方加力量、敌方减力量。 | 恢复进池后再做收益/减益两端的局部反馈；目前保留原生能力表现。 |
| ["宽恕"](../../src/Core/Models/Cards/relic/KarenForgive.cs)<br>`KarenForgive` | 暂缓 | 暂时移出主卡池；禁用遗物后群攻。 | 复用禁用遗物吸收与群体斩击家族，恢复进池时再设计独立主题。 |

## 4. 建议实施顺序与验收

本次共 **116** 个逐卡条目：主卡池 **87**、衍生/选项 **25**、诅咒 **2**、暂时移出主卡池 **2**。优先级分布：P1 **16**、P2 **67**、P3 **31**、暂缓 **2**。

| 顺序 | 最小实施范围 | 完成时检查 |
| --- | --- | --- |
| 1 | 基础五张牌专属表现，先完成打击/防御/闪耀打击 | 三种主形、独立入口、真实确认、低档辨识与原生关闭回退 |
| 2 | 永不遗忘的闪耀：伤痕固定预算、位置查询有界、节点复用 | 连击后保留数量稳定，新痕清楚，换场释放，低档更少 |
| 3 | 续演：不可变材质/粒子模板复用；续写与Star的连锁装饰预算 | 每次真实命中仍有接触，准备只按作用域建立，末次奖励准确 |
| 4 | 齿轮/燃料/塔桥/约定之塔/台词的等待策略审阅 | 列明共享规则时钟与本地画面时钟，主客机不同质量配置定向验证 |
| 5 | 星光指引/空虚/回塔家族的批量绘制优化 | MCP前后截图，动画后karen_check_hand，真实节点与模型一致 |
| 6 | 普通攻击、收益和其他牌按分批计划逐项完成 | 每张可打出牌有独立表现配置或控制器，常用牌保持即时 |

这些是后续实施候选，当前任务交付为逐卡分析。美术新增前先用现有形状、纹理、Shader和布局做小范围对照；主形和节奏确认后再添加素材。

## 5. 范围、证据与校验

- 源码范围：`src/Core/Models/Cards/` 的具体类，排除基类/扩展工具；核对 `KarenCardPool`、Token/Curse注册及中文本地化。
- 机器清单：[逐卡结构化数据](card-vfx-review-2026-10-29.json)，保存每张牌的类名、来源、源码SHA-256、直接表现标识、当前判断与建议。SHA-256采用UTF-8解码并统一换行后的源码内容，便于跨工作树复查。
- 中文名以当前 `ShoujoKagekiAijoKaren/localization/zhs/cards.json` 为准；同名选项按类名逐项保留。
- 共用机制参考：[main特效总览](../21.特效总览与兼容约定.md)、[制作经验](../15.卡牌特效制作经验.md)、[首次使用登记](../../tools/vfx/first-use/README.md)。
- 工坊技术参考：[瓦伦希娜](https://steamcommunity.com/sharedfiles/filedetails/?id=3747747045)，前序研究包版本0.10.0；本报告以华恋当前源码为判断基线。

| 检查 | 本次状态 |
| --- | --- |
| 当前具体卡牌类与报告一一对应 | 通过，116个，无重复/遗漏 |
| 正式/衍生/诅咒/停用范围核对 | 通过，87/25/2/2 |
| 卡名、源码路径、报告本地链接、源码指纹 | 逐项静态核对 |
| 卡牌代码、数值、资源、玩法改动 | 本次无改动 |
| 编译、游戏启动 | 未执行，本次仅文档与审阅数据 |
| 逐卡录像、帧耗时、首帧/重复帧、主客机专项 | 未测，按实际实施项定向安排 |
| 完整单人/多人矩阵与完整特效专项 | 未执行，后续按用户要求开展 |

任务记录：[card-vfx-audit-20261029](../worktree-tasks/card-vfx-audit-20261029/README.md)。
