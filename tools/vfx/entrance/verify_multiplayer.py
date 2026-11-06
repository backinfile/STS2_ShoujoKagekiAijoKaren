"""Real same-version loopback peers: entrance, per-player isolation and native turns.

Only the test games run on a private desktop. All game actions/images use KarenSTS2MCP.
No gameplay hooks are replayed by the duplicate-presentation fixture.
"""
import argparse
from concurrent.futures import ThreadPoolExecutor
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

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'six-card'))
from isolated_desktop import TestDesktop
from audit_log import audit

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
parser.add_argument('--source-repo', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--cases', default='KK,KI,IK,II,KK-fast,KK-instant')
parser.add_argument('--repeat', action='store_true', help='Request the same local presentation during and after its lifetime')
parser.add_argument('--record', action='store_true', help='Capture native entrance video and process audio on every peer')
args = parser.parse_args()
repo = Path(__file__).resolve().parents[3]
source = args.source_repo.resolve() / 'artifacts/dev-game' / args.branch
root = args.output.resolve()
assert not root.exists(), 'Use a fresh evidence directory'
root.mkdir(parents=True)
cases = args.cases.split(',')
max_peers = max(len(case.split('-')[0]) for case in cases)
assert 2 <= max_peers <= 4 and all(2 <= len(case.split('-')[0]) <= 4
    and set(case.split('-')[0]) <= {'K', 'I'} for case in cases)
peer_count = max_peers
ports = [(19157 if args.branch == 'stable' else 19167) + i for i in range(max_peers)]
for port in ports:
    with socket.socket() as check:
        assert check.connect_ex(('127.0.0.1', port)) != 0, f'MCP port {port} occupied'
with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as check:
    check.bind(('127.0.0.1', 33771))

def save(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')

def api(side, path, value=None):
    request = urllib.request.Request(f'http://127.0.0.1:{ports[side]}/api/v1' + path,
        data=None if value is None else json.dumps(value).encode(), headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)

case_root = root

def action(side, endpoint='multiplayer', **value):
    result = api(side, '/' + endpoint, value)
    with (case_root / 'actions.jsonl').open('a', encoding='utf-8') as log:
        log.write(json.dumps([side, value, result], ensure_ascii=False) + '\n')
    assert result.get('status') == 'ok', (side, value, result)
    return result

def cmd(side, command):
    return action(side, action='run_command', command=command)

def state(side, endpoint='multiplayer'):
    return api(side, '/' + endpoint + '?format=json')

def probe(side):
    return json.loads(cmd(side, 'entrance_probe status')['message'])

def wait(side, predicate, label, endpoint='multiplayer', seconds=65):
    end = time.monotonic() + seconds
    last = None
    while time.monotonic() < end:
        try:
            last = state(side, endpoint)
            if predicate(last):
                return last
            if last.get('menu_screen') == 'tutorial_prompt':
                action(side, 'singleplayer', action='menu_select', option='no')
        except (OSError, TimeoutError):
            pass
        time.sleep(.15)
    raise AssertionError((label, side, last))

def snap(side, label):
    save(case_root / f'{side}-{label}.json', state(side))
    with urllib.request.urlopen(f'http://127.0.0.1:{ports[side]}/api/v1/screenshot', timeout=30) as response:
        (case_root / f'{side}-{label}.png').write_bytes(response.read())

def pair_snap(label):
    with ThreadPoolExecutor(peer_count) as pool:
        list(pool.map(lambda side: snap(side, label), range(peer_count)))


def start_recording(side):
    api(side, '/settings', dict(muted=False, fullscreen=False, window_width=1280, window_height=720))
    save(case_root / f'{side}-audio-before.json', json.loads(cmd(side, 'entrance_probe audio')['message']))
    result = api(side, '/recording/start', dict(fps=30, max_duration_seconds=15, width=1280, height=720, audio=True))
    save(case_root / f'{side}-recording-start.json', result)


def finish_recording(side):
    api(side, '/recording/stop', {})
    end = time.monotonic() + 50
    while time.monotonic() < end:
        result = api(side, '/recording')
        if result['status'] in ('completed', 'failed'):
            break
        time.sleep(.2)
    assert result['status'] == 'completed' and result['audio'] and result['audio_source'] == 'game_process', result
    shutil.copy2(result['output_path'], case_root / f'{side}-game.mp4')
    save(case_root / f'{side}-recording.json', result)
    save(case_root / f'{side}-audio-after.json', json.loads(cmd(side, 'entrance_probe audio')['message']))
    api(side, '/settings', dict(muted=True))

def link(src, dst):
    try:
        os.link(src, dst)
    except OSError:
        shutil.copy2(src, dst)
    return dst

probe_output = root / 'probe'
subprocess.run(['dotnet', 'build', str(repo / 'tools/vfx/entrance/EntranceProbe.csproj'),
    f'-p:Game={source / "game"}', f'-p:Variant={args.branch.title()}', '-o', str(probe_output),
    '--nologo', '-v:q'], check=True)
games = []
for side in range(max_peers):
    game = root / ('host-game' if side == 0 else f'client{side}-game')
    shutil.copytree(source / 'game', game, copy_function=link,
        ignore=lambda directory, names: ['mods', 'override.cfg'] if Path(directory) == source / 'game' else [])
    mods = game / 'mods'
    mods.mkdir()
    shutil.copytree(source / 'game/mods/BaseLib', mods / 'BaseLib')
    shutil.copytree(repo / 'artifacts/ShoujoKagekiAijoKaren', mods / 'ShoujoKagekiAijoKaren')
    mcp = args.source_repo.resolve() / 'artifacts' / ('mcp-' + args.branch)
    assert json.loads((mcp / 'source-info.json').read_text(encoding='utf-8-sig'))['loopback_only']
    for suffix in ['dll', 'json']:
        shutil.copy2(mcp / ('KarenSTS2MCP.' + suffix), mods)
    shutil.copy2(probe_output / 'KarenEntranceProbe.dll', mods)
    (mods / 'KarenEntranceProbe.json').write_text(json.dumps(dict(id='KarenEntranceProbe', name='KarenEntranceProbe',
        version='1.0.0', author='Development', min_game_version='v0.107.1',
        description='Temporary local observation and loopback transport', has_dll=True, has_pck=False,
        affects_gameplay=False)), encoding='utf-8')
    (mods / 'KarenSTS2MCP.conf').write_text(json.dumps(dict(port=ports[side], fullscreen=False, muted=True,
        skip_first_prompt=True, skip_tutorial=True)), encoding='utf-8')
    (game / 'override.cfg').write_text('[display]\nwindow/size/mode=1\nwindow/size/no_focus=true\n'
        'window/size/window_width_override=1280\nwindow/size/window_height_override=720\n', encoding='utf-8')
    games.append(game)
save(root / 'build.json', {str(p.relative_to(repo)): hashlib.sha256(p.read_bytes()).hexdigest()
    for p in (repo / 'artifacts/ShoujoKagekiAijoKaren').rglob('*') if p.suffix in ['.dll', '.pck']})

results = {}
for index, case in enumerate(cases):
    case_root = root / case
    case_root.mkdir()
    characters = ['KAREN' if c == 'K' else 'IRONCLAD' for c in case.split('-')[0]]
    peer_count = len(characters)
    speeds = ['normal'] * peer_count
    speeds[1] = 'fast' if case.endswith('-fast') else 'instant' if case.endswith('-instant') else 'normal'
    expected = characters.count('KAREN')
    desktop = TestDesktop()
    processes = []
    try:
        for side in range(peer_count):
            dev = case_root / str(side)
            profile = 61000 + index * 10 + side
            settings = dev / f'user/AppData/Roaming/SlayTheSpire2/default/{profile}/settings.save'
            settings.parent.mkdir(parents=True)
            data = json.loads((source / 'user/AppData/Roaming/SlayTheSpire2/default/1/settings.save').read_text(encoding='utf-8-sig'))
            data.update(mute_in_background=False, limit_fps_in_background=False)
            if args.record:
                data.update(volume_sfx=1.0, volume_bgm=.12)
            settings.write_text(json.dumps(data), encoding='utf-8')
            (dev / 'user/AppData/Local').mkdir(parents=True)
            env = os.environ.copy()
            env.update(APPDATA=str(dev / 'user/AppData/Roaming'), LOCALAPPDATA=str(dev / 'user/AppData/Local'),
                KAREN_ENTRANCE_MP_TEST='1', STS2_MCP_FFMPEG=shutil.which('ffmpeg'))
            processes.append(desktop.launch([str(games[side] / 'SlayTheSpire2.exe'), '--rendering-driver', 'vulkan',
                '--force-steam=off', '--fastmp=' + ('host_standard' if side == 0 else 'join'),
                f'--clientId={profile}', '--log-file', str(dev / 'godot.log')], cwd=games[side], env=env))
            end = time.monotonic() + 80
            while time.monotonic() < end:
                if (dev / 'godot.log').exists() and '[Karen STS2 MCP]' in (dev / 'godot.log').read_text(encoding='utf-8', errors='replace'):
                    break
                time.sleep(.2)
            else:
                raise AssertionError(('MCP not loaded', side))
            if side == 0:
                wait(0, lambda s: s.get('menu_screen') == 'character_select', 'host lobby', 'singleplayer')
        wait(1, lambda s: s.get('lobby', {}).get('player_count') == peer_count, 'joined lobby', 'singleplayer')
        for side in range(peer_count):
            action(side, 'singleplayer', action='menu_select', option=characters[side])
            wait(side, lambda s: any(p.get('is_local') and p.get('character_id') == characters[side]
                for p in s.get('lobby', {}).get('players', [])), 'character', 'singleplayer')
        for side in range(peer_count):
            action(side, 'singleplayer', action='menu_select', option='embark')
        for side in range(peer_count):
            wait(side, lambda s: s.get('state_type') == 'map', 'map')
            cmd(side, 'entrance_probe speed ' + speeds[side])
            cmd(side, 'entrance_probe watch' + (' peer' if case == 'KK' and not args.record else ''))
        pair_snap('before')
        if args.record:
            with ThreadPoolExecutor(peer_count) as pool:
                list(pool.map(start_recording, range(peer_count)))
            time.sleep(.3)
        for side in range(peer_count):
            if state(side).get('state_type') == 'map':
                action(side, action='choose_map_node', index=0)
        if expected:
            observer = characters.index('KAREN')
            end = time.monotonic() + 25
            while time.monotonic() < end:
                p = probe(observer)
                if p['casts'] and p['maxAge'] >= .20:
                    break
                time.sleep(.02)
            assert p['casts'] > 0, p
            if args.repeat:
                for side in range(peer_count):
                    if speeds[side] != 'instant':
                        cmd(side, 'entrance_probe duplicate')
            time.sleep(.25)
            pair_snap('during')
        for side in range(peer_count):
            wait(side, lambda s: s.get('battle', {}).get('is_play_phase'), 'native opening')
        time.sleep(1.6)
        if args.repeat and expected:
            for side in range(peer_count):
                if speeds[side] != 'instant':
                    cmd(side, 'entrance_probe duplicate')
            time.sleep(1.6)
        reports = [probe(side) for side in range(peer_count)]
        save(case_root / 'entrance.json', reports)
        pair_snap('after')
        if args.record:
            with ThreadPoolExecutor(peer_count) as pool:
                list(pool.map(finish_recording, range(peer_count)))
        for side, report in enumerate(reports):
            count = int(characters[side] == 'KAREN' and speeds[side] != 'instant')
            assert report['casts'] == count and report['soundCues'] == 0, report
            assert report['beforeStartCalls'] == 1 and report['beforeStartVisible'] == count, report
            assert report['live'] == report['backLive'] == report['posesLive'] == report['towerEntrancesLive'] == 0, report
            assert report['colorsPreserved'] and report['materialPreserved'], report
            if count:
                actors = report['actors']
                assert report['beforeStartAge'] < .1 and len(actors) == expected, report
                assert sum(a['Local'] for a in actors) == 1, report
                assert all(a['PoseFrames'] and a['TowerFrames'] and a['PartialTowerFrames']
                    if a['Local'] else a['PoseFrames'] == a['TowerFrames'] == a['PartialTowerFrames'] == 0
                    for a in actors), report
                assert len({a['Material'] for a in actors}) == expected, report
                assert len({(round(a['X']), round(a['Y'])) for a in actors}) == expected, report
                if peer_count >= 3:
                    assert all(a['TowerLeft'] >= 17.8 and a['TowerTop'] >= 63.8
                        and a['TowerRight'] <= 1280 and a['TowerBottom'] <= 550 for a in actors), report
                if expected == 2:
                    assert abs(actors[0]['X'] - actors[1]['X']) > 10, report
                if case == 'KK' and not args.record:
                    assert report['peerInterrupted'] and report['peerIndependent'], report
            else:
                assert not report['actors'], report
        played = []
        for side in range(peer_count):
            current = state(side)
            card = next((c for c in current['player']['hand'] if c['can_play'] and c['id'] in ['KAREN_STRIKE', 'STRIKE_IRONCLAD']), None)
            if card is None:
                card = next((c for c in current['player']['hand'] if c['can_play'] and c['target_type'] == 'AnyEnemy'), None)
            if card is None:
                card = next(c for c in current['player']['hand'] if c['can_play'] and 'DEFEND' in c['id'])
            payload = dict(action='play_card', card_index=card['index'])
            if card['target_type'] == 'AnyEnemy':
                payload['target'] = current['battle']['enemies'][0]['entity_id']
            action(side, **payload)
            played.append(card['id'])
            time.sleep(1.2)
        snapshots = [state(side) for side in range(peer_count)]
        assert all([e['hp'] for e in snapshots[0]['battle']['enemies']] == [e['hp'] for e in s['battle']['enemies']]
            for s in snapshots[1:]), snapshots
        save(case_root / 'after-cards.json', snapshots)
        for side in range(peer_count):
            action(side, action='end_turn')
        for side in range(peer_count):
            wait(side, lambda s: s.get('battle', {}).get('round', 0) >= 2 and s['battle']['is_play_phase'], 'shared round 2')
        time.sleep(.7)
        pair_snap('round2')
        for side in range(peer_count):
            hand = json.loads(cmd(side, 'karen_check_hand')['message'])
            save(case_root / f'{side}-hand.json', hand)
            assert hand['modelCount'] == hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons'], hand
        if peer_count >= 3:
            # Exercise the fitted tower in the local and remote player's view:
            # real Promise card transfer and recall, not a direct VFX invocation.
            for actor_side in [0, peer_count - 1]:
                if characters[actor_side] != 'KAREN':
                    continue
                current = state(actor_side)
                initial_defends = sum(c['id'] == 'KAREN_DEFEND' for c in current['player']['hand'])
                initial_falls = sum(c['id'] == 'KAREN_FALL' for c in current['player']['hand'])
                cmd(actor_side, 'card KAREN_DEFEND Hand')
                cmd(actor_side, 'card KAREN_FALL Hand')
                fixture = wait(actor_side, lambda s: sum(c['id'] == 'KAREN_DEFEND' for c in s['player']['hand']) == initial_defends + 1
                    and sum(c['id'] == 'KAREN_FALL' for c in s['player']['hand']) == initial_falls + 1, 'promise fixture')
                pair_snap(f'promise-{actor_side}-before')
                fall = next(c for c in reversed(fixture['player']['hand']) if c['id'] == 'KAREN_FALL')
                action(actor_side, action='play_card', card_index=fall['index'])
                selection = wait(actor_side, lambda s: s.get('state_type') == 'hand_select', 'promise selection')
                defend = next(c for c in reversed(selection['hand_select']['cards']) if c['id'] == 'KAREN_DEFEND')
                action(actor_side, action='combat_select_card', card_index=defend['index'])
                if state(actor_side).get('state_type') == 'hand_select':
                    action(actor_side, action='combat_confirm_selection')
                wait(actor_side, lambda s: s.get('state_type') == 'monster' and s['battle']['is_play_phase'], 'promise transfer')
                time.sleep(.7)
                pair_snap(f'promise-{actor_side}-fall')
                after = state(actor_side)
                assert sum(c['id'] == 'KAREN_DEFEND' for c in after['player']['hand']) == initial_defends, after
                initial_towers = sum(c['id'] == 'KAREN_TOWER_OF_PROMISE' for c in after['player']['hand'])
                cmd(actor_side, 'card KAREN_TOWER_OF_PROMISE Hand')
                fixture = wait(actor_side, lambda s: sum(c['id'] == 'KAREN_TOWER_OF_PROMISE' for c in s['player']['hand']) == initial_towers + 1, 'recall fixture')
                tower = next(c for c in reversed(fixture['player']['hand']) if c['id'] == 'KAREN_TOWER_OF_PROMISE')
                action(actor_side, action='play_card', card_index=tower['index'])
                wait(actor_side, lambda s: s.get('state_type') == 'monster' and s['battle']['is_play_phase'], 'promise recall')
                time.sleep(1.2)
                pair_snap(f'promise-{actor_side}-tower')
                after = state(actor_side)
                assert sum(c['id'] == 'KAREN_DEFEND' for c in after['player']['hand']) == initial_defends + 1, after
                for side in range(peer_count):
                    hand = json.loads(cmd(side, 'karen_check_hand')['message'])
                    save(case_root / f'promise-{actor_side}-{side}-hand.json', hand)
                    assert hand['modelCount'] == hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons'], hand
        second_room = None
        # Finish through native combat/reward cleanup. A debug jump while still
        # in combat can interrupt the native hand pool's deferred card return.
        if peer_count >= 3:
            cmd(0, 'kill all')
            for side in range(peer_count):
                wait(side, lambda s: s.get('state_type') == 'rewards', 'native combat rewards')
            time.sleep(.7)
            pair_snap('rewards')
        if case in ['KK', 'KKKK']:
            for side in range(peer_count):
                cmd(side, 'entrance_probe watch')
            cmd(0, 'fight WATERFALL_GIANT_BOSS')
            for side in range(peer_count):
                end = time.monotonic() + 30
                while time.monotonic() < end:
                    if probe(side)['beforeStartCalls'] == 1:
                        break
                    time.sleep(.05)
                wait(side, lambda s: s.get('battle', {}).get('is_play_phase'), 'next combat')
            time.sleep(1.7)
            second_room = [probe(side) for side in range(peer_count)]
            save(case_root / 'next-combat.json', second_room)
            pair_snap('next-combat')
            assert all(p['casts'] == p['beforeStartCalls'] == 1 and p['soundCues'] == 0
                and len(p['actors']) == expected and p['live'] == p['backLive'] == p['posesLive'] == p['towerEntrancesLive'] == 0
                and sum(a['Local'] and a['PoseFrames'] > 0 and a['TowerFrames'] > 0 for a in p['actors']) == 1
                and all(a['Local'] or a['PoseFrames'] == a['TowerFrames'] == 0 for a in p['actors'])
                for p in second_room), second_room
        case_result = dict(status='pass', characters=characters, localSpeeds=speeds, views=reports,
            played=played, enemyHpMatched=True, allAtRound2=True, allHandsMatched=True, nextCombat=second_room)
    finally:
        desktop.close()
        errors = []
        for side in range(peer_count):
            path = case_root / str(side) / 'godot.log'
            if path.exists():
                diagnostic = audit(path.read_text(encoding='utf-8', errors='replace'))
                save(case_root / f'{side}-log.json', diagnostic)
                errors.extend((side, error) for error in diagnostic['unexpectedErrors'])
        assert not errors, errors
    results[case] = case_result
    save(root / 'results.json', results)
    print(args.branch, case, 'PASS', flush=True)
