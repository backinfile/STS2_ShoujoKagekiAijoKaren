"""Targeted native before/after scenes on an owned, silent Windows desktop."""
import argparse, hashlib, json, os, shutil, socket, subprocess, sys, time, urllib.request
from pathlib import Path
from types import SimpleNamespace
parser=argparse.ArgumentParser()
parser.add_argument('--source-root',type=Path,required=True)
parser.add_argument('--package',type=Path,required=True)
parser.add_argument('--branch',choices=['stable','beta'],required=True)
parser.add_argument('--phase',choices=['before','after'],required=True)
parser.add_argument('--suffix',default='')
parser.add_argument('--empty-matrix',action='store_true')
parser.add_argument('--case-filter',default='')
parser.add_argument('--fixture-root',type=Path)
parser.add_argument('--cases',default='empty,void,batch,infinite,swap,reward0,reward1,reward3,normal,tower')
args=parser.parse_args();root=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(root/'tools/vfx/six-card'))
from isolated_desktop import TestDesktop
out=root/'artifacts/review-boundaries'/args.phase/(args.branch+('-'+args.suffix if args.suffix else ''))
out.mkdir(parents=True,exist_ok=False)
game=out/'game';source=args.source_root/args.branch/'game';mods=game/'mods'
port=(18847 if args.branch=='stable' else 18848)+(0 if args.phase=='before' else 2)
base=f'http://127.0.0.1:{port}/api/v1';results=[];recording=False
with socket.socket() as check:
    assert check.connect_ex(('127.0.0.1',port))!=0,'Owned test port is occupied'
for current,dirs,files in os.walk(source):
    dirs[:]=[d for d in dirs if d!='mods'];rel=Path(current).relative_to(source);target=game/rel;target.mkdir(parents=True,exist_ok=True)
    for name in files:
        if name=='override.cfg':continue
        os.link(Path(current)/name,target/name)
mods.mkdir(exist_ok=True)
shutil.copytree(source/'mods/BaseLib',mods/'BaseLib')
for name in ['KarenSTS2MCP.dll','KarenSTS2MCP.json']:shutil.copy2(source/'mods'/name,mods/name)
mod=mods/'ShoujoKagekiAijoKaren';mod.mkdir()
for name in ['ShoujoKagekiAijoKaren.dll','ShoujoKagekiAijoKaren.json','ShoujoKagekiAijoKaren.pck']:shutil.copy2(args.package/name,mod/name)
(mod/'lib').mkdir()
impl='baseline' if args.phase=='before' else 'fixed'
for variant in ['Stable','Beta']:
    shutil.copy2(root/f'artifacts/{impl}/{variant.lower()}/ShoujoKagekiAijoKaren.{variant}.dll',mod/f'lib/ShoujoKagekiAijoKaren.{variant}.dll')
plugin=mods/'KarenBoundaryReview';plugin.mkdir()
fixture=args.fixture_root or root/f'artifacts/{"fixture-empty" if args.empty_matrix else "fixture"}'
shutil.copy2(fixture/args.branch/'KarenBoundaryReview.dll',plugin/'KarenBoundaryReview.dll')
(plugin/'KarenBoundaryReview.json').write_text(json.dumps(dict(id='KarenBoundaryReview',name='Boundary review fixtures',version='1.0.0',has_dll=True,has_pck=False,affects_gameplay=True,dependencies=[dict(id='ShoujoKagekiAijoKaren')])),encoding='utf-8')
(mods/'KarenSTS2MCP.conf').write_text(json.dumps(dict(port=port,fullscreen=False,muted=True,silent_test=True,skip_tutorial=True,skip_first_prompt=True,window_width=1280,window_height=720)),encoding='utf-8')
(game/'override.cfg').write_text('[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n',encoding='utf-8')
profile=out/'user/AppData/Roaming/SlayTheSpire2/default/8647';profile.mkdir(parents=True)
settings=json.loads((args.source_root/args.branch/'user/AppData/Roaming/SlayTheSpire2/default/1/settings.save').read_text(encoding='utf-8-sig'))
settings.update(language='zhs',fullscreen=False,limit_fps_in_background=False)
(profile/'settings.save').write_text(json.dumps(settings),encoding='utf-8')
env=os.environ.copy();env['APPDATA']=str(out/'user/AppData/Roaming');env['LOCALAPPDATA']=str(out/'user/AppData/Local')
log=out/'godot.log';desktop=TestDesktop()
def api(path,data=None,binary=False,timeout=15):
    req=urllib.request.Request(base+path,data=None if data is None else json.dumps(data).encode(),headers={'Content-Type':'application/json'})
    with urllib.request.urlopen(req,timeout=timeout) as r:body=r.read()
    return body if binary else json.loads(body)
def state():return api('/singleplayer?format=json')
def act(action,**data):
    reply=api('/singleplayer',dict(action=action,**data))
    with (out/'actions.jsonl').open('a',encoding='utf-8') as f:f.write(json.dumps([dict(action=action,**data),reply],ensure_ascii=False)+'\n')
    assert reply.get('status')=='ok',reply
    return reply

def cmd(value):return act('run_command',command=value)
def snap():return json.loads(cmd(('pile_empty' if args.empty_matrix else 'review_boundary')+' snapshot')['message'])
def wait(test,label,seconds=45,reader=state):
    until=time.monotonic()+seconds;last=None
    while time.monotonic()<until:
        try:
            last=reader()
            if test(last):return last
        except (OSError,ValueError,TimeoutError):pass
        time.sleep(.2)
    raise RuntimeError('Timeout '+label+'; '+str(last)[:800])
def settle():
    value=wait(lambda s:s['status'] in ['ready','finished','baseline-stopped-after-8-failures'] or s['status'].startswith('error:'),'fixture completion',reader=snap)
    assert not value['status'].startswith('error:'),value
    return value

def capture(name):
    assert not recording
    (out/(name+'.png')).write_bytes(api('/screenshot',binary=True))
    (out/(name+'.json')).write_text(json.dumps(dict(game=state(),audit=snap()),ensure_ascii=False,indent=2),encoding='utf-8')

def hand(name):
    s=state()
    if s.get('state_type') in ['rewards','map'] and not s.get('battle',{}).get('is_play_phase'):return {'status':'not_applicable','reason':'combat ended'}
    reply=cmd('karen_check_hand')
    assert reply['status']=='ok',reply
    audit=json.loads(reply['message'])
    assert audit['modelCount']==audit['visualCount'] and not audit['missing'] and not audit['unexpected'] and not audit['unexpectedPileIcons'],audit
    assert all(x['valid'] for x in audit.get('burnOverlays',[])),audit
    return reply

def start_record():
    global recording
    value=api('/recording/start',dict(fps=30,max_duration_seconds=60,width=1280,height=720,audio=False,encoder='auto'))
    assert value['status']=='recording',value
    recording=True;time.sleep(1)

def stop_record(name):
    global recording
    api('/recording/stop',{})
    value=wait(lambda s:s['status'] in ['completed','failed'],'recording finalization',seconds=45,reader=lambda:api('/recording'))
    recording=False
    (out/(name+'-recording.json')).write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8')
    assert value['status']=='completed',value
    shutil.copy2(value['output_path'],out/(name+'.mp4'))

def prepare(name):
    cmd('fight NIBBITS_WEAK')
    wait(lambda s:s.get('battle',{}).get('is_play_phase'),'combat ready')
    time.sleep(.8)
    cmd('review_boundary setup '+name);settle();time.sleep(1)

def reward(take,ordinary=False):
    cmd('review_boundary '+('normal' if ordinary else 'offer'))
    s=wait(lambda s:s.get('state_type')=='rewards','custom rewards')
    item=next(x for x in s['rewards']['items'] if ('闪耀牌奖励' in x.get('description','')) or ordinary)
    act('claim_reward',index=item['index'])
    if ordinary:
        wait(lambda s:s.get('state_type')=='card_reward','ordinary card reward')
        act('select_card_reward',card_index=0)
        settle();time.sleep(1);return
    s=wait(lambda s:s.get('state_type')=='card_select','card selection')
    time.sleep(.8)
    for i in range(take):
        act('select_card',index=i);time.sleep(.3)
    s=state()
    if s.get('state_type')=='card_select' and (s.get('card_select',{}).get('can_confirm') or s.get('card_select',{}).get('preview_showing')):act('confirm_selection')
    settle();time.sleep(1)

try:
    process=desktop.launch([game/'SlayTheSpire2.exe','--rendering-driver','vulkan','--force-steam=off','--clientId=8647','--log-file',log],game,env)
    print(f'START {args.phase} {args.branch} pid={process.pid} port={port}',flush=True)
    wait(lambda _:log.exists() and '[Karen STS2 MCP]' in log.read_text(encoding='utf-8',errors='replace'),'MCP loaded',reader=lambda:None)
    wait(lambda s:s.get('menu_screen')=='main' and 'singleplayer' in s.get('options',[]),'main menu')
    wait(lambda _:'Preloading \'Common\' Complete:' in log.read_text(encoding='utf-8',errors='replace'),'common resources',reader=lambda:None)
    act('menu_select',option='singleplayer')
    s=wait(lambda s:s.get('menu_screen') in ['singleplayer','character_select'],'character selection')
    if s.get('menu_screen')=='singleplayer':act('menu_select',option='standard')
    act('menu_select',option='KAREN');act('menu_select',option='embark')
    s=wait(lambda s:s.get('menu_screen')=='tutorial_prompt' or s.get('state_type')=='map','map')
    if s.get('menu_screen')=='tutorial_prompt':act('menu_select',option='no')
    wait(lambda s:s.get('state_type')=='map','map ready')
    if args.empty_matrix:
        from emptiness import run_matrix
        run_matrix(SimpleNamespace(cmd=cmd,state=state,wait=wait,capture=capture,hand=hand,act=act,out=out,case_filter=args.case_filter),results)
    else:
        for name in args.cases.split(','):
            prepare('reward' if name.startswith('reward') or name=='normal' else 'empty' if name in ['overflow','full'] else name)
            if name=='overflow':
                for _ in range(2):cmd('review_boundary refill');settle()
                for _ in range(9):cmd('card KAREN_DEFEND Hand')
                wait(lambda s:len(s.get('player',{}).get('hand',[]))==9,'nine-card hand');time.sleep(1)
            if name=='full':
                for _ in range(11):cmd('review_boundary refill');settle()
                cmd('card KAREN_TOWER_OF_PROMISE Hand')
                wait(lambda s:any(c['id']=='KAREN_TOWER_OF_PROMISE' for c in s.get('player',{}).get('hand',[])),'tower ready');time.sleep(1)
            capture(name+'-before');before=snap();before_hand=hand(name+'-before')
            record=name in ['empty','void','reward1','tower']
            if record:start_record()
            if name=='overflow':
                for _ in range(3):cmd('review_boundary draw');settle()
                after=snap();current=state()
                assert after['hand']==10 and after['promise']==0 and current['player']['discard_pile_count']==2,(after,current['player'])
            elif name=='full':
                tower=next(c for c in state()['player']['hand'] if c['id']=='KAREN_TOWER_OF_PROMISE')
                act('play_card',card_index=tower['index'])
                wait(lambda s:len(s.get('player',{}).get('hand',[]))==10,'tower fills hand')
                after=snap();assert after['promise']==2,after
            elif name in ['empty','void']:
                cmd('review_boundary draw');settle();time.sleep(1)
                cmd('review_boundary refill');settle();time.sleep(1)
                cmd('review_boundary draw');after=settle();expected=5 if args.phase=='after' else 4
                assert after['energy']==expected,after
            elif name.startswith('reward'):
                take=int(name[-1]);reward(take);after=snap()
                assert after['deckDelta']==take,after
                assert after['choicesDelta']==(0 if args.phase=='after' else 3-take),after
            elif name=='normal':
                reward(1,ordinary=True);after=snap()
                assert after['deckDelta']==1 and after['choicesDelta']==3,after
                assert sum(c['picked'] for c in after['choices'])==1,after
            else:
                cmd('review_boundary run');after=settle()
                if name=='batch':assert after['energy']==4 and after['hand']==3,after
                if name=='infinite':assert after['energy']==3 and after['hand']==3 and after['promise']==10,after
                if name=='swap':assert after['energy']==4 and after['hand']==3 and after['promise']==2,after
                if name=='tower':
                    assert after['failedMoves']==(0 if args.phase=='after' else 8),after
                    assert after['status']==('finished' if args.phase=='after' else 'baseline-stopped-after-8-failures'),after
            time.sleep(2)
            if record:stop_record(name)
            capture(name+'-after');after_hand=hand(name+'-after')
            entry=dict(case=name,result='pass',before=before,after=after,handBefore=before_hand,handAfter=after_hand)
            results.append(entry);print(json.dumps(entry,ensure_ascii=False),flush=True)
            (out/'results.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
except Exception:
    if recording:
        try:stop_record('interrupted')
        except Exception:pass
    try:capture('failure')
    except Exception:pass
    raise
finally:
    (out/'results.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
    desktop.close()
