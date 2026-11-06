"""Pair real MCP recordings by action marker, preserving 1x speed and new-version audio."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--branch', choices=['stable', 'beta'], default='stable')
parser.add_argument('--wake-upgraded', action='store_true', help='Compare the full upgraded two-stage Wake Up sequence only')
parser.add_argument('--mahiru', action='store_true', help='Compare previous muted Jealousy with the TV Mahiru revision')
parser.add_argument('--geer-speed', action='store_true', help='Compare real Geer plays in Normal/Fast/Instant and mid-cast Instant switching')
args = parser.parse_args()
assert sum((args.mahiru, args.wake_upgraded, args.geer_speed)) <= 1
root = args.output.resolve()
repo = Path(__file__).resolve().parents[3]
delivery = root / 'comparison'
edit = delivery / 'edit'
edit.mkdir(parents=True, exist_ok=True)

def probe(path):
    return json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_format', '-show_streams', '-of', 'json', str(path)], encoding='utf-8'))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def stamp(t):
    n = round(t * 100)
    return f'{n // 360000}:{n // 6000 % 60:02}:{n // 100 % 60:02}.{n % 100:02}'

def call(command, log):
    result = subprocess.run(command, cwd=edit, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, encoding='utf-8', errors='replace')
    log.write_text(result.stdout, encoding='utf-8')
    if result.returncode:
        raise RuntimeError(result.stdout[-2500:])

scenes = [
    ('polish-jealousy', '嫉妒', '接触短切光更明确；保留原位攻击、回飘方向和88个粒子槽'),
    ('polish-wake', '唤醒', '展开后降低幕布亮度和流速；选牌时短脉冲，确认后重新收束'),
    ('polish-arrogant', '骄傲', '星扣在角色上方收势；奖励数量只由通用激情图标确认'),
    ('polish-relay', '接力耗尽', '终幕改为短绸带收尾；不再重复显示奖章与数量'),
]
if args.wake_upgraded:
    scenes = [('polish-wake-upgraded', '唤醒＋ · 两段完整选择', '第一段选牌堆保持原样；第二段选牌幕布收敛，确认后回手')]
if args.mahiru:
    scenes = [('polish-jealousy', '嫉妒 · TV真昼彩纸', '恢复粉／黄／绿／紫片面，增强侧翻、珍珠光泽与边角短闪；攻击方向和节奏保持')]
if args.geer_speed:
    scenes = [(f'geer-{mode}', title, note) for mode, title, note in [
        ('normal', '命运的齿轮 · 普通', '保留1秒施放与0.62秒触发；选牌后真实自动打出'),
        ('fast', '命运的齿轮 · 快速', '保留已有演出节拍及规则等待，选牌前齿轮收束'),
        ('instant', '命运的齿轮 · 瞬时', '瞬时模式跳过齿轮装饰与机械声，原生选牌和自动出牌保留'),
        ('switch-cast', '命运的齿轮 · 播放中切瞬时', '齿轮播放至0.30秒切换原生偏好；下一帧退出并停止所属音效')]]
manifest = []
for scene, title, note in scenes:
    before = root / 'before/stable' / scene
    after = root / 'after' / args.branch / scene
    paths = [before / 'game.mp4', after / 'game.mp4']
    durations = [float(probe(p)['format']['duration']) for p in paths]
    marks = [json.loads((folder / 'markers.json').read_text(encoding='utf-8-sig')) for folder in (before, after)]
    assert len(marks[0]) == len(marks[1]) > 0
    captures = [json.loads((folder / 'recording.json').read_text(encoding='utf-8-sig')) for folder in (before, after)]
    for capture, package, branch in [(captures[0], root / 'before-package', 'stable'), (captures[1], root / ('after-package' if args.geer_speed else 'final-package'), args.branch)]:
        assert capture['status'] == 'completed' and capture['audio_source'] == 'game_process'
        assert capture['captureBuild']['assemblySha256'] == sha(package / 'lib' / f'ShoujoKagekiAijoKaren.{branch.title()}.dll')
        assert capture['captureBuild']['packSha256'] == sha(package / 'ShoujoKagekiAijoKaren.pck')
    for step in range(len(marks[0])):
        index = len(manifest) + 1
        starts = [max(0, markers[step]['time'] - .12) for markers in marks]
        ends = [min(duration, markers[step + 1]['time'] - .12) if step + 1 < len(markers) else duration
                for markers, duration in zip(marks, durations)]
        spans = [end - start for start, end in zip(starts, ends)]
        length = max(spans)
        entry = dict(index=index, scene=scene, title=title, phase=marks[1][step]['title'], note=note,
                     sources=[str(p.relative_to(repo)) for p in paths], sourceSha256=[sha(p) for p in paths],
                     starts=starts, ends=ends, duration=length,
                     captures=[{k:c.get(k) for k in ('encoder','capture_fps','frames_repeated','frames_dropped','captureBuild')} for c in captures])
        name = f'{index:02}'
        ass = '''[Script Info]
ScriptType: v4.00+
PlayResX: 1920
PlayResY: 660
WrapStyle: 2
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Main,Microsoft YaHei,25,&H00FFFFFF,&H00FFFFFF,&H0010151C,&H0010151C,0,0,0,0,100,100,0,0,1,0,0,7,0,0,0,1
[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
'''
        def line(text):
            return f'Dialogue: 0,0:00:00.00,{stamp(length)},Main,,0,0,0,,{text}\n'
        ass += line(r'{\pos(22,10)\b1\c&HAAA9A0&}' + ('修改前 · main 386aa5a' if args.geer_speed else '上一轮 · 压色版' if args.mahiru else '修改前 · main 782a71e'))
        ass += line(r'{\pos(982,10)\b1\c&H8BCEFF&}' + ('修改后 · 瞬时模式修复' if args.geer_speed else '修改后 · TV真昼参考修订' if args.mahiru else '修改后 · 审查调整版'))
        ass += line(r'{\an9\pos(1894,13)\fs20\c&HAAA9A0&}1× 原速 · 右侧游戏原声')
        ass += line(r'{\pos(22,598)\b1\fs25}' + f'{index:02}  {title}  ·  ' + marks[1][step]['title'].split('｜')[-1])
        ass += line(r'{\pos(22,633)\fs19\c&HD6DFDF&}' + note)
        (edit / (name + '.ass')).write_text(ass, encoding='utf-8-sig')
        video = []
        for i, (start, end, span) in enumerate(zip(starts, ends, spans)):
            video.append(f'[{i}:v]trim=start={start}:end={end},setpts=PTS-STARTPTS,scale=960:540,setsar=1,fps=30,'
                         f'tpad=stop_mode=clone:stop_duration={max(0,length-span)+.04},trim=duration={length}[v{i}]')
        video += [f'[v0][v1]hstack=inputs=2,pad=1920:660:0:52:color=0x10151c,drawbox=x=959:y=52:w=2:h=540:color=0x82949e:t=fill,ass={name}.ass[v]',
                  f'[1:a]atrim=start={starts[1]}:end={ends[1]},asetpts=PTS-STARTPTS,apad,atrim=duration={length}[a]']
        call(['ffmpeg','-hide_banner','-y','-i',str(paths[0]),'-i',str(paths[1]),'-filter_complex',';'.join(video),'-map','[v]','-map','[a]',
              '-c:v','libx264','-crf','18','-preset','fast','-threads','2','-c:a','aac','-b:a','192k','-ar','48000','-ac','2',str(edit / (name+'.mp4'))], edit / (name+'.log'))
        manifest.append(entry)
        print('ENCODED', index, scene, step, flush=True)

(edit / 'concat.txt').write_text(''.join(f"file '{e['index']:02}.mp4'\n" for e in manifest), encoding='utf-8')
offset = 0
metadata = ';FFMETADATA1\ntitle=Karen 特效审查调整 · 修改前后对比\n'
for entry in manifest:
    entry['start'] = offset
    offset += float(probe(edit / f"{entry['index']:02}.mp4")['format']['duration'])
    metadata += f"\n[CHAPTER]\nTIMEBASE=1/1000\nSTART={round(entry['start']*1000)}\nEND={round(offset*1000)}\ntitle={entry['index']:02} {entry['title']}\n"
(edit / 'chapters.ffmeta').write_text(metadata, encoding='utf-8')
# Calibrate the single game-process soundtrack; do not mix both sides or add sounds.
call(['ffmpeg','-hide_banner','-f','concat','-safe','0','-i',str(edit/'concat.txt'),'-vn','-af','volumedetect','-f','null','NUL'], edit/'audio.log')
import re
peak = float(re.search(r'max_volume: (-?[\d.]+) dB', (edit/'audio.log').read_text(encoding='utf8')).group(1))
gain = max(0, -4-peak)
target = delivery / ('Karen-Geer-speed-before-after.mp4' if args.geer_speed else 'Karen-Jealousy-TV-Mahiru-before-after.mp4' if args.mahiru else 'Karen-WakeUp-two-stage-before-after.mp4' if args.wake_upgraded else 'Karen-VFX-before-after.mp4')
call(['ffmpeg','-hide_banner','-y','-f','concat','-safe','0','-i',str(edit/'concat.txt'),'-i',str(edit/'chapters.ffmeta'),'-map_metadata','1','-map_chapters','1',
      '-c:v','copy','-c:a','aac','-b:a','192k','-af',f'volume={gain}dB','-movflags','+faststart',str(target)], edit/'concat.log')
(delivery/'manifest.json').write_text(json.dumps(dict(method='Action-marker alignment; original speed; shorter side holds its last frame. Right game-process audio only. No added effects.',
    audio=dict(sourcePeakDb=peak,gainDb=gain), videoSha256=sha(target), entries=manifest),ensure_ascii=False,indent=2),encoding='utf8')
print('DELIVERED',target,'seconds',offset,flush=True)
