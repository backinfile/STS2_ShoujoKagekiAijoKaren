"""Package accepted real-time footage without cropping or changing action speed."""
import argparse, pathlib, json, subprocess, hashlib, re
parser=argparse.ArgumentParser();parser.add_argument("--motion",action="store_true");args=parser.parse_args()
tag="motion" if args.motion else "lights"
from PIL import Image, ImageDraw, ImageFont
main=pathlib.Path(__file__).resolve().parents[3]
root=main/'artifacts/last-word-stage-lights'
out=main/'docs/worktree-tasks/last-word-stage-lights-20261019'
scratch=root/('delivery-motion' if args.motion else 'delivery');scratch.mkdir(exist_ok=True)
font='C:/Windows/Fonts/msyh.ttc'
def probe(p):
 return json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration:stream=codec_type,width,height,r_frame_rate','-of','json',str(p)]))
def run(args):subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y',*map(str,args)],check=True)
def strip(path,width,height,lines):
 im=Image.new('RGB',(width,height),(18,21,29));d=ImageDraw.Draw(im)
 for x,y,text,size,color in lines:d.text((x,y),text,font=ImageFont.truetype(font,size),fill=color)
 im.save(path)
before=root/('motion-before/stable/lastword-motion-before/ready-recording/game.mp4' if args.motion else 'before/stable/lastword-stage-before/ready-recording/game.mp4')
after=root/'stable/lastword-stage/ready-recording/game.mp4'
movie=root/'stable/lastword-stage/game.mp4'
duration=min(float(probe(before)['format']['duration']),float(probe(after)['format']['duration']))
strip(scratch/'compare-header.png',1920,64,[(25,14,'上一版｜轻微摆动，无独立 hover 反馈' if args.motion else '修改前｜卡牌周围 T 粒子',26,'#d6d9e3'),(985,14,'本次｜常态扫光 + hover 聚光 + T 字跟随' if args.motion else '方案 1｜原有 T 字 + 粉白玫红追光',26,'#ffdce8')])
strip(scratch/'compare-footer.png',1920,64,[(25,15,'1× 游戏实录 · 相同操作：常态停留 → hover → 移开 → 连续 hover；保留粉白/玫红与原 T 字' if args.motion else '1× 游戏实录 · 相同操作：条件满足 → 追光亮起 → 悬停放大 → 持续提示',23,'#d6d9e3')])
compare=out/('LastWord-stage-'+tag+'-before-after.mp4')
run(['-i',before,'-i',after,'-loop','1','-framerate','30','-i',scratch/'compare-header.png','-loop','1','-framerate','30','-i',scratch/'compare-footer.png',
 '-filter_complex','[0:v]scale=960:540,setsar=1,setpts=PTS-STARTPTS[a];[1:v]scale=960:540,setsar=1,setpts=PTS-STARTPTS[b];[a][b]hstack=inputs=2[m];[2:v][m][3:v]vstack=inputs=3,format=yuv420p[v]',
 '-map','[v]','-map','1:a:0','-t',str(duration),'-r','30','-c:v','libx264','-crf','19','-preset','medium','-threads','4','-c:a','aac','-b:a','160k','-movflags','+faststart',compare])
segments=[]
for index,(source,title) in enumerate([(after,'最后的台词｜常态错相扫光、呼吸与光尘；hover 平滑收拢，移开恢复' if args.motion else '最后的台词 · 方案 1｜条件满足后，原有 T 字与粉白玫红追光同时显示'),(movie,'真实出牌｜追光收回，原有终幕影片与伤害结算保留')]):
 strip(scratch/('footer%d.png'%index),1280,64,[(20,14,title,21,'#ffdeea')])
 dest=scratch/('segment%d.mp4'%index)
 run(['-i',source,'-loop','1','-framerate','30','-i',scratch/('footer%d.png'%index),'-filter_complex','[0:v][1:v]vstack=inputs=2,format=yuv420p[v]',
 '-map','[v]','-map','0:a:0','-t',probe(source)['format']['duration'],'-r','30','-c:v','libx264','-crf','19','-preset','medium','-threads','4','-c:a','aac','-b:a','160k',dest])
 segments.append(dest)
concat=scratch/'concat.txt';concat.write_text(''.join("file '"+str(p).replace('\\','/')+"'\n" for p in segments),encoding='utf-8')
showcase=out/('LastWord-stage-'+tag+'-showcase.mp4')
run(['-f','concat','-safe','0','-i',concat,'-c','copy','-movflags','+faststart',showcase])
for video in [compare,showcase]:
 run(['-i',video,'-f','null','-'])
run(['-ss','5.5','-i',after,'-frames:v','1',out/('motion-in-game.png' if args.motion else 'stage-lights-in-game.png')])
metadata={p.name:{'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'probe':probe(p)} for p in [compare,showcase]}
for video in [compare,showcase]:
 audio=subprocess.run(['ffmpeg','-hide_banner','-i',str(video),'-vn','-af','volumedetect','-f','null','-'],capture_output=True,text=True,check=True)
 values={key:float(value) for key,value in re.findall(r'(mean_volume|max_volume): (-?[0-9.]+) dB',audio.stderr)}
 assert values.get('max_volume',-999)>-80,(video,values)
 freeze=subprocess.run(['ffmpeg','-hide_banner','-i',str(video),'-an','-vf','freezedetect=n=-50dB:d=1','-f','null','-'],capture_output=True,text=True,check=True)
 starts=[float(v) for v in re.findall(r'freeze_start: ([0-9.]+)',freeze.stderr)]
 seconds=float(probe(video)['format']['duration'])
 assert not any(t<seconds-2.0 for t in starts),(video,starts)
 metadata[video.name]['audio']=values
 metadata[video.name]['freezeStarts']=starts
metadata['comparison']={'speed':1,'alignment':'same action sequence, original timestamps; trim longer tail only','duration':duration,'audio':'after game process'}
(out/('motion-videos.json' if args.motion else 'videos.json')).write_text(json.dumps(metadata,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(metadata,ensure_ascii=False,indent=2))
