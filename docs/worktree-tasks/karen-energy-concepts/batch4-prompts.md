# 第四批：依据原版特征重制

工具：内置 imagegen，每款一张无数字透明 PNG。参考 1 为原版资源分析图，参考 2 为本批独立 SVG 轮廓控制图；最终仍使用原生控件尺寸排布。

## 公共提示词

Use case: stylized-concept.
Asset type: one standalone transparent painted energy token sprite for Karen in Slay the Spire 2.
Input image 1: reference for the EXISTING GAME PAINTING STYLE. Specifically use its row of enlarged colored card icons as material/edge/core reference. Its original-game silhouettes are EXCLUSIONS, not shapes to copy.
Input image 2: the EXACT silhouette to paint for this candidate. Preserve its visible contour and vertex/arc structure faithfully; gray is only a silhouette mask, not the final color. It is a contour guide, not a second icon to include.
Create ONE compact, nearly 1:1 filled energy block with a broad approximately circular energy center. Same simple matte hand-painted grammar as the native icons: dark burgundy outline (#8B2A2D), coral red shell (#FB5458), 2-3 broad matte bevel tones, a flat brighter warm salmon energy well (#FF8B7B) with 2-3 broad curved energy brushmarks. Shell thickness about 12-15% of the body width, with restrained plane divisions. The center stays calm and wide enough for a separate 3/3 label; render NO text or digits.
This must look like the same class-energy UI family as the reference: chunky, flat/front-facing, simple large color areas and coarse subtle painted edges. No perspective, metallic objects, realistic gemstones or glossy jelly.
Keep the whole body and its core centered and visually balanced. Actual silhouette width/height should remain within 0.95 to 1.08, never stretched into a tall shield, thin triangle or wide lens. Leave about 8% transparent margin around the icon, no clipping.
Avoid ALL original-game overall silhouettes: regular-ish pointed octagon, Silent's top-and-bottom-pointed hexagon, flat-top regular hexagon, clipped-corner square, eight-point sunburst, circle, diamond and four-point sparkle. Also avoid previously rejected crown, tomato, half-disc, plus/cross, double-lobed blob, plain sharp straight-sided triangle, trapezoid and lens. Do not add ornaments, leaves, stars, fruits, symbols, separated parts, deep notches, holes, background, cast shadow, particles, text, number or labels.
Follow the candidate-specific shape below over any tendency to reuse the reference's original silhouettes.

## A 上尖五边

Exactly FIVE principal vertices: one short top apex at center, two side shoulders, and TWO bottom corners joined by one long HORIZONTAL FLAT BOTTOM. The flat bottom is crucial; there is NO bottom apex and no sixth corner. A compact near-regular upright pentagon with nearly equal width and height. Match guide 2's silhouette precisely, allowing only tiny hand-painted edge irregularities. Do not turn it into the Silent's six-sided shape with both top and bottom points.

轮廓控制源：`batch4-guides/batch4-a-upper-pentagon.svg`。

## B 下尖五边

Exactly FIVE principal vertices: a long HORIZONTAL FLAT TOP, two outward side shoulders, and one short BOTTOM CENTER APEX. No top point, no sixth vertex, no beveled-square corners. One compact convex pentagon, width and height nearly equal, sufficiently broad center. Match guide 2 precisely; no deep shield-like elongation and no long triangular tip. It must not become Silent's top-and-bottom-pointed hexagon.

轮廓控制源：`batch4-guides/batch4-b-lower-pentagon.svg`。

## C 弧边三角

A compact rounded constant-width TRIANGULAR BODY with exactly THREE broad outward-bulging arc sides (Reuleaux-like). The guide is exact: one short top corner and two low side corners, all connected by convex arcs; the bottom is also a convex arc. Nearly 1:1 footprint and a very generous circular middle. It is NOT an ordinary straight-sided triangle, circle, clover, teardrop, heart or three disconnected lobes. Keep corners short, body inflated and continuous, with no concave notches.

轮廓控制源：`batch4-guides/batch4-c-arc-triangle.svg`。

## D 三向切角

Match guide 2's SIX-SIDED truncated triangular body exactly: a short flat top, two LONG slant sides descending outwards, two SHORT lower corner cuts, and a LONG flat bottom. The six edge lengths alternate SHORT / LONG / SHORT / LONG / SHORT / LONG around the shape. Overall visible bounding box is square. The guide's long flat bottom and short flat top must remain different lengths; the widest shoulders are low, not at mid-height. Do NOT replace it with a regular flat-top hexagon, Silent's pointed hexagon, a plain trapezoid or a sharp complete triangle. This is one compact convex solid with 3 long faces and 3 small corner cuts.

轮廓控制源：`batch4-guides/batch4-d-truncated-triangle.svg`。


## B 轮廓修正

首稿添加了多余外角，不进入最终方案。改以 A 为编辑源，保持五边形结构并变为下尖方向。

Use case: precise-object-edit. Input image 1 is the EDIT TARGET, the finished coral energy token. Input image 2 is an orientation/contour guide only. Create the DOWNWARD pentagon version by rotating the ENTIRE existing icon in image 1 by exactly 180 degrees in its own flat image plane. Preserve the source icon's EXACTLY FIVE principal outer edges and five corners, the existing matte paint, core, outline, brushwork, colors, proportions and transparent alpha. It should now have ONE horizontal FLAT TOP, TWO upper side corners, TWO mid/low side shoulders, and ONE bottom-center point, total FIVE vertices. The two long lower edges must each be ONE straight segment connecting a side shoulder directly to the bottom-center tip: do not introduce any intermediate lower corner, cut or bevel in the OUTER silhouette. Image 2 confirms the target whole silhouette. No new texture style, frame, decorations or digits. Keep the silhouette compact, nearly square in visible bounding box, centered with transparent margin. No perspective. Output exactly one isolated icon on truly transparent background.

## D 比例修正

首稿略偏宽，以编辑保留轮廓并收回接近 1:1。

Use case: precise-object-edit. Edit image 1, the coral-red six-sided truncated triangular energy icon. Image 2 is its precise shape/aspect guide. The current visible body is a little too wide. Change ONLY the body's width-to-height proportion to make its visible outer bounding box EXACTLY SQUARE (1.00:1): reduce horizontal width by approximately 12% relative to height, keep the shape centered and keep the same contour structure. Preserve exactly SIX principal outer edges: SHORT FLAT TOP, long right slope, SHORT bottom-right corner cut, LONG FLAT BOTTOM, SHORT bottom-left corner cut, long left slope. Keep the short top / long bottom asymmetry. Preserve the existing hand-painted coral/burgundy shell, broad warm salmon energy core, brushstrokes, outline and modest shading. Keep the core broad and approximately round after the correction. No extra corners or clipped-square / regular-hexagon substitutions. Preserve actual transparent alpha and clear outer margin. No text, numbers, new decorations, glow or background. Output one corrected compact icon.
