# 第七版：从参考图提取边缘光层

模式：内置 Imagegen，background-extraction，transparent_background=true。

唯一输入：[preview-v6.png](preview-v6.png)，不是重新设计的概念图。保持参考的左下/右上斜向曝光和宽幅红色柔光。

原件：<user-home>/.codex/generated_images/01a1422e-05e3-7e82-9aa7-edf3d827d914/exec-b8c62e4f-386b-4200-b192-8c9a9dd06c5a.png

项目副本：images/vfx/why/reference_border.png。原 PNG 原样复制，保留透明度；不使用图像后期移除背景，不将 UI 或战场带进素材。

## Exact prompt

```text
Use case: background-extraction.
Asset type: exact-layout transparent full-screen game VFX overlay, landscape 16:9.
Input image 1 is the EDIT TARGET and visual source of truth. Extract ONLY its existing soft red edge illumination and amber-white optical light leaks into a transparent RGBA image. Preserve the reference composition, widths, softness, colors, diagonal angles and placement. Do NOT redesign the effect.
Keep the broad diffused wine-red glow along the outer edges and corners. Keep the dominant broad creamy amber diagonal exposure in the LOWER LEFT, which bends from the left edge near 72 percent height toward the bottom edge near 15 percent width. Keep the smaller angled amber-white exposure in the UPPER RIGHT near 95 percent width and 12 percent height. Preserve the broad red haze connecting the corners, especially lower left and lower right. Light should be softly spread over the outer 10–15 percent, not squeezed into a thin line.
Remove ALL game content: every character, card, UI, Chinese text, icon, HUD, scenery, objects, stone wall, black selection dimmer and all background pixels. The entire open central region must be truly transparent, with nothing inside it. Outer light itself should have soft semitransparent alpha feathering naturally into the empty center. Reconstruct only the light that was behind game UI, following its existing glow.
No new light plumes, no flames, no ribbons, no narrow neon strokes, no floating sparks, no extra ornaments, no duplicate highlights, no closed geometric frame. This is an extraction of the pictured cinematic edge-light overlay, not a new concept. Actual transparent background, no opaque black or gray matte, no checkerboard painted into the file. Preserve the full reference canvas layout.
```
