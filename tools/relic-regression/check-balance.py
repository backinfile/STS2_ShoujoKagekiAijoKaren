"""MCP-only targeted Little Star / one-turn Water Bottle checks; no full matrix."""
import argparse
import json
import time
import urllib.request
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
output = root / 'artifacts/relic-balance-20261012' / args.branch
base = f'http://127.0.0.1:{15657 if args.branch == "stable" else 15658}/api/v1'
output.mkdir(parents=True, exist_ok=True)
results = []

def request(path, body=None, binary=False):
    req = urllib.request.Request(base + path, data=None if body is None else json.dumps(body).encode(),
                                 headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=12) as response:
        value = response.read()
    return value if binary else json.loads(value)

def save(name, value):
    (output / (name + '.json')).write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')

def state(): return request('/singleplayer?format=json')
def act(action, **kwargs):
    value = request('/singleplayer', {'action': action, **kwargs})
    assert value.get('status') == 'ok', value
    return value
def cmd(command): return act('run_command', command=command)
def power(s, name): return next((x['amount'] for x in s['player']['status'] if x['id'] == name), 0)
def combat(s): return s.get('battle', {}).get('is_play_phase')
def wait(predicate, label):
    deadline = time.monotonic() + 35
    while time.monotonic() < deadline:
        s = state()
        if predicate(s): return s
        time.sleep(.25)
    raise RuntimeError(f'Timed out {label}: {s}')
def fixture(name, label=None):
    cmd('relic_fixture ' + name)
    for _ in range(140):
        r = cmd('relic_fixture_status')
        if r.get('message', '').startswith('pass:' + name):
            save((label or name) + '-fixture', r)
            return
        assert not r.get('message', '').startswith('fail:'), r
        time.sleep(.25)
    raise RuntimeError(r)
def check(label, condition, details=None):
    assert condition, (label, details)
    results.append({'case': label, 'result': 'passed', 'details': details})
    save('results-in-progress', results)
    print(args.branch + ': pass:' + label, flush=True)
def capture(label):
    time.sleep(1.7)
    save(label, state())
    (output / (label + '.png')).write_bytes(request('/screenshot', binary=True))
    r = cmd('karen_check_hand')
    save(label + '-hand', r)
    hand = json.loads(r['message'])
    assert hand['modelCount'] == hand['visualCount'] and not hand['missing'] and not hand['unexpected'], hand
    assert not hand.get('unexpectedPileIcons') and all(x['valid'] for x in hand.get('burnOverlays', [])), hand
def tower(label, mode='balance-prepare'):
    fixture(mode, label + '-prepare')
    capture(label + '-before')
    before = state()
    amount = power(before, 'KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER')
    cards = [c for c in before['player']['hand'] if c['id'] == 'KAREN_TOWER_OF_PROMISE']
    act('play_card', card_index=cards[-1]['index'])
    delta = 10 if mode == 'balance-replay' else 5
    s = wait(lambda s: power(s, 'KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER') == amount + delta, label)
    fixture('balance-check', label + '-check')
    check(label, s['player']['block'] == before['player']['block'] and
          power(s, 'STRENGTH_POWER') == power(before, 'STRENGTH_POWER') + delta,
          {'temporary_strength_delta': delta, 'block_delta': s['player']['block'] - before['player']['block']})
    capture(label + '-after')
    return state()
def end_to(turn):
    act('end_turn')
    return wait(lambda s: combat(s) and s['battle']['round'] == turn, f'turn {turn}')
def fight():
    cmd('kill all')
    wait(lambda s: s.get('state_type') == 'rewards', 'rewards')
    cmd('fight NIBBITS_WEAK')
    wait(lambda s: combat(s) and s['battle']['round'] == 1, 'new combat')
    fixture('buffer')

assert '[Karen STS2 MCP]' in (output / 'godot.log').read_text(encoding='utf-8-sig')
original = request('/settings')
save('original-settings', original)
try:
    request('/settings', {'muted': True, 'fullscreen': False, 'window_width': 1280, 'window_height': 720})
    wait(lambda s: s.get('menu_screen') == 'main' and 'singleplayer' in s.get('options', []), 'main menu')
    act('menu_select', option='singleplayer')
    s = wait(lambda s: s.get('menu_screen') in ('singleplayer', 'character_select'), 'character select')
    if s.get('menu_screen') == 'singleplayer': act('menu_select', option='standard')
    act('menu_select', option='KAREN')
    act('menu_select', option='embark')
    s = wait(lambda s: s.get('menu_screen') == 'tutorial_prompt' or s.get('state_type') == 'map', 'map')
    if s.get('menu_screen') == 'tutorial_prompt': act('menu_select', option='no')
    wait(lambda s: s.get('state_type') == 'map', 'map')
    fixture('balance-setup')
    act('choose_map_node', index=0)
    wait(combat, 'combat')
    fixture('buffer')
    tower('01-normal-tower')
    tower('02-upgraded-tower', 'balance-upgrade')
    tower('03-replayed-tower', 'balance-replay')
    capture('04-other-card-before')
    fixture('balance-ordinary')
    capture('04-other-card-after')
    check('other-card-does-not-trigger', power(state(), 'KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER') == 20)
    s = end_to(2)
    check('expires-without-bottle', power(s, 'KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER') == 0 and power(s, 'STRENGTH_POWER') == 0)
    capture('05-without-bottle-expired')
    cmd('relic add KAREN_WATER_BOTTLE_RELIC')
    fight()
    fixture('balance-reset')
    fixture('balance-negative')
    check('bottle-ignores-permanent-and-negative-strength', power(state(), 'KAREN_RETAIN_TMP_STRENGTH_POWER') == 0)
    s = tower('06-bottle-first-trigger')
    check('bottle-grants-one-retention', power(s, 'KAREN_RETAIN_TMP_STRENGTH_POWER') == 1)
    s = end_to(2)
    check('retained-through-one-turn-end', power(s, 'KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER') == 5 and
          power(s, 'KAREN_RETAIN_TMP_STRENGTH_POWER') == 0)
    capture('07-retained-next-turn')
    s = tower('08-bottle-no-retrigger')
    check('bottle-once-per-combat', power(s, 'KAREN_RETAIN_TMP_STRENGTH_POWER') == 0)
    s = end_to(3)
    check('expires-at-second-turn-end', power(s, 'KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER') == 0 and power(s, 'STRENGTH_POWER') == 1)
    capture('09-bottle-expired')
    fight()
    fixture('balance-reset')
    s = tower('10-next-combat')
    check('bottle-resets-next-combat', power(s, 'KAREN_RETAIN_TMP_STRENGTH_POWER') == 1)
    for name in ('KAREN_LITTLE_STAR_RELIC', 'KAREN_WATER_BOTTLE_RELIC'):
        cmd('relic_art ' + name)
        time.sleep(1)
        (output / ('inspect-' + name.lower() + '.png')).write_bytes(request('/screenshot', binary=True))
        cmd('relic_art close')
    save('result', {'branch': args.branch, 'suite': 'targeted balance', 'result': 'passed', 'cases': results,
                    'full_matrix': 'not run per user request', 'screenshots': len(list(output.glob('*.png')))})
finally:
    keys = ('muted', 'fullscreen', 'window_width', 'window_height')
    restored = request('/settings', {k: original[k] for k in keys})
    save('restored-settings', restored)
    current = request('/settings')
    check('settings-restored', all(current[k] == original[k] for k in keys))
