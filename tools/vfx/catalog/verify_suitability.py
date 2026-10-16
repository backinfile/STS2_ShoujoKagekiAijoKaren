"""Audit real-play outcomes, package provenance, hand nodes and capture metadata."""
import argparse
import hashlib
import json
from pathlib import Path
import re

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
root = args.output.resolve()

def load(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

summary = {'scope': 'Targeted single-player VFX checks only. Relay cards are injected to exercise actual exhaustion/reward, not multiplayer transfer.', 'runs': []}
for version, branch, package in [('before', 'stable', 'before-package'), ('after', 'stable', 'final-package'), ('after', 'beta', 'final-package')]:
    folder = root / version / branch
    build = dict(assemblySha256=sha(root / package / 'lib' / f'ShoujoKagekiAijoKaren.{branch.title()}.dll'),
                 packSha256=sha(root / package / 'ShoujoKagekiAijoKaren.pck'))
    run = dict(version=version, branch=branch, build=build, scenes=[])
    for name in ('jealousy', 'wake', 'arrogant', 'relay'):
        scene = folder / ('polish-' + name)
        observed = load(scene / 'polish-status.json')
        assert observed['live'] == 0 and observed['back'] == 0
        assert all(n['exited'] and n['frames'] > 0 for n in observed['nodes'])
        before, after = load(scene/'recording-before.json'), load(scene/'recording-after.json')
        if name == 'jealousy':
            assert before['battle']['enemies'][0]['hp'] - after['battle']['enemies'][0]['hp'] == 35
        if name == 'wake':
            assert len(after['player']['hand']) == 5 and after['player']['exhaust_pile_count'] == 1
        if name in ('arrogant','relay'):
            assert observed['passion'] == observed['relicRewards'] == 2
        capture = load(scene / 'recording.json')
        assert capture['captureBuild'] == build and capture['status'] == 'completed'
        assert capture['audio'] and capture['audio_source'] == 'game_process'
        assert capture.get('encoder') and capture.get('capture_fps', 0) > 0
        run['scenes'].append(dict(name=name, outcomes='passed', nodes=len(observed['nodes']),
            captureFps=capture['capture_fps'], repeatedFrames=capture['frames_repeated'], droppedFrames=capture['frames_dropped'],
            encoder=capture['encoder'], movieSha256=sha(scene/'game.mp4')))
    hand_count = 0
    for p in folder.rglob('*hand*.json'):
        # State files with hand in their label are not necessarily checker results.
        h = load(p)
        if isinstance(h,dict) and 'message' in h:
            h = json.loads(h['message'])
        if not isinstance(h,dict) or 'modelCount' not in h:
            continue
        assert h['modelCount'] == h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'], p
        hand_count += 1
    run['handChecks'] = hand_count
    monitor = load(folder/'window-monitor.json')
    assert monitor['foreground_samples'] == monitor['restored_samples'] == 0
    run['windowMonitor'] = monitor
    lines = (folder/'godot.log').read_text(encoding='utf8',errors='replace').splitlines()
    run['logDiagnostics'] = [line for line in lines if re.search(r'ERROR:|SCRIPT ERROR|SHADER ERROR|Exception',line)]
    summary['runs'].append(run)

for branch in ('stable','beta'):
    # Stable's broad regression preceded only the final opacity/size calibration.
    folder=root/'after'/('stable-first-pass' if branch=='stable' else 'beta')
    checks=[]
    for mode in ('fast','instant','pose','reverse','pre-hit','cancel-impact','replay','overlap'):
        p=folder/('jealousy-'+mode)/(mode+'-jealousy.json')
        r=load(p)
        assert r['live']==0 and r['maxActorZError']==0
        assert all(r[k]<.001 for k in ('maxActorOffsetError','maxModelPositionError','maxVisualPositionError'))
        assert all(c['exited'] and c['maxAnchorError']<.1 and c['particleCapacity']==88 and c['maxSpread']<=46 and c['minDirectionDot']>.99 for c in r['casts'])
        checks.append(mode)
    for p in folder.glob('polish-check-*/polish-status.json'):
        r=load(p);assert r['live']==r['back']==0 and r['passion']==r['relicRewards']==1
        if 'instant' in p.parent.name:assert not r['nodes']
        checks.append(p.parent.name)
    lethal=load(folder/'jealousy-lethal/lethal-jealousy.json');assert lethal['live']==0 and len(lethal['casts'])==1 and lethal['casts'][0]['exited']
    checks.append('lethal-and-next-room')
    summary.setdefault('regressions',[]).append(dict(branch=branch, cases=checks,
        package='first-presentation-pass' if branch=='stable' else 'final-package',
        note='Stable regression was before final foil opacity/size tuning; final Stable ordinary/upgraded and all final Beta cases were rerun. Motion/lifetimes/callbacks are unchanged by that last tuning.'))

# Optional supplemental coverage: upgraded Wake Up chooses a source, then cards.
upgrade_root = root/'wake-upgraded'
if (upgrade_root/'before/stable/polish-wake-upgraded/recording.json').exists():
    upgraded = []
    for version, branch, package in [('before', 'stable', 'before-package'), ('after', 'stable', 'final-package'), ('after', 'beta', 'final-package')]:
        folder = upgrade_root/version/branch
        scene = folder/'polish-wake-upgraded'
        source, cards = load(scene/'source-choice-state.json'), load(scene/'card-choice-state.json')
        assert source['card_select']['screen_type'] == 'choose'
        assert {c['id'] for c in source['card_select']['cards']} == {
            'KAREN_WAKE_UP_DRAW_PILE_OPTION', 'KAREN_WAKE_UP_PROMISE_PILE_OPTION', 'KAREN_WAKE_UP_DISCARD_PILE_OPTION'}
        assert cards['card_select']['screen_type'] == 'simple_select' and len(cards['card_select']['cards']) == 3
        assert load(scene/'source-choice-probe.json')['showingCurtains'] == 0
        assert load(scene/'card-choice-probe.json')['showingCurtains'] == 1
        after = load(scene/'recording-after.json')
        assert len(after['player']['hand']) == 5 and after['player']['exhaust_pile_count'] == 1
        status = load(scene/'polish-status.json')
        assert status['live'] == status['back'] == 0 and all(n['exited'] and n['frames'] > 0 for n in status['nodes'])
        capture = load(scene/'recording.json')
        assert capture['captureBuild'] == dict(assemblySha256=sha(root/package/'lib'/f'ShoujoKagekiAijoKaren.{branch.title()}.dll'),
            packSha256=sha(root/package/'ShoujoKagekiAijoKaren.pck'))
        assert capture['status'] == 'completed' and capture['audio_source'] == 'game_process'
        hands = 0
        for hand in scene.glob('*hand.json'):
            h = load(hand)
            if 'message' in h: h = json.loads(h['message'])
            assert h['modelCount'] == h['visualCount'] and not any(h[k] for k in ('missing','unexpected','unexpectedPileIcons'))
            hands += 1
        monitor = load(folder/'window-monitor.json')
        assert monitor['foreground_samples'] == monitor['restored_samples'] == 0
        diagnostics = [line for line in (folder/'godot.log').read_text(encoding='utf8',errors='replace').splitlines()
            if re.search(r'ERROR:|SCRIPT ERROR|SHADER ERROR|Exception',line)]
        assert not diagnostics, diagnostics
        upgraded.append(dict(version=version,branch=branch,passed=True,sourceOptions=3,candidates=3,handChecks=hands,
            showingCurtains=[0,1],captureFps=capture['capture_fps'],captureBuild=capture['captureBuild'],movieSha256=sha(scene/'game.mp4')))
    summary['upgradedWakeTwoStages'] = upgraded

# Later correction restores the user-requested TV Mahiru confetti identity.
mahiru = root/'mahiru'
if (mahiru/'after/stable/polish-jealousy/recording.json').exists():
    revised = []
    for branch in ('stable', 'beta'):
        folder = mahiru/'after'/branch
        scene = folder/'polish-jealousy'
        before, after = load(scene/'recording-before.json'), load(scene/'recording-after.json')
        assert before['battle']['enemies'][0]['hp'] - after['battle']['enemies'][0]['hp'] == 35
        capture = load(scene/'recording.json')
        assert capture['captureBuild'] == dict(assemblySha256=sha(root/'mahiru-package/lib'/f'ShoujoKagekiAijoKaren.{branch.title()}.dll'),
            packSha256=sha(root/'mahiru-package/ShoujoKagekiAijoKaren.pck'))
        assert capture['status'] == 'completed' and capture['audio_source'] == 'game_process'
        status = load(scene/'polish-status.json')
        assert status['live'] == status['back'] == 0 and all(n['exited'] for n in status['nodes'])
        modes = ('fast','instant','pose','reverse','pre-hit','cancel-impact','replay','overlap') if branch == 'stable' else ('fast','instant','reverse','replay')
        checks = []
        for mode in modes:
            report = load(folder/('jealousy-'+mode)/(mode+'-jealousy.json'))
            assert report['live'] == report['maxActorZError'] == 0
            assert all(report[k] < .001 for k in ('maxActorOffsetError','maxModelPositionError','maxVisualPositionError'))
            assert all(c['exited'] and c['maxAnchorError'] < .1 and c['particleCapacity'] == 88
                and c['maxSpread'] <= 46 and c['minDirectionDot'] > .99 for c in report['casts'])
            checks.append(mode)
        lethal = load(folder/'jealousy-lethal/lethal-jealousy.json')
        next_room = load(folder/'jealousy-lethal/next-room-jealousy.json')
        assert lethal['live'] == next_room['live'] == 0 and len(lethal['casts']) == 1 and lethal['casts'][0]['exited']
        hands = 0
        for p in folder.rglob('*hand*.json'):
            h = load(p)
            if isinstance(h,dict) and 'message' in h: h = json.loads(h['message'])
            if not isinstance(h,dict) or 'modelCount' not in h: continue
            assert h['modelCount'] == h['visualCount'] and not any(h[k] for k in ('missing','unexpected','unexpectedPileIcons'))
            hands += 1
        diagnostics = [line for line in (folder/'godot.log').read_text(encoding='utf8',errors='replace').splitlines()
            if re.search(r'ERROR:|SCRIPT ERROR|SHADER ERROR|Exception',line)]
        assert not diagnostics, diagnostics
        monitor = load(folder/'window-monitor.json')
        assert monitor['foreground_samples'] == monitor['restored_samples'] == 0
        revised.append(dict(branch=branch,passed=True,modes=checks,lethalAndNextRoom=True,handChecks=hands,
            captureBuild=capture['captureBuild'],captureFps=capture['capture_fps'],repeatedFrames=capture['frames_repeated'],
            droppedFrames=capture['frames_dropped'],movieSha256=sha(scene/'game.mp4'),logDiagnostics=diagnostics))
    summary['mahiruTvRevision'] = revised

# Local main advanced during the task; recheck all four affected scenes after synchronization.
synced_root = root/'synced'
if (synced_root/'after/stable/polish-jealousy/recording.json').exists():
    synchronized = []
    for branch in ('stable', 'beta'):
        folder = synced_root/'after'/branch
        build = dict(assemblySha256=sha(root/'synced-package/lib'/f'ShoujoKagekiAijoKaren.{branch.title()}.dll'),
            packSha256=sha(root/'synced-package/ShoujoKagekiAijoKaren.pck'))
        scenes = []
        for name in ('jealousy','wake','arrogant','relay'):
            scene = folder/('polish-'+name)
            observed = load(scene/'polish-status.json')
            assert observed['live'] == observed['back'] == 0 and all(n['exited'] and n['frames'] > 0 for n in observed['nodes'])
            before, after = load(scene/'recording-before.json'), load(scene/'recording-after.json')
            if name == 'jealousy': assert before['battle']['enemies'][0]['hp'] - after['battle']['enemies'][0]['hp'] == 35
            if name == 'wake': assert len(after['player']['hand']) == 5 and after['player']['exhaust_pile_count'] == 1
            if name in ('arrogant','relay'): assert observed['passion'] == observed['relicRewards'] == 2
            capture = load(scene/'recording.json')
            assert capture['captureBuild'] == build and capture['status'] == 'completed' and capture['audio_source'] == 'game_process'
            scenes.append(dict(name=name,passed=True,captureFps=capture['capture_fps'],movieSha256=sha(scene/'game.mp4')))
        hands = 0
        for p in folder.rglob('*hand*.json'):
            h = load(p)
            if isinstance(h,dict) and 'message' in h: h = json.loads(h['message'])
            if not isinstance(h,dict) or 'modelCount' not in h: continue
            assert h['modelCount'] == h['visualCount'] and not any(h[k] for k in ('missing','unexpected','unexpectedPileIcons'))
            hands += 1
        diagnostics = [line for line in (folder/'godot.log').read_text(encoding='utf8',errors='replace').splitlines()
            if re.search(r'ERROR:|SCRIPT ERROR|SHADER ERROR|Exception',line)]
        assert not diagnostics, diagnostics
        monitor = load(folder/'window-monitor.json')
        assert monitor['foreground_samples'] == monitor['restored_samples'] == 0
        synchronized.append(dict(branch=branch,main='04ed9bd75ae041afc0307740f1f6cf4fda817d72',captureBuild=build,
            scenes=scenes,handChecks=hands,logDiagnostics=diagnostics))
    summary['synchronizedMain'] = synchronized

(root/'verification.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('PASS',[(r['version'],r['branch'],r['handChecks']) for r in summary['runs']])
print('LOG DIAGNOSTICS',[(r['version'],r['branch'],r['logDiagnostics']) for r in summary['runs']])
