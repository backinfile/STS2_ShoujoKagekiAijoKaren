"""Present two native MCP clips; add an external title strip and playback gain only."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True, type=Path)
parser.add_argument('--output', required=True, type=Path)
args = parser.parse_args()
source = args.source.resolve()
out = args.output.resolve()
out.mkdir(parents=True, exist_ok=False)
clips = []
manifest = dict(edition='stable v0.107.1', capture='KarenSTS2MCP viewport + game_process audio',
    editing='Uncropped native game view, external 64px title strip, concatenation, +9 dB playback gain. No VFX added in post.', sources=[])

def run(argv, **kw):
    return subprocess.run(argv, check=True, **kw)

for i, (name, label) in enumerate([
    ('normal9', '星光闪耀之时  ·  正常速度  ·  九次闪耀集中兑现'),
    ('fast', '星光闪耀之时  ·  快速模式  ·  三次兑现与奖励确认'),
]):
    (out / f'title-{i}.txt').write_text(label, encoding='utf-8')
    clip = out / f'part-{i}.mp4'
    vf = ("pad=1280:784:0:64:color=0x0c1420,"
        f"drawtext=fontfile='C\\:/Windows/Fonts/msyh.ttc':textfile=title-{i}.txt:fontcolor=0xffd49a:fontsize=26:x=25:y=18")
    run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', str(source / f'{name}.mp4'),
        '-vf', vf, '-r', '30', '-c:v', 'libx264', '-threads', '2', '-preset', 'fast', '-crf', '18', '-pix_fmt', 'yuv420p',
        '-af', 'volume=9dB', '-c:a', 'aac', '-ar', '48000', '-ac', '2', '-b:a', '160k', '-movflags', '+faststart', str(clip)], cwd=out)
    clips.append(clip)
    manifest['sources'].append(dict(file=name + '.mp4', sha256=hashlib.sha256((source / f'{name}.mp4').read_bytes()).hexdigest()))
(out / 'concat.txt').write_text(''.join(f"file '{p.as_posix()}'\n" for p in clips), encoding='utf-8')
final = out / 'karen-star-showcase.mp4'
run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(out / 'concat.txt'), '-c', 'copy', '-movflags', '+faststart', str(final)])
metadata = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_format', '-show_streams', '-of', 'json', str(final)], text=True))
audio = run(['ffmpeg', '-hide_banner', '-i', str(final), '-af', 'volumedetect', '-vn', '-f', 'null', '-'], capture_output=True, text=True)
qa = dict(metadata=metadata, audioMeanDb=float(re.search(r'mean_volume: ([\d.\-]+) dB', audio.stderr).group(1)),
    audioPeakDb=float(re.search(r'max_volume: ([\d.\-]+) dB', audio.stderr).group(1)), motion=[])
assert qa['audioMeanDb'] > -65 and qa['audioPeakDb'] < 0
for clip in clips:
    hashes = subprocess.check_output(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-i', str(clip),
        '-vf', 'crop=1280:720:0:64,fps=4', '-an', '-f', 'framemd5', '-'], text=True)
    frames = [line.rsplit(',', 1)[-1].strip() for line in hashes.splitlines() if not line.startswith('#')]
    qa['motion'].append(dict(clip=clip.name, sampledFrames=len(frames), uniqueFrames=len(set(frames))))
    assert len(set(frames)) > 8
freeze = run(['ffmpeg', '-hide_banner', '-i', str(final), '-vf', 'freezedetect=n=-50dB:d=1', '-an', '-f', 'null', '-'], capture_output=True, text=True)
qa['freezeDiagnostics'] = [line for line in freeze.stderr.splitlines() if 'freeze_' in line]
(out / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
(out / 'qa.json').write_text(json.dumps(qa, ensure_ascii=False, indent=2), encoding='utf-8')
run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-ss', '3.1', '-i', str(final), '-frames:v', '1', str(out / 'poster.png')])
print(final)
