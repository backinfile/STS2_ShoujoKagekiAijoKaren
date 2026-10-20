"""Targeted native entrance recording and timing checks in an isolated game copy."""
import argparse
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
parser.add_argument('--port', type=int, required=True)
parser.add_argument('--sound-only', action='store_true', help='Isolate the actual lamp sample from entrance visuals')
parser.add_argument('--clean-preview', action='store_true', help='Remove the native relic fixture for a clear visual preview')
parser.add_argument('--label', default='', help='Keep a named evidence set alongside the original run')
parser.add_argument('--cases', default='normal,fast,instant,cancel,overlay,attack,material')
args = parser.parse_args()
repo = Path(__file__).resolve().parents[3]
root = args.output.resolve()
dev = root / 'isolated' / args.branch
game = dev / 'game'
mods = game / 'mods'
out = root / args.label / args.branch
out.mkdir(parents=True, exist_ok=True)
base = f'http://127.0.0.1:{args.port}/api/v1'
with socket.socket() as check:
    assert check.connect_ex(('127.0.0.1', args.port)) != 0, 'Test port already in use'
shutil.copytree(repo / 'artifacts/ShoujoKagekiAijoKaren', mods / 'ShoujoKagekiAijoKaren', dirs_exist_ok=True)
for name, folder in [('KarenEntranceProbe', 'probe'), ('KarenBackgroundTestGuard', 'guard')]:
    shutil.copy2(root / folder / args.branch / (name + '.dll'), mods / (name + '.dll'))
    (mods / (name + '.json')).write_text(json.dumps(dict(id=name, name=name, version='1.0.0',
        author='Development', description='Isolated entrance verification', has_dll=True, has_pck=False,
        affects_gameplay=False)), encoding='utf-8')
(game / 'override.cfg').write_text('[display]\nwindow/size/mode=1\nwindow/size/no_focus=true\n'
    'window/size/window_width_override=1280\nwindow/size/window_height_override=720\n', encoding='utf-8')
(mods / 'KarenSTS2MCP.conf').write_text(json.dumps(dict(port=args.port, fullscreen=False, muted=True)), encoding='utf-8')

def api(path, data=None):
    req = urllib.request.Request(base + path, data=None if data is None else json.dumps(data).encode(),
        headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=30) as response:
        return json.load(response)

def state(): return api('/singleplayer?format=json')

def action(**data):
    result = api('/singleplayer', data)
    with (out / 'actions.jsonl').open('a', encoding='utf-8') as f:
        f.write(json.dumps([data, result], ensure_ascii=False) + '\n')
    assert result.get('status') == 'ok', (data, result)
    return result

def cmd(command): return action(action='run_command', command=command)

def save(path, value): path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')

def screenshot(path):
    with urllib.request.urlopen(base + '/screenshot', timeout=30) as response:
        path.write_bytes(response.read())

def wait_play():
    for _ in range(120):
        if state().get('battle', {}).get('is_play_phase'): return
        time.sleep(.1)
    raise AssertionError(('Combat did not reach play phase', state()))

def end_recording(case):
    api('/recording/stop', {})
    for _ in range(100):
        result = api('/recording')
        if result['status'] in ('completed', 'failed'): break
        time.sleep(.2)
    assert result['status'] == 'completed', result
    shutil.copy2(result['output_path'], case / 'game.mp4')
    save(case / 'recording.json', result)

env = os.environ.copy()
env.update(APPDATA=str(dev / 'user/AppData/Roaming'), LOCALAPPDATA=str(dev / 'user/AppData/Local'),
    STS2_MCP_FFMPEG=shutil.which('ffmpeg'))
startup = subprocess.STARTUPINFO()
startup.dwFlags = subprocess.STARTF_USESHOWWINDOW
startup.wShowWindow = 0
process = subprocess.Popen([str(game / 'SlayTheSpire2.exe'), '--rendering-driver', 'vulkan', '--force-steam=off',
    '--log-file', str(dev / 'godot.log')], cwd=game, env=env, startupinfo=startup)
recording = False
results = {}
try:
    for _ in range(160):
        time.sleep(.25)
        try:
            log = (dev / 'godot.log').read_text(encoding='utf-8', errors='replace')
            if '[Karen STS2 MCP]' in log and "Preloading 'Common' Complete:" in log and 'continue' in state().get('options', []): break
        except (OSError, ValueError): pass
    else: raise RuntimeError('Isolated game did not reach ready menu')
    action(action='menu_select', option='continue')
    for _ in range(120):
        if state().get('player'): break
        time.sleep(.2)
    time.sleep(4)
    assert any(r['id'] == 'KAREN_HAIRPIN_RELIC' for r in state()['player']['relics']), state()['player']
    if state().get('state_type') in ('monster', 'elite', 'boss'): wait_play()
    if args.sound_only:
        case = out / 'sound'
        case.mkdir(exist_ok=True)
        api('/settings', dict(muted=False))
        save(case / 'audio-before.json', json.loads(cmd('entrance_probe audio')['message']))
        api('/recording/start', dict(fps=30, max_duration_seconds=6, width=1280, height=720, audio=True))
        recording = True
        time.sleep(.3)
        cmd('entrance_probe sound')
        time.sleep(1.6)
        save(case / 'audio-after.json', json.loads(cmd('entrance_probe audio')['message']))
        end_recording(case)
        recording = False
        api('/settings', dict(muted=True))
        print(args.branch, 'sound', (case / 'audio-after.json').read_text(encoding='utf-8'), flush=True)
        raise SystemExit(0)
    # A real BeforeCombatStart relic supplies a promise token after the entrance.
    if args.clean_preview:
        while any(r['id'] == 'KAREN_STAR_HAIRCLIP_RELIC' for r in state()['player']['relics']):
            cmd('relic remove KAREN_STAR_HAIRCLIP_RELIC')
            time.sleep(.1)
    else:
        cmd('relic KAREN_STAR_HAIRCLIP_RELIC')
    for speed in args.cases.split(','):
        case = out / speed
        case.mkdir(exist_ok=True)
        config_mode = 'off' if speed == 'config-off' else 'on' if speed == 'config-on' else 'status'
        config = json.loads(cmd('entrance_probe config ' + config_mode)['message'])
        assert config['defaultEnabled'] and config['enabled'] == (speed != 'config-off'), config
        if speed.startswith('config-'):
            # Reload the value written by the real BaseLib save path.
            config = json.loads(cmd('entrance_probe config reload')['message'])
            saved = json.loads(Path(config['path']).read_text(encoding='utf-8-sig'))
            assert config['enabled'] == (speed != 'config-off'), config
            assert saved['EnableCombatEntrance'].lower() == str(config['enabled']).lower(), saved
        save(case / 'config.json', config)
        cmd('entrance_probe speed ' + ('normal' if speed in ['cancel', 'overlay', 'attack', 'material', 'tower'] else speed))
        fixture = 'mark' if speed == 'overlay' else speed if speed in ['attack', 'material', 'tower'] else ''
        cmd('entrance_probe watch' + (' ' + fixture if fixture else ''))
        save(case / 'before.json', state())
        screenshot(case / 'before.png')
        api('/settings', dict(muted=False))
        save(case / 'audio-before.json', json.loads(cmd('entrance_probe audio')['message']))
        api('/recording/start', dict(fps=30, max_duration_seconds=12, width=1280, height=720, audio=True))
        recording = True
        start = time.monotonic()
        cmd('fight WATERFALL_GIANT_BOSS')
        for _ in range(100):
            probe = json.loads(cmd('entrance_probe status')['message'])
            if probe['beforeStartCalls'] > 0: break
            time.sleep(.03)
        else: raise AssertionError(('Native combat did not start', probe))
        if speed == 'cancel':
            for _ in range(100):
                probe = json.loads(cmd('entrance_probe status')['message'])
                if probe['casts'] > 0 and probe['maxAge'] >= .45: break
                time.sleep(.03)
            assert probe['casts'] == 1, probe
            cmd('entrance_probe cancel')
        wait_play()
        play_phase_probe = json.loads(cmd('entrance_probe status')['message'])
        save(case / 'at-play-phase.json', play_phase_probe)
        for _ in range(60):
            if json.loads(cmd('entrance_probe status')['message'])['live'] == 0: break
            time.sleep(.03)
        time.sleep(.4)
        result = json.loads(cmd('entrance_probe status')['message'])
        expected_casts = 0 if speed in ['instant', 'config-off'] else 1
        assert result['casts'] == expected_casts and result['soundCues'] == 0, result
        assert result['live'] == result['backLive'] == 0, result
        assert result['posesLive'] == result['towerEntrancesLive'] == 0 and result['materialPreserved'], result
        assert result['colorsPreserved'] and result['beforeStartCalls'] == 1, result
        assert result['beforeStartVisible'] == expected_casts, result
        if expected_casts:
            assert result['beforeStartAge'] < .1, result
            assert result['concurrentFrames'] > 0, result
        if speed == 'overlay': assert result['colorMarked'], result
        if speed == 'tower': assert result['towerInterrupted'] and not result['towerRestarted'] and result['towerEntranceFrames'] > 0, result
        if speed in ['normal', 'fast', 'overlay', 'config-on']:
            assert result['poseFrames'] > 0 and result['maxCape'] > .5 and result['maxParticles'] >= 20, result
            assert result['towerPartialFrames'] > 0 and result['towerRevealMax'] > .99, result
        if speed == 'attack':
            assert result['nativeTriggered'] and result['nativeInterrupted'] and result['nativeMotionFrames'] > 0, result
        # Native setup runs concurrently and can consume part of the first
        # frame. Wall time between sampled frames is not the full cue duration.
        if speed in ['normal', 'fast']: assert result['maxAge'] >= 1.3, result
        save(case / 'audio-after.json', json.loads(cmd('entrance_probe audio')['message']))
        result['elapsedToPlay'] = time.monotonic() - start
        results[speed] = result
        save(case / 'probe.json', result)
        hand_result = cmd('karen_check_hand')
        hand = json.loads(hand_result['message'])
        assert hand['modelCount'] == hand['visualCount'] and not hand['missing'] and not hand['unexpected'], hand
        save(case / 'hand.json', hand_result)
        save(case / 'after.json', state())
        screenshot(case / 'after.png')
        end_recording(case)
        recording = False
        api('/settings', dict(muted=True))
        print(args.branch, speed, json.dumps(result), flush=True)
    save(out / 'verification.json', results)
    log = (dev / 'godot.log').read_text(encoding='utf-8', errors='replace')
    errors = [line for line in log.splitlines() if 'ERROR:' in line]
    # Asset preloading may have a known worker-pool warning; operational errors
    # such as adding nodes during tree removal must fail this verification.
    assert not [line for line in errors if 'Invalid Task ID' not in line], errors
finally:
    if recording:
        try: api('/recording/stop', {})
        except Exception: pass
    process.terminate()
    try: process.wait(timeout=15)
    except subprocess.TimeoutExpired: process.kill(); process.wait()
