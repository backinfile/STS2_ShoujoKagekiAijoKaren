"""Targeted native-card tests and process-audio recording through KarenSTS2MCP."""
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
parser.add_argument('--source-dev', required=True, type=Path)
parser.add_argument('--output', type=Path)
parser.add_argument('--review-seconds', type=float, default=0)
args = parser.parse_args()
project = Path(__file__).resolve().parents[3]
root = (args.output or project / 'artifacts/position-zero').resolve()
out = root / args.branch
out.mkdir(parents=True, exist_ok=True)
source = args.source_dev.resolve() / args.branch
dev = root / 'isolated' / args.branch
game = dev / 'game'
port = 18971 if args.branch == 'stable' else 18972
with socket.socket() as check:
    assert check.connect_ex(('127.0.0.1', port)) != 0, 'Recording port occupied'

def copy_binary(src, dst):
    if Path(src).suffix.lower() in ('.json', '.conf', '.cfg'):
        return shutil.copy2(src, dst)
    try:
        os.link(src, dst)
    except OSError:
        shutil.copy2(src, dst)
    return dst

if not game.exists():
    shutil.copytree(source / 'game', game, copy_function=copy_binary,
                    ignore=lambda folder, names: ['mods'] if Path(folder) == source / 'game' else [])
    shutil.copytree(source / 'user', dev / 'user')
mods = game / 'mods'
mods.mkdir(exist_ok=True)
shutil.copytree(source / 'game/mods/BaseLib', mods / 'BaseLib', dirs_exist_ok=True)
shutil.copytree(project / 'artifacts/ShoujoKagekiAijoKaren', mods / 'ShoujoKagekiAijoKaren', dirs_exist_ok=True)
for suffix in ['dll', 'json']:
    shutil.copy2(source / f'game/mods/KarenSTS2MCP.{suffix}', mods)
shutil.copy2(project / f'artifacts/position-zero/probes/{args.branch}/KarenPositionZeroProbe.dll', mods)
(mods / 'KarenPositionZeroProbe.json').write_text(json.dumps(dict(
    id='KarenPositionZeroProbe', name='KarenPositionZeroProbe', version='1.0.0',
    author='Development', description='Temporary native-card observation fixture',
    has_dll=True, has_pck=False, affects_gameplay=True)), encoding='utf-8')
(mods / 'KarenSTS2MCP.conf').write_text(json.dumps(dict(port=port, fullscreen=False, muted=True,
    skip_tutorial=True, skip_first_prompt=True, window_width=1280, window_height=720)), encoding='utf-8')
# Configs are private copies. Do not minimize while capturing frames.
(game / 'override.cfg').write_text('[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\n'
    'window/size/window_width_override=1280\nwindow/size/window_height_override=720\n', encoding='utf-8')
for settings in (dev / 'user').rglob('settings.save'):
    data = json.loads(settings.read_text(encoding='utf-8-sig'))
    data.update(fullscreen=False, mute_in_background=False, limit_fps_in_background=False,
                seen_ea_disclaimer=True, skip_intro_logo=True, window_size={'X': 1280, 'Y': 720})
    settings.write_text(json.dumps(data), encoding='utf-8')
env = os.environ.copy()
env.update(APPDATA=str(dev / 'user/AppData/Roaming'), LOCALAPPDATA=str(dev / 'user/AppData/Local'),
           STS2_MCP_FFMPEG=shutil.which('ffmpeg'))
startup = subprocess.STARTUPINFO()
startup.dwFlags = subprocess.STARTF_USESHOWWINDOW
startup.wShowWindow = 0
process = subprocess.Popen([str(game / 'SlayTheSpire2.exe'), '--rendering-driver', 'vulkan',
    '--force-steam=off', '--log-file', str(dev / 'godot.log')], cwd=game, env=env, startupinfo=startup)
base = f'http://127.0.0.1:{port}/api/v1'
recording = False
results = {}

def save(name, data):
    (out / (name + '.json')).write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')

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

def wait(predicate, seconds=40):
    deadline = time.monotonic() + seconds
    last = None
    while time.monotonic() < deadline:
        assert process.poll() is None, 'Test game exited'
        try:
            last = state()
            if predicate(last):
                return last
        except (OSError, ValueError):
            pass
        time.sleep(.15)
    save('timeout', last)
    raise TimeoutError(last)

def shot(name):
    save(name, state())
    with urllib.request.urlopen(base + '/screenshot', timeout=30) as response:
        (out / (name + '.png')).write_bytes(response.read())

def status():
    return json.loads(cmd('pzero status')['message'])

def hand(name):
    result = cmd('karen_check_hand')
    save(name + '-hand', result)
    h = json.loads(result['message'])
    assert h['modelCount'] == h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'], h

def prepare(speed='normal'):
    cmd('pzero speed ' + speed)
    cmd('fight WATERFALL_GIANT_BOSS')
    wait(lambda s: s.get('battle', {}).get('is_play_phase') and
         any(e['entity_id'].startswith('WATERFALL') for e in s['battle']['enemies']))
    time.sleep(.7)
    for card in list(state()['player']['hand']):
        cmd('remove_card ' + card['id'] + ' Hand')
        time.sleep(.12)
    time.sleep(.5)
    cmd('energy 8')
    cmd('pzero watch')
    hand('setup-' + speed)

def add(card):
    cmd('card ' + card + ' Hand')
    time.sleep(.5)

def play(card):
    current = state()
    before_power = next((p['amount'] for p in current['player']['status'] if p['id'] == 'KAREN_POSITION0_POWER'), 0)
    c = next(c for c in current['player']['hand'] if c['id'] == card)
    action = dict(action='play_card', card_index=c['index'])
    if c['target_type'] == 'AnyEnemy':
        action['target'] = current['battle']['enemies'][0]['entity_id']
    post(**action)
    wait(lambda s: s.get('battle', {}).get('is_play_phase') and
         len(s['player']['hand']) < len(current['player']['hand']))
    if card == 'KAREN_POSITION0':
        wait(lambda s: next((p['amount'] for p in s['player']['status'] if p['id'] == 'KAREN_POSITION0_POWER'), 0) > before_power)
    time.sleep(.75)

def check(name, predicate):
    report = status()
    save(name, report)
    assert predicate(report), (name, report)
    results[name] = report
    return report

def remove(name):
    cmd('pzero remove')
    time.sleep(.25)
    return check(name, lambda r: r['live'] == r['back'] == 0 and abs(r['weight'] - 1) < .001)

try:
    # Observe the specified MCP implementation in this process before its first request.
    deadline = time.monotonic() + 50
    while time.monotonic() < deadline:
        if (dev / 'godot.log').exists() and '[Karen STS2 MCP]' in (dev / 'godot.log').read_text(encoding='utf-8', errors='replace'):
            break
        assert process.poll() is None
        time.sleep(.2)
    else:
        raise TimeoutError('Karen STS2 MCP did not load')
    initial = wait(lambda s: 'continue' in s.get('options', []))
    save('initial', initial)
    api('/settings', dict(fullscreen=False, muted=True, window_width=1280, window_height=720))
    # Allow native asset preload to complete before entering the copied saved Karen run.
    deadline = time.monotonic() + 45
    while time.monotonic() < deadline:
        if "Preloading 'Common' Complete:" in (dev / 'godot.log').read_text(encoding='utf-8', errors='replace'):
            break
        time.sleep(.2)
    post(action='menu_select', option='continue')
    time.sleep(3)
    wait(lambda s: bool(s.get('player')) and (s.get('battle', {}).get('is_play_phase')
         or s.get('state_type') in ['map', 'rewards']))
    prepare()
    for card in ['KAREN_POSITION0', 'KAREN_STRIKE', 'KAREN_STRIKE']:
        add(card)
    shot('before')
    hand('before')
    api('/settings', dict(muted=False))
    api('/recording/start', dict(fps=30, max_duration_seconds=75, width=1280, height=720, audio=True))
    recording = True
    time.sleep(1)
    play('KAREN_POSITION0')
    time.sleep(.4)
    shot('claim')
    time.sleep(1.6)
    check('normal-claim', lambda r: r['live'] == r['amount'] == 1 and r['back'] == 0 and r['ownership'] and r['maxLive'] == 1)
    if args.review_seconds:
        print('REVIEW READY', flush=True)
        time.sleep(args.review_seconds)
    play('KAREN_STRIKE')
    time.sleep(1.15)
    first = check('normal-trigger', lambda r: r['confirmations'] == 1 and r['block'] > 0 and r['maxReturns'] >= 1)
    play('KAREN_STRIKE')
    time.sleep(1.1)
    check('normal-limit', lambda r: r['confirmations'] == 1 and r['block'] == first['block'])
    hand('after-attacks')
    shot('hold')
    # The next natural turn resets the existing attack counter.
    round_before = state()['battle']['round']
    post(action='end_turn')
    wait(lambda s: s.get('battle', {}).get('is_play_phase') and s['battle']['round'] > round_before)
    cmd('energy 8')
    add('KAREN_STRIKE')
    play('KAREN_STRIKE')
    time.sleep(1.15)
    check('normal-next-turn', lambda r: r['confirmations'] == 2 and r['live'] == 1 and r['back'] == 0)
    time.sleep(.5)
    api('/recording/stop', {})
    for _ in range(150):
        rec = api('/recording')
        if rec['status'] in ['completed', 'failed']:
            break
        time.sleep(.2)
    assert rec['status'] == 'completed' and rec.get('audio') and rec['audio_source'] == 'game_process', rec
    recording = False
    package = mods / 'ShoujoKagekiAijoKaren'
    rec['build'] = {name: hashlib.sha256((package / name).read_bytes()).hexdigest() for name in
                    [f'lib/ShoujoKagekiAijoKaren.{args.branch.title()}.dll', 'ShoujoKagekiAijoKaren.pck']}
    save('recording', rec)
    shutil.copy2(rec['output_path'], out / 'game.mp4')
    api('/settings', dict(muted=True))
    shot('after')
    hand('after')
    remove('removed')
    print(args.branch, 'recorded normal', flush=True)

    prepare()
    add('KAREN_POSITION0'); add('KAREN_POSITION0'); add('TWIN_STRIKE')
    play('KAREN_POSITION0'); time.sleep(.3)
    play('KAREN_POSITION0'); time.sleep(.4)
    play('TWIN_STRIKE'); time.sleep(1.1)
    check('stack-multihit', lambda r: r['amount'] == 2 and r['confirmations'] == 2 and r['live'] == r['maxLive'] == 1 and r['back'] == 0)
    remove('stack-removed')

    prepare('fast')
    add('KAREN_POSITION0'); add('KAREN_STRIKE')
    play('KAREN_POSITION0'); time.sleep(.3)
    play('KAREN_STRIKE'); time.sleep(1)
    check('fast', lambda r: r['confirmations'] == 1 and r['live'] == 1)
    remove('fast-removed')

    prepare('instant')
    add('KAREN_POSITION0'); add('KAREN_STRIKE')
    play('KAREN_POSITION0'); play('KAREN_STRIKE'); time.sleep(.4)
    check('instant', lambda r: r['live'] == r['back'] == r['maxLive'] == r['confirmations'] == 0 and r['amount'] == 1 and r['block'] > 0)
    remove('instant-removed')

    prepare()
    add('KAREN_POSITION0'); add('KAREN_STRIKE')
    play('KAREN_POSITION0'); time.sleep(.3)
    cmd('pzero weak-attack'); time.sleep(.3)
    play('KAREN_STRIKE'); time.sleep(1)
    check('blocked-gain', lambda r: r['block'] == r['confirmations'] == 0 and r['live'] == 1)
    remove('no-block-removed')

    prepare()
    add('KAREN_POSITION0')
    play('KAREN_POSITION0')
    check('before-combat-end', lambda r: r['live'] == 1)
    cmd('kill all')
    wait(lambda s: s.get('state_type') == 'rewards')
    time.sleep(.4)
    check('combat-end-cleanup', lambda r: r['live'] == r['back'] == 0 and abs(r['weight'] - 1) < .001)
    save('results', dict(passed=True, checks=results))
    print(args.branch, 'PASS', len(results), 'targeted assertions', flush=True)
finally:
    if recording:
        try:
            api('/recording/stop', {})
        except OSError:
            pass
    if process.poll() is None:
        process.terminate()
        process.wait(timeout=15)
    if (dev / 'godot.log').exists():
        shutil.copy2(dev / 'godot.log', out / 'godot.log')
