# 星光与梦想：保留主体的简化

星光的整组星列定位已后续修正为固定在华恋前上方，见[舞台定位修正与视频](star-stage-anchor-2026-10-31.md)。本页星光录像为670440c定位修正前的历史对比，梦想继续沿用本页演出。


2026-10-31。用户指出上一候选f3b2c1b把主体动画替换成牌面提示。该候选保留为历史，当前以原始主体a1416a8作视觉参照，只改两个专用控制器。

## 原始主体与本次简化

左侧为a1416a8原特效，右侧为本次保留主体的简化。原速，底部说明每个阶段和简化位置。

| 卡牌 | 正式版 | 测试版 |
| --- | --- | --- |
| 星光闪耀之时 | [原主体对比](../../artifacts/star-dream-retain-redo-20261031/original-comparison/stable-karen_star.mp4) | [原主体对比](../../artifacts/star-dream-retain-redo-20261031/original-comparison/beta-karen_star.mp4) |
| 两人的梦想 | [原主体对比](../../artifacts/star-dream-retain-redo-20261031/original-comparison/stable-karen_we_kown.mp4) | [原主体对比](../../artifacts/star-dream-retain-redo-20261031/original-comparison/beta-karen_we_kown.mp4) |

[正式版合辑](../../artifacts/star-dream-retain-redo-20261031/original-comparison/star-dream-stable.mp4) · [测试版合辑](../../artifacts/star-dream-retain-redo-20261031/original-comparison/star-dream-beta.mp4)

## 改动

| 卡牌 | 保留的完整动作 | 实际简化 |
| --- | --- | --- |
| 星光闪耀之时 | 双色聚拢、有限星列、每次实际出牌对应的飞星、闪耀耗尽、奖励飞星与20颗完整档散点 | 星列变成按720高基准320像素宽的浅弧；聚拢直接汇入星列，叠亮芯合并为星列自身。飞星短尾保持方向，真实牌框仅作辅助。起手0.42秒，Fast为0.294秒。 |
| 两人的梦想 | 两颗双色主星、两条汇合轨迹及随行星点、真实光辉卡面成形、双色轨迹送往各自星点或空虚入口 | 大回环改开放短弧；光带变细，取消重叠亮芯，汇合区放到角色前方空白处。回合末按汇合、成形、送达三段播放1.10秒，Fast为0.77秒。初次施放以完整汇合和飞入实际能力图标收尾。 |

保留上一候选的真实模型绑定、所有光辉卡面和多层数量、真实奖励图标及原生池回收。Full和Low均有完整主体阶段，Low降低共享曲线采样和散点频率；Off/Instant沿既有启用规则。角色动作及另外407份C#源码保持。

两控制器复用现有KarenLightRibbon的资源与几何缓存，轨迹委托按实例缓存。未新增纹理、Shader或材质，目录仍为77类型、38共享准备者、167资源。

## 与上一候选对比

f3b2c1b仅作历史改前，以下能看到本轮恢复了哪些主体内容。

[正式版星光](../../artifacts/star-dream-retain-redo-20261031/comparison/stable-karen_star.mp4) · [正式版梦想](../../artifacts/star-dream-retain-redo-20261031/comparison/stable-karen_we_kown.mp4) · [测试版星光](../../artifacts/star-dream-retain-redo-20261031/comparison/beta-karen_star.mp4) · [测试版梦想](../../artifacts/star-dream-retain-redo-20261031/comparison/beta-karen_we_kown.mp4)

## 定向验证

| 项目 | 结果 |
| --- | --- |
| 最终双版本构建/资源目录 | 通过；77类型、38共享准备者、167资源。 |
| 最终包实录 | 30段通过：4段同包改前复用，26段最终改后新录。两版各13段覆盖Full/Low/Off/Fast、1次星光、技能3次重播、梦想3层、空虚和中途移除。 |
| 主体阶段 | 两版Full/Low/Fast均实际绘制星光聚拢、星列、逐次飞星和奖励收尾；梦想实际绘制双色汇合、成形卡面和双流入塔。 |
| 规则结果 | 星光5次45伤、1次9伤、技能3次15格挡，随后耗尽及战后奖励1次；梦想生成1/3张真实光辉，空虚模式进入抽牌堆并于下回合抽到手牌。 |
| 手牌与生命周期 | 30组MCP前后截图及karen_check_hand一致；节点退场、本演出卡面池失效/脱树占用均通过。两版外部移除在送达中段完成，规则结果保持。 |
| 视频 | 8段逐牌比较及4个两牌合辑带说明；30段原片音轨/包哈希核对通过，8段完整档改前/后原片未检出0.75秒以上停画。另核对4段a1416a8原始主体参考的来源与哈希。 |
| 改动范围 | 仅两个专用控制器变化，另外407份C#源码保持；main及原9文件保持。 |

最终出牌日志和逐项结果见[核验数据](star-dream-preserve-animation-2026-10-31.json)。

4段f3b2c1b改前原片按同包哈希复用，另保留4段a1416a8原始主体参考。首轮构图试录保存在after-placement-study；根据实际遮挡调整了梦想汇合位置、主星尺寸和节拍后重新录最终包。

完整帧耗时/首次使用专项、完整单人/多人矩阵、Android本轮未测。main保持0ef1ab4和原9文件，工作树、历史候选及全部媒体保留，候选等待审片。
