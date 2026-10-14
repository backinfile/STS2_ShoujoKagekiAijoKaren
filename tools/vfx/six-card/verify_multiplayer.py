"""Targeted Karen host + Ironclad client: real shared reward cast and both views. Isolated loopback only."""
import argparse, ctypes, hashlib, json, os, pathlib, shutil, socket, subprocess, threading, time, urllib.request
from isolated_desktop import TestDesktop
from audit_log import audit

parser=argparse.ArgumentParser()
parser.add_argument('branch',choices=['stable','beta'])
parser.add_argument('--source-repo',type=pathlib.Path,required=True)
parser.add_argument('--output',type=pathlib.Path,required=True)
args=parser.parse_args()
repo=pathlib.Path(__file__).resolve().parents[3]
root=args.output.resolve();root.mkdir(parents=True,exist_ok=True)
source=args.source_repo.resolve()/'artifacts/dev-game'/args.branch
ports=[19037,19038] if args.branch=='stable' else [19047,19048]
for port in ports:
    with socket.socket() as sock: assert sock.connect_ex(('127.0.0.1',port))!=0,'MCP test port occupied'
with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as sock:sock.bind(('127.0.0.1',33771))
processes=[];games=[];recording=False;monitor={'samples':0,'foreground_samples':0};stop=threading.Event();desktop=TestDesktop()

def save(name,data):
    (root/(name+'.json')).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')

def api(side,path,data=None):
    req=urllib.request.Request(f'http://127.0.0.1:{ports[side]}/api/v1'+path,
        data=None if data is None else json.dumps(data).encode(),headers={'Content-Type':'application/json'})
    with urllib.request.urlopen(req,timeout=30) as response:return json.load(response)

def state(side,endpoint='multiplayer'):return api(side,'/'+endpoint+'?format=json')
def post(side,endpoint='multiplayer',**kw):
    result=api(side,'/'+endpoint,kw)
    with (root/'actions.jsonl').open('a',encoding='utf-8') as f:f.write(json.dumps([side,kw,result],ensure_ascii=False)+'\n')
    assert result.get('status')=='ok',(side,kw,result)
    return result
def cmd(side,command):return post(side,action='run_command',command=command)
def probe(side):return json.loads(cmd(side,'six_probe status')['message'])
def snap(side,name):
    save(name,state(side))
    with urllib.request.urlopen(f'http://127.0.0.1:{ports[side]}/api/v1/screenshot',timeout=30) as response:
        (root/(name+'.png')).write_bytes(response.read())
def wait(side,predicate,label,endpoint='multiplayer',seconds=65):
    end=time.monotonic()+seconds;last=None
    while time.monotonic()<end:
        try:
            last=state(side,endpoint)
            if predicate(last):return last
            # New profiles may show the native tutorial prompt before the map.
            if last.get('menu_screen')=='tutorial_prompt':post(side,'singleplayer',action='menu_select',option='no')
        except (OSError,TimeoutError):pass
        time.sleep(.25)
    raise AssertionError((side,label,last))
def link(src,dst):
    try:os.link(src,dst)
    except OSError:shutil.copy2(src,dst)
    return dst

plugins=[]
for name,project in [('KarenSixCardProbe',repo/'tools/vfx/six-card/SixCardProbe.csproj'),
                     ('KarenBackgroundTestGuard',repo/'tools/vfx/catalog/guard/Guard.csproj')]:
    out=root/'plugins'/name
    subprocess.run(['dotnet','build',str(project),f'-p:Game={source/"game"}',f'-p:Variant={args.branch.title()}',
        '-o',str(out),'--nologo','-v:q'],check=True)
    plugins.append((name,out/(name+'.dll')))

for side in range(2):
    dev=root/('host' if side==0 else 'client');game=dev/'game';assert not dev.exists(),'Use a fresh output path'
    shutil.copytree(source/'game',game,copy_function=link,
        ignore=lambda folder,names:['mods','override.cfg'] if pathlib.Path(folder)==source/'game' else [])
    mods=game/'mods';mods.mkdir()
    shutil.copytree(source/'game/mods/BaseLib',mods/'BaseLib')
    shutil.copytree(repo/'artifacts/ShoujoKagekiAijoKaren',mods/'ShoujoKagekiAijoKaren')
    for suffix in ['dll','json']:shutil.copy2(source/f'game/mods/KarenSTS2MCP.{suffix}',mods)
    (mods/'KarenSTS2MCP.conf').write_text(json.dumps(dict(port=ports[side],fullscreen=False,muted=True,
        skip_first_prompt=True,skip_tutorial=True)),encoding='utf-8')
    for name,dll in plugins:
        shutil.copy2(dll,mods)
        (mods/(name+'.json')).write_text(json.dumps(dict(id=name,name=name,version='1.0.0',author='Development',
            description='Isolated six-card observation and loopback transport',has_dll=True,has_pck=False,affects_gameplay=False)),encoding='utf-8')
    profile=60110+side
    settings=dev/f'user/AppData/Roaming/SlayTheSpire2/default/{profile}/settings.save'
    settings.parent.mkdir(parents=True)
    data=json.loads((source/'user/AppData/Roaming/SlayTheSpire2/default/1/settings.save').read_text(encoding='utf-8-sig'))
    data.update(mute_in_background=False,limit_fps_in_background=False)
    settings.write_text(json.dumps(data,ensure_ascii=False),encoding='utf-8')
    (dev/'user/AppData/Local').mkdir(parents=True)
    (game/'override.cfg').write_text('[display]\nwindow/size/mode=1\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n',encoding='utf-8')
    games.append((dev,game,profile))
save('build',{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in
    [repo/'artifacts/ShoujoKagekiAijoKaren/lib'/f'ShoujoKagekiAijoKaren.{args.branch.title()}.dll',repo/'artifacts/ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck']})

u=ctypes.windll.user32;u.GetForegroundWindow.restype=ctypes.c_void_p
u.GetWindowThreadProcessId.argtypes=[ctypes.c_void_p,ctypes.POINTER(ctypes.c_ulong)]
def monitor_loop():
    while not stop.is_set():
        pid=ctypes.c_ulong();u.GetWindowThreadProcessId(u.GetForegroundWindow(),ctypes.byref(pid))
        monitor['samples']+=1
        if any(p.pid==pid.value for p in processes):monitor['foreground_samples']+=1
        stop.wait(.05)

try:
    threading.Thread(target=monitor_loop,daemon=True).start()
    for side,(dev,game,profile) in enumerate(games):
        env=os.environ.copy();env['APPDATA']=str(dev/'user/AppData/Roaming');env['LOCALAPPDATA']=str(dev/'user/AppData/Local')
        env['STS2_MCP_FFMPEG']=shutil.which('ffmpeg')
        processes.append(desktop.launch([str(game/'SlayTheSpire2.exe'),'--rendering-driver','vulkan','--force-steam=off',
            '--fastmp='+('host_standard' if side==0 else 'join'),f'--clientId={profile}','--log-file',str(dev/'godot.log')],
            cwd=game,env=env))
        end=time.monotonic()+80
        while time.monotonic()<end:
            if (dev/'godot.log').exists() and '[Karen STS2 MCP]' in (dev/'godot.log').read_text(encoding='utf-8',errors='replace'):break
            time.sleep(.25)
        else:raise AssertionError('MCP not loaded')
        if side==0:wait(0,lambda s:s.get('menu_screen')=='character_select','host lobby','singleplayer')
    wait(1,lambda s:s.get('lobby',{}).get('player_count')==2,'joined lobby','singleplayer')
    for side,character in [(0,'KAREN'),(1,'IRONCLAD')]:
        post(side,'singleplayer',action='menu_select',option=character)
        wait(side,lambda s:any(p.get('is_local') and p.get('character_id')==character for p in s.get('lobby',{}).get('players',[])),
            'selected character','singleplayer')
    for side in range(2):post(side,'singleplayer',action='menu_select',option='embark')
    for side in range(2):wait(side,lambda s:s.get('state_type')=='map','map')
    for side in range(2):post(side,action='choose_map_node',index=0)
    for side in range(2):wait(side,lambda s:s.get('battle',{}).get('is_play_phase'),'combat')
    time.sleep(2)
    for side in range(2):
        cmd(side,'banana_probe speed normal');cmd(side,'six_probe watch');snap(side,f'{side}-before')
    cmd(0,'card KAREN_SHINE_TOGETHER_ALL Hand')
    ready=wait(0,lambda s:any(c['id']=='KAREN_SHINE_TOGETHER_ALL' for c in s['player']['hand']),'shared shine card')
    time.sleep(1)
    for side in range(2):save(f'{side}-before-probe',probe(side))
    api(0,'/settings',dict(muted=False))
    api(0,'/recording/start',dict(fps=30,max_duration_seconds=40,width=1280,height=720,audio=True));recording=True
    started=time.monotonic();save('chapters',[dict(label='一起闪耀吧 · Karen + 铁甲战士',time=0)])
    time.sleep(.6)
    card=next(c for c in ready['player']['hand'] if c['id']=='KAREN_SHINE_TOGETHER_ALL')
    post(0,action='play_card',card_index=card['index'])
    for _ in range(100):
        if probe(0)['casts']:break
        time.sleep(.04)
    time.sleep(.45)
    snap(0,'host-burst');snap(1,'client-burst')
    for _ in range(120):
        reports=[probe(0),probe(1)]
        if all(r['live']==0 and r['backLive']==0 and r['hudLive']==0 and len(r['casts'])==1 and r['casts'][0]['recipients']==2 for r in reports):break
        time.sleep(.07)
    else:raise AssertionError(('shared cast cleanup',reports))
    for side,r in enumerate(reports):
        assert len(r['party'])==2 and all(p['reward']==1 for p in r['party']),r
        assert r['casts'][0]['kind']=='Shine' and r['casts'][0]['confirmed'],r
        save(f'{side}-after-probe',r);snap(side,f'{side}-after')
        check=json.loads(cmd(side,'karen_check_hand')['message']);save(f'{side}-hand',check)
        assert check['modelCount']==check['visualCount'] and not check['missing'] and not check['unexpected'] and not check['unexpectedPileIcons'],check
    time.sleep(1)
    api(0,'/recording/stop',{})
    for _ in range(180):
        rec=api(0,'/recording')
        if rec['status'] in ('completed','failed'):break
        time.sleep(.2)
    assert rec['status']=='completed' and rec.get('audio_source')=='game_process',rec
    shutil.copy2(rec['output_path'],root/'game.mp4');save('recording',rec);recording=False
    api(0,'/settings',dict(muted=True))
    cmd(0,'kill all')
    for side in range(2):
        reward=wait(side,lambda s:s.get('state_type')=='rewards','post-combat rewards')
        assert sum(item['type']=='special_card' for item in reward['rewards']['items'])==1,reward
        save(f'{side}-rewards',reward)
    save('results',dict(branch=args.branch,players=['KAREN','IRONCLAD'],sharedCasts=reports,rewardsReachedOnBoth=True))
    print(args.branch,'MULTIPLAYER PASS',flush=True)
finally:
    if recording:
        try:api(0,'/recording/stop',{})
        except Exception:pass
    for process in processes:
        if process.poll() is None:process.terminate();process.wait(timeout=15)
    desktop.close()
    stop.set();save('window-monitor',monitor)
    for side,(dev,_,_) in enumerate(games):
        if (dev/'godot.log').exists():shutil.copy2(dev/'godot.log',root/f'{side}-godot.log')
assert monitor['foreground_samples']==0,monitor
for side in range(2):
    diagnostics=audit((root/f'{side}-godot.log').read_text(encoding='utf-8',errors='replace'))
    save(f'{side}-log-audit',diagnostics)
    assert not diagnostics['unexpectedErrors'],dict(count=len(diagnostics['unexpectedErrors']),first=diagnostics['unexpectedErrors'][:8])
