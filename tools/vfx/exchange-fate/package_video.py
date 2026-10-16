"""Cut MCP game recordings into a short normal / slow / Void / repeat showcase."""
import argparse, json, subprocess
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('folder', type=Path, help='Contains final-stable and final-beta verification directories')
a = p.parse_args()
root = a.folder.resolve()
parts = root / 'video-parts'; parts.mkdir(exist_ok=True)
clips = [
    (root / 'final-stable/stable/normal.mp4', .6, None, 1, '正式版 · 正常速度'),
    (root / 'final-stable/stable/normal.mp4', 1.5, 2, .5, '正式版 · 0.5 倍慢放'),
    (root / 'final-beta/beta/void.mp4', .6, None, 1, '测试版 · 空虚模式：与抽牌堆交换'),
    (root / 'final-beta/beta/repeat.mp4', .6, None, 1, '测试版 · 升级与连续交换'),
]
for i, (source, start, length, speed, caption) in enumerate(clips):
    header = '[Script Info]\nScriptType: v4.00+\nPlayResX: 1280\nPlayResY: 720\n[V4+ Styles]\nFormat: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding\n'
    style = 'Style: Default,Microsoft YaHei,23,&H00FFFFFF,&H00FFFFFF,&H80201518,&H80201518,0,0,0,0,100,100,1,0,3,1,0,8,24,24,63,1\n'
    events = '[Events]\nFormat: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text\n'
    (parts / f'{i}.ass').write_text(header + style + events + f'Dialogue: 0,0:00:00.00,0:01:00.00,Default,,0,0,0,,{caption}\n', encoding='utf-8-sig')
    command = ['ffmpeg', '-v', 'error', '-y', '-ss', str(start)]
    if length: command += ['-t', str(length)]
    command += ['-i', str(source), '-vf', f'setpts=PTS/{speed},subtitles={i}.ass', '-af', f'atempo={speed}',
        '-r', '30', '-c:v', 'libx264', '-crf', '18', '-preset', 'fast', '-pix_fmt', 'yuv420p',
        '-c:a', 'aac', '-b:a', '192k', '-ar', '48000', '-movflags', '+faststart', f'{i}.mp4']
    subprocess.run(command, cwd=parts, check=True)
(parts / 'concat.txt').write_text(''.join(f"file '{i}.mp4'\n" for i in range(len(clips))), encoding='utf-8')
final = root / 'exchange-fate-showcase.mp4'
subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', 'concat.txt',
    '-c:v', 'copy', '-af', 'volume=8dB', '-c:a', 'aac', '-b:a', '192k', '-movflags', '+faststart', str(final)], cwd=parts, check=True)
probe = subprocess.check_output(['ffprobe', '-v', 'error', '-show_format', '-show_streams', '-of', 'json', str(final)])
(root / 'video-metadata.json').write_bytes(probe)
subprocess.run(['ffmpeg', '-v', 'info', '-i', str(final), '-vn', '-af', 'volumedetect', '-f', 'null', '-'],
    stdout=subprocess.DEVNULL, stderr=(root / 'video-audio-check.log').open('w', encoding='utf-8'), check=True)
print(final)
