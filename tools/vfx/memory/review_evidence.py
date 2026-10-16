"""Contact sheet of every MCP before/after capture, for manual visual verification."""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--branch', choices=['stable', 'beta'], required=True)
args = parser.parse_args()
root = args.output / args.branch
scenes = ['ordinary', 'last', 'upgrade', 'fast', 'instant', 'pose', 'replay', 'lethal']
sheet = Image.new('RGB', (1920, 1180), '#101823')
draw = ImageDraw.Draw(sheet)
font = ImageFont.truetype('C:/Windows/Fonts/consola.ttf', 18)
for i, name in enumerate(scenes):
    for j, stage in enumerate(['before', 'after']):
        index = i * 2 + j
        x, y = index % 4 * 480, index // 4 * 295
        path = root / name / (stage + '.png')
        if not path.exists(): continue
        with Image.open(path) as frame:
            sheet.paste(frame.resize((480, 270)), (x, y + 25))
        draw.text((x + 8, y + 3), name + ' / ' + stage, font=font, fill='#e5d1a1')
dest = root / 'mcp-before-after.png'
sheet.save(dest)
print(dest)
