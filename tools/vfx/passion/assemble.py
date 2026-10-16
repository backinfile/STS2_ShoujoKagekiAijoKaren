"""Package actual MCP clips at original speed; retain game audio and check the final encode."""
import argparse
import json
from pathlib import Path
import re
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--input', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
source = args.input.resolve()
out = args.output.resolve()
out.mkdir(parents=True, exist_ok=True)
font = 'C\\:/Windows/Fonts/msyh.ttc'
segments = [
    ('normal', '获得激情 Buff', '金红星光 → 状态图标 / 约一秒的获得确认', None),
    ('repeat', '激情 Buff · 连续叠层', '每次正向增加都响应 / 战后追加 3 份遗物奖励', None),
    ('reward', '战后 · 额外遗物奖励', '特效只确认预约，遗物到战后才揭晓', 2.2),
]
chapters = []
position = 0.0
encoded = []
for index, (name, title, caption, start) in enumerate(segments):
    video = source / (name + '.mp4')
    if not video.exists():
        continue
    title_file = out / f'{index}-title.txt'
    subtitle_file = out / f'{index}-caption.txt'
    title_file.write_text(title, encoding='utf-8')
    subtitle_file.write_text(caption, encoding='utf-8')
    target = out / f'part-{index}.mp4'
    filters = (
        'drawbox=x=iw-414:y=63:w=396:h=64:color=0x172027@0.76:t=fill,'
        f"drawtext=fontfile='{font}':textfile='{title_file.name}':x=w-398:y=71:fontsize=21:fontcolor=0xffdf9e,"
        f"drawtext=fontfile='{font}':textfile='{subtitle_file.name}':x=w-398:y=101:fontsize=13:fontcolor=0xf4e7d0"
    )
    command = ['ffmpeg', '-hide_banner', '-loglevel', 'warning', '-y']
    if start is not None:
        command += ['-ss', str(start)]
    command += ['-i', str(video), '-vf', filters, '-r', '30', '-c:v', 'libx264', '-threads', '2',
                '-preset', 'medium', '-crf', '18', '-pix_fmt', 'yuv420p', '-c:a', 'aac', '-b:a', '160k',
                '-movflags', '+faststart', str(target)]
    subprocess.run(command, cwd=out, check=True)
    info = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'json', str(target)]))
    duration = float(info['format']['duration'])
    chapters.append(dict(title=title, start=position, duration=duration))
    position += duration
    encoded.append(target)
(out / 'concat.txt').write_text(''.join(f"file '{p.name}'\n" for p in encoded), encoding='utf-8')
native = out / 'passion-showcase-native.mp4'
subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'warning', '-y', '-f', 'concat', '-safe', '0', '-i', 'concat.txt',
                '-vf', 'fps=30', '-c:v', 'libx264', '-threads', '2', '-preset', 'medium', '-crf', '18', '-pix_fmt', 'yuv420p',
                '-af', 'asetpts=N/SR/TB', '-c:a', 'aac', '-b:a', '160k', '-movflags', '+faststart', str(native)], cwd=out, check=True)
native_sound = subprocess.run(['ffmpeg', '-hide_banner', '-i', str(native), '-vn', '-af', 'volumedetect', '-f', 'null', 'NUL'], capture_output=True, text=True, check=True)
native_peak = float(re.search(r'max_volume: ([\d.\-]+) dB', native_sound.stderr)[1])
gain = -12.0 - native_peak
final = out / 'passion-showcase.mp4'
subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'warning', '-y', '-i', str(native), '-c:v', 'copy',
    '-af', f'volume={gain:.2f}dB', '-c:a', 'aac', '-b:a', '160k', '-movflags', '+faststart', str(final)], check=True)
metadata = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_streams', '-show_format', '-of', 'json', str(final)]))
assert any(s['codec_type'] == 'video' and s['width'] == 1280 and s['height'] == 720 and s['r_frame_rate'] == '30/1' for s in metadata['streams'])
assert any(s['codec_type'] == 'audio' for s in metadata['streams'])
sound = subprocess.run(['ffmpeg', '-hide_banner', '-i', str(final), '-vn', '-af', 'volumedetect', '-f', 'null', 'NUL'], capture_output=True, text=True, check=True)
(out / 'audio-check.txt').write_text(sound.stderr, encoding='utf-8')
peak = float(re.search(r'max_volume: ([\d.\-]+) dB', sound.stderr)[1])
assert -60 < peak < 0, peak
(out / 'chapters.json').write_text(json.dumps(chapters, ensure_ascii=False, indent=2), encoding='utf-8')
(out / 'validation.json').write_text(json.dumps(dict(duration=metadata['format']['duration'], fps=30,
    width=1280, height=720, audio_peak_db=peak, audio_gain_db=gain, original_speed=True, game_process_audio=True), indent=2), encoding='utf-8')
# Samples from encoded output, not source clips, for layout and animation review.
subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', str(final), '-vf', 'fps=3,scale=640:360', str(out / 'review-%03d.jpg')], check=True)
print(final)
