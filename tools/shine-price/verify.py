"""MCP-only integration checks for the X-cost relic attack, in owned game copies.

Prepare the copies with tools/prepare_relic_test_games.ps1 and build the package
with tools/build_dual_branch.ps1, then run this file for stable and beta.
"""
import argparse, hashlib, json, os, shutil, socket, subprocess, time, urllib.request
from pathlib import Path

p=argparse.ArgumentParser();p.add_argument('variant',choices=['stable','beta']);p.add_argument('--cases',default='');p.add_argument('--output',default='artifacts/shine-price');p.add_argument('--lethal',action='store_true');a=p.parse_args()
root=Path(__file__).resolve().parents[2];dev=root/'artifacts/dev-game'/a.variant;game=dev/'game'
out=root/a.output/a.variant;out.mkdir(parents=True,exist_ok=True)
port=15657 if a.variant=='stable' else 15658
with socket.socket() as s: assert s.connect_ex(('127.0.0.1',port))!=0,'Isolated test port occupied'
base=f'http://127.0.0.1:{port}/api/v1';opener=urllib.request.build_opener(urllib.request.ProxyHandler({}))
process=None
results=json.loads((out/'results.json').read_text(encoding='utf-8')) if a.cases and (out/'results.json').exists() else []
if a.cases:results=[r for r in results if r['case'] not in a.cases.split(',')+['combat-end-restoration']+(['lethal'] if a.lethal else [])]

def save(path,data): path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def api(path,body=None,binary=False):
    request=urllib.request.Request(base+path,data=None if body is None else json.dumps(body).encode(),headers={'Content-Type':'application/json'})
    with opener.open(request,timeout=30) as response: data=response.read()
    return data if binary else json.loads(data)
def state():return api('/singleplayer?format=json')
def action(action,**args):
    result=api('/singleplayer',dict(action=action,**args))
    with (out/'actions.jsonl').open('a',encoding='utf-8') as f:f.write(json.dumps([action,args,result],ensure_ascii=False)+'\n')
    assert result.get('status')=='ok',result
    return result
def cmd(command):return action('run_command',command=command)
def probe():return json.loads(cmd('shine_price_probe status')['message'])
def wait(test,timeout=45):
    deadline=time.monotonic()+timeout
    while time.monotonic()<deadline:
        assert process.poll() is None,'Owned test game exited'
        try:
            s=state()
            if s.get('prompt_id'):
                options=[x['name'] if isinstance(x,dict) else x for x in s.get('options',[])]
                option=next((x for x in ['no','advance','proceed'] if x in options),options[0])
                api('/ui/prompt',dict(option=option,prompt_id=s['prompt_id']))
            elif test(s):return s
        except (OSError,ValueError):pass
        time.sleep(.15)
    save(out/'timeout.json',state());raise TimeoutError('MCP state wait')
def snap(case,name):
    save(case/(name+'.json'),state())
    (case/(name+'.png')).write_bytes(api('/screenshot',binary=True))
    hand=json.loads(cmd('karen_check_hand')['message']);save(case/(name+'-hand.json'),hand)
    assert hand['modelCount']==hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons'],hand

def main():
    global process
    package=root/'artifacts/ShoujoKagekiAijoKaren';mods=game/'mods'
    shutil.copytree(package,mods/'ShoujoKagekiAijoKaren',dirs_exist_ok=True)
    build=root/'artifacts/shine-price/probe'/a.variant
    subprocess.run(['dotnet','build',str(Path(__file__).with_name('Probe.csproj')),f'-p:Variant={a.variant.title()}','-o',str(build),'--nologo','-v:q','-clp:ErrorsOnly'],check=True)
    shutil.copy2(build/'KarenShinePriceProbe.dll',mods)
    save(mods/'KarenShinePriceProbe.json',dict(id='KarenShinePriceProbe',name='Shine Price Test',author='Development',version='1.0.0',has_dll=True,has_pck=False,affects_gameplay=False,dependencies=[dict(id='ShoujoKagekiAijoKaren',min_version='v0.1.3')]))
    save(out/'package-hashes.json',{f:hashlib.sha256((package/f).read_bytes()).hexdigest() for f in ['ShoujoKagekiAijoKaren.pck',f'lib/ShoujoKagekiAijoKaren.{a.variant.title()}.dll']})
    env=os.environ.copy();env.update(APPDATA=str(dev/'user/AppData/Roaming'),LOCALAPPDATA=str(dev/'user/AppData/Local'))
    startup=subprocess.STARTUPINFO();startup.dwFlags=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
    log=dev/'godot.log';launch=(out/'launch.log').open('w',encoding='utf-8')
    # Clear this owned log before launch so an earlier readiness marker cannot win the startup race.
    log.write_text('',encoding='utf-8')
    process=subprocess.Popen([str(game/'SlayTheSpire2.exe'),'--rendering-driver','vulkan','--force-steam=off','--log-file',str(log)],cwd=game,env=env,startupinfo=startup,stdout=launch,stderr=subprocess.STDOUT)
    deadline=time.monotonic()+120
    while time.monotonic()<deadline:
        assert process.poll() is None,'Test game exited while loading'
        text=log.read_text(encoding='utf-8',errors='replace') if log.exists() else ''
        if '[Karen STS2 MCP]' in text and "Preloading 'Common' Complete:" in text:break
        time.sleep(.25)
    else:raise TimeoutError('MCP log readiness')
    s=wait(lambda s:s.get('menu_screen') in ['main','character_select'] or s.get('player'))
    time.sleep(1.5);s=state();save(out/'initial-state.json',s)
    if s.get('menu_screen')=='main' and 'continue' in s.get('options',[]):
        action('menu_select',option='continue');s=wait(lambda s:s.get('player'))
    if not s.get('player'):
        if s.get('menu_screen')=='main':action('menu_select',option='singleplayer')
        s=wait(lambda s:s.get('menu_screen') in ['singleplayer','character_select'])
        if s.get('menu_screen')=='singleplayer':action('menu_select',option='standard')
        action('menu_select',option='KAREN');action('menu_select',option='embark')
        s=wait(lambda s:s.get('state_type')=='map' or s.get('menu_screen')=='tutorial_prompt')
        if s.get('menu_screen')=='tutorial_prompt':action('menu_select',option='no')
        wait(lambda s:s.get('state_type')=='map')
    cmd('fight WATERFALL_GIANT_BOSS');wait(lambda s:s.get('battle',{}).get('is_play_phase'));time.sleep(.8)
    # Each case is a real native play, except blocked cases, which must be rejected.
    cases=[('zero','zero',0,0,0,0),('one','normal',1,2,1,10),('two','normal',2,4,2,20),('three','normal',3,6,3,30),('upgrade','upgrade',3,6,3,36),('strength','strength',2,4,2,28),('chemical','chemical',1,6,3,30),('lock-chemical','lock-chemical',1,6,3,30),('replay','replay',2,8,4,40),('replay-short','normal',2,4,2,20),('blocked','blocked',3,0,0,0),('blocked-upgrade','blocked-upgrade',3,0,0,0),('blocked-chemical','blocked-chemical',1,0,0,0),('fast','fast',3,6,3,30),('instant','instant',3,6,3,30)]
    cases.sort(key=lambda c:not c[0].startswith('blocked'))
    if a.cases:cases=[c for c in cases if c[0] in a.cases.split(',')]
    for name,mode,energy,hits,locks,damage in cases:
        case=out/name;case.mkdir(exist_ok=True)
        cmd('shine_price_probe seed '+('replay-short' if name=='replay-short' else mode))
        for _ in range(120):
            before=probe()
            assert not before['state'].startswith('failed'),before
            if before['state']=='ready':break
            time.sleep(.1)
        else:raise TimeoutError('Seed fixture')
        cmd('energy '+str(energy));wait(lambda s:s.get('player',{}).get('energy')==energy);time.sleep(.65)
        before=probe();save(case/'probe-before.json',before)
        action('hover_card',card_index=0);time.sleep(.45);snap(case,'before');action('unhover_card')
        assert before['pool'] and before['costX'] and before['hasPortrait'] and before['portrait']=='(500, 380)',before
        expected_base=6 if mode in ['upgrade','blocked-upgrade'] else 5
        expected_x=energy+(2 if mode in ['chemical','lock-chemical','blocked-chemical'] else 0)
        assert before['title']==('双星+' if before['upgraded'] else '双星'),before
        assert before['damage']==expected_base and before['repeat']==2 and before['disableCost']==expected_x,before
        if name=='blocked':
            cmd('shine_price_probe energy 2');wait(lambda s:s.get('player',{}).get('energy')==2);assert probe()['playable'],'Reducing energy must lower the relic requirement'
            cmd('shine_price_probe energy 3');wait(lambda s:s.get('player',{}).get('energy')==3);assert not probe()['playable'],'Increasing energy must restore the relic requirement'
        s=state();card=next(c for c in s['player']['hand'] if c['id']=='KAREN_SHINE_PRICE')
        assert bool(before['playable'])==(not name.startswith('blocked')),before
        response=api('/singleplayer',dict(action='play_card',card_index=card['index'],target=s['battle']['enemies'][0]['entity_id']))
        if name.startswith('blocked'):
            assert response['status']!='ok',response
        else:
            assert response['status']=='ok',response
            wait(lambda s:not any(c['id']=='KAREN_SHINE_PRICE' for c in s.get('player',{}).get('hand',[])))
        if not name.startswith('blocked'):
            deadline=time.monotonic()+35
            while time.monotonic()<deadline:
                after=probe()
                if len(after['attacks'])>=(2 if mode=='replay' else 1) and after['pile']=='Discard':break
                time.sleep(.15)
            else:raise TimeoutError('Native card resolution did not finish: '+str(after))
        time.sleep(.7);after=probe();save(case/'probe-after.json',after);snap(case,'after')
        actual_hits=sum(x['hits'] for x in after['attacks'])
        assert actual_hits==hits and after['locks']==locks,after
        assert before['targetHp']-after['targetHp']==damage,after
        assert after['energy']==(energy if name.startswith('blocked') else 0),after
        actual=[v for x in after['attacks'] for v in x['damage']]
        assert actual==([7 if mode=='strength' else expected_base]*hits),after
        visual_hits=0 if mode=='instant' else hits
        assert after['visualEvents'].count('gold')==(visual_hits+1)//2 and after['visualEvents'].count('pink')==visual_hits//2,after
        assert after['visualEvents'].count('contact')==visual_hits and after['activeVfx']==0 and 'gather' not in after['visualEvents'],after
        assert len(after['pairs'])==(visual_hits+1)//2 and all(p['fromRelic'] and 0 < p['distance'] <= 220.1 for p in after['pairs']),after
        results.append(dict(case=name,passed=True,pairs=after['pairs'],energy=energy,hits=actual_hits,relics_disabled=after['locks'],damage=damage,visual_events=after['visualEvents'],active_vfx_after=after['activeVfx']))
        save(out/'results.json',results);print(a.variant,name,'PASS',results[-1],flush=True)
    # A real combat end must restore the sacrificed relics.
    locks_before_end=probe()['locks'];assert locks_before_end>0,'Restoration needs a sacrificed relic'
    cmd('kill all');wait(lambda s:s.get('state_type')=='rewards')
    assert probe()['locks']==0,probe()
    results.append(dict(case='combat-end-restoration',passed=True,restored=locks_before_end));save(out/'results.json',results)
    if a.lethal:
        cmd('fight WATERFALL_GIANT_BOSS');wait(lambda s:s.get('battle',{}).get('is_play_phase'));time.sleep(.6)
        cmd('shine_price_probe seed lethal')
        deadline=time.monotonic()+20
        while time.monotonic()<deadline:
            if probe()['state']=='ready':break
            time.sleep(.1)
        cmd('energy 3');wait(lambda s:s.get('player',{}).get('energy')==3);time.sleep(.4)
        case=out/'lethal';case.mkdir(exist_ok=True);snap(case,'before');save(case/'probe-before.json',probe())
        s=state();action('play_card',card_index=0,target=s['battle']['enemies'][0]['entity_id'])
        wait(lambda s:s.get('state_type')=='rewards');time.sleep(.7)
        after=probe();save(case/'probe-after.json',after);snap(case,'after')
        assert after['targetHp']==0 and sum(x['hits'] for x in after['attacks'])==1,after
        assert len(after['pairs'])==1 and after['pairs'][0]['fromRelic'] and 0 < after['pairs'][0]['distance'] <= 220.1,after
        assert after['visualEvents']==['start','gold','contact'] and after['activeVfx']==0 and after['locks']==0,after
        results.append(dict(case='lethal',passed=True,hits=1,pairs=after['pairs'],visual_events=after['visualEvents'],active_vfx_after=0,relics_restored=True));save(out/'results.json',results)


try:main()
finally:
    save(out/'results.json',results)
    if process and process.poll() is None:process.terminate();process.wait(timeout=15)
    log=dev/'godot.log'
    if log.exists():shutil.copy2(log,out/'godot.log')
