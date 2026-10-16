"""Package actual MCP footage at original speed, with captions outside the game viewport."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--source', type=Path, required=True)
args = parser.parse_args()
root = args.source.resolve()
out = root / 'preview'
out.mkdir(exist_ok=True)


def run(argv, **kw):
    return subprocess.run(argv, check=True, **kw)


def audio(path):
    result = run(['ffmpeg', '-hide_banner', '-i', str(path), '-af', 'volumedetect', '-vn', '-f', 'null', '-'],
                 capture_output=True, text=True, encoding='utf-8', errors='replace')
    return {name: float(re.search(name + r'_volume: ([\d.\-]+) dB', result.stderr).group(1)) for name in ['mean', 'max']}


manifest = dict(capture='KarenSTS2MCP game viewport + game_process audio',
                edits='Original speed, external 80px bottom caption strip, measured playback gain and concatenation. No VFX added in post.', clips=[])
clips = []
for branch, edition in [('stable', '正式版 v0.107.1'), ('beta', '测试版 v0.111.0')]:
    source = root / branch / 'jealousy' / 'game.mp4'
    rec = json.loads((source.parent / 'recording.json').read_text(encoding='utf-8'))
    marks = json.loads((source.parent / 'markers.json').read_text(encoding='utf-8'))
    assert rec['status'] == 'completed' and rec['audio_source'] == 'game_process'
    levels = audio(source)
    assert levels['mean'] > -65, levels
    gain = min(12, -2 - levels['max'])
    for i, text in enumerate([f'嫉妒  ·  {edition}  ·  普通攻击', f'嫉妒＋  ·  {edition}  ·  升级攻击',
                              '角色保持原位 · 方片掠击 → 命中碎闪    |    原速游戏实录', '升级20伤害 · 同一方片攻击与命中    |    原速游戏实录']):
        (out / f'{branch}-{i}.txt').write_text(text, encoding='utf-8')
    change = marks[1]['time']
    vf = 'pad=1280:800:0:0:color=0x0d181c'
    for i in range(4):
        enabled = f'lt(t,{change})' if i % 2 == 0 else f'gte(t,{change})'
        vf += (f",drawtext=fontfile='C\\:/Windows/Fonts/msyh.ttc':textfile={branch}-{i}.txt:"
               f"fontcolor={'0xf1d397' if i < 2 else '0xb9d6ce'}:fontsize={24 if i < 2 else 16}:x=24:y={730 if i < 2 else 766}:enable='{enabled}'")
    final = out / f'Karen-Jealousy-{branch}.mp4'
    run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', str(source), '-vf', vf, '-r', '30',
         '-c:v', 'libx264', '-threads', '2', '-preset', 'fast', '-crf', '18', '-pix_fmt', 'yuv420p',
         '-af', f'volume={gain}dB', '-c:a', 'aac', '-ar', '48000', '-ac', '2', '-b:a', '160k', '-movflags', '+faststart', str(final)], cwd=out)
    metadata = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_format', '-show_streams', '-of', 'json', str(final)], text=True))
    final_levels = audio(final)
    assert final_levels['max'] < 0 and final_levels['mean'] > -65, final_levels
    motion = subprocess.check_output(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-i', str(final),
        '-vf', 'crop=1280:720:0:0,fps=6', '-an', '-f', 'framemd5', '-'], text=True)
    frames = [line.rsplit(',', 1)[-1].strip() for line in motion.splitlines() if not line.startswith('#')]
    assert len(set(frames)) > 12
    manifest['clips'].append(dict(branch=branch, file=final.name, sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
        duration=float(metadata['format']['duration']), captureFps=rec['capture_fps'], audio=final_levels, gainDb=gain,
        motionSamples=len(frames), uniqueSamples=len(set(frames)), captureBuild=rec['captureBuild']))
    clips.append(final)
(out / 'concat.txt').write_text(''.join(f"file '{p.as_posix()}'\n" for p in clips), encoding='utf-8')
combined = out / 'Karen-Jealousy-showcase.mp4'
run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(out / 'concat.txt'),
     '-c', 'copy', '-movflags', '+faststart', str(combined)])
run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-ss', '1.0', '-i', str(combined), '-frames:v', '1', str(out / 'poster.png')])
(out / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print(combined)
