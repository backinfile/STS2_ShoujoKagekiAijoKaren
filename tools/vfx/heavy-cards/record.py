"""Record B02 real card plays through KarenSTS2MCP on a private desktop."""
import argparse, hashlib, json, os, pathlib, shutil, socket, subprocess, sys, time, urllib.request
p = argparse.ArgumentParser()
p.add_argument('environment', choices=['stable','beta'])
p.add_argument('--root', type=pathlib.Path, required=True)
p.add_argument('--package', type=pathlib.Path, required=True)
p.add_argument('--phase', choices=['before','after'], required=True)
p.add_argument('--cases', default='full')
p.add_argument('--cards', default='')
p.add_argument('--port', type=int, required=True)
p.add_argument('--max-body-offset', type=float)
a = p.parse_args()
project = pathlib.Path(__file__).resolve().parents[3]
sys.path.insert(0, str(project/'tools/vfx/six-card'))
from isolated_desktop import TestDesktop
root = a.root.resolve(); dev = root/'isolated'/a.environment; game = dev/'game'; mods = game/'mods'
package = a.package.resolve(); out = root/a.phase/a.environment; out.mkdir(parents=True, exist_ok=True)
base = f'http://127.0.0.1:{a.port}/api/v1'
def save(path, data): path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
def api(path, data=None):
    req = urllib.request.Request(base+path, data=None if data is None else json.dumps(data).encode(), headers={'Content-Type':'application/json'})
    with urllib.request.urlopen(req, timeout=30) as r: return json.load(r)
def state(): return api('/singleplayer?format=json')
def cmd(text): return post(action='run_command', command=text)
def post(**body):
    r = api('/singleplayer', body)
    with (out/'actions.jsonl').open('a', encoding='utf-8') as f: f.write(json.dumps([body,r],ensure_ascii=False)+'\n')
    assert r.get('status') == 'ok', (body,r)
    return r
process = None; desktop = None; recording = False

def wait(predicate, seconds=55):
    deadline = time.monotonic()+seconds; last = None
    while time.monotonic() < deadline:
        assert process.poll() is None, 'Owned game exited'
        try:
            last = state()
            if last.get('prompt_id'):
                options = [x.get('name') if isinstance(x,dict) else x for x in last.get('options',[])]
                choice = next((x for x in ['no','advance','proceed'] if x in options),options[0])
                api('/ui/prompt',dict(option=choice,prompt_id=last['prompt_id']))
            elif predicate(last): return last
        except (OSError, ValueError): pass
        time.sleep(.15)
    save(out/'timeout.json', last); raise TimeoutError(last)

def hand_check():
    deadline=time.monotonic()+40
    while True:
        r=api('/singleplayer',dict(action='run_command',command='karen_check_hand'))
        if r.get('status')=='ok':break
        assert 'queued actions' in r.get('message','') and time.monotonic()<deadline,r
        time.sleep(.2)
    h=json.loads(r['message'])
    assert h['modelCount']==h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'],h
    return r

def snapshot(folder, name):
    save(folder/(name+'.json'),state())
    with urllib.request.urlopen(base+'/screenshot',timeout=30) as r: (folder/(name+'.png')).write_bytes(r.read())

with socket.socket() as check:
    assert check.connect_ex(('127.0.0.1',a.port)) != 0, 'Port occupied; no deployment performed'
# Copy only into this run's isolated environment. Source game hardlinks stay read-only.
shutil.copytree(package, mods/'ShoujoKagekiAijoKaren', dirs_exist_ok=True)
probe = root/'probe'/a.environment
subprocess.run(['dotnet','build',str(pathlib.Path(__file__).with_name('Probe.csproj')),
    '-p:Game='+str(game),'-p:Package='+str(package),'-p:Variant='+a.environment.title(),
    '-o',str(probe),'--nologo','-v:q','-clp:ErrorsOnly'],check=True)
shutil.copy2(probe/'KarenHeavyCardsProbe.dll',mods/'KarenHeavyCardsProbe.dll')
save(mods/'KarenHeavyCardsProbe.json',dict(id='KarenHeavyCardsProbe',name='KarenHeavyCardsProbe',version='1.0.0',
    author='Development',has_dll=True,has_pck=False,affects_gameplay=False))
save(mods/'KarenSTS2MCP.conf',dict(port=a.port,fullscreen=False,muted=True,skip_tutorial=True,
    skip_first_prompt=True,window_width=1280,window_height=720,silent_test=True))
for path in (dev/'user').rglob('settings.save'):
    settings=json.loads(path.read_text(encoding='utf-8-sig'))
    settings.update(fullscreen=False,mute_in_background=False,limit_fps_in_background=False,
        seen_ea_disclaimer=True,skip_intro_logo=True,window_size={'X':1280,'Y':720})
    save(path,settings)
override=game/'override.cfg'
if override.exists():
    assert override.read_text(encoding='utf-8') == '[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n'
else:
    override.write_text('[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n',encoding='utf-8')
build={name:hashlib.sha256((package/name).read_bytes()).hexdigest() for name in
    ['lib/ShoujoKagekiAijoKaren.'+a.environment.title()+'.dll','ShoujoKagekiAijoKaren.pck']}
save(out/'build.json',build)
results=[r for r in json.loads((out/'results.json').read_text(encoding='utf-8')) if r['mode'] not in a.cases.split(',')] if (out/'results.json').exists() else []
try:
    desktop=TestDesktop();env=os.environ.copy()
    env.update(APPDATA=str(dev/'user/AppData/Roaming'),LOCALAPPDATA=str(dev/'user/AppData/Local'),STS2_MCP_FFMPEG=shutil.which('ffmpeg'))
    process=desktop.launch([str(game/'SlayTheSpire2.exe'),'--rendering-driver','vulkan','--force-steam=off','--log-file',str(out/'godot.log')],cwd=game,env=env)
    wait(lambda s: 'continue' in s.get('options',[]) or s.get('player'), 90)
    log=out/'godot.log'
    assert '[Karen STS2 MCP]' in log.read_text(encoding='utf-8',errors='replace')
    deadline=time.monotonic()+100
    while "Preloading 'Common' Complete:" not in log.read_text(encoding='utf-8',errors='replace'):
        assert time.monotonic()<deadline, 'Common preloading timeout'; time.sleep(.25)
    settings=api('/settings',dict(fullscreen=False,muted=True,window_width=1280,window_height=720))
    save(out/'settings.json',settings)
    assert settings.get('silent_test') and not settings.get('silent_test_error'),settings
    if not state().get('player'): post(action='menu_select',option='continue')
    wait(lambda s: s.get('player') and (s.get('battle',{}).get('is_play_phase') or s.get('state_type') in ('map','rewards')))
    time.sleep(1)
    cards=['KAREN_SHINE_TOGETHER','KAREN_NONON','KAREN_SHINE_STRIKE_BARRAGE','KAREN_BACK_TO_BACK_CARD','KAREN_BASEBALL','KAREN_JEALOUSY','KAREN_CARRYING_GUILT']
    for mode in a.cases.split(','):
        quality='Low' if mode in ('low','budget_low') else 'Off' if mode=='off' else 'Full'
        speed=mode if mode in ('fast','instant') else 'normal'
        chosen=a.cards.split(',') if a.cards else {'upgrade':['KAREN_SHINE_STRIKE_BARRAGE'],'stock':['KAREN_BACK_TO_BACK_CARD'],'stress':['KAREN_SHINE_TOGETHER'],'area':['KAREN_NONON'],'replay':['KAREN_CARRYING_GUILT'],'hover':['KAREN_CARRYING_GUILT'],'mirror':['KAREN_JEALOUSY'],'cancel':['KAREN_NONON'],'budget':['KAREN_SHINE_TOGETHER'],'budget_low':['KAREN_SHINE_TOGETHER'],'fast':['KAREN_SHINE_STRIKE_BARRAGE'],'instant':['KAREN_SHINE_STRIKE_BARRAGE'],'off':['KAREN_NONON','KAREN_CARRYING_GUILT']}.get(mode,cards)
        for cardid in chosen:
            folder=out/(mode+'-'+cardid.lower()); folder.mkdir(exist_ok=True)
            cmd('heavy_probe speed normal'); cmd('heavy_probe quality '+quality)
            cmd('fight '+('KNIGHTS_ELITE' if mode in ('area','budget','budget_low') else 'WATERFALL_GIANT_BOSS'))
            wait(lambda s:s.get('battle',{}).get('is_play_phase'))
            time.sleep(1)
            cleanup=json.loads(cmd('heavy_probe status')['message'])
            save(folder/'room-reset.json',cleanup)
            assert not cleanup['effects'],cleanup
            assert all(cleanup['exited'].get(k,0)>=v for k,v in cleanup['spawned'].items()),cleanup
            snapshot(folder,'fixture-before')
            seedmode=mode if mode in ('upgrade','last','stress','stock','replay') else ''
            cmd('heavy_probe seed '+cardid+' '+seedmode)
            wait(lambda s:len(s['player']['hand'])==1 and any(c['id']==cardid for c in s['player']['hand']))
            if mode=='mirror':cmd('heavy_probe mirror')
            if mode=='lethal':cmd('heavy_probe lethal')
            if mode=='hover':
                cmd('heavy_probe focus');time.sleep(.5)
                cmd('heavy_probe focus');time.sleep(.3)
                cmd('heavy_probe unfocus');time.sleep(.5)
            cmd('energy 9');cmd('heavy_probe speed '+speed);time.sleep(1)
            before=state();snapshot(folder,'before')
            before_status=json.loads(cmd('heavy_probe status')['message']);save(folder/'before-status.json',before_status)
            selected=next(c for c in before['player']['hand'] if c['id']==cardid)
            hand=hand_check(); save(folder/'before-hand.json',hand)
            action=dict(action='play_card',card_index=selected['index'])
            if selected['target_type']=='AnyEnemy': action['target']=before['battle']['enemies'][0]['entity_id']
            # Video captures engine audio while the physical output remains silent.
            api('/settings',dict(muted=False))
            api('/recording/start',dict(fps=30,max_duration_seconds=50,width=1280,height=720,audio=True,encoder='auto'));recording=True
            time.sleep(.8)
            begin=time.monotonic();post(**action)
            if mode=='cancel':
                time.sleep(.15);cmd('heavy_probe clearfx')
            if mode in ('budget','budget_low'):
                time.sleep(.6);cmd('heavy_probe budget')
            time.sleep(11 if mode=='stress' else 4.5 if mode=='replay' else 3.1)
            settled_hand=hand_check()
            after=state(); status=json.loads(cmd('heavy_probe status')['message'])
            time.sleep(.4)
            api('/recording/stop',{})
            deadline=time.monotonic()+35
            while True:
                rec=api('/recording')
                if rec['status'] in ('completed','failed'):break
                assert time.monotonic()<deadline,rec;time.sleep(.2)
            assert rec['status']=='completed',rec
            recording=False;shutil.copy2(rec['output_path'],folder/'game.mp4');save(folder/'recording.json',rec)
            api('/settings',dict(muted=True));snapshot(folder,'after');save(folder/'status.json',status)
            save(folder/'after-hand.json',settled_hand)
            damage=sum(e['hp'] for e in before['battle']['enemies'])-sum(e['hp'] for e in after['battle']['enemies'])
            block=after['player']['block']-before['player']['block']
            assert not [e for e in status['effects'] if e['type']!='NKarenUnforgottenShineVfx'],status
            assert status['hand']==0 and not status.get('poseActive',False),status
            if 'bodyZ' in status:assert status['bodyZ']==before_status['bodyZ'] and status['bodyRelative']==before_status['bodyRelative'],status
            if a.phase=='after' and 'NKarenStageAttackVfx' in status['spawned'] and mode!='cancel':assert status['poseFrames']>0 and status['maxAttackOffset']>(1 if a.max_body_offset is not None else 5),status
            if a.phase=='after' and a.max_body_offset is not None:assert status['maxAttackOffset']<=a.max_body_offset,status
            if cardid=='KAREN_BASEBALL':
                powers={p['id']:p['amount'] for p in status['powers']}
                assert sum(1 for amount in powers.values() if amount==2)>=4,powers
            if cardid=='KAREN_SHINE_TOGETHER' and a.phase=='after':
                assert status['scars']<=((6 if quality=='Low' else 12) if mode in ('budget','budget_low') else (2 if quality=='Low' else 4)),status
                if mode in ('budget','budget_low'):assert status['reuseStable'],status
            if quality=='Off' or speed=='instant':assert not status['spawned'],status
            result=dict(mode=mode,card=cardid,quality=quality,speed=speed,damage=damage,block=block,passed=True,
                elapsed=time.monotonic()-begin,recording=rec,build=build,status=status,before_status=before_status)
            results.append(result);save(out/'results.json',results)
            print(a.phase,a.environment,mode,cardid,'passed',flush=True)
finally:
    if recording:
        try:api('/recording/stop',{})
        except Exception:pass
    if desktop:desktop.close()
    save(out/'results.json',results)
