"""Exercise OBS-032 through real card play, hand UI and the native synchronizer."""
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
parser.add_argument('--label', required=True)
parser.add_argument('--vanilla', action='store_true')
parser.add_argument('--card', choices=['rapid', 'native'], default='rapid')
parser.add_argument('--transition', choices=['none', 'replace', 'leave'], default='none')
args = parser.parse_args()
project = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(project / 'tools/vfx/six-card'))
from isolated_desktop import TestDesktop

dev = project / 'artifacts/dev-game' / args.branch
game = dev / 'game'
if args.vanilla:
    assert args.card == 'native', '--vanilla requires --card native'
    native_game = project / 'artifacts/rapid-choice-native' / args.branch / 'game'
    native_game.mkdir(parents=True, exist_ok=True)
    for source in game.rglob('*'):
        relative = source.relative_to(game)
        if relative.parts[0] == 'mods' or not source.is_file():
            continue
        target = native_game / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        if not target.exists():
            os.link(source, target)
    native_mods = native_game / 'mods'
    native_mods.mkdir(exist_ok=True)
    for name in ('KarenSTS2MCP.dll', 'KarenSTS2MCP.json'):
        shutil.copy2(game / 'mods' / name, native_mods)
    game = native_game
output = project / 'artifacts/rapid-choice-regression' / args.branch / args.label
output.mkdir(parents=True, exist_ok=False)
port = 18937 if args.branch == 'stable' else 18938
with socket.socket() as check:
    assert check.connect_ex(('127.0.0.1', port)) != 0, 'Test port occupied'
mods = game / 'mods'
fixture = mods / 'KarenRapidChoiceRegression'
fixture.mkdir(exist_ok=True)
shutil.copy2(project / 'artifacts/rapid-choice-fixture' / args.branch.title() / 'KarenRapidChoiceRegression.dll', fixture)
manifest = dict(id='KarenRapidChoiceRegression', name='Rapid choice regression', author='Karen tests', version='1.0.0', has_dll=True, has_pck=False, affects_gameplay=True, min_game_version='v0.107.1')
(fixture / 'KarenRapidChoiceRegression.json').write_text(json.dumps(manifest), encoding='utf-8')
(mods / 'KarenSTS2MCP.conf').write_text(json.dumps(dict(port=port, fullscreen=False, muted=True, silent_test=True, skip_tutorial=True, skip_first_prompt=True, window_width=1280, window_height=720)), encoding='utf-8')
profile_id = 9137
roaming = output / 'user/AppData/Roaming'
settings_path = roaming / f'SlayTheSpire2/default/{profile_id}/settings.save'
settings_path.parent.mkdir(parents=True)
settings = json.loads((dev / 'user/AppData/Roaming/SlayTheSpire2/default/1/settings.save').read_text(encoding='utf-8-sig'))
settings.update(fullscreen=False, limit_fps_in_background=False)
settings_path.write_text(json.dumps(settings), encoding='utf-8')
config = roaming / 'SlayTheSpire2/mod_configs/ShoujoKagekiAijoCore.cfg'
config.parent.mkdir(parents=True, exist_ok=True)
config.write_text(json.dumps(dict(EnableDataUpload='False', EnableErrorUpload='False')), encoding='utf-8')
env = os.environ.copy()
env['APPDATA'] = str(roaming)
env['LOCALAPPDATA'] = str(output / 'user/AppData/Local')
base = f'http://127.0.0.1:{port}/api/v1'
log = output / 'godot.log'
result = dict(card=args.card, vanilla=args.vanilla, transition=args.transition, result='fail')


def api(path, data=None):
    request = urllib.request.Request(base + path, data=None if data is None else json.dumps(data).encode(), headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=20) as response:
        return json.load(response)


def state():
    return api('/singleplayer?format=json')


def act(action, **values):
    response = api('/singleplayer', dict(action=action, **values))
    assert response.get('status') == 'ok', response
    return response


def cmd(command):
    return act('run_command', command=command)


def wait(condition, label, seconds=45):
    end = time.monotonic() + seconds
    last = None
    while time.monotonic() < end:
        try:
            last = state()
            if condition(last):
                return last
        except (OSError, TimeoutError):
            pass
        time.sleep(.15)
    raise RuntimeError('Timeout ' + label + ' ' + json.dumps(last))


def capture(name):
    time.sleep(.8)
    (output / (name + '.json')).write_text(json.dumps(state(), ensure_ascii=False, indent=2), encoding='utf-8')
    with urllib.request.urlopen(base + '/screenshot', timeout=20) as response:
        (output / (name + '.png')).write_bytes(response.read())


def hand_check(name):
    check = json.loads(cmd('rapid_fixture hand' if args.vanilla else 'karen_check_hand')['message'])
    (output / (name + '-hand.json')).write_text(json.dumps(check, indent=2), encoding='utf-8')
    assert check['modelCount'] == check['visualCount'], check
    assert not any(check.get(key) for key in ('missing', 'unexpected', 'unexpectedPileIcons')), check
    return check


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
    api('/settings', dict(fullscreen=False, window_width=1280, window_height=720, muted=True))
    act('menu_select', option='singleplayer')
    current = wait(lambda s: s.get('menu_screen') in ('singleplayer', 'character_select'), 'character select')
    if current.get('menu_screen') == 'singleplayer':
        act('menu_select', option='standard')
    act('menu_select', option='IRONCLAD' if args.vanilla else 'KAREN')
    act('menu_select', option='embark')
    current = wait(lambda s: s.get('state_type') == 'map' or s.get('menu_screen') == 'tutorial_prompt', 'run')
    if current.get('menu_screen') == 'tutorial_prompt':
        act('menu_select', option='no')
    wait(lambda s: s.get('state_type') == 'map', 'map')
    act('choose_map_node', index=0)
    wait(lambda s: s.get('battle', {}).get('is_play_phase'), 'combat')
    card_id = 'KAREN_RAPID' if args.card == 'rapid' else 'SURVIVOR'
    cmd('card ' + card_id + ' Hand')
    current = wait(lambda s: any(c['id'] == card_id for c in s.get('player', {}).get('hand', [])), 'fixture card')
    time.sleep(.8)
    capture('before')
    result['beforeHand'] = hand_check('before')['modelCount']
    result['beforeSync'] = cmd('rapid_fixture status')['message']
    marker = len(log.read_text(encoding='utf-8', errors='replace'))
    card = next(c for c in current['player']['hand'] if c['id'] == card_id)
    act('play_card', card_index=card['index'])
    if args.card == 'rapid':
        current = wait(lambda s: s.get('state_type') == 'card_select', 'rapid option')
        (output / 'option.json').write_text(json.dumps(current, ensure_ascii=False, indent=2), encoding='utf-8')
        cards = current['card_select']['cards']
        option = next(c for c in cards if c['id'] == 'KAREN_RAPID_PUT_OPTION')
        time.sleep(.8)
        act('select_card', index=option['index'])
    current = wait(lambda s: s.get('state_type') == 'hand_select', 'hand choice')
    capture('selection')
    result['pendingSync'] = cmd('rapid_fixture status')['message']
    if args.transition == 'leave':
        cmd('rapid_fixture leave')
        wait(lambda s: s.get('menu_screen') == 'main', 'return to main')
        time.sleep(1)
    else:
        if args.transition == 'replace':
            result['replacement'] = cmd('rapid_fixture replace')['message']
        count = 2 if args.card == 'rapid' else 1
        # Card indices can shift when cards enter the selected region.
        for _ in range(count):
            current = state()
            selected = next(c for c in current['hand_select']['cards'] if not c.get('selected', False))
            act('combat_select_card', card_index=selected['index'])
        if state().get('state_type') == 'hand_select':
            act('combat_confirm_selection')
        time.sleep(2)
        if args.transition == 'replace':
            cmd('rapid_fixture restore')
        else:
            wait(lambda s: s.get('battle', {}).get('is_play_phase'), 'play completes')
            capture('after')
            result['afterHand'] = hand_check('after')['modelCount']
            result['afterSync'] = cmd('rapid_fixture status')['message']
            expected_removed = 3 if args.card == 'rapid' else 2
            assert result['afterHand'] == result['beforeHand'] - expected_removed, result
    segment = log.read_text(encoding='utf-8', errors='replace')[marker:]
    (output / 'action.log').write_text(segment, encoding='utf-8')
    result['syncTrace'] = [line for line in segment.splitlines() if '[OBS032]' in line]
    result['exactError'] = 'System.ArgumentOutOfRangeException' in segment and 'PlayerChoiceSynchronizer.GetChoiceId' in segment
    assert not result['exactError'], 'OBS-032: ArgumentOutOfRangeException in PlayerChoiceSynchronizer.GetChoiceId'
    result['otherErrors'] = [line for line in segment.splitlines() if line.startswith(('ERROR:', '[ERROR]'))]
    assert not any(line.startswith('[ERROR]') for line in result['otherErrors']), segment[-3000:]
    result['result'] = 'pass'
except Exception as error:
    result['error'] = str(error)
    raise
finally:
    (output / 'result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False), flush=True)
    desktop.close()
