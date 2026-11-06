# 视觉依据与概念图

- 已实际查看本地卡图 `images/packed/card_portraits/karen/karen_shine_strike_barrage.png`：华恋、红披风、金色剑柄和持剑准备姿态。采用角色现有红／白／金色语言；卡图不能证明连续突刺的动画时序。
- 检索了官方角色页 <https://revuestarlight.bushiroad.com/character/karen/> 及官方手办页 <https://sp.bushiroad-creative.com/figure/revuestarlight/karen/>。搜索摘要可核对角色／剑的身份，但本次直接读取均返回 404；未观察官方视频片段，不据此声称官方镜头复刻。
- 主要动作是本任务的设计推断：在原本多段攻击基础上采用细长红白刺光、真实逐击星芒和少量粒子。保留现有角色 Attack 动画；概念图中的完整伸臂姿态不是新增角色动画承诺。
- `concept.png` 由内置 imagegen 生成，作为尺寸／主轮廓预览，不是实机截图。两张输入分别是既有游戏视口和本地卡图；没有用于替换卡图或角色素材。最终生产主体采用独立 Shader 与 Godot 绘制，装饰使用真实 CPU 粒子。
- 实机判断以本任务 contact 截图及原速 MP4 为准，保持卡牌、血条和伤害数字可读；不添加全屏压暗或灯阵。

## ImageGen prompt

Use case: stylized-concept. Asset type: concept preview for a Godot Slay the Spire 2 Karen Aijo character mod attack VFX, not a final gameplay screenshot. Input image 1 is the exact game battlefield layout reference; image 2 is the card portrait reference for Karen's costume and sword identity. Create a single wide 16:9 faithful mockup of the battlefield from image 1: preserve the player character at left, stone waterfall monster at right, the HUD and hand size/locations, remove the turn announcement overlay. Add only a compact rapid sword thrust attack effect from Karen toward the monster's chest. Main silhouette: one long sharply tapering horizontal ivory-white blade of light edged coral-red, a subtle paired offset stroke conveying repeated thrusts, no circular slash arcs; thin bright core, painterly cel-shaded outer edge. At the actual target contact show a small sharp four-point warm-white star with red-gold filaments and sparse tiny outward sparks. Preserve clear monster silhouette, HP bars, card text, and player. Effect around 30-45 pixels thick at 1280x720; contact star about 90 pixels across, not a giant explosion. No full-screen tint, no laser cannon, no giant rings, no new props or characters, no invented labels. This concept extrapolates sword thrusts from the local card art and is not a recreation of a verified anime scene.
