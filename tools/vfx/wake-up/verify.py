"""Targeted native card-play checks; requires catalog/prepare.py and built probe/guard."""
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
from PIL import Image
from check_motion import compare_frames

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--cases', default='base,draw,discard,promise,void,infinite,empty,single,full,instant,ordinary')
args = parser.parse_args()
main = Path(__file__).resolve().parents[3]
root = args.output.resolve()
dev = root / 'isolated' / args.branch
game = dev / 'game'
out = root / args.branch
out.mkdir(exist_ok=True)
config = json.loads((game / 'mods/KarenSTS2MCP.conf').read_text(encoding='utf-8-sig'))
base = f"http://127.0.0.1:{config['port']}/api/v1"
opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
with socket.socket() as port_check:
    port_check.settimeout(.3)
    assert port_check.connect_ex(('127.0.0.1', config['port'])) != 0, 'Test port is occupied; no deployment performed'

def api(path, body=None):
    req = urllib.request.Request(base + path, None if body is None else json.dumps(body).encode(),
                                 headers={'Content-Type': 'application/json'})
    with opener.open(req, timeout=20) as response:
        return json.load(response)

def state():
    return api('/singleplayer?format=json')

def post(**body):
    response = api('/singleplayer', body)
    with (out / 'actions.jsonl').open('a', encoding='utf-8') as f:
        f.write(json.dumps([body, response], ensure_ascii=False) + '\n')
    assert response.get('status') == 'ok', (body, response)
    return response

def cmd(command):
    return post(action='run_command', command=command)

def save(path, value):
    path.with_suffix('.json').write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')

def snap(path):
    save(path, state())
    with opener.open(base + '/screenshot', timeout=20) as response:
        path.with_suffix('.png').write_bytes(response.read())

def check_hand(path):
    result = json.loads(cmd('karen_check_hand')['message'])
    save(path, result)
    assert result['modelCount'] == result['visualCount'] and not result['missing'] and not result['unexpected'] and not result['unexpectedPileIcons'], result

def wait_for(predicate, timeout=20):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        value = state()
        if predicate(value):
            return value
        time.sleep(.1)
    raise AssertionError(('State timeout', state()))

def probe():
    return json.loads(cmd('wake_probe status')['message'])

process = None
recording = False
results_path = out / 'results.json'
results = json.loads(results_path.read_text(encoding='utf-8')) if results_path.exists() else []
try:
    shutil.copytree(main / 'artifacts/ShoujoKagekiAijoKaren', game / 'mods/ShoujoKagekiAijoKaren', dirs_exist_ok=True)
    for name, folder in [('KarenWakeUpProbe', 'probes'), ('KarenBackgroundTestGuard', 'guard')]:
        shutil.copy2(root / folder / args.branch / (name + '.dll'), game / 'mods')
        (game / 'mods' / (name + '.json')).write_text(json.dumps(dict(id=name, name=name, version='1.0.0', author='Development',
            description='Temporary isolated card VFX observation', has_dll=True, has_pck=False, affects_gameplay=False)), encoding='utf-8')
    override = game / 'override.cfg'
    if not override.exists():
        override.write_text('[display]\nwindow/size/mode=1\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n', encoding='utf-8')
    config.update(muted=True, fullscreen=False)
    (game / 'mods/KarenSTS2MCP.conf').write_text(json.dumps(config), encoding='utf-8')
    environment = os.environ.copy()
    environment['APPDATA'] = str(dev / 'user/AppData/Roaming')
    environment['LOCALAPPDATA'] = str(dev / 'user/AppData/Local')
    environment['STS2_MCP_FFMPEG'] = shutil.which('ffmpeg') or ''
    startup = subprocess.STARTUPINFO()
    startup.dwFlags = subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = 0
    process = subprocess.Popen([str(game / 'SlayTheSpire2.exe'), '--rendering-driver', 'vulkan', '--force-steam=off',
                                '--log-file', str(dev / 'godot.log')], cwd=game, env=environment, startupinfo=startup)
    log = dev / 'godot.log'
    deadline = time.monotonic() + 45
    while time.monotonic() < deadline:
        time.sleep(.3)
        if process.poll() is not None:
            raise RuntimeError('Isolated game exited')
        startup_log = log.read_text(encoding='utf-8', errors='replace') if log.exists() else ''
        if '[Karen STS2 MCP]' not in startup_log or "Preloading 'Common' Complete:" not in startup_log:
            continue
        try:
            if 'continue' in state().get('options', []):
                break
        except Exception:
            pass
    else:
        raise RuntimeError('Karen MCP or saved run not ready')
    post(action='menu_select', option='continue')
    wait_for(lambda s: s.get('player') and (s.get('battle', {}).get('is_play_phase') or s['state_type'] in ['rewards', 'map']))
    assembly = game / f'mods/ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.{args.branch.title()}.dll'
    build_sha = hashlib.sha256(assembly.read_bytes()).hexdigest()
    save(out / 'build', dict(assemblySha256=build_sha))

    for name in args.cases.split(','):
        case = out / name
        case.mkdir(exist_ok=True)
        cmd('fight WATERFALL_GIANT_BOSS')
        wait_for(lambda s: s.get('battle', {}).get('is_play_phase') and any(e['entity_id'].startswith('WATERFALL') for e in s['battle']['enemies']))
        time.sleep(.5)
        source = name if name in ['draw', 'discard', 'promise', 'void', 'infinite'] else 'draw'
        count = 0 if name == 'empty' else 1 if name == 'single' else 3
        mode = 'full' if name == 'full' else 'upgrade' if name in ['draw', 'discard', 'promise', 'infinite'] else 'base'
        cmd(f'wake_probe seed {source} {count} {mode}')
        wait_for(lambda s: s.get('state_type') in ['monster', 'boss'] and any(c['id'] == 'KAREN_WAKE_UP' for c in s.get('player', {}).get('hand', [])))
        time.sleep(1.1)
        cmd('wake_probe speed ' + ('instant' if name == 'instant' else 'normal'))
        cmd('wake_probe watch')
        snap(case / 'before')
        check_hand(case / 'before-hand')
        if name in ['base', 'promise']:
            api('/settings', dict(muted=False))
            api('/recording/start', dict(fps=30, max_duration_seconds=20, width=1280, height=720, audio=True))
            recording = True
            time.sleep(.5)
        if name == 'ordinary':
            cmd('draw 1')
        else:
            s = state()
            card = next(c for c in s['player']['hand'] if c['id'] == 'KAREN_WAKE_UP')
            post(action='play_card', card_index=card['index'])
            if mode == 'upgrade':
                s = wait_for(lambda s: s['state_type'] == 'card_select' and s['card_select']['screen_type'] == 'choose')
                time.sleep(.55)  # Native choose-screen entrance disables clicks briefly.
                snap(case / 'source-choice')
                assert probe()['live'] == 0
                wanted = 'PROMISE' if source in ['promise', 'infinite'] else source.upper()
                option = next(c for c in s['card_select']['cards'] if c['id'] == f'KAREN_WAKE_UP_{wanted}_PILE_OPTION')
                post(action='select_card', index=option['index'])
            if count > 2 and name != 'infinite':
                s = wait_for(lambda s: s['state_type'] == 'card_select' and s['card_select']['screen_type'] == 'simple_select')
                time.sleep(.70)
                snap(case / 'selection')
                selected_probe = probe()
                save(case / 'selection-probe', selected_probe)
                assert selected_probe['live'] == 0 and selected_probe['showingCurtains'] == 1
                if name in ['base', 'promise']:
                    # Hold the actual grid still: two screenshots must show moving cloth,
                    # not card flight, screen entrance, hover outlines or just tiny motes.
                    snap(case / 'motion-a')
                    time.sleep(1.0)
                    snap(case / 'motion-b')
                    assert state()['state_type'] == 'card_select'
                    motion = compare_frames([Image.open(case / 'motion-a.png'), Image.open(case / 'motion-b.png')])
                    save(case / 'motion', motion)
                    assert motion['passed'], ('Curtain is visually stationary', motion)
                indices = [c['index'] for c in s['card_select']['cards'][:2]]
                if name == 'controls':
                    cmd('wake_probe peek on')
                    time.sleep(.4)
                    snap(case / 'peek')
                    assert probe()['showingCurtains'] == 0
                    cmd('wake_probe peek off')
                    time.sleep(.4)
                    assert probe()['showingCurtains'] == 1
                if name == 'cancel':
                    cmd('wake_probe abort_vfx')
                    time.sleep(.2)
                    assert probe()['liveCurtains'] == 0
                post(action='select_card', index=indices[0])
                time.sleep(.50)
                assert state()['state_type'] == 'card_select' and probe()['live'] == 0
                if name != 'cancel':
                    assert probe()['selected'] == 1
                    snap(case / 'one-selected')
                if name == 'controls':
                    post(action='select_card', index=indices[0])
                    time.sleep(.2)
                    assert probe()['selected'] == 0
                    post(action='select_card', index=indices[0])
                    time.sleep(.2)
                    assert probe()['selected'] == 1
                post(action='select_card', index=indices[1])
                time.sleep(.18)
                snap(case / 'return-peak')
        time.sleep(2.3)
        snap(case / 'after')
        check_hand(case / 'after-hand')
        metrics = probe()
        save(case / 'probe', metrics)
        started = [e for e in metrics['effects'] if e['Started']]
        expected = 0 if name in ['empty', 'ordinary'] else 1 if name in ['single', 'full'] else 2
        assert len(started) == expected, (name, expected, metrics)
        assert metrics['live'] == 0 and all(e['Freed'] and not e['WrongCard'] for e in metrics['effects']), metrics
        assert metrics['liveCurtains'] == 0 and all(c['Freed'] for c in metrics['curtains']), metrics
        with_grid = count > 2 and name not in ['infinite', 'ordinary']
        curtains = [c for c in metrics['curtains'] if c['Shown']]
        assert len(curtains) == (1 if with_grid else 0), metrics
        assert all(c['BelowCards'] and c['InputIgnored'] for c in curtains), metrics
        if with_grid and name != 'cancel':
            assert all(c['Gathered'] and c['Targets'] == 2 for c in curtains), metrics
        assert all(e['FrameBound'] and 0 < e['MaxSparks'] <= 5 and e['Landed'] for e in started), metrics
        assert all(e['FiniteMesh'] for e in started), ('Non-finite silk mesh vertices', metrics)
        s = state()
        if name != 'ordinary':
            assert len(s['player']['hand']) == (9 if name == 'full' else 3) + expected, s
            assert s['player']['exhaust_pile_count'] == 1, s
        if recording:
            time.sleep(.4)
            api('/recording/stop', {})
            for _ in range(100):
                rec = api('/recording')
                if rec['status'] in ['completed', 'failed']:
                    break
                time.sleep(.2)
            assert rec['status'] == 'completed' and rec.get('audio_source') == 'game_process', rec
            shutil.copy2(rec['output_path'], case / 'game.mp4')
            save(case / 'recording', rec)
            recording = False
            api('/settings', dict(muted=True))
        results = [result for result in results if result['case'] != name]
        results.append(dict(case=name, passed=True, assemblySha256=build_sha, effects=len(started), metrics=metrics))
        save(out / 'results', results)
        print(args.branch, name, 'PASS', len(started), flush=True)
finally:
    if recording:
        try:
            api('/recording/stop', {})
        except Exception:
            pass
    if process and process.poll() is None:
        process.terminate()
        process.wait(timeout=15)
    if (dev / 'godot.log').exists():
        shutil.copy2(dev / 'godot.log', out / 'godot.log')
        shutil.copy2(dev / 'godot.log', out / (time.strftime('run-%Y%m%d-%H%M%S-') + args.cases.replace(',', '-') + '.log'))
