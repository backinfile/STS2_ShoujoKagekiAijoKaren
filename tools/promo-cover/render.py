#!/usr/bin/env python3
"""Render the approved Bilibili promo cover in 16:9 and 4:3 from local assets."""
import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont, ImageOps


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]


def find_font(preferred, alternatives):
    for candidate in [preferred, *alternatives]:
        try:
            return ImageFont.truetype(str(candidate), 16).path
        except OSError:
            continue
    raise ValueError(f"Font unavailable: {preferred}. Set --font-cn or --font-latin.")


def render(layout, assets, config, font_cn, font_latin):
    width, height = layout["size"]
    style = config["style"]

    def cn(size):
        face = ImageFont.truetype(str(font_cn), size)
        try:
            face.set_variation_by_name("Black")
        except (OSError, ValueError, AttributeError):
            pass  # A static bold font is also supported.
        return face

    game = assets["battle"].convert("RGB")
    bg = ImageOps.fit(game, (width, height), method=Image.Resampling.LANCZOS)
    bg = bg.filter(ImageFilter.GaussianBlur(8))
    bg = ImageEnhance.Color(bg).enhance(1.20)
    bg = ImageEnhance.Brightness(bg).enhance(0.56).convert("RGBA")

    portrait = assets["portrait"].convert("RGB")
    if layout["portrait"]["size"]:
        portrait = portrait.resize(tuple(layout["portrait"]["size"]), Image.Resampling.LANCZOS)
    portrait = portrait.point(lambda value: round(255 * (value / 255) ** 0.70))
    portrait = ImageEnhance.Color(portrait).enhance(1.12).convert("RGBA")
    horizontal = np.interp(np.arange(portrait.width), [0, 80, 630, portrait.width - 1], [0, 0, 255, 255]).astype(np.uint8)
    portrait.putalpha(Image.fromarray(np.tile(horizontal, (portrait.height, 1))))
    bg.alpha_composite(portrait, tuple(layout["portrait"]["xy"]))

    shade = np.zeros((height, width, 4), dtype=np.uint8)
    x = np.arange(width, dtype=np.float32)[None, :]
    shade[:, :, :3] = style["shade"]
    shade[:, :, 3] = np.clip((1040 - x) / 1040 * 120, 0, 120).astype(np.uint8)
    bg = Image.alpha_composite(bg, Image.fromarray(shade))

    accents = Image.new("RGBA", (width, height))
    draw = ImageDraw.Draw(accents)
    for points, color in zip(layout["accent_polygons"], style["accent_fills"]):
        draw.polygon([tuple(point) for point in points], fill=tuple(color))
    for points, color, thickness in zip(layout["accent_lines"], style["accent_line_colors"], [8, 3]):
        draw.line([tuple(point) for point in points], fill=tuple(color), width=thickness)
    bg = Image.alpha_composite(bg, accents)

    panel_width = layout["panel"]["width"]
    panel_height = round(panel_width * 9 / 16)
    shot = ImageOps.fit(game, (panel_width, panel_height), method=Image.Resampling.LANCZOS)
    shot = ImageEnhance.Brightness(shot).enhance(1.18)
    shot = ImageEnhance.Color(shot).enhance(1.12)
    panel = Image.new("RGBA", (panel_width + 26, panel_height + 26), (255, 255, 255, 255))
    ImageDraw.Draw(panel).rectangle((7, 7, panel_width + 18, panel_height + 18), fill=(25, 17, 36, 255))
    panel.paste(shot, (13, 13))
    panel = panel.rotate(2.5, Image.Resampling.BICUBIC, expand=True)
    px, py = layout["panel"]["xy"]
    mask = Image.new("L", (width, height))
    mask.paste(panel.getchannel("A"), (px + 10, py + 12))
    shadow = Image.new("RGBA", (width, height), (9, 3, 17, 0))
    shadow.putalpha(mask.filter(ImageFilter.GaussianBlur(14)).point(lambda value: round(value * 0.8)))
    bg = Image.alpha_composite(bg, shadow)
    bg.alpha_composite(panel, (px, py))

    card = assets["card"].convert("RGBA")
    card.thumbnail(tuple(layout["card"]["size"]), Image.Resampling.LANCZOS)
    card = card.rotate(-9, Image.Resampling.BICUBIC, expand=True)
    card_alpha = Image.new("L", (width, height))
    card_alpha.paste(card.getchannel("A"), tuple(layout["card"]["xy"]))
    outline = Image.new("RGBA", (width, height), (255, 250, 237, 0))
    outline.putalpha(card_alpha.filter(ImageFilter.MaxFilter(19)))
    bg = Image.alpha_composite(bg, outline)
    bg.alpha_composite(card, tuple(layout["card"]["xy"]))

    star = assets["star"].convert("RGBA").resize((116, 116), Image.Resampling.LANCZOS)
    star = star.rotate(-14, Image.Resampling.BICUBIC, expand=True)
    bg.alpha_composite(star, tuple(layout["star_xy"]))

    typography = Image.new("RGBA", (width, height))
    draw = ImageDraw.Draw(typography)
    for key, color in [("title", "headline"), ("subtitle_latin", "subtitle"), ("subtitle_cn", "subtitle")]:
        text = config[key]
        size = layout[key]["size"]
        face = ImageFont.truetype(str(font_latin), size) if key == "subtitle_latin" else cn(size)
        tx, ty = layout[key]["xy"]
        draw.text((tx + 7, ty + 11), text, font=face, fill=(121, 18, 52, 255), stroke_width=18, stroke_fill=(14, 10, 31, 255))
        draw.text((tx, ty), text, font=face, fill=tuple(style[color]), stroke_width=13, stroke_fill=(26, 15, 38, 255))
    return Image.alpha_composite(bg, typography).convert("RGB")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config", type=Path, default=HERE / "template.json")
    parser.add_argument("--aspect", choices=["all", "16:9", "4:3"], default="all")
    for field in ["title", "subtitle-latin", "subtitle-cn", "output-stem"]:
        parser.add_argument(f"--{field}")
    for field in ["battle", "card", "portrait", "star"]:
        parser.add_argument(f"--{field}", type=Path)
    parser.add_argument("--font-cn", type=Path)
    parser.add_argument("--font-latin", type=Path)
    parser.add_argument("--output-dir", type=Path, default=ROOT / "output")
    args = parser.parse_args()
    config_path = args.config.resolve()
    config = json.loads(config_path.read_text(encoding="utf-8"))
    for key in ["title", "subtitle_latin", "subtitle_cn", "output_stem"]:
        value = getattr(args, key)
        if value is not None:
            config[key] = value
    stem = config["output_stem"]
    if not stem or any(char in stem for char in '<>:"/\\|?*') or stem.endswith((" ", ".")):
        parser.error("--output-stem must be a filename, without directory components.")

    assets = {}
    for key, configured in config["assets"].items():
        path = getattr(args, key) or (config_path.parent / configured)
        with Image.open(path) as source:
            assets[key] = source.copy()
    if "A" not in assets["card"].getbands():
        parser.error("--card must be a transparent PNG cutout, not a full screenshot.")
    font_cn = find_font(args.font_cn or "NotoSansSC-VF.ttf", [] if args.font_cn else ["NotoSansCJK-Bold.ttc", "msyhbd.ttc"])
    font_latin = find_font(args.font_latin or "arialbd.ttf", [] if args.font_latin else ["LiberationSans-Bold.ttf", "DejaVuSans-Bold.ttf"])
    args.output_dir.mkdir(parents=True, exist_ok=True)
    for aspect, layout in config["layouts"].items():
        if args.aspect not in ("all", aspect):
            continue
        image = render(layout, assets, config, font_cn, font_latin)
        name = stem + layout["suffix"]
        image.save(args.output_dir / f"{name}.png", optimize=True)
        image.save(args.output_dir / f"{name}.jpg", quality=93, subsampling=0, optimize=True)
        image.resize(tuple(layout["preview"]), Image.Resampling.LANCZOS).save(args.output_dir / f"{name}-预览.jpg", quality=93)
        print(f"{aspect}: {image.width}x{image.height}; PNG, JPG, thumbnail saved.")


if __name__ == "__main__":
    main()
