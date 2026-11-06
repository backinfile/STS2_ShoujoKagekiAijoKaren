"""Join ordinary-speed actual-game clips, keeping captions outside the viewport."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
root = args.output.resolve()

def stamp(value):
    centis = round(value * 100)
    return f'{centis // 360000}:{centis // 6000 % 60:02}:{centis // 100 % 60:02}.{centis % 100:02}'

header = '''[Script Info]
ScriptType: v4.00+
PlayResX: 1280
PlayResY: 768
WrapStyle: 2
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Default,Microsoft YaHei,21,&H00E7F0F7,&H000000FF,&H00181410,&H00000000,0,0,0,0,100,100,0,0,1,1,0,2,16,16,14,1
[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
'''
events, inputs, filters, segments = [], [], [], []
offset = 0
for i, (branch, label) in enumerate([('stable', '正式版 v0.107.1'), ('beta', '测试版 v0.111.0')]):
    folder = root / branch / 'pile-view'
    path = folder / 'game.mp4'
    markers = json.loads((folder / 'markers.json').read_text(encoding='utf-8'))
    # Stop before fast-mode test scenes; the showcase preserves normal game timing.
    duration = markers[2]['time']
    for j in range(2):
        begin = 0 if j == 0 else markers[j]['time']
        end = markers[j + 1]['time']
        text = label + '｜' + ('约定牌堆：双星细线' if j == 0 else '闪耀耗尽牌堆：空心星与余烬')
        events.append(f'Dialogue: 0,{stamp(offset + begin)},{stamp(offset + end)},Default,,0,0,0,,{text}')
    inputs += ['-i', str(path)]
    filters += [f'[{i}:v]trim=duration={duration},setpts=PTS-STARTPTS[v{i}]',
                f'[{i}:a]apad,atrim=duration={duration},asetpts=PTS-STARTPTS[a{i}]']
    segments.append(dict(branch=branch, duration=duration, starts_at=offset,
                         source_sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    offset += duration
(root / 'pile-view-captions.ass').write_text(header + '\n'.join(events) + '\n', encoding='utf-8-sig')
filters += ['[v0][a0][v1][a1]concat=n=2:v=1:a=1[video][audio]',
            '[video]pad=iw:ih+48:0:0:color=0x101318,ass=pile-view-captions.ass[captioned]']
target = root / 'pile-view-showcase.mp4'
subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'warning', '-y', *inputs,
                '-filter_complex', ';'.join(filters), '-map', '[captioned]', '-map', '[audio]',
                '-c:v', 'libx264', '-crf', '18', '-preset', 'fast', '-threads', '2',
                '-c:a', 'aac', '-b:a', '160k', '-movflags', '+faststart', str(target)], cwd=root, check=True)
media = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_streams', '-show_format', '-of', 'json', str(target)], text=True))
(root / 'showcase-metadata.json').write_text(json.dumps(dict(segments=segments, media=media,
       result_sha256=hashlib.sha256(target.read_bytes()).hexdigest()), ensure_ascii=False, indent=2), encoding='utf-8')
print(target)
