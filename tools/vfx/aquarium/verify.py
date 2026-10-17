"""Real native card plays through KarenSTS2MCP in owned, isolated game copies."""
import argparse, hashlib, json, os, shutil, socket, subprocess, time, urllib.request
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('branch',choices=['stable','beta']);p.add_argument('--cases',default='normal,stack,full,empty,single,void,fast,instant,remove,cancel,switch,room,pose,combo');p.add_argument('--run',default='checks');p.add_argument('--capture',action='store_true');p.add_argument('--record',default='normal,stack,void');a=p.parse_args()
project=Path(__file__).resolve().parents[3];root=project/'artifacts/aquarium';dev=root/'isolated'/a.branch;game=dev/'game';out=root/a.branch/((a.run+'-visual' if a.run!='checks' else 'visual') if a.capture else a.run);out.mkdir(parents=True,exist_ok=True)
port=20727 if a.branch=='stable' else 20728;base=f'http://127.0.0.1:{port}/api/v1';opener=urllib.request.build_opener(urllib.request.ProxyHandler({}));process=None;recording=False
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
def probe():return json.loads(cmd('aquarium_probe status')['message'])
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
    assert h['modelCount']==h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'] and all(b['valid'] for b in h['burnOverlays']),h
def fight():
    previous=probe()['roomId']
    cmd('fight WATERFALL_GIANT_BOSS')
    wait(lambda s:s.get('battle',{}).get('is_play_phase') and probe()['roomId']!=previous)
    time.sleep(.8)

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
    shutil.copy2(root/'probe'/a.branch/'KarenAquariumProbe.dll',mods)
    save(mods/'KarenAquariumProbe.json',dict(id='KarenAquariumProbe',name='KarenAquariumProbe',version='1.0.0',author='Development',has_dll=True,has_pck=False,affects_gameplay=False,dependencies=[dict(id='ShoujoKagekiAijoKaren',min_version='v0.1.3')]))
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
    wait(lambda s:s.get('player') and (not s.get('battle') or s['battle'].get('is_play_phase')));time.sleep(1)
    save(out/'build.json',{name:hashlib.sha256((package/name).read_bytes()).hexdigest() for name in ['ShoujoKagekiAijoKaren.pck',f'lib/ShoujoKagekiAijoKaren.{a.branch.title()}.dll']})
    for name in a.cases.split(','):
        case=out/name;case.mkdir(exist_ok=True)
        cmd('aquarium_probe speed normal');fight()
        cmd('aquarium_probe seed '+(name if name in ['upgrade','stack','full','empty','single','void','combo'] else 'normal'));cmd('energy 9')
        wait(lambda s:any(c['id']=='KAREN_AQUARIUM_CARD' for c in s['player']['hand']));time.sleep(.6)
        cmd('aquarium_probe watch')
        speed=name if name in ['fast','instant'] else 'normal';cmd('aquarium_probe speed '+speed)
        snap(case/'before');hand(case/'before-hand.json')
        if name in a.record.split(',') and not a.capture:
            api('/settings',dict(muted=False));api('/recording/start',dict(fps=30,max_duration_seconds=40,width=1280,height=720,audio=True));recording=True;time.sleep(.6)
        for i in range(2 if name=='stack' else 1):
            s=state();card=next(c for c in s['player']['hand'] if c['id']=='KAREN_AQUARIUM_CARD')
            post(action='play_card',card_index=card['index'])
            wait(lambda s:sum(c['id']=='KAREN_AQUARIUM_CARD' for c in s.get('player',{}).get('hand',[]))==(1-i if name=='stack' else 0))
            time.sleep(.6)
        if name=='pose':cmd('aquarium_probe pose')
        time.sleep(2.5)
        if a.capture:snap(case/'cast-finished')
        cast=probe();save(case/'cast.json',cast)
        assert cast['amount']==(2 if name=='stack' else 1),cast
        assert cast['live']==0 and cast['layers']==0 and cast['showing']==0,cast
        turns=3 if name=='normal' else 1
        for turn in range(turns):
            if a.capture:snap(case/f'turn-{turn}-before')
            post(action='end_turn')
            s=wait(lambda s:s.get('state_type')=='hand_select' or (s.get('battle',{}).get('is_play_phase') and s['battle']['round']>=turn+2))
            if s.get('state_type')=='hand_select':
                time.sleep(.5)
                if a.capture:snap(case/f'turn-{turn}-selection')
                metrics=probe();save(case/f'turn-{turn}-selection.json',metrics)
                assert metrics['selecting']==(0 if speed=='instant' else 1),metrics
                # A slow native choice must not hold water, jellyfish or bubbles on screen.
                time.sleep(2.5)
                if a.capture:snap(case/f'turn-{turn}-selection-idle')
                idle=probe();save(case/f'turn-{turn}-selection-idle.json',idle)
                assert idle['showing']==0 and idle['emitting']==0 and idle['looping']==0,idle
                assert idle['selecting']==(0 if speed=='instant' else 1),idle
                if name=='remove':cmd('aquarium_probe remove')
                if name=='cancel':cmd('aquarium_probe clearfx')
                if name=='switch':cmd('aquarium_probe speed instant');time.sleep(.4)
                count=min(2 if name=='stack' else 1,len(s['player']['hand']))
                choices=[c for c in s['player']['hand'] if c['id']=='KAREN_DEFEND'][:count]
                for c in choices:post(action='combat_select_card',card_index=c['index'])
                chosen=state()
                if chosen.get('state_type')=='hand_select' and chosen['hand_select']['can_confirm']:post(action='combat_confirm_selection')
                wait(lambda s:s.get('state_type')!='hand_select' and s.get('battle',{}).get('is_play_phase'))
            post(action='unhover_card')
            time.sleep(2.0)
            if a.capture:snap(case/f'turn-{turn}-after')
            hand(case/f'turn-{turn}-hand.json')
        if recording:time.sleep(.5);stop(case);api('/settings',dict(muted=True))
        snap(case/'after');after=probe();save(case/'probe.json',after)
        assert after['selecting']==0,after
        assert after['live']==0 and after['layers']==0 and after['showing']==0 and after['emitting']==0 and after['looping']==0,after
        assert all(e['Exited'] and e['Ignored'] for e in after['effects']),after
        if speed=='instant':assert not after['effects'],after
        else:
            assert after['peakLive']==1,after
            total=lambda key:sum(e[key] for e in after['effects'])
            assert total('Turns')==turns and total('Casts')==(2 if name=='stack' else 1),after
            assert total('Drawn')==(0 if name in ['full','empty','single'] else 2 if name=='stack' else turns),after
            assert total('Moved')==(0 if name in ['empty','cancel','switch','remove'] else 2 if name=='stack' else turns),after
            assert total('ParticleFrames')>0,after
        assert after['promise']==(0 if name in ['empty','void'] else 2 if name=='stack' else turns),after
        if name=='void':assert after['draw']==13,after
        if name=='remove':assert after['amount']==0,after
        if name=='room':
            fight();final=probe();save(case/'cleanup.json',final)
            assert final['live']==0 and final['layers']==0 and final['showing']==0,final
        results.append(dict(case=name,passed=True,turns=turns,metrics=after));save(out/'results.json',results)
        print(a.branch,name,'PASS',flush=True)
finally:
    save(out/'results.json',results)
    if recording:
        try:api('/recording/stop',{})
        except Exception:pass
    if process and process.poll() is None:process.terminate();process.wait(timeout=15)
    if (dev/'godot.log').exists():shutil.copy2(dev/'godot.log',out/'godot.log')
