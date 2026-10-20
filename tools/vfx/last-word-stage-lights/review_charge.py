
"""Review actual MCP hover distribution and post-film T afterglow; package original-speed footage."""
from pathlib import Path
import hashlib,json,math,re,subprocess,sys
uniform="--uniform" in sys.argv
regular="--regular" in sys.argv or uniform
tag="uniform" if uniform else ("regular" if regular else "charge")
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[3]
art=root/'artifacts/last-word-stage-lights';out=root/'docs/worktree-tasks/last-word-stage-lights-20261019'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def diagnostics(path):
 lines=path.read_text(encoding='utf-8',errors='replace').splitlines()
 common=next((i for i,line in enumerate(lines) if "Preloading 'Common' Complete:" in line),-1)
 values=[{'line':i+1,'text':line} for i,line in enumerate(lines) if re.search(r'\bERROR\b|Exception',line)]
 assert all(v['text']=='ERROR: Invalid Task ID' and v['line']<=common for v in values),values
 return values
summary={'before':{},'branches':{},'unchanged':{}}
pictures=[]
before=art/('uniform-prior/stable/lastword-regular' if uniform else ('regular-prior/stable/lastword-charge' if regular else 'charge-before/stable/lastword-charge-before'))
def measure(case,changed,normal_hover=False):
 phases={name:read(case/(name+'-probe.json')) for name in ['blocked','idle','hover-first','hover-held','hover-screenshot','unhover-restored','unhover-screenshot','postplay']}
 cvs=[]
 for name,data in phases.items():
  count=0 if name in ['blocked','postplay'] else 1
  assert len(data['current']['nodes'])==count and len(data['current']['tNodes'])==count,(name,data['current'])
  if count:
   t=data['current']['tNodes'][0]
   assert t['anchorError']<1 and t['basisError']<.02,(name,t)
  if name.startswith('hover'):
   t=data['current']['tNodes'][0];cvs.append(t['distributionCv'])
   if normal_hover:
    assert t['charge']==0 and data['current']['nodes'][0]['hover']>.99,(name,t)
   elif changed:assert t['charge']>.99 and min(t['bins'])>0 and t['distributionCv']<.45,(name,t)
  if changed and name.startswith('unhover'):assert t['charge']<.01,(name,t)
 d=phases['postplay'];assert not d['current']['attack'] and not d['current']['videos']
 samples=d['samples']
 active=[(v['time'],n,v['current']['combatInProgress']) for v in samples for n in v['current']['attack']]
 assert active,'No actual attack VFX was observed'
 start=active[0][0]
 visible=[t-start for t,n,_ in active if n['visible']>0]
 tail=[(t,n,combat) for t,n,combat in active if n['afterglowAge']>=0]
 result={'hoverDistributionCv':cvs,'meanHoverDistributionCv':sum(cvs)/len(cvs),
  'lastVisibleAttackSeconds':max(visible),'afterglowObserved':bool(tail)}
 if changed:
  assert max(n['afterglowAge'] for _,n,_ in tail)>1.6
  late=[n for _,n,_ in tail if .95<n['afterglowAge']<1.4]
  assert late and max(n['visible'] for n in late)>5,late
  alphas=[n['alpha'] for _,n,_ in tail]
  assert all(a>=b-.001 for a,b in zip(alphas,alphas[1:])),alphas
  result['maxAfterglowAge']=max(n['afterglowAge'] for _,n,_ in tail)
  result['visibleLateAfterglow']=max(n['visible'] for n in late)
  result['tailSurvivedCombatEnd']=any(not combat and n['visible']>0 for _,n,combat in tail)
 idle_cvs=[phases[name]['current']['tNodes'][0]['distributionCv'] for name in ['idle','unhover-restored','unhover-screenshot']]
 result['idleDistributionCv']=idle_cvs
 result['meanIdleDistributionCv']=sum(idle_cvs)/len(idle_cvs)
 result['handChecks']=0
 for p in case.rglob('*-hand.json'):
  value=read(p)
  if 'message' not in value:continue
  h=json.loads(value['message'])
  assert value['status']=='ok' and h['modelCount']==h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'],(p,h)
  result['handChecks']+=1
 result['captures']=[]
 for p in [case/'ready-recording/recording.json',case/'recording.json']:
  v=read(p)
  assert v['status']=='completed' and v['frames_dropped']==0 and v['audio_source']=='game_process',v
  result['captures'].append({k:v.get(k) for k in ['capture_fps','frames_dropped','frames_repeated','video_duration_seconds','captureBuild']})
 return result
summary['before']=measure(before,regular,uniform)
for branch in ['stable','beta']:
 folder=art/branch;case=folder/('lastword-'+tag)
 result=measure(case,True,regular)
 if uniform:
  for key in ['meanIdleDistributionCv','meanHoverDistributionCv']:
   assert result[key]<.35 and result[key]<summary['before'][key]*.75,(branch,key,result[key],summary['before'][key])
 if not regular:assert result['lastVisibleAttackSeconds']>summary['before']['lastVisibleAttackSeconds']+.6,(branch,result,summary['before'])
 monitor=read(folder/'window-monitor.json');assert monitor['foreground_samples']==monitor['restored_samples']==0,monitor
 result['windowMonitor']=monitor
 result['diagnostics']=diagnostics(folder/'godot.log')
 audits=[read(p) for p in (folder/'capture-audit').glob('*.json')]
 audits=[a for a in audits if 'timelineDriftSeconds' in a]
 assert audits and all(abs(a['timelineDriftSeconds'])<.05 and a['longestUnchangedClockSeconds']<.2 for a in audits),audits
 result['captureAudit']=audits
 summary['branches'][branch]=result
 pictures += [(branch+'/'+p.stem,p) for p in sorted(case.glob('*.png'))]
pictures += [('before/'+p.stem,p) for p in sorted(before.glob('*.png'))]
for rel in [
 'src/Core/Models/Cards/neutral/KarenLastWord.cs',
 'src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenLastWordStageLightsVfx.cs',
 'materials/vfx/karen_last_word_stage_lights.gdshader',
 'ShoujoKagekiAijoKaren/video/last_word.ogv']:
 old=subprocess.check_output(['git','show','fa9a435:'+rel],cwd=root);new=(root/rel).read_bytes()
 if rel.endswith(('.cs','.gdshader')):old=old.replace(b'\r\n',b'\n');new=new.replace(b'\r\n',b'\n')
 assert old==new,rel
 summary['unchanged'][rel]=hashlib.sha256(new).hexdigest()
if regular:
 for revision,rel in [('fa9a435','src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenLastWordPlayableVfx.cs'),('b313907','src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenLastWordVfx.cs'),('b313907','src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenLastWordVideoVfx.cs')]:
  if uniform and rel.endswith('NKarenLastWordPlayableVfx.cs'):continue
  old=subprocess.check_output(['git','show',revision+':'+rel],cwd=root).replace(b'\r\n',b'\n');new=(root/rel).read_bytes().replace(b'\r\n',b'\n')
  assert old==new,rel
  summary['unchanged'][revision+':'+rel]=hashlib.sha256(new).hexdigest()
if uniform:
 rel='src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenLastWordPlayableVfx.cs'
 old=subprocess.check_output(['git','show','822395f:'+rel],cwd=root).decode('utf-8').replace('\r\n','\n');new=(root/rel).read_text(encoding='utf-8')
 def particle_class(code):
  start=code.index('    private partial class NKarenLastWordPlayableTParticle')
  return code[start:code.index('\n    }\n',start)+7]
 assert particle_class(old)==particle_class(new),'Outward drift, alpha, size, lifetime or rotation changed'
 summary['unchanged']['822395f:particleMotion']=hashlib.sha256(particle_class(new).encode('utf-8')).hexdigest()
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
for page in range((len(pictures)+11)//12):
 contact=Image.new('RGB',(1600,3*251),(18,22,29));draw=ImageDraw.Draw(contact)
 for j,(label,path) in enumerate(pictures[page*12:(page+1)*12]):
  im=Image.open(path).convert('RGB');im.thumbnail((392,220));x=(j%4)*400+4;y=(j//4)*251+25
  contact.paste(im,(x,y));draw.text((x,y-21),label,font=font,fill='white')
 contact.save(out/(tag+'-mcp-contact-%02d.jpg'%(page+1)),quality=90)
(out/(tag+'-verification.json')).write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({key:{k:v[k] for k in ['meanHoverDistributionCv','lastVisibleAttackSeconds','visibleLateAfterglow','tailSurvivedCombatEnd']} for key,v in summary['branches'].items()},indent=2),flush=True)
# Both game processes have exited before ffmpeg packaging.
scratch=art/('delivery-'+tag);scratch.mkdir(exist_ok=True)
def probe(p):return json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration:stream=codec_type,width,height,r_frame_rate','-of','json',str(p)]))
def run(args):subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y',*map(str,args)],check=True)
def strip(path,width,height,text,size=24):
 im=Image.new('RGB',(width,height),(18,21,29));d=ImageDraw.Draw(im)
 d.text((20,14),text,font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',size),fill='#ffdeea');im.save(path)
case=art/('stable/lastword-'+tag);outputs=[];lengths={}
video_prefix='LastWord-T-'+(tag+'-' if regular else '')
for name,source,title in [('hover','ready-recording/game.mp4','Hover T 字｜恢复普通状态的向外飘散' if regular else 'Hover T 字｜均匀贴边、轻微内聚与呼吸蓄力'),('play','game.mp4','打出 T 字｜影片结束后减速停留、柔和淡出；伤害节奏保留')]:
 if uniform and name=='play':continue
 if uniform:title='T 字分布｜沿牌边均匀生成，保留普通向外飘散'
 old=before/source;new=case/source
 seconds=min(float(probe(old)['format']['duration']),float(probe(new)['format']['duration']))
 # This review sequence reaches rewards around 11.2s; retain the complete T tail
 # and rewards transition, omitting the extra static rewards hold.
 if regular and name=='play':seconds=min(seconds,11.9)
 strip(scratch/(name+'-header.png'),1920,64,'上一版｜随机分布' if uniform else ('上一版｜hover 蓄力' if regular and name=='hover' else '上一版'),26)
 header=Image.open(scratch/(name+'-header.png'));ImageDraw.Draw(header).text((980,14),title,font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',25),fill='#ffdeea');header.save(scratch/(name+'-header.png'))
 strip(scratch/(name+'-footer.png'),1920,64,'1× 游戏实录 · 同一组原生操作 · 原 T 字、粉白/玫红灯光与影片保留',23)
 dest=out/(video_prefix+name+'-before-after.mp4')
 run(['-i',old,'-i',new,'-loop','1','-framerate','30','-i',scratch/(name+'-header.png'),'-loop','1','-framerate','30','-i',scratch/(name+'-footer.png'),
 '-filter_complex','[0:v]scale=960:540,setsar=1,setpts=PTS-STARTPTS[a];[1:v]scale=960:540,setsar=1,setpts=PTS-STARTPTS[b];[a][b]hstack[m];[2:v][m][3:v]vstack=inputs=3,format=yuv420p[v]',
 '-map','[v]','-map','1:a:0','-t',seconds,'-r','30','-c:v','libx264','-crf','19','-preset','medium','-threads','4','-c:a','aac','-b:a','160k','-movflags','+faststart',dest])
 outputs.append(dest);lengths[name]=seconds
if not uniform:
 concat=scratch/'concat.txt';concat.write_text(''.join("file '"+str(p).replace('\\','/')+"'\n" for p in outputs),encoding='utf-8')
 review=out/(video_prefix+'review.mp4');run(['-f','concat','-safe','0','-i',concat,'-c','copy','-movflags','+faststart',review]);outputs.append(review)
 strip(scratch/'play-after-footer.png',1280,64,'最后的台词｜打出后的 T 字减速留存，再自然淡出',21)
 solo=out/(video_prefix+'play-after.mp4')
 run(['-i',case/'game.mp4','-loop','1','-framerate','30','-i',scratch/'play-after-footer.png','-filter_complex','[0:v][1:v]vstack=inputs=2,format=yuv420p[v]',
  '-map','[v]','-map','0:a:0','-t',min(float(probe(case/'game.mp4')['format']['duration']),11.9) if regular else probe(case/'game.mp4')['format']['duration'],'-r','30','-c:v','libx264','-crf','19','-preset','medium','-threads','4','-c:a','aac','-b:a','160k','-movflags','+faststart',solo]);outputs.append(solo)
metadata={'comparisonDurations':lengths,'videos':{}}
for p in outputs:
 run(['-i',p,'-f','null','-'])
 a=subprocess.run(['ffmpeg','-hide_banner','-i',str(p),'-vn','-af','volumedetect','-f','null','-'],capture_output=True,text=True,check=True)
 volumes={k:float(v) for k,v in re.findall(r'(mean_volume|max_volume): (-?[0-9.]+) dB',a.stderr)};assert volumes.get('max_volume',-999)>-80,(p,volumes)
 freeze=subprocess.run(['ffmpeg','-hide_banner','-i',str(p),'-an','-vf','freezedetect=n=-50dB:d=1','-f','null','-'],capture_output=True,text=True,check=True)
 starts=[float(v) for v in re.findall(r'freeze_start: ([0-9.]+)',freeze.stderr)]
 info=probe(p);seconds=float(info['format']['duration'])
 assert not any(t<seconds-2 for t in starts),(p,starts)
 metadata['videos'][p.name]={'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'probe':info,'audio':volumes,'freezeStarts':starts}
(out/(tag+'-videos.json')).write_text(json.dumps(metadata,indent=2),encoding='utf-8')
print('VIDEO PACKAGING PASSED',lengths,flush=True)

