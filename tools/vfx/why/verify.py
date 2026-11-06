"""Targeted Why VFX verification and game-process recording through KarenSTS2MCP."""
import argparse, hashlib, json, os, pathlib, shutil, socket, subprocess, time, urllib.request
import numpy as np
from PIL import Image

p = argparse.ArgumentParser()
p.add_argument('branch', choices=['stable', 'beta'])
p.add_argument('--output', type=pathlib.Path, required=True)
p.add_argument('--cases', default='base,zero,upgrade,fast,instant,void,empty,repeat,peek,resize,exit')
p.add_argument('--selection-hold', type=float, default=1.6, help='Extra selection hold in recorded cases, in seconds.')
p.add_argument('--audit-capture', action='store_true', help='Record and decode a temporary in-game wall clock before the showcase.')
p.add_argument('--recording-encoder', choices=['auto','h264_nvenc','libx264'], default='auto')
p.add_argument('--staged-selection', action='store_true', help='Distribute the hold between real individual selections for a continuously interactive recording.')
p.add_argument('--smooth-recording', action='store_true', help='Defer PNG screenshots until after recording; keep JSON state snapshots during capture.')
p.add_argument('--no-recording', action='store_true', help='Run screenshot and gameplay diagnostics without recording; PNG capture stalls the render thread.')
args = p.parse_args()
project = pathlib.Path(__file__).resolve().parents[3]
root = args.output.resolve()
dev = root / 'isolated' / args.branch
game = dev / 'game'
out = root / args.branch
out.mkdir(exist_ok=True)
conf = json.loads((game / 'mods/KarenSTS2MCP.conf').read_text(encoding='utf-8-sig'))
with socket.socket() as check:
    assert check.connect_ex(('127.0.0.1', conf['port'])) != 0, 'Port occupied; no deployment performed'
base = f"http://127.0.0.1:{conf['port']}/api/v1"
opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))

def api(path, data=None):
    req = urllib.request.Request(base + path, None if data is None else json.dumps(data).encode(), headers={'Content-Type':'application/json'})
    with opener.open(req, timeout=25) as r: return json.load(r)

def state(): return api('/singleplayer?format=json')
def post(**kw):
    r = api('/singleplayer', kw)
    with (out/'actions.jsonl').open('a', encoding='utf-8') as f: f.write(json.dumps([kw,r], ensure_ascii=False)+'\n')
    assert r.get('status') == 'ok', (kw,r)
    return r
def cmd(command): return post(action='run_command', command=command)
def save(path, data): path.with_suffix('.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
def snap(path):
    save(path, state())
    with opener.open(base+'/screenshot', timeout=25) as r: path.with_suffix('.png').write_bytes(r.read())
def hand(path):
    h = json.loads(cmd('karen_check_hand')['message']); save(path,h)
    assert h['modelCount']==h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'],h
def probe(): return json.loads(cmd('why_probe status')['message'])

def recording_snapshot(path):
    if args.smooth_recording and recording:save(path,state())
    else:snap(path)

def finish_recording(case):
    global recording
    time.sleep(.5);api('/recording/stop',{})
    for _ in range(120):
        rec=api('/recording')
        if rec['status'] in ['completed','failed']:break
        time.sleep(.2)
    assert rec['status']=='completed' and rec.get('audio_source')=='game_process',rec
    assert rec.get('encoder') in ['h264_nvenc','libx264'] and 'frames_dropped' in rec,'Update KarenSTS2MCP before recording'
    assert rec['capture_fps']>=27 and rec['frames_repeated']<=rec['frames_written']*.1 and rec['frames_dropped']<=rec['frames_captured']*.1,rec
    shutil.copy2(rec['output_path'],case/'game.mp4');save(case/'recording',rec);recording=False
    api('/settings',dict(muted=True))

def audit_capture():
    global recording
    folder=out/'capture-audit';folder.mkdir(exist_ok=True)
    cmd('why_probe clock on');time.sleep(.3)
    save(folder/'clock',probe()['clock']);snap(folder/'before')
    api('/settings',dict(fullscreen=False,muted=False,window_width=1280,window_height=720))
    api('/recording/start',dict(fps=30,max_duration_seconds=8,width=1280,height=720,audio=True,encoder=args.recording_encoder));recording=True
    deadline=time.monotonic()+30
    while time.monotonic()<deadline:
        rec=api('/recording')
        if rec['status'] in ['completed','failed']:break
        time.sleep(.2)
    assert rec['status']=='completed',rec
    save(folder/'recording',rec);shutil.copy2(rec['output_path'],folder/'game.mp4');recording=False
    cmd('why_probe clock off');api('/settings',dict(muted=True))
    subprocess.run(['python',str(project/'tools/vfx/why/check_capture.py'),str(folder)],check=True)

def check_border_pixels(case):
    without=np.array(Image.open(case/'without-border.png').convert('RGB')).astype(int)
    border=np.array(Image.open(case/'border.png').convert('RGB')).astype(int)
    height,width=border.shape[:2]
    y,x=np.mgrid[:height,:width]
    edge=(x<width*.14)|(x>width*.86)|(y<height*.18)|(y>height*.88)
    changed=np.max(abs(border-without),axis=2)>40
    # Native hand-card rims keep pulsing between screenshots. Inspect the border
    # and central play area, while omitting the native hand's interior below 72%.
    inspected=edge|(y<height*.72)
    changed &= inspected
    report=dict(changedPixels=int(changed.sum()),edgeChangedFraction=float((changed&edge).sum()/max(1,changed.sum())),
        ignored='Interior of native animated hand area below 72% screen height')
    save(case/'border-distribution',report)
    assert report['changedPixels']>200 and report['edgeChangedFraction']>.90,report

def check_light_motion(case):
    first=np.array(Image.open(case/'motion-a.png').convert('RGB')).astype(int)
    second=np.array(Image.open(case/'motion-b.png').convert('RGB')).astype(int)
    height,width=first.shape[:2]
    y,x=np.mgrid[:height,:width]
    edge=(x<width*.10)|(x>width*.90)|(y<height*.12)|(y>height*.88)
    moved=(np.max(abs(first-second),axis=2)>20)&edge
    report=dict(edgePixelsChanged=int(moved.sum()),intervalSeconds=.65,threshold=20)
    save(case/'light-motion',report)
    assert report['edgePixelsChanged']>500,report
def wait(predicate, seconds=25):
    deadline=time.monotonic()+seconds
    while time.monotonic()<deadline:
        if process and process.poll() is not None: raise RuntimeError('Isolated game exited')
        try: s=state()
        except (OSError, ValueError): time.sleep(.1); continue
        if predicate(s): return s
        time.sleep(.1)
    raise AssertionError(('State timeout',state()))
def play():
    s=state(); c=next(c for c in s['player']['hand'] if c['id']=='KAREN_WHY')
    post(action='play_card',card_index=c['index'])
def select(count, case):
    s=wait(lambda s:s.get('state_type')=='hand_select')
    time.sleep(.85)
    if case.name=='resize':
        api('/settings',dict(fullscreen=False,window_width=1600,window_height=900));time.sleep(.5)
    if case.name=='peek':
        cmd('why_probe peek true');time.sleep(.25);snap(case/'peek')
        assert probe()['peeking'] and not probe()['visible']
        cmd('why_probe peek false');time.sleep(.25)
        assert not probe()['peeking'] and probe()['visible']
    recording_snapshot(case/'selection')
    save(case/'selection-probe',probe())
    stage_pause=max(0,args.selection_hold)/(count+1) if args.staged_selection and case.name in ['upgrade','void'] else 0
    if case.name in ['base','upgrade','void']:
        time.sleep(stage_pause or max(0,args.selection_hold))
    if case.name=='base' and not args.smooth_recording:
        snap(case/'motion-a');time.sleep(.65);snap(case/'motion-b')
        check_light_motion(case)
        cmd('why_probe visual off');time.sleep(.05);snap(case/'without-border')
        cmd('why_probe visual on');time.sleep(.05);snap(case/'border')
        check_border_pixels(case)
    # MCP reports original hand indices on each selection; deselection and confirm stay native.
    for _ in range(count):
        s=state()
        candidates=s.get('hand_select',{}).get('cards',s.get('player',{}).get('hand',[]))
        selected=s.get('hand_select',{}).get('selected_indices',[])
        card=next(c for c in candidates if not c.get('selected',False) and c['index'] not in selected)
        post(action='combat_select_card',card_index=card['index']);time.sleep(.12+stage_pause)
    recording_snapshot(case/'selected')
    post(action='combat_confirm_selection')

process=None; recording=False
results=json.loads((out/'results.json').read_text(encoding='utf-8')) if (out/'results.json').exists() else []
try:
    mods=game/'mods'
    shutil.copytree(project/'artifacts/ShoujoKagekiAijoKaren',mods/'ShoujoKagekiAijoKaren',dirs_exist_ok=True)
    # Separate restore and compiler caches as well as outputs: parallel variant builds
    # must not reuse a fixture compiled against the other implementation assembly.
    intermediate=(root/'probe-obj'/args.branch).as_posix()+'/'
    subprocess.run(['dotnet','build',str(project/'tools/vfx/why/WhyProbe.csproj'),'-p:Variant='+args.branch.title(),'-p:Game='+str(game),
        '-p:BaseIntermediateOutputPath='+intermediate,'-p:MSBuildProjectExtensionsPath='+intermediate,
        '-o',str(root/'probes'/args.branch),'--nologo','-v:q','-clp:ErrorsOnly'],check=True)
    shutil.copy2(root/'probes'/args.branch/'KarenWhyProbe.dll',mods)
    (mods/'KarenWhyProbe.json').write_text(json.dumps(dict(id='KarenWhyProbe',name='KarenWhyProbe',author='Development',version='1.0.0',description='Isolated VFX checks',has_dll=True,has_pck=False,affects_gameplay=False)),encoding='utf-8')
    conf.update(fullscreen=False,muted=True,skip_tutorial=True,skip_first_prompt=True,window_width=1280,window_height=720)
    (mods/'KarenSTS2MCP.conf').write_text(json.dumps(conf),encoding='utf-8')
    override=game/'override.cfg'
    if override.exists(): override.unlink()  # Break the read-only snapshot hardlink before writing our local override.
    override.write_text('[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n',encoding='utf-8')
    for settings in (dev/'user').rglob('settings.save'):
        data=json.loads(settings.read_text(encoding='utf-8-sig'))
        data.update(mute_in_background=False,limit_fps_in_background=False,fullscreen=False,volume_master=.5,seen_ea_disclaimer=True,skip_intro_logo=True)
        settings.write_text(json.dumps(data),encoding='utf-8')
    env=os.environ.copy();env.update(APPDATA=str(dev/'user/AppData/Roaming'),LOCALAPPDATA=str(dev/'user/AppData/Local'),STS2_MCP_FFMPEG=shutil.which('ffmpeg') or '')
    startup=subprocess.STARTUPINFO();startup.dwFlags=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
    log=dev/'godot.log'
    if log.exists(): log.rename(out/f'previous-{time.time_ns()}.log')
    process=subprocess.Popen([str(game/'SlayTheSpire2.exe'),'--rendering-driver','vulkan','--force-steam=off','--log-file',str(dev/'godot.log')],cwd=game,env=env,startupinfo=startup)
    deadline=time.monotonic()+55
    while time.monotonic()<deadline:
        assert process.poll() is None,'Game exited before MCP load'
        text=log.read_text(encoding='utf-8',errors='replace') if log.exists() else ''
        if '[Karen STS2 MCP]' in text and "Preloading 'Common' Complete:" in text: break
        time.sleep(.3)
    else: raise RuntimeError('MCP/common assets not ready')
    s=wait(lambda s:'continue' in s.get('options',[]))
    post(action='menu_select',option='continue')
    wait(lambda s:s.get('player') and (s.get('battle',{}).get('is_play_phase') or s.get('state_type') in ['map','rewards']))
    save(out/'build',{f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in (mods/'ShoujoKagekiAijoKaren').rglob('*') if f.is_file() and f.suffix in ['.dll','.pck']})
    save(out/'mcp-build',{'sha256':hashlib.sha256((mods/'KarenSTS2MCP.dll').read_bytes()).hexdigest(),'bytes':(mods/'KarenSTS2MCP.dll').stat().st_size})
    if args.audit_capture:
        audit_capture()
        # Warm the card's first-use shader/texture path before performance-sensitive footage.
        cmd('fight WATERFALL_GIANT_BOSS')
        wait(lambda s:s.get('battle',{}).get('is_play_phase') and any(e['entity_id'].startswith('WATERFALL') for e in s['battle']['enemies']))
        time.sleep(.8);cmd('why_probe seed 4 base')
        wait(lambda s:any(c['id']=='KAREN_WHY' for c in s.get('player',{}).get('hand',[])))
        time.sleep(1);cmd('why_probe speed normal');play()
        wait(lambda s:s.get('state_type')=='hand_select');time.sleep(.8)
        post(action='combat_confirm_selection');time.sleep(.6)
        wait(lambda s:s.get('state_type')!='hand_select')
    for name in args.cases.split(','):
        case=out/name;case.mkdir(exist_ok=True)
        cmd('fight WATERFALL_GIANT_BOSS')
        wait(lambda s:s.get('battle',{}).get('is_play_phase') and any(e['entity_id'].startswith('WATERFALL') for e in s['battle']['enemies']))
        time.sleep(.8)
        mode='upgrade' if name=='upgrade' else 'void' if name=='void' else 'base'
        count=0 if name=='empty' else 4
        cmd(f'why_probe seed {count} {mode}')
        wait(lambda s:any(c['id']=='KAREN_WHY' for c in s.get('player',{}).get('hand',[])))
        time.sleep(1)
        if name=='repeat': cmd('card KAREN_WHY Hand');time.sleep(.5)
        cmd('why_probe speed '+(name if name in ['fast','instant'] else 'normal'))
        cmd('why_probe reset');snap(case/'before');hand(case/'before-hand')
        before=probe()['block']
        capture=name in ['base','upgrade','void'] and not args.no_recording
        if capture:
            api('/settings',dict(fullscreen=False,muted=False,window_width=1280,window_height=720))
            api('/recording/start',dict(fps=30,max_duration_seconds=20,width=1280,height=720,audio=True,encoder=args.recording_encoder));recording=True
            time.sleep(.5)
        started=time.monotonic();play()
        expected=0 if name in ['zero','empty','repeat'] else 3 if name=='upgrade' else 2
        if name=='exit':
            wait(lambda s:s.get('state_type')=='hand_select');time.sleep(.85)
            snap(case/'selection');save(case/'selection-probe',probe())
            cmd('fight WATERFALL_GIANT_BOSS')
            wait(lambda s:s.get('battle',{}).get('is_play_phase') and s.get('state_type')!='hand_select')
        elif count: select(expected,case)
        if name=='repeat':
            wait(lambda s:s.get('battle',{}).get('is_play_phase') and s.get('state_type')!='hand_select')
            play();select(1,case);expected=1
        time.sleep(2)
        deadline=time.monotonic()+4
        while probe()['live'] and time.monotonic()<deadline: time.sleep(.15)
        if recording:finish_recording(case)
        snap(case/'after');hand(case/'after-hand');metrics=probe();save(case/'probe',metrics)
        assert metrics['live']==0 and metrics['layers']==0 and all(e['Freed'] for e in metrics['effects']),metrics
        for effect in metrics['effects']:
            assert effect['UnexpectedVisibleFrames']==0 and effect['ScopeCorrect'],metrics
            if name!='empty':
                assert effect['SelectionFrames']>5 and effect['SurfaceFrames']>5,metrics
                assert effect['SurfaceCoversViewport'] and effect['InputIgnored'],metrics
                if name!='exit': assert effect['AdvancingSamples']>2,metrics
            else: assert effect['SurfaceFrames']==0 and effect['SelectionFrames']==0,metrics
        if name!='exit':
            assert metrics['block']-before==(12 if name=='upgrade' else 18 if name=='repeat' else 9),metrics
            assert len(metrics['hand'])==count-expected,metrics
            assert len(metrics['draw'] if name=='void' else metrics['promise'])==expected,metrics
            if name=='instant': assert not metrics['effects'],metrics
            else:
                if name!='empty': assert len(metrics['effects'])==(2 if name=='repeat' else 1),metrics
        if name=='resize': api('/settings',dict(fullscreen=False,window_width=1280,window_height=720))
        results=[result for result in results if result['case']!=name]
        results.append(dict(case=name,passed=True,metrics=metrics,duration=time.monotonic()-started))
        save(out/'results',results);print(args.branch,name,'PASS',flush=True)
finally:
    if recording:
        try:api('/recording/stop',{})
        except Exception:pass
    if process and process.poll() is None:
        process.terminate()
        try:process.wait(10)
        except subprocess.TimeoutExpired:process.kill()
