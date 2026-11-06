"""Run the review fixtures through KarenSTS2MCP in an isolated copy of a dev game.

Native game files are cached in an isolated directory; mods and user data are copied.
No existing dev game, mod folder, save or running process is modified.
"""
from pathlib import Path
import argparse, json, os, shutil, socket, subprocess, time, urllib.request

parser=argparse.ArgumentParser()
parser.add_argument('--branch',choices=['stable','beta'],default='stable')
parser.add_argument('--stage',default='baseline')
parser.add_argument('--package',type=Path)
parser.add_argument('--fixture',type=Path)
parser.add_argument('--cases',default='menu,save,revive,music,hover,star,replay')
args=parser.parse_args()
project=Path(__file__).resolve().parents[2]
run_root=project/'artifacts/review-fixes'/args.stage/args.branch
source=project/'artifacts/dev-game'/args.branch/'game'
game=project/'artifacts/review-fixes/games'/args.branch
port=15727 if args.branch=='stable' else 15728
url=f'http://127.0.0.1:{port}/api/v1'
results=[]

def request(endpoint, body=None, timeout=12):
    data=None if body is None else json.dumps(body).encode()
    req=urllib.request.Request(url+endpoint,data=data,headers={'Content-Type':'application/json'})
    with urllib.request.urlopen(req,timeout=timeout) as response:
        return json.loads(response.read())

def state(): return request('/singleplayer?format=json')
def action(**body):
    r=request('/singleplayer',body)
    if r.get('status')!='ok': raise RuntimeError(json.dumps({'action':body,'result':r},ensure_ascii=False))
    return r
def wait_for(test,label,seconds=35):
    deadline=time.monotonic()+seconds
    last=None
    while time.monotonic()<deadline:
        try:
            last=state()
            if test(last):return last
        except (OSError,ValueError):pass
        time.sleep(.25)
    raise RuntimeError(f'Timeout {label}; last state: {last}')
def capture(name):
    try:
        with urllib.request.urlopen(url+'/screenshot',timeout=20) as r:
            (run_root/(name+'.png')).write_bytes(r.read())
    except OSError as e: print('CAPTURE',name,str(e),flush=True)
def fixture(case):
    capture(case+'-before')
    action(action='run_command',command='review_fixes '+case)
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        r=action(action='run_command',command='review_fixes_status')
        message=r.get('message','')
        if message.startswith(('pass:','fail:')):
            if case=='hover' and message.startswith('pass:'):
                card=next(c for c in state()['player']['hand'] if c['id']=='KAREN_REWRITE')
                action(action='hover_card',card_index=card['index'])
                time.sleep(.7)
            if case in ['star','replay']:time.sleep(1.5)
            capture(case+'-after')
            entry={'case':case,'pass':message.startswith('pass:'),'message':message}
            if case in ['star','replay','hover']:
                entry['hand']=request('/singleplayer',{'action':'run_command','command':'karen_check_hand'})
            if case=='hover':action(action='unhover_card')
            results.append(entry)
            print(json.dumps(entry,ensure_ascii=False),flush=True)
            return
        time.sleep(.2)
    raise RuntimeError('Fixture timed out: '+case)

with socket.socket() as check:
    if check.connect_ex(('127.0.0.1',port))==0: raise RuntimeError(f'Port {port} is already in use')
run_root.mkdir(parents=True,exist_ok=True)
if (run_root/'report.json').exists():raise RuntimeError('Use a new --stage; an existing test profile must not be reused')
game.mkdir(parents=True,exist_ok=True)
for p in source.iterdir():
    if p.is_file() and not (game/p.name).exists():shutil.copy2(p,game/p.name)
data=game/'data_sts2_windows_x86_64'
if not data.exists():
    shutil.copytree(source/data.name,data)
for mod in ['BaseLib','ShoujoKagekiAijoKaren']:
    target=game/'mods'/mod
    if not target.exists():shutil.copytree(source/'mods'/mod,target)
for name in ['KarenSTS2MCP.dll','KarenSTS2MCP.json']:
    if not (game/'mods'/name).exists():shutil.copy2(source/'mods'/name,game/'mods'/name)
if args.package:
    shutil.copytree(args.package,game/'mods/ShoujoKagekiAijoKaren',dirs_exist_ok=True)
fixture_source=args.fixture or project/('artifacts/review-fixes/fixture-'+args.branch)
plugin=game/'mods/KarenReviewFixes'; plugin.mkdir(exist_ok=True)
shutil.copy2(fixture_source/'KarenReviewFixes.dll',plugin/'KarenReviewFixes.dll')
(plugin/'KarenReviewFixes.json').write_text(json.dumps(dict(id='KarenReviewFixes',name='Review fix fixtures',version='1.0.0',has_dll=True,has_pck=False,affects_gameplay=True,dependencies=[{'id':'ShoujoKagekiAijoKaren'}])),encoding='utf-8')
(game/'mods/KarenSTS2MCP.conf').write_text(json.dumps(dict(port=port,muted=True,fullscreen=False,window_width=1280,window_height=720,skip_tutorial=True,skip_first_prompt=True)),encoding='utf-8')
profile=run_root/'user/AppData/Roaming/SlayTheSpire2/default/8601'
profile.mkdir(parents=True,exist_ok=True)
settings=profile/'settings.save'
if not settings.exists():shutil.copy2(project/'artifacts/dev-game'/args.branch/'user/AppData/Roaming/SlayTheSpire2/default/1/settings.save',settings)
env=os.environ.copy();env['APPDATA']=str(run_root/'user/AppData/Roaming');env['LOCALAPPDATA']=str(run_root/'user/AppData/Local')
env['KAREN_REVIEW_MENU_TEST']='1' if 'menu' in args.cases.split(',') else '0'
Path(env['LOCALAPPDATA']).mkdir(parents=True,exist_ok=True)
log=run_root/'godot.log'
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
process=subprocess.Popen([str(game/'SlayTheSpire2.exe'),'--rendering-driver','vulkan','--force-steam=off','--clientId=8601','--log-file',str(log)],cwd=game,env=env,startupinfo=startup,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
print(f'START {args.branch} {args.stage} pid={process.pid} port={port}',flush=True)
try:
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        if log.exists() and '[Karen STS2 MCP]' in log.read_text(encoding='utf-8',errors='replace'):break
        if process.poll() is not None:raise RuntimeError('Game exited: '+str(process.returncode))
        time.sleep(.25)
    else:raise RuntimeError('Karen STS2 MCP did not load')
    wait_for(lambda s:s.get('menu_screen')=='main' and 'singleplayer' in s.get('options',[]),'main menu')
    request('/settings',{'muted':True})
    action(action='menu_select',option='singleplayer')
    s=wait_for(lambda s:s.get('menu_screen') in ['singleplayer','character_select'],'character menu')
    if s['menu_screen']=='singleplayer':action(action='menu_select',option='standard')
    wait_for(lambda s:s.get('menu_screen')=='character_select' and any('KAREN' in str(o) for o in s.get('options',[])),'character selection')
    cases=args.cases.split(',')
    if 'menu' in cases:
        capture('menu-before')
        deadline=time.monotonic()+15
        while time.monotonic()<deadline:
            lines=log.read_text(encoding='utf-8',errors='replace').splitlines()
            found=next((line.split('[ReviewFixes] ',1)[1] for line in reversed(lines) if '[ReviewFixes] ' in line and ':menu' in line),None)
            if found:
                entry={'case':'menu','pass':found.startswith('pass:'),'message':found}
                results.append(entry);print(json.dumps(entry,ensure_ascii=False),flush=True);break
            time.sleep(.2)
        else:raise RuntimeError('Menu fixture did not complete')
        capture('menu-after')
    action(action='menu_select',option='KAREN')
    action(action='menu_select',option='embark')
    s=wait_for(lambda s:s.get('menu_screen')=='tutorial_prompt' or s.get('state_type')=='map','map startup')
    if s.get('menu_screen')=='tutorial_prompt':action(action='menu_select',option='no')
    wait_for(lambda s:s.get('state_type')=='map','map')
    action(action='choose_map_node',index=0)
    wait_for(lambda s:s.get('state_type')=='monster' and s.get('battle',{}).get('is_play_phase'),'combat')
    (run_root/'combat.json').write_text(json.dumps(state(),ensure_ascii=False,indent=2),encoding='utf-8')
    for case in cases:
        if case!='menu':fixture(case)
except Exception as e:
    results.append({'case':'runner','pass':False,'message':str(e)})
    print('RUNNER_ERROR',str(e),flush=True)
finally:
    (run_root/'report.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
    # This Popen handle owns only the disposable game created above.
    if process.poll() is None:process.terminate();process.wait(timeout=15)
print('REPORT',run_root/'report.json',flush=True)
raise SystemExit(0 if results and all(x['pass'] for x in results) else 1)
