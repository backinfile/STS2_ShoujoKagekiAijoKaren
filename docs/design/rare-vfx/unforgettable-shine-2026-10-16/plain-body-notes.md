# 朴素主体迭代

历史迭代记录：当前运行已改用 [新 ImageGen 刀光参考及透明主体](slash-reference-redraw.md)，并移除本牌专属音效；下文 SVG 月牙是此前被否定版本。

用户原文：

> 蓝白主体太花了，要求朴素

最终运行资源是 images/vfx/unforgettable/crescent.svg：一个填充 #DFF4FF 的闭合路径，无内部线条、描边、渐变、贴图纹理、发光滤镜或装饰。Godot 导入后沿弧展开，命中后整体透明淡出；Shader 中的高频细丝分解一并移除。其余装饰仍由独立粒子负责，耗尽奖励特效维持删除。

后续用户原文：

> 太大了

本轮不改素材造型，运行时 VisualScale 改为 0.65，蓄势、命中和粒子一起收小。宽弧尾部从角色身后移到前方，主体和粒子共用 StrokePoint 映射，命中端保持真实目标位置。

再次后续用户原文：

> 这个弧修改到正常大小（现在太大了），修改为正常的半月弧

最终改为真正的局部半月：SVG 512×512，外弧是以 (128,256) 为圆心、224 为半径的右半圆，内弧回收出两个尖端。运行时用等比方形映射，外侧中点对准真实命中点，尺寸不依赖攻击距离；方形边长上限为视口高度的 32%，实际可见半月高上限 28%、宽上限 14%。Shader 展开与四组粒子发射同步改为这条半圆。维持朴素单色，不增加装饰纹理。

## ImageGen 造型尝试

使用内置 imagegen，transparent_background=true；两张结果都有粗糙白色边屑，未用作运行纹理。为了准确落实朴素、干净的轮廓，最终使用上述可编辑的单路径矢量资源，不对这些生成 PNG 做抠图加工。

第一次 edit 输入为上一轮 crescent.png；输出 exec-45c007f2-b3fe-47b2-8297-fb2097302a32.png。

```text
Use case: precise-object-edit. Edit target: this sword crescent sprite. The user says it is much too ornate and wants it PLAIN. Keep the broad C-shaped slash geometry: two tapered ends on the left and rounded thick bow on the right, transparent open center, about 2.1:1 occupied bounding box. Replace ALL existing painterly rendering with ONE FLAT, UNIFORM PALE BLUE-WHITE FILL (#DFF4FF). The entire stroke is a single solid, clean silhouette, like a plain cel-shaded graphic shape. Smooth simple outer and inner curves. Each of the two left ends finishes in a SINGLE point, without branches or forks. ZERO internal detail: NO brushstrokes, NO streaks, NO stripes, NO veins, NO wisps, NO layered bands, NO white core line, NO outlines, NO multi-layer blue edges, NO highlights, NO shading, NO gradients, NO texture, NO grain, NO sparkles, NO particles, NO glow halos. Only minimal 1–2 pixel antialiasing at the silhouette edge. This must look intentionally simple and restrained, one flat-color curved slash on genuine RGBA transparency. No background and no text. The previous ornate inner strokes must disappear completely.
```

第二次 edit 输入为第一次输出；输出 exec-f95500f0-19cf-4844-933c-42106f379edd.png。

```text
Precise cleanup edit of the provided plain crescent. Preserve its flat pale blue-white fill and EXACT curved silhouette. Change ONLY edge cleanliness: remove every ragged white blob, chip, detached pixel speck, torn-paper fragment, paint residue and protrusion outside the smooth intended curves, especially along the top center and bottom center. Make the outer and inner boundaries perfectly smooth and clean like a vector shape, with minimal antialiasing, absolutely NO rough brush edge. The result must be ONE flat solid C-shaped slash on true transparent RGBA, two single tapered points at left, one rounded bow at right. NO internal lines, texture, shading, glow, decorative edges or added detail. Keep it extremely plain. No scene, no text, no background.
```
