"""Targeted real plays and game-process audio recording in an isolated MCP game."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import time
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--cases', default='ordinary,last,upgrade,fast,instant,pose,replay,lethal')
parser.add_argument('--record-cases', default='ordinary,last,upgrade,replay')
parser.add_argument('--capture-stages', action='store_true', help='Separate visual pass; GPU readback may stall a frame')
args = parser.parse_args()
main = Path(__file__).resolve().parents[3]
root = args.output.resolve()
dev = root / 'isolated' / args.branch
game = dev / 'game'
out = root / args.branch
if args.capture_stages: out = out / 'visual-stages'
out.mkdir(parents=True, exist_ok=True)
config = json.loads((game / 'mods/KarenSTS2MCP.conf').read_text(encoding='utf-8-sig'))
base = f"http://127.0.0.1:{config['port']}/api/v1"
opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
with socket.socket() as check:
    check.settimeout(.3)
    assert check.connect_ex(('127.0.0.1', config['port'])) != 0, 'Occupied test port; no deployment'

def api(path, body=None):
    req = urllib.request.Request(base + path, None if body is None else json.dumps(body).encode(),
                                 headers={'Content-Type': 'application/json'})
    with opener.open(req, timeout=25) as response:
        return json.load(response)

def state():
    return api('/singleplayer?format=json')

def cmd(command):
    return post(action='run_command', command=command)

def post(**body):
    response = api('/singleplayer', body)
    with (out / 'actions.jsonl').open('a', encoding='utf-8') as f:
        f.write(json.dumps([body, response], ensure_ascii=False) + '\n')
    assert response.get('status') == 'ok', (body, response)
    return response

def save(path, data):
    path.with_suffix('.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')

def snap(path):
    save(path, state())
    with opener.open(base + '/screenshot', timeout=25) as response:
        path.with_suffix('.png').write_bytes(response.read())

def hand(path):
    h = json.loads(cmd('karen_check_hand')['message'])
    save(path, h)
    assert h['modelCount'] == h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'], h

def wait_for(predicate, timeout=22):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        s = state()
        if predicate(s): return s
        time.sleep(.12)
    raise AssertionError(('Timeout', state()))

def probe():
    return json.loads(cmd('memory_probe status')['message'])

process = None
recording = False
results = json.loads((out / 'results.json').read_text(encoding='utf-8')) if (out / 'results.json').exists() else []
try:
    shutil.copytree(main / 'artifacts/ShoujoKagekiAijoKaren', game / 'mods/ShoujoKagekiAijoKaren', dirs_exist_ok=True)
    for name, folder in [('KarenMemoryProbe', 'probes'), ('KarenBackgroundTestGuard', 'guard')]:
        shutil.copy2(root / folder / args.branch / (name + '.dll'), game / 'mods')
        (game / 'mods' / (name + '.json')).write_text(json.dumps(dict(id=name, name=name, version='1.0.0', author='Development',
            description='Temporary isolated card VFX observation', has_dll=True, has_pck=False, affects_gameplay=False)), encoding='utf-8')
    (game / 'override.cfg').write_text('[display]\nwindow/size/mode=1\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n', encoding='utf-8')
    config.update(muted=True, fullscreen=False)
    (game / 'mods/KarenSTS2MCP.conf').write_text(json.dumps(config), encoding='utf-8')
    environment = os.environ.copy()
    environment['APPDATA'] = str(dev / 'user/AppData/Roaming')
    environment['LOCALAPPDATA'] = str(dev / 'user/AppData/Local')
    environment['STS2_MCP_FFMPEG'] = shutil.which('ffmpeg') or ''
    startup = subprocess.STARTUPINFO()
    startup.dwFlags = subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = 0
    launch_output = (out / 'launch-output.log').open('w', encoding='utf-8')
    process = subprocess.Popen([str(game / 'SlayTheSpire2.exe'), '--rendering-driver', 'vulkan', '--force-steam=off',
                                '--log-file', str(dev / 'godot.log')], cwd=game, env=environment, startupinfo=startup,
                               stdout=launch_output, stderr=subprocess.STDOUT)
    log = dev / 'godot.log'
    deadline = time.monotonic() + 150
    while time.monotonic() < deadline:
        time.sleep(.3)
        if process.poll() is not None: raise RuntimeError('Isolated game exited')
        content = log.read_text(encoding='utf-8', errors='replace') if log.exists() else ''
        if 'System.OutOfMemoryException' in content: raise RuntimeError('Startup memory pressure; retry after other game instances release resources')
        if '[Karen STS2 MCP]' not in content or "Preloading 'Common' Complete:" not in content: continue
        try:
            if 'continue' in state().get('options', []): break
        except Exception: pass
    else: raise RuntimeError('MCP or saved run not ready')
    post(action='menu_select', option='continue')
    wait_for(lambda s: s.get('player') and (s.get('battle', {}).get('is_play_phase') or s['state_type'] in ['rewards', 'map']))
    assembly = game / f'mods/ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.{args.branch.title()}.dll'
    build_sha = hashlib.sha256(assembly.read_bytes()).hexdigest()
    save(out / 'build', dict(assemblySha256=build_sha, pckSha256=hashlib.sha256((game/'mods/ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck').read_bytes()).hexdigest()))
    for name in args.cases.split(','):
        case = out / name
        case.mkdir(exist_ok=True)
        cmd('memory_probe speed normal')
        cmd('fight WATERFALL_GIANT_BOSS')
        wait_for(lambda s: s.get('battle', {}).get('is_play_phase') and any(e['entity_id'].startswith('WATERFALL') for e in s['battle']['enemies']))
        time.sleep(.6)
        mode = name if name in ['ordinary', 'upgrade', 'replay'] else 'last'
        cmd('memory_probe seed ' + mode)
        wait_for(lambda s: any(c['id'] == 'KAREN_SHINE_TOGETHER' for c in s.get('player', {}).get('hand', [])))
        cmd('energy 99')
        if name in ['fast', 'instant']: cmd('memory_probe speed ' + name)
        if name == 'pose': cmd('memory_probe pose')
        if name == 'lethal': cmd('memory_probe lethal')
        time.sleep(1.2)
        cmd('memory_probe watch' + (' ' + case.as_posix() if args.capture_stages else ''))
        snap(case / 'before'); hand(case / 'before-hand')
        before = probe(); save(case / 'before-probe', before)
        hp = state()['battle']['enemies'][0]['hp']
        if name in args.record_cases.split(','):
            api('/settings', dict(muted=False))
            api('/recording/start', dict(fps=30, max_duration_seconds=25, width=1280, height=720, audio=True))
            recording = True
            time.sleep(.65)
        s = state()
        card = next(c for c in s['player']['hand'] if c['id'] == 'KAREN_SHINE_TOGETHER')
        post(action='play_card', card_index=card['index'], target=s['battle']['enemies'][0]['entity_id'])
        if name == 'lethal': wait_for(lambda s: s['state_type'] == 'rewards')
        else: wait_for(lambda s: not any(c['id'] == 'KAREN_SHINE_TOGETHER' for c in s['player']['hand']))
        time.sleep(3.0 if name == 'replay' else 2.6)
        # Native Shine disposal has its own card animation before invoking the reward.
        # Wait for actual registration and cleanup rather than assuming a fixed end time.
        deadline = time.monotonic() + 8
        while time.monotonic() < deadline:
            current = probe()
            if current['live'] == 0 and (name in ['ordinary', 'lethal'] or current['rewardPower'] > before['rewardPower']): break
            time.sleep(.15)
        snap(case / 'after'); hand(case / 'after-hand')
        metrics = probe(); save(case / 'probe', metrics)
        assert metrics['live'] == 0 and metrics['backLive'] == 0 and metrics['particleLive'] == 0 and all(e['Exited'] for e in metrics['effects']), metrics
        count = 0 if name == 'ordinary' else 4 if name == 'upgrade' else 3
        if name != 'lethal':
            damage = (40 if name == 'upgrade' else 30) * (2 if name == 'replay' else 1)
            assert hp - state()['battle']['enemies'][0]['hp'] == damage, (name, state())
            assert metrics['extraRewards'] - before['extraRewards'] == count, metrics
            assert metrics['rewardPower'] - before['rewardPower'] == count, metrics
            if name == 'ordinary': assert metrics['shine'] == 2 and metrics['shinePile'] == before['shinePile'], metrics
            else: assert metrics['shinePile'] == before['shinePile'] + 1, metrics
        effects = metrics['effects']
        if name == 'instant': assert len(effects) == 0, metrics
        else:
            assert len([e for e in effects if e['Kind'] == 'Attack']) == (2 if name == 'replay' else 1), metrics
            for e in effects:
                assert e['Frames'] > 0 and e['MaxAnchorError'] < 1 and e['AssetsReady'], e
                if e['Kind'] == 'Attack':
                    assert abs(e['HitAt'] - .64) < (.25 if args.capture_stages else .10), e
                    assert e['particleEmitters'] == 4 and e['ParticleEmissionFrames'] > 0, e
            assert not any(e['Kind'] == 'Reward' for e in effects), metrics
        if recording:
            time.sleep(.35)
            api('/recording/stop', {})
            for _ in range(150):
                rec = api('/recording')
                if rec['status'] in ['completed', 'failed']: break
                time.sleep(.2)
            assert rec['status'] == 'completed' and rec.get('audio_source') == 'game_process', rec
            shutil.copy2(rec['output_path'], case / 'game.mp4'); save(case / 'recording', rec)
            recording = False
            api('/settings', dict(muted=True))
        if name == 'pose':
            assert all(e['Unit'] < .7 for e in metrics['effects'] if e['Kind'] == 'Attack'), metrics
        results = [r for r in results if r['case'] != name]
        results.append(dict(case=name, passed=True, assemblySha256=build_sha, metrics=metrics))
        save(out / 'results', results)
        print(args.branch, name, 'PASS', flush=True)
finally:
    if recording:
        try: api('/recording/stop', {})
        except Exception: pass
    if process and process.poll() is None:
        process.terminate(); process.wait(timeout=15)
    if (dev / 'godot.log').exists():
        shutil.copy2(dev / 'godot.log', out / 'godot.log')
        shutil.copy2(dev / 'godot.log', out / (time.strftime('run-%Y%m%d-%H%M%S-') + args.cases.replace(',', '-') + '.log'))
