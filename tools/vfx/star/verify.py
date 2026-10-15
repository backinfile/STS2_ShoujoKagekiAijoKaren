"""Targeted real-game KarenStar verification and process-audio recording over KarenSTS2MCP."""
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

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
parser.add_argument('--output', required=True, type=Path)
parser.add_argument('--cases', default='normal9,discard3,draw1,promise4,fast,instant,cancel,multiple,stacked,high14,lethal')
parser.add_argument('--source-dev', type=Path, default=Path('D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/artifacts/dev-game'))
parser.add_argument('--no-record', action='store_true', help='Capture screenshots and assertions without a continuous video stream')
args = parser.parse_args()
repo = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(repo / 'tools/vfx/six-card'))
from isolated_desktop import TestDesktop
root = args.output.resolve()
assert not root.exists(), 'Use a fresh evidence directory'
root.mkdir(parents=True)
branch = args.branch
port = 19167 if branch == 'stable' else 19168
with socket.socket() as check:
    assert check.connect_ex(('127.0.0.1', port)) != 0, 'Port is already in use'
subprocess.run([sys.executable, str(repo / 'tools/vfx/catalog/prepare.py'), branch, '--output', str(root),
    '--skip-build', '--source-dev', str(args.source_dev), '--port', str(port)], check=True)
dev = root / 'isolated' / branch
game = dev / 'game'
mods = game / 'mods'
shutil.copytree(repo / 'artifacts/ShoujoKagekiAijoKaren', mods / 'ShoujoKagekiAijoKaren')
probe = root / 'probe'
subprocess.run(['dotnet', 'build', str(repo / 'tools/vfx/star/StarProbe.csproj'), f'-p:Game={game}',
    f'-p:Variant={branch.title()}', '-o', str(probe), '--nologo', '-v:q'], check=True)
shutil.copy2(probe / 'KarenStarProbe.dll', mods)
(mods / 'KarenStarProbe.json').write_text(json.dumps(dict(id='KarenStarProbe', name='KarenStarProbe', version='1.0.0',
    author='Development', description='Temporary isolated fixture and observer', has_dll=True, has_pck=False, affects_gameplay=False)), encoding='utf-8')
(game / 'override.cfg').write_text('[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n', encoding='utf-8')
base = f'http://127.0.0.1:{port}/api/v1'
out = root / branch
out.mkdir()
recording = False
desktop = TestDesktop()
results = {}
settings = None

def api(path, data=None):
    request = urllib.request.Request(base + path, data=None if data is None else json.dumps(data).encode(), headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=25) as response:
        return json.load(response)

def save(name, data):
    (out / (name + '.json')).write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')

def state():
    return api('/singleplayer?format=json')

def post(**data):
    response = api('/singleplayer', data)
    with (out / 'actions.jsonl').open('a', encoding='utf-8') as stream:
        stream.write(json.dumps([data, response], ensure_ascii=False) + '\n')
    assert response.get('status') == 'ok', (data, response)
    return response

def cmd(command):
    return post(action='run_command', command=command)

def status():
    return json.loads(cmd('star_probe status')['message'])

def until(predicate, message, seconds=20):
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        value = predicate()
        if value:
            return value
        time.sleep(.08)
    raise AssertionError(message)

def snap(name):
    save(name, state())
    with urllib.request.urlopen(base + '/screenshot', timeout=25) as response:
        (out / (name + '.png')).write_bytes(response.read())

def hand(name):
    data = json.loads(cmd('karen_check_hand')['message'])
    save(name + '-hand', data)
    assert data['modelCount'] == data['visualCount'] and not data['missing'] and not data['unexpected'] and not data['unexpectedPileIcons'], data

def stop_record(name):
    global recording
    api('/recording/stop', {})
    rec = until(lambda: (r if (r := api('/recording')).get('status') in ('completed', 'failed') else None), 'Recording did not finish', 35)
    save(name + '-recording', rec)
    assert rec['status'] == 'completed' and rec.get('audio') and rec.get('audio_source') == 'game_process', rec
    shutil.copy2(rec['output_path'], out / (name + '.mp4'))
    recording = False

try:
    env = os.environ.copy()
    env.update(APPDATA=str(dev / 'user/AppData/Roaming'), LOCALAPPDATA=str(dev / 'user/AppData/Local'), STS2_MCP_FFMPEG=shutil.which('ffmpeg'))
    process = desktop.launch([str(game / 'SlayTheSpire2.exe'), '--rendering-driver', 'vulkan', '--force-steam=off', '--log-file', str(dev / 'godot.log')], game, env)
    for _ in range(240):
        time.sleep(.25)
        try:
            log = (dev / 'godot.log').read_text(encoding='utf-8', errors='replace')
            if '[Karen STS2 MCP]' in log and '[Star Probe] Loaded' in log and "Preloading 'Common' Complete:" in log and 'continue' in state().get('options', []):
                break
        except (OSError, ValueError):
            pass
    else:
        raise AssertionError('MCP/probe/saved run unavailable')
    post(action='menu_select', option='continue')
    until(lambda: state().get('player'), 'Saved Karen run did not load', 40)
    # player is exposed while the saved room is still loading. Do not jump rooms
    # until its native combat UI and initial draw tasks have finished.
    time.sleep(3)
    settings = api('/settings')
    save('original-settings', settings)
    api('/settings', dict(fullscreen=False, window_width=1280, window_height=720, muted=False))
    for name in args.cases.split(','):
        cmd('fight WATERFALL_GIANT_BOSS')
        until(lambda: state().get('battle', {}).get('is_play_phase'), 'No player turn')
        time.sleep(.8)
        if any(r['id'] == 'KAREN_HAIRPIN_RELIC' for r in state()['player']['relics']):
            cmd('relic remove KAREN_HAIRPIN_RELIC')
        speed = name if name in ('fast', 'instant') else 'normal'
        cmd('star_probe speed ' + speed)
        shine, pile = {'normal9': (9, 'hand'), 'discard3': (3, 'discard'), 'draw1': (1, 'draw'), 'promise4': (4, 'promise'), 'high14': (14, 'hand')}.get(name, (3, 'hand'))
        cmd(f'star_probe setup {shine} {pile}' + (' multiple' if name == 'multiple' else ''))
        until(lambda: any(c['id'] == 'KAREN_STAR' for c in state()['player']['hand']), 'Fixture did not finish')
        time.sleep(.65)
        cmd('energy 99')
        if name == 'stacked':
            cmd('star_probe act2')
            time.sleep(.4)
        expected = shine + (2 if name == 'stacked' else 0)
        if name == 'lethal':
            # Combat clears models while retaining hidden native hand holders at
            # the rewards page. Keep this targeted teardown fixture to its two
            # participating cards so the combat-only hand checker remains valid.
            cmd('remove_card KAREN_DEFEND Hand')
            time.sleep(.5)
            cmd('star_probe lethal')
        snap(name + '-before')
        hand(name + '-before')
        before = status()
        save(name + '-before-probe', before)
        if name != 'multiple' and not args.no_record:
            save(name + '-start-recording', api('/recording/start', dict(fps=30, max_duration_seconds=25, width=1280, height=720, audio=True)))
            recording = True
        index = next(c['index'] for c in state()['player']['hand'] if c['id'] == 'KAREN_STAR')
        post(action='hover_card', card_index=index)
        time.sleep(.65)
        if name == 'multiple':
            response = api('/singleplayer', dict(action='play_card', card_index=index))
            save(name + '-rejected', response)
            assert response.get('status') != 'ok', response
            assert status()['live'] == 0 and not status()['plays'], status()
            post(action='unhover_card')
            results[name] = dict(passed=True, response=response)
            print(branch, name, 'PASS', flush=True)
            continue
        post(action='play_card', card_index=index)
        time.sleep(.3 if speed == 'normal' else .12)
        snap(name + '-gather')
        if name == 'cancel':
            cmd('star_probe cancel')
        time.sleep(.65 if speed == 'normal' else .2)
        snap(name + '-replay')
        def settled():
            p = status()
            s = state()
            if p['live'] == p['backLive'] == p['playPileCount'] == 0 and p['sourceDepleted'] and p['extraRewards'] > before['extraRewards']:
                return p
            if name == 'lethal' and s.get('state_type') == 'rewards':
                return p
        after = until(settled, 'Gameplay or presentation did not finish', 24)
        if name == 'lethal':
            until(lambda: state().get('state_type') == 'rewards', 'Combat did not reach rewards', 20)
            time.sleep(1)
        time.sleep(.8)
        snap(name + '-after')
        hand(name + '-after')
        save(name + '-after-probe', after)
        if name == 'lethal':
            # Beta can stop replay execution when its only target dies. The
            # visual must stop with the native game, never invent remaining hits.
            assert 0 < len(after['plays']) <= expected and after['plays'] == list(range(len(after['plays']))), after
            assert after['sourceDepleted'] and after['extraRewards'] == before['extraRewards'] + 1 and not after['hp'], after
        else:
            assert after['plays'] == list(range(expected)), after
        if name != 'lethal':
            assert after['sourceDepleted'] and after['extraRewards'] == before['extraRewards'] + 1, after
            assert sum(before['hp']) - sum(after['hp']) == expected * 9, (before, after)
        if speed == 'instant':
            assert not after['cues'], after
        elif name != 'cancel' and name != 'lethal':
            assert len(after['cues']) == 1, after
            cue = after['cues'][0]
            assert cue['played'] == cue['planned'] == expected and cue['reward'] and cue['backCorrect'], after
        assert after['live'] == after['backLive'] == 0, after
        if recording:
            stop_record(name)
        results[name] = dict(passed=True, before=before, after=after)
        save('results', results)
        print(branch, name, 'PASS', flush=True)
    save('results', results)
    save('build', {str(p.relative_to(mods / 'ShoujoKagekiAijoKaren')): hashlib.sha256(p.read_bytes()).hexdigest()
        for p in (mods / 'ShoujoKagekiAijoKaren').rglob('*') if p.is_file()})
finally:
    if recording:
        try:
            stop_record('interrupted')
        except Exception as error:
            save('recording-error', dict(error=str(error)))
    desktop.close()
    if (dev / 'godot.log').exists():
        shutil.copy2(dev / 'godot.log', out / 'godot.log')
