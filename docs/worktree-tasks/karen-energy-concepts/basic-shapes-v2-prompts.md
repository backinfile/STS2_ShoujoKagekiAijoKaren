# 基础形状新一轮提示词

使用内置 imagegen；每个方案单独生成一张透明无数字图标，再使用本体控件尺寸的 HTML/CSS 预览同图比较。参考图只作为原版画法与复杂度依据，不复制外轮廓。

## 公共提示词

Use case: stylized-concept.
Asset type: one standalone transparent raster sprite for a Karen character energy icon in Slay the Spire 2.
Reference image 1 is STYLE AND COMPLEXITY REFERENCE ONLY: the five original character icons show thick simple geometric silhouettes, a few broad hand-painted bevel faces, and an almost flat painted energy center. Do not copy any of their octagon, pentagon, hexagon, square or sun/star silhouettes.
Create ONE new icon with the exact geometric silhouette described below. It must feel as basic and low-detail as those native icons, not a logo badge and not a cut gemstone. Plain broad coral red body (#FB5458), dark burgundy outline/shadow (#8B2A2D), 3 or 4 wide matte bevel color planes, pale salmon central energy area (#FF9A94). The center should blend into the body, with only 2 or 3 small broad curved brushmarks suggesting energy, no added ring/frame. Leave the middle calm and readable for a separately overlaid white numeral or 3/3 label; do not render text.
Nearly front-on flat painted game UI sprite, subtly imperfect brush edges but disciplined shape, dark outline with even thickness. Icon centered on a square transparent canvas, with about 8% clear margin. Both width and height should use the canvas sensibly. Actual transparent alpha outside the silhouette.
Avoid: crown, tomato, fruit, leaf, stem, heart, face, gold, jewels, gloss, specular pinpricks, engraving, ornament, metallic frame, stars, external glow, sparks, pedestal, extra icons, text, numerals, labels, checkerboard background.

## A 正三角

Silhouette: an upright broad equilateral TRIANGLE, one apex at top center and one straight horizontal base below. Three main sides only, corners lightly softened without cutting off the corners. Wide enough near the center to hold a numeral. A complete triangular red energy token; do not enclose a circle inside a triangular metal frame.

## B 倒三角

Silhouette: a broad inverted equilateral TRIANGLE, one straight horizontal top edge and one apex at bottom center. Three main sides only, corners lightly softened without cutting off corners. The upper half is broad; keep a generously broad center for a later 3/3 label. An entire triangular red energy token, no separate metallic frame.

## C 梯形

Silhouette: a simple upright ISOSCELES TRAPEZOID, four straight sides, shorter horizontal top edge and longer horizontal bottom edge, two outward-sloping straight sides. Top width approximately 58% of bottom width. Body height approximately 85% of bottom width. Slightly softened corners but no clipped-corner polygon. No fifth side or bottom point: it must not become a shield/pentagon. Entire body filled with coral energy; no separate metal frame.

## D 梭形

Silhouette: a simple symmetric horizontal biconvex geometric LENS, exactly two smooth broad arc edges joining in a point at the far left and a point at far right. Width approximately 1.35 times height, substantial full middle with room for 3/3. No notch, no stem, no vein and no pointed top or bottom. It is an abstract basic geometric lens, not an illustrated leaf, eye, diamond or oval. Entire body coral energy; no separate metallic frame.
