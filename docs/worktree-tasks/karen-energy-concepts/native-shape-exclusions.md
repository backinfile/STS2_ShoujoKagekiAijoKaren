# 本体能量轮廓对照与避重依据

本表按实际资源的整体剪影判断相似性，不把边数视为唯一分类。更完整的特征分析见 [原版特征分析](native-features-analysis.md)。

| 已有能量图形 | 应重点避免复用的整体外形 |
| --- | --- |
| 铁甲战士 | 边长接近、顶端有角的近正八边形 |
| 静默猎手 | 上下收尖、两侧斜立且长短边不均的六边形 |
| 故障机器人 | 四条长直边配四个短削角的方形 |
| 亡灵契约师 | 上下平边、左右有角的六边形 |
| 储君 | 八个宽短外尖形成的太阳形 |
| 无色、任务池当前接线 | 整圆；QuestCardPool.EnergyColorName 当前为 colorless |
| energy_quest 残留贴图 | 菱形；已核查真实 AtlasTexture 区域，不等同于任务池当前接线 |
| 观者残留行内符号 | 紫色多边形（残留资源，不推断当前可玩职业） |
| 储君 energy_star | 内凹弧边、四个尖端的四芒星 |

此前将静默写为“五边形”不准确，现已更正。静默和亡灵契约师均具有六条主要边，但顶点朝向与长短边分配不同；铁甲与故障机器人的主要边数也重合。不重复应同时比较整体比例、顶点位置、边长、削角和凹凸关系。

第二轮的正三角、倒三角、梯形、梭形，以及第三轮半圆、厚十字、双瓣均已被用户否定。它们不再作为推荐方案。历史生成 PNG / 提示词保留原貌；其中旧标签不应作为形状分类依据。

核查来源：本体 res://images/atlases/ui_atlas.sprites/card/energy_*.tres、res://scenes/combat/energy_counters/*_energy_counter.tscn、res://images/ui/combat/energy_star.png、res://images/packed/sprite_fonts/watcher_energy_icon.png，以及 QuestCardPool.EnergyColorName。
