"""Real card-play evidence using KarenSTS2MCP in a private game/desktop copy."""
import argparse, hashlib, json, os
from pathlib import Path
import shutil, socket, subprocess, sys, time, urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--cases', default='normal,empty-in,empty-out,empty,full,fast,instant,void,repeat,cancel')
parser.add_argument('--source-dev', type=Path, default=Path('D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/artifacts/dev-game'))
args = parser.parse_args()
repo = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(repo / 'tools/vfx/six-card'))
from isolated_desktop import TestDesktop
from audit_log import audit
root = args.output.resolve()
assert not root.exists(), 'Use a fresh evidence directory'
root.mkdir(parents=True)
branch = args.branch
port = 19487 if branch == 'stable' else 19488
with socket.socket() as check:
    assert check.connect_ex(('127.0.0.1', port)) != 0, 'Port occupied'
subprocess.run([sys.executable, str(repo / 'tools/vfx/catalog/prepare.py'), branch, '--output', str(root),
    '--skip-build', '--source-dev', str(args.source_dev), '--port', str(port)], check=True)
dev = root / 'isolated' / branch
game = dev / 'game'
mods = game / 'mods'
shutil.copytree(repo / 'artifacts/ShoujoKagekiAijoKaren', mods / 'ShoujoKagekiAijoKaren')
subprocess.run(['dotnet', 'build', str(repo / 'tools/vfx/exchange-fate/Probe.csproj'), f'-p:Game={game}',
    f'-p:Variant={branch.title()}', '-o', str(root / 'probe'), '--nologo', '-v:q'], check=True)
shutil.copy2(root / 'probe/KarenExchangeFateProbe.dll', mods)
(mods / 'KarenExchangeFateProbe.json').write_text(json.dumps(dict(id='KarenExchangeFateProbe', name='KarenExchangeFateProbe',
    version='1.0.0', author='Development', description='Temporary fixture', has_dll=True, has_pck=False, affects_gameplay=False)), encoding='utf-8')
# prepare.py hardlinks the game files. Replace a pre-existing configuration link
# before writing, so even a source snapshot with override.cfg stays unchanged.
override = game / 'override.cfg'
if override.exists(): override.unlink()
override.write_text('[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n', encoding='utf-8')
base = f'http://127.0.0.1:{port}/api/v1'
out = root / branch; out.mkdir()
opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
desktop = TestDesktop()
recording = False
results = {}

def api(path, data=None):
    req = urllib.request.Request(base + path, None if data is None else json.dumps(data).encode(), headers={'Content-Type': 'application/json'})
    with opener.open(req, timeout=25) as response: return json.load(response)

def save(name, data):
    (out / (name + '.json')).write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')

def post(**data):
    response = api('/singleplayer', data)
    with (out / 'actions.jsonl').open('a', encoding='utf-8') as stream: stream.write(json.dumps([data, response], ensure_ascii=False) + '\n')
    assert response.get('status') == 'ok', (data, response)
    return response

def cmd(command): return post(action='run_command', command=command)
def state(): return api('/singleplayer?format=json')
def status(): return json.loads(cmd('fate_probe status')['message'])

def until(predicate, seconds=25):
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        value = predicate()
        if value: return value
        time.sleep(.1)
    raise AssertionError(('State timeout', state(), status()))

def snap(name):
    save(name, state())
    with opener.open(base + '/screenshot', timeout=25) as response: (out / (name + '.png')).write_bytes(response.read())

def hand(name):
    data = json.loads(cmd('karen_check_hand')['message'])
    save(name + '-hand', data)
    assert data['modelCount'] == data['visualCount'] and not data['missing'] and not data['unexpected'] and not data['unexpectedPileIcons'], data

def stop_record(name):
    global recording
    api('/recording/stop', {})
    rec = until(lambda: r if (r := api('/recording'))['status'] in ('completed', 'failed') else None, 40)
    save(name + '-recording', rec)
    assert rec['status'] == 'completed' and rec.get('audio') and rec.get('audio_source') == 'game_process', rec
    shutil.copy2(rec['output_path'], out / (name + '.mp4'))
    recording = False

try:
    env = os.environ.copy()
    env.update(APPDATA=str(dev / 'user/AppData/Roaming'), LOCALAPPDATA=str(dev / 'user/AppData/Local'), STS2_MCP_FFMPEG=shutil.which('ffmpeg'))
    process = desktop.launch([str(game / 'SlayTheSpire2.exe'), '--rendering-driver', 'vulkan', '--force-steam=off', '--log-file', str(dev / 'godot.log')], game, env)
    deadline = time.monotonic() + 65
    while time.monotonic() < deadline:
        time.sleep(.3)
        try:
            log = (dev / 'godot.log').read_text(encoding='utf-8', errors='replace')
            if '[Karen STS2 MCP]' in log and '[Exchange Fate Probe] Loaded' in log and "Preloading 'Common' Complete:" in log and 'continue' in state().get('options', []): break
        except (OSError, ValueError): pass
    else: raise AssertionError('MCP/probe/saved run not ready')
    post(action='menu_select', option='continue')
    until(lambda: state().get('player')); time.sleep(3)
    settings = api('/settings'); save('original-settings', settings)
    api('/settings', dict(fullscreen=False, window_width=1280, window_height=720, muted=False))
    for name in args.cases.split(','):
        # Finish the previous native combat before jumping to the next fixture.
        # Replacing a live room can return beta's pooled NCards twice and make
        # the next fixture invalid before its card has even been played.
        if state().get('battle', {}).get('is_play_phase'):
            cmd('kill all')
            until(lambda: state().get('state_type') == 'rewards')
            time.sleep(.8)
        cmd('fight WATERFALL_GIANT_BOSS')
        until(lambda: state().get('battle', {}).get('is_play_phase')); time.sleep(.8)
        if any(r['id'] == 'KAREN_HAIRPIN_RELIC' for r in state()['player']['relics']): cmd('relic remove KAREN_HAIRPIN_RELIC')
        speed = name if name in ('fast', 'instant') else 'normal'
        cmd('fate_probe speed ' + speed)
        nout, nin = {'empty-in': (4, 0), 'empty-out': (0, 3), 'empty': (0, 0), 'full': (9, 12)}.get(name, (4, 3))
        mode = name if name in ('void', 'repeat') else 'normal'
        cmd(f'fate_probe setup {nout} {nin} {mode}')
        until(lambda: status()['ready']); time.sleep(.9)
        cmd('energy 99'); snap(name + '-before'); hand(name + '-before')
        before = status(); save(name + '-before-probe', before)
        if name in ('normal', 'void', 'repeat'):
            save(name + '-record-start', api('/recording/start', dict(fps=30, max_duration_seconds=30, width=1280, height=720, audio=True)))
            recording = True
        time.sleep(.6)
        index = next(c['index'] for c in state()['player']['hand'] if c['id'] == 'KAREN_EXCHANGE_FATE')
        post(action='hover_card', card_index=index); time.sleep(.55)
        post(action='play_card', card_index=index)
        time.sleep(.07 if speed == 'normal' else .04)
        snap(name + '-opening')
        if name == 'cancel': cmd('fate_probe cancel')
        elif speed == 'normal' and nout + nin > 0:
            until(lambda: any(t >= .35 for t in status()['progress']))
            snap(name + '-cross')
        after = until(lambda: p if (p := status())['live'] == p['playCount'] == 0 and p['sourcePile'] in ('Exhaust', 'Discard') else None)
        time.sleep(.8); snap(name + '-after'); hand(name + '-after')
        after = status(); save(name + '-after-probe', after)
        destination = 'Draw' if mode == 'void' else 'PromisePile'
        # Custom pile enum uses its registered name; assert one consistent non-native destination.
        assert len(after['sent']) == nout and all(x == 'Draw' if mode == 'void' else x not in (None, 'Hand', 'Draw', 'Discard', 'Exhaust', 'Play') for x in after['sent']), after
        assert sum(x == 'Hand' for x in after['received']) == min(10, nin), after
        assert sum(x == 'Discard' for x in after['received']) == max(0, nin - 10), after
        assert after['handCount'] == min(10, nin), after
        assert after['sourcePile'] == ('Discard' if mode == 'repeat' else 'Exhaust'), after
        if speed == 'instant' or nout + nin == 0: assert not after['cues'], after
        elif name != 'cancel':
            assert len(after['cues']) == 1 and after['cues'][0]['confirmed'], after
            assert after['cues'][0]['sent'] == nout and after['cues'][0]['received'] == min(10, nin), after
            cue = after['cues'][0]
            assert cue['framesBeforeSwap'] > 0 and not cue['movedBeforeSwap'], cue
            assert cue['firstMoveMs'] >= cue['swapMs'] > 0, cue
            assert .49 <= cue['swapProgress'] <= .51, cue
            assert 0 < cue['endMs'] < 1200, cue
        if name == 'repeat':
            index = next(c['index'] for c in state()['player']['hand'] if c['id'] == 'KAREN_EXCHANGE_FATE')
            post(action='play_card', card_index=index)
            until(lambda: (p := status())['live'] == p['playCount'] == 0 and len(p['cues']) == 2)
            time.sleep(.7); snap(name + '-twice-after'); hand(name + '-twice-after')
            second = status(); save(name + '-twice-probe', second)
            assert all(x == 'Hand' for x in second['sent']), second
        if recording: time.sleep(.5); stop_record(name)
        results[name] = dict(passed=True, before=before, after=after); save('results', results)
        print(branch, name, 'PASS', flush=True)
    save('build', {str(p.relative_to(mods / 'ShoujoKagekiAijoKaren')): hashlib.sha256(p.read_bytes()).hexdigest()
        for p in (mods / 'ShoujoKagekiAijoKaren').rglob('*') if p.is_file()})
finally:
    if recording:
        try: stop_record('interrupted')
        except Exception as error: save('recording-error', dict(error=str(error)))
    try:
        api('/settings', dict(muted=True))
    except Exception: pass
    desktop.close()
    if (dev / 'godot.log').exists():
        shutil.copy2(dev / 'godot.log', out / 'godot.log')
        diagnostics = audit((out / 'godot.log').read_text(encoding='utf-8', errors='replace'))
        save('log-audit', diagnostics)

assert not diagnostics['unexpectedErrors'], diagnostics
