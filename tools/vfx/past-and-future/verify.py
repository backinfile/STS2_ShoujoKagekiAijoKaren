"""Targeted passive-trace integration checks in disposable, isolated Karen games."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import sys
import time
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
parser.add_argument('--source-dev', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--port', type=int, required=True)
parser.add_argument('--visibility-only', action='store_true')
args = parser.parse_args()
project = Path(__file__).resolve().parents[3]
root = args.output.resolve()
branch = args.branch
variant = branch.title()
source = args.source_dev.resolve() / branch
out = root / branch
out.mkdir(parents=True, exist_ok=True)
with socket.socket() as sock:
    assert sock.connect_ex(('127.0.0.1', args.port)) != 0, 'Isolated test port is occupied'
subprocess.run([sys.executable, str(project/'tools/vfx/catalog/prepare.py'), branch,
    '--output', str(root), '--source-dev', str(args.source_dev.resolve()), '--port', str(args.port), '--skip-build'], check=True)
dev = root / 'isolated' / branch
game = dev / 'game'
mods = game / 'mods'
package = project/'artifacts/ShoujoKagekiAijoKaren'
shutil.copytree(package, mods/'ShoujoKagekiAijoKaren')
for csproj, name in [('tools/promise-transform/KarenPromiseTransformTests.csproj','KarenPromiseTransformTests'),
                     ('tools/vfx/catalog/guard/Guard.csproj','KarenBackgroundTestGuard')]:
    build = root/'probes'/branch/name
    subprocess.run(['dotnet','build',str(project/csproj),'-p:Game='+str(source/'game'),
        '-p:Variant='+variant,'-p:KarenAssembly='+str(package/'lib'/('ShoujoKagekiAijoKaren.'+variant+'.dll')),
        '-o',str(build),'--nologo','-v:q'], check=True)
    shutil.copy2(build/(name+'.dll'), mods)
    (mods/(name+'.json')).write_text(json.dumps(dict(id=name,name=name,version='1.0.0',author='Development',
        description='Temporary isolated passive VFX fixture',has_dll=True,has_pck=False,affects_gameplay=False)),encoding='utf-8')
for path in (dev/'user').rglob('settings.save'):
    settings=json.loads(path.read_text(encoding='utf-8-sig'))
    settings.update(mod_settings=dict(mods_enabled=True,mod_list=[]),fullscreen=False,
        mute_in_background=False,limit_fps_in_background=False,seen_ea_disclaimer=True,skip_intro_logo=True)
    path.write_text(json.dumps(settings,ensure_ascii=False),encoding='utf-8')
(mods/'KarenSTS2MCP.conf').write_text(json.dumps(dict(port=args.port,fullscreen=False,muted=True,silent_test=True,
    skip_tutorial=True,skip_first_prompt=True,window_width=1280,window_height=720)),encoding='utf-8')
(game/'override.cfg').write_text('[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\n'
    'window/size/window_width_override=1280\nwindow/size/window_height_override=720\n',encoding='utf-8')
sys.path.insert(0,str(project/'tools/vfx/six-card'))
from isolated_desktop import TestDesktop
base=f'http://127.0.0.1:{args.port}/api/v1'
desktop=TestDesktop()
env=os.environ.copy()
env.update(APPDATA=str(dev/'user/AppData/Roaming'),LOCALAPPDATA=str(dev/'user/AppData/Local'),STS2_MCP_FFMPEG=shutil.which('ffmpeg') or '')
process=desktop.launch([game/'SlayTheSpire2.exe','--rendering-driver','vulkan','--force-steam=off','--log-file',dev/'godot.log'],cwd=game,env=env)
recording=False
results={}

def save(name,data):
    (out/(name+'.json')).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
def api(path,data=None):
    request=urllib.request.Request(base+path,data=None if data is None else json.dumps(data).encode(),headers={'Content-Type':'application/json'})
    with urllib.request.urlopen(request,timeout=15) as response:return json.load(response)
def state():return api('/singleplayer?format=json')
def post(**action):
    result=api('/singleplayer',action)
    with (out/'actions.jsonl').open('a',encoding='utf-8') as f:f.write(json.dumps(dict(action=action,result=result),ensure_ascii=False)+'\n')
    assert result.get('status')=='ok',(action,result)
    return result
def cmd(command):return post(action='run_command',command=command)
def wait(predicate,timeout=40):
    end=time.monotonic()+timeout
    last=None
    while time.monotonic()<end:
        assert process.poll() is None,'Owned test game exited'
        try:
            last=state()
            if predicate(last):return last
        except (OSError,ValueError):pass
        time.sleep(.15)
    save('timeout',last)
    raise TimeoutError(last)
def shot(name):
    save(name,state())
    if not recording:
        with urllib.request.urlopen(base+'/screenshot',timeout=30) as response:(out/(name+'.png')).write_bytes(response.read())
def hand(name):
    data=cmd('karen_check_hand');save(name+'-hand',data)
    value=json.loads(data['message'])
    assert value['modelCount']==value['visualCount'] and not value['missing'] and not value['unexpected'] and not value['unexpectedPileIcons'],value
    return value
def ambient(name):
    report=json.loads(cmd('past_ambient status')['message']);save(name,report);results[name]=report;return report
def single(report):
    assert report['nodes']==report['fronts']==1 and report['traces']==3,report
    node=report['owners'][0]
    assert node['own'] and node['behind'] and node['frontAfter'] and node['originError']<.1 and node['scaleError']<.001,node
    assert node['scaleX']>0 and node['scaleY']>0,node
    return node
def add(card):cmd('card '+card+' Hand');time.sleep(.4)
def play(card):
    c=next(c for c in state()['player']['hand'] if c['id']==card)
    action=dict(action='play_card',card_index=c['index'])
    if c['target_type']=='AnyEnemy':action['target']=state()['battle']['enemies'][0]['entity_id']
    post(**action);time.sleep(.7)
    wait(lambda s:s.get('battle',{}).get('is_play_phase'))
def stream():return json.loads(cmd('past_stream status')['message'])
def stop_recording(name="actual"):
    global recording
    api('/recording/stop',{})
    for _ in range(100):
        report=api('/recording')
        if report['status'] in ('completed','failed'):break
        time.sleep(.2)
    assert report['status']=='completed',report
    shutil.copy2(report.pop('output_path'),out/(name+'.mp4'))
    save(name+'-recording',report);recording=False

try:
    for _ in range(160):
        log=(dev/'godot.log').read_text(encoding='utf-8',errors='replace') if (dev/'godot.log').exists() else ''
        if '[Karen STS2 MCP]' in log and "Preloading 'Common' Complete:" in log:break
        assert process.poll() is None
        time.sleep(.25)
    else:raise TimeoutError('Expected MCP and preloaded assets did not become ready')
    wait(lambda s:'continue' in s.get('options',[]))
    post(action='menu_select',option='continue');time.sleep(2)
    wait(lambda s:bool(s.get('player')))
    cmd('fight WATERFALL_GIANT_BOSS')
    wait(lambda s:s.get('battle',{}).get('is_play_phase') and any(e['entity_id'].startswith('WATERFALL') for e in s['battle']['enemies']))
    time.sleep(1)
    shot('00-before-setup');hand('00-before-setup')
    for c in list(state()['player']['hand']):cmd('remove_card '+c['id']+' Hand');time.sleep(.12)
    cmd('energy 99');cmd('past_stream seed 3');time.sleep(1)
    add('KAREN_PAST_AND_FUTURE');add('KAREN_PROMISE_DRAW')
    shot('01-before-power');hand('01-before-power')
    assert ambient('before-power')['nodes']==0
    play('KAREN_PAST_AND_FUTURE');time.sleep(.8)
    node=single(ambient('idle-a'));assert node['visibility']>.99
    time.sleep(.65)
    later=single(ambient('idle-b'));assert abs(later['time']-node['time'])>.2
    assert stream()['state']['active']==0
    cmd('past_ambient capture')
    for _ in range(60):
        report=json.loads(cmd('past_ambient status')['message'])
        if not report['capturing']:break
        time.sleep(.1)
    else:raise TimeoutError('Frozen visual capture did not finish')
    captured=list((dev/'user').rglob('past-ambient-audit/visible.png'))
    assert len(captured)==1,captured
    for filename in ['visible.png','hidden.png']:
        shutil.copy2(captured[0].parent/filename,out/('audit-'+filename))
    if args.visibility_only:
        print(branch,'visibility pair captured',flush=True)
        sys.exit(0)

    cmd('past_ambient hide');time.sleep(.2);shot('02-idle-hidden')
    cmd('past_ambient show');time.sleep(.2);shot('03-idle-visible');hand('03-idle-visible')
    time.sleep(1);shot('03-idle-visible-later')
    api('/recording/start',dict(fps=30,max_duration_seconds=70,width=1280,height=720,audio=False,encoder='auto'));recording=True
    time.sleep(7.5)
    for transform in ['small','small_flip','large','normal']:
        cmd('promise_transform '+transform);time.sleep(1.25)
        single(ambient('transform-'+transform))
    stop_recording('idle')
    before=stream();hand('04-before-draw');shot('04-before-draw')
    api('/recording/start',dict(fps=30,max_duration_seconds=20,width=1280,height=720,audio=False,encoder='auto'));recording=True
    cmd('past_stream watch');play('KAREN_PROMISE_DRAW')
    single(ambient('during-draw'))
    time.sleep(3)
    after=stream();save('draw-before',before);save('draw-after',after)
    removed=before['promise']-after['promise']
    assert removed>0 and after['strength']-before['strength']==removed,(before,after)
    assert after['stats']['maxActive']>0 and after['stats']['maxActive']<=8,after
    assert after['stats']['minAmbientVisibility']<.5,after
    assert single(ambient('after-draw'))['visibility']>.99
    stop_recording('draw')
    hand('05-after-draw');shot('05-after-draw')
    add('KAREN_BURN');play('KAREN_BURN');time.sleep(1)
    stacked=ambient('stacked');single(stacked);assert stacked['ambientWeight']<1
    time.sleep(2)
    shot('06-stacked');hand('06-stacked')
    cmd('past_ambient stop');time.sleep(.1)
    fading=single(ambient('fading'));assert 0<fading['alpha']<1
    cmd('past_ambient start');time.sleep(.5)
    resumed=single(ambient('restart'));assert resumed['alpha']>.99
    cmd('past_ambient stop');time.sleep(.55)
    removed=ambient('stopped');assert removed['nodes']==removed['fronts']==removed['traces']==0
    shot('07-stopped')
    cmd('past_ambient start');time.sleep(.7);single(ambient('started'))
    cmd('fight WATERFALL_GIANT_BOSS');time.sleep(1)
    wait(lambda s:s.get('battle',{}).get('is_play_phase'))
    cleared=ambient('new-room');assert cleared['nodes']==cleared['fronts']==cleared['traces']==0
    shot('08-new-room');hand('08-new-room')
    dll='ShoujoKagekiAijoKaren.'+variant+'.dll'
    loaded=(mods/'ShoujoKagekiAijoKaren/lib'/dll).read_bytes()
    assert loaded==(package/'lib'/dll).read_bytes()
    log=(dev/'godot.log').read_text(encoding='utf-8',errors='replace')
    lines=log.splitlines()
    common_done=next((i for i,line in enumerate(lines) if "Preloading 'Common' Complete:" in line),-1)
    startup=[];runtime=[]
    for i,line in enumerate(lines):
        if 'ERROR:' not in line and 'Exception' not in line:continue
        diagnostic=dict(line=i+1,text=line)
        # Established beta engine diagnostic; require the exact native stack and startup boundary.
        if (branch=='beta' and line=='ERROR: Invalid Task ID' and i<common_done
                and i+1<len(lines) and 'wait_for_task_completion (core/object/worker_thread_pool.cpp:' in lines[i+1]):
            startup.append(diagnostic)
        else:runtime.append(diagnostic)
    save('log-audit',dict(startupDiagnostics=startup,runtimeErrors=runtime));assert not runtime,runtime
    save('results',dict(passed=True,cases=results,loadedAssemblySha256=hashlib.sha256(loaded).hexdigest()))
    print(branch,'PASS:',len(results),'checks, real draw, transforms, stack and lifecycle',flush=True)
finally:
    if recording:
        try:stop_recording()
        except Exception:pass
    desktop.close()
    if (dev/'godot.log').exists():shutil.copy2(dev/'godot.log',out/'godot.log')