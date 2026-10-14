"""Cut actual seven-card MCP recordings into chapters; preserve the game image and its audio."""
import argparse, hashlib, html, json, pathlib, re, shutil, subprocess

p=argparse.ArgumentParser()
p.add_argument('--solo',type=pathlib.Path,required=True)
p.add_argument('--multiplayer',type=pathlib.Path,required=True)
p.add_argument('--output',type=pathlib.Path,required=True)
args=p.parse_args();out=args.output.resolve();out.mkdir(parents=True,exist_ok=True)
solo=args.solo.resolve();multi=args.multiplayer.resolve()
chapters=json.loads((solo/'chapters.json').read_text(encoding='utf-8'))
render=out/'render';render.mkdir(parents=True,exist_ok=True)

def run(argv,**kw):
    if kw.get('text'):kw.update(encoding='utf-8',errors='replace')
    return subprocess.run(argv,check=True,**kw)
def probe(path):return json.loads(subprocess.check_output(['ffprobe','-v','error','-show_format','-show_streams','-of','json',str(path)],text=True,encoding='utf-8'))
def duration(path):return float(probe(path)['format']['duration'])

segments=[]
descriptions=['金棕碎屑内收 → 真实药水凝成 → 原生药水栏接收','琥珀暖流快升与散热 · 获得临时力量','厚重暖辉分层缓升 · 获得永久力量',
    '金尘体积流卷起 · 向两侧分流','暖冷两股流光汇成亮核 · 共同梦想',
    '脚下扇形舞台灯点亮真实队伍 → 每人一项战后闪耀牌奖励',
    '短碎屑配合原生变牌 · 梦想生成光辉 · 门票生成塔牌','晚餐与光辉回到实际手牌',
    '实际光辉卡面绽放 → 两道能量火花进入真实能量槽']
for i in range(5):segments.append((chapters[i]['label'],solo/'game.mp4',chapters[i]['time'],chapters[i+1]['time']))
segments[0]=(segments[0][0],segments[0][1],segments[0][2]+.55,segments[0][3])
segments.append(('一起闪耀吧 · 双人联机',multi/'game.mp4',.2,duration(multi/'game.mp4')-1.15))
segments.append(('回合触发 · 变牌 / 梦想 / 门票',solo/'game.mp4',chapters[6]['time'],chapters[7]['time']))
segments.append(('约定之塔 · 取回晚餐与光辉',solo/'game.mp4',chapters[7]['time'],chapters[8]['time']))
segments.append(('光辉 · 获得2点能量',solo/'game.mp4',chapters[8]['time'],duration(solo/'game.mp4')))

manifest={'recording':'Godot viewport + game_process audio','edition':'stable v0.107.1',
    'edits':'Trim, concatenate, +12 dB playback gain, and a 96px title band outside the original game viewport. No generated VFX added in post. Raw source recordings are unchanged.',
    'sources':[],'chapters':[]}
for folder,name in [(solo,'source-singleplayer'),(multi,'source-multiplayer')]:
    shutil.copy2(folder/'game.mp4',out/(name+'.mp4'))
    for suffix in ['recording','build']:
        shutil.copy2(folder/(suffix+'.json'),out/(name+'-'+suffix+'.json'))
    manifest['sources'].append({'file':name+'.mp4','sha256':hashlib.sha256((folder/'game.mp4').read_bytes()).hexdigest()})

cursor=0;clips=[]
for i,(title,source,start,end) in enumerate(segments):
    titlefile=render/f'title-{i}.txt';titlefile.write_text(f'{i+1:02d}   {title}',encoding='utf-8')
    subfile=render/f'sub-{i}.txt';subfile.write_text(descriptions[i]+'    |    游戏实录 · 原速',encoding='utf-8')
    clip=out/f'{i+1:02d}.mp4'
    # Relative text files avoid shell and drawtext escaping of user-facing Chinese text.
    vf=("pad=1280:816:0:96:color=0x0c1420,"
        f"drawtext=fontfile='C\\:/Windows/Fonts/msyh.ttc':textfile=title-{i}.txt:fontcolor=0xffd49a:fontsize=30:x=26:y=12,"
        f"drawtext=fontfile='C\\:/Windows/Fonts/msyh.ttc':textfile=sub-{i}.txt:fontcolor=0xc1ccd9:fontsize=17:x=28:y=59")
    run(['ffmpeg','-hide_banner','-loglevel','error','-y','-ss',str(start),'-i',str(source),'-t',str(end-start),
        '-vf',vf,'-r','30','-c:v','libx264','-threads','2','-preset','fast','-crf','18','-pix_fmt','yuv420p',
        '-af','volume=12dB','-c:a','aac','-ar','48000','-ac','2','-b:a','160k','-movflags','+faststart',str(clip)],cwd=render)
    length=duration(clip)
    manifest['chapters'].append({'number':i+1,'title':title,'description':descriptions[i],'start':cursor,'duration':length,'file':clip.name})
    cursor+=length;clips.append(clip)
    print('Rendered',i+1,title,flush=True)

concat=render/'concat.txt';concat.write_text(''.join(f"file '{f.as_posix()}'\n" for f in clips),encoding='utf-8')
combined=render/'combined.mp4'
run(['ffmpeg','-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(concat),'-c','copy',str(combined)])
metadata=render/'chapters.ffmeta'
text=';FFMETADATA1\ntitle=七张卡牌 · 抽象光效重设计\n'
for c in manifest['chapters']:
    text+=f"[CHAPTER]\nTIMEBASE=1/1000\nSTART={round(c['start']*1000)}\nEND={round((c['start']+c['duration'])*1000)}\ntitle={c['title']}\n"
metadata.write_text(text,encoding='utf-8')
final=out/'seven-card-showcase.mp4'
run(['ffmpeg','-hide_banner','-loglevel','error','-y','-i',str(combined),'-i',str(metadata),'-map_metadata','1','-c','copy','-movflags','+faststart',str(final)])
poster_time=manifest['chapters'][4]['start']+1.65
run(['ffmpeg','-hide_banner','-loglevel','error','-y','-ss',str(poster_time),'-i',str(final),'-frames:v','1',str(out/'poster.png')])

qa={'metadata':probe(final),'motion':[]}
for clip in clips:
    hashes=subprocess.check_output(['ffmpeg','-hide_banner','-loglevel','error','-i',str(clip),'-vf','crop=1280:720:0:96,fps=4',
        '-an','-f','framemd5','-'],text=True,encoding='utf-8')
    frames=[line.rsplit(',',1)[-1].strip() for line in hashes.splitlines() if not line.startswith('#')]
    unique=len(set(frames));assert unique>=4,(clip,unique)
    qa['motion'].append({'file':clip.name,'sampledFrames':len(frames),'uniqueFrames':unique})
audio=run(['ffmpeg','-hide_banner','-i',str(final),'-af','volumedetect','-vn','-f','null','-'],capture_output=True,text=True)
qa['audioMeanDb']=float(re.search(r'mean_volume: ([\d.\-]+) dB',audio.stderr).group(1))
qa['audioPeakDb']=float(re.search(r'max_volume: ([\d.\-]+) dB',audio.stderr).group(1))
assert qa['audioMeanDb']>-65,qa
freeze=run(['ffmpeg','-hide_banner','-i',str(final),'-vf','freezedetect=n=-50dB:d=1','-an','-f','null','-'],capture_output=True,text=True)
qa['freezeDiagnostics']=[line for line in freeze.stderr.splitlines() if 'freeze_' in line]
(out/'qa.json').write_text(json.dumps(qa,ensure_ascii=False,indent=2),encoding='utf-8')
(out/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')

portraits={1:'karen/karen_banana_cake.png',2:'karen/karen_small_snack.png',3:'token/karen_banana.png',
    4:'karen/karen_ticket.png',5:'karen/karen_we_kown.png',6:'karen/karen_shine_together_all.png',9:'token/karen_we_kown_token.png'}
(out/'cards').mkdir(exist_ok=True)
repo=pathlib.Path(__file__).resolve().parents[3]
for number,relative in portraits.items():shutil.copy2(repo/'images/packed/card_portraits'/relative,out/f'cards/{number}.png')
buttons=''.join(f'<button type="button" data-time="{c["start"]}">'+
    (f'<img src="cards/{c["number"]}.png" alt="原卡面">' if c['number'] in portraits else '<span class="extra">回合<br>联动</span>')+
    f'<span>{c["number"]:02d}　{html.escape(c["title"])}</span></button>' for c in manifest['chapters'])
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>七张卡牌 · 抽象光效重设计</title><style>
*{box-sizing:border-box}body{margin:0;background:#0c1420;color:#e6edf7;font:16px/1.6 system-ui,"Microsoft YaHei",sans-serif}
main{max-width:1240px;margin:auto;padding:28px 24px}header{display:flex;align-items:baseline;justify-content:space-between;gap:20px}
h1{font-size:26px;letter-spacing:.03em;margin:0 0 8px}p{color:#acbdce;margin:0 0 20px}video{display:block;width:100%;background:#05090e}
nav{display:grid;grid-template-columns:repeat(3,1fr);gap:1px;background:#2c3746;margin:16px 0}button{display:flex;align-items:center;gap:12px;color:#e6edf7;background:#121e2d;border:0;text-align:left;padding:12px;cursor:pointer;font:inherit;font-size:14px}
button img{width:64px;height:48px;object-fit:cover;border-radius:2px}button .extra{width:64px;flex-shrink:0;font-size:12px;text-align:center;color:#acbdce}button:hover,button:focus-visible{background:#243346;outline:2px solid #ffd49a;outline-offset:-2px}button span{color:#ffd49a}.controls{display:flex;gap:10px;align-items:center;flex-wrap:wrap}a{color:#ffd49a}.note{font-size:13px;margin-top:22px}select{background:#142032;color:white;border:1px solid #41516a;padding:7px}label{font-size:14px}
@media(max-width:700px){main{padding:16px 10px}header{display:block}nav{grid-template-columns:repeat(2,1fr)}h1{font-size:22px}}
</style><main><header><h1>七张卡牌 · 抽象光效重设计</h1><p>2026.10.13 / Godot 游戏实录</p></header>
<p>本轮调整：光辉从实际卡面飞出、一起闪耀吧使用舞台灯、门票改为金尘体积流。点击原卡面跳转对应实录，「光辉」在第 09 段。</p>
<video id="film" src="seven-card-showcase.mp4" poster="poster.png" controls preload="metadata" playsinline></video><nav>BUTTONS</nav>
<div class="controls"><label>播放速度 <select id="speed"><option value="1">原速 1×</option><option value="0.5">慢放 0.5×</option><option value="0.25">逐步看细节 0.25×</option></select></label>
<a href="seven-card-showcase.mp4">完整视频</a><span>·</span><a href="09.mp4">单独看光辉</a><span>·</span><a href="source-singleplayer.mp4">单人原片</a><span>·</span><a href="source-multiplayer.mp4">双人原片</a><span>·</span><a href="../abstract-seven-card-vfx-showcase-20261013/index.html">上一版抽象光效对照</a></div>
<p class="note">六张单人牌、一张真实双人联机牌，以及什锦变晚餐、生成与取回光辉。成片在游戏画面之外加入标题，游戏原声统一提高播放音量；特效由游戏实时渲染，以原创实时材质和原版辅助纹理绘制，未复制储君 Mod 资源。原始录像也可直接查看。</p></main>
<script>const film=document.querySelector('#film');document.querySelectorAll('[data-time]').forEach(b=>b.addEventListener('click',()=>{film.currentTime=Number(b.dataset.time);film.play()}));document.querySelector('#speed').addEventListener('change',e=>{film.playbackRate=Number(e.target.value)})</script></html>'''
(out/'index.html').write_text(page.replace('BUTTONS',buttons),encoding='utf-8')
print('Showcase',final,'duration',duration(final),'audio',qa['audioMeanDb'],flush=True)
