"""Targeted real-card checks and game-process audio recording via KarenSTS2MCP.

Prepare with catalog/prepare.py --skip-build and compile PassionProbe + catalog/guard.
The private desktop and cloned saves keep every change inside this worktree's artifacts.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import sys
import time
import urllib.request

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'six-card'))
from isolated_desktop import TestDesktop
from audit_log import audit

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--cases', default='normal,repeat,fast,instant,cancel,reward,direct,loss,unrelated')
parser.add_argument('--record', action='store_true')
args = parser.parse_args()
main = Path(__file__).resolve().parents[3]
root = args.output.resolve()
dev = root / 'isolated' / args.branch
game = dev / 'game'
mods = game / 'mods'
out = root / args.branch
out.mkdir(exist_ok=True)
config = json.loads((mods / 'KarenSTS2MCP.conf').read_text(encoding='utf-8-sig'))
port = config['port']
with socket.socket() as check:
    assert check.connect_ex(('127.0.0.1', port)) != 0, 'Test port is occupied'
base = f'http://127.0.0.1:{port}/api/v1'
opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))


def api(path, data=None):
    req = urllib.request.Request(base + path, None if data is None else json.dumps(data).encode(), headers={'Content-Type': 'application/json'})
    with opener.open(req, timeout=30) as response:
        return json.load(response)


def state():
    return api('/singleplayer?format=json')


def post(**body):
    result = api('/singleplayer', body)
    with (out / 'actions.jsonl').open('a', encoding='utf-8') as log:
        log.write(json.dumps([body, result], ensure_ascii=False) + '\n')
    assert result.get('status') == 'ok', (body, result)
    return result


def cmd(command):
    return post(action='run_command', command=command)


def probe():
    return json.loads(cmd('passion_probe status')['message'])


def save(name, data):
    (out / (name + '.json')).write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')


def snap(name):
    save(name, state())
    with opener.open(base + '/screenshot', timeout=30) as response:
        (out / (name + '.png')).write_bytes(response.read())


def wait_for(predicate, timeout=25):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        result = state()
        if predicate(result):
            return result
        time.sleep(.1)
    raise AssertionError(('State timeout', state()))


def hand_check(name):
    report = json.loads(cmd('karen_check_hand')['message'])
    save(name + '-hand', report)
    assert report['modelCount'] == report['visualCount'] and not report['missing'] and not report['unexpected'] and not report['unexpectedPileIcons'], report


def fight():
    cmd('fight WATERFALL_GIANT_BOSS')
    wait_for(lambda s: s.get('battle', {}).get('is_play_phase') and any(e['entity_id'].startswith('WATERFALL') for e in s['battle']['enemies']))
    time.sleep(.7)
    cmd('energy 6')
    cmd('passion_probe speed normal')
    for card in list(state()['player']['hand']):
        cmd('remove_card ' + card['id'] + ' Hand')
        time.sleep(.12)
    time.sleep(1.6)


def play():
    card = next(c for c in state()['player']['hand'] if c['id'] == 'KAREN_PASSION')
    post(action='play_card', card_index=card['index'])


def record_start():
    global recording
    api('/settings', dict(muted=False))
    time.sleep(.2)
    api('/recording/start', dict(fps=30, max_duration_seconds=24, width=1280, height=720, audio=True))
    recording = True


def record_stop(name):
    global recording
    api('/recording/stop', {})
    for _ in range(150):
        report = api('/recording')
        if report['status'] in ['completed', 'failed']:
            break
        time.sleep(.2)
    assert report['status'] == 'completed' and report.get('audio_source') == 'game_process', report
    shutil.copy2(report['output_path'], out / (name + '.mp4'))
    save(name + '-recording', report)
    recording = False
    api('/settings', dict(muted=True))


desktop = TestDesktop()
recording = False
results_path = out / 'results.json'
results = json.loads(results_path.read_text(encoding='utf-8')) if results_path.exists() else []
try:
    shutil.copytree(main / 'artifacts/ShoujoKagekiAijoKaren', mods / 'ShoujoKagekiAijoKaren', dirs_exist_ok=True)
    for name, folder in [('KarenPassionProbe', 'probes'), ('KarenBackgroundTestGuard', 'guard')]:
        shutil.copy2(root / folder / args.branch / (name + '.dll'), mods)
        (mods / (name + '.json')).write_text(json.dumps(dict(id=name, name=name, version='1.0.0', author='Development',
            description='Temporary isolated Passion VFX observation', has_dll=True, has_pck=False, affects_gameplay=False)), encoding='utf-8')
    (game / 'override.cfg').write_text('[display]\nwindow/size/mode=1\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n', encoding='utf-8')
    config.update(fullscreen=False, muted=True, skip_first_prompt=True)
    (mods / 'KarenSTS2MCP.conf').write_text(json.dumps(config), encoding='utf-8')
    for path in (dev / 'user').rglob('prefs.save'):
        prefs = json.loads(path.read_text(encoding='utf-8-sig'))
        prefs.update(fast_mode='normal', mute_in_background=False)
        path.write_text(json.dumps(prefs, ensure_ascii=False), encoding='utf-8')
    env = os.environ.copy()
    env.update(APPDATA=str(dev / 'user/AppData/Roaming'), LOCALAPPDATA=str(dev / 'user/AppData/Local'), STS2_MCP_FFMPEG=shutil.which('ffmpeg') or '')
    process = desktop.launch([game / 'SlayTheSpire2.exe', '--rendering-driver', 'vulkan', '--force-steam=off', '--log-file', dev / 'godot.log'], game, env)
    for _ in range(160):
        time.sleep(.4)
        if process.poll() is not None:
            raise RuntimeError('Isolated game exited before MCP initialization: ' + str(process.poll()))
        log = (dev / 'godot.log').read_text(encoding='utf-8', errors='replace') if (dev / 'godot.log').exists() else ''
        if '[Karen STS2 MCP]' not in log or "Preloading 'Common' Complete:" not in log:
            continue
        try:
            if 'continue' in state().get('options', []):
                break
        except OSError:
            pass
    else:
        raise RuntimeError('MCP or saved Karen run unavailable')
    assert '[Background Test Guard]' in log
    package = mods / 'ShoujoKagekiAijoKaren'
    save('build', {str(p.relative_to(package)): hashlib.sha256(p.read_bytes()).hexdigest() for p in
        [package / f'lib/ShoujoKagekiAijoKaren.{args.branch.title()}.dll', package / 'ShoujoKagekiAijoKaren.pck']})
    post(action='menu_select', option='continue')
    wait_for(lambda s: s.get('player') and (s.get('battle', {}).get('is_play_phase') or s['state_type'] in ['map', 'rewards']))
    # Warm import/drawing/audio before capturing any clips.
    fight()
    cmd('card KAREN_PASSION Hand')
    time.sleep(.7)
    play()
    time.sleep(1.8)
    for name in args.cases.split(','):
        fight()
        buff_only = name in ['direct', 'loss', 'unrelated']
        count = 3 if name == 'repeat' else 2 if name == 'direct' else 1
        for _ in range(0 if buff_only else count):
            cmd('card KAREN_PASSION Hand')
            time.sleep(.5)
        if name in ['loss', 'unrelated']:
            cmd('passion_probe gain 2')
            time.sleep(1.8)
        if name == 'upgrade':
            cmd('upgrade 0')
        if name in ['fast', 'instant']:
            cmd('passion_probe speed ' + name)
        if name == 'mirror':
            cmd('passion_probe mirror')
        cmd('passion_probe watch')
        before = probe()
        snap(name + '-before')
        hand_check(name + '-before')
        capturing = args.record and name in ['normal', 'repeat', 'reward']
        if capturing:
            record_start()
            time.sleep(.8)
        if buff_only:
            cmd('passion_probe ' + {'direct': 'gain 2', 'loss': 'lose 1', 'unrelated': 'unrelated'}[name])
        else:
            play()
        if name not in ['loss', 'unrelated']:
            for _ in range(150):
                report = probe()
                if report['power'] > before['power']:
                    break
                time.sleep(.025)
            else:
                raise AssertionError((name, 'Passion did not resolve'))
        if name == 'repeat':
            for expected in [2, 3]:
                play()
                # HTTP acknowledges the queued action before native card execution finishes.
                for _ in range(150):
                    if probe()['power'] - before['power'] >= expected:
                        break
                    time.sleep(.025)
                else:
                    raise AssertionError('Queued repeat did not resolve')
        if name == 'cancel':
            time.sleep(.15)
            cmd('kill all')
        time.sleep(1.8 if name not in ['fast', 'instant'] else 1.3)
        after = probe()
        save(name + '-probe', after)
        if name != 'cancel':
            hand_check(name + '-after')
        else:
            save(name + '-after-hand', dict(skipped='Combat ended; no local combat hand remains.'))
        if name == 'reward':
            cmd('kill all')
            reward_state = wait_for(lambda s: s.get('state_type') == 'rewards')
            save('actual-rewards', reward_state)
            assert any(item['type'] == 'relic' for item in reward_state['rewards']['items']), reward_state
            time.sleep(1.2)
        if capturing:
            record_stop(name)
        snap(name + '-after')
        assert after['live'] == 0 and all(e['Freed'] and e['Finite'] and e['IconValid'] for e in after['effects']), (name, after)
        if name not in ['cancel', 'loss', 'unrelated']:
            assert after['power'] - before['power'] == count, (name, before, after)
            assert after['reserved'] - before['reserved'] == (0 if buff_only else count), (name, before, after)
            assert after['relics'] == before['relics'], 'Relic was given early'
        if name in ['instant', 'loss', 'unrelated']:
            assert not after['effects'], after
            if name == 'loss': assert after['power'] == before['power'] - 1, after
            if name == 'unrelated': assert after['power'] == before['power'], after
        elif name != 'cancel':
            assert after['effects'] and all(e['Arrived'] and e['Bound'] and e['PeakPulses'] <= 3 and 0 <= e['ArrivalError'] < .01 for e in after['effects']), (name, after)
            assert sum(e['Gains'] for e in after['effects']) == count, (name, after)
        results = [r for r in results if r['case'] != name]
        results.append(dict(case=name, passed=True, before=before, after=after))
        save('results', results)
        print(args.branch, name, 'PASS', flush=True)
    log = (dev / 'godot.log').read_text(encoding='utf-8', errors='replace')
    log_audit = audit(log)
    save('log-audit', log_audit)
    assert not log_audit['unexpectedErrors'], dict(errorCount=len(log_audit['unexpectedErrors']), firstErrors=log_audit['unexpectedErrors'][:5])
    print('COMPLETE', out, flush=True)
finally:
    if recording:
        try:
            api('/recording/stop', {})
        except OSError:
            pass
    desktop.close()
