"""Add a caption strip outside untouched MCP gameplay captures; preserve captured game audio."""
import argparse
import json
import pathlib
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=pathlib.Path, required=True)
parser.add_argument('--energy-detail', action='store_true', help='Add a same-frame energy-counter enlargement below the untouched gameplay')
args = parser.parse_args()
root = args.output.resolve()
font = 'C\\:/Windows/Fonts/msyh.ttc'
clips = []
for branch, version, label in [('stable', 'v0.107.1', '正式版'), ('beta', 'v0.111.0', '测试版')]:
    folder = root / branch / 'waiting-stage'
    source = folder / 'game.mp4'
    info = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_format', '-show_streams', '-of', 'json', str(source)]))
    duration = float(info['format']['duration'])
    assert any(s['codec_type'] == 'audio' for s in info['streams'])
    marks = json.loads((folder / 'markers.json').read_text(encoding='utf-8'))
    lines = [f'{label} {version}   舞台正在等待着｜圆角贴边｜光点数量等于能力层数']
    lines += [m['title'] for m in marks]
    filters = ['pad=1280:' + ('976' if args.energy_detail else '808') + ':0:0:color=0x111318']
    for i, line in enumerate(lines):
        name = f'caption-{i}.txt'
        (folder / name).write_text(line, encoding='utf-8')
        draw = f"drawtext=fontfile='{font}':textfile='{name}':fontcolor={'white' if i == 0 else '0xa9d9ff'}:fontsize={22 if i == 0 else 18}:x=24:y={735 if i == 0 else 774}"
        if i > 0:
            start = max(0, marks[i - 1]['time'])
            end = marks[i]['time'] if i < len(marks) else duration
            draw += f":enable='between(t,{start:.3f},{end:.3f})'"
        filters.append(draw)
    target = folder / 'captioned.mp4'
    render = ['ffmpeg', '-y', '-hide_banner', '-loglevel', 'error', '-i', str(source)]
    if args.energy_detail:
        graph = '[0:v]split[scene][detail];[scene]' + ','.join(filters) + '[caption];'
        graph += '[detail]crop=180:160:20:510,scale=252:224:flags=lanczos[zoom];'
        graph += '[caption][zoom]overlay=1004:722,'
        graph += f"drawtext=fontfile='{font}':text='能量槽局部 ×1.4':fontcolor=0xa9d9ff:fontsize=14:x=1004:y=950[out]"
        render += ['-filter_complex', graph, '-map', '[out]', '-map', '0:a:0']
    else:
        render += ['-vf', ','.join(filters)]
    subprocess.run(render + ['-c:v', 'libx264', '-preset', 'medium', '-crf', '18', '-c:a', 'aac', '-b:a', '192k',
                            '-movflags', '+faststart', str(target)], cwd=folder, check=True)
    clips.append(target)
playlist = root / 'clips.txt'
playlist.write_text(''.join(f"file '{p.as_posix()}'\n" for p in clips), encoding='utf-8')
target = root / 'stage-is-waiting-gameplay.mp4'
subprocess.run(['ffmpeg', '-y', '-hide_banner', '-loglevel', 'error', '-f', 'concat', '-safe', '0', '-i', str(playlist),
                '-c', 'copy', '-movflags', '+faststart', str(target)], check=True)
print(target)
