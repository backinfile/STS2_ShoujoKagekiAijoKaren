# C 生产贴图

- 内置 image_gen 生成；输入为 ../persistent-v2/C-lingering-star.png，角色、场景、文字均不进入生产图。
- 生成原图原样复制到 images/vfx/unforgettable/lingering-star.png，不通过程序改写像素。
- 生成源：C:/Users/17575/.codex/generated_images/01a14eee-aa2d-7c62-8922-3895df012aaf/exec-b9cdf83e-fa68-4149-91b2-18fdf3b7b3c8.png。

```text
Use case: background-extraction
Asset type: production transparent RGBA sprite for a 2D game single-hit sword impact and persistent air scar.
Input image 1: reference and extraction target. It is a concept board with a warm gold oblique four-point impact on a stone dummy.
Primary request: Isolate and recreate ONLY the exact warm ivory/gold four-point star-shaped sword cut visible in the large top panel of the reference. Remove the character, dummy, stone stage, text, panels, every background object and all cast light on surfaces. Output ONE isolated small VFX shape centered on a fully transparent square canvas.
Subject silhouette: one long needle-tapered diagonal from lower-left to upper-right crossing a much shorter needle-tapered diagonal from upper-left to lower-right, forming FOUR sharp unequal arms. The short axis is about 40% of the long axis. The arms merge at one tiny luminous ivory core. Keep the elegant narrow cutting shape of the reference; no additional rays, no extra stars, no particles or sparks.
Materials/colors: warm white and pale ivory central blade, thin restrained pale-gold border, barely-there soft amber halo right at the core only. The core must stay small and the arms fine; this is a sword-cut scar, not a fat decorative badge or explosion. The long axis should take about 78% of the canvas width and height; retain clean transparent padding around it. Preserve smooth antialiased alpha edges and pure transparent exterior, no checkerboard painted into the image, no opaque black rectangle, no shadow.
Composition: square sprite, centered at the exact intersection, longer tips at bottom-left and top-right, no text, no variants, no storyboard. No other objects. This SAME texture will be dimmed and retained in combat, so its silhouette must remain readable with low opacity.
```
