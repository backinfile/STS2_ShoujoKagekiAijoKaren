# 皇冠与番茄三种风格 / 显示比例校正提示词

工具：内置 imagegen。使用精确比例线框、本体资源对照、已有战斗截图及《打击》肖像；本轮同时比较三款皇冠与三款番茄，并校正显示比例。测量来源见 size-audit.md。

```text
Use case: ui-mockup / compositing.
Create a comparison of SIX Karen class energy icons: THREE CROWN styles and THREE TOMATO styles. MAIN PRIORITY is CORRECT ORIGINAL GAME DISPLAY SCALE. Earlier concept icons were much too large; this image must correct them.

INPUT 1 IS AN EXACT MEASURED LAYOUT GUIDE. Edit/replace its card and battle placeholders with final artwork, while preserving its positions, dimensions, all 6 panels and 2:3 portrait canvas. The red squares define the MAXIMUM COMPLETE ICON BOUNDING BOXES including crown tips, tomato leaves, outlines, shadows and digits. Final icon silhouettes MUST FIT INSIDE those red square sizes. Red guide squares and explanatory wireframe copy must disappear in the final artwork. Do not expand the slots just to improve legibility.
At the 1024x1536 guide resolution:
- Each complete card FRAME occupies 180 x 253 px.
- The COMPLETE cost badge occupies at most 38 x 38 px (native 64x64 on a 300x422 card, i.e. 21.33% of card width). Its center is only ~10px right/down from the frame upper-left corner. Badge sits partly outside the frame. Do not use the old 60-70px cost badges.
- Each complete battlefield view occupies exactly 270 x 152 px, 16:9. Never turn the battlefield into a tall/square viewport.
- The COMPLETE combat energy icon INCLUDING tips/leaves is at most 18 x 18 px (native 128x128 on a 1920x1080 screen, 6.67% of viewport width). Its top-left is ~14px right and ~116px down from the battlefield view upper-left. It must be TINY in the full scene. DO NOT make the full-scene icon 35-50px. It is okay if the 3/3 text is small and hard to read in the thumbnail; the DETAIL crop is for reading it.
- Each DETAIL 3x crop is a separately magnified view, icon at most 54 x 54 px. Never use that large detail icon size inside the complete battlefield.
If generating higher resolution, multiply ALL layout and icon dimensions together by the same uniform factor, never enlarge icons independently of the card or battlefield.

INPUT 2: original game class token contact sheet, STYLE REFERENCE ONLY. All class tokens are simple chunky hand-painted shapes, broad bevel facets, dark outline and an energy center with only a few curved brushstrokes. Match this level of simplicity. Do not replicate their square, pentagon, hexagon, octagon, sun or star outlines.
INPUT 3: actual game screenshot, same waterfall background, Karen left, stone enemy right, top HUD and bottom hand for all 6 panels. This screenshot is the truth for original game proportions. Copy its actual relative energy size, NEVER inflate its energy icon. Source '102/3' must become '3/3'. It can remain tiny in the full-scene thumbnail.
INPUT 4: exact Karen Strike portrait. Use the same supplied portrait on every full card and same red/ivory card frame. Card title '打击', type '攻击', description '造成 6 点伤害。'. The complete card itself must follow the guide's proportions. Don't make it too tall just to fill the panel.

Whole sheet:
Portrait 2:3, same layout as guide, 2 columns x 3 rows.
Order reading across rows:
A — FACETED CROWN
B — SOFT CROWN
C — GRAPHIC CROWN
D — FACETED TOMATO
E — SOFT TOMATO
F — GRAPHIC TOMATO
Charcoal background, modest heading 'KAREN — CROWN & TOMATO', small 'GAME-SIZE MOCKUP'. Slim panel dividers. Keep panel title and 'DETAIL 3x' labels. Replace the guide's technical explanatory paragraphs with clean negative space. No giant detached decorative icons; if a blank silhouette swatch is desired, it must be a modest 28px thumbnail by the title. No lengthy text. All six equal layout, artwork and game scale.

Basic forms:
Every crown is ONE unified three-peaked abstract crown silhouette, not a circle with a little crown attached. Broad lower body, short peaks, simple outer edge; no gold, gems, rings, band, filigree, ribbons, leaves or particles. Color coral red #FB5458 / burgundy #8B2A2D, flat light salmon-red energy well. Entire crown including tips fits the same original-game icon box.
Every tomato is ONE slightly flattened/lobed red fruit silhouette with ONE tiny muted-green three-leaf calyx at top, leaf less than 10% total area. Entire fruit+calyx fits original-game icon box. No tall stem, face, seeds, detailed veins, realism, detached leaves, medallion or extra background shape. Broad red inner body leaves room for numbers.

Style variations (subtle but clearly different, all remain SIMPLE original-game UI assets):
A FACETED CROWN: crisp straight-sided 3-point crown, a handful of large angular red bevel planes, slightly rough flat brushwork, dark outline. The strongest geometric and close-to-original token look.
B SOFT CROWN: 3 rounded peaks and lower corners, matte coral body with two broad softly blended shade regions, hand-painted edge, no shiny specular gloss. A warmer and softer impression.
C GRAPHIC CROWN: squat asymmetric 3-point crown, only two flat red values plus burgundy outline, nearly graphic/cel-shaded, one broad curved energy stroke. More spare and graphic; do not add decorations to differentiate it.
D FACETED TOMATO: chunky slightly lobed tomato with just 4-5 large angular red planes and tiny flat green calyx; light-red center with two rough red perimeter swirl marks. Geometric original-game painted treatment.
E SOFT TOMATO: rounder flattened tomato shoulders with restrained softly blended matte red shading, one broad warm highlight patch, tiny rounded green calyx. No photorealistic or glossy fruit.
F GRAPHIC TOMATO: very simple squat tomato, two-tone flat coral fill, dark burgundy contour, tiny green calyx, just 2 broad red curved energy marks. Minimal cel-shaded version, very readable silhouette.

Within EACH panel the exact same respective design must appear on:
- the full card cost badge at the 38px box, overlay digit '1';
- the main battle energy at the TINY 18px box, overlay '3/3';
- the detail energy at 54px, overlay '3/3';
- small hand costs at actual native relative scale.
Numerals remain upright, cream serif with burgundy outline. All other portraits and battlefield contents are the same in every panel. Do not distort cost numbers. No enlarged numbers outside the shape. Forbid oversized icons even when this makes them harder to see in the tiny full view.
FINAL ACCEPTANCE CHECK: Cost badge max 21.33% card width, full battle energy max 6.67% screen width, main view exactly 16:9, detail magnification explicitly separate. These size constraints outrank decorative impact and easy thumbnail readability. Entire image is a concept mockup, not proof of actual in-game integration.
```
