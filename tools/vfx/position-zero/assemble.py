"""Verify the two isolated captures and make a labelled, game-audio-only review video."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--input', required=True, type=Path)
parser.add_argument('--output', required=True, type=Path)
parser.add_argument('--name', default='light-revision')
args = parser.parse_args()
args.output.mkdir(parents=True, exist_ok=True)
reports = {}
project = Path(__file__).resolve().parents[3]
for branch in ['stable', 'beta']:
    folder = args.input / branch
    result = json.loads((folder / 'results.json').read_text(encoding='utf-8'))
    minimum = 13 if result.get('scope') == 'normal-only' else 26
    assert result['passed'] and len(result['checks']) >= minimum, result
    recording = json.loads((folder / 'recording.json').read_text(encoding='utf-8'))
    assert recording['status'] == 'completed' and recording['audio'] and recording['audio_source'] == 'game_process'
    assert recording['capture_fps'] >= 20 and recording['frames_repeated'] / recording['frames_written'] < .15, recording
    for name, expected in recording['build'].items():
        assert hashlib.sha256((project / 'artifacts/ShoujoKagekiAijoKaren' / name).read_bytes()).hexdigest() == expected, name
    recording.pop('output_path', None)
    recording['source'] = (folder / 'game.mp4').as_posix()
    log = (folder / 'godot.log').read_text(encoding='utf-8', errors='replace')
    assert not re.search(r'^(?:\[ERROR\]|ERROR:)', log, re.M), 'Runtime error in ' + branch
    assert '[Karen STS2 MCP]' in log
    hands = []
    for path in folder.glob('*-hand.json'):
        hand = json.loads(json.loads(path.read_text(encoding='utf-8'))['message'])
        assert hand['modelCount'] == hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons'], path
        hands.append(path.name)
    volume = subprocess.run(['ffmpeg', '-hide_banner', '-i', str(folder / 'game.mp4'), '-af', 'volumedetect',
                             '-vn', '-f', 'null', '-'], capture_output=True, text=True, check=True).stderr
    mean = float(re.search(r'mean_volume: ([-\d.]+)', volume)[1])
    peak = float(re.search(r'max_volume: ([-\d.]+)', volume)[1])
    assert mean > -80 and peak > -60, 'Silent recording'
    frozen = subprocess.run(['ffmpeg', '-hide_banner', '-i', str(folder / 'game.mp4'), '-vf', 'freezedetect=n=-50dB:d=1',
                             '-an', '-f', 'null', '-'], capture_output=True, text=True, check=True).stderr
    assert 'freeze_start' not in frozen, 'Review frozen interval in ' + branch
    reports[branch] = dict(passed=True, scope=result.get('scope', 'targeted-regressions'), checks=result['checks'], hands=hands, recording=recording,
                           audio=dict(meanDb=mean, peakDb=peak), freezeIntervalsOverOneSecond=0)

target = args.output / ('position-zero-' + args.name + '.mp4')
font = "C\\:/Windows/Fonts/arial.ttf"
graph = (
    f"[0:v]trim=start=0.6:end=14.5,setpts=PTS-STARTPTS,drawtext=fontfile='{font}':"
    "text='POSITION 0  /  Stable - ready, hover, block lights':x=24:y=88:fontsize=22:fontcolor=white:box=1:boxcolor=black@0.55:boxborderw=8[v0];"
    "[0:a]atrim=start=0.6:end=14.5,asetpts=PTS-STARTPTS,volume=8dB[a0];"
    f"[1:v]trim=start=0.6:end=14.5,setpts=PTS-STARTPTS,drawtext=fontfile='{font}':"
    "text='POSITION 0  /  Beta - ready, hover, block lights':x=24:y=88:fontsize=22:fontcolor=white:box=1:boxcolor=black@0.55:boxborderw=8[v1];"
    "[1:a]atrim=start=0.6:end=14.5,asetpts=PTS-STARTPTS,volume=8dB[a1];"
    "[v0][a0][v1][a1]concat=n=2:v=1:a=1[v][a]"
)
subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y',
    '-i', str(args.input / 'stable/game.mp4'), '-i', str(args.input / 'beta/game.mp4'),
    '-filter_complex', graph, '-map', '[v]', '-map', '[a]', '-c:v', 'libx264', '-preset', 'medium',
    '-crf', '19', '-pix_fmt', 'yuv420p', '-c:a', 'aac', '-b:a', '160k', '-movflags', '+faststart', str(target)], check=True)
def method(source, signature):
    start = source.index(signature)
    i = source.index('{', start)
    depth, end = 1, i + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]

source_path = 'src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenPositionZeroVfx.cs'
baseline = subprocess.check_output(['git', 'show', '724dfa2:' + source_path], cwd=project, text=True, encoding='utf-8')
current = (project / source_path).read_text(encoding='utf-8')
fixed_cast = {}
for signature in ['private void DrawStage()', 'private void Beam(']:
    assert method(baseline, signature) == method(current, signature), 'Fixed cast changed: ' + signature
    fixed_cast[signature] = hashlib.sha256(method(current, signature).encode()).hexdigest()

report = dict(date='2026-10-16', scope='Targeted singleplayer VFX verification; full matrix and multiplayer not run',
              fixedCast=dict(baseline='724dfa2', unchanged=True, methods=fixed_cast),
              branches=reports, video=dict(path=target.name, sha256=hashlib.sha256(target.read_bytes()).hexdigest(),
                                          note='Two native captures, labelled and concatenated; game process audio +8 dB, no added audio'))
(args.output / (args.name + '-verification.json')).write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(target)
