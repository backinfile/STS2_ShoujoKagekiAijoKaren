# 星光与梦想特效重做

本候选f3b2c1b被用户指出删掉了主体动画，保留作历史参考；当前两牌演出见[保留主体的简化](star-dream-preserve-animation-2026-10-31.md)。


2026-10-31，改前候选 `a1416a8`，工作分支 `codex/card-vfx-audit-20261029`。本次只改两张牌的专用演出；原有朴素守势和其他牌的表现沿用上一候选。

## 前后录像

左侧改前、右侧改后，原速；片内说明修改位置，并保留改后的游戏音轨。所有原片均针对本轮存档的前后包新录。

| 卡牌 | 正式版 | 测试版 |
| --- | --- | --- |
| 星光闪耀之时 | [前后](../../artifacts/star-dream-redo-20261031/comparison/stable-karen_star.mp4) · [改后原片](../../artifacts/star-dream-redo-20261031/after/stable/full-karen_star/game.mp4) | [前后](../../artifacts/star-dream-redo-20261031/comparison/beta-karen_star.mp4) · [改后原片](../../artifacts/star-dream-redo-20261031/after/beta/full-karen_star/game.mp4) |
| 两人的梦想 | [前后](../../artifacts/star-dream-redo-20261031/comparison/stable-karen_we_kown.mp4) · [改后原片](../../artifacts/star-dream-redo-20261031/after/stable/full-karen_we_kown/game.mp4) | [前后](../../artifacts/star-dream-redo-20261031/comparison/beta-karen_we_kown.mp4) · [改后原片](../../artifacts/star-dream-redo-20261031/after/beta/full-karen_we_kown/game.mp4) |

[正式版两牌合辑](../../artifacts/star-dream-redo-20261031/comparison/star-dream-stable.mp4) · [测试版合辑](../../artifacts/star-dream-redo-20261031/comparison/star-dream-beta.mp4)

## 改动

**星光闪耀之时**：去掉专用星座、聚光、亮芯和飘带。唯一闪耀牌真实移到打出区后，牌框随真实播放事件短亮；每张牌本身的攻击/技能特效照常播放。结束后在真实战后奖励能力图标上短亮确认，奖励仍在战后领取。起手等待由0.62秒改为0.15秒，Fast为0.105秒；耗尽沿用原生流程。

**两人的梦想**：施放后确认真实能力图标；回合末真实生成光辉后，显示约200屏幕像素高的原生卡面，短程收进各自对应的约定之塔星点。多层同时显示所有实际生成模型的小扇面和数量；空虚模式收进真实抽牌堆入口。生成阶段专用星核、双螺旋和长飘带已替换。等待由触发0.64秒加交付1.12秒，改为生成后的0.60秒，Fast为0.42秒。光辉以后实际使用时仍按原卡回能。

两者Full/Low保留相同的实际次数、卡面和去向信息，Low降低几何重绘频率；Off/Instant不创建专用演出或附加等待。新演出使用原生卡面与简单轮廓，资源目录更新为77类型、38共享准备者、167资源。

## 定向验证

| 项目 | 结果 |
| --- | --- |
| 最终双版本构建与目录 | 通过；77类型、38共享准备者、167资源。 |
| 最终包实录 | 30段：改前两版各2段，改后两版各13段。完整、低档、关闭和Fast分别覆盖两张牌，另含单次/技能重播、3层梦想、空虚和中途移除。 |
| 星光 | 普通5次45伤、末点1次9伤；技能3次15格挡。次数、耗尽与实际战后奖励1次一致，真实牌面与奖励图标已绘制。 |
| 梦想 | 普通真实生成1张、3层生成3张，原生卡面数与实际数一致；空虚进入抽牌堆并于下回合抽到手中。 |
| 模型与退出 | 每例MCP前后截图和karen_check_hand一致；演出节点全部退出。26段改后场景的本演出卡面池无失效引用和脱树占用；两版中途移除均通过。 |
| 录像 | 4段逐牌比较与两版合辑带说明和改后游戏音轨；30段原片可解码且音轨有声，8段完整档原片未检出0.75秒以上停画。 |
| 改动范围 | 409份当前C#源码中403份保持，仅两张牌、对应Power、两个专用控制器和生成目录变化；main及原9文件保持。 |

改前测试版Common预加载出现2条既有原生Invalid Task ID记录，之后均完成预加载；最终改后出牌未见C#异常、Shader错误或池回收错误。完整/低档连续帧、数量及去向已核对，逐项数据见[核验数据](star-dream-redesign-2026-10-31.json)。

## 补充实录

| 情况 | 正式版 | 测试版 |
| --- | --- | --- |
| 星光仅剩1点 | [原片](../../artifacts/star-dream-redo-20261031/after/stable/last-karen_star/game.mp4) | [原片](../../artifacts/star-dream-redo-20261031/after/beta/last-karen_star/game.mp4) |
| 星光重播技能3次 | [原片](../../artifacts/star-dream-redo-20261031/after/stable/skill-karen_star/game.mp4) | [原片](../../artifacts/star-dream-redo-20261031/after/beta/skill-karen_star/game.mp4) |
| 梦想3层 | [原片](../../artifacts/star-dream-redo-20261031/after/stable/stack-karen_we_kown/game.mp4) | [原片](../../artifacts/star-dream-redo-20261031/after/beta/stack-karen_we_kown/game.mp4) |
| 空虚模式 | [原片](../../artifacts/star-dream-redo-20261031/after/stable/void-karen_we_kown/game.mp4) | [原片](../../artifacts/star-dream-redo-20261031/after/beta/void-karen_we_kown/game.mp4) |

Full/Low/Off、Fast和中途移除演出原片及逐项状态保存在相同目录的对应case子目录。卡面回收检查只追踪本演出持有的原生NCard，正常结束和外部移除均核对池内失效引用与脱树占用。

首轮演出已录制并保留为`after-initial`。静态复核后补齐新卡面的原生池归还和空数量标签的父节点归属；中间轮次保留为`after-pool-iteration`。测试夹具最初对原生池做全局断言，在尚无梦想预览的测试版星光用例观察到5个其他卡面引用；后续断言限定为本次演出实际创建的预览。生产演出规则与收益沿用原卡。

完整帧耗时/首次使用专项、完整单人/多人矩阵及Android本轮未测。

main保持`0ef1ab4`和原9个文件；工作树、改前候选、两套隔离游戏和全部媒体保留，待用户审片后决定合并。
