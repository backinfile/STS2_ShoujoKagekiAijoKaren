"""Assemble original-speed MCP footage with captions outside the game viewport."""
import argparse
import json
import pathlib
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=pathlib.Path, required=True)
args = parser.parse_args()
root = args.output.resolve()
segments = [('stable', 'arrogant-normal', '正式版'), ('stable', 'arrogant-fast', '正式版 · 快速'),
            ('beta', 'arrogant-normal', '测试版')]

def probe(path):
    return json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_format', '-show_streams', '-of', 'json', str(path)], text=True))

def timestamp(value):
    centis = round(value * 100)
    return f'{centis // 360000}:{centis // 6000 % 60:02}:{centis // 100 % 60:02}.{centis % 100:02}'

header = '''[Script Info]
ScriptType: v4.00+
PlayResX: 1280
PlayResY: 768
WrapStyle: 2
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Default,Microsoft YaHei,21,&H00F7F0E7,&H000000FF,&H00101418,&H00000000,0,0,0,0,100,100,0,0,1,1,0,2,16,16,14,1
[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
'''
events = []
inputs = []
filters = []
offset = 0
metadata = []
for branch, scene, label in segments:
    folder = root / branch / scene
    path = folder / 'game.mp4'
    info = probe(path)
    duration = float(info['format']['duration'])
    assert any(s['codec_type'] == 'audio' for s in info['streams']), path
    markers = json.loads((folder / 'markers.json').read_text(encoding='utf-8'))
    for i, marker in enumerate(markers):
        begin = 0 if i == 0 else marker['time']
        end = markers[i + 1]['time'] if i + 1 < len(markers) else duration
        text = (label + '｜' + marker['title']).replace('\n', ' ').replace('{', '').replace('}', '')
        events.append(f'Dialogue: 0,{timestamp(offset+begin)},{timestamp(offset+end)},Default,,0,0,0,,{text}')
    index = len(metadata)
    inputs += ['-i', str(path)]
    filters += [f'[{index}:v]setpts=PTS-STARTPTS[v{index}]',
                f'[{index}:a]apad,atrim=duration={duration},asetpts=PTS-STARTPTS[a{index}]']
    metadata.append(dict(branch=branch, scene=scene, duration=duration, starts_at=offset))
    offset += duration
(root / 'captions.ass').write_text(header + '\n'.join(events) + '\n', encoding='utf-8-sig')
filters += [''.join(f'[v{i}][a{i}]' for i in range(len(segments))) + f'concat=n={len(segments)}:v=1:a=1[video][audio]',
            '[video]pad=iw:ih+48:0:0:color=0x101318,ass=captions.ass[captioned]',
            '[audio]volume=6dB[audible]']
subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'warning', '-y', *inputs,
                '-filter_complex', ';'.join(filters), '-map', '[captioned]', '-map', '[audible]', '-c:v', 'libx264', '-crf', '18',
                '-preset', 'fast', '-threads', '2', '-c:a', 'aac', '-b:a', '192k', '-movflags', '+faststart', 'arrogant-showcase.mp4'], cwd=root, check=True)
(root / 'showcase-metadata.json').write_text(json.dumps(dict(segments=metadata, audio_gain_db=6, result=probe(root / 'arrogant-showcase.mp4')), indent=2), encoding='utf-8')
print(root / 'arrogant-showcase.mp4')
