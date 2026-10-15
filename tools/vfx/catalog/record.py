import ctypes, hashlib, json, os, pathlib, shutil, socket, subprocess, sys, threading, time, urllib.request
import argparse
parser=argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable','beta'])
parser.add_argument('--output',type=pathlib.Path,required=True)
parser.add_argument('--scenes',default='')
parser.add_argument('--setup-wait',type=float,default=2.2)
parser.add_argument('--port',type=int)
parser.add_argument('--package',type=pathlib.Path)
parser.add_argument('--speed',choices=['normal','fast'],default='normal')
args=parser.parse_args()
assert args.setup_wait >= 0
root=args.output.resolve()
main=pathlib.Path(__file__).resolve().parents[3]
branch=args.branch; dev=root/'isolated'/branch; game=dev/'game'; mods=game/'mods'
port=args.port or (18637 if branch=='stable' else 18638)
with socket.socket() as check:
    check.settimeout(.3)
    assert check.connect_ex(('127.0.0.1',port))!=0, 'Test port is already occupied; no deployment performed'
base=f'http://127.0.0.1:{port}/api/v1'
out=root/branch;out.mkdir(exist_ok=True)
def api(path,data=None):
    req=urllib.request.Request(base+path,data=None if data is None else json.dumps(data).encode(),headers={'Content-Type':'application/json'})
    with urllib.request.urlopen(req,timeout=30) as r:return json.load(r)
def state():return api('/singleplayer?format=json')
def post(**kw):
    r=api('/singleplayer',kw)
    with (out/'actions.jsonl').open('a',encoding='utf-8') as f:f.write(json.dumps([kw,r],ensure_ascii=False)+'\n')
    assert r.get('status')=='ok',(kw,r)
    return r
def cmd(s):return post(action='run_command',command=s)
def snap(dest):
    dest.with_suffix('.json').write_text(json.dumps(state(),ensure_ascii=False,indent=2),encoding='utf-8')
    if not recording:
        with urllib.request.urlopen(base+'/screenshot',timeout=30) as r:dest.with_suffix('.png').write_bytes(r.read())
backups={}
def backup(p):
    backups[p]=p.read_bytes() if p.exists() else None
for p in [game/'override.cfg',mods/'KarenSTS2MCP.conf']:
    backup(p)
for p in (dev/'user/AppData/Roaming/SlayTheSpire2').rglob('settings*'):
    if p.is_file():backup(p)
(out/'backup').mkdir(exist_ok=True)
for i,(p,data) in enumerate(backups.items()):
    if data is not None:(out/'backup'/str(i)).write_bytes(data)
(out/'backup/index.json').write_text(json.dumps([str(p) for p in backups],indent=2))
assert backups[game/'override.cfg'] is None,'Existing override requires merge'
process=None;stop=threading.Event();recording=False;owned=[]
monitor={'samples':0,'foreground_samples':0,'restored_samples':0,'minimized_samples':0}
u=ctypes.windll.user32
u.GetForegroundWindow.restype=ctypes.c_void_p
u.GetWindowThreadProcessId.argtypes=[ctypes.c_void_p,ctypes.POINTER(ctypes.c_ulong)]
callback_type=ctypes.WINFUNCTYPE(ctypes.c_bool,ctypes.c_void_p,ctypes.c_void_p)
u.IsIconic.argtypes=[ctypes.c_void_p];u.IsWindowVisible.argtypes=[ctypes.c_void_p]
def monitor_loop():
    while not stop.is_set():
        monitor['samples']+=1
        pid=ctypes.c_ulong();u.GetWindowThreadProcessId(u.GetForegroundWindow(),ctypes.byref(pid))
        if process and pid.value==process.pid:monitor['foreground_samples']+=1
        @callback_type
        def window(hwnd,_):
            pid=ctypes.c_ulong();u.GetWindowThreadProcessId(hwnd,ctypes.byref(pid))
            if process and pid.value==process.pid and u.IsWindowVisible(hwnd):
                monitor['minimized_samples' if u.IsIconic(hwnd) else 'restored_samples']+=1
            return True
        u.EnumWindows(window,0);stop.wait(.02)
try:
    shutil.copytree(args.package or main/'artifacts/ShoujoKagekiAijoKaren',mods/'ShoujoKagekiAijoKaren',dirs_exist_ok=True)
    package=mods/'ShoujoKagekiAijoKaren'
    capture_build=dict(assemblySha256=hashlib.sha256((package/'lib'/f'ShoujoKagekiAijoKaren.{branch.title()}.dll').read_bytes()).hexdigest(),
                       packSha256=hashlib.sha256((package/'ShoujoKagekiAijoKaren.pck').read_bytes()).hexdigest())
    for name,src in [('KarenBackgroundTestGuard',(root/'guard')/branch/'KarenBackgroundTestGuard.dll'),('KarenPromiseTransformTests',(root/'probes')/branch/'KarenPromiseTransformTests.dll')]:
        for suffix in ['.dll','.json']:
            target=mods/(name+suffix);assert not target.exists(),target;owned.append(target)
        shutil.copy2(src,mods/(name+'.dll'))
        manifest={'id':name,'name':name,'version':'1.0.0','author':'Development','description':'Temporary isolated VFX test','has_dll':True,'has_pck':False,'affects_gameplay':False}
        (mods/(name+'.json')).write_text(json.dumps(manifest),encoding='utf-8')
    (game/'override.cfg').write_text('[display]\nwindow/size/mode=1\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n',encoding='utf-8')
    conf=json.loads(backups[mods/'KarenSTS2MCP.conf'].decode('utf-8-sig'));conf.update(fullscreen=False,muted=True)
    (mods/'KarenSTS2MCP.conf').write_text(json.dumps(conf),encoding='utf-8')
    env=os.environ.copy();env['APPDATA']=str(dev/'user/AppData/Roaming');env['LOCALAPPDATA']=str(dev/'user/AppData/Local');env['STS2_MCP_FFMPEG']=shutil.which('ffmpeg')
    startup=subprocess.STARTUPINFO();startup.dwFlags=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
    t=threading.Thread(target=monitor_loop,daemon=True);t.start()
    process=subprocess.Popen([str(game/'SlayTheSpire2.exe'),'--rendering-driver','vulkan','--force-steam=off','--log-file',str(dev/'godot.log')],cwd=game,env=env,startupinfo=startup)
    for _ in range(100):
        time.sleep(.5)
        try:
            if '[Karen STS2 MCP]' in (dev/'godot.log').read_text(encoding='utf-8',errors='replace') and 'continue' in state().get('options',[]):break
        except Exception:pass
    else:raise RuntimeError('Game not ready')
    log=(dev/'godot.log').read_text(encoding='utf-8',errors='replace')
    assert '[Karen STS2 MCP]' in log and '[Background Test Guard]' in log
    api('/window/minimize',{})
    assert monitor['foreground_samples']==0 and monitor['restored_samples']==0,monitor
    # Let menu background preloading settle before requesting Continue.
    for _ in range(160):
        if "Preloading 'Common' Complete:" in (dev/'godot.log').read_text(encoding='utf-8',errors='replace'):break
        time.sleep(.25)
    else:raise RuntimeError('Common asset preload did not finish')
    if 'continue' in state().get('options',[]):post(action='menu_select',option='continue');time.sleep(3)
    for _ in range(100):
        current=state()
        if current.get('player') and (current.get('battle',{}).get('is_play_phase') or current.get('state_type') in ('rewards','map')):break
        time.sleep(.25)
    else:raise RuntimeError('Continue did not load the saved run')
    case=out/'normal';case.mkdir(exist_ok=True)
    def save(name,value):
        (case/(name+'.json')).write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8')
    def check_hand(name):
        r=cmd('karen_check_hand');save(name,r)
        h=json.loads(r['message']);assert h['modelCount']==h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'],h
    def record_start():
        global recording
        snap(case/'recording-before');check_hand('recording-before-hand')
        api('/settings',dict(muted=False));time.sleep(.3)
        api('/recording/start',dict(fps=30,max_duration_seconds=180,width=1280,height=720,audio=True));recording=True
    def record_stop():
        global recording
        api('/recording/stop',{})
        for _ in range(100):
            rec=api('/recording')
            if rec['status'] in ('completed','failed'):break
            time.sleep(.2)
        assert rec['status']=='completed' and rec.get('audio') and rec.get('audio_source')=='game_process',rec
        rec['captureBuild']=capture_build
        shutil.copy2(rec['output_path'],case/'game.mp4');save('recording',rec)
        recording=False; snap(case/'recording-after')
        api('/settings',dict(muted=True))
    def add(card):
        cmd('card '+card+' Hand');time.sleep(.55)
    def play(card):
        current=state();c=next(c for c in current['player']['hand'] if c['id']==card)
        args={'action':'play_card','card_index':c['index']}
        if c['target_type']=='AnyEnemy':args['target']=current['battle']['enemies'][0]['entity_id']
        post(**args)
    def fight():
        cmd('fight WATERFALL_GIANT_BOSS')
        for _ in range(100):
            current=state()
            if current.get('battle',{}).get('is_play_phase') and any(e.get('entity_id','').startswith('WATERFALL') for e in current.get('battle',{}).get('enemies',[])):break
            time.sleep(.2)
        else:raise RuntimeError('Target combat not ready')
        time.sleep(.65);cmd('energy 99')
        for _ in range(15):
            cards=state()['player']['hand']
            if len(cards)<=3:break
            cmd('remove_card '+cards[-1]['id']+' Hand');time.sleep(.25)
        time.sleep(args.setup_wait)

    def integration(): return json.loads(cmd('promise_integration status')['message'])
    def trim():
        for card in list(state()['player']['hand']):
            cmd('remove_card '+card['id']+' Hand'); time.sleep(.15)
        time.sleep(.5)
    def settle(name):
        time.sleep(2.2)
        s=integration()
        for _ in range(100):
            if s['entryCount']==0 and s['flightCount']==0 and s['identitiesMatch']:break
            time.sleep(.1);s=integration()
        save(name+'-state',s); snap(case/name); check_hand(name+'-hand')
        assert s['identitiesMatch'] and s['anchorError']<.1 and s['flightModelMismatchFrames']==0,s
        assert s['entryCount']==0 and s['flightCount']==0,s
        if s['voidMode']:
            for _ in range(70):
                orbit=api('/singleplayer',dict(action='run_command',command='void_orbit_check'))
                if orbit.get('status')=='ok':break
                time.sleep(.1)
            save(name+'-orbit',orbit);assert orbit.get('status')=='ok',orbit
        return s
    def begin(name,void=False,seed=True):
        global case
        cmd('promise_integration off');time.sleep(1.5);fight();trim()
        case=out/name;case.mkdir(exist_ok=True)
        if void:
            add('KAREN_VOID');snap(case/'before-void');play('KAREN_VOID');time.sleep(2.5)
            check_hand('after-void-hand');snap(case/'after-void')
        if seed:cmd('bridge_vfx '+('void' if void else 'prepare'));time.sleep(1.5)
        cmd('promise_integration watch');settle('initial')
        print(branch,name,'started',flush=True)
    def cast(card,label=None):
        label=label or card.lower().removeprefix('karen_')
        if card not in [c['id'] for c in state()['player']['hand']]:add(card)
        snap(case/(label+'-before'));play(card);time.sleep(.10);snap(case/(label+'-during'))
        return settle(label+'-after')
    def select_first(label):
        for _ in range(100):
            s=state()
            if s['state_type'] in ('hand_select','card_select'):break
            time.sleep(.1)
        else:raise AssertionError(('selection not ready',s))
        snap(case/(label+'-selection'))
        if s['state_type']=='hand_select':
            post(action='combat_select_card',card_index=s['hand_select']['cards'][0]['index'])
            if state()['state_type']=='hand_select':post(action='combat_confirm_selection')
        else:
            selection=s['card_select'];post(action='select_card',index=selection['cards'][0]['index'])
            if state()['state_type']=='card_select':post(action='confirm_selection')
            # Multi-choice screens expose confirmation only when needed.
        time.sleep(.12);snap(case/(label+'-moving'));return settle(label+'-after')
    wanted=set(args.scenes.split(',')) if args.scenes else set()
    def want(name): return not wanted or name in wanted
    def marking(title):
        marks.append({'time':max(0,time.monotonic()-record_clock),'title':title})
    original_record_start=record_start
    original_record_stop=record_stop
    def record_start():
        global marks,record_clock
        original_record_start();record_clock=time.monotonic();marks=[]
        time.sleep(.4)
    def record_stop():
        time.sleep(.65);original_record_stop();save('markers',marks)
    def stage(name,void=False,seed=True):
        begin(name,void,seed)
        cmd('bridge_vfx watch')
        cmd('stage_probe watch')
        # Native atlas readback is expensive; reserve pixel-level contour checks
        # for the dedicated border scenes instead of every unrelated VFX cast.
        cmd('revision_probe '+('watch-border' if name.startswith('shine-ambient') else 'watch'))
        cmd('shine_motion watch')
        cmd('showcase restore-relics')
        cmd('bridge_vfx energy2')
        # Enough energy for individual demonstrations, without a distracting 100+ counter.
        cmd('energy 8');time.sleep(.3)
    def prep(card): add(card)
    def clip_cast(card,title):
        marking(title);snap(case/(card+'-before'));play(card);time.sleep(.12);snap(case/(card+'-during'))
        if card=='KAREN_VOID':time.sleep(2.5)
        return settle(card+'-after')
    def finish():
        save('final-state',integration());check_hand('final-hand')
        save('native-finished',json.loads(cmd('bridge_vfx status')['message']))
        save('stage-probe',json.loads(cmd('stage_probe status')['message']))
        save('revision-probe',json.loads(cmd('revision_probe status')['message']))
        save('shine-motion',json.loads(cmd('shine_motion status')['message']))
        record_stop();print('RECORDED',case.name,flush=True)
    cmd('native_speed '+args.speed);fight()
    if want('tower'):
        stage('tower',seed=False)
        for card in ['KAREN_DEFEND','KAREN_STRIKE','KAREN_FALL','KAREN_TOWER_OF_PROMISE']:prep(card)
        record_start();marking('约定牌堆｜坠落：选牌入塔');play('KAREN_FALL');select_first('fall')
        marking('约定之塔｜拖住时持续点亮塔腿与平台')
        cmd('tower_recall drag KAREN_TOWER_OF_PROMISE');time.sleep(2)
        save('drag-probe',json.loads(cmd('tower_recall status')['message']))
        marking('取消拖牌｜灯光淡出，库存保持不变')
        cmd('tower_recall cancel');time.sleep(1)
        clip_cast('KAREN_TOWER_OF_PROMISE','约定之塔｜蓄光后取牌，尾迹抵达实际手牌位置');finish()
    if want('shine'):
        stage('shine',seed=False);prep('KAREN_SHINE_STRIKE');cmd('promise_integration shine1')
        record_start();marking('闪耀临界提示｜最后一点闪耀的柔和星光');time.sleep(2);clip_cast('KAREN_SHINE_STRIKE','闪耀打击｜闪耀耗尽与收牌');finish()
    if want('native'):
        stage('native',seed=False)
        for card in ['KAREN_STRIKE','KAREN_HOLY_STAR','KAREN_DEFEND']:prep(card)
        record_start()
        for card,title in [('KAREN_STRIKE','打击｜共用单段斩击'),('KAREN_HOLY_STAR','神圣星辰｜共用多段斩击'),('KAREN_DEFEND','防御｜原生格挡反馈')]:clip_cast(card,title)
        finish()
    if want('yesterglow'):
        stage('yesterglow');cmd('card KAREN_DEFEND Discard');cmd('card KAREN_STRIKE Discard');prep('KAREN_YESTERGLOW')
        record_start();marking('昨日夜空｜从弃牌堆选择并加入约定牌堆');play('KAREN_YESTERGLOW');select_first('yesterglow');finish()
    if want('burn'):
        stage('burn');prep('KAREN_BURN');prep('KAREN_PROMISE_DRAW')
        record_start();clip_cast('KAREN_BURN','燃烧吧燃烧吧｜人物火焰与塔模式')
        clip_cast('KAREN_PROMISE_DRAW','燃烧联动｜取出的手牌燃烧边框');time.sleep(1);finish()
    if want('past'):
        stage('past');prep('KAREN_PAST_AND_FUTURE');prep('KAREN_PROMISE_DRAW')
        record_start();clip_cast('KAREN_PAST_AND_FUTURE','过去与未来｜环绕光带')
        clip_cast('KAREN_PROMISE_DRAW','过去与未来联动｜取牌穿环与脉冲');finish()
    if want('fuel'):
        stage('fuel',seed=False);prep('KAREN_DROP_FUEL');cmd('bridge_vfx energy2')
        record_start();clip_cast('KAREN_DROP_FUEL','投下燃料｜卡面爆散、随机粒子汇聚与回能');finish()
    if want('guilt'):
        stage('guilt',seed=False);prep('KAREN_CARRYING_GUILT')
        record_start();marking('我们犯下的罪过｜目标聚光预览')
        cmd('guilt_hover on');time.sleep(1.8);snap(case/'focused');cmd('guilt_hover off');time.sleep(.7)
        clip_cast('KAREN_CARRYING_GUILT','我们犯下的罪过｜四束舞台灯命中');finish()
    if want('relic-pick'):
        stage('relic-pick',seed=False);save('relic-help',cmd('help relic'))
        cmd('relic ANCHOR');cmd('relic PEN_NIB');time.sleep(.6);prep('KAREN_PICK_STAR');cmd('bridge_vfx energy2')
        record_start();marking('摘星｜待禁用遗物的星光预告');cmd('showcase relichover KAREN_PICK_STAR');time.sleep(1.7);snap(case/'preview')
        cmd('showcase relicoff');clip_cast('KAREN_PICK_STAR','摘星｜遗物星光汇入实际能量图标，回能 2')
        assert state()['player']['energy'] == 4, state()['player']
        cmd('showcase restore-relics');prep('KAREN_PICK_STAR');cmd('stage_probe upgrade KAREN_PICK_STAR');cmd('bridge_vfx energy2')
        clip_cast('KAREN_PICK_STAR','摘星＋｜禁用一个遗物，星光仍落入能量图标')
        assert state()['player']['energy'] == 4, state()['player']
        relic_report=json.loads(cmd('revision_probe status')['message'])
        assert len(relic_report['absorbs'])==3 and relic_report['liveAbsorbs']==0, relic_report
        assert all(s['destination']=='Energy' and s['landings']==6 and s['landingError']<.1 and s['canvasError']<.1 for s in relic_report['absorbs']), relic_report
        finish()
    if want('relic-crime'):
        stage('relic-crime',seed=False);cmd('relic ANCHOR');cmd('relic PEN_NIB');time.sleep(.6);prep('KAREN_STAR_CRIME')
        cmd('showcase clear')
        for _ in range(7):cmd('card KAREN_DEFEND Draw');time.sleep(.15)
        record_start();marking('星罪｜待禁用遗物的星光预告');cmd('showcase relichover KAREN_STAR_CRIME');time.sleep(1.7);snap(case/'preview')
        before=len(state()['player']['hand'])
        cmd('showcase relicoff');clip_cast('KAREN_STAR_CRIME','星罪｜遗物星光汇入实际抽牌堆图标，抽牌 3')
        assert len(state()['player']['hand'])==before+2, state()['player']
        cmd('showcase restore-relics');prep('KAREN_STAR_CRIME');cmd('stage_probe upgrade KAREN_STAR_CRIME')
        before=len(state()['player']['hand'])
        clip_cast('KAREN_STAR_CRIME','星罪＋｜禁用一个遗物，星光仍落入抽牌堆图标')
        assert len(state()['player']['hand'])==before+2, state()['player']
        relic_report=json.loads(cmd('revision_probe status')['message'])
        assert len(relic_report['absorbs'])==3 and relic_report['liveAbsorbs']==0, relic_report
        assert all(s['destination']=='DrawPile' and s['landings']==6 and s['landingError']<.1 and s['canvasError']<.1 for s in relic_report['absorbs']), relic_report
        finish()
    if want('geer'):
        stage('geer');prep('KAREN_GEER')
        record_start();clip_cast('KAREN_GEER','命运的齿轮｜施放机械演出')
        marking('命运的齿轮｜回合开始、选牌、自动打出');post(action='end_turn');select_first('geer');finish()
    if want('void-activation'):
        stage('void-activation');prep('KAREN_VOID')
        record_start();time.sleep(.8);clip_cast('KAREN_VOID','世界上最空虚的人｜抽牌堆销毁与塔星迁移');time.sleep(1);finish()
    if want('void-geer'):
        stage('void-geer',void=True)
        for _ in range(8):cmd('card KAREN_DEFEND Draw');time.sleep(.12)
        time.sleep(.8);prep('KAREN_GEER')
        record_start();clip_cast('KAREN_GEER','空虚 × 命运的齿轮｜施放')
        marking('空虚 × 齿轮｜从新抽牌堆选择并自动打出');post(action='end_turn');select_first('geer');finish()
    if want('void-fuel-stack'):
        stage('void-fuel-stack',void=True)
        for card in ['KAREN_BURN','KAREN_PAST_AND_FUTURE','KAREN_FORM']:cast(card)
        cmd('showcase seed KAREN_DEFEND 5');time.sleep(.8);prep('KAREN_DROP_FUEL');cmd('bridge_vfx energy2')
        record_start();clip_cast('KAREN_DROP_FUEL','空虚＋燃烧＋过去与未来＋觉醒形态｜投下燃料抽牌');time.sleep(1);finish()
    if want('replay'):
        stage('replay',seed=False)
        for _ in range(9):cmd('card KAREN_DEFEND Draw');time.sleep(.08)
        prep('KAREN_STARLIGHT02_CARD');prep('KAREN_DROP_FUEL')
        record_start();clip_cast('KAREN_STARLIGHT02_CARD','Starlight 第二幕｜赋予闪耀牌额外重播')
        marking('第二幕 × 投下燃料｜三次演出与闪耀耗尽')
        snap(case/'KAREN_DROP_FUEL-before');play('KAREN_DROP_FUEL')
        time.sleep(.12);snap(case/'KAREN_DROP_FUEL-during')
        # Promise-pile flights can be empty while native replay is still drawing.
        # Wait for all three real CardPlay completions before inspecting settled hands.
        for _ in range(160):
            history=json.loads(cmd('bridge_vfx status')['message'])
            if sum(e['id']=='KAREN_DROP_FUEL' for e in history['finished'])==3:break
            time.sleep(.1)
        else:raise AssertionError(('Fuel replays did not finish',history))
        save('replay-native-finished',history)
        settle('KAREN_DROP_FUEL-after');time.sleep(1)
        settle('replay-complete');assert state()['player']['energy']==14,state()
        finish()
    for void in [False,True]:
        name='allstack-void' if void else 'allstack-normal'
        if not want(name):continue
        stage(name,void,seed=False)
        for card in ['KAREN_BURN','KAREN_PAST_AND_FUTURE','KAREN_FORM','KAREN_STAGE_REPRODUCE']:cast(card)
        prep('KAREN_PROMISE_DRAW');prep('KAREN_TOWER_OF_PROMISE');prep('KAREN_BRIDGE');cmd('bridge_vfx energy2')
        record_start();clip_cast('KAREN_PROMISE_DRAW',('空虚｜' if void else '普通｜')+'燃烧＋光环＋觉醒＋再生产：取牌并补足')
        clip_cast('KAREN_TOWER_OF_PROMISE','新旧联动｜塔取牌尾迹＋牌缘火焰＋光环＋再生产补牌')
        marking('多卡联动｜塔桥自动出牌、追光与再生产持续补牌');play('KAREN_BRIDGE')
        time.sleep(.8)
        for _ in range(160):
            bs=json.loads(cmd('bridge_vfx status')['message'])
            complete=any(e['id']=='KAREN_BRIDGE' and not e['auto'] for e in bs['finished'])
            if complete and bs['nodeCount']==0:break
            time.sleep(.15)
        else:raise AssertionError(bs)
        save('bridge-completed',bs);settle('bridge-after');finish()
    exec(compile((pathlib.Path(__file__).with_name('extra_scenes.py')).read_text(encoding='utf-8'), 'extra_scenes.py', 'exec'))
    exec(compile((pathlib.Path(__file__).with_name('benefit_scenes.py')).read_text(encoding='utf-8'), 'benefit_scenes.py', 'exec'))
    if want('lastword'):
        stage('lastword',seed=True);cmd('showcase clear');trim();prep('KAREN_LAST_WORD');time.sleep(1)
        snap(case/'playable');check_hand('playable-hand')
        cmd('promise_integration off')
        record_start();marking('最后的台词｜满足条件后的牌框提示');time.sleep(1.4)
        marking('最后的台词｜文字、影片与最终伤害');play('KAREN_LAST_WORD');time.sleep(.4);snap(case/'letters')
        time.sleep(2.3);snap(case/'movie')
        for _ in range(160):
            s=state()
            if s['state_type']=='rewards':break
            time.sleep(.15)
        else:raise AssertionError(('No reward screen',s))
        marking('最后的台词｜伤害结算与战斗收尾');snap(case/'rewards');time.sleep(1.5);record_stop();print('RECORDED lastword',flush=True)
    cmd('native_speed restore')


finally:
    if recording:
        try:
            api('/recording/stop',{})
            for _ in range(100):
                if api('/recording')['status'] in ['completed','failed']:break
                time.sleep(.2)
        except Exception:pass
    if process and process.poll() is None:process.terminate();process.wait(timeout=15)
    stop.set()
    if 't' in globals():t.join(timeout=2)
    (out/'window-monitor.json').write_text(json.dumps(monitor,indent=2))
    if (dev/'godot.log').exists():shutil.copy2(dev/'godot.log',out/'godot.log')
    for p,data in backups.items():
        if data is None:p.unlink(missing_ok=True)
        else:p.write_bytes(data)
    for p in owned:p.unlink(missing_ok=True)
assert monitor['foreground_samples']==0 and monitor['restored_samples']==0,monitor
