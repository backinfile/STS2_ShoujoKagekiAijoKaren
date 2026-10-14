"""Build review sheets from MCP before/after screenshots and actual video frames."""
import argparse
import hashlib
import json
import pathlib
import subprocess
from PIL import Image, ImageDraw, ImageFont
from assemble import CURRENT_ENTRIES, probe

parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True, type=pathlib.Path)
parser.add_argument('--branch', choices=['stable', 'beta'], default='stable')
parser.add_argument('--partial', action='store_true')
args = parser.parse_args()
root = args.output.resolve()
review = root / 'review' / args.branch
review.mkdir(parents=True, exist_ok=True)
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 17)
rows = []
repo = pathlib.Path(__file__).resolve().parents[3]
pack_hash = hashlib.sha256((repo / 'artifacts/ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck').read_bytes()).hexdigest()
for index, (name, title) in enumerate(CURRENT_ENTRIES, 1):
    folder = root / args.branch / name
    if args.partial and not (folder / 'recording-after.png').exists():
        continue
    if args.partial and json.loads((folder / 'recording.json').read_text(encoding='utf-8'))['captureBuild']['packSha256'] != pack_hash:
        continue
    duration = float(probe(folder / 'game.mp4')['format']['duration'])
    markers = json.loads((folder / 'markers.json').read_text(encoding='utf-8'))
    # Prefer the first real card action, excluding long idle / hover holds.
    time = min(duration - .1, (markers[-1 if name == 'starlight-act2' else 0]['time'] if markers else 0) + .8)
    frame = review / (name + '-motion.png')
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-ss', str(time), '-i', str(folder / 'game.mp4'),
                    '-frames:v', '1', str(frame)], check=True)
    row = Image.new('RGB', (1278, 268), '#111b21')
    draw = ImageDraw.Draw(row)
    draw.text((6, 2), f'{index:02} {title}   |   MCP before / recorded motion / MCP after', font=font, fill='white')
    for col, path in enumerate([folder / 'recording-before.png', frame, folder / 'recording-after.png']):
        with Image.open(path) as img:
            row.paste(img.convert('RGB').resize((426, 240)), (col * 426, 28))
    rows.append(row)
for start in range(0, len(rows), 4):
    page = Image.new('RGB', (1278, 268 * len(rows[start:start+4])), '#111b21')
    for j, row in enumerate(rows[start:start+4]): page.paste(row, (0, j * 268))
    path = review / f'sheet-{start // 4 + 1:02}.jpg'
    page.save(path, quality=94)
    print(path, flush=True)
