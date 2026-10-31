# 全特效一批优化与审片记录

星光闪耀之时、两人的梦想已按机制重做，当前演出及前后视频见[保留主体的两牌简化](star-dream-preserve-animation-2026-10-31.md)；本文这两张的录像保留为上一候选历史参考。


后续修订：防御/坠落已恢复之前认可的朴素守势，最新视频与最终包核验见[无盾守势恢复记录](guard-plain-restore-2026-10-31.md)。本页保留首轮候选0eca53e的实录与统计。

2026-10-31。基线 `main 0ef1ab4`；候选分支 `codex/card-vfx-audit-20261029`。

已将五阶段合为一批：五张基础牌获得独立表现；16个既有效果及辅助类型处理内部重复工作。原目录73个类型和4个目录外入口均有复核结论，其余条目保留现有设计。当前目录为74个类型、39个共享资源准备者、167项资源。

候选留在任务工作树供审片。主分支保持 `0ef1ab4`，原目录9个文件哈希保持；工作树、旧包、新包与所有录像保留。

## 分组视频

全部为真实出牌，左侧改前、右侧改后，原速播放，使用改后引擎音轨；每段字幕分别说明改动与保留内容。两版各21个逐牌比较片、5个合辑，原始有声录像独立保留。

| 小组 | 内容 | 正式版 | 测试版 |
| --- | --- | --- | --- |
| 基础五张 | 斩面、护面与真实移牌交接 | [视频](../../artifacts/all-vfx-batch-20261031/comparison/01-basics-stable.mp4) | [视频](../../artifacts/all-vfx-batch-20261031/comparison/01-basics-beta.mp4) |
| 剑技与伤痕 | 三层刀带、贯穿、灯束、完整伤痕 | [视频](../../artifacts/all-vfx-batch-20261031/comparison/02-swords-stable.mp4) | [视频](../../artifacts/all-vfx-batch-20261031/comparison/02-swords-beta.mp4) |
| NONNON与燃料 | 完整粒子与雾团、流场保持 | [视频](../../artifacts/all-vfx-batch-20261031/comparison/03-particles-stable.mp4) | [视频](../../artifacts/all-vfx-batch-20261031/comparison/03-particles-beta.mp4) |
| 塔、连锁与收益 | 舞台、换牌、梦想生成与自动出牌 | [视频](../../artifacts/all-vfx-batch-20261031/comparison/04-tower-stable.mp4) | [视频](../../artifacts/all-vfx-batch-20261031/comparison/04-tower-beta.mp4) |
| 遗物与影片 | 飞星预览、吸收与完整长演出 | [视频](../../artifacts/all-vfx-batch-20261031/comparison/05-relic-video-stable.mp4) | [视频](../../artifacts/all-vfx-batch-20261031/comparison/05-relic-video-beta.mp4) |

## 每张看哪里

| 卡牌 | 实际改动或回归用途 | 正式版 | 测试版 |
| --- | --- | --- | --- |
| 打击 | 新增玫红斜向斩面，由近侧向目标上方送出；实际命中触发。 人物原位，完整主形和短碎光；原生伤害与等待沿用。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_strike.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_strike/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_strike.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_strike/game.mp4) |
| 防御 | 新增贴身折面护幕，在真实获得格挡时一次展开。 替换本次原生蓝盾提示；珍珠色边缘、淡玫红面，无延迟金环。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_defend.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_defend/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_defend.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_defend/game.mp4) |
| 闪耀打击 | 新增金色主斩面与12枚短碎光，和普通打击区分。 真实命中触发，闪耀消耗和耗尽动画继续沿用。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_shine_strike.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_shine_strike/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_shine_strike.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_shine_strike/game.mp4) |
| 坠落 | 真实格挡展开护面，轻微下落收稳；随后沿真实选牌入塔。 保留原有选牌与完整移牌路径，人物原位。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_fall.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_fall/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_fall.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_fall/game.mp4) |
| 不再遥不可及 | 新增冷色弯折斜切，命中后接原有取牌与归手。 3点伤害、取2张牌；牌面和图标沿真实位置交接。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_promise_draw.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_promise_draw/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_promise_draw.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_promise_draw/game.mp4) |
| 用你的闪耀贯穿我吧 | 内部优化：共享粒子曲线、渐变和混合材质，复用刀面和星形顶点。 保留5/10粒子、每次贯穿、三层命中星、末击与原节拍。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_shine_strike_barrage.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_shine_strike_barrage/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_shine_strike_barrage.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_shine_strike_barrage/game.mp4) |
| 背靠背 | 内部优化：红蓝刀带三层共用同一份49点路径与法线。 曲线计算减少为原来的1/3；层数、宽度、接触与碎片保持。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_back_to_back_card.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_back_to_back_card/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_back_to_back_card.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_back_to_back_card/game.mp4) |
| 永不遗忘的闪耀 | 内部优化：直接持有全部伤痕，复用邻居查询列表。 每次命中仍新增伤痕，全部累积到战斗结束，位置采样规则保持。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_shine_together.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_shine_together/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_shine_together.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_shine_together/game.mp4) |
| 我们犯下的罪过 | 内部优化：灯束共享混合材质，每帧只查一次目标节点。 各灯束仍读取实时碰撞框，原数量、方向、命中和退场保持。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_carrying_guilt.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_carrying_guilt/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_carrying_guilt.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_carrying_guilt/game.mp4) |
| NONNON | 内部优化：粒子透明度直接使用本次已计算的位置。 Full单目标1280粒子、全部碎片与雾团、刀光、风场和时间保持。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_nonon.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_nonon/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_nonon.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_nonon/game.mp4) |
| 投下燃料 | 内部优化：全部发光精灵与粒子渲染层共享不可变混合材质。 Full1468粒子和完整充能爆发保持；流场Shader仍各自持有参数。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_drop_fuel.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_drop_fuel/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_drop_fuel.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_drop_fuel/game.mp4) |
| 约定之塔桥 | 内部优化：灯光归一化复用集合；光束与星形复用顶点数组。 整幅舞台、每张实际自动出牌、光照权重和星尘保持。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_bridge.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_bridge/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_bridge.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_bridge/game.mp4) |
| 命运交换之日 | 内部优化：同向三条丝带共用每帧路径与法线。 六条丝带及原网格、交叉点、真实换牌和尾部完整保留。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_exchange_fate.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_exchange_fate/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_exchange_fate.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_exchange_fate/game.mp4) |
| 两人的梦想 | 内部优化：生成卡展示复用遍历快照，及时释放卡面引用。 原生能力给予、回合末真实光辉生成、卡面飞入塔和清理保持。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_we_kown.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_we_kown/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_we_kown.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_we_kown/game.mp4) |
| 星光闪耀之时 | 沿用已有内部优化；自动打出的闪耀打击使用本轮完整金色斩面。 5次真实播放、耗尽、奖励与整串背景演出保持。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_star.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_star/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_star.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_star/game.mp4) |
| Starlight第二幕 | 第二幕舞台保持；后续真实闪耀打击接本轮专属金色斩面。 12灯束、每次真实播放和耗尽完整保留。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_starlight02_card.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_starlight02_card/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_starlight02_card.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_starlight02_card/game.mp4) |
| 续演 | 回归对照：沿用已合并的共享材质与粒子模板优化。 原飞刀、火屑和命中层保持。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_continue.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_continue/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_continue.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_continue/game.mp4) |
| 续写 | 回归对照：保留整套逐次出牌与逐击表现。 本例实际7次播放、70点伤害；整串内容保持。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_rewrite.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_rewrite/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_rewrite.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_rewrite/game.mp4) |
| 命运的齿轮 | 回归对照：保留回合末到下一回合的真实选牌与自动出牌。 双牌选择、原齿轮舞台和真实收益保持。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_geer.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_geer/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_geer.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_geer/game.mp4) |
| 摘星 | 内部优化：预览飞星与六星吸收共享不可变混合材质。 保留全部飞星、漂散、弯曲飞行和能量落点；真实禁用遗物。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_pick_star.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_pick_star/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_pick_star.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_pick_star/game.mp4) |
| 最后的台词 | 内部优化：影片布局仅在视口尺寸变化时更新。 完整文字、影片、音轨、6.7秒命中等待和退场保持。 | [对比](../../artifacts/all-vfx-batch-20261031/comparison/stable-karen_last_word.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/stable/full-karen_last_word/game.mp4) | [对比](../../artifacts/all-vfx-batch-20261031/comparison/beta-karen_last_word.mp4) · [改后原片](../../artifacts/all-vfx-batch-20261031/after/beta/full-karen_last_word/game.mp4) |

## 实现位置与内容保留

- 基础五张：`NKarenBasicCardVfx` 提供三种攻击斩面与两种护面。三张攻击在原生命中回调绘制并播放短剑声；防御/坠落通过 `KarenBasicBlockCue` 仅替换当前卡牌、当前目标的原生格挡提示，数值和UI沿用。角色变换代码保持。
- 连刺：曲线、渐变和混合材质按原参数共享；命中Shader继续逐实例保存age。刀面、三层星形及UV保持，改用复用数组。每次完整出牌的准备加四击可省去15个重复Curve/Gradient/混合材质对象。
- 背靠背：同一条刀带三层共用49点和法线，路径求值从每侧441次降为147次；三层、宽度、颜色与碎片保持。
- NONNON：透明度复用已计算的粒子位置；生成顺序、随机调用、风场公式保持。Full单目标实录1280粒子，全部种类与6–9团雾保留。
- 伤痕：每次真实命中仍添加完整伤痕，全部保留至战斗结束；改为直接访问完整列表并复用邻居查询集合。
- 燃料、遗物飞星与灯束：共享不可变混合材质；燃料的动态流场参数继续各自持有。Full燃料实录1468粒子。
- 灯束：每帧查询一次目标节点，各灯束继续读取实时碰撞框变换。
- 塔桥：复用舞台查询集合、光束和星形数组，保留原灯光权重、星尘、整幅舞台与每次自动出牌。
- 命运交换：同向三层共用路径/法线求值；六条丝带、原网格拓扑与提交方式、真实交换和收尾保持。
- 梦想：复用逐帧卡面遍历快照，保留池化模型/父节点检查，并在收尾清空引用。
- 最后的台词：视口尺寸变化时更新影片布局；逐帧解码、原文字与影片、音轨、6.7秒等待及退场保持。
- 续演、星光、第二幕的已核验内部优化继续保留。续写、齿轮等作为相关连锁回归；字幕明确标注。选牌、常驻、选角影片、宝箱与音乐/能量UI完成静态复核，沿用现有生命周期。

源码：[`NKarenBasicCardVfx`](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenBasicCardVfx.cs)、[`KarenBasicBlockCue`](../../src/Core/Vfx/KarenBasicBlockCue.cs)。其余逐项位置见[全目录覆盖表](../../artifacts/all-vfx-batch-20261031/coverage.json)。性能变化以上述分配及计算次数为依据，本轮执行效果、规则和清理定向核验。

## 特效等级

新增表现接现有 Full/Low/Off：Low保留主斩面与护面，普通碎光7→2、闪耀碎光12→3，并沿用20Hz重绘；Off关闭新增节点，数值、移牌与原生UI继续。两版五张均实录三档。

| 基础牌 | 正式版Low | 正式版Off | 测试版Low | 测试版Off |
| --- | --- | --- | --- | --- |
| 打击 | [原片](../../artifacts/all-vfx-batch-20261031/after/stable/low-karen_strike/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/stable/off-karen_strike/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/beta/low-karen_strike/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/beta/off-karen_strike/game.mp4) |
| 防御 | [原片](../../artifacts/all-vfx-batch-20261031/after/stable/low-karen_defend/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/stable/off-karen_defend/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/beta/low-karen_defend/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/beta/off-karen_defend/game.mp4) |
| 闪耀打击 | [原片](../../artifacts/all-vfx-batch-20261031/after/stable/low-karen_shine_strike/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/stable/off-karen_shine_strike/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/beta/low-karen_shine_strike/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/beta/off-karen_shine_strike/game.mp4) |
| 坠落 | [原片](../../artifacts/all-vfx-batch-20261031/after/stable/low-karen_fall/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/stable/off-karen_fall/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/beta/low-karen_fall/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/beta/off-karen_fall/game.mp4) |
| 不再遥不可及 | [原片](../../artifacts/all-vfx-batch-20261031/after/stable/low-karen_promise_draw/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/stable/off-karen_promise_draw/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/beta/low-karen_promise_draw/game.mp4) | [原片](../../artifacts/all-vfx-batch-20261031/after/beta/off-karen_promise_draw/game.mp4) |

## 定向验证

| 项目 | 结果与范围 |
| --- | --- |
| 双版本最终构建 | 通过；正式版v0.107.1、测试版v0.111.0，最终产物与已录包指纹一致。 |
| 静态等价 | 12项通过；五张牌规则/等待、NONNON生成与随机顺序、燃料数量/时序、角色实现保持。 |
| 资源登记 | 74类型、39共享准备者、167资源清单通过。新增材质接原生准备入口。 |
| Full前后 | 两版各21段改前、21段改后，共84段；真实伤害、格挡、手牌与约定数量一致。 |
| 新基础牌Low/Off | 两版各10段，共20段；关闭档无新增基础特效节点。 |
| 音轨与产物 | 104段原片均有引擎混音、非静音音轨；包和源码SHA核对通过。 |
| 移牌与清理 | 八种移牌场景的双版本MCP前后截图已查看，动画后karen_check_hand一致；相关节点退出核验通过。 |
| 画面复核 | 基础牌连续帧、24张专属效果关键帧、4张MCP移牌拼图及字幕布局已查看。 |
| 完整帧耗时/首次使用专项 | 未测；本轮采用改动相关定向验证。 |
| 完整单人/多人矩阵、Android | 未测；本轮为Windows单人审片候选。 |

日志中保留2条历史同类原生预加载任务ID记录，均随后完成预加载；定向出牌日志未见C#异常或Shader错误。录像夹具的手牌选牌接口、重复选牌分支和战后汇总字段已修正；失败日志保留。最后的台词两段改前原片已完整落盘，汇总字段修正后依据保存的奖励状态与退出计数核验纳入。

[逐片核验数据](../../artifacts/all-vfx-batch-20261031/verified-results.json) · [解释字幕与视频哈希（正式版）](../../artifacts/all-vfx-batch-20261031/comparison/manifest-stable.json) · [测试版](../../artifacts/all-vfx-batch-20261031/comparison/manifest-beta.json) · [任务记录](../worktree-tasks/card-vfx-audit-20261029/README.md)
