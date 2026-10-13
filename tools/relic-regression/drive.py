"""MCP-only targeted regression driver. Start launch.ps1 first."""
import argparse
import json
import time
import urllib.request
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
parser.add_argument('phase', choices=['start', 'mechanics', 'turns', 'continue', 'finish', 'white-boundaries', 'state'])
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
output = root / 'artifacts/relic-regression' / args.branch
output.mkdir(parents=True, exist_ok=True)
port = 15657 if args.branch == 'stable' else 15658
base = f'http://127.0.0.1:{port}/api/v1'

def request(path, body=None, binary=False):
    data = None if body is None else json.dumps(body).encode('utf-8')
    req = urllib.request.Request(base + path, data=data, headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=12) as response:
        content = response.read()
    return content if binary else json.loads(content)

def state(): return request('/singleplayer?format=json')
def act(action, **kwargs):
    result = request('/singleplayer', {'action': action, **kwargs})
    if result.get('status') != 'ok': raise RuntimeError(result)
    return result
def cmd(command): return act('run_command', command=command)
def wait(predicate, label):
    deadline = time.monotonic() + 30
    while time.monotonic() < deadline:
        s = state()
        if predicate(s): return s
        time.sleep(.25)
    raise RuntimeError(f'Timed out {label}: {json.dumps(s, ensure_ascii=False)}')
def combat(s): return s.get('battle', {}).get('is_play_phase')
def fixture(name):
    cmd('relic_fixture ' + name)
    for _ in range(100):
        response = cmd('relic_fixture_status')
        message = response.get('message', '')
        if 'pass:' + name in message:
            (output / (name + '-result.json')).write_text(json.dumps(response, ensure_ascii=False, indent=2), encoding='utf-8')
            print(message, flush=True)
            return
        if 'fail:' in message: raise RuntimeError(response)
        time.sleep(.25)
    raise RuntimeError(response)
def capture(name, hand=True):
    time.sleep(1.6)
    (output / (name + '.json')).write_text(json.dumps(state(), ensure_ascii=False, indent=2), encoding='utf-8')
    (output / (name + '.png')).write_bytes(request('/screenshot', binary=True))
    if hand:
        result = cmd('karen_check_hand')
        (output / (name + '-hand.json')).write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
        print(name + ': ' + result.get('message', ''), flush=True)

if args.phase == 'start':
    log = root / f'artifacts/dev-game/{args.branch}/godot.log'
    assert '[Karen STS2 MCP]' in log.read_text(encoding='utf-8-sig'), 'MCP loading marker missing'
    wait(lambda s: s.get('menu_screen') == 'main', 'main menu')
    act('menu_select', option='singleplayer')
    s = wait(lambda s: s.get('menu_screen') in ('singleplayer', 'character_select'), 'character select')
    if s.get('menu_screen') == 'singleplayer': act('menu_select', option='standard')
    act('menu_select', option='KAREN')
    act('menu_select', option='embark')
    s = wait(lambda s: s.get('menu_screen') == 'tutorial_prompt' or s.get('state_type') == 'map', 'map or tutorial')
    if s.get('menu_screen') == 'tutorial_prompt': act('menu_select', option='no')
    wait(lambda s: s.get('state_type') == 'map', 'map')
    fixture('setup')
    capture('00-before-combat', hand=False)
    act('choose_map_node', index=0)
    wait(combat, 'combat')
    capture('01-start')
    fixture('start')
elif args.phase == 'mechanics':
    capture('02-before-tower')
    fixture('tower')
    capture('03-after-tower')
    fixture('cat')
    capture('04-after-cat')
elif args.phase == 'turns':
    capture('05-before-white')
    act('end_turn')
    s = wait(lambda s: s.get('state_type') == 'hand_select', 'Mr. White selection')
    (output / 'white-selection.json').write_text(json.dumps(s, ensure_ascii=False, indent=2), encoding='utf-8')
    print('Mr. White selection: ' + str(len(s['hand_select']['cards'])) + ' cards; zero selection allowed', flush=True)
elif args.phase == 'finish':
    fixture('finisher')
    wait(lambda s: s.get('state_type') == 'rewards', 'final rewards')
    capture('12-finishing-rewards', hand=False)
elif args.phase == 'continue':
    original = json.loads((output / 'white-selection.json').read_text(encoding='utf-8'))
    if len(state()['hand_select']['cards']) == len(original['hand_select']['cards']):
        act('combat_select_card', card_index=0)
    act('combat_select_card', card_index=len(state()['hand_select']['cards']) - 1)
    act('combat_confirm_selection')
    wait(lambda s: combat(s) and s['battle']['round'] == 2, 'second turn')
    capture('06-after-white')
    fixture('water2')
    for turn in (3, 4):
        act('end_turn')
        wait(lambda s: combat(s) and s['battle']['round'] == turn, 'turn ' + str(turn))
        fixture('water' + str(turn))
    fixture('tiara')
    act('end_turn')
    s = wait(lambda s: combat(s) and s['battle']['round'] == 5, 'tiara turn')
    assert s['player']['energy'] == 4, 'tiara must grant energy when it disables itself'
    assert any(r['id'] == 'KAREN_LOCK_RELIC' for r in s['player']['relics']), 'tiara not locked'
    capture('08-tiara-self-disabled')
    act('end_turn')
    s = wait(lambda s: combat(s) and s['battle']['round'] == 6, 'disabled tiara next turn')
    assert s['player']['energy'] == 3, 'disabled tiara must not activate again'
    cmd('kill')
    wait(lambda s: s.get('state_type') == 'rewards', 'rewards')
    capture('09-restored-relics', hand=False)
    cmd('fight NIBBITS_WEAK')
    wait(lambda s: combat(s) and s['battle']['round'] == 1, 'second combat')
    fixture('reset')
    capture('10-second-combat')
elif args.phase == 'white-boundaries':
    for mode in ('none', 'all'):
        cmd('fight NIBBITS_WEAK')
        wait(lambda s: combat(s) and s['battle']['round'] == 1, 'boundary combat')
        capture('white-' + mode + '-before')
        act('end_turn')
        s = wait(lambda s: s.get('state_type') == 'hand_select', 'boundary selection')
        count = len(s['hand_select']['cards']) if mode == 'all' else 0
        for _ in range(count): act('combat_select_card', card_index=0)
        if state().get('state_type') == 'hand_select': act('combat_confirm_selection')
        wait(lambda s: combat(s) and s['battle']['round'] == 2, 'boundary second turn')
        response = cmd('karen_check_promise_pile KAREN_WE_KOWN_TOKEN')
        contents = json.loads(response['message'])
        assert len(contents['cards']) == count + 1, contents
        (output / ('white-' + mode + '-result.json')).write_text(json.dumps(contents, indent=2), encoding='utf-8')
        capture('white-' + mode + '-after')
        cmd('kill all')
        wait(lambda s: s.get('state_type') == 'rewards', 'boundary rewards')
    cmd('relic add STRAWBERRY')
    cmd('fight NIBBITS_WEAK')
    s = wait(lambda s: combat(s) and s['battle']['round'] == 1, 'tiara with neighbor')
    assert s['player']['energy'] == 4
    assert any(r['id'] == 'KAREN_STAR_TIARA_RELIC' for r in s['player']['relics'])
    assert not any(r['id'] == 'STRAWBERRY' for r in s['player']['relics'])
    capture('tiara-neighbor-first')
    act('end_turn')
    wait(lambda s: s.get('state_type') == 'hand_select', 'tiara neighbor selection')
    act('combat_confirm_selection')
    s = wait(lambda s: combat(s) and s['battle']['round'] == 2, 'tiara with neighbor turn 2')
    assert s['player']['energy'] == 4
    assert not any(r['id'] == 'KAREN_STAR_TIARA_RELIC' for r in s['player']['relics'])
    capture('tiara-neighbor-second')
    cmd('kill all')
    wait(lambda s: s.get('state_type') == 'rewards', 'tiara neighbor rewards')
    print('pass:white-none; pass:white-all; pass:tiara-neighbor', flush=True)
else:
    print(json.dumps(state(), ensure_ascii=False, indent=2))
