"""Targeted real-card tests for persistent star cuts. Runs only owned isolated games."""
import argparse, hashlib, json, math, os, shutil, socket, subprocess, sys, time, urllib.request
from pathlib import Path

p=argparse.ArgumentParser()
p.add_argument('branch', choices=['stable','beta'])
p.add_argument('--cases', default='ordinary,stack,last,upgrade,fast,instant,pose,replay,lethal,turn,room')
p.add_argument('--record', default='none')
p.add_argument('--run', default='checks')
p.add_argument('--capture-stages', action='store_true')
a=p.parse_args()
project=Path(__file__).resolve().parents[3]
root=project/'artifacts/unforgettable-persistent-20261018'
dev=root/'isolated'/a.branch;game=dev/'game'
out=root/a.branch/((a.run+'-visual') if a.capture_stages else a.run)
out.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(project/'tools/vfx/six-card'))
from isolated_desktop import TestDesktop
port=20937 if a.branch=='stable' else 20938
base=f'http://127.0.0.1:{port}/api/v1'
opener=urllib.request.build_opener(urllib.request.ProxyHandler({}))
with socket.socket() as check: assert check.connect_ex(('127.0.0.1',port))!=0,'Occupied test port'
desktop=None;process=None;recording=False
def save(path,data):path.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
def api(path,data=None):
    request=urllib.request.Request(base+path,data=None if data is None else json.dumps(data).encode(),headers={'Content-Type':'application/json'})
    with opener.open(request,timeout=30) as response:return json.load(response)
def state():return api('/singleplayer?format=json')
def post(**data):
    r=api('/singleplayer',data)
    with (out/'actions.jsonl').open('a',encoding='utf-8') as f:f.write(json.dumps([data,r],ensure_ascii=False)+'\n')
    assert r.get('status')=='ok',(data,r)
    return r
def cmd(command):return post(action='run_command',command=command)
def probe():return json.loads(cmd('scar_probe status')['message'])
def wait(test,timeout=30):
    end=time.monotonic()+timeout
    while time.monotonic()<end:
        assert process.poll() is None,'Owned game exited'
        s=state()
        if s.get('prompt_id'):
            options=[o.get('name') if isinstance(o,dict) else o for o in s.get('options',[])]
            api('/ui/prompt',dict(option=next((o for o in ['no','advance','proceed'] if o in options),options[0]),prompt_id=s['prompt_id']))
        elif test(s):return s
        time.sleep(.12)
    save(out/'timeout.json',state());raise TimeoutError('Gameplay state timeout')
def snap(path):
    save(path.with_suffix('.json'),state())
    with opener.open(base+'/screenshot',timeout=30) as response:path.with_suffix('.png').write_bytes(response.read())
def hand(path):
    h=json.loads(cmd('karen_check_hand')['message']);save(path,h)
    assert h['modelCount']==h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'],h
def fight(encounter='WATERFALL_GIANT_BOSS'):
    previous=probe()['roomId']
    cmd('fight '+encounter)
    wait(lambda s:s.get('battle',{}).get('is_play_phase') and probe()['roomId']!=previous)
    time.sleep(.6)
def cards(s):return [c for c in s.get('player',{}).get('hand',[]) if c['id']=='KAREN_SHINE_TOGETHER']
def play(target,expected,case,stages=False):
    s=state();before=len(cards(s))
    post(action='play_card',card_index=cards(s)[0]['index'],target=target)
    deadline=time.monotonic()+12;samples=[];captured=set()
    while time.monotonic()<deadline:
        metrics=probe();samples.append(dict(t=time.monotonic(),metrics=metrics))
        if stages and metrics['retained']>=expected and metrics['flashes']:
            value=metrics['roots'][-1]['fading']
            label='hit' if value and max(value)>.85 else 'fading' if value and .20<max(value)<.55 else None
            if label and label not in captured:snap(case/label);captured.add(label)
        if metrics['retained']==expected and metrics['flashes']==0 and len(cards(state()))==before-1:break
        time.sleep(.09)
    else:raise AssertionError(('Attack/fade did not finish',probe()))
    save(case/('samples-'+str(expected)+'.json'),samples)
    assert metrics['live']==1 and metrics['retained']==expected and metrics['flashes']==0,metrics
    assert all(abs(r['opacity']-.2)<.001 and r['MaxDrift']<.02 for r in metrics['roots'] if not r['Exited']),metrics
    assert all(m['texture'].endswith('/lingering-star.png') for r in metrics['roots'] if not r['Exited'] for m in r['marks']),metrics
    return metrics
def stop_record(case):
    global recording
    api('/recording/stop',{})
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        result=api('/recording')
        if result['status'] in ['completed','failed']:break
        time.sleep(.2)
    assert result['status']=='completed' and result.get('audio_source')=='game_process',result
    shutil.copy2(result['output_path'],case/'game.mp4');save(case/'recording.json',result);recording=False
    api('/settings',dict(muted=True))

results=json.loads((out/'results.json').read_text(encoding='utf-8')) if (out/'results.json').exists() else []
try:
    mods=game/'mods';package=project/'artifacts/ShoujoKagekiAijoKaren'
    shutil.copytree(package,mods/'ShoujoKagekiAijoKaren',dirs_exist_ok=True)
    subprocess.run(['dotnet','build',str(Path(__file__).with_name('ScarProbe.csproj')),'-p:Variant='+a.branch.title(),'-p:Game='+str(game),'-o',str(root/'probe'/a.branch),'--nologo','-v:q','-clp:ErrorsOnly'],check=True)
    shutil.copy2(root/'probe'/a.branch/'KarenScarProbe.dll',mods)
    save(mods/'KarenScarProbe.json',dict(id='KarenScarProbe',name='KarenScarProbe',version='1.0.0',author='Development',has_dll=True,has_pck=False,affects_gameplay=False,dependencies=[dict(id='ShoujoKagekiAijoKaren',min_version='v0.1.3')]))
    save(mods/'KarenSTS2MCP.conf',dict(port=port,fullscreen=False,muted=True,skip_tutorial=True,skip_first_prompt=True,window_width=1280,window_height=720))
    for settings in (dev/'user').rglob('settings.save'):
        data=json.loads(settings.read_text(encoding='utf-8-sig'))
        data.update(fullscreen=False,volume_master=.5,volume_sfx=.85,volume_bgm=.15,mute_in_background=False,limit_fps_in_background=False,seen_ea_disclaimer=True,skip_intro_logo=True)
        save(settings,data)
    override=game/'override.cfg'
    if override.exists():override.unlink()
    override.write_text('[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n',encoding='utf-8')
    env=os.environ.copy();env.update(APPDATA=str(dev/'user/AppData/Roaming'),LOCALAPPDATA=str(dev/'user/AppData/Local'),STS2_MCP_FFMPEG=shutil.which('ffmpeg') or '')
    log=dev/'godot.log'
    if log.exists():log.unlink()
    desktop=TestDesktop()
    process=desktop.launch([str(game/'SlayTheSpire2.exe'),'--rendering-driver','vulkan','--force-steam=off','--log-file',str(log)],game,env)
    save(out/'process.json',dict(pid=process.pid,port=port,game=str(game),privateDesktop=desktop.name))
    deadline=time.monotonic()+160
    while time.monotonic()<deadline:
        assert process.poll() is None,'Owned game exited at startup'
        text=log.read_text(encoding='utf-8',errors='replace') if log.exists() else ''
        if '[Karen STS2 MCP]' in text and "Preloading 'Common' Complete:" in text:break
        time.sleep(.3)
    else:raise TimeoutError('MCP startup log not ready')
    s=wait(lambda s:'continue' in s.get('options',[]) or s.get('player'))
    if not s.get('player'):post(action='menu_select',option='continue')
    wait(lambda s:s.get('player') and (not s.get('battle') or s['battle'].get('is_play_phase')))
    save(out/'build.json',{name:hashlib.sha256((package/name).read_bytes()).hexdigest() for name in ['ShoujoKagekiAijoKaren.pck',f'lib/ShoujoKagekiAijoKaren.{a.branch.title()}.dll']})
    for name in a.cases.split(','):
        case=out/name;case.mkdir(exist_ok=True)
        cmd('scar_probe speed normal')
        fight('CULTISTS_NORMAL' if name in ['lethal','showcase'] else 'WATERFALL_GIANT_BOSS')
        snap(case/'setup-before');hand(case/'setup-before-hand.json')
        mode='replay' if name=='instant-replay' else 'stack' if name=='fast-stack' else name if name in ['last','upgrade','replay','stack'] else 'stack' if name=='showcase' else 'ordinary'
        cmd('scar_probe seed '+mode);cmd('energy 99')
        wait(lambda s:len(cards(s))==(3 if mode=='stack' else 1));time.sleep(.9)
        if name in ['fast','instant','fast-stack','instant-replay']:cmd('scar_probe speed '+('fast' if name=='fast-stack' else 'instant' if name=='instant-replay' else name))
        if name=='pose':cmd('scar_probe pose')
        if name in ['lethal','showcase']:cmd('scar_probe lethal')
        cmd('scar_probe watch')
        snap(case/'before');hand(case/'before-hand.json');initial=probe();save(case/'initial.json',initial)
        if name in a.record.split(',') and not a.capture_stages:
            api('/settings',dict(muted=False));api('/recording/start',dict(fps=30,max_duration_seconds=40,width=1280,height=720,audio=True));recording=True;time.sleep(.6)
        s=state();target=s['battle']['enemies'][0]['entity_id'];before_hp=s['battle']['enemies'][0]['hp']
        expected=2 if name in ['replay','instant-replay'] else 1
        metrics=play(target,expected,case,a.capture_stages)
        if name not in ['lethal','showcase']:
            actual=next(e['hp'] for e in state()['battle']['enemies'] if e['entity_id']==target)
            assert before_hp-actual==(40 if name=='upgrade' else 30)*(2 if name in ['replay','instant-replay'] else 1),(name,before_hp,actual)
        if name in ['stack','fast-stack','showcase']:
            time.sleep(2)
            s=state();second=next((e for e in s['battle']['enemies'] if e['entity_id']!=target and e['hp']>0),s['battle']['enemies'][0])
            metrics=play(second['entity_id'],2,case)
            if name in ['stack','fast-stack']:metrics=play(second['entity_id'],3,case)
            expected=3 if name in ['stack','fast-stack'] else 2
        if name in ['last','upgrade','replay','instant-replay']:
            bonus=4 if name=='upgrade' else 3
            wait(lambda s:probe()['extraRewards']-initial['extraRewards']==bonus)
            metrics=probe()
            assert metrics['rewardPower']-initial['rewardPower']==bonus,metrics
            assert metrics['shinePile']==initial['shinePile']+1,metrics
        elif name!='showcase':
            assert metrics['extraRewards']==initial['extraRewards'],metrics
        if name in ['instant','instant-replay']:assert all(r['MaxFlashes']==0 for r in metrics['roots']),metrics
        if name=='pose':
            old=metrics['roots'][-1]['marks'];cmd('scar_probe shift');time.sleep(.6)
            assert probe()['roots'][-1]['marks']==old,'Scar followed target'
        if name in ['lethal','showcase']:
            wait(lambda s:len([e for e in s['battle']['enemies'] if e['hp']>0])==1)
        if name in ['turn','showcase']:
            round_before=state()['battle']['round'];post(action='end_turn')
            wait(lambda s:s.get('battle',{}).get('is_play_phase') and s['battle']['round']>round_before)
            time.sleep(2)
        else:time.sleep(2.0)
        final=probe();save(case/'retained.json',final)
        assert final['live']==1 and final['retained']==expected and final['flashes']==0,final
        assert all(r['MaxDrift']<.02 and r['MaxRotationDrift']<.0001 and r['MaxFlashMismatch']<.02 for r in final['roots']),final
        if a.run=='jitter' and expected>1:
            marks=final['roots'][-1]['marks']
            distances=[math.dist(x['anchor'],y['anchor']) for i,x in enumerate(marks) for y in marks[i+1:]]
            assert min(distances)>2,(name,'Scars still overlap',marks)
            assert all(abs(m['rotation'])<=math.radians(10)+.0001 for m in marks),(name,'Rotation outside range',marks)
            save(case/'separation.json',dict(minCenterDistance=min(distances),anglesDegrees=[math.degrees(m['rotation']) for m in marks]))
        if recording:stop_record(case)
        snap(case/'retained');hand(case/'after-hand.json')
        if name=='room':
            fight()
        else:
            cmd('win')
            wait(lambda s:s.get('state_type')=='rewards')
            time.sleep(1.0)
        time.sleep(.4);clean=probe();save(case/'clean.json',clean)
        assert clean['live']==0 and all(r['Exited'] for r in clean['roots']),clean
        snap(case/'finished')
        results=[r for r in results if r['case']!=name]
        results.append(dict(case=name,passed=True,retained=final,clean=clean))
        save(out/'results.json',results)
        print(a.branch,name,'PASS',flush=True)
finally:
    if recording:
        try:api('/recording/stop',{})
        except Exception:pass
    if desktop:desktop.close()
    if (dev/'godot.log').exists():shutil.copy2(dev/'godot.log',out/('godot-'+time.strftime('%Y%m%d-%H%M%S')+'.log'))
