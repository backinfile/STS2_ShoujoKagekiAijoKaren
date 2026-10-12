# 七件新增遗物图片 · 2026-10-11

> 历史版本：此批含三张错误稿，怀表牌也已撤下；不要使用此处旧压缩包。当前交付见[修复 V4](../repair-v4/gallery.html)与[新版 PNG 包](../repair-v4/karen-seven-relics-v4.zip)。[错误复核记录](../source-audit-v3/references.html)保留供对照。


本批以 STS1 Karen Mod 原有素材为依据制作：怀表牌、饼干、小小星、棒球猫、Mr. White、星形发饰、星之王冠。名称仍按当前候选记录，遗物效果未修改，未接入游戏资源。

历史文件：[旧七件总览与前后对照](gallery.html) · [旧压缩包（含错误稿）](karen-seven-relics-cleaned.zip) · [旧总览图片](overview.png)。本目录 `final/` 仅保留该批历史输出，尺寸为1254×1254。

## 制作方式

- 图片编辑采用内置 image_gen；已有四张高清图沿用上一轮结果，新补绘怀表牌、金色四角星和双星发饰三张。所有参考来自本地 STS1 Karen 素材，不把修复稿称为原图。
- 本地 Artifact Cleaner 使用现有 CUDA 环境、LatentR 权重和 FLUX.2-VAE，试验 0.25 与 0.50 后选择 0.25。减弱细碎假纹理；不声称所有细纹均被消除，也不把原图小字推补当作准确还原。
- `cleaned/` 保留清理器的直接结果。七张的尺寸和 alpha 通道均与输入逐像素一致，验证见 [batch-results.json](batch-results.json)。
- `final/` 是交付版。怀表牌、小小星和星形发饰额外清除主体之外的稀薄透明杂点，保留主体 RGB 和紧邻轮廓的一像素抗锯齿区域；另外四张直接沿用清理结果。记录见 [alpha-results.json](alpha-results.json)。因此不能把后三张交付图描述为 alpha 完全未变。
- 原始 STS1 文件和前一轮高清稿均未覆盖；本轮输出也没有修改游戏实现或正式资源。

## 来源与提示词

- [逐件来源和差异说明](items.json)
- [三张新补绘提示词](new-edit-prompts.json)
- [已有四张的初次编辑提示词](../sts1-upscale-v1/prompts.json)及[返修提示词](../sts1-upscale-v1/refinement-prompts.json)
- [生成文件来源](generation-provenance.json)与[最终文件校验](manifest.json)
- [清理脚本](clean_relics.py)及[透明边缘清理脚本](finish_alpha.py)

## 查看

`gallery.html` 上方是七件交付图，下方逐件并列展示「STS1 原素材」「高清重绘、未清理」「本地清理后交付图」，并提供 100% 局部对比、浅深背景切换及 PNG 链接。图片缩略图只为查看，最终 PNG 未缩放。

小字、图案与低清轮廓仍有补绘成分；小小星选用金色四角星，普通位选用怀表牌，这两项是本轮素材方案，尚未代表游戏中的最终命名。
