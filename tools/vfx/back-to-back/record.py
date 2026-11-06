"""Native-play VFX checks/recording in an isolated game. Operates only through KarenSTS2MCP."""
import argparse
import hashlib
import json
import os
import pathlib
import shutil
import socket
import subprocess
import time
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
parser.add_argument('--output', type=pathlib.Path, required=True)
parser.add_argument('--cases', default='normal4,zero,one,ten,upgraded,high24,fast,instant,replay,pose,cancel,lethal')
parser.add_argument('--port', type=int, required=True)
parser.add_argument('--client-id', type=int, default=91234)
args = parser.parse_args()
project = pathlib.Path(__file__).resolve().parents[3]
root = args.output.resolve()
dev = root / 'isolated' / args.branch
game = dev / 'game'
out = root / args.branch
out.mkdir(parents=True, exist_ok=True)
base = f'http://127.0.0.1:{args.port}/api/v1'
process = None
recording = False
results_file = out / 'results.json'
requested = args.cases.split(',')
results = [r for r in json.loads(results_file.read_text(encoding='utf-8')) if r['case'] not in requested] if results_file.exists() else []

def save(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')

def api(path, data=None):
    request = urllib.request.Request(base + path, data=None if data is None else json.dumps(data).encode(),
        headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)

def state():
    return api('/singleplayer?format=json')

def post(**action):
    result = api('/singleplayer', action)
    with (out / 'actions.jsonl').open('a', encoding='utf-8') as f:
        f.write(json.dumps(dict(action=action, result=result), ensure_ascii=False) + '\n')
    assert result.get('status') == 'ok', (action, result)
    return result

def cmd(command):
    return post(action='run_command', command=command)

def probe():
    return json.loads(cmd('b2b_probe status')['message'])

def wait(predicate, seconds=45):
    deadline = time.monotonic() + seconds
    last = None
    while time.monotonic() < deadline:
        assert process.poll() is None, 'Owned game exited'
        try:
            last = state()
            if last.get('prompt_id'):
                options = [o.get('name') if isinstance(o, dict) else o for o in last.get('options', [])]
                choice = next((o for o in ['no', 'advance', 'proceed'] if o in options), options[0])
                api('/ui/prompt', dict(option=choice, prompt_id=last['prompt_id']))
            elif predicate(last):
                return last
        except (OSError, ValueError):
            pass
        time.sleep(.15)
    save(out / 'timeout.json', last)
    raise TimeoutError(last)

def snap(case, name):
    save(case / (name + '.json'), state())
    with urllib.request.urlopen(base + '/screenshot', timeout=30) as response:
        (case / (name + '.png')).write_bytes(response.read())

def hand(case, name):
    result = cmd('karen_check_hand')
    save(case / (name + '-hand.json'), result)
    data = json.loads(result['message'])
    assert data['modelCount'] == data['visualCount'] and not data['missing'] and not data['unexpected'] and not data['unexpectedPileIcons'], data

def hp(s):
    return sum(e['hp'] for e in s['battle']['enemies'])

def stop_recording(case):
    global recording
    api('/recording/stop', {})
    for _ in range(150):
        data = api('/recording')
        if data['status'] in ('completed', 'failed'):
            break
        time.sleep(.2)
    assert data['status'] == 'completed' and data.get('audio') and data.get('audio_source') == 'game_process', data
    shutil.copy2(data['output_path'], case / 'game.mp4')
    save(case / 'recording.json', data)
    recording = False

with socket.socket() as port:
    assert port.connect_ex(('127.0.0.1', args.port)) != 0, 'Port occupied; no deployment performed'
package = project / 'artifacts/ShoujoKagekiAijoKaren'
subprocess.run(['dotnet', 'build', str(pathlib.Path(__file__).with_name('BackToBackProbe.csproj')),
    '-p:Game=' + str(game), '-p:Variant=' + args.branch.title(), '-o', str(root / 'probe' / args.branch),
    '--nologo', '-v:q', '-clp:ErrorsOnly'], check=True)
mods = game / 'mods'
shutil.copytree(package, mods / 'ShoujoKagekiAijoKaren', dirs_exist_ok=True)
shutil.copy2(root / 'probe' / args.branch / 'KarenBackToBackProbe.dll', mods)
save(mods / 'KarenBackToBackProbe.json', dict(id='KarenBackToBackProbe', name='KarenBackToBackProbe',
    version='1.0.0', author='Development', has_dll=True, has_pck=False, affects_gameplay=False,
    min_game_version='v0.107.1', dependencies=[dict(id='ShoujoKagekiAijoKaren', min_version='v0.1.3')]))
save(out / 'build.json', {name: hashlib.sha256((package / name).read_bytes()).hexdigest() for name in
    [f'lib/ShoujoKagekiAijoKaren.{args.branch.title()}.dll', 'ShoujoKagekiAijoKaren.pck']})
save(mods / 'KarenSTS2MCP.conf', dict(port=args.port, fullscreen=False, muted=True, skip_tutorial=True,
    skip_first_prompt=True, window_width=1280, window_height=720))
for path in (dev / 'user').rglob('settings.save'):
    data = json.loads(path.read_text(encoding='utf-8-sig'))
    data.update(fullscreen=False, volume_master=.5, volume_bgm=.15, volume_sfx=.85, mute_in_background=False,
        limit_fps_in_background=False, seen_ea_disclaimer=True, skip_intro_logo=True, window_size={'X':1280, 'Y':720})
    save(path, data)
# This is a newly prepared environment; never overwrite a hardlinked source configuration.
override = game / 'override.cfg'
assert not override.exists(), 'Existing override configuration requires explicit preservation'
override.write_text('[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\n'
    'window/size/window_width_override=1280\nwindow/size/window_height_override=720\n', encoding='utf-8')
try:
    env = os.environ.copy()
    env.update(APPDATA=str(dev / 'user/AppData/Roaming'), LOCALAPPDATA=str(dev / 'user/AppData/Local'), STS2_MCP_FFMPEG=shutil.which('ffmpeg'))
    startup = subprocess.STARTUPINFO()
    startup.dwFlags = subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = 0
    process = subprocess.Popen([str(game / 'SlayTheSpire2.exe'), '--rendering-driver', 'vulkan', '--force-steam=off', '--clientId=' + str(args.client_id),
        '--log-file', str(dev / 'godot.log')], cwd=game, env=env, startupinfo=startup)
    for _ in range(200):
        assert process.poll() is None, 'Game exited before MCP load'
        if (dev / 'godot.log').exists() and '[Karen STS2 MCP]' in (dev / 'godot.log').read_text(encoding='utf-8', errors='replace'):
            break
        time.sleep(.25)
    else:
        raise TimeoutError('Karen MCP did not load')
    current = wait(lambda s: 'continue' in s.get('options', []) or s.get('player'))
    api('/settings', dict(fullscreen=False, muted=True, skip_tutorial=True, skip_first_prompt=True, window_width=1280, window_height=720))
    if not current.get('player'):
        # Starting Continue while Common preloading is still allocating GPU buffers can crash Vulkan.
        for _ in range(180):
            if "Preloading 'Common' Complete:" in (dev / 'godot.log').read_text(encoding='utf-8', errors='replace'):
                break
            assert process.poll() is None, 'Game exited during Common preloading'
            time.sleep(.25)
        else:
            raise TimeoutError('Common preloading did not finish')
        wait(lambda s: 'continue' in s.get('options', []))
        post(action='menu_select', option='continue')
        wait(lambda s: s.get('player'))
    time.sleep(2)
    for name in requested:
        case = out / name
        case.mkdir(exist_ok=True)
        stock = {'zero':0, 'one':1, 'ten':10, 'high24':24, 'upgraded':10}.get(name, 4)
        speed = name if name in ('fast', 'instant') else 'normal'
        cmd('b2b_probe speed normal')
        cmd('fight WATERFALL_GIANT_BOSS')
        wait(lambda s: s.get('battle', {}).get('is_play_phase') and any(e.get('entity_id', '').startswith('WATERFALL') for e in s['battle']['enemies']))
        time.sleep(1.2)
        for card in state()['player']['hand']:
            cmd('remove_card ' + card['id'] + ' Hand')
            time.sleep(.05)
        snap(case, 'before-fixture')
        cmd('b2b_probe seed ' + str(stock))
        deadline = time.monotonic() + 15
        while probe()['promise'] != stock:
            assert time.monotonic() < deadline, 'Promise fixture did not settle'
            time.sleep(.15)
        cmd('card KAREN_BACK_TO_BACK_CARD Hand')
        cmd('energy 3')
        time.sleep(1.3)
        if name == 'upgraded': cmd('b2b_probe upgrade')
        if name == 'replay': cmd('b2b_probe replay')
        if name == 'pose': cmd('b2b_probe pose')
        if name == 'lethal': cmd('b2b_probe lethal')
        cmd('b2b_probe speed ' + speed)
        cmd('b2b_probe watch')
        hand(case, 'before')
        snap(case, 'before')
        before = state()
        card = next(c for c in before['player']['hand'] if c['id'] == 'KAREN_BACK_TO_BACK_CARD')
        enemy = before['battle']['enemies'][0]['entity_id']
        post(action='hover_card', card_index=card['index'], target=enemy)
        api('/settings', dict(muted=False))
        api('/recording/start', dict(fps=30, max_duration_seconds=15, width=1280, height=720, audio=True))
        recording = True
        time.sleep(.8)
        post(action='unhover_card')
        start = time.monotonic()
        post(action='play_card', card_index=card['index'], target=enemy)
        if name == 'cancel':
            time.sleep(.13)
            cmd('b2b_probe clearfx')
        time.sleep(.43)
        snap(case, 'contact')
        count = 3 if name == 'replay' else 1
        expected = min(hp(before), stock * (6 if name == 'upgraded' else 4) * count)
        # Replays have native gaps between plays. A temporary empty VFX container is not completion.
        deadline = time.monotonic() + 12
        while True:
            after = state()
            status = probe()
            actual = hp(before) - (hp(after) if after.get('battle') else 0)
            complete = speed == 'instant' or sum(c['impact'] for c in status['casts']) == count
            if actual == expected and complete and status['live'] == 0:
                break
            assert time.monotonic() < deadline, (name, 'native play did not settle', actual, expected, status)
            time.sleep(.1)
        time.sleep(.45)
        save(case / 'probe.json', status)
        snap(case, 'after')
        if name != 'lethal':
            hand(case, 'after')
        else:
            wait(lambda s: s.get('state_type') == 'rewards')
            save(case / 'after-hand.json', dict(skipped='Combat ended; the native hand UI was destroyed.'))
        # A lethal battle may have removed its enemy list before the rewards transition.
        actual = hp(before) - (hp(after) if after.get('battle') else 0)
        assert actual == expected, (name, actual, expected, after)
        assert (name == 'lethal' or status['promise'] == stock) and status['live'] == 0, (name, status)
        if speed == 'instant':
            assert not status['casts'], status
        else:
            assert sum(c['impact'] for c in status['casts']) == count, (name, status)
            assert sum(not c['impact'] for c in status['casts']) == count, (name, status)
            assert all(c['exited'] and c['maxAnchorError'] < 2 for c in status['casts']), status
        results.append(dict(case=name, stock=stock, speed=speed, damage=actual, expected=expected,
            casts=len(status['casts']), live=status['live'], elapsed=time.monotonic()-start, passed=True))
        stop_recording(case)
        print(args.branch, name, 'PASS', actual, 'damage;', len(status['casts']), 'VFX nodes', flush=True)
    save(out / 'results.json', results)
finally:
    save(out / 'results.json', results)
    if recording:
        try:
            api('/recording/stop', {})
            for _ in range(100):
                if api('/recording')['status'] in ('completed', 'failed'): break
                time.sleep(.2)
        except Exception:
            pass
    if process and process.poll() is None:
        process.terminate()
        process.wait(timeout=15)
    if (dev / 'godot.log').exists(): shutil.copy2(dev / 'godot.log', out / 'godot.log')
    override.unlink(missing_ok=True)
