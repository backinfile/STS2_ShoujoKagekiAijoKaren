"""Package original-speed game-process recordings with captions outside the game viewport."""
import argparse, json, subprocess
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--run',default='checks');args=parser.parse_args()
root=Path(__file__).resolve().parents[3];capture=root/'artifacts/aquarium';edit=capture/('edit-'+args.run);edit.mkdir(exist_ok=True)
delivery=root/'docs/worktree-tasks/aquarium-vfx-20261017';clips=[('stable','showcase','正式版 · 水族馆施放、常驻与真实回合抽选牌'),('stable','stack','正式版 · 两层水族馆 · 抽 2 张后选 2 张入塔'),('stable','void','正式版 · 空虚模式 · 选牌进入实际抽牌堆'),('beta','showcase','测试版 · 相同施放与回合触发表现')]
if args.run.startswith('transient'):
    clips=[('stable','showcase','正式版 · 施放与回合触发短暂播放 · 等待时无特效'),('beta','showcase','测试版 · 相同短暂特效 · 播放后完全清理')]
chapters=[];clock=0
for i,(branch,name,title) in enumerate(clips):
    src=capture/branch/args.run/name/'game.mp4';assert src.is_file(),src
    subtitle=edit/f'{i}.ass';subtitle.write_text(f'''[Script Info]
ScriptType: v4.00+
PlayResX: 1280
PlayResY: 788
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Default,Microsoft YaHei,25,&H00EAEFFF,&H00EAEFFF,&H00151015,&H00151015,0,0,0,0,100,100,0,0,1,0,0,2,24,24,22,1
[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
Dialogue: 0,0:00:00.00,0:01:00.00,Default,,0,0,0,,{title}
''',encoding='utf-8-sig')
    dst=edit/f'{i}.mp4';subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-i',str(src),'-vf',f'pad=1280:788:0:0:color=0x151015,subtitles={subtitle.name},format=yuv420p','-c:v','libx264','-crf','19','-preset','fast','-c:a','aac','-b:a','160k',str(dst)],cwd=edit,check=True)
    duration=float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','csv=p=0',str(dst)]));chapters.append(dict(start=clock,duration=duration,title=title));clock+=duration
(edit/'concat.txt').write_text(''.join(f"file '{i}.mp4'\n" for i in range(len(clips))),encoding='utf-8')
subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(edit/'concat.txt'),'-c','copy','-movflags','+faststart',str(delivery/('aquarium.mp4' if args.run=='checks' else 'aquarium-'+args.run+'.mp4'))],check=True)
(delivery/('chapters.json' if args.run=='checks' else 'chapters-'+args.run+'.json')).write_text(json.dumps(chapters,ensure_ascii=False,indent=2),encoding='utf-8');print(f'Delivered {clock:.2f}s')
