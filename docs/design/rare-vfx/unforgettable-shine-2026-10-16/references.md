# V2：永不熄灭的舞台闪耀

当前用户再次否定攻击弧线，并要求“去掉音效”“攻击弧线还是不对，你重新用imagegen生成参考图”。[新 ImageGen 参考与提示词](slash-reference-redraw.md) 是本轮画面依据，已生成透明主体接入运行；下文旧半月实装与历史分镜不代表用户已认可。专属蓄势、斩击音效已移除。

最新要求“这个弧修改到正常大小（现在太大了），修改为正常的半月弧”：运行主体已改成命中点附近的等比半月，尺寸不随攻击距离拉伸，保持单路径纯色，无内部纹路、多层亮边与细丝分解。当前实现见 [朴素主体记录](plain-body-notes.md)。下文关于旧 PNG 主体和旧构图的描述仅保留作历史，独立粒子与取消奖励特效的要求继续有效。

## 当前覆盖要求：主体贴图＋粒子

用户此前输入：“耗尽奖励的特效不用制作； 宽弧你制作的不对，应该只需要画主体，其他装饰部分用粒子来做”。因此以下旧四阶段编排中的奖励彗尾不再实现；原分镜作为主题参考，并不要求保留这部分。

- 旧 crescent.png 曾只保留蓝白主体，其历史提示词见 [body-only-prompt.txt](body-only-prompt.txt)；现已被朴素半月 crescent.svg 替代。
- 四组 Godot CpuParticles2D 沿已展开弧线产生金色火花、稀疏金星、珊瑚光屑和蓝色碎光；[官方 CPUParticles2D 文档](https://docs.godotengine.org/en/4.5/classes/class_cpuparticles2d.html) 对 DirectedPoints、EmissionPoints／Normals、LocalCoords 及生命周期的定义与本地 GodotSharp 4.5 API 一起核查。使用全局粒子坐标，已发出的粒子不跟着角色拖移。
- 删除 Reward 分支、Reward 方法、彗尾运动、奖励图标查询、奖励专属声音与卡牌视觉目标缓存；OnShineExhausted 仅保留原有实际奖励登记。

用户否定上一版的主题和画面，因此撤销胶片、底片、方框与星印方向，从角色与卡图重新制作。

## 参考事实与原创转译

- [官方华恋介绍](https://revuestarlight.bushiroad.com/character/karen/)：童年看过《Starlight》，与光约定同台，随后走向下一个舞台。搜索索引返回这些内容；本次直接访问旧站返回 404。
- [官方华恋髪饰周边](https://revuestarlight.com/goods/3872/)：确认存在该角色髪饰商品，但本次未观察到原图。没有将商品轮廓硬加成漂浮图标。
- [官方故事发布资料](https://bushiroad.co.jp/media/493)：幼年的舞台与约定是两位角色的关系起点。采用“舞台上的一瞬闪耀仍留下光”的情绪，不声称是原作某镜头的复刻。
- 项目现有卡图：华恋持蓝白发光剑，身着红色披风。新剑光沿用蓝白、红色与舞台金光，不引入胶片、票券等外来主题。
- [Godot 4.5 CanvasItem 文档](https://docs.godotengine.org/en/4.5/tutorials/shaders/shader_reference/canvas_item_shader.html)：fragment 的 COLOR 已含纹理色与绘制调色，因此 Shader 只修改 alpha，避免把纹理与 alpha 乘两次。

预览由内置 imagegen 生成。初版独立素材未落实预览的轮廓，用户指出“和参考图完全不一样”后，以这张原分镜为编辑目标，重新提取 FOCUS、CUT、SHINE、REMAIN 四张透明纹理，提示词与源标识见 [参考图对齐记录](reference-match-prompts.md)。[旧剑光提示词](crescent-prompt.txt) 与 [旧星芒提示词](starburst-prompt.txt) 仅供历史追溯，不再对应当前资源。运行时保留新素材原 RGBA。

## 编排

0–0.22 秒：剑前金星聚光，红蓝两道长弧向光点收拢，人物后方暖色舞台追光。

0.19–0.48 秒：完整宽弧从角色身后展开，左下尾端靠近脚边，沿弧揭示纹理。0.48–0.64 秒保持完整剪影。0.64 秒的原生命中回调创建蓝金星芒，并使剑光沿连续细丝消退。

命中后：大星芒在 0.16 秒内展开，0.74 秒内退出；目标处的金星继续保留，耗尽后的余辉阶段仍锁定同一实际目标，并带蓝色碎光。

仅耗尽奖励登记后：三／四颗星像 REMAIN 分镜一样从脚边弯曲上扬，最后沿独立蓝／珊瑚彗尾回落到真实战后奖励标记。规则、随机奖励与伤害沿用原牌；这段光只确认已登记的未来收益。
