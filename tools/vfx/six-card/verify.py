"""Six actual card casts, power generation, cleanup and captured game audio in fresh isolated games."""
import argparse, ctypes, hashlib, json, os, pathlib, shutil, socket, subprocess, threading, time, urllib.request
from isolated_desktop import TestDesktop
from audit_log import audit

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
parser.add_argument('--source-repo', type=pathlib.Path, required=True)
parser.add_argument('--output', type=pathlib.Path, required=True)
parser.add_argument('--focused', action='store_true', help='Skip the extra Banana upgrade, speed and scale cases')
parser.add_argument('--ticket-only', action='store_true', help='Only Ticket activation, turn delivery, stacking and speed/cancellation cases')
args = parser.parse_args()
main = pathlib.Path(__file__).resolve().parents[3]
root = args.output.resolve(); root.mkdir(parents=True, exist_ok=True)
branch = args.branch; source = args.source_repo.resolve() / 'artifacts/dev-game' / branch
dev = root / 'isolated' / branch; game = dev / 'game'; mods = game / 'mods'
out = root / branch; out.mkdir(exist_ok=True)
port = 18937 if branch == 'stable' else 18938
with socket.socket() as check:
    assert check.connect_ex(('127.0.0.1', port)) != 0, 'Test port occupied'
assert not dev.exists(), 'Use a fresh output directory for each run'
assert (source / 'game/SlayTheSpire2.exe').is_file()

def link_or_copy(src, dst):
    try: os.link(src, dst)
    except OSError: shutil.copy2(src, dst)
    return dst

# Hardlink read-only game binaries only. Mutable mods, configuration and saves are copies.
shutil.copytree(source / 'game', game, copy_function=link_or_copy,
                ignore=lambda folder, names: ['mods', 'override.cfg'] if pathlib.Path(folder) == source / 'game' else [])
mods.mkdir(); shutil.copytree(source / 'game/mods/BaseLib', mods / 'BaseLib')
for suffix in ['dll', 'json']: shutil.copy2(source / f'game/mods/KarenSTS2MCP.{suffix}', mods)
(mods / 'KarenSTS2MCP.conf').write_text(json.dumps(dict(port=port, fullscreen=False, muted=True, skip_first_prompt=True)), encoding='utf-8')
shutil.copytree(source / 'user', dev / 'user')
for settings in (dev / 'user').rglob('settings.save'):
    data = json.loads(settings.read_text(encoding='utf-8-sig'))
    data.update(mute_in_background=False, limit_fps_in_background=False)
    settings.write_text(json.dumps(data, ensure_ascii=False), encoding='utf-8')
shutil.copytree(main / 'artifacts/ShoujoKagekiAijoKaren', mods / 'ShoujoKagekiAijoKaren')
for name, project in [('KarenSixCardProbe', main / 'tools/vfx/six-card/SixCardProbe.csproj'),
                      ('KarenBackgroundTestGuard', main / 'tools/vfx/catalog/guard/Guard.csproj')]:
    target = root / 'plugins' / branch / name
    subprocess.run(['dotnet', 'build', str(project), f'-p:Game={source / "game"}', f'-p:Variant={branch.title()}',
                    '-o', str(target), '--nologo', '-v:q'], check=True)
    shutil.copy2(target / (name + '.dll'), mods)
    (mods / (name + '.json')).write_text(json.dumps(dict(id=name, name=name, version='1.0.0', author='Development',
        description='Temporary isolated Banana VFX test', has_dll=True, has_pck=False, affects_gameplay=False)), encoding='utf-8')
(game / 'override.cfg').write_text('[display]\nwindow/size/mode=1\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n', encoding='utf-8')
base = f'http://127.0.0.1:{port}/api/v1'
def api(path, data=None):
    req = urllib.request.Request(base + path, data=None if data is None else json.dumps(data).encode(), headers={'Content-Type':'application/json'})
    with urllib.request.urlopen(req, timeout=30) as response: return json.load(response)
def state(): return api('/singleplayer?format=json')
def post(**kw):
    response = api('/singleplayer', kw)
    with (out / 'actions.jsonl').open('a', encoding='utf-8') as f: f.write(json.dumps([kw, response], ensure_ascii=False) + '\n')
    assert response.get('status') == 'ok', (kw, response)
    return response
def cmd(command): return post(action='run_command', command=command)
def save(name, data): (out / (name + '.json')).write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
def snap(name):
    save(name, state())
    with urllib.request.urlopen(base + '/screenshot', timeout=30) as response: (out / (name + '.png')).write_bytes(response.read())
def probe(): return json.loads(cmd('banana_probe status')['message'])
def wait_play():
    for _ in range(120):
        if state().get('battle', {}).get('is_play_phase'): return
        time.sleep(.2)
    raise AssertionError('Player turn did not become ready')
def hand_check(name):
    result = json.loads(cmd('karen_check_hand')['message']); save(name, result)
    assert result['modelCount'] == result['visualCount'] and not result['missing'] and not result['unexpected'] and not result['unexpectedPileIcons'], result
def add(card, upgrade=False):
    cmd('card ' + card + ' Hand'); time.sleep(.55)
    if upgrade: cmd('banana_probe upgrade ' + card)
def play(card):
    actual = next(c for c in state()['player']['hand'] if c['id'] == card)
    return post(action='play_card', card_index=actual['index'])
def clear_potions():
    for potion in list(state()['player'].get('potions', [])): post(action='discard_potion', slot=potion['slot'])
    time.sleep(.2)
def cast(card, label, gain=None, success=None, upgrade=False, instant=False, cancel=False):
    add(card, upgrade); cmd('banana_probe watch'); before_strength = probe()['strength']; snap(label + '-before')
    before_state=state()['player']
    before_count=sum(c['id']==card for c in before_state['hand'])
    before_potions=len(before_state.get('potions',[]))
    play(card)
    # Native play_card queues a card animation before OnPlay. Observe actual VFX creation,
    # rather than timing from HTTP acknowledgement (especially after a turn transition).
    for _ in range(160):
        report = probe()
        if report['casts'] or (instant and sum(c['id']==card for c in state()['player']['hand'])<before_count): break
        time.sleep(.04)
    else: raise AssertionError((label, 'Actual card cast did not start'))
    time.sleep(.06 if cancel else .55); snap(label + '-during')
    if cancel: cmd('banana_probe cancel')
    for _ in range(160):
        report = probe()
        if report['live'] == 0 and report['steamLive'] == 0 and report['hudLive']==0: break
        time.sleep(.06)
    else: raise AssertionError((label, 'VFX did not clean up', report))
    time.sleep(.5); snap(label + '-after'); hand_check(label + '-hand')
    report = probe(); save(label + '-probe', report)
    assert report['live'] == 0 and report['steamLive'] == 0 and report['hudLive']==0, report
    assert all(r['exists'] for r in report['resources']) and report['shader'], report
    if instant:
        assert report['casts'] == [], report
        if gain is not None:assert report['strength']==before_strength+gain,report
        if success is not None:assert len(state()['player'].get('potions',[]))==before_potions+(1 if success else 0),report
    else:
        assert len(report['casts']) == 1, report
        event = report['casts'][0]
        assert not event['falsePulse'], report
        if gain is not None:
            if cancel: assert report['strength'] == before_strength + gain, report
            else: assert event['gain'] == gain and event['pulse'], report
        if success is not None:
            assert event['potionSucceeded'] is success, report
            if success:
                assert len(report['potionFlights']) == 1 and report['potionFlights'][0]['hasOrigin'] and report['potionFlights'][0]['centreError'] < .2, report
            else: assert not report['potionFlights'], report
    print(branch, label, 'PASS', flush=True)
    return report

process = None; recording = False; results = {}; stop = threading.Event(); desktop=TestDesktop()
monitor = dict(samples=0, foreground_samples=0)
u = ctypes.windll.user32; u.GetForegroundWindow.restype = ctypes.c_void_p
u.GetWindowThreadProcessId.argtypes = [ctypes.c_void_p, ctypes.POINTER(ctypes.c_ulong)]
def monitor_loop():
    while not stop.is_set():
        pid = ctypes.c_ulong(); u.GetWindowThreadProcessId(u.GetForegroundWindow(), ctypes.byref(pid))
        monitor['samples'] += 1
        if process and pid.value == process.pid: monitor['foreground_samples'] += 1
        stop.wait(.05)
try:
    env = os.environ.copy(); env['APPDATA'] = str(dev / 'user/AppData/Roaming'); env['LOCALAPPDATA'] = str(dev / 'user/AppData/Local'); env['STS2_MCP_FFMPEG'] = shutil.which('ffmpeg')
    process = desktop.launch([str(game / 'SlayTheSpire2.exe'), '--rendering-driver', 'vulkan', '--force-steam=off', '--log-file', str(dev / 'godot.log')], cwd=game, env=env)
    thread = threading.Thread(target=monitor_loop, daemon=True); thread.start()
    for _ in range(160):
        time.sleep(.5)
        try:
            log = (dev / 'godot.log').read_text(encoding='utf-8', errors='replace')
            if '[Karen STS2 MCP]' in log and "Preloading 'Common' Complete:" in log and 'continue' in state().get('options', []): break
        except Exception: pass
    else: raise AssertionError('Game/MCP or saved Karen run unavailable')
    assert '[Banana Probe]' in log and '[Six Card Probe]' in log and '[Background Test Guard]' in log
    save('build', {p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in [mods / 'ShoujoKagekiAijoKaren/lib' / f'ShoujoKagekiAijoKaren.{branch.title()}.dll', mods / 'ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck']})
    post(action='menu_select', option='continue'); time.sleep(3)
    cmd('fight WATERFALL_GIANT_BOSS'); wait_play(); time.sleep(1)
    cmd('energy 99'); cmd('banana_probe speed normal')
    if args.ticket_only and any(r['id']=='KAREN_HAIRPIN_RELIC' for r in state()['player']['relics']):
        cmd('relic remove KAREN_HAIRPIN_RELIC');time.sleep(.4)
        assert all(r['id']!='KAREN_HAIRPIN_RELIC' for r in state()['player']['relics'])
    # Compile the shared shader and load textures before recording; then start a fresh combat.
    warmup='KAREN_TICKET' if args.ticket_only else 'KAREN_BANANA_CAKE'
    add(warmup); play(warmup); time.sleep(3.5)
    cmd('fight WATERFALL_GIANT_BOSS'); wait_play(); time.sleep(.8); cmd('energy 99')
    for card in list(state()['player']['hand']): cmd('remove_card ' + card['id'] + ' Hand'); time.sleep(.15)
    time.sleep(2.2); clear_potions(); snap('initial'); hand_check('initial-hand')
    api('/settings', dict(muted=False)); time.sleep(.3)
    api('/recording/start', dict(fps=30, max_duration_seconds=150, width=1280, height=720, audio=True)); recording=True
    timeline=[]; started=time.monotonic(); capture_active=True
    def chapter(label):
        if capture_active:
            timeline.append(dict(label=label,time=time.monotonic()-started)); save('chapters',timeline)
    def six(): return json.loads(cmd('six_probe status')['message'])
    def benefit(card,label,key,instant=False,cancel=False):
        add(card); cmd('six_probe watch'); before=six()[key]; snap(label+'-before'); chapter(label); play(card)
        for _ in range(180):
            r=six()
            if r['casts'] or (instant and r[key]>before): break
            time.sleep(.04)
        else: raise AssertionError((label,'cast not started'))
        time.sleep(.06 if cancel else .55); snap(label+'-during')
        if cancel: cmd('six_probe cancel')
        for _ in range(160):
            r=six()
            if r['live']==0 and r['backLive']==0 and r['hudLive']==0 and r[key]>before: break
            time.sleep(.06)
        else: raise AssertionError((label,'not resolved',r))
        snap(label+'-after'); hand_check(label+'-hand'); save(label+'-probe',r)
        assert r[key]==before+1,r
        if instant: assert r['casts']==[],r
        elif not cancel: assert len(r['casts'])==1 and r['casts'][0]['confirmed'],r
        results[label]=r; print(branch,label,'PASS',flush=True)
    def radiance(label,upgrade=False,existing=False,instant=False,cancel=False):
        if not existing:add('KAREN_WE_KOWN_TOKEN',upgrade)
        cmd('six_probe energy 0');time.sleep(.12);cmd('six_probe watch');before=six()['energy'];snap(label+'-before');chapter(label)
        play('KAREN_WE_KOWN_TOKEN')
        for _ in range(180):
            r=six()
            if r['casts'] or (instant and r['energy']>before):break
            time.sleep(.04)
        else:raise AssertionError((label,'radiance did not start'))
        time.sleep(.06 if cancel else .85);snap(label+'-during')
        if cancel:cmd('six_probe cancel')
        for _ in range(160):
            r=six()
            if r['energy']>before and r['live']==r['backLive']==r['hudLive']==0:break
            time.sleep(.06)
        else:raise AssertionError((label,'radiance unresolved',r))
        gain=4 if upgrade else 2
        assert r['energy']==before+gain,r
        if instant:assert r['casts']==[],r
        elif not cancel:
            assert len(r['casts'])==1 and r['casts'][0]['kind']=='Radiance',r
            event=r['casts'][0]
            assert event['confirmed'] and event['energyGain']==event['energyMotes']==gain and event['usesEnergyHud'],r
            assert event['usesPlayedCardSource'] and event['cardSourceSamples']>0 and event['cardSourceMaxError']<1,r
        snap(label+'-after');hand_check(label+'-hand');save(label+'-probe',r)
        results[label]=r;print(branch,label,'PASS',flush=True)
    if args.ticket_only:
        def clear_hand():
            for old in list(state()['player']['hand']):cmd('remove_card '+old['id']+' Hand')
            time.sleep(.35)
        def ticket_turn(label,amount,instant=False,cancel=False):
            clear_hand();cmd('heal 999');time.sleep(.5)
            cmd('six_probe watch');snap(label+'-before');chapter(label)
            post(action='end_turn')
            for _ in range(200):
                r=six();hand=state()['player']['hand']
                if r['casts'] or (instant and sum(c['id']=='KAREN_TOWER_OF_PROMISE' for c in hand)>=amount):break
                time.sleep(.04)
            else:raise AssertionError((label,'turn trigger did not start'))
            time.sleep(.05 if cancel else .16);snap(label+'-during')
            if cancel:cmd('six_probe cancel')
            for _ in range(200):
                r=six();current=state()
                towers=sum(c['id']=='KAREN_TOWER_OF_PROMISE' for c in current['player']['hand'])
                if towers==amount and current.get('battle',{}).get('is_play_phase') and r['live']==r['backLive']==r['hudLive']==0:break
                time.sleep(.08)
            else:raise AssertionError((label,'delivery or cleanup',r,current))
            time.sleep(.5);snap(label+'-after');hand_check(label+'-hand');save(label+'-probe',r)
            assert r['ticket']==amount,r
            if instant:assert r['casts']==[],r
            else:
                assert len(r['casts'])==1 and r['casts'][0]['kind']=='TicketTrigger',r
                if not cancel:assert r['casts'][0]['confirmed'] and r['casts'][0]['deliveries']==amount,r
            results[label]=r;print(branch,label,'PASS',flush=True)
        benefit('KAREN_TICKET','门票 · 珍珠白与淡蓝','ticket')
        ticket_turn('回合开始 · 生成1张约定之塔',1)
        cmd('energy 99');benefit('KAREN_TICKET','门票叠加至2层','ticket')
        ticket_turn('回合开始 · 生成2张约定之塔',2)
        api('/recording/stop',{})
        for _ in range(180):
            rec=api('/recording')
            if rec['status'] in ('completed','failed'):break
            time.sleep(.2)
        assert rec['status']=='completed' and rec.get('audio_source')=='game_process',rec
        shutil.copy2(rec['output_path'],out/'game.mp4');save('recording',rec);recording=False;capture_active=False
        api('/settings',dict(muted=True))
        for speed,cancel in [('fast',False),('instant',False),('normal',True)]:
            cmd('fight WATERFALL_GIANT_BOSS');wait_play();time.sleep(.8)
            clear_hand();cmd('banana_probe speed '+speed);cmd('energy 99')
            label='cancel' if cancel else speed
            benefit('KAREN_TICKET',label+'-ticket','ticket',instant=speed=='instant',cancel=cancel)
            ticket_turn(label+'-ticket-trigger',1,instant=speed=='instant',cancel=cancel)
    else:
        chapter('Banana蛋糕'); results['cake']=cast('KAREN_BANANA_CAKE','cake',success=True)
        chapter('Banana什锦'); results['snack']=cast('KAREN_SMALL_SNACK','snack',gain=8)
        chapter('Banana晚餐'); results['dinner']=cast('KAREN_BANANA','dinner',gain=8)
        benefit('KAREN_TICKET','门票','ticket')
        benefit('KAREN_WE_KOWN','两人的梦想','dream')
        benefit('KAREN_SHINE_TOGETHER_ALL','一起闪耀吧（单人注入，仅检查实际卡牌路径）','reward')
        # Real Fall selection puts an upgraded Assortment in PromisePile, then the normal turn hook transforms it.
        add('KAREN_SMALL_SNACK',upgrade=True);add('KAREN_FALL');snap('fall-before');play('KAREN_FALL')
        for _ in range(100):
            selection=state()
            if selection.get('state_type')=='hand_select' or 'KAREN_SMALL_SNACK' in six()['promise']:break
            time.sleep(.1)
        else:raise AssertionError('Fall selection missing')
        # The native selector auto-resolves when only one eligible card remains.
        if selection.get('state_type')=='hand_select':
            snack=next(c for c in selection['hand_select']['cards'] if c['id']=='KAREN_SMALL_SNACK')
            post(action='combat_select_card',card_index=snack['index'])
            if state().get('state_type')=='hand_select':post(action='combat_confirm_selection')
        wait_play();time.sleep(1.5);snap('fall-after');hand_check('fall-hand')
        assert 'KAREN_SMALL_SNACK' in six()['promise']
        cmd('six_probe watch'); snap('generation-before'); chapter('回合结束光辉诞生 / 下回合门票兑现')
        post(action='end_turn'); wait_play(); time.sleep(2)
        r=six(); snap('generation-after'); hand_check('generation-hand'); save('generation-probe',r)
        assert r['live']==0 and r['backLive']==0 and r['hudLive']==0,r
        assert any(c['kind']=='DreamTrigger' and c['confirmed'] and c['deliveries']==1 for c in r['casts']),r
        assert any(c['kind']=='TicketTrigger' and c['confirmed'] and c['deliveries']==1 for c in r['casts']),r
        assert any(c['kind']=='SnackTransform' and c['confirmed'] and c['deliveries']==1 for c in r['casts']),r
        assert 'KAREN_SMALL_SNACK' not in r['promise'] and any(c['id']=='KAREN_BANANA' and c['upgraded'] for c in r['promiseCards']),r
        results['promise-transformation-and-generation']=r
        assert 'KAREN_WE_KOWN_TOKEN' in r['promise'],r
        assert any(c['id']=='KAREN_TOWER_OF_PROMISE' for c in state()['player']['hand'])
        assert probe()['strength']==8
        cmd('energy 99'); chapter('约定之塔取回实际光辉'); snap('tower-before'); play('KAREN_TOWER_OF_PROMISE'); time.sleep(3)
        snap('tower-after'); hand_check('tower-hand')
        assert any(c['id']=='KAREN_WE_KOWN_TOKEN' for c in state()['player']['hand'])
        radiance('光辉 · 实际取回后获得2能量',existing=True)
        api('/recording/stop',{})
        for _ in range(180):
            rec=api('/recording')
            if rec['status'] in ('completed','failed'):break
            time.sleep(.2)
        assert rec['status']=='completed' and rec.get('audio_source')=='game_process',rec
        shutil.copy2(rec['output_path'],out/'game.mp4');save('recording',rec);recording=False;capture_active=False
        api('/settings',dict(muted=True))
        # The story sequence deliberately returns an upgraded Dinner. Clear those fixtures so
        # later per-card cases cannot accidentally play an earlier card with the same ID.
        for old in list(state()['player']['hand']):cmd('remove_card '+old['id']+' Hand')
        time.sleep(.3)
        radiance('radiance-upgraded',upgrade=True)
        cmd('banana_probe speed fast');radiance('radiance-fast')
        cmd('banana_probe speed instant');radiance('radiance-instant',instant=True)
        cmd('banana_probe speed normal');radiance('radiance-cancel',cancel=True)
        cmd('banana_probe speed fast'); cmd('energy 99')
        for card,label,key in [('KAREN_TICKET','fast-ticket','ticket'),('KAREN_WE_KOWN','fast-dream','dream'),('KAREN_SHINE_TOGETHER_ALL','fast-shine','reward')]:benefit(card,label,key)
        cmd('banana_probe speed instant'); cmd('energy 99')
        for card,label,key in [('KAREN_TICKET','instant-ticket','ticket'),('KAREN_WE_KOWN','instant-dream','dream'),('KAREN_SHINE_TOGETHER_ALL','instant-shine','reward')]:benefit(card,label,key,instant=True)
        cmd('banana_probe speed normal'); cmd('energy 99')
        benefit('KAREN_TICKET','cancel-ticket','ticket',cancel=True)
        if not args.focused:
            results['snack-upgraded']=cast('KAREN_SMALL_SNACK','snack-upgraded',gain=10,upgrade=True)
            results['dinner-upgraded']=cast('KAREN_BANANA','dinner-upgraded',gain=10,upgrade=True)
            while len(state()['player']['potions'])<state()['player']['max_potion_slots']:
                cmd('potion FLEX_POTION');time.sleep(.3)
            results['cake-full']=cast('KAREN_BANANA_CAKE','cake-full',success=False,upgrade=True)
            clear_potions();cmd('banana_probe speed fast')
            results['fast-cake']=cast('KAREN_BANANA_CAKE','fast-cake',success=True)
            results['fast-snack']=cast('KAREN_SMALL_SNACK','fast-snack',gain=8)
            results['fast-dinner']=cast('KAREN_BANANA','fast-dinner',gain=8)
            cmd('banana_probe speed instant')
            for card,label in [('KAREN_BANANA_CAKE','instant-cake'),('KAREN_SMALL_SNACK','instant-snack'),('KAREN_BANANA','instant-dinner')]:
                results[label]=cast(card,label,instant=True,gain=None if card=='KAREN_BANANA_CAKE' else 8,
                    success=True if card=='KAREN_BANANA_CAKE' else None)
            cmd('banana_probe speed normal');cmd('energy 99')
            for scale in ['small','large','flip','normal']:
                cmd('banana_probe scale '+scale)
                results['scale-'+scale]=cast('KAREN_BANANA','scale-'+scale,gain=8)
            results['interrupted-dinner']=cast('KAREN_BANANA','interrupted-dinner',gain=8,cancel=True)
    save('results',results)

finally:
    if recording:
        try: api('/recording/stop', {})
        except Exception: pass
    if process and process.poll() is None: process.terminate(); process.wait(timeout=15)
    desktop.close()
    stop.set()
    if 'thread' in globals(): thread.join(timeout=2)
    save('window-monitor', monitor)
    if (dev / 'godot.log').exists(): shutil.copy2(dev / 'godot.log', out / 'godot.log')
assert monitor['foreground_samples'] == 0, monitor
diagnostics=audit((out/'godot.log').read_text(encoding='utf-8',errors='replace'))
save('log-audit',diagnostics)
assert not diagnostics['unexpectedErrors'],dict(count=len(diagnostics['unexpectedErrors']),first=diagnostics['unexpectedErrors'][:8])
print(branch, 'COMPLETE', flush=True)
