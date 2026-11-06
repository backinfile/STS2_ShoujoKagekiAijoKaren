"""Compare the generated design with raw gameplay, without compositing effects into gameplay."""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--branch', choices=['stable', 'beta'], required=True)
args = parser.parse_args()
main = Path(__file__).resolve().parents[3]
root = args.output.resolve()
reference = Image.open(main / 'docs/design/rare-vfx/unforgettable-shine-2026-10-16/slash-reference-redraw.png').convert('RGB')
actual = Image.open(root / args.branch / 'visual-stages/last/02-cut.png').convert('RGB')
result = Image.new('RGB', (1536, 1580), '#0d1723')
draw = ImageDraw.Draw(result)
title = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 28)
small = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 22)
draw.text((20, 12), 'ImageGen 参考图（非实机）', font=title, fill='#ffe0a1')
result.paste(reference.resize((1536, 768)), (0, 60))
draw.text((20, 850), args.branch.upper() + ' 实机：完整场景与刀光放大（仅裁切缩放）', font=title, fill='#c4d7e8')
result.paste(actual.resize((1024, 576)), (0, 910))
# Fixed boss-test viewport crop; original full viewport remains alongside it.
w, h = actual.size
box = tuple(round(v * (w / 1280 if i % 2 == 0 else h / 720)) for i, v in enumerate((850, 190, 1110, 450)))
result.paste(actual.crop(box).resize((512, 512)), (1024, 910))
draw.text((20, 1518), '参考图角色姿势为示意；实机刀光采用生成主体，专属音效已移除。', font=small, fill='#c4d7e8')
out = root / 'preview' / args.branch
out.mkdir(parents=True, exist_ok=True)
result.save(out / 'reference-vs-game.png')
print(out / 'reference-vs-game.png')
