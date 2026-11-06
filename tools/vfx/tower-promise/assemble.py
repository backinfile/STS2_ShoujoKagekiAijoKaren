"""Cut real MCP recordings into a short review video; never synthesize game frames."""
import argparse
import hashlib
import html
import json
import pathlib
import re
import shutil
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--stable', type=pathlib.Path, required=True)
parser.add_argument('--beta', type=pathlib.Path, required=True)
parser.add_argument('--output', type=pathlib.Path, required=True)
args = parser.parse_args()
out = args.output.resolve()
render = out / 'render'
render.mkdir(parents=True, exist_ok=True)
ffmpeg = shutil.which('ffmpeg')
ffprobe = shutil.which('ffprobe')
assert ffmpeg and ffprobe, 'FFmpeg and FFprobe must be on PATH'


def run(argv, **kwargs):
    return subprocess.run(argv, check=True, encoding='utf-8', errors='replace', **kwargs)


def probe(path):
    return json.loads(subprocess.check_output(
        [ffprobe, '-v', 'error', '-show_streams', '-show_format', '-of', 'json', str(path)], encoding='utf-8'))


sources = {}
manifest = {'edits': 'Trim/concatenate, title band outside the viewport, game audio +10 dB. Original speed; no VFX composited in post.',
            'sources': [], 'chapters': []}
for branch in ('stable', 'beta'):
    folder = getattr(args, branch).resolve()
    source = folder / f'tower-promise-{branch}.mp4'
    recording = json.loads((folder / 'recording.json').read_text(encoding='utf-8-sig'))
    checks = json.loads((folder / 'checks.json').read_text(encoding='utf-8-sig'))
    assert recording['status'] == 'completed' and recording['audio_source'] == 'game_process'
    assert len(checks) == 35 and all(c['passed'] for c in checks), folder
    chapters = json.loads((folder / 'chapters.json').read_text(encoding='utf-8-sig'))
    sources[branch] = (source, {c['case']: c['time'] for c in chapters})
    manifest['sources'].append({'branch': branch, 'file': source.as_posix(),
                                'sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
                                'captureFps': recording['capture_fps'], 'passedChecks': len(checks)})

segments = [
    ('stable', 'normal', '正式版 · 约定重逢', '双路蓄光 → 观景台星光 → 真实手牌落点', 3.6),
    ('stable', 'repeat', '正式版 · 再次取回', '连续触发后清理尾迹，塔回到静息状态', 3.6),
    ('stable', 'fast', '正式版 · 快速模式', '缩短蓄光与尾光，保留取回确认', 3.2),
    ('stable', 'full', '正式版 · 仅余一个手牌空位', '实际只取回一张卡牌，逐张对应尾迹', 3.6),
    ('beta', 'normal', '测试版 · 约定重逢', '同一套演出适配 v0.111.0 的原生移牌', 3.6),
    ('beta', 'small-flip', '测试版 · 缩小与翻转', '光路随当前塔坐标，卡牌抵达真实手牌位置', 3.6),
    ('beta', 'empty', '测试版 · 空库存', '仅显示短促弱星光，实际不生成回收尾迹', 2.6),
    ('beta', 'void', '测试版 · 空虚模式', '从当前抽牌堆图标发出，仍由实际卡牌确认抵达', 3.6),
]
clips = []
cursor = 0.0
for i, (branch, case, title, subtitle, length) in enumerate(segments):
    source, chapters = sources[branch]
    start = max(0, chapters[case] - 0.08)
    (render / f'title-{i}.txt').write_text(title, encoding='utf-8')
    (render / f'sub-{i}.txt').write_text(subtitle + '  |  游戏实录 · 原速', encoding='utf-8')
    filters = ("pad=1280:816:0:96:color=0x101822,"
               f"drawtext=fontfile='C\\:/Windows/Fonts/msyh.ttc':textfile=title-{i}.txt:fontcolor=0xffd18a:fontsize=29:x=26:y=10,"
               f"drawtext=fontfile='C\\:/Windows/Fonts/msyh.ttc':textfile=sub-{i}.txt:fontcolor=0xc5d9ec:fontsize=18:x=28:y=58")
    clip = render / f'{i:02d}.mp4'
    run([ffmpeg, '-hide_banner', '-loglevel', 'error', '-y', '-ss', str(start), '-i', str(source), '-t', str(length),
         '-vf', filters, '-r', '30', '-c:v', 'libx264', '-threads', '2', '-preset', 'fast', '-crf', '18',
         '-pix_fmt', 'yuv420p', '-af', 'volume=10dB', '-c:a', 'aac', '-ar', '48000', '-ac', '2', '-b:a', '160k', str(clip)], cwd=render)
    duration = float(probe(clip)['format']['duration'])
    manifest['chapters'].append({'title': title, 'start': cursor, 'duration': duration,
                                 'sourceBranch': branch, 'sourceCase': case, 'sourceStart': start})
    cursor += duration
    clips.append(clip)
    print(f'Rendered {i + 1}: {title}', flush=True)

(render / 'concat.txt').write_text(''.join(f"file '{p.as_posix()}'\n" for p in clips), encoding='utf-8')
final = out / 'tower-of-promise-showcase.mp4'
run([ffmpeg, '-hide_banner', '-loglevel', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(render / 'concat.txt'),
     '-c', 'copy', '-movflags', '+faststart', str(final)])
run([ffmpeg, '-hide_banner', '-loglevel', 'error', '-y', '-ss', '1.8', '-i', str(final), '-frames:v', '1', str(out / 'poster.png')])

qa = {'metadata': probe(final), 'motion': []}
for clip in clips:
    result = run([ffmpeg, '-hide_banner', '-loglevel', 'error', '-i', str(clip), '-vf', 'crop=1280:720:0:96,fps=6',
                  '-an', '-f', 'framemd5', '-'], capture_output=True)
    hashes = [line.rsplit(',', 1)[-1].strip() for line in result.stdout.splitlines() if not line.startswith('#')]
    assert len(set(hashes)) >= 5, (clip, hashes)
    qa['motion'].append({'file': clip.name, 'sampledFrames': len(hashes), 'uniqueFrames': len(set(hashes))})
audio = run([ffmpeg, '-hide_banner', '-i', str(final), '-af', 'volumedetect', '-vn', '-f', 'null', '-'], capture_output=True)
qa['audioMeanDb'] = float(re.search(r'mean_volume: ([\d.\-]+) dB', audio.stderr).group(1))
qa['audioPeakDb'] = float(re.search(r'max_volume: ([\d.\-]+) dB', audio.stderr).group(1))
assert qa['audioMeanDb'] > -65, qa
freeze = run([ffmpeg, '-hide_banner', '-i', str(final), '-vf', 'freezedetect=n=-50dB:d=1', '-an', '-f', 'null', '-'], capture_output=True)
qa['freezeDiagnostics'] = [line for line in freeze.stderr.splitlines() if 'freeze_' in line]
(out / 'qa.json').write_text(json.dumps(qa, ensure_ascii=False, indent=2), encoding='utf-8')
(out / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
repo = pathlib.Path(__file__).resolve().parents[3]
shutil.copy2(repo / 'docs/worktree-tasks/karen-promise-tower-vfx-20261015/preview.png', out / 'preview.png')
buttons = ''.join(f'<button data-time="{c["start"]}">{html.escape(c["title"])}</button>' for c in manifest['chapters'])
page = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>约定之塔 · 特效重制</title><style>
*{box-sizing:border-box}body{margin:0;background:#101822;color:#edf2f7;font:16px/1.65 system-ui,"Microsoft YaHei",sans-serif}
main{max-width:1328px;margin:auto;padding:28px 24px}h1{margin:0;font-size:28px}p{color:#b8c7d7}video,img{display:block;width:100%;height:auto}
nav{display:flex;flex-wrap:wrap;gap:8px;margin:18px 0 28px}button{border:1px solid #3e4c5e;background:#1c2939;color:#ffd18a;padding:9px 15px;cursor:pointer;font:inherit;font-size:14px}
button:focus-visible,button:hover{outline:2px solid #ffd18a}details{margin:24px 0}summary{cursor:pointer;color:#ffd18a}a{color:#a4d2ff}</style>
<main><h1>约定之塔 · 双光重逢</h1><p>正式版 v0.107.1 / 测试版 v0.111.0 · 原速游戏实录与游戏进程音频 · 70 项专项检查通过</p>
<video id="film" src="tower-of-promise-showcase.mp4" poster="poster.png" controls preload="metadata" playsinline></video><nav>BUTTONS</nav>
<details><summary>查看 imagegen 三阶段预览</summary><img src="preview.png" alt="双路蓄光、观景台释放、手牌抵达的三阶段美术预览"></details>
<p>剪辑只裁切时间、拼接片段、在原视口外添加标题并提高游戏音轨播放音量；没有后期合成特效。专项结果包含取消、普通、连续取回、缩小翻转、快速、Instant、手牌上限、空库存与空虚模式。多人联机未测。</p>
<p><a href="manifest.json">片段与来源记录</a> · <a href="qa.json">音视频检查</a></p></main>
<script>document.querySelectorAll('button').forEach(b=>b.onclick=()=>{film.currentTime=Number(b.dataset.time);film.play()})</script></html>'''
(out / 'index.html').write_text(page.replace('BUTTONS', buttons), encoding='utf-8')
print(final)
