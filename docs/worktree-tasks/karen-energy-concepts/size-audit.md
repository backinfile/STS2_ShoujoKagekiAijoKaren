# 能量 UI 显示尺寸核对

本次核对使用本地提取的游戏场景与代码；无需运行游戏。

| 项目 | 本体值 | 参考 |
| --- | --- | --- |
| 卡牌基础尺寸 | 300×422 | `src/Core/Nodes/Cards/NCard.cs` 的 `defaultSize`；`scenes/cards/card.tscn` 的 Frame |
| 卡牌费用纹理图集区域 | 74×74 | `images/atlases/ui_atlas.sprites/card/energy_*.tres` |
| **卡牌费用实际 TextureRect** | **64×64** | `scenes/cards/card.tscn`，EnergyIcon offsets -166/-227/-102/-163 |
| 费用图标相对卡面位置 | 左上角 (-16,-16)，64×64 | Frame 左上 (-150,-211)，EnergyIcon 左上 (-166,-227) |
| 费用图标宽度 / 卡宽 | **64/300 = 21.33%** | 不是 74/300；图集区域不等于显示尺寸 |
| 战斗能量球 Control | 128×128 | `scenes/combat/energy_counters/ironclad_energy_counter.tscn` 等 |
| 战斗基准视口 | 1920×1080 | `project.godot` |
| 战斗能量球 Control 宽度 / 视口宽度 | **128/1920 = 6.67%** | 实际可见 alpha 轮廓通常略小于 Control |
| 战斗能量容器位置 | (100,1080-252) = (100,828) | `scenes/combat/combat_ui.tscn` |

## 预览比例约束

- 卡牌预览宽 180 时，费用控件为 38.4 方框；图标左上相对卡面约 (-9.6,-9.6)。
- 完整战斗预览宽 270、高 151.875（16:9）时，能量球控件为 18×18，不能为了让数字容易看清而放大。
- 战斗缩略图中的 3/3 可以很小；下方单独提供明确标注的 DETAIL 3x 供辨认。
- 本次生成附带精确线框参考 `scale-layout-guide.png`。该图是按本体尺寸计算的绘制参考，imagegen 结果仍要记录近似误差，不能宣称像素严格一致。

## 原图评估

前一批概念图的主战斗区域被重绘得接近方形，费用图标及能量球也被生成器强调放大。新一批同时校正卡牌费用相对卡面比例、完整战斗视图 16:9 比例，以及战斗能量球相对视口比例。
