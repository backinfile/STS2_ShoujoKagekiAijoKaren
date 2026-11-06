# Imagegen 第六版

模式：内置 imagegen，compositing。

输入：第五版测试证据中的原生选牌截图 without-border.png，已关闭全部旧特效，仅作为 UI 布局编辑目标。

输出原件：<user-home>/.codex/generated_images/01a1422e-05e3-7e82-9aa7-edf3d827d914/exec-fc34688d-57ab-4bf5-b807-5f5309f70a9b.png

项目副本：[preview-v6.png](preview-v6.png)，未加工。

用途：重建光色、边缘分布与层次的概念参考；运行时由新生成的透明光斑素材和 canvas_item Shader 实现，不将预览截图作为背景。

## Exact prompt

```text
Use case: compositing.
Asset type: high quality game VFX concept preview for a Slay the Spire 2 hand-selection screen.
Input image 1 is the EDIT TARGET: preserve its game UI, characters, cards, Chinese text, proportions and dark native selection backdrop.
Primary request: create a completely NEW SCREEN-EDGE effect: amber-white theatrical light leaks sweeping through translucent deep crimson at the outer edges, as if moving stage spotlights glance across the lens. Broad organic tapered washes and a few bright curved caustic crests emerge from beyond the frame. Rich layered light with soft translucent depth, sophisticated anime theatre color design. A luminous lower-left sweep is balanced by a thinner upper-right sweep, with quiet breathing space along the other edges; broken asymmetric composition, not an enclosing drawn rectangle.
Keep effects confined to outermost 8 to 12 percent of the image, mostly clipped by screen edges, with smoothly transparent inward falloff. The central 70 percent MUST remain identical and unobstructed, cards and selection text must remain crisp and readable. Top HUD and confirm/peek controls remain readable.
Palette: wine red / scarlet outer light, warm peach and amber-white light crests.
Avoid: all ribbons, cloth, silk scraps, confetti, petals, stars, floating dots, orbiting objects, thin line loops, geometric borders, ornate frames, curtains, background scenery, magic circles. Do not reuse any existing VFX. Do not add text. The only addition is fresh photographic/stylized luminous edge light.
```

## Runtime light texture

模式：内置 imagegen，stylized-concept，transparent_background=true。

输出原件：<user-home>/.codex/generated_images/01a1422e-05e3-7e82-9aa7-edf3d827d914/exec-d9054b79-a4de-4029-8614-e158bba1f815.png

项目副本：images/vfx/why/edge_light.png。PNG 原样复制，保留 alpha，没有图像后期处理。

```text
Use case: stylized-concept.
Asset type: production transparent game VFX texture, new optical light leak.
Primary request: one beautiful broad photographic / anime-compositing light leak plume, elongated horizontally, with a rich ember-crimson diffused glow and a warm peach-white brilliant irregular hotspot near the left quarter. Several overlapping translucent caustic veils taper toward the right into nothing. Extremely soft airy falloff and subtle optical refractions inside the glow. The bright mass is broad and softly tapered, never a thin uniform line. Gentle irregular branching highlights provide depth. The glow is light itself, not a material object.
Composition: wide landscape canvas, light stretches across lower half of canvas, hottest base along bottom, transparent at the top, both ends and all outside the plume. Include comfortable empty transparent padding. A single cohesive plume, no layout or contact sheet.
Palette: rich wine / scarlet translucency, amber and peach midtones, creamy white light core.
Background: actual transparency, soft semitransparent glow feathering into alpha zero. No black or colored backdrop.
Avoid: ribbons, cloth, silk, fabric folds, scraps, confetti, stars, dust particles, physical flames, smoke, lasers, hard outlines, magic circles, frame borders, UI, text, watermark. This will be warped and animated as abstract screen-edge illumination.
```
