"""Add a separate caption band without covering or retiming the game footage."""
import argparse
import json
from pathlib import Path
import re
import subprocess
from PIL import Image, ImageDraw, ImageFont

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--branch', choices=['stable', 'beta'], required=True)
parser.add_argument('--scenes', default='ordinary,last')
args = parser.parse_args()
root = args.output.resolve()
out = root / 'preview' / args.branch
out.mkdir(parents=True, exist_ok=True)
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 26)
small = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 18)
labels = {
    'ordinary': ('01  永不遗忘的闪耀 · 参考图重制', 'ImageGen 透明刀光主体 · 少量独立粒子 · 已移除专属音效'),
    'last': ('02  闪耀耗尽 · 无奖励专属特效', '奖励数值保留 · 无奖励专属特效 · 已移除本牌专属音效'),
    'upgrade': ('03  永不遗忘的闪耀＋', '升级版 · 40 点伤害 · 耗尽后封存四次奖励'),
    'replay': ('04  第二幕重播', '两次实际斩击 · 奖励仅在最终耗尽后确认'),
}
clips = []
for name in args.scenes.split(','):
    source = root / args.branch / name / 'game.mp4'
    assert source.exists(), source
    band = Image.new('RGB', (1280, 96), '#111c28')
    draw = ImageDraw.Draw(band)
    draw.line((26, 1, 1254, 1), fill='#b88e5d', width=2)
    draw.text((28, 14), labels[name][0], font=font, fill='#ffe0a1')
    draw.text((29, 54), labels[name][1], font=small, fill='#c4d7e8')
    version = 'v0.107.1' if args.branch == 'stable' else 'v0.111.0'
    draw.text((1040, 22), f'{args.branch.upper()}  {version}', font=small, fill='#91adca')
    png = out / (name + '-caption.png')
    band.save(png)
    audio = subprocess.run(['ffmpeg', '-hide_banner', '-i', str(source), '-af', 'volumedetect', '-vn', '-sn', '-f', 'null', 'NUL'], capture_output=True, text=True, encoding='utf-8').stderr
    peak = float(re.search(r'max_volume: ([-\d.]+) dB', audio)[1])
    assert peak > -70, 'Silent game capture'
    # Calibrate to each capture's measured peak; no additional sound sources.
    gain = min(18, max(0, -3 - peak))
    clip = out / (name + '.mp4')
    subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', str(source), '-i', str(png),
                    '-filter_complex', '[0:v]pad=1280:816:0:0:color=0x111c28[v];[v][1:v]overlay=0:720[out]',
                    '-map', '[out]', '-map', '0:a:0', '-c:v', 'libx264', '-threads', '2', '-crf', '18', '-preset', 'fast',
                    '-af', f'volume={gain}dB', '-c:a', 'aac', '-b:a', '192k', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(clip)], check=True)
    (out / (name + '-audio.json')).write_text(json.dumps(dict(sourcePeakDb=peak, previewGainDb=gain)), encoding='utf-8')
    clips.append(clip)
listing = out / 'concat.txt'
listing.write_text(''.join("file '" + p.as_posix() + "'\n" for p in clips), encoding='utf-8')
final = out / f'Unforgettable-Shine-{args.branch}.mp4'
subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(listing),
                '-c', 'copy', '-movflags', '+faststart', str(final)], check=True)
meta = subprocess.run(['ffprobe', '-v', 'error', '-show_streams', '-show_format', '-of', 'json', str(final)], capture_output=True, text=True, encoding='utf-8', check=True).stdout
(out / 'media.json').write_text(meta, encoding='utf-8')
subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', str(final), '-vf', 'fps=2,scale=640:-1,tile=4x5',
                '-frames:v', '1', str(out / 'review.png')], check=True)
print(final)
