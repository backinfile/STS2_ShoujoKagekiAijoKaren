# Exchange Fate ornament atlas

`ornaments.png` is an original image_gen asset, RGBA 1774×887 with genuine transparent padding. Left and right 887×887 cells contain the red enamel crown and crossed gold hairclip. The engine selects a cell at drawing time; the PNG is not cropped or recolored offline. Mipmaps and linear mip filtering keep the small metal highlights stable in motion.

The built-in generation prompt is preserved in [the task record](../../../docs/worktree-tasks/exchange-fate-vfx-20261016/imagegen-v2-assets.txt). The [in-game target preview](../../../docs/worktree-tasks/exchange-fate-vfx-20261016/preview-v2.png) guides composition rather than acting as a screen overlay.

The six silk meshes use `shaders/karen_fate_silk.gdshader` with per-instance material parameters. All geometry, flow, dust and lighting remain runtime effects attached to the combat room. The cosmetic RNG is independent of the game's RNG. Audio is synthesized by `tools/vfx/exchange-fate/make_audio.py`.
