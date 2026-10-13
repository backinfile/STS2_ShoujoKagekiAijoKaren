"""Caption and concatenate the seven native MCP recordings; keep every source."""
import json
import re
import shutil
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[2]
output = root / 'artifacts/relic-video'
captioned = output / 'captioned'
captioned.mkdir(exist_ok=True)
ffmpeg = shutil.which('ffmpeg')
ffprobe = shutil.which('ffprobe')
assert ffmpeg and ffprobe, 'FFmpeg and FFprobe are required'
segments = [
    ('01-cookie','饼干','罕见','拾起时，随机获得普通、罕见、稀有闪耀牌各1张。','karen_cookie_relic'),
    ('02-hairclip','星形发饰','罕见','战斗开始时，将1张未升级《光辉》加入约定牌堆。发饰已换用二维稿。','karen_star_hairclip_relic'),
    ('03-water','华恋的水壶','普通','第一次获得临时力量时保留力量2回合；本段跨回合仍保有临时力量。','karen_water_bottle_relic'),
    ('04-star','小小星','稀有','打出《约定之塔》时额外获得8点格挡；额外重播也会逐次触发。','karen_little_star_relic'),
    ('05-white','Mr. White','稀有','第一回合结束可选择0到全部手牌放入约定牌堆；本段示范选择2张。','karen_mr_white_relic'),
    ('06-cat','棒球猫','稀有','闪耀耗尽后追加随机闪耀牌奖励，同名每战计1次；终结一击也会触发。','karen_baseball_cat_relic'),
    ('07-tiara','星之王冠','商店','回合开始先禁用再+1能量；示范禁用草莓、禁用自身，以及后续停止触发。','karen_star_tiara_relic'),
]

def run(args, capture=False):
    return subprocess.run(args, cwd=output, check=True, encoding='utf-8', errors='replace',
                          stdout=subprocess.PIPE if capture else subprocess.DEVNULL,
                          stderr=subprocess.PIPE if capture else None)
def probe(path):
    return json.loads(run([ffprobe,'-v','error','-show_format','-show_streams','-show_chapters','-of','json',str(path)], True).stdout)
def ass_time(seconds):
    c = round(seconds * 100)
    return f'{c//360000}:{c//6000%60:02d}:{c//100%60:02d}.{c%100:02d}'

duration = 0
chapters = [';FFMETADATA1','title=Karen 七件遗物触发实录','comment=Native KarenSTS2MCP recordings from isolated stable v0.107.1; game-process audio only.']
concat = []
manifest = []
for number, (stem,title,rarity,body,icon) in enumerate(segments,1):
    source = output / (stem + '.mp4')
    info = probe(source)
    seconds = float(info['format']['duration'])
    assert any(x['codec_type']=='audio' for x in info['streams']), source
    meta = json.loads((output / (stem + '-recording.json')).read_text(encoding='utf-8'))
    # Older recordings retain their original captions; never relabel historical gameplay.
    if meta.get('balance_revision') == '2026-10-12':
        if stem == '03-water': body = '第一次获得临时力量时保留力量1回合；本段跨回合仍保有临时力量。'
        if stem == '04-star': body = '打出《约定之塔》时获得5点临时力量；额外重播也会逐次触发。'
    assert meta['audio_source']=='game_process' and meta['status']=='completed'
    ass = f'''[Script Info]
ScriptType: v4.00+
PlayResX: 1280
PlayResY: 800
WrapStyle: 2

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Title,Microsoft YaHei,25,&H00FFFFFF,&H00FFFFFF,&H00000000,&H00000000,-1,0,0,0,100,100,0,0,1,0,0,1,110,20,46,1
Style: Body,Microsoft YaHei,20,&H00E6E6E6,&H00FFFFFF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,1,110,16,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
Dialogue: 0,0:00:00.00,{ass_time(seconds)},Title,,0,0,0,,{number:02d}  {title} · {rarity}
Dialogue: 0,0:00:00.00,{ass_time(seconds)},Body,,0,0,0,,{body}
'''
    (output / (stem + '.ass')).write_text(ass,encoding='utf-8-sig')
    destination = captioned / (stem + '.mp4')
    run([ffmpeg,'-hide_banner','-loglevel','error','-y','-i',str(source),'-i',str(root/'images/relics'/(icon+'.png')),
         '-filter_complex',f'[0:v]pad=1280:800:0:0:color=0x141416[v];[1:v]scale=68:68[i];[v][i]overlay=26:726,ass={stem}.ass[out]',
         '-map','[out]','-map','0:a:0','-c:v','libx264','-preset','fast','-crf','20','-pix_fmt','yuv420p',
         '-c:a','aac','-b:a','160k','-ar','48000','-ac','2','-movflags','+faststart',str(destination)])
    actual = float(probe(destination)['format']['duration'])
    chapters += ['[CHAPTER]','TIMEBASE=1/1000',f'START={round(duration*1000)}',f'END={round((duration+actual)*1000)}',f'title={title}']
    manifest.append({'segment':stem,'title':title,'start':duration,'duration':actual,'source_recording':meta,'source_probe':info})
    duration += actual
    concat.append(f"file 'captioned/{stem}.mp4'")
    print(f'Captioned {stem}: {actual:.2f}s',flush=True)

(output/'concat.txt').write_text('\n'.join(concat)+'\n',encoding='utf-8')
(output/'chapters.ffmeta').write_text('\n'.join(chapters)+'\n',encoding='utf-8')
final = output/'Karen-seven-relics-effects.mp4'
run([ffmpeg,'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i','concat.txt',
     '-i','chapters.ffmeta','-map','0','-map_metadata','1','-map_chapters','1','-c','copy','-movflags','+faststart',str(final)])
info = probe(final)
assert len(info['chapters'])==7 and any(x['codec_type']=='audio' for x in info['streams'])
audio = run([ffmpeg,'-hide_banner','-i',str(final),'-vn','-af','volumedetect','-f','null','NUL'],True).stderr
match = re.search(r'max_volume: ([-\w.]+) dB',audio)
assert match and match[1] != '-inf', 'The final game audio is silent'
(output/'verification.json').write_text(json.dumps({'final':str(final),'segments':manifest,'probe':info,'audio_max_db':float(match[1])},ensure_ascii=False,indent=2),encoding='utf-8')
print(f'Final video: {final} ({duration:.1f}s, seven chapters, audio peak {match[1]} dB)',flush=True)
