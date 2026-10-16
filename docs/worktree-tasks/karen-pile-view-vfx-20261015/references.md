# 参考、预览与实现对应

- [项目工坊特效研究](../../19.创意工坊特效Mod解包与制作流程.md)：真实事件驱动、挂载层与生命周期；本任务只装饰查看界面。
- [少女歌剧官方 TV 设定](https://revuestarlight.com/animation/tv/)：舞台、星、闪耀的主题语言。
- [官方《約束タワー》唱片](https://revuestarlight.com/music/5thsinglecd/)及[官方剧情](https://revuestarlight.com/animation/story/)：约定与舞台主题。双星连线是本任务的设计解释，并非复刻官方特效。
- [Regent FX Omnistar 工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3747497501)和[Lovecs 作者仓库](https://github.com/r2Nexus/LovecsVfxLib)：参考程序编排、区分常驻与短触发、明确节点所有权；没有复制第三方图像、代码或声音。
- [Godot 官方自定义绘制](https://docs.godotengine.org/en/stable/tutorials/2d/custom_drawing_in_2d.html)：`_Draw` 缓存，变化时 `QueueRedraw`，沿用父 Control 坐标。此处使用单个 Control 绘制少量线条与星形。
- [Godot Tween 官方说明](https://docs.godotengine.org/en/stable/classes/class_tween.html)：评估节点绑定动画方案。最终此处只需短过程驱动绘制，无 Tween 或异步任务。

查询日期：2026-10-15。查询和预览发生在实现之前。

## imagegen 预览

通过内置 imagegen 生成四格表现图：`output/pile-view-vfx/concept.png`（本地交付，忽略目录）。原始生成路径保留在本机 imagegen 输出中。

Prompt specification (built-in tool, `ui-mockup`): two columns by two rows, 16:9 game VFX art-direction preview; existing native card pile inspection, dark dimmed dungeon battle, unobstructed native card grid and return button. Left: promise opening/idle, muted crimson and champagne gold, tiny paired five-point stars joined by a thin expanding thread, extremely faint margin lights. Right: shine-depleted opening/idle, antique gold and ash mauve, a hollow star and broken orbit, at most six faint downward embers. Labels: PROMISE / OPEN, SHINE DEPLETED / OPEN, PROMISE / IDLE, SHINE DEPLETED / IDLE. No large burst, lens flare, neon, ornate card frames, extra fullscreen filter, confetti or characters; feasible Godot linework and gradients.

## 从预览落地

| 预览要素 | 实现 |
| --- | --- |
| 约定双星与展开细线 | 金色小双星、两侧向外展开的红金曲线 |
| 约定侧台灯 | 两侧低透明度三层三角形，位于牌列外侧 |
| 耗尽空心星与残轨 | 单颗空心五角星、两侧短断续弧线 |
| 少量余烬 | 仅开场的六个小星点；耗尽向下漂落 |
| 不干扰查看 | 在原生背景之后、CardGrid 之前挂载；MouseFilter Ignore、FocusMode None |
| 克制、轻量 | Normal 0.64 秒、Fast 0.32 秒、Instant 直接静态；结束后停用 `_Process` |
| 关闭与替换 | 由原生 NCardPileScreen 直接拥有；退场释放整棵子树，无全局订阅或独立任务 |

预览卡牌和文字是概念示意；实际沿用原生卡牌、原有本地化说明及返回按钮。未导入生成图作为游戏贴图。
