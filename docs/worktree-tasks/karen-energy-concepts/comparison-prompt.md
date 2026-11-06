# 四方案上下文对比图提示词

工具：内置 imagegen；五张参考图为 A/B/C/D 原始设计板及项目现有战斗截图。目标为概念合成，不是实机接入。

## 首次合成

```text
Use case: compositing / ui-mockup.
Task: Combine the FOUR supplied existing Karen energy UI design proposals into ONE high-resolution comparison image, showing for EVERY design both its appearance ON A COMPLETE CARD and its appearance IN THE GAME COMBAT SCREEN. This is a new comparison presentation of the supplied designs; preserve all four visual identities, do not invent fifth variants.
Output: a single landscape 3:2 image, target 3072x2048 or larger at same aspect ratio if possible. Four equally sized quadrants in a clean 2x2 grid, arranged A top-left, B top-right, C bottom-left, D bottom-right. Outer background neutral dark charcoal. Slim dividers and generous gutters. Short title 'KAREN ENERGY — IN CONTEXT'; modest subtitle 'CONCEPT MOCKUP'. All four quadrants get exactly the SAME composition, card size, screenshot size, lighting and camera. Fair comparison is essential.

Reference inputs:
Image 1: A STAR HAIRCLIP design board. Preserve the pale-gold FOUR-point star frame, left pin bar, smaller companion four-point star on lower-right, coral red center and dark burgundy outline. Do not replace with five-point star.
Image 2: B STAGE SPOTLIGHT board. Preserve round red lens, thin brass ring and four diagonal burgundy stage-light barn-door tabs.
Image 3: C REVUE MEDALLION board. Preserve small three-point crown at top, gold shoulder-braid shapes at sides, short red/ivory ribbons at bottom, round coral center.
Image 4: D POSITION ZERO board. Preserve ivory positioning circle and radial short cue ticks, coral red circle and tiny golden FIVE-point star at top, restrained sweeping red arcs around the larger HUD counter.
Image 5: ACTUAL GAME COMBAT SCREENSHOT. This is the visual ground truth for the battlefield, game UI, Karen character model, stone enemy, waterfall background, hand of cards, HUD and composition. Reuse this exact same screenshot composition in all four quadrants. Keep all unrelated parts recognizable and unchanged; DO NOT create a new imagined game scene, replace Karen with a different character, or replace the enemy. The screenshot currently shows a debug counter '102/3'; replace it with '3/3' in ALL four variants. Remove the small debug red line rig above Karen and the debug number 0 ONLY for a clean presentation, while preserving character and environment.
Card portrait reference: the Karen Strike cards visible in image 5 have a close-up anime portrait of Karen's red/brown-haired face, violet-brown eyes, navy uniform with gold trim, and a bright diagonal slash. Use the SAME portrait interpretation consistently inside the complete card in every quadrant.

Inside EACH quadrant:
1. Header with exact corresponding name: 'A — STAR HAIRCLIP', 'B — STAGE SPOTLIGHT', 'C — REVUE MEDALLION', 'D — POSITION ZERO'.
2. Left approximately 30%: ONE complete upright Slay the Spire 2 Karen Strike card, fully visible, same red/ivory existing-game card frame in all variants, the same Karen Strike portrait and text. Cost badge is attached naturally to the UPPER-LEFT corner of the card at standard card-badge scale, showing an ivory serif '1' with dark burgundy outline. Only the energy badge changes between variants. Card title '打击', type '攻击', short description '造成 6 点伤害。' Keep the cost badge compact, never half the card width. Do not put a huge separate logo above or behind the card.
3. Right approximately 70%: a wide 16:9 view of the referenced ACTUAL combat screenshot with Karen standing left, stone enemy right, cards along the bottom and top HUD. Replace only the original left-bottom combat energy counter with the respective design and update visible hand-card upper-left energy badges to the same design. Combat orb remains in its authentic bottom-left HUD location and at authentic relative screen size, about 6-8% of viewport width. It must never float in the middle of the battle or behind a character. Show ivory '3/3' on the orb in all variants. Keep visible hand badges small and normal. Preserve the top HUD and end-turn button.
4. Under the screenshot, one compact 2x zoom crop of the BOTTOM-LEFT HUD CORNER showing the same energy counter embedded among its neighboring environment/UI, labelled 'HUD DETAIL'. This is a contextual crop, not a giant detached icon on a blank background. The small battle screenshot shows real relative size; this detail allows comparing materials. Do not over-fill the layout with repeated detached logos.
5. Tiny consistent labels 'CARD' above card area and 'COMBAT' above screenshot area, 'HUD DETAIL' by the crop.

Materials/style: follow the supplied hand-painted UI concepts with coral #FB5458, dark burgundy #8B2A2D and pale gold/ivory; do not re-render as photorealistic jewels. Maintain broad forms, bold dark outlines and clear white numeric overlays. All four full card portraits, all four battle backgrounds and characters MUST be the same repeated reference for honest side-by-side assessment. Focus on what the SMALL COST BADGE looks like on the card and what the COUNTER looks like in the real battle HUD. No unrelated characters, no additional concepts, no long explanatory copy, no watermarks. This is explicitly a concept mockup, not evidence of actual implementation.
```

## 定点修订

```text
Edit this existing 2x2 Karen energy UI comparison image with ONE precise correction. Preserve the full canvas, all four A/B/C/D panels, layout, colors, portraits, battlefield screenshots, frames, all icons and labels exactly as they are. In the TOP-LEFT quadrant A — STAR HAIRCLIP, the large card's cost correctly says 1 and must remain 1. However, the STAR-SHAPED COMBAT ENERGY HUD COUNTER in the bottom-left corner INSIDE A's main battlefield screenshot incorrectly says '1'. Change ONLY THAT COMBAT HUD number from '1' to '3/3', using the same cream serif digits and burgundy outline as the correctly rendered '3/3' in A's HUD DETAIL crop directly beneath it. Keep the star frame identical and make enough room for legible 3/3 inside its red center. The card cost badges everywhere remain 1 as before. B/C/D already have correct combat 3/3 and must stay unchanged. Do not reinterpret the UI, do not add new art, do not remove anything. Return the entire corrected comparison image at the same 3:2 aspect ratio, with maximal sharpness and resolution.
```
