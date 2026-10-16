# 永不遗忘的闪耀：ImageGen 刀光主体与独立粒子

当前运行主体为 slash-body.png，来自新 ImageGen 参考板的右侧刀光。原始生成 RGBA 直接复制，不做程序改像素；两端收尖、斜向弯曲、内部朴素蓝白。参考图、完整提示词及来源见 docs/design/rare-vfx/unforgettable-shine-2026-10-16/slash-reference-redraw.md。此前 crescent.svg 保留为历史资源，运行时不再加载。

运行时保持贴图原始比例和角度，命中锚点与 32 个外缘 UV 点来自对其 alpha 的只读分析。全贴图方形边长为 min(300 × unit, 视口高度 × 0.22)，不随攻击距离拉伸；Shader 从上尖端到下尖端展开，命中后整体淡出。

外围为四组原生 CpuParticles2D，共 20 槽：GoldSparks 10、GoldTwinkles 2、CoralFragments 4、BlueWisps 4。沿主体实际外缘发射，速度、随机寿命、缩放和淡出独立。命中后停发，残留粒子自然退出，子节点随攻击释放。

focus.png、impact.png、starburst.png 保留蓄势、命中和留星。耗尽奖励专属特效维持删除；蓄势和斩击专属音效调用、独占 WAV 及音频生成脚本已删除，奖励规则不变。
