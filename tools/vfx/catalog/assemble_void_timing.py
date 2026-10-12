"""Export two separate, captioned MCP clips for the Void tower timing review."""
import argparse
import json
import pathlib
from assemble import assemble, call

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=pathlib.Path, required=True)
parser.add_argument('--branch', choices=['stable', 'beta'], default='stable')
args = parser.parse_args()
root = args.output.resolve()
assemble(root, args.branch, entries=[
    ('void-tower-occupied', '世界上最空虚的人 · 约定牌堆有牌'),
    ('void-tower-empty', '世界上最空虚的人 · 约定牌堆无牌'),
], title='Karen 卡牌星星随光传递与图标重组 · 2026-10-12', filename='Karen-Void-Timing-2026-10-12.mp4')
gain = json.loads((root / 'audio-gain.json').read_text(encoding='utf-8'))['gainDb']
for index, condition in [(1, 'Occupied'), (2, 'Empty')]:
    call(['ffmpeg', '-hide_banner', '-y', '-i', str(root / 'edit' / f'{index:02}.mp4'),
          '-c:v', 'copy', '-c:a', 'aac', '-b:a', '192k', '-af', f'volume={gain}dB',
          '-movflags', '+faststart', str(root / f'Karen-Void-{condition}-2026-10-12.mp4')],
         root / 'edit' / (condition.lower() + '-export.log'))
