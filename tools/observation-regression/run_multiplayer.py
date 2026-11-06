"""Target Flower selections and skips on two local peers."""
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

source = project / 'artifacts/dev-game' / args.branch
game = source / 'game'
output = project / 'artifacts/observation-multiplayer' / args.branch / args.label
output.mkdir(parents=True, exist_ok=False)
ports = [18861, 18862]
for port in ports:
    with socket.socket() as check:
        assert check.connect_ex(('127.0.0.1', port)) != 0, 'Test MCP port in use'
with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as check:
    check.bind(('127.0.0.1', 33771))
client = output / 'client/game'
for current, dirs, files in os.walk(game):
    dirs[:] = [d for d in dirs if d != 'mods']
    target = client / Path(current).relative_to(game)
    target.mkdir(parents=True, exist_ok=True)
    for name in files:
        if name != 'override.cfg':
            os.link(Path(current) / name, target / name)
shutil.copytree(game / 'mods', client / 'mods')
results = []

def api(peer, path, data=None, binary=False):
    request = urllib.request.Request(f'http://127.0.0.1:{ports[peer]}/api/v1' + path, data=None if data is None else json.dumps(data).encode(), headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=15) as response:
        content = response.read()
    return content if binary else json.loads(content)

def state(peer, endpoint='multiplayer'):
    return api(peer, '/' + endpoint + '?format=json')

def act(peer, action, endpoint='multiplayer', **values):
    result = api(peer, '/' + endpoint, dict(action=action, **values))
    assert result.get('status') == 'ok', result
    return result

def cmd(peer, command):
    return act(peer, 'run_command', command=command)

def wait(peer, condition, label, endpoint='multiplayer'):
    end = time.monotonic() + 60
    while time.monotonic() < end:
        try:
            current = state(peer, endpoint)
            if condition(current):
                return current
        except (OSError, TimeoutError):
            pass
        time.sleep(.25)
    raise RuntimeError('Timeout ' + label + ' ' + json.dumps(state(peer, endpoint)))

def snapshot(label):
    states = [json.loads(cmd(peer, 'observation_fixture rewards')['message']) for peer in (0, 1)]
    for peer, value in enumerate(states):
        (output / f'{label}-{peer}.json').write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')
    return states

def same_states(label):
    end = time.monotonic() + 15
    while time.monotonic() < end:
        states = snapshot(label)
        if states[0]['players'] == states[1]['players'] and states[0]['nextRewardIds'] == states[1]['nextRewardIds']:
            return states
        time.sleep(.25)
    raise AssertionError('Peer states differ at ' + label)

def capture(label, peer):
    (output / f'{label}-{peer}.png').write_bytes(api(peer, '/screenshot', binary=True))

settings_source = json.loads((source / 'user/AppData/Roaming/SlayTheSpire2/default/1/settings.save').read_text(encoding='utf-8-sig'))
settings_source.update(fullscreen=False, limit_fps_in_background=False)
desktop = TestDesktop()
try:
    for peer, peer_game in enumerate((game, client)):
        profile = 9140 + peer
        root = output / ('host' if peer == 0 else 'client')
        roaming = root / 'user/AppData/Roaming'
        settings = roaming / f'SlayTheSpire2/default/{profile}/settings.save'
        settings.parent.mkdir(parents=True)
        settings.write_text(json.dumps(settings_source), encoding='utf-8')
        (peer_game / 'mods/KarenSTS2MCP.conf').write_text(json.dumps(dict(port=ports[peer], fullscreen=False, muted=True, silent_test=True, skip_tutorial=True, skip_first_prompt=True, window_width=1280, window_height=720)), encoding='utf-8')
        env = os.environ.copy()
        env.pop('KAREN_OBSERVATION_UI', None)
        env['APPDATA'] = str(roaming)
        env['LOCALAPPDATA'] = str(root / 'user/AppData/Local')
        log = root / 'godot.log'
        mode = 'host_standard' if peer == 0 else 'join'
        process = desktop.launch([peer_game / 'SlayTheSpire2.exe', '--rendering-driver', 'vulkan', '--force-steam=off', '--fastmp=' + mode, f'--clientId={profile}', '--log-file', log], peer_game, env)
        end = time.monotonic() + 60
        while time.monotonic() < end:
            if log.exists() and '[Karen STS2 MCP]' in log.read_text(encoding='utf-8', errors='replace'):
                break
            assert process.poll() is None, 'Game exited before MCP loaded'
            time.sleep(.25)
        else:
            raise RuntimeError('Karen MCP not loaded')
        wait(peer, lambda s: s.get('menu_screen') == 'character_select', 'lobby', 'singleplayer')
    for peer in (0, 1):
        wait(peer, lambda s: s.get('lobby', {}).get('player_count') == 2, 'two peers', 'singleplayer')
        act(peer, 'menu_select', endpoint='singleplayer', option='KAREN')
    for peer in (0, 1):
        act(peer, 'menu_select', endpoint='singleplayer', option='embark')
    for peer in (0, 1):
        wait(peer, lambda s: s.get('state_type') == 'map', 'map')
        act(peer, 'choose_map_node', index=0)
    for peer in (0, 1):
        wait(peer, lambda s: s.get('battle', {}).get('is_play_phase'), 'combat')
        cmd(peer, 'observation_flower_prepare')
        time.sleep(1)
    cmd(0, 'kill all')
    for peer in (0, 1):
        wait(peer, lambda s: s.get('state_type') == 'rewards', 'combat rewards')
    time.sleep(2)
    before = same_states('before-choices')
    assert all(len(p['groups']) == 3 for p in before[0]['players']), before
    for peer in (0, 1):
        capture('before-choices', peer)
    # Host selects the original Cauldron, whose grant opens a nested potion set.
    items = json.loads(cmd(0, 'observation_fixture rewards')['message'])['buttons']
    cauldron = next(item for item in items if item.get('relic_id') == 'CAULDRON')
    act(0, 'claim_reward', index=cauldron['index'])
    nested = wait(0, lambda s: s.get('state_type') == 'rewards' and any(i.get('type') == 'potion' for i in s.get('rewards', {}).get('items', [])) and not any(i.get('type') == 'relic' for i in s.get('rewards', {}).get('items', [])), 'nested Cauldron rewards')
    capture('nested', 0)
    # Skip the nested set, then return to the captured outer set.
    act(0, 'proceed')
    wait(0, lambda s: s.get('state_type') == 'rewards' and any(i.get('type') == 'relic' for i in s.get('rewards', {}).get('items', [])), 'outer rewards')
    # Client chooses the second option of its Cauldron pair.
    items = json.loads(cmd(1, 'observation_fixture rewards')['message'])['buttons']
    cauldron_index = next(i for i, item in enumerate(items) if item.get('relic_id') == 'CAULDRON')
    alternative = items[cauldron_index + 1]
    assert alternative.get('relic_id'), items
    act(1, 'claim_reward', index=alternative['index'])
    time.sleep(2)
    after = same_states('after-choices')
    for peer, chosen in ((0, 'CAULDRON'), (1, alternative['relic_id'])):
        player_id = before[0]['players'][peer]['id']
        player = next(p for p in after[0]['players'] if p['id'] == player_id)
        assert chosen in player['relics'], player
        assert sum(bool(group['completed']) for group in player['groups']) == 1, player
        original = next(group for group in before[0]['players'][peer]['groups'] if group['original'] == 'CAULDRON')
        skipped = next(r for r in original['remaining'] if r != chosen)
        assert sum(c['id'] == skipped and not c['picked'] for c in player['history']) == 1, player
        capture('after-choices', peer)
    # Exercise naturally generated Flower pairs too, taking the first option on
    # the host and the second option on the client, then leaving one pair to skip.
    for peer in (0, 1):
        items = json.loads(cmd(peer, 'observation_fixture rewards')['message'])['buttons']
        original_index = next(i for i, item in enumerate(items) if item.get('relic_id') == 'VAJRA')
        chosen = items[original_index + peer]
        act(peer, 'claim_reward', index=chosen['index'])
    time.sleep(2)
    natural = same_states('after-natural-choices')
    assert all(sum(bool(group['completed']) for group in p['groups']) == 2 for p in natural[0]['players']), natural
    # Skip the other Flower group and all ordinary rewards on both peers.
    for peer in (0, 1):
        act(peer, 'proceed')
    for peer in (0, 1):
        wait(peer, lambda s: s.get('state_type') == 'map', 'map after skip')
    # The native backend skips terminal rewards when the next room is entered.
    for peer in (0, 1):
        act(peer, 'choose_map_node', index=0)
    for peer in (0, 1):
        wait(peer, lambda s: s.get('state_type') not in ('map', 'rewards'), 'next room')
    time.sleep(2)
    final = same_states('after-skip')
    for player in final[0]['players']:
        unclaimed = next(group for group in player['groups'] if group['original'] == 'ANCHOR')
        for relic in unclaimed['remaining']:
            assert sum(c['id'] == relic and not c['picked'] for c in player['history']) == 1, player
    for value in final:
        assert value['wireIndices'] and all(0 <= index < 255 for index in value['wireIndices']), value
    errors = []
    for role in ('host', 'client'):
        log = (output / role / 'godot.log').read_text(encoding='utf-8', errors='replace')
        startup, gameplay = log.split('[Observation] FLOWER PREPARE', 1)
        diagnostics = [line for line in startup.splitlines() if '[ERROR]' in line or line.startswith('ERROR:')]
        (output / f'{role}-startup-diagnostics.json').write_text(json.dumps(diagnostics), encoding='utf-8')
        errors.extend(line for line in gameplay.splitlines() if '[ERROR]' in line or line.startswith('ERROR:'))
    assert not errors, errors
    results = [dict(case='host-original-and-nested-skip', result='pass'), dict(case='client-second-option', result='pass'), dict(case='outer-skip-and-peer-state', result='pass'), dict(case='native-wire-indices', result='pass')]
    print(args.branch + ' Flower multiplayer passed', flush=True)
finally:
    (output / 'results.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
    desktop.close()
