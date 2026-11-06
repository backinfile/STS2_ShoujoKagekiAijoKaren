"""Real native card plays through KarenSTS2MCP in owned, isolated game copies."""
import argparse, hashlib, json, os, shutil, socket, subprocess, time, urllib.request
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('branch',choices=['stable','beta']);p.add_argument('--cases',default='normal,upgrade,last,fast,instant,replay,stage,pose,cancel,lethal');p.add_argument('--run',default='checks');p.add_argument('--capture',action='store_true');p.add_argument('--record',default='normal,upgrade,replay');a=p.parse_args()
project=Path(__file__).resolve().parents[3];root=project/'artifacts/shine-barrage';dev=root/'isolated'/a.branch;game=dev/'game';out=root/a.branch/((a.run+'-visual' if a.run!='checks' else 'visual') if a.capture else a.run);out.mkdir(parents=True,exist_ok=True)
port=20627 if a.branch=='stable' else 20628;base=f'http://127.0.0.1:{port}/api/v1';opener=urllib.request.build_opener(urllib.request.ProxyHandler({}));process=None;recording=False
with socket.socket() as s:assert s.connect_ex(('127.0.0.1',port))!=0,'Test port occupied'
def save(path,value):path.write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8')
def api(path,data=None):
    r=urllib.request.Request(base+path,data=None if data is None else json.dumps(data).encode(),headers={'Content-Type':'application/json'})
    with opener.open(r,timeout=30) as response:return json.load(response)
def state():return api('/singleplayer?format=json')
def post(**body):
    r=api('/singleplayer',body)
    with (out/'actions.jsonl').open('a',encoding='utf-8') as f:f.write(json.dumps([body,r],ensure_ascii=False)+'\n')
    assert r.get('status')=='ok',(body,r)
    return r
def cmd(text):return post(action='run_command',command=text)
def probe():return json.loads(cmd('barrage_probe status')['message'])
def wait(test,timeout=35):
    deadline=time.monotonic()+timeout
    while time.monotonic()<deadline:
        assert process.poll() is None,'Owned game exited'
        try:s=state()
        except (OSError,ValueError):time.sleep(.15);continue
        if s.get('prompt_id'):
            opts=[o.get('name') if isinstance(o,dict) else o for o in s.get('options',[])];api('/ui/prompt',dict(option=next((o for o in ['no','advance','proceed'] if o in opts),opts[0]),prompt_id=s['prompt_id']))
        elif test(s):return s
        time.sleep(.12)
    save(out/'timeout.json',state());raise TimeoutError('State timeout')
def snap(path):
    save(path.with_suffix('.json'),state())
    with opener.open(base+'/screenshot',timeout=30) as response:path.with_suffix('.png').write_bytes(response.read())
def hand(path):
    h=json.loads(cmd('karen_check_hand')['message']);save(path,h)
    assert h['modelCount']==h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'],h
def hp(s):return sum(e['hp'] for e in s.get('battle',{}).get('enemies',[]))
def stop(case):
    global recording
    api('/recording/stop',{});deadline=time.monotonic()+40
    while time.monotonic()<deadline:
        result=api('/recording')
        if result['status'] in ['completed','failed']:break
        time.sleep(.25)
    assert result['status']=='completed',result
    shutil.copy2(result['output_path'],case/'game.mp4');save(case/'recording.json',result);recording=False
results=json.loads((out/'results.json').read_text(encoding='utf-8')) if (out/'results.json').exists() else []
results=[r for r in results if r['case'] not in a.cases.split(',')]
try:
    package=project/'artifacts/ShoujoKagekiAijoKaren';mods=game/'mods';shutil.copytree(package,mods/'ShoujoKagekiAijoKaren',dirs_exist_ok=True)
    subprocess.run(['dotnet','build',str(Path(__file__).with_name('Probe.csproj')),'-p:Game='+str(game),'-p:Variant='+a.branch.title(),'-o',str(root/'probe'/a.branch),'--nologo','-v:q','-clp:ErrorsOnly'],check=True)
    shutil.copy2(root/'probe'/a.branch/'KarenBarrageProbe.dll',mods)
    save(mods/'KarenBarrageProbe.json',dict(id='KarenBarrageProbe',name='KarenBarrageProbe',version='1.0.0',author='Development',has_dll=True,has_pck=False,affects_gameplay=False,dependencies=[dict(id='ShoujoKagekiAijoKaren',min_version='v0.1.3')]))
    save(mods/'KarenSTS2MCP.conf',dict(port=port,fullscreen=False,muted=True,skip_tutorial=True,skip_first_prompt=True,window_width=1280,window_height=720))
    for settings in (dev/'user').rglob('settings.save'):
        data=json.loads(settings.read_text(encoding='utf-8-sig'));data.update(fullscreen=False,volume_master=.5,volume_sfx=.85,volume_bgm=.15,mute_in_background=False,limit_fps_in_background=False,seen_ea_disclaimer=True,skip_intro_logo=True);save(settings,data)
    override=game/'override.cfg'
    # Unlink the copied game's hardlink before creating a private override.
    if override.exists():override.unlink()
    override.write_text('[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n',encoding='utf-8')
    env=os.environ.copy();env.update(APPDATA=str(dev/'user/AppData/Roaming'),LOCALAPPDATA=str(dev/'user/AppData/Local'),STS2_MCP_FFMPEG=shutil.which('ffmpeg') or '')
    startup=subprocess.STARTUPINFO();startup.dwFlags=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
    log=dev/'godot.log';log.unlink(missing_ok=True);launch=(out/'launch.log').open('w',encoding='utf-8');process=subprocess.Popen([str(game/'SlayTheSpire2.exe'),'--rendering-driver','vulkan','--force-steam=off','--log-file',str(log)],cwd=game,env=env,startupinfo=startup,stdout=launch,stderr=subprocess.STDOUT)
    deadline=time.monotonic()+150
    while time.monotonic()<deadline:
        assert process.poll() is None,'Game exited at startup'
        text=log.read_text(encoding='utf-8',errors='replace') if log.exists() else ''
        if '[Karen STS2 MCP]' in text and "Preloading 'Common' Complete:" in text:break
        time.sleep(.3)
    else:raise TimeoutError('MCP not ready')
    s=wait(lambda s:'continue' in s.get('options',[]) or s.get('player'))
    if not s.get('player'):post(action='menu_select',option='continue')
    wait(lambda s:s.get('player'));time.sleep(1)
    save(out/'build.json',{name:hashlib.sha256((package/name).read_bytes()).hexdigest() for name in ['ShoujoKagekiAijoKaren.pck',f'lib/ShoujoKagekiAijoKaren.{a.branch.title()}.dll']})
    # Warm the new shader/particle pipelines before recording; retain the first cold play in logs.
    cmd('fight WATERFALL_GIANT_BOSS');wait(lambda s:s.get('battle',{}).get('is_play_phase'));time.sleep(.7)
    cmd('barrage_probe seed normal');cmd('energy 9');wait(lambda s:any(c['id']=='KAREN_SHINE_STRIKE_BARRAGE' for c in s['player']['hand']));time.sleep(.5)
    warm=state();warmcard=next(c for c in warm['player']['hand'] if c['id']=='KAREN_SHINE_STRIKE_BARRAGE')
    post(action='play_card',card_index=warmcard['index'],target=warm['battle']['enemies'][0]['entity_id']);time.sleep(2)
    for name in a.cases.split(','):
        case=out/name;case.mkdir(exist_ok=True);cmd('barrage_probe speed normal');cmd('fight WATERFALL_GIANT_BOSS')
        wait(lambda s:s.get('battle',{}).get('is_play_phase') and any(e['entity_id'].startswith('WATERFALL') for e in s['battle']['enemies']));time.sleep(.8)
        cmd('barrage_probe seed '+(name if name in ['upgrade','last','replay','stage'] else 'normal'));cmd('energy 9')
        wait(lambda s:any(c['id']=='KAREN_SHINE_STRIKE_BARRAGE' for c in s['player']['hand']));time.sleep(.7)
        if name in ['pose','lethal']:cmd('barrage_probe '+name)
        speed=name if name in ['fast','instant'] else 'normal';cmd('barrage_probe speed '+speed)
        cmd('barrage_probe watch'+(' '+(case/'contact.png').as_posix() if a.capture else ''));time.sleep(.2)
        snap(case/'before');hand(case/'before-hand.json');before=state();stock_before=probe()['shineStock'];card=next(c for c in before['player']['hand'] if c['id']=='KAREN_SHINE_STRIKE_BARRAGE')
        if name in a.record.split(',') and not a.capture:
            api('/settings',dict(muted=False));api('/recording/start',dict(fps=30,max_duration_seconds=15,width=1280,height=720,audio=True));recording=True;time.sleep(.6)
        started=time.monotonic();post(action='play_card',card_index=card['index'],target=before['battle']['enemies'][0]['entity_id'])
        if name=='cancel':time.sleep(.30);cmd('barrage_probe clearfx')
        plays=3 if name=='replay' else 2 if name=='stage' else 1;hits=(5 if name=='upgrade' else 4)*plays;expected=min(hp(before),hits*3)
        wait(lambda s:hp(before)-hp(s)==expected and not any(c['id']=='KAREN_SHINE_STRIKE_BARRAGE' for c in s.get('player',{}).get('hand',[])))
        time.sleep(1.5);status=probe();after=state()
        if recording:time.sleep(.4);stop(case);api('/settings',dict(muted=True))
        snap(case/'after')
        if name!='lethal':hand(case/'after-hand.json')
        save(case/'probe.json',status);effects=status['effects'];impacts=[e for e in effects if e['impact']]
        assert status['live']==0 and status['childLive']==0 and all(e['exited'] and e['ownedChildren']==2 for e in effects),status
        if speed=='instant':assert not effects,status
        elif name!='cancel':
            assert len(impacts)==(2 if name=='lethal' else hits),status
            assert all(e['maxAnchorError']<2 for e in effects),status
            assert all(e['particleFrames']>0 for e in impacts),status
            if name!='lethal':assert all(e['maxBeyondSilhouette']>30 for e in impacts),status
            assert sum(e['final'] for e in impacts)==plays,status
        if name=='last':assert status['shineStock']==stock_before+1,status
        elapsed=time.monotonic()-started
        results.append(dict(case=name,passed=True,damage=hp(before)-hp(after),expected=expected,impacts=len(impacts),live=status['live'],elapsed=elapsed));save(out/'results.json',results)
        print(a.branch,name,'PASS',results[-1],flush=True)
finally:
    save(out/'results.json',results)
    if recording:
        try:api('/recording/stop',{})
        except Exception:pass
    if process and process.poll() is None:process.terminate();process.wait(timeout=15)
    if (dev/'godot.log').exists():shutil.copy2(dev/'godot.log',out/'godot.log')
