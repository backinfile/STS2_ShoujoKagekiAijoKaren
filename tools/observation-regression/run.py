"""Run only the three observation regressions in an isolated, muted development game."""
import argparse
import json
import os
from pathlib import Path
import shutil
import socket
import sys
import time
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
parser.add_argument('--label', default='current')
args = parser.parse_args()
project = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(project / 'tools/vfx/six-card'))
from isolated_desktop import TestDesktop

dev = project / 'artifacts/dev-game' / args.branch
game = dev / 'game'
output = project / 'artifacts/observation-regression' / args.branch / args.label
output.mkdir(parents=True, exist_ok=False)
port = 18857 if args.branch == 'stable' else 18858
with socket.socket() as check:
    assert check.connect_ex(('127.0.0.1', port)) != 0, 'Test port in use'
mods = game / 'mods'
fixture = mods / 'KarenObservationRegression'
fixture.mkdir(exist_ok=True)
shutil.copy2(project / 'artifacts/observation-fixture' / args.branch.title() / 'KarenObservationRegression.dll', fixture)
manifest = dict(id='KarenObservationRegression', name='Observation regression', author='Karen tests', version='1.0.0', has_dll=True, has_pck=False, affects_gameplay=True, min_game_version='v0.107.1', dependencies=[dict(id='ShoujoKagekiAijoKaren')])
(fixture / 'KarenObservationRegression.json').write_text(json.dumps(manifest), encoding='utf-8')
(mods / 'KarenSTS2MCP.conf').write_text(json.dumps(dict(port=port, fullscreen=False, muted=True, silent_test=True, skip_tutorial=True, skip_first_prompt=True, window_width=1280, window_height=720)), encoding='utf-8')
profile_id = 9136
roaming = output / 'user/AppData/Roaming'
settings_path = roaming / f'SlayTheSpire2/default/{profile_id}/settings.save'
settings_path.parent.mkdir(parents=True)
settings = json.loads((dev / 'user/AppData/Roaming/SlayTheSpire2/default/1/settings.save').read_text(encoding='utf-8-sig'))
settings.update(fullscreen=False, limit_fps_in_background=False)
settings_path.write_text(json.dumps(settings), encoding='utf-8')
env = os.environ.copy()
env['KAREN_OBSERVATION_UI'] = '1'
env['APPDATA'] = str(roaming)
env['LOCALAPPDATA'] = str(output / 'user/AppData/Local')
base = f'http://127.0.0.1:{port}/api/v1'
log = output / 'godot.log'
results = []

def api(path, data=None):
    request = urllib.request.Request(base + path, data=None if data is None else json.dumps(data).encode(), headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=15) as response:
        return json.load(response)

def state():
    return api('/singleplayer?format=json')

def act(action, **values):
    result = api('/singleplayer', dict(action=action, **values))
    assert result.get('status') == 'ok', result
    return result

def cmd(command):
    return act('run_command', command=command)

def wait(condition, label):
    end = time.monotonic() + 60
    while time.monotonic() < end:
        try:
            current = state()
            if condition(current):
                return current
        except (OSError, TimeoutError):
            pass
        time.sleep(.25)
    raise RuntimeError('Timeout ' + label + ' ' + json.dumps(state()))

def test(name):
    before = log.read_text(encoding='utf-8', errors='replace')
    cmd('observation_fixture ' + name)
    end = time.monotonic() + 15
    while time.monotonic() < end:
        status = cmd('observation_fixture status').get('message', '')
        if status.startswith(('pass:', 'fail:')):
            break
        time.sleep(.2)
    else:
        raise RuntimeError('Fixture timeout ' + name)
    after = log.read_text(encoding='utf-8', errors='replace')[len(before):]
    if name == 'locked' and 'Failed to find Karen character select background host node' in after:
        status = 'fail:locked:Failed to find Karen character select background host node'
    results.append(dict(case=name, result='pass' if status.startswith('pass:') else 'fail', detail=status))
    print(status, flush=True)

desktop = TestDesktop()
try:
    process = desktop.launch([game / 'SlayTheSpire2.exe', '--rendering-driver', 'vulkan', '--force-steam=off', f'--clientId={profile_id}', '--log-file', log], game, env)
    end = time.monotonic() + 60
    while time.monotonic() < end:
        if log.exists() and '[Karen STS2 MCP]' in log.read_text(encoding='utf-8', errors='replace'):
            break
        assert process.poll() is None, 'Game exited before MCP loaded'
        time.sleep(.25)
    else:
        raise RuntimeError('Karen MCP not loaded')
    wait(lambda s: s.get('menu_screen') == 'main' and 'singleplayer' in s.get('options', []), 'main')
    act('menu_select', option='singleplayer')
    current = wait(lambda s: s.get('menu_screen') in ('singleplayer', 'character_select'), 'character select')
    if current.get('menu_screen') == 'singleplayer':
        act('menu_select', option='standard')
    act('menu_select', option='KAREN')
    ui_result = roaming / 'SlayTheSpire2/observation-ui.json'
    end = time.monotonic() + 20
    while not ui_result.exists() and time.monotonic() < end:
        time.sleep(.2)
    ui_status = json.loads(ui_result.read_text(encoding='utf-8'))
    text = log.read_text(encoding='utf-8', errors='replace')
    locked_log = text.split('[Observation] LOCKED BEGIN')[-1].split('[Observation] LOCKED END')[0]
    if 'Failed to find Karen character select background host node' in locked_log:
        ui_status[0] = 'fail:locked:Failed to find Karen character select background host node'
    for name, status in zip(('locked', 'unlocked'), ui_status):
        results.append(dict(case=name, result='pass' if status.startswith('pass:') else 'fail', detail=status))
        print(status, flush=True)
    act('menu_select', option='embark')
    current = wait(lambda s: s.get('state_type') == 'map' or s.get('menu_screen') == 'tutorial_prompt', 'run')
    if current.get('menu_screen') == 'tutorial_prompt':
        act('menu_select', option='no')
    wait(lambda s: s.get('state_type') == 'map', 'map')
    act('choose_map_node', index=0)
    wait(lambda s: s.get('battle', {}).get('is_play_phase'), 'combat')
    test('canonical')
    test('flower')
finally:
    (output / 'results.json').write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding='utf-8')
    desktop.close()
if any(item['result'] != 'pass' for item in results):
    sys.exit(1)
