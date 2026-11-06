"""Targeted real-game verification of selected Ticket and Starlight designs."""
import argparse, hashlib, json, os, pathlib, shutil, subprocess, sys, time, urllib.request

parser=argparse.ArgumentParser()
parser.add_argument('branch',choices=['stable','beta'])
parser.add_argument('--output',required=True,type=pathlib.Path)
parser.add_argument('--phase',choices=['all','remaining','final'],default='all')
args=parser.parse_args()
repo=pathlib.Path(__file__).resolve().parents[3]
sys.path.insert(0,str(repo/'tools/vfx/six-card'))
from isolated_desktop import TestDesktop
root=args.output.resolve();root.mkdir(parents=True,exist_ok=True)
branch=args.branch;port=18947 if branch=='stable' else 18948
subprocess.run([sys.executable,str(repo/'tools/vfx/catalog/prepare.py'),branch,'--output',str(root),'--skip-build','--source-dev','D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/artifacts/dev-game','--port',str(port)],check=True)
dev=root/'isolated'/branch;game=dev/'game';mods=game/'mods';out=root/branch;out.mkdir(exist_ok=True)
shutil.copytree(repo/'artifacts/ShoujoKagekiAijoKaren',mods/'ShoujoKagekiAijoKaren')
for name,project in [('KarenStageProbe',repo/'tools/vfx/starlight/StageProbe.csproj'),('KarenBackgroundTestGuard',repo/'tools/vfx/catalog/guard/Guard.csproj')]:
    target=root/'probes'/branch/name
    subprocess.run(['dotnet','build',str(project),f'-p:Game={game}',f'-p:Variant={branch.title()}','-o',str(target),'--nologo','-v:q'],check=True)
    shutil.copy2(target/(name+'.dll'),mods)
    (mods/(name+'.json')).write_text(json.dumps(dict(id=name,name=name,version='1.0.0',author='Development',description='Isolated targeted stage verification',has_dll=True,has_pck=False,affects_gameplay=False)),encoding='utf-8')
(game/'override.cfg').write_text('[display]\nwindow/size/mode=1\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n',encoding='utf-8')
base=f'http://127.0.0.1:{port}/api/v1'
def api(path,data=None):
    req=urllib.request.Request(base+path,data=None if data is None else json.dumps(data).encode(),headers={'Content-Type':'application/json'})
    with urllib.request.urlopen(req,timeout=25) as res:return json.load(res)
def state():return api('/singleplayer?format=json')
def post(**kw):
    res=api('/singleplayer',kw)
    with (out/'actions.jsonl').open('a',encoding='utf-8') as f:f.write(json.dumps([kw,res],ensure_ascii=False)+'\n')
    assert res.get('status')=='ok',(kw,res)
    return res
def cmd(s):return post(action='run_command',command=s)
def probe():return json.loads(cmd('stage_probe status')['message'])
def save(name,obj):(out/(name+'.json')).write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf-8')
def snap(name):
    save(name,state())
    with urllib.request.urlopen(base+'/screenshot',timeout=25) as res:(out/(name+'.png')).write_bytes(res.read())
def hand(name):
    data=json.loads(cmd('karen_check_hand')['message']);save(name+'-hand',data)
    assert data['modelCount']==data['visualCount'] and not data['missing'] and not data['unexpected'] and not data['unexpectedPileIcons'],data
def wait_turn():
    for _ in range(150):
        if state().get('battle',{}).get('is_play_phase'):return
        time.sleep(.15)
    raise AssertionError('No player turn')
def wait_idle():
    for _ in range(160):
        p=probe()
        if p['allLive']==p['backLive']==p['hudLive']==0:return p
        time.sleep(.06)
    raise AssertionError(('effects did not clean up',p))
def clear_hand():
    post(action='unhover_card')
    for c in list(state()['player']['hand']):cmd('remove_card '+c['id']+' Hand')
    time.sleep(.35)
def add(card,upgrade=False):
    cmd('card '+card+' Hand');time.sleep(.45)
    if upgrade:cmd('stage_probe upgrade '+card);time.sleep(.25)
def play(card):
    s=state();c=next(c for c in s['player']['hand'] if c['id']==card)
    enemies=s['enemies'] if 'enemies' in s else s.get('battle',{}).get('enemies',[])
    kw=dict(action='play_card',card_index=c['index'])
    if card=='KAREN_SHINE_STRIKE':kw['target']=enemies[0]['entity_id']
    return post(**kw)
results={};chapters=[];recording=False;desktop=TestDesktop()
def chapter(label):chapters.append({'label':label,'time':time.monotonic()-started});save('chapters',chapters)
def cast(card,label,kind,key=None,upgrade=False,instant=False,cancel=False):
    clear_hand();add(card,upgrade);cmd('energy 99');cmd('stage_probe watch');before=probe();snap(label+'-before');chapter(label);play(card)
    for _ in range(180):
        p=probe()
        if p['casts'] or (instant and all(c['id']!=card for c in state()['player']['hand'])):break
        time.sleep(.03)
    else:raise AssertionError((label,'no cast'))
    time.sleep(.05 if cancel else .25);snap(label+'-during')
    if cancel:cmd('stage_probe cancel')
    p=wait_idle();time.sleep(.35);snap(label+'-after');hand(label)
    if key:
        for _ in range(160):
            p=probe()
            if p[key]>before[key]:break
            time.sleep(.04)
        else:raise AssertionError((label,'actual gameplay result did not finish',p))
    if instant:assert not p['casts'],p
    else:
        match=[c for c in p['casts'] if c['kind']==kind]
        expected=before['act2']+1 if kind=='Act1Draw' else 1
        assert len(match)==expected,(label,p)
        if key:assert p[key]>before[key],(label,p)
        if not cancel and kind in ('TicketCast','Act1Cast','Act2Cast'):assert match[0]['confirmed'],(label,p)
    save(label+'-probe',p);results[label]=p;print(branch,label,'PASS',flush=True);return p
try:
    env=os.environ.copy();env['APPDATA']=str(dev/'user/AppData/Roaming');env['LOCALAPPDATA']=str(dev/'user/AppData/Local');env['STS2_MCP_FFMPEG']=shutil.which('ffmpeg')
    process=desktop.launch([str(game/'SlayTheSpire2.exe'),'--rendering-driver','vulkan','--force-steam=off','--log-file',str(dev/'godot.log')],game,env)
    for _ in range(180):
        time.sleep(.4)
        try:
            log=(dev/'godot.log').read_text(encoding='utf-8',errors='replace')
            if '[Karen STS2 MCP]' in log and '[Stage Probe] Loaded' in log and 'continue' in state().get('options',[]):break
        except Exception:pass
    else:raise AssertionError('Game/MCP/Karen saved run unavailable')
    post(action='menu_select',option='continue')
    for _ in range(240):
        if state().get('player'):break
        time.sleep(.15)
    else:raise AssertionError('Continued Karen run did not finish loading')
    time.sleep(.6)
    cmd('fight WATERFALL_GIANT_BOSS');wait_turn();time.sleep(1);cmd('stage_probe speed normal')
    if any(r['id']=='KAREN_HAIRPIN_RELIC' for r in state()['player']['relics']):cmd('relic remove KAREN_HAIRPIN_RELIC')
    api('/settings',dict(fullscreen=False,window_width=1280,window_height=720,muted=False));time.sleep(.3)
    record=api('/recording/start',dict(fps=30,max_duration_seconds=240,width=1280,height=720,audio=True));save('recording-start',record);recording=True;started=time.monotonic()
    if args.phase=='all':
        run_core=True
    else:
        run_core=False
        cast('KAREN_STARLIGHT02_CARD','setup-act2','Act2Cast','act2')
    if run_core:
        cast('KAREN_TICKET','ticket-B','TicketCast','ticket')
        clear_hand();cmd('stage_probe watch');chapter('ticket-return');snap('ticket-return-before');post(action='end_turn');wait_turn();time.sleep(1);p=wait_idle()
        assert any(c['kind']=='TicketReturn' and c['cards']==1 for c in p['casts']),p
        snap('ticket-return-after');hand('ticket-return');save('ticket-return-probe',p);results['ticket-return']=p;print(branch,'ticket-return PASS',flush=True)
        for n in (0,2,4):
            cmd('stage_probe promise '+str(n));time.sleep(.5)
            p=cast('KAREN_ON_STAGE','duet-'+str(n),'Duet');event=next(c for c in p['casts'] if c['kind']=='Duet')
            assert event['cards']==n and event['energy']==2,p
        cast('KAREN_STARLIGHT_CARD','act1-cast','Act1Cast','act1',upgrade=True)
        for _ in range(2):cmd('card KAREN_STRIKE Draw')
        time.sleep(.4)
        cast('KAREN_SHINE_STRIKE','act1-draw','Act1Draw');p=results['act1-draw'];assert next(c for c in p['casts'] if c['kind']=='Act1Draw')['cards']==2,p
        cast('KAREN_STARLIGHT02_CARD','act2-bright-cast','Act2Cast','act2')
        def shine(label,plays):
            clear_hand();add('KAREN_SHINE_STRIKE');cmd('energy 99');cmd('stage_probe watch');snap(label+'-before');chapter(label)
            c=next(c for c in state()['player']['hand'] if c['id']=='KAREN_SHINE_STRIKE');post(action='hover_card',card_index=c['index']);time.sleep(.55);snap(label+'-hover')
            time.sleep(3);p=probe();assert p['allLive']==1 and p['casts'][0]['backCorrect'],p
            post(action='unhover_card');wait_idle()
            cmd('stage_probe watch');post(action='hover_card',card_index=c['index']);time.sleep(.3);play('KAREN_SHINE_STRIKE');time.sleep(.6);snap(label+'-during');p=wait_idle();time.sleep(.35);snap(label+'-after');hand(label)
            hover=[x for x in p['casts'] if x['kind']=='Act2Hover'];assert len(hover)==1,p
            assert hover[0]['playIndices']==list(range(plays)) and hover[0]['ownedFrames']>0 and hover[0]['settledHeldFrames']>0,(label,p)
            assert p['shine']>0,p
            save(label+'-probe',p);results[label]=p;print(branch,label,'PASS',flush=True)
        shine('act2-three',3)
        cast('KAREN_STARLIGHT02_CARD','act2-stack-cast','Act2Cast','act2');shine('act2-five',5)
    if args.phase=='final':
        cast('KAREN_STARLIGHT_CARD','act1-final-cast','Act1Cast','act1',upgrade=True)
        for _ in range(2):cmd('card KAREN_STRIKE Draw')
        time.sleep(.4)
        p=cast('KAREN_SHINE_STRIKE','act1-final-edge','Act1Draw')
        assert any(c['kind']=='Act1Draw' and c['cards']==2 for c in p['casts']),p
        clear_hand();add('KAREN_SHINE_STRIKE');cmd('energy 99');cmd('stage_probe weaken');cmd('stage_probe watch');snap('lethal-before');chapter('lethal')
        c=next(c for c in state()['player']['hand'] if c['id']=='KAREN_SHINE_STRIKE');post(action='hover_card',card_index=c['index']);time.sleep(.5);snap('lethal-hover');play('KAREN_SHINE_STRIKE')
        time.sleep(.4);snap('lethal-during');p=wait_idle();time.sleep(.5);snap('lethal-after');hand('lethal')
        hover=[c for c in p['casts'] if c['kind']=='Act2Hover'];assert len(hover)==1 and hover[0]['ownedFrames']>0 and hover[0]['endingHeldFrames']>0,p
        save('lethal-probe',p);results['lethal']=p;print(branch,'lethal PASS',flush=True)
    else:
        cast('KAREN_STARLIGHT02_CARD','cast-presentation-cancel','Act2Cast','act2',cancel=True)
        cmd('stage_probe speed fast');cast('KAREN_TICKET','ticket-fast','TicketCast','ticket');cast('KAREN_STARLIGHT02_CARD','act2-fast','Act2Cast','act2')
        while probe()['ticket']<2:cast('KAREN_TICKET','ticket-stack','TicketCast','ticket')
        clear_hand();cmd('heal 999');cmd('stage_probe watch');chapter('ticket-return-two');snap('ticket-return-two-before');post(action='end_turn');wait_turn();time.sleep(.6)
        p=wait_idle();assert any(c['kind']=='TicketReturn' and c['cards']==probe()['ticket'] for c in p['casts']),p
        snap('ticket-return-two-after');hand('ticket-return-two');save('ticket-return-two-probe',p);results['ticket-return-two']=p;print(branch,'ticket-return-two PASS',flush=True)
        cmd('stage_probe speed instant');cast('KAREN_TICKET','ticket-instant','TicketCast',instant=True);cast('KAREN_STARLIGHT02_CARD','act2-instant','Act2Cast',instant=True)
        clear_hand();add('KAREN_SHINE_STRIKE');cmd('stage_probe watch');c=next(c for c in state()['player']['hand'] if c['id']=='KAREN_SHINE_STRIKE');post(action='hover_card',card_index=c['index']);time.sleep(.4);assert probe()['allLive']==0
        post(action='unhover_card');cmd('stage_probe speed normal')
    save('results',results)
    build={str(p.relative_to(mods/'ShoujoKagekiAijoKaren')):hashlib.sha256(p.read_bytes()).hexdigest() for p in (mods/'ShoujoKagekiAijoKaren').rglob('*') if p.is_file()};save('build',build)
finally:
    if recording:
        try:
            api('/recording/stop',{})
            for _ in range(120):
                rec=api('/recording')
                if rec.get('status')=='completed':
                    save('recording',rec)
                    if rec.get('output_path'):shutil.copy2(rec['output_path'],out/'gameplay.mp4')
                    break
                time.sleep(.3)
        except Exception as error:save('recording-error',{'error':str(error)})
    desktop.close()
    if (dev/'godot.log').exists():shutil.copy2(dev/'godot.log',out/'godot.log')
