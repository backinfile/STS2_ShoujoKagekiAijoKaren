"""Assemble labeled native MCP recordings, including one explicitly labeled half-speed replay."""
import argparse
import json
import pathlib
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('capture', type=pathlib.Path)
parser.add_argument('destination', type=pathlib.Path)
args = parser.parse_args()
capture = args.capture.resolve()
destination = args.destination.resolve()
destination.parent.mkdir(parents=True, exist_ok=True)
edit = capture / 'edit'
edit.mkdir(exist_ok=True)
scenes = [
    ('normal4', '背靠背  /  4 张约定 · 16 伤害', 1),
    ('ten', '协同重斩  /  10 张约定 · 40 伤害', 1),
    ('ten', '剑光细节  /  0.5× 慢放', .5),
    ('upgraded', '背靠背＋  /  10 张约定 · 60 伤害', 1),
    ('high24', '24 张约定 · 96 伤害  /  表现强度封顶，伤害照常计算', 1),
    ('fast', '快速模式  /  更短的蓄势与收招', 1),
    ('replay', '额外重播  /  三次出手，各自命中与清理', 1),
    ('zero', '空约定  /  0 伤害 · 轻薄空斩', 1),
    ('one', '1 张约定  /  4 伤害', 1),
    ('lethal', '最后一击  /  命中后正常结束战斗', 1),
]
markers = []
clock = 0
for i, (name, title, speed) in enumerate(scenes):
    source = capture / name / 'game.mp4'
    assert source.is_file(), source
    # The slow replay isolates the actual strike; all other scenes retain native timing.
    start, duration = (.80, 1.5) if speed == .5 else (0, None)
    ass = f'''[Script Info]
ScriptType: v4.00+
PlayResX: 1280
PlayResY: 720
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Default,Microsoft YaHei,25,&H00F1F4FF,&H00F1F4FF,&H80111720,&H80111720,0,0,0,0,100,100,0,0,3,8,0,7,72,32,65,1
[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
Dialogue: 0,0:00:00.00,0:01:00.00,Default,,0,0,0,,{title}
'''
    subtitle = edit / f'{i:02}.ass'
    subtitle.write_text(ass, encoding='utf-8-sig')
    target = edit / f'{i:02}.mp4'
    command = ['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y']
    if start: command += ['-ss', str(start)]
    command += ['-i', str(source)]
    # Trim in the filter before stretching the slow replay, so its entire 1.5 s source is retained.
    video = (f'trim=duration={duration},setpts=PTS-STARTPTS,' if duration else '')
    video += f'setpts=PTS/{speed},fps=30,subtitles={subtitle.name},format=yuv420p'
    audio = (f'atrim=duration={duration},asetpts=PTS-STARTPTS,' if duration else '') + f'atempo={speed}'
    command += ['-vf', video, '-af', audio, '-c:v', 'libx264', '-crf', '20', '-preset', 'fast',
                '-c:a', 'aac', '-b:a', '160k', '-movflags', '+faststart', str(target)]
    subprocess.run(command, cwd=edit, check=True)
    length = float(subprocess.check_output(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', str(target)]))
    markers.append(dict(start=clock, duration=length, title=title, source=str(source.relative_to(capture)), speed=speed))
    clock += length
concat = edit / 'concat.txt'
concat.write_text(''.join(f"file '{i:02}.mp4'\n" for i in range(len(scenes))), encoding='utf-8')
subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-f', 'concat', '-safe', '0',
    '-i', str(concat), '-r', '30', '-fps_mode', 'cfr', '-c:v', 'libx264', '-crf', '20', '-preset', 'fast',
    '-c:a', 'aac', '-b:a', '160k', '-movflags', '+faststart', str(destination)], check=True)
destination.with_suffix('.chapters.json').write_text(json.dumps(markers, ensure_ascii=False, indent=2), encoding='utf-8')
print(destination, f'{clock:.2f}s')
