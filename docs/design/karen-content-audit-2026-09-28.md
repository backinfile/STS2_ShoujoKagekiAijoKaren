# Karen 实装内容盘点与补齐建议

日期：2026-09-28；遗物规划更新于 2026-10-10。以当前工作区源码、显式卡池和中文本地化为依据。本文为静态设计盘点，不代表双版本运行、掉落概率或平衡性已经实测。补齐数量是设计建议，不是原版强制配额。

## 结论（按原版总牌数一致要求修订）

完整原版结构核对见[原版卡池结构与等量补齐方案](vanilla-pool-parity-2026-09-28.md)。正式版五职业常规池均82张，测试版均85张，两版单人常规池均80张；Karen当前分别为80、80、78。

**卡牌改为：两版共同补1张罕见＋1张稀有通用牌；仅测试版再补2张罕见＋1张稀有多人专属牌。普通牌不净增。** 下文原先的6个功能位及其原版参照保留为设计候选，不再作为净新增清单；普通方向通过重做或等量替换实现。

遗物按双版本原版五职业结构规划新增 7 件：普通 1、罕见 2、稀有 3、商店 1。目前 7 件设计均已确认，具体效果见“遗物”小节；这些新增遗物尚未实装。药水暂不必新增。

## 统计口径与现状

- 具体卡牌类 115 个：卡池内 86 个，池外 29 个。
- 卡池内：基础 4、普通 20、罕见 35、稀有 25、Ancient 2。
- 常规稀有度合计 80 张；其中《歌唱吧舞蹈吧互相争斗吧》《一起闪耀吧》仅限多人，因此单人候选为 78 张（普通 20、罕见 34、稀有 24）。这是源码候选数量，不是实际奖励抽样结果。
- Ancient 2 张是《Position 0》《为什么要离开？我的朋友》，不计入常规奖励密度。
- 池外：暂时移除的《披萨》《“宽恕”》2 张、诅咒 2 张、实体 Token 10 张、选项 Token 15 张。
- 起始卡组 10 张：打击×4、防御×4、闪耀打击×1、坠落×1；只有 4 种基础卡。
- 《约定之塔桥》是 **X 费**，不能按构造函数中的 0 统计。
- 费用均指未升级、未受战斗修正的费用。《下一个舞台》虽为 3 费，但主要靠移动触发，不应当作普通的高费打出收益牌。

| 稀有度 | 攻击 | 技能 | 能力 | 合计 |
|---|---:|---:|---:|---:|
| 普通 | 11 | 9 | 0 | 20 |
| 罕见 | 8 | 20 | 7 | 35 |
| 稀有 | 6 | 7 | 12 | 25 |
| 合计 | 25 | 36 | 19 | 80 |

| 稀有度 | 0 费 | 1 费 | 2 费 | 3 费 | X 费 |
|---|---:|---:|---:|---:|---:|
| 普通 | 3 | 16 | 1 | 0 | 0 |
| 罕见 | 11 | 20 | 3 | 1 | 0 |
| 稀有 | 2 | 7 | 8 | 7 | 1 |
| 合计 | 16 | 43 | 12 | 8 | 1 |

80 张中，0～1 费占 73.75%；普通牌里 16/20 为 1 费，唯一的 2 费普通牌是《体操练习》，没有 2 费普通攻击。费用集中本身不必修正；只有存在功能缺口时才值得补高费牌。

普通牌中 8 张自带闪耀；常规池中共有 22 张在构造时初始化闪耀。目录不等同机制：例如《以觉悟的名义》《嫉妒》《激情》《不再犹豫》也有闪耀。不要只统计 shine 文件夹。

## 卡牌功能判断

### 已经比较充足的部分

1. **约定牌堆的存取与运转。** 《我们的约定》《重逢》《舞动》《不再遥不可及》《极速下降》《不再犹豫》覆盖放入、抽出、定向取牌。《水族馆》《信封》《命运与本能》《舞台正在等待着》提供持续收益。无需继续大量添加同质化存取牌。
2. **能量与过牌。** 已有普通《迈向那个舞台》（0 费获得 3 能量，闪耀 6）、罕见《命运与本能》、稀有《投下燃料》，以及《Starlight第一幕》等。不能仅因为手感卡顿就直接补另一张 0 费抽牌回能牌，应先区分缺能量、缺入口还是卡牌没抽到。
3. **群攻与多段攻击。** 普通《出场》《勇气打击》，罕见《落地》，以及《用你的闪耀贯穿我吧》《神圣星辰》等已有基础覆盖；没有必要为了“补群攻”机械新增。
4. **稀有构筑终端。** 《觉醒形态》《命运的齿轮》《世界上最空虚的人》《命运舞台的再生产》《Starlight第二幕》《最后的台词》等已有多条改变玩法的路线。25 张稀有中 12 张为能力，继续补大型能力牌优先级很低。
5. **成长和续航不是空白。** 有闪耀牌奖励、获取遗物、《我们犯下的罪过》《旋转》《过去与未来》，也有《土豆》《武术练习》。不能把“需要铺垫”误判为“没有成长或恢复”。

### 更值得补的部分

- **不依赖闪耀的普通防御与减伤。** 《重逢》《观察情况》已经能长期使用，《东京塔下》提供一次性防御和取牌；因此不是没有防御，而是稳定防御的风格选择偏少。《武术练习》《体操练习》有闪耀，持续循环又往往依赖罕见牌《招架》《信封》《舞蹈练习》。建议补在这里，而不是再做一张无限收益能力。
- **普通 2 费攻击。** 第一幕选择可以出现“用更多能量换可靠伤害”的选项，且在闪耀牌耗尽后仍留下一张稳定输出牌。
- **移牌收益的兑现。** 《旋转》《背靠背》已经提供成长输出，《开拓星辰》提供减益。新牌需要形成有区别的打出时机，而非再做一个按移动次数无限抽牌/回能的引擎。
- **禁用遗物的收益类型窄。** 《摘星》《星罪》都以禁用 2 个遗物换资源，《“宽恕”》已移出池。若保留这条路线，缺少只禁用 1 个遗物就能换取即时防御的中间选择。新增不应把禁用成本退款，否则失去取舍。

## 历史提出的 6 个功能位（不代表净新增，见修订结论）

下表是设计规格，不是已确定卡名或最终数值。先确认功能，再定伤害、格挡和升级幅度；新增普通牌优先不带闪耀、不消耗，保留 Karen 原有闪耀牌的高效率优势。

| 优先级 | 数量 | 稀有度 | 费用 / 类型 | 功能与边界 |
|---|---:|---|---|---|
| P1 | 1 | 普通 | 2 / 攻击 | 可靠单体伤害，基础效果无需约定牌堆或能力启动；提供普通池缺少的高投入攻击选择，不附加抽牌回能 |
| P1 | 1 | 普通 | 1 / 技能 | 可靠基础格挡；约定牌堆非空时小额追加格挡。检查牌堆状态即可，不再制造来回移牌 |
| P1 | 1 | 普通 | 1 / 攻击 | 中低伤害＋短时虚弱，不带闪耀；与《上挑》的区别是长期稳定，不能同时保留其双减益和高效率 |
| P1 | 1 | 普通 | 2 / 技能 | 持续可用的大格挡；数值效率低于闪耀《体操练习》，打出后正常弃置，与《拉伸》的约定回收定位区分 |
| P2 | 1 | 罕见 | 1 / 技能 | 禁用 1 个遗物换较强即时格挡；不返还能量、不再送遗物，保留禁用代价。先验证玩家是否愿意采用这条路线 |
| P2 | 1 | 罕见 | 2 / 攻击 | 按本回合约定牌堆实际取出次数追加伤害，设较低上限，打出时一次结算；与《背靠背》的库存数量、《旋转》的整场成长区分 |

原“全部新增6张”的方案已撤回：其常规总量86张与正式版82、测试版85均不一致，且普通牌超过原版20张。费用和类型也不再作为机械补齐指标。

《披萨》《“宽恕”》不要直接计入新增或恢复计划：前者会增加力量与降力量，后者会扩展禁用遗物群攻；应先明确当初移出原因，再决定是否用重做代替新增。

## 六个补牌方向的原版职业参照

核对日期：2026-09-28。以下直接读取本项目独立开发环境正式版 v0.107.1、测试版 v0.111.0 的 sts2.dll 与 SlayTheSpire2.pck 内中文 cards.json；不使用一代数值。职业归属同时对照本地反编译 CardPool 清单。

“原本描述”指未升级、无外部加成的中文文本：将本地化动态变量填入代码基础值，去除颜色标记，略去仅战斗中显示的实时计数。关键字若由代码单独提供则另列。除《牺牲》注明的差异外，下列牌两版的基础费用、稀有度和所列效果一致。

### 1. 普通 2 费可靠单体攻击 → 储君《天穹之力》

原版：2 费，普通，攻击。

> 造成6点伤害3次。

升级：改为 4 次，即基础总伤害 18 → 24。

对应定位：无须先启动机制，花 2 能量即可获得足够的即时单体伤害。这是六项中较直接的同费用、同稀有度参照。

Karen 的设计边界：借鉴稳定输出定位即可，不必照搬三连击。Karen 已有临时力量和多段攻击，如果新卡也做多段，力量收益会再次集中；单次较高伤害更容易产生不同用途。18 点总伤害不能直接当作单次攻击的等价数值。

### 2. 普通 1 费条件增强防御 → 铁甲战士《邪眼》

原版：1 费，**罕见**，技能。

> 获得8点格挡。
> 如果你在本回合消耗过卡牌，则额外获得8点格挡。

升级：每段格挡 8 → 11，满足条件时总计 16 → 22。

对应定位：不满足条件也有基础防御；完成角色机制后得到额外防御。Karen 可把条件换成“约定牌堆非空”。

需要修正强度预期：这个参照不支持直接做普通版的 8＋8。约定牌堆非空可以跨回合维持，条件成本与“本回合消耗过牌”不同；若仍定普通，建议基础防御加小额奖励，而不是翻倍。若坚持强奖励，应重新考虑罕见定位。

### 3. 普通 1 费伤害＋虚弱 → 静默猎手《突然一拳》

原版：1 费，普通，攻击。

> 造成8点伤害。
> 给予1层虚弱。

升级：10 点伤害、2 层虚弱。

对应定位：输出与减伤合在一张容易获取的攻击牌中，是直接参照。

Karen 的设计边界：与《上挑》区分——用不带闪耀的持续可用性，交换较少的附加功能，不同时再给予易伤或回能。需要实测《上挑》是否真的出现耗尽后的减伤缺口；如果没有，这个新增位可以由旧牌调整替代。

### 4. 普通 2 费持续防御 → 储君《流光溢彩》

原版：2 费，普通，技能。

> 获得11点格挡。
> 在下一回合获得5点格挡。

升级：本回合 13、下回合 7。

对应定位：2 费普通防御，能在长战斗中反复使用，并对未来回合提供价值。它不是“当回合获得16格挡”，跨回合分配是其定位的一部分。

另一条即时大格挡参照是铁甲战士《血墙》：2 费，普通，技能。

> 失去2点生命。
> 获得16点格挡。

升级：20 格挡，仍失去 2 生命。

Karen 的设计边界：原版同档牌分别通过延迟收益或生命代价约束效率。因此我更倾向将这个新增位做成“跨回合防御”，而非再做一张无代价大格挡。它也更容易与《体操练习》《拉伸》区别；不能因为它们都是 2 费，就把两回合收益加总后当作即时格挡标准。

### 5. 罕见 1 费禁用遗物换格挡 → 亡灵契约师《牺牲》

原版：1 费，**稀有**，技能；另有关键字：**保留**。

正式版 v0.107.1 原本描述：

> 如果奥斯提存活，则他死去，然后你获得等量于其双倍最大生命值的格挡。

测试版 v0.111.0 原本描述：

> 如果奥斯提存活，则他死去，然后你获得等量于其最大生命值三倍的格挡。

升级：费用 1 → 0；保留关键字不变。

对应定位：放弃当前已建立的角色资源，换取当回合防御。这里是**功能参照，不是效果、稀有度或数值等价**。原版五职业没有可直接对应 Karen“禁用 1 个遗物换格挡”的同机制牌。

辅助参照《血墙》则展示了“确定的额外代价换大格挡”。但生命、奥斯提和遗物的机会成本完全不同：某件遗物可能已在战斗开始时结算完收益，此时禁用的实际代价很小。Karen 新卡应设固定且克制的防御上限，不应按遗物数量无限增长，也不能照搬《牺牲》的大倍率。

### 6. 罕见 2 费兑现本回合移牌 → 静默猎手《终结技》

原版：**1 费**，罕见，攻击。

> 在这个回合内每打出过一张攻击牌，就造成6点伤害一次。

升级：每次伤害 6 → 8。战斗中另显示当前攻击次数；原版没有这里建议的低次数上限。

对应定位：前面的操作积累进度，最后用一张攻击将进度兑现。Karen 可将“本回合打出攻击牌数”替换成“本回合从约定牌堆取出的牌数”。

需要修正费用预期：这张原版参照是 1 费，所以之前建议的 2 费不是已经成立的结论。若 Karen 版本既要 2 费又限制次数，就应有不依赖前置操作的基础伤害，或更高的每次收益；若纯粹按次数结算而没有保底，应重新评估 1 费。采用单次总伤害还是多段，也要分别考虑临时力量收益。

### 对原建议的收敛

六项里，可靠 2 费攻击、1 费伤害＋虚弱、2 费持续防御都有同费用同稀有度参照；条件防御、资源牺牲、操作兑现只提供功能参照，不能证明此前给出的所有费用和稀有度已经合适。

因此维持“优先填普通牌功能位”的方向，同时将第 4 项优先改为跨回合防御，并将第 6 项费用明确标为待定（有保底时测试 2 费，无保底时重新评估 1 费）。前文全部新增后的费用统计仅是最初 2 费方案的示意，不是最终规划。

## 药水

当前角色已接入 KarenPotionPool，实际不是旧文档写的 IroncladPotionPool。

| 药水 | 稀有度 | 当前效果 | 评价 |
|---|---|---|---|
| 闪耀药水 | 普通 | 从 3 张符合生成条件的闪耀牌中选 1 张加入手牌及永久牌组，当回合免费 | 同时包含当场救急和整局成长；重点观察是否成为无脑最佳选择。不是所有闪耀牌都可被选中，代码还过滤 CanBeGeneratedInCombat |
| 红茶 | 罕见 | 3 临时力量，保留力量 2 回合 | 与多段攻击和临时力量机制匹配；延续这一中等强度、跨回合爆发定位即可 |
| 唤醒之泪 | 稀有 | 从抽牌堆、弃牌堆、约定牌堆选择牌，直至手牌上限或停止选择 | 强检索与组合启动工具；不包含消耗堆、闪耀耗尽堆。约定虚无模式有单独处理 |

**推荐新增 0 瓶，最多可选 1 瓶普通防御药水。** 现有 3 瓶已覆盖闪耀、力量、取牌，且各占一个稀有度。不能因为专属池里没有通用伤害/格挡就认定角色完全缺这些药水，专属内容不应机械复制共享药水。

可选普通药水功能：获得适量格挡，并可将手中至多 1 张牌放入约定牌堆。空手或不选择时仍有基础防御效果。不要额外取牌回能，以免与唤醒之泪和现有卡牌引擎重复。

优先实测两点：闪耀药水的永久补牌与战后奖励叠加价值；唤醒之泪配合《命运与本能》《投下燃料》等是否过于稳定地启动整套连段。保留原有效果还是调整稀有度，需要数据，不凭静态阅读直接判定超模。

## 遗物

| 遗物 | 代码稀有度 / 来源 | 效果与口径 |
|---|---|---|
| 发夹 | Starter / 起始 | 第 2 回合抽牌前生成一张约定之塔 |
| 发夹-立体版 | Starter / TouchOfOrobas 升级初始遗物补丁 | 第 1 回合抽牌前生成一张约定之塔+；不能计为普通掉落遗物 |
| 上锁的遗物 | Event / 禁用系统占位 | 暂存被禁用遗物的引用，战后恢复；不属于独立构筑奖励 |

因此是 **2 个角色遗物模型＋1 个机制占位模型，正常掉落专属遗物为 0**。卡牌 relic 目录里的牌不是遗物；《骄傲》《激情》《嫉妒》给予随机遗物，也不等于已经有 Karen 专属遗物设计。

### 新增遗物分配

2026-10-10 确认的目标为新增 **7 件：普通 1、罕见 2、稀有 3、商店 1**。正式版 v0.107.1 与测试版 v0.111.0 的五个原版职业均采用该非初始遗物结构；另有初始遗物与其升级版。Karen 保留现有两版发夹，补齐后共有 9 件实际遗物设计，机制占位不计数。[双版本遗物核对数据](../../artifacts/research/relic-planning-20261010/relic-audit.json)

以下七件为用户已确认、待实装的设计；未命名项保留为“待命名”。

查看[待制作遗物列表（HTML）](karen-pending-relics.html)：集中展示全部7件的效果、当前图片与名称状态，并可按稀有度筛选、搜索或展开触发规则。

2026-10-10 的[遗物文字候选](relic-concepts-2026-10-10/README.md)保留名称、效果与出处，旧图片及展示引用已全部删除。“番茄”和“温暖的归处”已撤回，新增“怀表”“星形发饰”候选；新增名称尚未定稿。图片制作改为先寻找参考图、截取物件，再基于参考图修复重绘，尽量保留原貌。

图片当前优先采用[STS1 Karen 原始素材查找](relic-concepts-2026-10-10/sts1-sources/gallery.html)：饼干、棒球猫、Mr.White、星之皇冠已有原始图标；独立四角星及两张卡牌大图可作为小小星、星形发饰的参考。当前素材页仅展示从老 Mod 复制并校验一致的文件，未重绘。此前[参考与编辑对照 V2](relic-concepts-2026-10-10/reference-edits-v2/comparison.html)保留为历史记录。全部尚未部署为游戏资源。

2026-10-11 当前图片为[修复 V4：七件总览与来源对照](relic-concepts-2026-10-10/repair-v4/gallery.html)。棒球猫按官方造型重绘；星形发饰与星之王冠直接从已核实的商品照片／动画截图提取；普通位以 TV 第5话出现的「华恋的水壶」替换怀表牌候选。饼干、金色四角星、Mr. White 保留已清理稿。七件提供1254×1254透明画布，其中两张直接抠图只是常规插值，另附原生分辨率版本；没有改变下表已确认效果，也未接入游戏。

此前[七件清理稿](relic-concepts-2026-10-10/complete-relics-2026-10-11/gallery.html)中棒球猫、星形发饰、星之王冠存在造型错误，旧压缩包不再作为当前交付。[错误与正确参考复核](relic-concepts-2026-10-10/source-audit-v3/references.html)保留作历史记录；修复方式、原始参考、提示词与校验数据见[V4制作说明](relic-concepts-2026-10-10/repair-v4/README.md)。

| 稀有度 | 名称 | 效果 | 流派定位 | 状态 |
|---|---|---|---|---|
| 普通 | 待命名 | 每场战斗第一次获得临时力量时，保留力量 2 回合。 | 临时力量与保留，延长爆发窗口 | 已确认，待实装 |
| 罕见 | 饼干 | 拾起时，随机获得普通、罕见、稀有闪耀牌各 1 张。 | 闪耀牌补充，提供跨稀有度的构筑材料 | 已确认，待实装 |
| 罕见 | 待命名 | 战斗开始时，将 1 张光辉加入约定牌堆。 | 约定启动，提供可取回的能量资源 | 已确认，待实装 |
| 稀有 | 棒球猫 | 每场战斗中，每耗尽一张非同名闪耀牌，获得 1 个闪耀牌奖励。 | 闪耀耗尽与再生产，鼓励耗尽不同名称的闪耀牌 | 已确认，待实装 |
| 稀有 | 待命名 | 每场战斗的第一回合结束时，可选择任意手牌放入约定牌堆。 | 首回合手牌储备与约定启动 | 已确认，待实装 |
| 稀有 | 待命名 | 打出约定之塔时，额外获得 8 点格挡。 | 约定之塔的防御收益 | 已确认，待实装 |
| 商店 | 星之王冠 | 每回合开始时禁用1个遗物，然后获得1点能量。 | 禁用遗物换取回合能量 | 已确认，待实装 |

触发口径：

- 普通遗物每场战斗只触发一次；保留力量的持续时间为 2 回合。
- 饼干在拾起时分别随机获得三个稀有度的闪耀牌，合计 3 张；这里的“获得牌”与棒球猫的“获得闪耀牌奖励”分别记录。
- 约定启动遗物每场战斗开始时加入 1 张光辉，不升级该光辉。
- 棒球猫按每场战斗内的牌名去重。同名闪耀牌在同一场战斗中重复耗尽，只产生 1 个奖励；下一场战斗重新计数。不同名称分别产生奖励，不额外设置每回合触发上限。
- 首回合存牌遗物在每场战斗第一回合结束、常规弃牌前结算；可选择任意数量的手牌，包括不选或全选。
- 约定之塔格挡遗物在每次打出约定之塔时触发，额外获得 8 点格挡。
- 星之王冠沿用现有禁用规则，可以禁用自身；效果按“禁用1个遗物，然后获得1点能量”记录，不附加“其他遗物”或“成功禁用后才获得能量”的限制。

**7 个新增遗物位均已填满：普通 1、罕见 2、稀有 3、商店 1。** 已确认设计覆盖临时力量、约定启动与首回合储备、约定之塔的防御收益、闪耀补牌、耗尽奖励和禁用遗物，均待实装。

禁用相关遗物还有实现约束：当前系统会把原遗物从玩家遗物列表移除并替换为占位。因此“遗物自己被禁用以后继续监听收益”不能作为默认可行设计；应使用其他仍生效的遗物监听，并为每战计数、恢复、保存和联机明确状态归属。

## 历史实施建议（卡牌及遗物新增数量已由修订方案取代）

1. 先做 4 个普通卡功能位和 2 个普通专属遗物；若已有牌可重做补位，优先重做。
2. 通过实际对局看第一幕选牌与长战斗：是否缺可靠伤害、闪耀耗尽后是否仍有防御、两张新防御是否只是重复选项。
3. 再决定 2 张罕见卡与 3 件进阶遗物；尤其确认禁用遗物是否要成为正式支持的方向。
4. 3 瓶现有药水先测；可选第 4 瓶放最后。

新增内容的运行验收仍须遵守项目双版本单人、同版本多人矩阵；本文没有实施代码改动，也没有开展这些实测。卡牌稀有度份额不等于奖励出现概率，补牌会稀释同稀有度内现有关键牌，需跟踪选取率、跳过率和构筑依赖，而非只追求卡牌总数。

## 完整卡牌名单

以下按实际池登记和构造定义列出。闪耀列是初始上限；“—”表示该类未直接初始化闪耀，不表示与闪耀无交互。效果请点源码查看，避免将动态本地化模板误当最终数值。

### 基础

| 卡牌 | 费用 | 类型 | 闪耀 | 限制 |
|---|---:|---|---:|---|
| [防御](../../src/Core/Models/Cards/basic/KarenDefend.cs) | 1 | 技能 | — | — |
| [坠落](../../src/Core/Models/Cards/basic/KarenFall.cs) | 1 | 技能 | — | — |
| [闪耀打击](../../src/Core/Models/Cards/basic/KarenShineStrike.cs) | 1 | 攻击 | 9 | — |
| [打击](../../src/Core/Models/Cards/basic/KarenStrike.cs) | 1 | 攻击 | — | — |


### 普通

| 卡牌 | 费用 | 类型 | 闪耀 | 限制 |
|---|---:|---|---:|---|
| [不再遥不可及](../../src/Core/Models/Cards/basic/KarenPromiseDraw.cs) | 0 | 攻击 | — | — |
| [RevueDuet](../../src/Core/Models/Cards/promisePile/KarenRevueDuet.cs) | 0 | 攻击 | — | — |
| [迈向那个舞台](../../src/Core/Models/Cards/shine/KarenToTheStage.cs) | 0 | 技能 | 6 | — |
| [Banana松饼](../../src/Core/Models/Cards/strength/KarenBananaMuffin.cs) | 1 | 技能 | — | — |
| [蓄力打击](../../src/Core/Models/Cards/shine/KarenChargeStrike.cs) | 1 | 攻击 | 9 | — |
| [勇气打击](../../src/Core/Models/Cards/promisePile/KarenCourageStrike.cs) | 1 | 攻击 | — | — |
| [舞动](../../src/Core/Models/Cards/promisePile/KarenDance.cs) | 1 | 攻击 | — | — |
| [出场](../../src/Core/Models/Cards/shine/KarenDebut.cs) | 1 | 攻击 | 6 | — |
| [一起吃饭吧](../../src/Core/Models/Cards/strength/KarenEatTogether.cs) | 1 | 技能 | — | — |
| [重逢](../../src/Core/Models/Cards/promisePile/KarenMeetAgain.cs) | 1 | 技能 | — | — |
| [观察情况](../../src/Core/Models/Cards/promisePile/KarenNewSituation.cs) | 1 | 技能 | — | — |
| [我们的约定](../../src/Core/Models/Cards/promisePile/KarenOurPromise.cs) | 1 | 技能 | — | — |
| [武术练习](../../src/Core/Models/Cards/shine/KarenPractice.cs) | 1 | 技能 | 9 | — |
| [用你的闪耀贯穿我吧](../../src/Core/Models/Cards/shine/KarenShineStrikeBarrage.cs) | 1 | 攻击 | 6 | — |
| [星星串起了我们的友谊](../../src/Core/Models/Cards/shine/KarenStarFriend.cs) | 1 | 攻击 | 3 | — |
| [上挑](../../src/Core/Models/Cards/shine/KarenSwordUp.cs) | 1 | 攻击 | 9 | — |
| [东京塔下](../../src/Core/Models/Cards/promisePile/KarenUnderTower.cs) | 1 | 技能 | — | — |
| [双人舞](../../src/Core/Models/Cards/promisePile/KarenWhosPromise.cs) | 1 | 攻击 | — | — |
| [昨日夜空](../../src/Core/Models/Cards/promisePile/KarenYesterglow.cs) | 1 | 攻击 | — | — |
| [体操练习](../../src/Core/Models/Cards/shine/KarenPractice3.cs) | 2 | 技能 | 9 | — |


### 罕见

| 卡牌 | 费用 | 类型 | 闪耀 | 限制 |
|---|---:|---|---:|---|
| [添麻烦了](../../src/Core/Models/Cards/neutral/KarenCry.cs) | 0 | 技能 | — | — |
| [闪避](../../src/Core/Models/Cards/promisePile/KarenDodge.cs) | 0 | 技能 | — | — |
| [命运交换之日](../../src/Core/Models/Cards/promisePile/KarenExchangeFate.cs) | 0 | 技能 | — | — |
| [歌唱吧舞蹈吧互相争斗吧](../../src/Core/Models/Cards/shine/KarenFightRelay.cs) | 0 | 技能 | 3 | 仅多人 |
| [不再犹豫](../../src/Core/Models/Cards/promisePile/KarenNoHesitate.cs) | 0 | 技能 | 6 | — |
| [开拓星辰](../../src/Core/Models/Cards/promisePile/KarenOpenUpStars.cs) | 0 | 技能 | — | — |
| [摘星](../../src/Core/Models/Cards/relic/KarenPickStar.cs) | 0 | 技能 | — | — |
| [土豆](../../src/Core/Models/Cards/shine/KarenPotato.cs) | 0 | 技能 | 3 | — |
| [极速下降](../../src/Core/Models/Cards/promisePile/KarenRapid.cs) | 0 | 技能 | — | — |
| [准备完成](../../src/Core/Models/Cards/shine/KarenReady.cs) | 0 | 技能 | 6 | — |
| [星罪](../../src/Core/Models/Cards/relic/KarenStarCrime.cs) | 0 | 技能 | — | — |
| [水族馆](../../src/Core/Models/Cards/promisePile/KarenAquariumCard.cs) | 1 | 能力 | — | — |
| [背靠背](../../src/Core/Models/Cards/promisePile/KarenBackToBackCard.cs) | 1 | 攻击 | — | — |
| [Banana午餐](../../src/Core/Models/Cards/promisePile/KarenBananaLunch.cs) | 1 | 能力 | — | — |
| [以觉悟的名义](../../src/Core/Models/Cards/neutral/KarenConsciousness.cs) | 1 | 攻击 | 9 | — |
| [永无结束的命运舞台](../../src/Core/Models/Cards/shine/KarenContinue02.cs) | 1 | 攻击 | 9 | — |
| [水分补充](../../src/Core/Models/Cards/shine/KarenDrinkWater.cs) | 1 | 技能 | 3 | — |
| [Banana费南雪](../../src/Core/Models/Cards/relic/KarenFinancier.cs) | 1 | 能力 | — | — |
| [落地](../../src/Core/Models/Cards/promisePile/KarenLanding.cs) | 1 | 攻击 | — | — |
| [信封](../../src/Core/Models/Cards/promisePile/KarenLetterCard.cs) | 1 | 能力 | — | — |
| [新的一天](../../src/Core/Models/Cards/promisePile/KarenNewDay.cs) | 1 | 技能 | — | — |
| [约定的那天](../../src/Core/Models/Cards/neutral/KarenOldPlace.cs) | 1 | 技能 | — | — |
| [命运与本能](../../src/Core/Models/Cards/promisePile/KarenOnStage.cs) | 1 | 技能 | — | — |
| [招架](../../src/Core/Models/Cards/neutral/KarenParry.cs) | 1 | 技能 | — | — |
| [舞蹈练习](../../src/Core/Models/Cards/shine/KarenPractice2.cs) | 1 | 技能 | — | — |
| [续写](../../src/Core/Models/Cards/shine/KarenRewrite.cs) | 1 | 攻击 | 6 | — |
| [行动](../../src/Core/Models/Cards/promisePile/KarenRun.cs) | 1 | 技能 | — | — |
| [旋转](../../src/Core/Models/Cards/globalMove/KarenSpin.cs) | 1 | 攻击 | — | — |
| [Starlight第三幕](../../src/Core/Models/Cards/shine/KarenStarlight03Card.cs) | 1 | 能力 | — | — |
| [Starlight第一幕](../../src/Core/Models/Cards/shine/KarenStarlightCard.cs) | 1 | 能力 | — | — |
| [耀眼的阳光](../../src/Core/Models/Cards/shine/KarenSunlight.cs) | 1 | 攻击 | 3 | — |
| [神圣星辰](../../src/Core/Models/Cards/promisePile/KarenHolyStar.cs) | 2 | 攻击 | — | — |
| [舞台正在等待着](../../src/Core/Models/Cards/promisePile/KarenStageIsWaiting.cs) | 2 | 能力 | — | — |
| [拉伸](../../src/Core/Models/Cards/promisePile/KarenStretching.cs) | 2 | 技能 | — | — |
| [下一个舞台](../../src/Core/Models/Cards/globalMove/KarenNextStage.cs) | 3 | 技能 | — | — |


### 稀有

| 卡牌 | 费用 | 类型 | 闪耀 | 限制 |
|---|---:|---|---:|---|
| [投下燃料](../../src/Core/Models/Cards/shine/KarenDropFuel.cs) | 0 | 技能 | 9 | — |
| [最后的台词](../../src/Core/Models/Cards/neutral/KarenLastWord.cs) | 0 | 攻击 | — | — |
| [Banana蛋糕](../../src/Core/Models/Cards/neutral/KarenBananaCake.cs) | 1 | 技能 | — | — |
| [燃烧吧燃烧吧](../../src/Core/Models/Cards/neutral/KarenBurn.cs) | 1 | 能力 | — | — |
| [嫉妒](../../src/Core/Models/Cards/relic/KarenJealousy.cs) | 1 | 攻击 | 9 | — |
| [过去与未来](../../src/Core/Models/Cards/promisePile/KarenPastAndFuture.cs) | 1 | 能力 | — | — |
| [星光指引](../../src/Core/Models/Cards/shine/KarenStarGuide.cs) | 1 | 技能 | — | — |
| [门票](../../src/Core/Models/Cards/promisePile/KarenTicket.cs) | 1 | 能力 | — | — |
| [唤醒](../../src/Core/Models/Cards/globalMove/KarenWakeUp.cs) | 1 | 技能 | — | — |
| [我们犯下的罪过](../../src/Core/Models/Cards/shine/KarenCarryingGuilt.cs) | 2 | 攻击 | — | — |
| [激情](../../src/Core/Models/Cards/relic/KarenPassion.cs) | 2 | 技能 | 1 | — |
| [永不遗忘的闪耀](../../src/Core/Models/Cards/shine/KarenShineTogether.cs) | 2 | 攻击 | 3 | — |
| [一起闪耀吧](../../src/Core/Models/Cards/shine/KarenShineTogetherAll.cs) | 2 | 能力 | — | 仅多人 |
| [Banana什锦](../../src/Core/Models/Cards/promisePile/KarenSmallSnack.cs) | 2 | 能力 | — | — |
| [星光闪耀之时](../../src/Core/Models/Cards/shine/KarenStar.cs) | 2 | 技能 | — | — |
| [Starlight第二幕](../../src/Core/Models/Cards/shine/KarenStarlight02Card.cs) | 2 | 能力 | — | — |
| [世界上最空虚的人](../../src/Core/Models/Cards/promisePile/KarenVoid.cs) | 2 | 能力 | — | — |
| [骄傲](../../src/Core/Models/Cards/relic/KarenArrogant.cs) | 3 | 能力 | — | — |
| [棒球](../../src/Core/Models/Cards/promisePile/KarenBaseball.cs) | 3 | 攻击 | — | — |
| [觉醒形态](../../src/Core/Models/Cards/neutral/KarenForm.cs) | 3 | 能力 | — | — |
| [命运的齿轮](../../src/Core/Models/Cards/neutral/KarenGeer.cs) | 3 | 能力 | — | — |
| [NONNON哒哟](../../src/Core/Models/Cards/shine/KarenNonon.cs) | 3 | 攻击 | 9 | — |
| [命运舞台的再生产](../../src/Core/Models/Cards/promisePile/KarenStageReproduce.cs) | 3 | 能力 | — | — |
| [两人的梦想](../../src/Core/Models/Cards/promisePile/KarenWeKnow.cs) | 3 | 能力 | — | — |
| [约定之塔桥](../../src/Core/Models/Cards/promisePile/KarenBridge.cs) | X | 技能 | — | — |


### Ancient

| 卡牌 | 费用 | 类型 | 闪耀 | 限制 |
|---|---:|---|---:|---|
| [为什么要离开？我的朋友](../../src/Core/Models/Cards/ancient/KarenWhy.cs) | 0 | 技能 | — | — |
| [Position 0](../../src/Core/Models/Cards/neutral/KarenPosition0.cs) | 3 | 能力 | — | — |

### 未纳入上述卡池的模型

| 模型 | 分类 |
|---|---|
| 困意 | 诅咒 |
| 存在于舞台的理由 | 诅咒 |
| "宽恕" | 稀有 |
| 弃牌堆 | Token |
| 抽牌堆 | Token |
| 约定牌堆 | Token |
| banana晚餐 | Token |
| 对峙 | Token |
| 续演 | Token |
| 回击 | Token |
| 空壳 | Token |
| 弃牌堆 | Token |
| 抽牌堆 | Token |
| 手牌 | Token |
| 保留格挡 | Token |
| 保留能量 | Token |
| 保留手牌 | Token |
| 保留力量 | Token |
| 放入 | Token |
| 取回 | Token |
| 三明治 | Token |
| 闪耀的再生产 | Token |
| 侧身 | Token |
| 约定之塔 | Token |
| 弃牌堆 | Token |
| 抽牌堆 | Token |
| 约定牌堆 | Token |
| 光辉 | Token |
| 披萨 | 罕见 |

## 源码依据

- [KarenCardPool](../../src/Core/Models/CardPools/KarenCardPool.cs)：登记清单及两张暂时移除的牌。
- [Karen](../../src/Core/Models/Characters/Karen.cs)：起始卡组、遗物、药水池接线。
- [KarenPotionPool](../../src/Core/Models/PotionPools/KarenPotionPool.cs)、[KarenRelicPool](../../src/Core/Models/RelicPools/KarenRelicPool.cs)：专属池。
- [AwakePotionAction](../../src/Core/Commands/AwakePotionAction.cs)：唤醒之泪的实际可选牌堆。
- [TouchOfOrobasPatch](../../src/Core/Patches/TouchOfOrobasPatch.cs)：升级初始遗物来源。
- [DisableRelicManager](../../src/Core/DisableRelicSystem/DisableRelicManager.cs)：锁定、占位和恢复流程。
