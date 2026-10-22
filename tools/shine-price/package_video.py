"""Add labels outside the viewport, concatenate native-speed captures, verify media."""
from pathlib import Path
import argparse,hashlib,json,re,shutil,subprocess
from PIL import Image,ImageDraw
parser=argparse.ArgumentParser();parser.add_argument('--capture',default='artifacts/shine-price-recording/capture');parser.add_argument('--name',default='Twin-Stars-showcase.mp4');parser.add_argument('--evidence',default='evidence/twin-stars');parser.add_argument('--individual',action='store_true');args=parser.parse_args()
root=Path(__file__).resolve().parents[2];capture=root/args.capture;edit=root/'artifacts/shine-price-recording/edit';edit.mkdir(parents=True,exist_ok=True)
delivery=root/'docs/worktree-tasks/karen-x-relic-barrage-20261022';video=delivery/args.name;evidence=delivery/args.evidence;evidence.mkdir(parents=True,exist_ok=True)
clips=json.loads((capture/'clips.json').read_text(encoding='utf-8'));report={'clips':[],'processing':'Native game speed; no gameplay compositing. Labels are added below the intact viewport.','mcp_source_commit':'b8535120fbcdfb4fe2bf1ae915a410d611ee4067'}
contact=Image.new('RGB',(4*480,len(clips)*292),'#11171d');draw=ImageDraw.Draw(contact)
for index,clip in enumerate(clips):
 mode=clip['mode'];source=capture/mode/'game.mp4';meta=json.loads((capture/mode/'recording.json').read_text(encoding='utf-8'))
 assert meta['status']=='completed' and meta['audio'] and meta['audio_source']=='engine_mix',meta
 assert meta['capture_fps']>24 and meta['frames_repeated']/meta['frames_written']<.15,meta
 media=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_streams','-show_format','-of','json',str(source)]));assert any(s['codec_type']=='audio' for s in media['streams'])
 audio=subprocess.run(['ffmpeg','-hide_banner','-i',str(source),'-af','volumedetect','-vn','-sn','-f','null','NUL'],capture_output=True,text=True).stderr
 mean=float(re.search(r'mean_volume: ([\-\d.]+)',audio)[1]);peak=float(re.search(r'max_volume: ([\-\d.]+)',audio)[1]);assert mean>-65 and peak<=0,(mode,mean,peak)
 duration=float(media['format']['duration']);label='普通版' if mode=='normal' else '升级版'
 title=clip.get('caption') or f'双星  ·  {label}  ·  X = 3  ·  禁用 {clip["relics_disabled"]} 个遗物  ·  {clip["per_hit"]} 点伤害 × 6'
 detail=clip.get('detail') or '正式版 v0.107.1 · 正常速度 · 实机录制 · 保留游戏声音'
 subtitle=edit/f'{mode}.ass';subtitle.write_text(f'''[Script Info]
ScriptType: v4.00+
PlayResX: 1280
PlayResY: 800
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Main,Microsoft YaHei,25,&H00F5F1EC,&H00F5F1EC,&H001D1711,&H001D1711,0,0,0,0,100,100,0,0,1,0,0,2,20,20,42,1
Style: Sub,Microsoft YaHei,17,&H00C1BBB4,&H00C1BBB4,&H001D1711,&H001D1711,0,0,0,0,100,100,0,0,1,0,0,2,20,20,14,1
[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
Dialogue: 0,0:00:00.00,0:01:00.00,Main,,0,0,0,,{title}
Dialogue: 0,0:00:00.00,0:01:00.00,Sub,,0,0,0,,{detail}
''',encoding='utf-8-sig')
 target=edit/f'{mode}.mp4';subprocess.run(['ffmpeg','-v','error','-y','-i',str(source),'-vf',f'pad=1280:800:0:0:color=0x11171d,subtitles={subtitle.name},format=yuv420p','-c:v','libx264','-preset','fast','-crf','18','-c:a','aac','-b:a','160k',str(target)],cwd=edit,check=True)
 for j,moment in enumerate([1,clip['play_at']+.4,(clip['play_at']+clip['resolved_at'])/2,clip['resolved_at']+.7]):
  frame=edit/f'{mode}-{j}.png';subprocess.run(['ffmpeg','-v','error','-y','-ss',str(min(moment,duration-.2)),'-i',str(target),'-frames:v','1',str(frame)],check=True)
  im=Image.open(frame);im.thumbnail((480,270));x,y=j*480,index*292;contact.paste(im,(x,y+22));draw.text((x+8,y+4),f'{mode} {moment:.2f}s',fill='white')
 # Compare decoded consecutive game-view frames, not only container fps or recorder counters.
 hashes=subprocess.check_output(['ffmpeg','-v','error','-i',str(source),'-an','-vf','scale=160:90','-f','framemd5','-'],text=True)
 rows=[line.split(',')[-1].strip() for line in hashes.splitlines() if line and not line.startswith('#')]
 longest=run=1
 for k in range(1,len(rows)):
  run=run+1 if rows[k]==rows[k-1] else 1;longest=max(longest,run)
 assert len(set(rows))>30 and longest<30,(mode,len(set(rows)),longest)
 # Also deliver each captured case separately with the same complete viewport and labels.
 individual=delivery/f'{video.stem}-{mode}.mp4' if args.individual else None
 if individual:shutil.copy2(target,individual)
 report['clips'].append({**clip,'individual_output':individual.relative_to(root).as_posix() if individual else None,'individual_sha256':hashlib.sha256(individual.read_bytes()).hexdigest() if individual else None,'duration':duration,'audio_mean_db':mean,'audio_peak_db':peak,'longest_identical_frame_run':longest,'unique_lowres_frames':len(set(rows)),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest()})
(edit/'concat.txt').write_text(''.join(f"file '{clip['mode']}.mp4'\n" for clip in clips),encoding='utf-8')
subprocess.run(['ffmpeg','-v','error','-y','-f','concat','-safe','0','-i',str(edit/'concat.txt'),'-c','copy','-movflags','+faststart',str(video)],check=True)
subprocess.run(['ffmpeg','-v','error','-i',str(video),'-f','null','NUL'],check=True)
report['output']=video.relative_to(root).as_posix();report['output_sha256']=hashlib.sha256(video.read_bytes()).hexdigest();report['duration']=float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','csv=p=0',str(video)]));report['source_hashes']=json.loads((capture/'source-hashes.json').read_text(encoding='utf-8'))
(evidence/'recording-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');contact.save(evidence/'recording-review.jpg',quality=90)
print(json.dumps(report,ensure_ascii=False,indent=2))
