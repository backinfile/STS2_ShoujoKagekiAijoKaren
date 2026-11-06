"""Native hover and inspect previews for selected or object-style candidate portraits.

Prepare the copies with tools/prepare_relic_test_games.ps1 and build the package
with tools/build_dual_branch.ps1, then run this file for stable.
"""
import argparse, hashlib, json, os, shutil, socket, subprocess, time, urllib.request
from pathlib import Path

p=argparse.ArgumentParser();p.add_argument('variant',choices=['stable'],nargs='?',default='stable');p.add_argument('--choices',default='a,wide');p.add_argument('--assets',default='docs/worktree-tasks/karen-x-relic-barrage-20261022/evidence/art-preview');p.add_argument('--output',default='artifacts/shine-price-art-preview');a=p.parse_args()
root=Path(__file__).resolve().parents[2];dev=root/'artifacts/dev-game'/a.variant;game=dev/'game'
out=root/a.output/a.variant;out.mkdir(parents=True,exist_ok=True)
port=15657 if a.variant=='stable' else 15658
with socket.socket() as s: assert s.connect_ex(('127.0.0.1',port))!=0,'Isolated test port occupied'
base=f'http://127.0.0.1:{port}/api/v1';opener=urllib.request.build_opener(urllib.request.ProxyHandler({}))
process=None
choices=a.choices.split(',');assert choices and all(c in ['a','wide','palm','clasp','pieces','off'] for c in choices)
results=[]

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
    env=os.environ.copy();env.update(APPDATA=str(dev/'user/AppData/Roaming'),LOCALAPPDATA=str(dev/'user/AppData/Local'),KAREN_SHINE_PRICE_ART_ROOT=str(root/a.assets))
    startup=subprocess.STARTUPINFO();startup.dwFlags=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
    log=dev/'godot.log';launch=(out/'launch.log').open('w',encoding='utf-8')
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
    for choice in choices:
        case=out/choice;case.mkdir(exist_ok=True)
        snap(case,'before-setup')
        cmd('shine_price_probe portrait '+choice)
        cmd('shine_price_probe seed normal')
        deadline=time.monotonic()+25
        while time.monotonic()<deadline:
            p=probe();assert not p['state'].startswith('failed'),p
            if p['state']=='ready':break
            time.sleep(.1)
        else:raise TimeoutError('Preview seed')
        cmd('shine_price_probe present');cmd('energy 3');time.sleep(1)
        p=probe();assert p['artPreview']==choice and p['damage']==5 and p['title']=='双星' and p['portrait']=='(500, 380)',p
        action('hover_card',card_index=0);time.sleep(.7);snap(case,'hover');action('unhover_card');time.sleep(.35)
        cmd('shine_price_probe inspect');time.sleep(1);snap(case,'inspect')
        save(case/'probe.json',probe());cmd('shine_price_probe close-inspect');time.sleep(.5)
        snap(case,'after-setup')
        results.append(dict(case=choice,passed=True,damage=p['damage'],portrait=p['portrait'],artPreview=p['artPreview']))
        save(out/'results.json',results);print(choice,'PREVIEWED',flush=True)
    cmd('shine_price_probe portrait off')

try:main()
finally:
    save(out/'results.json',results)
    if process and process.poll() is None:process.terminate();process.wait(timeout=15)
    log=dev/'godot.log'
    if log.exists():shutil.copy2(log,out/'godot.log')
