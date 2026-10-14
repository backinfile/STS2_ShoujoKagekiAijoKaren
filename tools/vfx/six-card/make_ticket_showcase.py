"""Cut actual Ticket activation/delivery recordings with the original card and power art beside the player."""
import argparse,hashlib,html,json,pathlib,re,shutil,subprocess
p=argparse.ArgumentParser();p.add_argument('--solo',type=pathlib.Path,required=True);p.add_argument('--output',type=pathlib.Path,required=True)
args=p.parse_args();solo=args.solo.resolve();out=args.output.resolve();out.mkdir(parents=True,exist_ok=True)
render=out/'render';render.mkdir(exist_ok=True);repo=pathlib.Path(__file__).resolve().parents[3]
def run(args,**kw):return subprocess.run(args,check=True,encoding='utf-8',errors='replace',**kw)
def info(path):return json.loads(subprocess.check_output(['ffprobe','-v','error','-show_format','-show_streams','-of','json',str(path)],text=True,encoding='utf-8'))
def duration(path):return float(info(path)['format']['duration'])
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
chapters=json.loads((solo/'chapters.json').read_text(encoding='utf-8'));source=solo/'game.mp4'
# Saved screenshot times use the same clock as the last saved chapter. Keep each
# actual effect and its resolved hand view, trimming fixture preparation and enemy waits.
epoch=(solo/'chapters.json').stat().st_mtime-chapters[-1]['time']
segments=[]
for number in [0,1,3]:
 c=chapters[number];during=(solo/(c['label']+'-during.png')).stat().st_mtime-epoch
 after=(solo/(c['label']+'-after.png')).stat().st_mtime-epoch
 start=max(c['time'],during-(.85 if number==0 else .55))
 end=min(duration(source),after+.1)
 assert start<end
 segments.append((c['label'],start,end))
descriptions=['珍珠白光瓣收拢 · 淡蓝亮核 · 实际门票能力生效','门票 1 层 → 实际生成1张约定之塔进入手牌','门票 2 层 → 一次主体演出，实际生成2张约定之塔']
manifest={'sources':[{'file':'source-stable.mp4','sha256':sha(source)}],'chapters':[],
          'edits':'Trim fixture/enemy waits using saved screenshot timing, concatenate, +12 dB playback gain and a 96px title band outside the original viewport. No post-production VFX.'}
shutil.copy2(source,out/'source-stable.mp4')
for kind in ['build','recording']:shutil.copy2(solo/(kind+'.json'),out/('source-stable-'+kind+'.json'))
cursor=0;clips=[]
for i,(title,start,end) in enumerate(segments):
 (render/f'title-{i}.txt').write_text(f'{i+1:02d}  {title}',encoding='utf-8')
 (render/f'sub-{i}.txt').write_text(descriptions[i]+'   |   游戏实录 · 原速',encoding='utf-8')
 vf=("pad=1280:816:0:96:color=0x101b29,"+f"drawtext=fontfile='C\\:/Windows/Fonts/msyh.ttc':textfile=title-{i}.txt:fontcolor=0xf2f0e8:fontsize=30:x=26:y=12,"+
     f"drawtext=fontfile='C\\:/Windows/Fonts/msyh.ttc':textfile=sub-{i}.txt:fontcolor=0xbfd2f5:fontsize=17:x=28:y=59")
 clip=out/f'{i+1:02d}.mp4'
 run(['ffmpeg','-hide_banner','-loglevel','error','-y','-ss',str(start),'-i',str(source),'-t',str(end-start),'-vf',vf,
      '-r','30','-c:v','libx264','-threads','2','-preset','fast','-crf','18','-pix_fmt','yuv420p','-af','volume=12dB',
      '-c:a','aac','-ar','48000','-ac','2','-b:a','160k','-movflags','+faststart',str(clip)],cwd=render)
 length=duration(clip);manifest['chapters'].append({'title':title,'description':descriptions[i],'start':cursor,'duration':length,'sourceIn':start,'sourceOut':end,'file':clip.name})
 cursor+=length;clips.append(clip)
concat=render/'concat.txt';concat.write_text(''.join(f"file '{c.as_posix()}'\n" for c in clips),encoding='utf-8')
video=out/'ticket-showcase.mp4';run(['ffmpeg','-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(concat),'-c','copy','-movflags','+faststart',str(video)])
run(['ffmpeg','-hide_banner','-loglevel','error','-y','-ss','1.3','-i',str(video),'-frames:v','1',str(out/'poster.png')])
qa={'metadata':info(video),'motion':[]}
for c in clips:
 output=subprocess.check_output(['ffmpeg','-hide_banner','-loglevel','error','-i',str(c),'-vf','crop=1280:720:0:96,fps=4','-an','-f','framemd5','-'],text=True,encoding='utf-8')
 frames=[x.rsplit(',',1)[-1].strip() for x in output.splitlines() if not x.startswith('#')]
 assert len(set(frames))>=4;qa['motion'].append({'file':c.name,'frames':len(frames),'uniqueFrames':len(set(frames))})
audio=run(['ffmpeg','-hide_banner','-i',str(video),'-af','volumedetect','-vn','-f','null','-'],capture_output=True,text=True)
qa['audioMeanDb']=float(re.search(r'mean_volume: ([\d.\-]+) dB',audio.stderr).group(1));qa['audioPeakDb']=float(re.search(r'max_volume: ([\d.\-]+) dB',audio.stderr).group(1));assert qa['audioMeanDb']>-65
freeze=run(['ffmpeg','-hide_banner','-i',str(video),'-vf','freezedetect=n=-50dB:d=1','-an','-f','null','-'],capture_output=True,text=True)
qa['freezeDiagnostics']=[x for x in freeze.stderr.splitlines() if 'freeze_' in x]
for name,value in [('manifest',manifest),('qa',qa)]: (out/(name+'.json')).write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(out/'reference').mkdir(exist_ok=True)
shutil.copy2(repo/'images/packed/card_portraits/karen/karen_ticket.png',out/'reference/card.png')
shutil.copy2(repo/'images/powers/karen_ticket_power.png',out/'reference/buff.png')
buttons=''.join(f'<button data-time="{c["start"]}">{i+1:02d}　{html.escape(c["title"])}</button>' for i,c in enumerate(manifest['chapters']))
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>门票 · 珍珠白与淡蓝</title><style>
*{box-sizing:border-box}body{margin:0;background:#101b29;color:#f2f0e8;font:16px/1.65 system-ui,"Microsoft YaHei",sans-serif}main{max-width:1240px;margin:auto;padding:28px 24px}h1{font-size:27px;margin:0 0 10px}p{color:#b5c4d8;margin:0 0 20px}video{display:block;width:100%;background:#080e18}nav{display:flex;gap:1px;margin:14px 0;background:#344155}button{flex:1;text-align:left;padding:14px;background:#19283a;color:#bfd2f5;border:0;font:inherit;cursor:pointer}button:hover,button:focus-visible{background:#293d56;outline:2px solid #bfd2f5}.controls{display:flex;gap:12px;flex-wrap:wrap;align-items:center}a{color:#bfd2f5}select{background:#19283a;color:white;padding:7px;border:1px solid #344155}.reference{display:flex;gap:24px;align-items:center;margin:26px 0}.reference img.card{width:250px;max-width:48vw}.reference img.buff{width:64px;height:64px}.caption{font-size:13px;color:#b5c4d8}figure{margin:0}.note{font-size:13px;margin-top:24px}@media(max-width:700px){main{padding:16px 10px}nav{flex-direction:column}h1{font-size:23px}}
</style><main><h1>门票 · 珍珠白与淡蓝</h1><p>依据实际卡图与能力图标：白色光瓣、淡蓝亮核和细粒子。包含打出门票及1／2层回合生成。</p>
<video id="film" src="ticket-showcase.mp4" poster="poster.png" controls preload="metadata" playsinline></video><nav>BUTTONS</nav>
<div class="controls"><label>播放速度 <select id="speed"><option value="1">原速 1×</option><option value="0.5">慢放 0.5×</option><option value="0.25">慢放 0.25×</option></select></label><a href="ticket-showcase.mp4">完整视频</a><a href="source-stable.mp4">正式版原片</a><a href="source-beta.mp4">测试版原片</a><a href="../card-origin-stage-vfx-showcase-20261013/revision.html">上一版对照</a></div>
<div class="reference"><figure><img class="card" src="reference/card.png" alt="门票实际卡图"><figcaption class="caption">门票卡图</figcaption></figure><figure><img class="buff" src="reference/buff.png" alt="门票实际能力图标"><figcaption class="caption">门票能力图标</figcaption></figure></div>
<p class="note">Godot 实机视口及游戏进程音频。标题在视口外，原片保持原样；图形由游戏实时绘制。回合验证在隔离存档中移除起始发夹，核对门票自身的生成数量。</p></main>
<script>const film=document.querySelector('#film');document.querySelectorAll('[data-time]').forEach(b=>b.addEventListener('click',()=>{film.currentTime=Number(b.dataset.time);film.play()}));document.querySelector('#speed').addEventListener('change',e=>film.playbackRate=Number(e.target.value))</script></html>'''
(out/'index.html').write_text(page.replace('BUTTONS',buttons),encoding='utf-8')
print(video,'duration',duration(video),'audio',qa['audioMeanDb'],flush=True)
