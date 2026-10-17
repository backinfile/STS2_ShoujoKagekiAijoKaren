# main 已实现特效逐项适配审查

审查日期：2026-10-16。基准：`main@782a71ebd767a5c168ffabbaf06e15008b6bf146`。

后续已落实嫉妒、唤醒、骄傲、接力耗尽四项调整，并完成针对性实测和前后实录：[落地记录与视频](../testing/vfx-suitability-polish-2026-10-16.md)。下文评估对象仍为原main快照；第27项已根据用户补充的TV真昼既定参考更正，历史审查的“未启动游戏”只指初次审查阶段。

## 结论先读

当前方向成立：约定塔、卡牌星光、舞台灯光和红金／蓝白配色已形成 Karen 的角色语言。最有价值的是能告诉玩家“哪张牌去了哪里、哪次收益已经发生、现在还有什么可触发”的反馈。下一步应先完善事件之间的主次和辨识度，不建议把现有特效整体推翻。

我的具体取舍：

- **优先保留**：闪耀的普通／临界／耗尽／抵达链，约定牌堆真实进出牌，Position 0 的剩余次数与攻击 hover，棒球、背靠背、续演的实际命中反馈，光辉的真实回能交接。
- **优先优化**：唤醒选择期间持续铺满的高对比幕布，以及骄傲／接力终幕与通用激情 Buff 反馈之间的重复表达。
- **嫉妒保留TV真昼彩纸语义**：原任务明确要求这种专属闪亮方片。初次将其泛化成“礼花感”并压低多彩配色的判断撤回；优化应集中在片面亮泽、侧翻、边角闪光及真实命中切光，详见[真昼参考修订](../testing/jealousy-mahiru-tv-revision-2026-10-16.md)。
- **保留主题、调整层级**：燃烧＋过去与未来＋觉醒＋再生产与新灯光叠加；第二幕、入场、Position 0 都可使用灯，但应各自占据不同空间和时间段。
- **需要运行核查再下结论**：当前整合版的跨特效共存、齿轮的 Fast／Instant 行为、多人同时释放全屏效果。源码能指出这些边界，历史单项通过不能代替当前整合验收。

以下“微调”和“优先优化”是设计建议，不等同于功能故障。用户已有明确要求的全屏粒子、原位攻击、无专属奖励演出、无音效等方向均作为约束，不因本报告反向恢复已否决方案。

## 范围、方法与证据口径

这是 **71 个审查条目**，按玩家看到的事件拆分，并不声称有 71 套独立特效实现。共用节点的不同触发分别评价；普通／空虚路由合并评价并指出落点差异。没有专属演出的普通打防牌归入共用表现，不把每个 `WithHitFx(slashPath)` 重复计算。

范围覆盖当前生产代码中的战斗特效、牌堆／资源 UI，以及相关角色表现入口。`docs/design/rare-vfx` 的未接入提案、旧版参考图、测试 Probe、HTML 预演、SK 空壳兼容类不按“已实现专属特效”计入。选角、死亡和商店／休息点补在末表，证据不足时只给静态判断。

本次实际做了：

1. 在独立 worktree 检查上述快照，沿 Card／Power／Patch／Manager 查到实际节点与资源；只修改审查材料。
2. 查看 2026-10-14 历史目录的 13 张检查页，覆盖其 49 个场景；另外查看齿轮连续帧、耗尽图标连续帧及已提交的实机关键图。
3. 从 19 段既有实机录像重新提取分时采样，查看入场、闪耀、门票、梦想、第二幕、摘星、齿轮、终幕，以及后来新增的激情、Position 0、命运交换、Why、永不遗忘、背靠背、接力、嫉妒、唤醒。这些录像属于各自历史构建，并非都来自本快照。采样表与来源 SHA-256 见本任务的[视觉证据清单](../worktree-tasks/vfx-suitability-review-2026-10-16/evidence-index.json)。
4. 将已有专项的通过／失败／未测信息作为历史记录引用。**没有启动游戏、没有新做双版本或多人实测，没有主观试听音轨**；声音仅核对触发、开关和已有记录。7张关键检查页随任务保留，其余可按清单中的原片、时间点重新提取。抽样帧也不能证明整段无卡顿或无瞬间遮挡。

证据标记：

| 标记 | 含义 | 能支持什么 |
| --- | --- | --- |
| V | 本次看过相应版本的既有实机图或录像采样，并核对当前接线 | 可评价已见构图、视觉语义；不是当前整包重新运行通过 |
| H | 看过历史目录画面，结合当前源码／后续修订记录 | 可评价方向；已修订的尺寸、落点、运动以当前代码和修订记录为准 |
| S | 仅源码、场景与历史文字记录 | 只给静态适配判断，视觉质量待补证据 |

关键采样页：[嫉妒](../worktree-tasks/vfx-suitability-review-2026-10-16/evidence/jealousy.jpg)、[唤醒](../worktree-tasks/vfx-suitability-review-2026-10-16/evidence/wake-up.jpg)、[刀光](../worktree-tasks/vfx-suitability-review-2026-10-16/evidence/unforgettable.jpg)、[Why](../worktree-tasks/vfx-suitability-review-2026-10-16/evidence/why.jpg)、[Position 0](../worktree-tasks/vfx-suitability-review-2026-10-16/evidence/position-zero.jpg)、[命运交换](../worktree-tasks/vfx-suitability-review-2026-10-16/evidence/exchange.jpg)、[接力](../worktree-tasks/vfx-suitability-review-2026-10-16/evidence/relay.jpg)。

原检出 main 的未提交修改和未跟踪文档没有带入审查。特别是尚未提交的 `KarenVfxComposition` 调整、共存测试与 `docs/21.特效总览与兼容约定.md`，均不作为此快照已经解决问题的证据。报告写的是已提交 main，而非这些正在开发的内容。

## 评价标准

- **机制准确**：预告与实际成功分开；不把待领战后奖励画成已经拿到遗物，不向错误牌堆／图标运输。
- **主题准确**：舞台、剑光、塔、食物各有理由；不靠卡图物件堆砌来代替出牌动作。
- **视觉层级**：事件峰值高于常驻装饰；卡牌文本、目标、伤害与输入控件持续可读。
- **节奏适合频率**：首次施放可以展开，回合／逐张触发应更短。Fast、Instant 可以按功能需求保留小反馈，不强行要求所有 UI 都完全消失。
- **组合有边界**：同一结果不重复确认，多玩家／重播不无上限累加；取消、对象池复用与退场有自己的清理。

“保留”表示当前选择合理；“微调”表示主体合理、建议局部改善；“优先优化”表示应先于新增华丽演出来处理；“待验证”表示缺少足够运行证据。

## 1. 入场、约定塔与牌堆界面

| 编号／对象 | 当前表现与玩法对应 | 判断 | 理由与建议 | 证据 |
| --- | --- | --- | --- | --- |
| 01 战斗入场 | 舞台亮灯、轻迈步与披风粒子，塔同步显形；不等待结算，本机 Karen 播放，配置默认开启 | 保留 | 角色身份明确，且允许原生攻击等动作接手。开场部分被原生转场遮住属于现有取舍，不应为完整展示而延迟战斗。灯光规模不要再扩大。 | V；[入场记录](../worktree-tasks/karen-battle-entrance/README.md) |
| 02 约定塔常驻 | 角色身后红塔、库存星点、数量与走灯 | 保留 | 是角色最强的持久识别物，也承担牌堆定位。空库存仍可留塔轮廓，事件灯与常驻灯应分档；不要把每个灯点做成持续闪烁焦点。 | H；[塔节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenPromiseStarNode.cs)、[手绘塔](../testing/painted-promise-tower-2026-10-10.md) |
| 03 普通存牌与拖牌预告 | 原生卡牌进塔，星点承接；持牌预告与真正移动分别接线 | 保留 | “从哪里进入哪里”比单纯爆星有用。预告不能提前增加真实库存；主角应是卡牌，星点只承接归属。昨日夜空等使用同一链，不需要各加一套远程运输。 | H；[移牌补丁](../../src/Core/PromisePileSystem/Patches/NCardFindOnTablePatch.cs)、[拖牌预告](../../src/Core/PromisePileSystem/Patches/PromiseTransferDragPatch.cs) |
| 04 普通取牌／释放预告 | 塔蓄光、库存到真实卡牌的交接，取消预告退去 | 保留 | 能区分“准备取”和“确实取到”。保留真实目标跟随和取消清理，不用复制牌或延长悬空来凑镜头。 | H；[召回实现](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenPromiseStarNode.Recall.cs) |
| 05 《约定之塔》专属取回 | 释放星冠、前后分层与真实回手尾迹；空虚模式从抽牌堆入口出发 | 保留 | 与角色核心机制高度一致，专项还覆盖了空库存、满手和重复取回。效果已经足够完整，应优先确保“空／只取到一张”时仍克制。 | H；[最新专项](../testing/promise-tower-vfx-2026-10-15.md)、[实机前后图](../worktree-tasks/karen-promise-tower-vfx-20261015/evidence/stable-after-tower.png) |
| 06 《世界上最空虚的人》销毁原抽牌堆 | 在图标处统一消耗表现，不逐张展示被移除卡面 | 保留 | 避免把整堆删除演成多次抽牌或展示私有牌面；规则上的移除也没有被视觉冒充为普通消耗触发。应继续和后续库存交接分阶段表达。 | H；[销毁节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenDrawPileDestroyVfx.cs)、[卡牌](../../src/Core/Models/Cards/promisePile/KarenVoid.cs) |
| 07 空虚切换：塔→图标 | 卡牌扫光、塔保持到扫描结束、星点随光路转入图标，塔退出 | 保留 | 将“约定改由抽牌堆承载”可视化，是必要的机制教学。空库存仍保留传光，但不造假库存星。后续不应又同时保留一座可操作旧塔。 | H；[切换记录](../testing/void-tower-handoff-2026-10-12.md)、[轨道实现](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenPromiseStarNode.VoidOrbit.cs) |
| 08 空虚常驻与进出牌 | 库存星光、燃烧／取牌等改挂抽牌堆入口 | 微调 | 路由语义正确，但图标周边空间比塔小，多个模式容易挤在同一点。保留数量和当前动作，压低其余背景装饰；不再加独立图标环。 | H；[空虚实现](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenPromiseStarNode.Void.cs) |
| 09 查看约定牌堆 | 双星、红金细线与六颗开场星点，结束后静态显示 | 保留 | 查询界面需要读牌，当前克制程度合适；停止逐帧更新也适合长期停留。没有理由改为选择界面那种全屏幕布。 | S；[查看界面专项](../worktree-tasks/karen-pile-view-vfx-20261015/README.md) |
| 10 查看闪耀耗尽牌堆 | 空心星、断续短弧与少量下落余烬 | 保留 | 和约定界面共享语言，又用空心／断续表达“已经用尽”。沿用静态终态；既有专项最终为 OpenGL，不能扩大成 Vulkan／多尺寸已验收。 | S；[查看节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Node/NKarenPileViewVfx.cs) |

## 2. 闪耀与长期模式

| 编号／对象 | 当前表现与玩法对应 | 判断 | 理由与建议 | 证据 |
| --- | --- | --- | --- | --- |
| 11 普通闪耀微光 | 细金流光与稀疏星屑，临界态出现时让位 | 保留 | 让玩家不用逐牌查文字也能识别闪耀牌。当前与临界互斥正确；普通态不要继续提亮到与最后一点闪耀同级。 | V；[普通微光](../testing/shine-ambient-2026-10-15.md) |
| 12 最后一点闪耀预警 | 卡缘强光、带惯性的漂浮亮星、独立余烬，持续到真实打出过程 | 保留 | 对不可逆的耗尽有预告价值。比单纯把牌染色更清晰；需同时保护可打出高亮、升级边和原生选中状态。 | V；[运动修订](../testing/shine-motion-2026-10-15.md)、[预警节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/ShineSystem/Nodes/NKarenShineWarningVfx.cs) |
| 13 耗尽收拢与飞入 | 卡面缩小、旋转星屑收拢、速度有起伏地飞到右上耗尽牌堆；燃料跳过卡面重现 | 保留 | 解释了牌的最终去向，且特殊燃料避免重复爆散。动作应服务“已经离开本场牌组”，不再增加第二次奖励爆闪。 | V；[耗尽节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/ShineSystem/Nodes/NKarenShineDepleteVfx.cs)、[最新速度修订](../testing/shine-motion-2026-10-15.md) |
| 14 耗尽图标接住星光 | 实际抵达后轻压回弹、旋转、闪亮与数量反馈 | 保留 | 接住动作补全了运输闭环；普通／Fast 与 Instant 的短反馈可分别保留，不需要为了统一而删掉功能性提示。连续多张时应合并动势，避免图标一直摇。 | V；[抵达记录](../testing/shine-pile-arrival-2026-10-15.md) |
| 15 燃烧模式角色／牌堆常驻 | 红火、火星与旋转气场，普通塔和空虚入口适配 | 微调 | “燃烧”容易理解，视觉语言也有辨识度。和觉醒风、再生产并存时，火焰只做底层状态，事件脉冲再抬升；不让红火掩盖红色目标提示。 | H；[燃烧节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenBurnVfx.cs) |
| 16 燃烧模式手牌反馈 | 手中闪耀牌使用缩小的火星／气场 | 微调 | 能说明哪些牌受模式影响，但与普通闪耀、临界、可用高亮是同一块卡框空间。建议按临界＞模式＞普通微光明确分档，先调整火星密度，再考虑增亮。 | H；[卡牌燃烧补丁](../../src/Core/Patches/KarenBurnCardVfxPatch.cs)、[卡牌节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenBurnCardVfx.cs) |
| 17 过去与未来 | 环穿过角色，实际抽取时连续脉冲，最多八个可视环 | 微调 | 与燃烧的离散火星有形态差异，触发连贯。环容易让人联想到通用护盾；建议保留穿越方向和前后遮挡，减少完整等亮圆环，突出时间流动而非保护罩。 | H；[环实现](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenPastAndFutureRingVfx.cs) |
| 18 觉醒形态常驻 | 角色周围风场，配合独立 BGM 替换 | 微调 | 与持续能力相符，但长期高运动量容易削弱普通打牌反馈。风应后退为低对比底层，BGM 恢复是单独技术边界；本次不评价音乐主观听感。 | H；[形态节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenFormVfx.cs)、[Power](../../src/Core/Models/Powers/KarenFormPower.cs) |
| 19 觉醒回合抽牌脉冲 | 仅真正回合起手抽到牌时加速风场／发声 | 保留 | 触发与能力收益准确对应，比整回合一直强风更有信息。多张起手应读成一次连贯的抽牌过程，不宜每张都重启完整开场。 | H；[回合抽牌专项](../testing/form-draw-vfx-2026-09-30.md) |
| 20 命运舞台的再生产 | 短密集卡流入场，之后随真实补牌显示供给，背景参与降亮 | 微调 | 供给不断发生的主题准确；26 个视觉槽位和约 0.883 秒涌入已是合理收束。和塔召回共存时，应确保“补入库存”与“取回手牌”方向不同，不用再延长前奏。 | H；[V6记录](../testing/reproduction-v6-short-surge-2026-10-10.md)、[卡流实现](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenPromiseStarNode.Reproduction.cs) |

## 3. 攻击与命中

| 编号／对象 | 当前表现与玩法对应 | 判断 | 理由与建议 | 证据 |
| --- | --- | --- | --- | --- |
| 21 共用斩击／格挡 | 普通打击等复用原生 slash／原生收益 UI | 保留 | 低频大演出需要普通牌的克制来衬托。没有必要为每张数值牌另造一层舞台和星雨；原生伤害数字与命中节拍应继续作为基准。 | H；[打击接线](../../src/Core/Models/Cards/basic/KarenStrike.cs) |
| 22 NONNON哒哟 | 横向剑风、非规则碎屑和降力量原生反馈 | 保留 | 横扫轮廓与动作清晰，和局部单斩有区别。保持边缘破碎、主风刃清楚；重播时缩短余韵，避免每次叠出同样亮度的长弧。 | H；[NONNON专项](../testing/nonon-vfx-2026-10-10.md)、[节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenNononVfx.cs) |
| 23 《我们犯下的罪过》目标 hover | 指向目标时聚光预告，退出取消 | 保留 | 给单体重击明确的舞台目标感。当前只对本地操作者预览的归属合理；预告应弱于真正落灯，不提前播放命中反馈。 | H；[卡牌与hover](../../src/Core/Models/Cards/shine/KarenCarryingGuilt.cs) |
| 24 《我们犯下的罪过》落灯命中 | 真实命中回调触发红色垂直舞台灯／冲击 | 保留 | 竖向轮廓有独立性，既与卡牌主题一致，也没有伪装成普通剑斩。伤害增长无需无限放大灯宽；数字与目标身体保持可见。 | H；[命中节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenStageLightImpactVfx.cs) |
| 25 棒球 | 蓄势、带缝线的球飞行、真实命中；后续四种保留能力由原生 UI 表达 | 保留 | 读得出“球被打出去”，攻击部分已经独特。暂不为四种 Buff 再加四套连续演出；球的轮廓优先于长尾亮度。 | V；[棒球专项](../testing/baseball-vfx-2026-10-15.md) |
| 26 背靠背 | 红宽弧＋冰蓝细斩，在同一命中点交叉，库存影响视觉强度但有饱和 | 保留 | 是较成功的双人语义转攻击动作；没有用卡图物件代替打击。保留清楚交点，尽量缩短横贯敌人身体的细白长线余留，不再扩大面积。 | V；[实机命中](../worktree-tasks/karen-back-to-back-vfx-20261016/runtime-impact.png)、[任务记录](../worktree-tasks/karen-back-to-back-vfx-20261016/README.md) |
| 27 嫉妒 | 角色原位，无中间攻击轨迹；实际命中产生短切光，64 方片＋24 星闪朝玩家回飘，扇角42°／46° | 微调 | TV真昼的多彩闪亮方片就是既定角色参考，彩纸感本身适合此牌。保留粉／黄／绿／紫片面、侧翻、向玩家回飘和现有角度；改善片面柔光与边角短闪，并修正命中短切光。初次以“礼花感”否定配色的意见撤回；不恢复身体突进或奖励演出。 | V；[最终角度专项](../testing/jealousy-angle-v7-2026-10-16.md)、[粒子源码](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenJealousyVfx.Particles.cs) |
| 28 永不遗忘的闪耀 | 最新透明刀光主体等比定位，外缘独立粒子，命中蓝金冲击；专属声音与耗尽奖励演出均已删除 | 微调 | 新刀光比历史大弧更干净，当前大小和轮廓应保留。命中放射纹比刀光复杂，容易使“这一刀”的辨识输给爆闪；建议先收束命中纹理的亮度／余韵，不重画当前已收束的主体，不补回音效。 | V；[最新重制专项](../testing/unforgettable-shine-redraw-2026-10-16.md)、[参考与实机对照](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/redraw-stable.png) |
| 29 续演命中与消耗 | 原生 Shiv 节拍，附加红金星屑消耗；独立节点观察原生消耗 | 保留 | 这是会高频生成／打出的牌，小而准的反馈最合适；独立视觉不碰池化 NCard 的做法也值得沿用。不能升级为每张一次全屏终幕。 | H；[续演实现记录](encore-vfx/game-implementation.md)、[消耗记录](encore-vfx/exhaust-implementation.md) |

## 4. 资源、运牌与增益卡

| 编号／对象 | 当前表现与玩法对应 | 判断 | 理由与建议 | 证据 |
| --- | --- | --- | --- | --- |
| 30 投下燃料 | 从卡牌爆散为全屏火星，再向角色／回能方向收束；真实回能确认与耗尽接续分离 | 微调 | 爆散→兑现的动作语义成立，特殊耗尽不重现卡面也正确。它本身是常用资源牌，和激情全屏粒子相邻时辨识容易靠颜色；应坚持“先爆开后回收”的运动特征，连播优先减余韵。 | H；[燃料节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenDropFuelVfx.cs)、[后续整合](../testing/shine-fuel-vfx-2026-10-10.md) |
| 31 星光指引 | 短金色屏边闪光，来源牌位置的星光进入真实库存轨道 | 保留 | 一次性集中整理牌组，短全屏提示合理；重点仍是多来源汇入同一库存。无闪耀牌时不应靠同强度星雨伪造大量入堆。 | H；[指引节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenStarGuideVfx.cs) |
| 32 命运的齿轮：打出 | 屏边钢齿轮进入，角色后齿轮最后保留，短机械声 | 微调 | 具象形态与其他星光／幕布有差异，值得保留。屏边机械轮偏写实，与角色风格稍分离；建议柔化材质对比，勿改回通用法阵。速度模式另见下文待核查项。 | V；[齿轮专项](../testing/geer-vfx-2026-10-08.md)、[齿轮节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenGeerVfx.cs) |
| 33 命运的齿轮：回合触发 | 所属角色后方约0.62秒局部齿轮，随后真实选牌自动打出 | 保留 | 回合收益使用局部提示，强度低于首次施放，分工合理。多层能力应避免每层都重放屏边大齿轮；当前规则等待不能改为等待本机动画完成。 | S；[管理器](../../src/Core/PromisePileSystem/Vfx/KarenGeerVfxManager.cs)、[Power](../../src/Core/Models/Powers/KarenGeerPower.cs) |
| 34 约定之塔桥 | 手绘桥景在角色和牌后展开，真实自动打出的牌各有聚光，完成后退场 | 微调 | 大招场景和连续自动打牌相配；属于能合理占据大画幅的少数效果。桥梁横跨画面会抢目标，优先压暗非当前出牌位置的桥体，保留当前牌灯。空库存不应完整展开。 | H；[桥节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenBridgeVfx.cs)、[最终灯光修订](../testing/bridge-v14-rail-glow-2026-10-10.md) |
| 35 Banana蛋糕 | 局部食物光／瓶子组装，沿原生药水飞行落入药水栏；满槽消散 | 保留 | 可见产物与实际收益一致，满槽不播放成功爆亮尤其正确。保持生活感和局部范围，避免变成通用金色施法。 | H；[Banana节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenBananaVfx.cs)、[六牌记录](../testing/vfx-catalog-2026-10-14.md) |
| 36 Banana什锦 | 暖烟／食物质感，临时力量真实增加后确认 | 微调 | 食物和蒸汽适合此牌；与永久力量版的区别不宜只靠色温。建议使用短促升腾与更快退场表达临时增益，不添加新图标遮住力量层数。 | H；[食物绘制](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenBenefitVfx.Food.cs) |
| 37 Banana晚餐 | 食物暖光与真实力量增加确认 | 微调 | 主题合适，但同一力量 UI 容易使普通／临时力量看起来只是换皮。建议收束点更稳、余韵略沉，和什锦形成节奏差；不必扩大到全屏。 | H；[Banana接线](../../src/Core/Models/Cards/token/KarenBanana.cs) |
| 38 什锦→晚餐变换 | 读取真实变换来源，光流确认生成结果 | 保留 | 是值得保留的状态变化提示，能解释库存中牌为何变了。不能让原生粉色移牌尾迹承担全部辨识；保持变换前后身份对应。 | H；[什锦模型](../../src/Core/Models/Cards/promisePile/KarenSmallSnack.cs)、[增益实现](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenBenefitVfx.cs) |
| 39 门票：打出 | 透视收窄的铺光路，成功获得能力后交给真实 Power 图标 | 保留 | “获得通往舞台的入口”与每回合生成塔牌的机制相合；当前已改连续地面投影，不再按旧悬浮横条评价。保留透视，不再叠实体票券。 | V；[修订记录](../testing/vfx-revision-2026-10-14.md)、[当前光路绘制](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenStageCueVfx.Drawing.cs) |
| 40 门票：回合回返 | 短光路回返与实际生成的约定之塔手牌交接 | 保留 | 比重新播放整套铺路更适合回合频率；看得出收益去了手牌。层数很多时宜合并背景，只让真实生成牌分别点亮。 | V；[门票Power](../../src/Core/Models/Powers/KarenTicketPower.cs) |
| 41 两人的梦想：打出 | 暖／冷双向光粒汇聚，真实获得能力后确认 | 微调 | 双向汇聚能表达“两人”，但也容易与命运与本能、命运交换混为一类。建议坚持“汇成一个生成源”，不增加交叉运输或长绸带。 | V；[增益舞台绘制](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenBenefitVfx.Stage.cs) |
| 42 两人的梦想：回合结束生成 | 实际生成光辉，再向约定库存／空虚入口交接 | 保留 | 触发时机与目的地有用，区别于回合开始的门票。应把生成光辉作为峰值；回合结束按钮和敌方接管不需要另被整屏照亮。 | V；[梦想Power](../../src/Core/Models/Powers/KarenWeKownPower.cs)、[修订记录](../testing/vfx-revision-2026-10-14.md) |
| 43 光辉 | 从真实打出卡图散光，收益发生后向能量计数器交接 | 保留 | 把卡图亮星转成资源路径，来源与目的地都清楚，是值得复用的思路。没有实际回能时不应同样闪能量槽。 | H；[光辉绘制](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenBenefitVfx.Radiance.cs) |
| 44 一起闪耀吧 | 队伍脚灯，逐个确认实际加入的战后闪耀牌奖励 | 保留 | 多人牌使用队伍分布的光，比只照施放者更合适。光从各受益者脚边上升即可，不应画成所有人立刻得到手牌。当前单人目录展示不能充当多人同步验收。 | H；[卡牌](../../src/Core/Models/Cards/shine/KarenShineTogetherAll.cs) |
| 45 命运与本能 | 双星起势，真实约定／空虚取牌与实际回能分别交接 | 保留 | 一个动作连接两项收益，同时保留各自真实落点。应保持比大招更短，避免为了双人主题另加第二套屏幕背景。 | H；[卡牌接线](../../src/Core/Models/Cards/promisePile/KarenOnStage.cs) |

## 5. 舞台能力、奖励、选择与终幕

| 编号／对象 | 当前表现与玩法对应 | 判断 | 理由与建议 | 证据 |
| --- | --- | --- | --- | --- |
| 46 Starlight 第一幕：打出 | 上升柔光，能力确认后送入图标 | 微调 | 舞台开场感合适，但和门票、Position 0 的光束需要分工。第一幕宜强调上升、展开，避免再加同样的脚底双聚光。 | H；[舞台节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenStageCueVfx.cs) |
| 47 第一幕：闪耀后抽牌 | 实际抽到的卡面短扫光与小星芒 | 保留 | 高频能力不重启大开场，读得到“这几张是刚抽来的”。满手、抽不到牌时不显示虚假到达；多张反馈可交错但不叠白。 | H；[第一幕Power](../../src/Core/Models/Powers/KarenStarlightPower.cs) |
| 48 Starlight 第二幕：打出 | 冷白放射灯阵与暗场，光束终点按当前角色地面定位，随后交给能力图标 | 微调 | 有大招级舞台感，但场面等级应只占首次施放。旧录像中扫到视口底部的问题已有后续源码修订，本报告不把它当现存问题；建议继续控制中心灯源峰值，防止和入场共用“开灯奇观”。 | H；[场景地面修订](../testing/shine-motion-2026-10-15.md)、[当前绘制](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenStageCueVfx.Drawing.cs) |
| 49 第二幕：hover／真实重播持有 | 单个背光场跨完整真实出牌任务持有，等待相关耗尽结束再退 | 保留 | 将多次重播读成同一段演出，比每次重新开灯更合适。hover 取消必须短收尾；强光只强调当前播放，普通悬停应保持低对比。 | H；[持有管理](../../src/Core/Vfx/KarenStarlightStage.cs)、[舞台补丁](../../src/Core/Patches/KarenStarlightStagePatch.cs) |
| 50 星光闪耀之时 | 聚光／有限星轨，逐次反馈真实自动出牌，最后确认战后奖励 | 保留 | 多次播放与剩余闪耀相关，允许一套比普通技能更完整的时序。最多12个显示槽但规则不截断合理；不应把星轨槽数误当真实次数上限。 | S；[实现与限制](../worktree-tasks/karen-starlight-hour-vfx-20261015/README.md)、[节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenStarVfx.cs) |
| 51 激情卡牌 | 独立全屏红金粒子，单次约2.1秒，连续触发有320槽上限；不增加规则等待 | 微调 | 符合用户指定的全屏粒子方向，也和 Buff 局部提示分开。红金散点本身偏通用，应保留规模上限与角色静区；第二幕叠加时优先降背景粒子，不删全屏方向。 | V；[最新双层记录](../worktree-tasks/karen-passion-vfx-20261015/README.md)、[卡牌粒子](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenPassionCardVfx.cs) |
| 52 激情 Buff 获得／叠层 | 真实正增量时放大已有图标，显示本次+N，星屑收向实际图标；无提示音 | 保留 | 这是“预约战后遗物”的正确共用确认，来源不限于本卡。图标与文字比信封更直接；不得恢复旧信封或 Buff 音效。其他牌的专属奖励演出应避让它。 | V；[Buff节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenPassionVfx.cs)、[奖励命令](../../src/Core/Commands/ExtraRewardCmd.cs) |
| 53 骄傲 | 星形扣子翻转、红色易伤确认、金光送向激情图标 | 优先优化 | “付代价换战后奖励”表达完整，但加入通用激情提示后，同一图标可能被两套运输／放大同时强调。保留真实易伤与扣子主题，优先明确谁负责最终奖励确认；不恢复旧圆扣。此组合问题为静态推断，尚未实机证实遮挡。 | V＋静态推断；[骄傲专项](../testing/arrogant-vfx-2026-10-15.md)、[骄傲接线](../../src/Core/Models/Cards/relic/KarenArrogant.cs) |
| 54 唤醒：选择期间 | 候选卡背后全屏红金流动幕布，选满收向双星，窥视时隐藏 | 优先优化 | 当前已经明显且有动态，方向满足用户请求；但大面积高对比绸幕持续运动，会与“读候选牌”争注意力。保留全屏，建议开场完成后进入慢速低对比持有，选中／确认时再抬峰值；不要回退到不可见的细线方案。 | V；[当前唤醒记录](../worktree-tasks/karen-wake-up-vfx-20261015/README.md)、[选择节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenWakeUpSelectionVfx.cs) |
| 55 唤醒：实际回手 | 全屏收束接三条局部光绸，尾迹观察真实移动，落手后退出 | 微调 | 选择与回手形成闭环，比毫无关联的星爆好。三条光绸和约定塔原有取牌尾迹可能一起出现，建议一次运输保留一个主轮廓，减少贴着卡牌的重复亮边。 | V；[回手节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenWakeUpCardVfx.cs) |
| 56 《为什么要离开？我的朋友》选择边光 | 当前认可构图的整幅红金屏边光，Shader连续流动，中央留空，确认／窥视控件避让 | 保留 | 相比早期角色周围绸带，现方案跟随实际选择状态，功能归属清楚。保留已确认的左下／右上曝光构图；只控制持续峰值与选择文字可读，不加回背景窗景／实体装饰。 | V；[当前流动版](../worktree-tasks/karen-why-vfx-20261016/README.md)、[节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenWhyVfx.cs) |
| 57 最后的台词：可打出提示 | 条件满足时卡缘与角落蝴蝶／粒子，条件失效清理 | 保留 | 条件特殊的终结牌需要独立可用提示。比所有时候持续闪亮更有用；建议粒子稀疏，不能比卡名和可打出状态更抢眼。 | H；[提示节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenLastWordPlayableVfx.cs) |
| 58 最后的台词：终幕 | 本机视频＋文字／蝴蝶；远端文字，缺视频回退；正常序列等待6.7秒，纯文字0.7秒，Instant跳过 | 微调 | 满足极特殊终结条件后，全屏收尾有资格存在。它是目前最重的节奏中断，建议后续提供独立“短终幕／跳过视频”偏好，默认可保留；这涉及结算和各端时序，不能只删本地画面或更改某端等待。不是本次直接改动。 | V；[终幕卡牌](../../src/Core/Models/Cards/neutral/KarenLastWord.cs)、[视频节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenLastWordVideoVfx.cs) |

## 6. Position 0、交换与多人接力

| 编号／对象 | 当前表现与玩法对应 | 判断 | 理由与建议 | 证据 |
| --- | --- | --- | --- | --- |
| 59 Position 0：固定打出 | 暖金双聚光、金尘，脚底硬横／竖线已删除 | 保留 | 占据舞台位置的表达明确，且按用户要求与后续触发分离。保持柔和地光，不恢复像第二根血条的横线，也不让 hover 重启此开场。 | V；[当前软地光记录](../worktree-tasks/position-zero-vfx/README.md) |
| 60 Position 0：实际格挡 | 真实格挡正增长后，冷白交叉扫灯与地光短闪，约0.76秒 | 保留 | 冷／暖加不同运动方式，比只换颜色更能区分“已获得能力”和“能力生效”。多次触发宜合并照明包络，不放大成第二幕的全场灯阵。 | V；[格挡Power接线](../../src/Core/Models/Powers/KarenPosition0Power.cs)、[节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenPositionZeroVfx.cs) |
| 61 Position 0：剩余机会与攻击hover | 还有真实攻击机会才亮常驻脚灯，本地攻击牌hover有小T和灯光标；用尽熄灭、下回合恢复 | 保留 | 全项目中信息价值较高的设计，玩家能预判下一次攻击是否返格挡。地面硬T已删而卡框小T仍保留，两者不能混淆；注意和闪耀警告共用卡框时的主次。 | V；[机会／hover记录](../worktree-tasks/position-zero-vfx/README.md)、[卡牌hover节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenPositionZeroCardHoverVfx.cs) |
| 62 命运交换之日 | 红蓝短弧绸带掠交，在本节点实际交汇标记后执行换牌，六层材质收束；实体头饰已删除 | 保留 | 双向交叉对应手牌与库存交换，V4短动作比绕行更贴合操作频率。保留交汇绑定真实换牌，不再加头饰或拉长起手；单向／两空场景应只显示实际发生的方向。 | V；[当前V4记录](../worktree-tasks/exchange-fate-vfx-20261016/README.md)、[节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenExchangeFateVfx.cs) |
| 63 接力：转给队友 | 成功移入队友牌组后，红金网格丝带、星携卡和接收者短光，保留原生牌组预览 | 保留 | 路径指向真实队友，能表达这张牌离开本机的原因。注意目标是队友牌组，不应做出直接加入当前手牌的暗示；原生预览与自有卡形不要长时间重叠。 | V；[双端专项与夹具说明](../worktree-tasks/karen-fight-relay-vfx-20261015/README.md)、[卡牌接线](../../src/Core/Models/Cards/shine/KarenFightRelay.cs) |
| 64 接力：耗尽终幕 | 真实遗物预约完成后星冠勋章与金屑，同时也会增加通用激情Power | 微调 | 终幕与非耗尽传递分开合理，但与通用Buff反馈有重复确认风险。保留接力结束的短强调，让一个图标／数字承担奖励确认；双Karen专项不等于混合队伍全矩阵通过。 | V＋静态推断；[接力节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenFightRelayVfx.cs)、[奖励接线](../../src/Core/Commands/ExtraRewardCmd.cs) |

## 7. 其他共用反馈与外围表现

| 编号／对象 | 当前表现与玩法对应 | 判断 | 理由与建议 | 证据 |
| --- | --- | --- | --- | --- |
| 65 禁用遗物：hover预告 | 对实际可禁用遗物挂星光，取消时退出 | 保留 | 属于重要的代价预览，尤其不能被普通奖励庆祝替代。预告局限在遗物，不提前播放吸收。历史目录的部分预告由回调夹具触发，不代表真实鼠标端到端已测。 | H；[hover补丁](../../src/Core/DisableRelicSystem/Patches/DisableRelicHoverPatch.cs) |
| 66 禁用遗物：实际吸收 | 从真实遗物位置向玩家或能量图标移动，星罪与摘星按收益区分 | 保留 | 代价来源与收益目的地都清楚，后续已有图标落点修订。建议只保留一次遗物破碎／吸收，避免和能量计数器原生效果重复爆亮。 | V；[吸收节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/DisableRelicSystem/Vfx/NDisableRelicAbsorbVfx.cs)、[落点修订](../testing/vfx-revision-2026-10-14.md) |
| 67 能量计数器前后粒子 | 红色角色专属计数器，能量变化重启自己的前后层 | 保留 | 功能明确、面积小，适合作为燃料／光辉的最终接收端。所有回能牌共用这一端，不要逐牌改写它的颜色和时长。 | S；[计数器场景](../../scenes/combat/energy_counters/karen_energy_counter.tscn)、[变化补丁](../../src/Core/Patches/NEnergyCounterOnEnergyChangedPatch.cs) |
| 68 卡牌尾迹／选中瞄准 | 原生继承节点和角色场景资源；SN类是当前场景入口，SK空壳不算第二套 | 保留 | 原生粉色尾迹贯穿许多录像，不应误认成嫉妒／刀光新增粒子。共用运输保持一致即可；专属效果辨识应来自主动作，不靠再堆一条长尾。 | H；[尾迹场景](../../scenes/vfx/card_trail_karen.tscn)、[瞄准场景](../../scenes/ui/selection_reticle.tscn) |
| 69 角色待机与死亡转场 | 待机呼吸／披风形变，死亡时淡出原图、幕片与尸体图接替 | 待验证 | 静态上能减少纸片感，死亡有独立收束；但本次未看完整死亡／恢复录像，不能评价动作自然程度。应重点补原生受击、入场中死亡、复活／重建时材质复原。 | S；[角色表现](../../src/ShoujoKagekiAijoKaren/GodotScripts/Node/SNCreatureVisuals.cs) |
| 70 选角色视频 | Karen专属视频、音效和菜单音乐恢复，重复选择有一分钟冷却 | 待验证 | 独立选角界面适合较完整身份展示；有冷却避免反复播放，方向合理。本次没有重看选角片的完整起止与快速切角色，不能给画面／音画验收结论。 | S；[选角控制器](../../src/Core/Patches/KarenCharSelectVideoController.cs) |
| 71 商店／休息点角色表现 | 角色独立场景，继承原生商店／休息点节点 | 待验证 | 场景接入存在，但不据空壳继承就宣称有独立华丽特效。当前不建议为外围场景补全屏舞台；如后续要提升品质，先补实际画面证据。 | S；[商店场景](../../scenes/merchant/characters/karen_merchant.tscn)、[休息点场景](../../scenes/rest_site/characters/karen_rest_site.tscn) |

## 8. 单项合理、组合仍需收口的地方

### 8.1 奖励确认应只有一个主要落点

这是本报告最优先的组合核查项。当前 [ExtraRewardCmd.AddRelicReward](../../src/Core/Commands/ExtraRewardCmd.cs) 在登记额外遗物后会增加 `KarenPassionPower`，而这个 Power 的正增量会自动播放通用 `NKarenPassionVfx`。同时：

- 骄傲还会执行自己的 `ConfirmReward()`，把金光送向激情图标。
- 接力耗尽还会执行 `RewardRegistered()`，播放星冠勋章终幕。
- 嫉妒的专属奖励演出虽已删除，其耗尽仍调用原有奖励命令，因此可能出现**通用**激情 Buff 提示；这不等于旧专属演出被恢复。

可从代码确认“这些入口会共存”，不能只凭代码断言“一定互相遮挡”。建议先录这三条真实奖励路径，确认同一笔 `+N` 不被读成两份奖励，再决定由通用图标提示负责最终数值、专属演出负责主题起势。保持激情 Buff 无提示音，不用给所有来源都追加新奖励动画。

### 8.2 当前亮度协调有范围，不能叫作全特效互斥

[KarenVfxComposition](../../src/Core/PromisePileSystem/Vfx/KarenVfxComposition.cs) 的既有做法合理：取最强事件抑制，而非把每个节点／每个玩家的抑制反复相乘；旧常驻四层同时启用时基础权重最低0.58。注册的局部事件最大再降16%，第二幕全舞台事件最大再降28%，前景命中和 HUD 不应一并变暗。

但有两个边界：

1. 常驻计数只包含燃烧、过去与未来、再生产、觉醒。Position 0 的待机脚灯虽然读取 `AmbientWeight`，自身并不是这四项计数之一。
2. `NKarenPassionCardVfx` 有角色／HUD静区，但没有注册／读取这套事件权重。嫉妒等局部命中也没有统一走这条调光路径；独立选择 UI 又有自己的层级。

这些不自动构成缺陷。局部命中本来就可保持清楚，选择 UI 也不应被战场装饰控制。真正需要补的是“全屏激情粒子＋第二幕＋旧四常驻＋Position 0”的主次验证：先减少背景叠亮，再保住命中、当前牌和收益落点。原检出正在进行的未提交共存修改不在本快照结论内。

### 8.3 给灯光分配不同任务

| 视觉元素 | 建议固定任务 | 避免的重复 |
| --- | --- | --- |
| 入场顶灯 | 每场一次角色亮相，与原生开场并行 | 不复用于每张技能的起手 |
| Position 0 暖灯／冷灯 | 暖灯表示占位，低光表示有机会；冷交叉灯表示真实格挡 | 不把每次格挡演成第二幕 |
| 第一幕上升光 | 建立持续抽牌能力，之后只扫刚抽到的牌 | 不重复铺全屏舞台 |
| 第二幕冷白背光 | 给一次完整重播任务搭台 | 每次重播不重新开场 |
| 罪过的红色落灯 | 只点当前敌人，命中形成竖直攻击 | 不扩大成友方全队增益 |
| 门票的地面光路 | 连接入口与实际回返手牌 | 不变成悬浮横条或通用光柱 |

### 8.4 速度模式需核查，不凭旧目录推定

> 2026-10-17 后续：齿轮速度专项已实施 Instant 创建／运行期检查，普通与快速节拍、卡牌及 Power 规则保持。双版本实测与前后视频见[后续专项](../testing/geer-speed-vfx-2026-10-17.md)。下述原文保留为审查基线。

齿轮是明确值得核查的静态差异：[管理器](../../src/Core/PromisePileSystem/Vfx/KarenGeerVfxManager.cs) 只检查 `NonInteractiveMode`，没有与多数新专属演出相同的 Instant 判定；[节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenGeerVfx.cs) 用原始 `delta` 推进，施放约1秒、触发约0.62秒；卡牌与 Power 的固定规则等待又分别是1秒和0.65秒。

因此能确认“代码策略不同”，本次没有实机确认 Instant 最终可见多少帧。建议先按三种速度做单卡核查，再决定只调整视觉开关／时钟，保留多人一致的规则时序。燃料、闪耀图标和查看界面有功能性短反馈，本就允许作为例外，不应用机械替换把它们一起删掉。

最后的台词则是有意保留的重序列：正常视频路径6.7秒，远端文字显示后也可能按视频资源可用性等待同一段时间。若做短终幕偏好，必须先设计各端一致的结算方式，不能让本地渲染决定网络进度。

## 9. 推荐处理顺序与验收方式

这是后续建议，本次没有实施以下修改或运行这些测试。

| 优先级 | 工作 | 最小可复核结果 |
| --- | --- | --- |
| P1 | 奖励确认去重：骄傲／接力／嫉妒耗尽与通用激情 | 双版本各走真实奖励路径；画面只读出一笔预约，数值／战后结果一致，Buff无专属声音；保留前后截图 |
| P1 | 新旧大效果同时出现的层级 | 普通塔／空虚各一组：旧四常驻＋Position 0，再放第二幕和激情；目标、伤害、卡名与落点持续可读，结束后背景恢复 |
| P2 | 嫉妒命中材质与动势 | 原位、无中间轨迹、向玩家回飘和现有收窄角度保留；以原速比较接触前后约0.3秒，真实命中短切光明确，彩纸仍可辨认为TV真昼风格 |
| P2 | 唤醒长时间选择的持有态 | 保留全屏与动态；停留5～8秒能读牌，确认时再出现明显收束；窥视／取消无残留 |
| P2 | 齿轮速度模式（[2026-10-17 已完成](../testing/geer-speed-vfx-2026-10-17.md)） | Normal／Fast／Instant分别记录节点可见时间与规则完成时间；不引入“等本机动画”的结算依赖 |
| P2 | 手牌状态层级 | 同手牌覆盖普通闪耀、临界、燃烧、可打出、Position 0 hover；不靠颜色也能分清警告与可用状态 |
| P3 | 高频回合特效与大招余韵 | 连续门票／梦想／第一幕／觉醒收益只出现一次主节奏；燃料和刀光余韵不遮下一张牌 |
| P3 | 外围表现补证据、终幕短演出偏好 | 死亡／恢复与选角切换有实际录像；短终幕先写清各端时序，不直接改等待 |

涉及真实移牌的验证仍须通过 KarenSTS2MCP 保存并查看操作前后截图，动画结束执行 `karen_check_hand`，核对模型与可见节点。多次重播以真实播放任务结束为准。日常按变更针对性验证；只有用户要求或发布前才跑 full，同版本多人组合不能用单人录像替代。

## 10. 已有证据不能推出的结论

- 2026-10-14 的49段合辑是历史实现，并非本次 main 的最终全览；闪耀运动、第二幕地面、嫉妒、Why、刀光等必须看各自后续版本。
- 既有记录里双版本单卡专项很多，但本次没有重跑。入场、接力的部分同版本联机结果也不能扩展为所有卡牌的完整多人矩阵通过。
- 原生 `Invalid Task ID`、强制切房卡池重复回收、个别历史启动资源问题均以各专项原记录为准，不因视觉合适而改写为“日志全绿”。
- 本次对嫉妒／唤醒的判断是实机采样下的审美与信息层级意见；没有测得“帧耗时过高”。CPU／GPU粒子类型或槽位数本身也不能直接证明性能好坏。
- 没有主观试听，因而不评价现有音效是否悦耳或足够响；用户要求的激情 Buff 静音、永不遗忘去专属声音保持有效。
- 当前静态审查没有发现必须整体返工的理由，也没有足够证据宣布整合版全面兼容。

## 11. 实现覆盖索引与版本选取

| 实现族 | 本报告覆盖 | 当前版本入口／依据 |
| --- | --- | --- |
| `NKarenCombatEntranceVfx`、`SNCreatureVisuals.Entrance`、塔入场部分 | 01、02、69 | [入场任务](../worktree-tasks/karen-battle-entrance/README.md)；保留默认开关与本机归属 |
| `NKarenPromiseStarNode`及Tower／Interaction／Recall／Void／VoidOrbit／Reproduction分部，`NKarenPromiseCardEntryVfx`、`NKarenTowerRecallCardVfx` | 02～08、20 | [牌堆管理器](../../src/Core/PromisePileSystem/Vfx/KarenPromiseVfxStarManager.cs)、[塔专项](../testing/promise-tower-vfx-2026-10-15.md) |
| `NKarenPileViewVfx`、`NKarenShineAmbientVfx`／`NKarenShineWarningVfx`／`NKarenShineDepleteVfx`／`NKarenShinePileArrivalVfx`、`NTopBarShinePileButton` | 09～14 | [Shine接线](../../src/Core/ShineSystem/ShineVfx.cs)、[图标记录](../testing/shine-pile-arrival-2026-10-15.md) |
| `NKarenBurnVfx`／`NKarenBurnCardVfx`／`NKarenPastAndFutureRingVfx`／`NKarenFormVfx`及Manager | 15～19 | [Power状态接线](../../src/Core/Models/Powers/KarenPromisePilePower.cs) |
| `NKarenNononVfx`、`NKarenStageLightVfx`、`NKarenStageLightImpactVfx`、`NKarenBaseballVfx`、`NKarenBackToBackVfx` | 22～26 | 各条目的模型／最新专项；未把测试Probe计成生产特效 |
| `NKarenJealousyVfx`与Drawing／Particles | 27 | 第七版42°／46°，非身体突进版、非65°／70°宽扇版 |
| `NKarenUnforgottenShineVfx` | 28 | `slash-body.png`新参考重制、无专属音效；旧`crescent.svg`及历史大弧不是当前主体 |
| `NKarenContinueVfx`、`NKarenContinueExhaustVfx` | 29 | 原生命中节奏＋独立消耗节点；双版本原生消耗类分别适配 |
| `NKarenDropFuelVfx`、`NKarenStarGuideVfx`、`NKarenGeerVfx`、`NKarenBridgeVfx`、`NKarenDrawPileDestroyVfx` | 06、30～34 | 实际生产调用，不将历史原型当现状 |
| `NKarenBananaVfx`、`NKarenBenefitVfx`及Food／Flow／Stage／Radiance | 35～45 | 香蕉三类、变换、梦想、光辉、队伍脚灯；枚举中旧Ticket／食物分支不另算新特效 |
| `NKarenStageCueVfx`与Drawing、`KarenStarlightStage` | 39～40、45～49 | 门票当前地面光路、第二幕当前地面终点、整任务持有 |
| `NKarenStarVfx`、`NKarenPassionCardVfx`、`NKarenPassionVfx`、`NKarenArrogantVfx` | 50～53 | 激情双层且Buff静音；骄傲五角星扣，非旧信封／圆扣 |
| `NKarenWakeUpSelectionVfx`、`NKarenWakeUpCardVfx`、`NKarenWhyVfx` | 54～56 | 唤醒第五版动态幕布；Why已认可构图的连续流动版，非旧角色绕绸带 |
| `NKarenPositionZeroVfx`、`NKarenPositionZeroCardHoverVfx`、`NKarenExchangeFateVfx`、`NKarenFightRelayVfx` | 59～64 | Position 0当前软地光、命运交换V4无头饰短动作、接力真实传递／耗尽分流 |
| `NKarenLastWordPlayableVfx`、`NKarenLastWordVfx`、`NKarenLastWordVideoVfx` | 57～58 | 可用预告、文字回退与视频三部分分别评价 |
| `NDisableRelicStarBurstVfx`、`NDisableRelicAbsorbVfx`、能量／尾迹／瞄准场景 | 65～68 | 禁用预告与真实吸收分开，SN生产场景与SK空壳区分 |
| 角色／选角／商店／休息点节点 | 69～71 | 仅实际接线的静态适配；未补新的完整视觉验收 |

[任务记录](../worktree-tasks/vfx-suitability-review-2026-10-16/README.md)保存原始请求、工作树与验证状态。本文只提出取舍和后续验收目标，没有改动卡牌规则、运行特效、测试工具或共享游戏部署。
