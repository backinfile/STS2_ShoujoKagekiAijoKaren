"""Record actual relic triggers through KarenSTS2MCP, one segment per phase."""
import argparse
import json
import shutil
import time
import urllib.request
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('phase', choices=['start', 'cookie', 'hairclip', 'water', 'star', 'white', 'cat', 'tiara'])
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
output = root / 'artifacts/relic-video'
output.mkdir(parents=True, exist_ok=True)
base = 'http://127.0.0.1:15657/api/v1'

def request(path, body=None, binary=False):
    data = None if body is None else json.dumps(body).encode('utf-8')
    req = urllib.request.Request(base + path, data=data, headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=15) as response:
        data = response.read()
    return data if binary else json.loads(data)

def state(): return request('/singleplayer?format=json')
def act(action, **kwargs):
    r = request('/singleplayer', {'action': action, **kwargs})
    if r.get('status') != 'ok': raise RuntimeError(r)
    return r
def cmd(command): return act('run_command', command=command)
def wait_fixture(name):
    end = time.monotonic() + 20
    while time.monotonic() < end:
        r = cmd('relic_fixture_status')
        if r.get('message') == 'pass:' + name: return r
        if r.get('message', '').startswith('fail:'): raise RuntimeError(r)
        time.sleep(.2)
    raise RuntimeError(f'Timed out fixture {name}: {r}')
def wait(predicate, label):
    end = time.monotonic() + 35
    while time.monotonic() < end:
        s = state()
        if predicate(s): return s
        time.sleep(.25)
    raise RuntimeError(f'Timed out {label}: {s}')
def combat(s): return s.get('battle', {}).get('is_play_phase')
def power(s, name): return next((x['amount'] for x in s['player']['status'] if x['id'] == name), 0)
def play(name, last=False):
    cards = [x for x in state()['player']['hand'] if x['id'] == name and x['can_play']]
    if not cards: raise RuntimeError('No playable ' + name)
    card = cards[-1] if last else cards[0]
    body = {'card_index': card['index']}
    if card['target_type'] == 'AnyEnemy': body['target'] = state()['battle']['enemies'][0]['entity_id']
    return act('play_card', **body)
def capture(label, hand=True):
    time.sleep(1.5)
    (output / (label + '.json')).write_text(json.dumps(state(), ensure_ascii=False, indent=2), encoding='utf-8')
    (output / (label + '.png')).write_bytes(request('/screenshot', binary=True))
    if hand:
        r = cmd('karen_check_hand')
        (output / (label + '-hand.json')).write_text(json.dumps(r, ensure_ascii=False, indent=2), encoding='utf-8')

def copy_recording(meta, label):
    # Windows packaged apps may virtualize the LocalApplicationData recordings folder.
    # Copy only the exact video returned by this task's MCP recording operation.
    source = Path(meta['output_path']).resolve()
    assert source.parent.name == 'recordings' and source.parent.parent.name == 'KarenSTS2MCP', source
    assert source.suffix == '.mp4' and source.name.startswith('sts2-'), source
    shutil.copy2(source, output / (label + '.mp4'))
def record(number, name, action):
    label = f'{number:02d}-{name}'
    request('/recording/start', {'fps':30, 'max_duration_seconds':50, 'width':1280, 'height':720, 'audio':True})
    error = None
    try:
        time.sleep(1.8)
        action()
        time.sleep(3)
    except Exception as ex:
        error = ex
    finally:
        request('/recording/stop', {})
    for _ in range(100):
        meta = request('/recording')
        if meta['status'] in ('completed', 'failed'): break
        time.sleep(.25)
    meta['balance_revision'] = '2026-10-12'
    (output / (label + '-recording.json')).write_text(json.dumps(meta, indent=2), encoding='utf-8')
    if meta['status'] != 'completed' or meta.get('error'): raise RuntimeError(meta)
    assert meta['audio'] and meta['audio_source'] == 'game_process', meta
    copy_recording(meta, label)
    if error: raise error
    print(f'{label}: {meta["video_duration_seconds"]:.1f}s, game audio, {meta["frames_captured"]} captured frames', flush=True)

if args.phase == 'start':
    wait(lambda s: s.get('menu_screen') == 'main', 'main menu')
    original = request('/settings')
    (output / 'original-settings.json').write_text(json.dumps(original, indent=2), encoding='utf-8')
    request('/settings', {'fullscreen':False, 'window_width':1280, 'window_height':720, 'muted':False})
    act('menu_select', option='singleplayer')
    s = wait(lambda s: s.get('menu_screen') in ('singleplayer','character_select'), 'character selection')
    if s.get('menu_screen') == 'singleplayer': act('menu_select', option='standard')
    act('menu_select', option='KAREN')
    act('menu_select', option='embark')
    s = wait(lambda s: s.get('menu_screen') == 'tutorial_prompt' or s.get('state_type') == 'map', 'map')
    if s.get('menu_screen') == 'tutorial_prompt': act('menu_select', option='no')
    wait(lambda s: s.get('state_type') == 'map', 'map')
    capture('00-map', False)
elif args.phase == 'cookie':
    def action():
        cmd('relic_fixture video-cookie')
        wait_fixture('video-cookie')
        capture('01-cookie-after', False)
    record(1, 'cookie', action)
elif args.phase == 'hairclip':
    cmd('relic add KAREN_STAR_HAIRCLIP_RELIC')
    time.sleep(1)
    def action():
        act('choose_map_node', index=0)
        wait(combat, 'combat start')
        time.sleep(1)
        r = cmd('karen_check_promise_pile KAREN_WE_KOWN_TOKEN')
        assert len(json.loads(r['message'])['cards']) == 1, r
        capture('02-hairclip-after')
    record(2, 'hairclip', action)
elif args.phase == 'water':
    cmd('relic add KAREN_WATER_BOTTLE_RELIC')
    cmd('card KAREN_BANANA_MUFFIN Hand')
    time.sleep(1)
    capture('03-water-before')
    def action():
        play('KAREN_BANANA_MUFFIN', True)
        wait(lambda s: power(s,'KAREN_RETAIN_TMP_STRENGTH_POWER') == 1, 'retain one')
        time.sleep(2)
        act('end_turn')
        s = wait(lambda s: combat(s) and s['battle']['round'] == 2, 'water turn 2')
        assert power(s,'KAREN_BANANA_MUFFIN_TEMP_STRENGTH_POWER') == 3, s['player']['status']
        capture('03-water-after')
    record(3, 'water', action)
elif args.phase == 'star':
    cmd('relic add KAREN_LITTLE_STAR_RELIC')
    time.sleep(1)
    capture('04-star-before')
    strength = power(state(), 'KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER')
    def action():
        play('KAREN_TOWER_OF_PROMISE')
        wait(lambda s: power(s, 'KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER') == strength + 3, 'three temporary Strength')
        capture('04-star-after')
    record(4, 'star', action)
elif args.phase == 'white':
    cmd('kill all')
    wait(lambda s: s.get('state_type') == 'rewards', 'previous battle end')
    cmd('relic add KAREN_MR_WHITE_RELIC')
    cmd('fight NIBBITS_WEAK')
    wait(lambda s: combat(s) and s['battle']['round'] == 1, 'new first turn')
    capture('05-white-before')
    def action():
        act('end_turn')
        wait(lambda s: s.get('state_type') == 'hand_select', 'Mr. White selection')
        time.sleep(1.8)
        act('combat_select_card', card_index=0)
        time.sleep(.6)
        act('combat_select_card', card_index=0)
        time.sleep(.6)
        act('combat_confirm_selection')
        wait(lambda s: combat(s) and s['battle']['round'] == 2, 'Mr. White next turn')
        r = cmd('karen_check_promise_pile KAREN_WE_KOWN_TOKEN')
        assert len(json.loads(r['message'])['cards']) == 3, r
        capture('05-white-after')
    record(5, 'white', action)
elif args.phase == 'cat':
    cmd('relic add KAREN_BASEBALL_CAT_RELIC')
    cmd('relic_fixture video-finisher')
    wait_fixture('video-finisher')
    wait(lambda s: any(x['id']=='KAREN_SHINE_STRIKE' for x in s['player']['hand']) and s['battle']['enemies'][0]['hp']==1, 'finishing card prepared')
    capture('06-cat-before')
    def action():
        play('KAREN_SHINE_STRIKE', True)
        s = wait(lambda s: s.get('state_type') == 'rewards', 'finishing attack rewards')
        assert sum(x['type']=='special_card' for x in s['rewards']['items']) == 1, s['rewards']
        capture('06-cat-after', False)
    record(6, 'cat', action)
elif args.phase == 'tiara':
    cmd('relic remove KAREN_MR_WHITE_RELIC')
    cmd('relic add KAREN_STAR_TIARA_RELIC')
    cmd('relic add STRAWBERRY')
    time.sleep(1)
    def action():
        cmd('fight NIBBITS_WEAK')
        s = wait(lambda s: combat(s) and s['battle']['round']==1, 'tiara turn 1')
        assert s['player']['energy']==4, s['player']['energy']
        capture('07-tiara-turn1')
        time.sleep(2)
        act('end_turn')
        s = wait(lambda s: combat(s) and s['battle']['round']==2, 'tiara turn 2')
        assert s['player']['energy']==4, s['player']['energy']
        assert not any(x['id']=='KAREN_STAR_TIARA_RELIC' for x in s['player']['relics'])
        capture('07-tiara-turn2')
        time.sleep(2)
        act('end_turn')
        s = wait(lambda s: combat(s) and s['battle']['round']==3, 'tiara turn 3')
        assert s['player']['energy']==3, s['player']['energy']
        capture('07-tiara-after')
    record(7, 'tiara', action)
    original = json.loads((output / 'original-settings.json').read_text(encoding='utf-8'))
    request('/settings', {'muted':original['muted']})
