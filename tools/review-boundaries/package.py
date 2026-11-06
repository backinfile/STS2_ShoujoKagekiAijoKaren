"""Pair original MCP clips at native speed; captions stay outside the viewport."""
from pathlib import Path
import hashlib,json,os,re,shutil,subprocess
root=Path(__file__).resolve().parents[2];base=root/'artifacts/review-boundaries';out=base/'comparison';out.mkdir(exist_ok=True)
font=Path(os.environ['WINDIR'])/'Fonts/msyh.ttc';shutil.copy2(font,out/'font.ttc')
scenes=[('empty','约定牌堆：补牌后再次清空'),('void','Void：补牌后再次清空'),('reward1','闪耀奖励：3 张领取 1 张'),('tower','约定之塔：移牌联动结束战斗')]
versions={'stable':'正式版 v0.107.1','beta':'测试版 v0.111.0'}
manifest={'date':'2026-10-29','scope':'targeted singleplayer before/after; no full multiplayer matrix','audio':False,'nativeSpeed':True,'baselineSafetyStop':8,'runs':[],'clips':[],'videos':[]}
def run(argv):
    r=subprocess.run(argv,cwd=out,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding='utf-8',errors='replace')
    if r.returncode:raise RuntimeError(r.stderr[-3500:])
    return r

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def locate(phase,branch,name):
    folder=base/phase/(branch+'-retake')
    if not (folder/(name+'.mp4')).exists():folder=base/phase/branch
    if not (folder/(name+'.mp4')).exists():folder=base/phase/(branch+'-tail')
    assert (folder/(name+'.mp4')).is_file(),folder
    return folder

for branch,version in versions.items():
    for phase in ['before','after']:
        rows=[]
        folders=[base/phase/branch]
        if (base/phase/(branch+'-tail')/'results.json').exists():folders.append(base/phase/(branch+'-tail'))
        for folder in folders:rows+=json.loads((folder/'results.json').read_text(encoding='utf-8'))
        assert len(rows)==10 and len({r['case'] for r in rows})==10,(branch,phase,len(rows))
        manifest['runs'].append(dict(branch=branch,phase=phase,cases=rows))
    encoded=[];chapters=[';FFMETADATA1','title='+version+' 修复前后对比'];elapsed=0
    for name,title in scenes:
        files=[];durations=[]
        for phase in ['before','after']:
            source=locate(phase,branch,name)
            record=json.loads((source/(name+'-recording.json')).read_text(encoding='utf-8'))
            assert record['status']=='completed' and record['capture_fps']>=24 and record['frames_dropped']==0,record
            raw=source/(name+'.mp4');local=out/(branch+'-'+phase+'-'+name+'.mp4');shutil.copy2(raw,local);files.append(local.name)
            info=json.loads(run(['ffprobe','-v','error','-show_format','-show_streams','-of','json',local.name]).stdout)
            duration=float(info['format']['duration']);durations.append(duration)
            assert info['streams'][0]['width']==1280 and info['streams'][0]['height']==720
            framemd5=run(['ffmpeg','-v','error','-i',local.name,'-vf','fps=2','-f','framemd5','-']).stdout
            samples=[x.rsplit(',',1)[-1].strip() for x in framemd5.splitlines() if x and not x.startswith('#')]
            assert len(set(samples))>2,(local.name,'stale capture')
            manifest['clips'].append(dict(branch=branch,phase=phase,scene=name,path=raw.relative_to(root).as_posix(),sha256=sha(raw),seconds=duration,encoder=record['encoder'],captureFps=record['capture_fps'],framesDropped=record['frames_dropped'],framesRepeated=record.get('frames_repeated'),sampledFrames=len(samples),uniqueSampledFrames=len(set(samples))))
        duration=max(durations)
        stem=branch+'-'+name
        (out/(stem+'-title.txt')).write_text(version+' · '+title,encoding='utf-8')
        left='修复前';right='修复后'
        if name in ['empty','void']:left+=' · 能量 4/3';right+=' · 能量 5/3'
        if name=='reward1':left+=' · 未选记录 2';right+=' · 未选记录 0'
        if name=='tower':left+=' · 失败 8 次，测试中止';right+=' · 正常结束战斗'
        for side,text in [('left',left),('right',right)]: (out/(stem+'-'+side+'.txt')).write_text(text,encoding='utf-8')
        filt=f"[0:v]setpts=PTS-STARTPTS,scale=960:540,setsar=1,tpad=stop_mode=clone:stop_duration={duration-durations[0]+.1},trim=duration={duration}[a];[1:v]setpts=PTS-STARTPTS,scale=960:540,setsar=1,tpad=stop_mode=clone:stop_duration={duration-durations[1]+.1},trim=duration={duration}[b];[a][b]hstack=inputs=2,pad=1920:640:0:90:color=0x171c25,drawtext=fontfile=font.ttc:textfile={stem}-title.txt:fontsize=26:fontcolor=white:x=24:y=10,drawtext=fontfile=font.ttc:textfile={stem}-left.txt:fontsize=22:fontcolor=0xffb2ad:x=24:y=52,drawtext=fontfile=font.ttc:textfile={stem}-right.txt:fontsize=22:fontcolor=0xb1e1bd:x=984:y=52[v]"
        target=stem+'-pair.mp4'
        run(['ffmpeg','-y','-v','error','-i',files[0],'-i',files[1],'-filter_complex',filt,'-map','[v]','-an','-c:v','libx264','-threads','2','-preset','fast','-crf','20','-pix_fmt','yuv420p','-r','30','-movflags','+faststart',target])
        encoded.append(target);end=elapsed+round(duration*1000)
        chapters+=['[CHAPTER]','TIMEBASE=1/1000',f'START={elapsed}',f'END={end}','title='+title];elapsed=end
        print('PAIRED '+stem,flush=True)
    playlist=branch+'-concat.txt';(out/playlist).write_text(''.join("file '"+p+"'\n" for p in encoded),encoding='utf-8')
    meta=branch+'-chapters.ffmeta';(out/meta).write_text('\n'.join(chapters)+'\n',encoding='utf-8')
    target=branch+'-before-after.mp4'
    run(['ffmpeg','-y','-v','error','-f','concat','-safe','0','-i',playlist,'-i',meta,'-map','0:v','-map_metadata','1','-map_chapters','1','-c','copy','-movflags','+faststart',target])
    info=json.loads(run(['ffprobe','-v','error','-show_format','-show_streams','-show_chapters','-of','json',target]).stdout)
    assert len(info['chapters'])==4 and info['streams'][0]['width']==1920 and info['streams'][0]['height']==640
    manifest['videos'].append(dict(branch=branch,path=(out/target).relative_to(root).as_posix(),sha256=sha(out/target),seconds=float(info['format']['duration']),bytes=(out/target).stat().st_size,chapters=[c['tags']['title'] for c in info['chapters']]))
    run(['ffmpeg','-y','-v','error','-i',target,'-vf','fps=1/3,scale=640:-1,tile=3x3','-frames:v','1',branch+'-frames.png'])
for branch in versions:
    extra=json.loads((base/'after'/(branch+'-hand-limits')/'results.json').read_text(encoding='utf-8'))
    assert {r['case'] for r in extra}=={'overflow','full'}
    manifest.setdefault('supplementary',[]).append(dict(branch=branch,cases=extra))
for phase,folder in [('before','baseline'),('after','fixed')]:
    for branch in versions:
        path=root/f'artifacts/{folder}/{branch}/ShoujoKagekiAijoKaren.{branch.title()}.dll'
        manifest.setdefault('assemblies',[]).append(dict(phase=phase,branch=branch,path=path.relative_to(root).as_posix(),sha256=sha(path)))
for name in ['PromisePileManager.cs','PromisePileCmd.cs','PromisePileHookPatch.cs','KarenShineCardReward.cs']:
    path=next((root/'src').rglob(name));manifest.setdefault('source',[]).append(dict(path=path.relative_to(root).as_posix(),sha256=sha(path)))
# Keep audit and log summaries portable; raw responses remain in ignored artifacts.
audit_ok=0;not_applicable=0;folders=[]
for phase in ['before','after']:
    for branch in versions:
        for suffix in ['', '-tail', '-retake', '-hand-limits']:
            folder=base/phase/(branch+suffix)
            if (folder/'results.json').exists():folders.append(folder)
for folder in folders:
    rows=json.loads((folder/'results.json').read_text(encoding='utf-8'))
    assert all(row['result']=='pass' for row in rows),folder
    if folder.name.endswith('-retake'):
        manifest.setdefault('retakes',[]).append(dict(path=folder.relative_to(root).as_posix(),cases=rows))
    for row in rows:
        for key in ['handBefore','handAfter']:
            result=row[key]
            if result['status']=='not_applicable':not_applicable+=1;continue
            audit=json.loads(result['message'])
            assert audit['modelCount']==audit['visualCount'] and not audit['missing'] and not audit['unexpected'] and not audit['unexpectedPileIcons'],(folder,row['case'],audit)
            assert all(x['valid'] for x in audit.get('burnOverlays',[])),audit
            audit_ok+=1
        for side in ['before','after']:
            path=folder/(row['case']+'-'+side+'.png')
            manifest.setdefault('screenshots',[]).append(dict(path=path.relative_to(root).as_posix(),sha256=sha(path)))
    branch=folder.name.split('-')[0];variant=branch.title();phase=folder.parent.name
    installed=folder/f'game/mods/ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.{variant}.dll'
    expected=root/f'artifacts/{"baseline" if phase=="before" else "fixed"}/{branch}/ShoujoKagekiAijoKaren.{variant}.dll'
    assert sha(installed)==sha(expected),(installed,'tested assembly mismatch')
    lines=(folder/'godot.log').read_text(encoding='utf-8',errors='replace').splitlines()
    errors=[dict(line=i+1,message=x.strip()) for i,x in enumerate(lines) if re.search(r'ERROR:|Exception',x)]
    assert all(e['message']=='ERROR: Invalid Task ID' for e in errors),errors
    manifest.setdefault('logs',[]).append(dict(path=(folder/'godot.log').relative_to(root).as_posix(),sha256=sha(folder/'godot.log'),errors=errors))
manifest['handAudit']=dict(passed=audit_ok,combatEnded=not_applicable)
(out/'evidence.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'videos':manifest['videos'],'clips':len(manifest['clips'])},ensure_ascii=False),flush=True)
