"""Package unmodified MCP recordings with chapter captions outside the game viewport."""
import pathlib,json,subprocess,shutil,hashlib,html
repo=pathlib.Path(__file__).resolve().parents[2];out=repo/'artifacts/energy-demo-output';out.mkdir(exist_ok=True)
clips=[('01-native-layout','卡牌费用与固定九边形外框','费用和数字沿用原生位置，外框保持稳定'),('02-spend-to-zero','真实出牌：3 → 2 → 1 → 0','出牌扣能由游戏执行，耗尽时数字转红、能量球置灰'),('03-next-turn','结束回合，能量恢复至 3/3','内部笔触继续流动，下一回合自动补满能量'),('04-inline-and-refill','零费卡、行内符号与实际补能','迈向那个舞台实际增加 3 点能量，显示 6/3 并触发补能闪光'),('05-two-and-X-cost','2 费与 X 费卡牌显示','神圣星辰与约定之塔桥沿用游戏本身的费用计算和数字'),('06-promise-in','坠落：放入约定牌堆','演示卡由开发命令加入；实际移牌、扣能和减费由游戏执行'),('07-promise-out','约定之塔：取回后费用 2 → 1','取回的神圣星辰保留本场战斗减费，继续使用九边形费用图标')]
versions={'stable':'正式版 v0.107.1','beta':'测试版 v0.111.0'}
def run(command):
 result=subprocess.run(command,cwd=out,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding='utf-8',errors='replace')
 if result.returncode:raise RuntimeError(result.stderr[-5000:])
 return result
all_records=[];encoded=[]
for branch,version in versions.items():
 evidence=repo/'artifacts/energy-demo'/branch/'evidence';raw=out/('raw-'+branch);raw.mkdir(exist_ok=True)
 for i,(name,title,note) in enumerate(clips):
  record=json.loads((evidence/(name+'-recording.json')).read_text(encoding='utf-8'));assert record['status']=='completed' and record['capture_fps']>=24 and record['frames_dropped']==0,record
  source=pathlib.Path(record['output_path']);raw_file=raw/(name+'.mp4');shutil.copy2(source,raw_file)
  ident=branch+'-'+name
  (out/(ident+'-title.txt')).write_text(version+'  ·  '+str(i+1).zfill(2)+'  '+title,encoding='utf-8')
  (out/(ident+'-note.txt')).write_text(note,encoding='utf-8')
  filt=f"pad=1280:840:0:72:color=0x201f25,drawtext=fontfile='C\\:/Windows/Fonts/msyh.ttc':textfile={ident}-title.txt:fontcolor=0xffaaa1:fontsize=25:x=26:y=23,drawtext=fontfile='C\\:/Windows/Fonts/msyh.ttc':textfile={ident}-note.txt:fontcolor=0xd7cac4:fontsize=20:x=26:y=804"
  target=out/(ident+'.mp4');run(['ffmpeg','-y','-v','error','-i',str(raw_file),'-vf',filt,'-an','-c:v','libx264','-threads','2','-preset','fast','-crf','19','-pix_fmt','yuv420p','-r','30','-movflags','+faststart',str(target)])
  record.update(branch=branch,version=version,key=name,title=title,note=note,rawPath=str(raw_file.relative_to(out)),captionedPath=target.name,sha256=hashlib.sha256(raw_file.read_bytes()).hexdigest());all_records.append(record);encoded.append(target)
  print(json.dumps({'encoded':ident,'captureFps':record['capture_fps']},ensure_ascii=False),flush=True)
def assemble(name,records):
 playlist=out/(name+'-concat.txt');playlist.write_text(''.join("file '"+r['captionedPath']+"'\n" for r in records),encoding='utf-8')
 meta=[';FFMETADATA1','title=Karen 正九边形能量图标实机演示'];time_ms=0
 for r in records:
  duration_ms=round(r['video_duration_seconds']*1000);meta+=['[CHAPTER]','TIMEBASE=1/1000',f'START={time_ms}',f'END={time_ms+duration_ms}','title='+r['version']+' · '+r['title']];time_ms+=duration_ms
 metadata=out/(name+'-chapters.ffmeta');metadata.write_text('\n'.join(meta)+'\n',encoding='utf-8')
 target=out/(name+'.mp4');run(['ffmpeg','-y','-v','error','-f','concat','-safe','0','-i',str(playlist),'-i',str(metadata),'-map','0:v:0','-map_metadata','1','-map_chapters','1','-c','copy','-movflags','+faststart',str(target)])
 info=json.loads(run(['ffprobe','-v','error','-show_format','-show_streams','-show_chapters','-of','json',str(target)]).stdout)
 assert info['streams'][0]['width']==1280 and info['streams'][0]['height']==840 and len(info['chapters'])==len(records),info
 assert abs(float(info['format']['duration'])-time_ms/1000)<.25,info['format']
 (out/(name+'-media-audit.json')).write_text(json.dumps(info,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 return {'file':target.name,'durationSeconds':float(info['format']['duration']),'chapters':len(records),'bytes':target.stat().st_size}
outputs=[assemble('stable-demo',[r for r in all_records if r['branch']=='stable']),assemble('beta-demo',[r for r in all_records if r['branch']=='beta']),assemble('full-demo',all_records)]
(out/'recordings.json').write_text(json.dumps({'tool':'KarenSTS2MCP, native game viewport recordings','audio':False,'viewportPixelsModified':False,'captionOutsideViewport':True,'clips':all_records,'outputs':outputs},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
shutil.copy2(repo/'artifacts/energy-demo/stable/evidence/01-hover-card.png',out/'poster.png')
time_ms=0;buttons=[]
for r in all_records:
 buttons.append('<button data-time="'+str(time_ms/1000)+'"><small>'+r['version']+'</small>'+html.escape(r['title'])+'</button>');time_ms+=round(r['video_duration_seconds']*1000)
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>Karen 九边形能量 · 全面实机演示</title><style>body{margin:0;background:#201f25;color:#eee8df;font:16px "Microsoft YaHei",sans-serif}main{max-width:1280px;margin:0 auto;padding:30px 24px}h1{font-size:30px;font-weight:600;margin:0 0 10px}p{color:#bcaea8;line-height:1.8}video{width:100%;display:block;background:#111;border:1px solid #594c51}.chapters{display:grid;grid-template-columns:repeat(2,1fr);gap:9px;margin-top:18px}button{text-align:left;border:1px solid #594c51;background:#2b272f;color:#eee8df;padding:13px;cursor:pointer;font:inherit}button:hover{border-color:#fb777c}small{display:block;color:#ffaaa1;margin-bottom:5px;font-size:12px}footer{font-size:13px;color:#aa9891;line-height:1.8;margin-top:20px}</style><main><h1>Karen 九边形能量 · 全面实机演示</h1><p>双版本正式安装包实测。卡牌费用、行内符号、能量耗尽、回合恢复、补能和约定牌堆减费。<br>点击章节可直接跳转；静音录制，游戏视口保持原始内容，字幕放在视口外。</p><video controls preload="metadata" poster="poster.png" src="full-demo.mp4"></video><div class="chapters">'''+''.join(buttons)+'''</div><footer>卡费 64×64；战斗控件 128×128；行内符号 24×24。演示审计工具只读，没有换图补丁。<br>完整测试结果见 matrix-report.json；实际采帧率、原片哈希与封装信息见 recordings.json。</footer></main><script>const player=document.querySelector('video');document.querySelectorAll('button').forEach(button=>button.onclick=()=>{player.currentTime=Number(button.dataset.time);player.play();player.scrollIntoView({behavior:'smooth',block:'center'});});</script></html>'''
(out/'index.html').write_text(page,encoding='utf-8')
print(json.dumps({'outputs':outputs,'minCaptureFps':min(r['capture_fps'] for r in all_records)},ensure_ascii=False))