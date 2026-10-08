# Gear sprite provenance and filtering

`steel-gear.png` replaces the procedural wheel drawing in both Geer effects.

- Generated with the built-in imagegen tool on 2026-10-08: one orthographic cool-steel gear, six open spokes, transparent background, broad cel shading, no grain or dense line textures. The generator did not identify its exact underlying model.
- Original: 1254×1254 RGBA. Source and comparison copies are retained under the task worktree's `artifacts/geer-image-vfx/`.
- Compared GPT Image 2 Artifact Cleaner strengths 0.25 and 0.50 using the existing local installation and FLUX.2-VAE. Selected 0.25 to soften fine generated surface noise conservatively. Original alpha was copied verbatim and checked equal before resizing; RGB reconstruction can change detail.
- Final delivery: 1024×1024 lossless RGBA PNG, Lanczos downsampling, no sharpening. The resizing step naturally resamples alpha as well.
- Godot import: lossless, `mipmaps/generate=true`, `process/fix_alpha_border=true`. Drawing uses `CanvasItem.TextureFilterEnum.LinearWithMipmaps` for rotation and reduction to the small character wheels.
- This reduces source microtexture and sampling aliasing; it does not claim to eliminate every possible moire pattern on every display. The texture is a stylized visual wheel, not a mechanical engineering model of tooth meshing.

The cleaning utility and neural model are local processing dependencies, not bundled in the Mod.
