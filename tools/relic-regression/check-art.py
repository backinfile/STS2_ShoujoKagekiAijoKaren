"""MCP-only asset validation, isolated disposable profiles."""
import argparse
import json
import time
import urllib.request
from pathlib import Path

root = Path(__file__).resolve().parents[2]
branch = argparse.ArgumentParser()
branch.add_argument('branch', choices=['stable', 'beta'])
branch.add_argument('--revision', default='v7', choices=['v6', 'v7', 'v8'])
branch.add_argument('--focus-crown', action='store_true', help='Inspect only crown and three native style references')
args = branch.parse_args()
output = root / ('artifacts/relic-art-native-' + args.revision) / args.branch
output.mkdir(parents=True, exist_ok=True)
base = f'http://127.0.0.1:{15657 if args.branch == "stable" else 15658}/api/v1'

def request(path, body=None, binary=False):
    req = urllib.request.Request(base + path, data=None if body is None else json.dumps(body).encode(), headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=10) as r:
        data = r.read()
    return data if binary else json.loads(data)

def state(): return request('/singleplayer?format=json')
def act(action, **kwargs):
    result = request('/singleplayer', {'action': action, **kwargs})
    assert result.get('status') == 'ok', result
    return result
def cmd(command): return act('run_command', command=command)
def wait(check, label):
    for _ in range(120):
        s = state()
        if check(s): return s
        time.sleep(.25)
    raise RuntimeError(f'Timed out {label}: {s}')
def capture(name):
    time.sleep(1)
    (output / (name + '.png')).write_bytes(request('/screenshot', binary=True))
    (output / (name + '.json')).write_text(json.dumps(state(), ensure_ascii=False, indent=2), encoding='utf-8')

log = output / 'godot.log'
assert '[Karen STS2 MCP]' in log.read_text(encoding='utf-8-sig')
original_settings = request('/settings')
(output / 'original-settings.json').write_text(json.dumps(original_settings, indent=2), encoding='utf-8')
request('/settings', {'muted': True, 'fullscreen': False, 'window_width': 1280, 'window_height': 720})
wait(lambda s: s.get('state_type') == 'menu' and s.get('menu_screen') == 'main' and 'singleplayer' in s.get('options', []), 'main menu ready')
time.sleep(1.2)
act('menu_select', option='singleplayer')
s = wait(lambda s: s.get('menu_screen') in ('singleplayer', 'character_select'), 'character select')
if s.get('menu_screen') == 'singleplayer': act('menu_select', option='standard')
act('menu_select', option='KAREN')
act('menu_select', option='embark')
s = wait(lambda s: s.get('menu_screen') == 'tutorial_prompt' or s.get('state_type') == 'map', 'map')
if s.get('menu_screen') == 'tutorial_prompt': act('menu_select', option='no')
wait(lambda s: s.get('state_type') == 'map', 'map')
cmd('relic_fixture setup')
for _ in range(100):
    r = cmd('relic_fixture_status')
    if r.get('message') == 'pass:setup': break
    assert not r.get('message', '').startswith('fail:'), r
    time.sleep(.2)
else: raise RuntimeError(r)
ids = ['KAREN_WATER_BOTTLE_RELIC', 'KAREN_COOKIE_RELIC', 'KAREN_LITTLE_STAR_RELIC', 'KAREN_BASEBALL_CAT_RELIC', 'KAREN_MR_WHITE_RELIC', 'KAREN_STAR_HAIRCLIP_RELIC', 'KAREN_STAR_TIARA_RELIC']
cmd('relic add KAREN_STAR_TIARA_RELIC')
for native in ['BLOOD_VIAL', 'VAJRA', 'WHITE_STAR', 'VELVET_CHOKER']:
    cmd('relic add ' + native)
    time.sleep(.5)
time.sleep(1)
texture_check = cmd('relic_art check')
assert texture_check['message'].startswith('pass:'), texture_check
(output / 'texture-check.json').write_text(json.dumps(texture_check, indent=2), encoding='utf-8')
capture('00-topbar')
inspect_ids = (['KAREN_STAR_TIARA_RELIC'] if args.focus_crown else ids) + ['BLOOD_VIAL', 'VAJRA', 'VELVET_CHOKER']
for i, relic in enumerate(inspect_ids, 1):
    cmd('relic_art ' + relic)
    capture(f'{i:02d}-inspect-' + relic.lower())
    cmd('relic_art close')
    time.sleep(.4)
act('choose_map_node', index=0)
wait(lambda s: s.get('battle', {}).get('is_play_phase'), 'combat')
time.sleep(2.4)
capture('11-combat-topbar')
r = cmd('karen_check_hand')
hand = json.loads(r['message'])
assert hand['modelCount'] == hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand.get('unexpectedPileIcons'), r
assert all(x['valid'] for x in hand.get('burnOverlays', [])), r
(output / '11-hand-check.json').write_text(json.dumps(r, indent=2), encoding='utf-8')
summary = {'branch': args.branch, 'revision': args.revision, 'scope': 'crown appearance' if args.focus_crown else 'seven relics appearance', 'small': [85,85], 'big': [256,256], 'textures': texture_check, 'hand': hand, 'captured': len(inspect_ids)+2, 'result': 'passed'}
(output / 'result.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
restored = request('/settings', {key: original_settings[key] for key in ('muted','fullscreen','window_width','window_height')})
(output / 'restored-settings.json').write_text(json.dumps(restored, indent=2), encoding='utf-8')
assert all(restored[key] == original_settings[key] for key in ('muted','fullscreen','window_width','window_height'))
print(f'{args.branch}: native-size textures validated; {summary["captured"]} native UI captures saved; settings restored.', flush=True)
