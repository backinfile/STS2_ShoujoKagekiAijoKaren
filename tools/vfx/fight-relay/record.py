"""Targeted two-process relay VFX recording through KarenSTS2MCP. No full matrix."""
import argparse, hashlib, json, os, pathlib, shutil, socket, subprocess, time, urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
parser.add_argument('--source-dev', type=pathlib.Path, required=True)
parser.add_argument('--output', type=pathlib.Path, required=True)
args = parser.parse_args()
project = pathlib.Path(__file__).resolve().parents[3]
source = args.source_dev.resolve() / args.branch
out = args.output.resolve() / args.branch
out.mkdir(parents=True, exist_ok=True)
ports = [18957, 18958] if args.branch == 'stable' else [18959, 18960]
processes, markers, cases = [], [], []
recording = False

def save(name, data):
    (out / (name + '.json')).write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')

def api(side, path, data=None):
    request = urllib.request.Request(f'http://127.0.0.1:{ports[side]}/api/v1' + path,
        data=None if data is None else json.dumps(data).encode(), headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=30) as r:
        return json.load(r)

def state(side, endpoint='multiplayer'):
    return api(side, '/' + endpoint + '?format=json')

def post(side, endpoint='multiplayer', **action):
    result = api(side, '/' + endpoint, action)
    with (out / 'actions.jsonl').open('a', encoding='utf-8') as f:
        f.write(json.dumps(dict(side=side, action=action, result=result), ensure_ascii=False) + '\n')
    assert result.get('status') == 'ok', (side, action, result)
    return result

def cmd(side, command):
    return post(side, action='run_command', command=command)

def wait(side, predicate, endpoint='multiplayer', seconds=60):
    deadline = time.monotonic() + seconds
    last = None
    while time.monotonic() < deadline:
        assert all(p.poll() is None for p in processes), 'Game exited'
        try:
            last = state(side, endpoint)
            if last.get('prompt_id'):
                options = [o.get('name') for o in last.get('options',[]) if isinstance(o,dict) and o.get('enabled',True)]
                option = next((o for o in ['no','advance','proceed'] if o in options), options[0] if options else 'advance')
                api(side, '/ui/prompt', dict(option=option, prompt_id=last['prompt_id']))
            elif predicate(last):
                return last
        except (OSError, ValueError):
            pass
        time.sleep(.2)
    save('timeout-' + str(side), last)
    raise TimeoutError((side, endpoint, last))

def snapshot(name, side):
    save(name + '-' + str(side), state(side))
    with urllib.request.urlopen(f'http://127.0.0.1:{ports[side]}/api/v1/screenshot', timeout=30) as r:
        (out / (name + '-' + str(side) + '.png')).write_bytes(r.read())

def status(side):
    return json.loads(cmd(side, 'relay_status')['message'])

def hand(name, side):
    result = cmd(side, 'karen_check_hand')
    save(name + '-hand-' + str(side), result)
    h = json.loads(result['message'])
    assert h['modelCount'] == h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'], h

def link_or_copy(src, dst):
    if pathlib.Path(src).suffix.lower() in ['.cfg', '.conf', '.json']:
        return shutil.copy2(src, dst)
    try:
        os.link(src, dst)
    except OSError:
        shutil.copy2(src, dst)
    return dst

for port in ports:
    with socket.socket() as check:
        assert check.connect_ex(('127.0.0.1', port)) != 0, f'Occupied MCP port {port}'
# FastMP uses this UDP port; never take over another test session.
udp = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
udp.bind(('127.0.0.1', 33771)); udp.close()

subprocess.run(['dotnet', 'build', str(project/'tools/vfx/fight-relay/Probe.csproj'),
    '-p:Variant=' + args.branch.title(), '-p:Game=' + str(source/'game'),
    '-o', str(project/'artifacts/relay-probe'/args.branch), '--nologo', '-v:q', '-clp:ErrorsOnly'], check=True)
package = project / 'artifacts/ShoujoKagekiAijoKaren'
build = {name: hashlib.sha256((package/name).read_bytes()).hexdigest() for name in
    [f'lib/ShoujoKagekiAijoKaren.{args.branch.title()}.dll', 'ShoujoKagekiAijoKaren.pck']}
save('capture-build', build)

try:
    for side, role in enumerate(['host', 'client']):
        dev = out / role
        game = dev / 'game'
        if not game.exists():
            shutil.copytree(source/'game', game, copy_function=link_or_copy,
                ignore=lambda folder, names: ['mods'] if pathlib.Path(folder) == source/'game' else [])
        mods = game/'mods'; mods.mkdir(exist_ok=True)
        shutil.copytree(source/'game/mods/BaseLib', mods/'BaseLib', dirs_exist_ok=True)
        # Use the project's previously built loopback-only MCP with networked console commands.
        mcp = args.source_dev.resolve().parent / ('mcp-' + args.branch)
        assert json.loads((mcp/'source-info.json').read_text(encoding='utf-8-sig'))['loopback_only']
        for suffix in ['dll', 'json']:
            shutil.copy2(mcp/('KarenSTS2MCP.'+suffix), mods)
        shutil.copytree(package, mods/'ShoujoKagekiAijoKaren', dirs_exist_ok=True)
        shutil.copy2(project/'artifacts/relay-probe'/args.branch/'KarenRelayProbe.dll', mods)
        (mods/'KarenRelayProbe.json').write_text(json.dumps(dict(id='KarenRelayProbe', name='KarenRelayProbe',
            version='1.0.0', author='Development', description='Temporary isolated relay recording',
            has_dll=True, has_pck=False, affects_gameplay=True)), encoding='utf-8')
        (mods/'KarenSTS2MCP.conf').write_text(json.dumps(dict(port=ports[side], fullscreen=False, muted=True,
            skip_tutorial=True, skip_first_prompt=True, window_width=1280, window_height=720)), encoding='utf-8')
        client_id = 7910 + side
        profile = dev/f'user/AppData/Roaming/SlayTheSpire2/default/{client_id}'
        profile.mkdir(parents=True, exist_ok=True)
        # Copy known tutorial/unlock preferences; the game creates an independent fresh run.
        original = source/'user/AppData/Roaming/SlayTheSpire2/default/1'
        for file in original.rglob('*.save'):
            if file.name in ['settings.save', 'prefs.save', 'progress.save']:
                target = profile / file.relative_to(original)
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(file, target)
        for file in profile.rglob('*.save'):
            if file.name == 'settings.save':
                data = json.loads(file.read_text(encoding='utf-8-sig'))
                data.update(fullscreen=False, volume_master=.5, mute_in_background=False,
                    limit_fps_in_background=False, seen_ea_disclaimer=True, skip_intro_logo=True,
                    window_size={'X':1280,'Y':720})
                file.write_text(json.dumps(data), encoding='utf-8')
            elif file.name == 'prefs.save':
                data = json.loads(file.read_text(encoding='utf-8-sig'))
                data.update(fast_mode='normal', mute_in_background=False)
                file.write_text(json.dumps(data), encoding='utf-8')
        (game/'override.cfg').write_text('[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\n'
            'window/size/window_width_override=1280\nwindow/size/window_height_override=720\n', encoding='utf-8')
        env = os.environ.copy()
        env.update(APPDATA=str(dev/'user/AppData/Roaming'), LOCALAPPDATA=str(dev/'user/AppData/Local'),
                   STS2_MCP_FFMPEG=shutil.which('ffmpeg'))
        startup = subprocess.STARTUPINFO(); startup.dwFlags = subprocess.STARTF_USESHOWWINDOW; startup.wShowWindow = 0
        p = subprocess.Popen([str(game/'SlayTheSpire2.exe'), '--rendering-driver', 'vulkan', '--force-steam=off',
            '--fastmp=' + ('host_standard' if side == 0 else 'join'), '--clientId='+str(client_id),
            '--log-file', str(dev/'godot.log')], cwd=game, env=env, startupinfo=startup)
        processes.append(p)
        # Observe intended MCP startup before issuing its first request.
        for _ in range(180):
            if (dev/'godot.log').exists() and '[Karen STS2 MCP]' in (dev/'godot.log').read_text(encoding='utf-8',errors='replace'):
                break
            assert p.poll() is None, 'Game exited before MCP load'
            time.sleep(.25)
        else:
            raise TimeoutError('MCP did not load')
        wait(side, lambda s: s.get('menu_screen') == 'character_select', 'singleplayer')
        api(side, '/settings', dict(fullscreen=False, muted=True, skip_tutorial=True,
            skip_first_prompt=True, window_width=1280, window_height=720))
    for side in range(2):
        wait(side, lambda s: s.get('lobby',{}).get('player_count') == 2, 'singleplayer')
        post(side, 'singleplayer', action='menu_select', option='KAREN')
    for side in range(2):
        wait(side, lambda s: any(isinstance(o,dict) and o.get('name')=='embark' and o.get('enabled') for o in s.get('options',[])), 'singleplayer')
        post(side, 'singleplayer', action='menu_select', option='embark')
    for side in range(2):
        wait(side, lambda s: s.get('player') and s.get('state_type') in ['event','map','monster'])
    # The native networked fight command avoids random ancient reward/selection setup.
    cmd(0, 'fight WATERFALL_GIANT_BOSS')
    for side in range(2):
        wait(side, lambda s: s.get('battle',{}).get('is_play_phase'))
    time.sleep(2)
    for side in range(2):
        for card in state(side)['player']['hand']:
            cmd(side, 'remove_card '+card['id']+' Hand')
            time.sleep(.15)
    time.sleep(1.2)
    for side in range(2):
        hand('initial',side)
    cmd(0,'relay_fixture speed normal')
    cmd(0,'relay_fixture seed 3 0')
    wait(0,lambda s:any(c['id']=='KAREN_FIGHT_RELAY' for c in s['player']['hand']))
    api(0,'/settings',dict(muted=False))
    api(0,'/recording/start',dict(fps=30,max_duration_seconds=90,width=1280,height=720,audio=True))
    recording=True; clock=time.monotonic(); time.sleep(.6)

    def cast(name, side, mode='normal', receive=False, seed=None):
        cmd(0, 'relay_fixture speed '+mode)
        if receive: cmd(side, 'relay_fixture receive')
        if seed: cmd(side, 'relay_fixture seed '+seed)
        current=wait(side, lambda s:any(c['id']=='KAREN_FIGHT_RELAY' for c in s['player']['hand']))
        time.sleep(.65)
        before=status(0); save(name+'-before-status',before)
        slot = current['local_player_slot']
        original = before['players'][slot]['deck'][-1]
        for view in range(2): snapshot(name+'-before',view); hand(name+'-before',view)
        markers.append(dict(time=time.monotonic()-clock,title=name))
        card=next(c for c in current['player']['hand'] if c['id']=='KAREN_FIGHT_RELAY')
        post(side,action='play_card',card_index=card['index'])
        if mode != 'instant':
            for _ in range(50):
                during=status(0)
                if during['active']: break
                time.sleep(.05)
            assert during['active'], (name, 'cue did not start')
            time.sleep(.6 if mode=='normal' else .3)
        else: time.sleep(.3)
        during=status(0); save(name+'-during-status',during)
        snapshot(name+'-during',0)
        time.sleep(2.5)
        after=status(0); remote=status(1)
        save(name+'-after-status',dict(host=after,client=remote))
        assert after==remote and after['active']==after['back']==0, (after,remote)
        for idx in range(2):
            delta = len(after['players'][idx]['deck']) - len(before['players'][idx]['deck'])
            assert delta == (-1 if idx == slot else (1 if original['shine'] > 1 else 0)), (name, idx, delta)
            pending = after['players'][idx]['pending'] - before['players'][idx]['pending']
            assert pending == ((2 if original['upgrade'] else 1) if idx == slot and original['shine'] == 1 else 0), (name, idx, pending)
        if original['shine'] > 1:
            assert after['players'][1-slot]['deck'][-1] == dict(shine=original['shine']-1, upgrade=original['upgrade'])
        if mode=='instant': assert during['active']==during['back']==0,during
        else: assert during['active']==1 and during['back']==1,during
        for view in range(2): snapshot(name+'-after',view);hand(name+'-after',view)
        cases.append(dict(name=name,mode=mode,side=side,before=before,after=after))
        print('PASS',args.branch,name,flush=True)
    cast('01-host-to-client',0)
    cast('02-client-to-host',1,receive=True)
    cast('03-shine-exhausted',0,receive=True)
    cast('04-upgraded-finale',1,seed='1 1')
    cast('05-fast-relay',0,mode='fast',seed='3 0')
    cast('06-fast-finale',1,mode='fast',seed='1 0')
    cast('07-instant-relay',0,mode='instant',seed='3 0')
    cast('08-instant-finale',1,mode='instant',seed='1 0')
    api(0,'/recording/stop',{})
    for _ in range(160):
        rec=api(0,'/recording')
        if rec['status'] in ['completed','failed']: break
        time.sleep(.2)
    assert rec['status']=='completed' and rec.get('audio_source')=='game_process',rec
    recording=False;save('recording',rec)
    shutil.copy2(rec['output_path'],out/'game.mp4')
    save('markers',markers);save('cases',cases)
    cmd(0,'relay_fixture speed normal')
    logs = {}
    for role in ['host','client']:
        lines = (out/role/'godot.log').read_text(encoding='utf-8',errors='replace').splitlines()
        begin = next(i for i,line in enumerate(lines) if 'relay_fixture seed 3 0' in line)
        errors = lambda seq: [line for line in seq if line.startswith('ERROR:') or '[ERROR]' in line]
        logs[role] = dict(setupErrors=errors(lines[:begin]),presentationErrors=errors(lines[begin:]))
        assert not logs[role]['presentationErrors'], logs[role]
    save('log-review',logs)
    save('result',dict(result='pass',scope='Targeted same-version Karen/Karen relay and pending relic VFX; not full matrix',cases=len(cases),build=build,
        setupErrorCounts={role:len(logs[role]['setupErrors']) for role in logs}))
finally:
    if recording:
        try: api(0,'/recording/stop',{});time.sleep(2)
        except OSError: pass
    for p in reversed(processes):
        if p.poll() is None: p.terminate();p.wait(timeout=15)
