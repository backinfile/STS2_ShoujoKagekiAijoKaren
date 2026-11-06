"""Record actual Shiv/Encore card plays through KarenSTS2MCP in a disposable game copy."""
import argparse
import hashlib
import json
import os
import pathlib
import re
import shutil
import socket
import subprocess
import time
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
parser.add_argument('--source-dev', type=pathlib.Path, required=True)
parser.add_argument('--output', type=pathlib.Path, required=True)
parser.add_argument('--keep-game', action='store_true')
args = parser.parse_args()
project = pathlib.Path(__file__).resolve().parents[3]
source = args.source_dev.resolve() / args.branch
out = args.output.resolve() / args.branch
out.mkdir(parents=True, exist_ok=True)
dev = out / 'isolated'
game = dev / 'game'
port = 18647 if args.branch == 'stable' else 18648
with socket.socket() as sock:
    assert sock.connect_ex(('127.0.0.1', port)) != 0, 'Recording port already in use'

def link_binary(src, dst):
    # Never hardlink mutable configuration into the disposable game.
    if pathlib.Path(src).suffix.lower() in ['.cfg', '.conf', '.json']:
        return shutil.copy2(src, dst)
    try:
        os.link(src, dst)
    except OSError:
        shutil.copy2(src, dst)
    return dst

if not dev.exists():
    shutil.copytree(source / 'game', game, copy_function=link_binary,
                    ignore=lambda folder, names: ['mods'] if pathlib.Path(folder) == source / 'game' else [])
    shutil.copytree(source / 'user', dev / 'user')
mods = game / 'mods'
mods.mkdir(exist_ok=True)
shutil.copytree(source / 'game/mods/BaseLib', mods / 'BaseLib', dirs_exist_ok=True)
for suffix in ['dll', 'json']:
    shutil.copy2(source / ('game/mods/KarenSTS2MCP.' + suffix), mods)
shutil.copytree(project / 'artifacts/ShoujoKagekiAijoKaren', mods / 'ShoujoKagekiAijoKaren', dirs_exist_ok=True)
(mods / 'KarenSTS2MCP.conf').write_text(json.dumps(dict(port=port)), encoding='utf-8')
backups = {p: p.read_bytes() for p in (dev / 'user').rglob('*.save')
           if p.name in ['settings.save', 'prefs.save']}
for p in backups:
    settings = json.loads(p.read_text(encoding='utf-8-sig'))
    if p.name == 'prefs.save':
        settings.update(fast_mode='normal', mute_in_background=False)
    else:
        settings.update(fullscreen=False, volume_master=0.5, limit_fps_in_background=False,
                        seen_ea_disclaimer=True, skip_intro_logo=True,
                        window_size={'X': 1280, 'Y': 720})
    p.write_text(json.dumps(settings, ensure_ascii=False, indent=2), encoding='utf-8')
override = game / 'override.cfg'
override_backup = override.read_bytes() if override.exists() else None
override.write_text('[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\n'
                    'window/size/window_width_override=1280\nwindow/size/window_height_override=720\n', encoding='utf-8')
env = os.environ.copy()
env['APPDATA'] = str(dev / 'user/AppData/Roaming')
env['LOCALAPPDATA'] = str(dev / 'user/AppData/Local')
env['STS2_MCP_FFMPEG'] = shutil.which('ffmpeg')
startup = subprocess.STARTUPINFO()
startup.dwFlags = subprocess.STARTF_USESHOWWINDOW
startup.wShowWindow = 0
process = subprocess.Popen([str(game / 'SlayTheSpire2.exe'), '--rendering-driver', 'vulkan',
                            '--force-steam=off', '--log-file', str(out / 'godot.log')],
                           cwd=game, env=env, startupinfo=startup)
(out / 'process.json').write_text(json.dumps(dict(pid=process.pid, port=port, game=str(game))))
base = f'http://127.0.0.1:{port}/api/v1'
recording = False
markers = []

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

def wait_for(predicate, timeout=40):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        if process.poll() is not None:
            raise RuntimeError('Game exited: ' + str(process.returncode))
        try:
            current = state()
            if predicate(current):
                return current
        except (OSError, ValueError):
            pass
        time.sleep(0.1)
    raise TimeoutError('Game state did not become ready')

def save(name, data):
    (out / (name + '.json')).write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')

def snap(name):
    save(name, state())
    with urllib.request.urlopen(base + '/screenshot', timeout=30) as response:
        (out / (name + '.png')).write_bytes(response.read())

def play(card_id, title, pause):
    current = state()
    card = next(c for c in current['player']['hand'] if c['id'] == card_id)
    target = current['battle']['enemies'][0]['entity_id']
    markers.append(dict(time=time.monotonic() - clock, title=title))
    post(action='play_card', card_index=card['index'], target=target)
    time.sleep(pause)
    return wait_for(lambda s: len(s['player']['hand']) < len(current['player']['hand']), 10)

try:
    # Confirm the intended MCP is loaded before contacting the interface.
    for _ in range(180):
        if (out / 'godot.log').exists() and '[Karen STS2 MCP]' in (out / 'godot.log').read_text(encoding='utf-8', errors='replace'):
            break
        if process.poll() is not None:
            raise RuntimeError('Game exited before MCP load')
        time.sleep(0.25)
    else:
        raise TimeoutError('KarenSTS2MCP did not load')
    current = wait_for(lambda s: s.get('state_type') == 'menu')
    save('initial-settings', api('/settings'))
    # A normal window is essential: minimizing freezes GPU particle rendering.
    save('recording-settings', api('/settings', dict(fullscreen=False, muted=False,
         skip_tutorial=True, skip_first_prompt=True, window_width=1280, window_height=720)))
    for _ in range(160):
        if "Preloading 'Common' Complete:" in (out / 'godot.log').read_text(encoding='utf-8', errors='replace'):
            break
        time.sleep(0.25)
    assert 'continue' in state().get('options', []), 'Source user must contain a saved Karen run'
    post(action='menu_select', option='continue')
    wait_for(lambda s: s.get('player') is not None)
    time.sleep(2)
    cmd('fight WATERFALL_GIANT_BOSS')
    wait_for(lambda s: s.get('battle', {}).get('is_play_phase', False)
             and any(e['entity_id'].startswith('WATERFALL') for e in s['battle']['enemies']))
    time.sleep(1)
    assert state()['player']['character'] in ['爱城华恋', 'Karen', 'Aijo Karen']
    for card in list(state()['player']['hand']):
        cmd('remove_card ' + card['id'] + ' Hand')
        time.sleep(0.2)
    time.sleep(2.2)
    for card in ['SHIV'] + ['KAREN_CONTINUE'] * 7:
        cmd('card ' + card + ' Hand')
        time.sleep(0.45)
    # Native upgrade command uses a zero-based hand position.
    cmd('upgrade 7')
    time.sleep(0.7)
    snap('before')
    before = state()
    hand_check = cmd('karen_check_hand')
    save('before-hand', hand_check)
    hand = json.loads(hand_check['message'])
    assert hand['modelCount'] == hand['visualCount'] == 8 and not hand['missing'] and not hand['unexpected']
    save('recording-start', api('/recording/start', dict(fps=30, max_duration_seconds=30,
                                                       width=1280, height=720, audio=True)))
    recording = True
    clock = time.monotonic()
    time.sleep(1)
    play('SHIV', '原生 Shiv 对照', 1.8)
    play('KAREN_CONTINUE', '续演｜单次命中', 1.8)
    for i in range(5):
        play('KAREN_CONTINUE', f'续演｜连续第 {i + 1} 张', 0.16)
    time.sleep(1.2)
    play('KAREN_CONTINUE', '续演+｜升级命中', 1.8)
    time.sleep(0.5)
    api('/recording/stop', {})
    for _ in range(100):
        rec = api('/recording')
        if rec['status'] in ['completed', 'failed']:
            break
        time.sleep(0.2)
    assert rec['status'] == 'completed' and rec.get('audio_source') == 'game_process', rec
    recording = False
    shutil.copy2(rec['output_path'], out / 'game.mp4')
    package = mods / 'ShoujoKagekiAijoKaren'
    rec['capture_build'] = {name: hashlib.sha256((package / name).read_bytes()).hexdigest() for name in
        [f'lib/ShoujoKagekiAijoKaren.{args.branch.title()}.dll', 'ShoujoKagekiAijoKaren.pck']}
    save('recording', rec)
    save('markers', markers)
    snap('after')
    after = state()
    hand_check = cmd('karen_check_hand')
    save('after-hand', hand_check)
    hand = json.loads(hand_check['message'])
    assert hand['modelCount'] == hand['visualCount'] == 0 and not hand['missing'] and not hand['unexpected']
    assert before['battle']['enemies'][0]['hp'] - after['battle']['enemies'][0]['hp'] == 34
    assert after['player']['exhaust_pile_count'] - before['player']['exhaust_pile_count'] == 8
    sound = subprocess.run(['ffmpeg', '-hide_banner', '-i', str(out / 'game.mp4'), '-vn',
                            '-af', 'volumedetect', '-f', 'null', 'NUL'], capture_output=True, text=True, check=True)
    (out / 'audio-check.txt').write_text(sound.stderr, encoding='utf-8')
    peak = float(re.search(r'max_volume: ([\d.\-]+) dB', sound.stderr)[1])
    assert peak > -60, 'Game audio is effectively silent: ' + str(peak)
    save('validation', dict(damage=34, exhausted=8, hand_consistent=True, audio_peak_db=peak,
                           fps=rec['fps'], frames_captured=rec['frames_captured'],
                           frames_repeated=rec['frames_repeated'], capture_build=rec['capture_build']))
    print('RECORDED', out / 'game.mp4', flush=True)
finally:
    if recording:
        try:
            api('/recording/stop', {})
        except OSError:
            pass
    if not args.keep_game:
        if process.poll() is None:
            try:
                initial = json.loads((out / 'initial-settings.json').read_text(encoding='utf-8'))
                api('/settings', dict(fullscreen=initial['fullscreen'], muted=initial['muted']))
                api('/window/minimize', {})
            except (OSError, KeyError, FileNotFoundError):
                pass
            process.terminate()
            process.wait(timeout=15)
        for path, data in backups.items():
            path.write_bytes(data)
        if override_backup is None:
            override.unlink(missing_ok=True)
        else:
            override.write_bytes(override_backup)
