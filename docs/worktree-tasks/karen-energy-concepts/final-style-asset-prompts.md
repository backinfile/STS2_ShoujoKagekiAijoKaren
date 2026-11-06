# 最终独立图标与场景处理提示词

全部图标通过内置 imagegen 生成透明 PNG，无数字；背景也由 imagegen 编辑。预览使用这些素材按本体尺寸建立代码原生 UI，再以浏览器截图导出；没有使用 Python 或手工栅格改图。

## A 切面皇冠

```text
Use case: background-extraction / precise-object-edit.
Extract ONE icon from the provided Karen Crown & Tomato comparison image as a clean reusable game UI texture.
Output a single isolated icon on a genuinely TRANSPARENT background, no shadows beyond the icon edge, no board or screenshot, no text and NO NUMBERS anywhere. Remove the '3/3' numeric overlay by completing the simple red energy interior consistently with its style; keep a broad unobstructed center for a separate numeric label. Center the icon in a square canvas with modest padding. Thick burgundy outline, coral-red body, simple light-red energy interior. Preserve the selected icon's recognizable silhouette and style. Do not include any panel title, card, frame, adjacent icon or rectangular background. Not photorealistic, no gold, jewels or extra decoration. Use only the chosen standalone LARGE detail icon, not any mistaken cost badge in the surrounding card.
Select the LARGE three-point ANGULAR FACETED CROWN in the top-left panel's DETAIL. Angular three peaks, broad lower body, 3-5 broad flat red bevel facets, crisp hand-painted surfaces. No distinct underline, band, jewels or circular medallion.
```

## B 圆润皇冠

```text
Use case: background-extraction / precise-object-edit.
Extract ONE icon from the provided Karen Crown & Tomato comparison image as a clean reusable game UI texture.
Output a single isolated icon on a genuinely TRANSPARENT background, no shadows beyond the icon edge, no board or screenshot, no text and NO NUMBERS anywhere. Remove the '3/3' numeric overlay by completing the simple red energy interior consistently with its style; keep a broad unobstructed center for a separate numeric label. Center the icon in a square canvas with modest padding. Thick burgundy outline, coral-red body, simple light-red energy interior. Preserve the selected icon's recognizable silhouette and style. Do not include any panel title, card, frame, adjacent icon or rectangular background. Not photorealistic, no gold, jewels or extra decoration. Use only the chosen standalone LARGE detail icon, not any mistaken cost badge in the surrounding card.
Select the LARGE ROUND-CORNERED three-point CROWN in the top-right panel's DETAIL. Three soft tips, rounded lower corners, only two softly blended matte shading regions. Avoid specular gloss, tiny bevels or embossed detail.
```

## C 平面皇冠

```text
Use case: background-extraction / precise-object-edit.
Extract ONE icon from the provided Karen Crown & Tomato comparison image as a clean reusable game UI texture.
Output a single isolated icon on a genuinely TRANSPARENT background, no shadows beyond the icon edge, no board or screenshot, no text and NO NUMBERS anywhere. Remove the '3/3' numeric overlay by completing the simple red energy interior consistently with its style; keep a broad unobstructed center for a separate numeric label. Center the icon in a square canvas with modest padding. Thick burgundy outline, coral-red body, simple light-red energy interior. Preserve the selected icon's recognizable silhouette and style. Do not include any panel title, card, frame, adjacent icon or rectangular background. Not photorealistic, no gold, jewels or extra decoration. Use only the chosen standalone LARGE detail icon, not any mistaken cost badge in the surrounding card.
Select the LARGE spare GRAPHIC CROWN in the middle-left panel's DETAIL. Three peaks with a squat broad lower body, simple two-tone coral paint, dark burgundy contour, only one broad curved energy stroke. Make this clearly flatter and less shaded than the other crowns.
```

## D 切面番茄

```text
Use case: background-extraction / precise-object-edit.
Extract ONE icon from the provided Karen Crown & Tomato comparison image as a clean reusable game UI texture.
Output a single isolated icon on a genuinely TRANSPARENT background, no shadows beyond the icon edge, no board or screenshot, no text and NO NUMBERS anywhere. Remove the '3/3' numeric overlay by completing the simple red energy interior consistently with its style; keep a broad unobstructed center for a separate numeric label. Center the icon in a square canvas with modest padding. Thick burgundy outline, coral-red body, simple light-red energy interior. Preserve the selected icon's recognizable silhouette and style. Do not include any panel title, card, frame, adjacent icon or rectangular background. Not photorealistic, no gold, jewels or extra decoration. Use only the chosen standalone LARGE detail icon, not any mistaken cost badge in the surrounding card.
Select the LARGE FACETED TOMATO in the middle-right panel's DETAIL. Single squat lobed red fruit with just 4-5 broad angular color planes, small plain green three-leaf calyx. Keep the fruit silhouette irregularly organic rather than a regular polygon. No leaf veins, stem, numbers, fruit seeds or face.
```

## E 圆润番茄

```text
Use case: background-extraction / precise-object-edit.
Extract ONE icon from the provided Karen Crown & Tomato comparison image as a clean reusable game UI texture.
Output a single isolated icon on a genuinely TRANSPARENT background, no shadows beyond the icon edge, no board or screenshot, no text and NO NUMBERS anywhere. Remove the '3/3' numeric overlay by completing the simple red energy interior consistently with its style; keep a broad unobstructed center for a separate numeric label. Center the icon in a square canvas with modest padding. Thick burgundy outline, coral-red body, simple light-red energy interior. Preserve the selected icon's recognizable silhouette and style. Do not include any panel title, card, frame, adjacent icon or rectangular background. Not photorealistic, no gold, jewels or extra decoration. Use only the chosen standalone LARGE detail icon, not any mistaken cost badge in the surrounding card.
Select the LARGE SOFT TOMATO in the bottom-left panel's DETAIL, NOT THE INCORRECT CROWN ON THAT PANEL'S FULL CARD. A slightly flattened red fruit, broad rounded shoulders, restrained matte soft shading, tiny single green calyx. Simpler shading, no speckles, pores, tiny facets or photorealism.
```

## F 平面番茄

```text
Use case: background-extraction / precise-object-edit.
Extract ONE icon from the provided Karen Crown & Tomato comparison image as a clean reusable game UI texture.
Output a single isolated icon on a genuinely TRANSPARENT background, no shadows beyond the icon edge, no board or screenshot, no text and NO NUMBERS anywhere. Remove the '3/3' numeric overlay by completing the simple red energy interior consistently with its style; keep a broad unobstructed center for a separate numeric label. Center the icon in a square canvas with modest padding. Thick burgundy outline, coral-red body, simple light-red energy interior. Preserve the selected icon's recognizable silhouette and style. Do not include any panel title, card, frame, adjacent icon or rectangular background. Not photorealistic, no gold, jewels or extra decoration. Use only the chosen standalone LARGE detail icon, not any mistaken cost badge in the surrounding card.
Select the LARGE GRAPHIC TOMATO in the bottom-right panel's DETAIL. Very simple flattened lobed tomato silhouette, flat two-tone coral body, thick dark burgundy outline, one tiny flat muted-green calyx and two broad dark-red inner energy curves. Clearly the most minimal of the tomatoes.
```

## 战斗背景清理

```text
Edit the supplied actual game screenshot to produce a clean reusable background for a UI concept comparison. Keep the EXACT 16:9 composition and every existing pixel region unchanged as much as possible: same Karen at left, stone enemy at right, waterfall, HUD top, cards bottom, labels, health bars, draw pile and end turn. Remove ONLY the red/pink energy counter with the debug number '102/3' in the bottom-left area, around x=65..155 and y=550..640 of the 1280x720 source. Fill that small area by continuing the dark blue waterfall/stone scenery behind it. Do not remove the neighboring draw pile below and do not add any replacement energy icon. Preserve the screenshot's 16:9 aspect, ideally 1280x720 or 1920x1080. The entire result is an almost identical screenshot with an empty energy counter spot. No title, sheet, panel, borders, cropping, labels or additional UI. Background opaque.
```
