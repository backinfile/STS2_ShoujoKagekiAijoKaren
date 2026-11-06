# 正九边形试装

工具：内置 imagegen；参考 1 为数学构造的正九边形，参考 2 为本体 UI 特征图。生成透明 PNG 原样复制，未修改像素。

Use case: stylized-concept.
Asset type: ONE isolated transparent energy token sprite for Karen, matching Slay the Spire 2's simple painted class-energy UI.
Input image 1 is the EXACT OUTER GEOMETRY guide. Input image 2 is a STYLE REFERENCE ONLY (native colored icons in its second row); do not copy any of its eight-sided/six-sided silhouettes or include the reference sheet in the output.
Primary request: a precise REGULAR NONAGON. Exactly NINE straight outer edges of equal length, exactly NINE principal corners, equal 140 degree interior angles. One vertex points straight UP, the opposite BOTTOM is a horizontal flat edge. Following the guide clockwise, vertex angles are -90,-50,-10,30,70,110,150,190,230 degrees around the center. Faithfully preserve the guide's entire outline and proportions. Every edge is a single straight segment: no rounded arcs, scallops, additional corner cuts, star points, dents or teeth. The nonagon is front-facing and flat in image plane, not in perspective.
Paint that exact body in the native UI grammar: dark burgundy outline #8B2A2D, coral red shell #FB5458, a simple broad warm salmon-red energy well #FF8B7B at center. The inner well is approximately circular and broad, around 65% of body width, with 2 or 3 broad subtle curved paint strokes; it must stay uncluttered for a separately rendered game numeral. Use simple matte painted planes in the rim, coarse restrained brushwork, limited values. Upper left is softly brighter, lower right dark burgundy. Crisp simple silhouette at a 64px display size. Not a jewel, polished metal, glass, shiny jelly, fruit or badge with attached decoration. No nine tiny gemstones or segmented ornaments.
Composition: centered single token, symmetrical left/right, nearly square visible footprint, roughly 8% transparent breathing room all around. Keep whole outline visible.
Text: NONE. No number, letter, label, watermark, diagram, shadows outside the body, background, glow, particles, crown, leaf, star or separate pieces. Truly transparent background. Output only the one nine-sided painted icon.
## 轮廓修正尝试

Use case: precise-object-edit.
Image 1 is the edit target: the coral painted energy token. Image 2 is the EXACT regular nonagon silhouette to fit.
Correct ONLY the outer geometry and corresponding rim divisions of image 1 to fit image 2's mathematically regular NINE-sided polygon. Preserve its painting style, colors, broad energy core and transparency. The target currently has unequal outer edge lengths and a too-sharp top angle; correct those small irregularities.
All NINE outer edge lengths must be equal, all NINE interior angles exactly 140 degrees. The topmost vertex is centered and the bottom edge is horizontal. Match the guide's normalized proportions precisely, do not stretch it. For a 1024 canvas the nine ideal normalized vertices are approximately (512,80), (790,181), (937,437), (886,728), (660,918), (364,918), (138,728), (87,437), (234,181). One straight segment between successive vertices. Retain nine main corners, do not clip the corners or add small edges. Keep the entire icon front-facing in the flat image plane.
Keep all other properties: matte coral red shell, dark burgundy outline, simple broad warm salmon energy well, understated curved brushwork, chunky painted rim, no text, no numerals, no decorations, no outside shadow, no background. Output one isolated corrected token on truly transparent background.

修正输出改变了比例，未采用。当前试装使用首稿的九边手绘轮廓；数学控制源为严格正九边形，生成位图的实际边长和角度偏差另见 shape-audit.json，不将其称为像素级严格正多边形。