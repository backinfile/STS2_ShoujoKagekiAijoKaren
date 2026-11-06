"""Audit actual captured state, node cleanup, replay ownership and current package provenance."""
import argparse
import hashlib
import json
import pathlib
import sys

sys.stdout.reconfigure(encoding='utf-8')
parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True, type=pathlib.Path)
parser.add_argument('--branch', choices=['stable', 'beta'], required=True)
parser.add_argument('--package', type=pathlib.Path, help='Exact package used by the capture; defaults to the current build')
parser.add_argument('--full-catalog', action='store_true')
parser.add_argument('--catalog', choices=['current','latest'], default='current')
parser.add_argument('--scenes', help='Comma-separated targeted scenes; defaults to the four stage compatibility scenes')
args = parser.parse_args()
repo = pathlib.Path(__file__).resolve().parents[3]
sys.path.insert(0, str(repo / 'tools/vfx/six-card'))
from audit_log import audit
from assemble import CURRENT_ENTRIES, LATEST_ENTRIES

root = args.output.resolve()
folder = root / args.branch
read = lambda p: json.loads(p.read_text(encoding='utf-8'))
checks = []
for path in folder.rglob('*hand.json'):
    hand = read(path)
    if 'message' in hand:
        hand = json.loads(hand['message'])
    assert hand['modelCount'] == hand['visualCount'], path
    assert not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons'], path
    checks.append(str(path.relative_to(folder)))

cleanup = []
for path in folder.rglob('*-composition.json'):
    report = read(path)
    assert all(report[k] == 0 for k in ['registered', 'states', 'stages', 'benefits', 'back', 'hud']), (path, report)
    assert report['eventWeight'] == report['otherOwnerWeight'] == 1, (path, report)
    cleanup.append(str(path.relative_to(folder)))

holds = []
for path in folder.rglob('*-held.json'):
    report = read(path)
    if 'counter' in report:
        # count-held.json belongs to the independent depletion-count fixture.
        continue
    assert report['stages'] == report['states'] == 1, (path, report)
    assert abs(report['eventWeight'] - .72) < .002 and abs(report['otherOwnerWeight'] - .72) < .002, (path, report)
    if path.parent.name.startswith('newstack'):
        assert abs(report['ambient'] - .4176) < .002, (path, report)
    holds.append(str(path.relative_to(folder)))

package = args.package.resolve() if args.package else repo / 'artifacts/ShoujoKagekiAijoKaren'
expected = dict(assemblySha256=hashlib.sha256((package / 'lib' / f'ShoujoKagekiAijoKaren.{args.branch.title()}.dll').read_bytes()).hexdigest(),
                packSha256=hashlib.sha256((package / 'ShoujoKagekiAijoKaren.pck').read_bytes()).hexdigest())
recordings = []
if args.scenes:
    names = args.scenes.split(',')
elif args.full_catalog:
    names = [n for n, _ in (LATEST_ENTRIES if args.catalog=='latest' else CURRENT_ENTRIES)]
else:
    names = ['starlight-act2', 'newstack-normal', 'newstack-void', 'stage-lifecycle']
for name in names:
    scene = folder / name
    rec = read(scene / 'recording.json')
    assert rec['status'] == 'completed' and rec['audio_source'] in ('game_process','engine_mix') and rec['audio'], scene
    assert rec['captureBuild'] == expected, (scene, 'Not the final package')
    assert rec['capture_fps'] >= 20, (scene, rec['capture_fps'])
    assert (scene / 'recording-before.png').is_file() and (scene / 'recording-after.png').is_file(), scene
    if name in ['shine-lethal', 'shine-end-flight']:
        counter = read(scene / 'next-combat-counter.json')
        assert counter['liveDepletes'] == counter['liveArrivals'] == 0, counter
        counter = counter['counter']
        assert counter['displayed'] == counter['target'] == counter['model'] and counter['pending'] == 0, counter
    elif name == 'relay-transfer':
        assert read(scene/'result.json')['result']=='pass', scene
    elif name != 'lastword':
        final = read(scene / 'final-state.json')
        assert final['identitiesMatch'] and final['flightModelMismatchFrames'] == 0 and final['anchorError'] < .1, (scene, final)
    if name in ['starlight-act2', 'newstack-normal', 'newstack-void']:
        observed = read(scene / 'stage-probe.json')
        hovers = [c for c in observed['casts'] if c['kind'] == 'Act2Hover' and c['ownedFrames'] > 0]
        assert len(hovers) == 1 and hovers[0]['playIndices'] == [0, 1, 2], (scene, observed)
        assert hovers[0]['backCorrect'], (scene, observed)
    if name == 'compatibility-regressions':
        regressions = read(scene / 'regressions.json')
        assert all(regressions['cardCache'].values()), regressions
        for row in regressions['audio']:
            assert row['buses'] == ['SFX'] * 3, row
            assert all(abs(row[k] - .4) < .001 for k in ['direct', 'ordinary', 'loop']), row
    if name == 'coexistence-regressions':
        regressions = read(scene / 'regressions.json')
        assert len(regressions['composition']) == 11 and len(regressions['music']) == 9, regressions
        if 'powerAnchor' in regressions: assert len(regressions['powerAnchor']) == 10, regressions
        assert all(all(values.values()) for values in regressions.values()), regressions
    if name in ['recent-stack-normal', 'recent-stack-void']:
        observed = read(scene / 'native-finished.json')
        played = {row['id'] for row in observed['finished']}
        required = {'KAREN_POSITION0', 'KAREN_PASSION', 'KAREN_ARROGANT', 'KAREN_JEALOUSY',
                    'KAREN_BACK_TO_BACK_CARD', 'KAREN_SHINE_TOGETHER', 'KAREN_WHY',
                    'KAREN_WAKE_UP', 'KAREN_EXCHANGE_FATE'}
        assert required <= played, (scene, required - played)
        for label, kind in [('why', 'hand_select'), ('wakeup', 'card_select')]:
            assert read(scene / (label + '-selection.json'))['state_type'] == kind, scene
            assert (scene / (label + '-before.png')).is_file() and (scene / (label + '-after.png')).is_file(), scene
    recordings.append(dict(name=name, captureFps=rec['capture_fps'], repeatedFrames=rec['frames_repeated']))

logs = [folder / 'godot.log', *sorted(root.glob(args.branch + '-run-*-godot.log'))]
diagnostics = dict(knownStartupWorkerPoolErrors=[], unexpectedErrors=[], files=[])
for path in logs:
    log = path.read_text(encoding='utf-8', errors='replace')
    result = audit(log)
    if result['knownStartupWorkerPoolErrors']:
        preload = next(i for i, line in enumerate(log.splitlines(), 1) if "Preloading 'Common' Complete:" in line)
        assert all(e['line'] < preload for e in result['knownStartupWorkerPoolErrors']), (path, result)
    diagnostics['files'].append(str(path.relative_to(root)))
    for key in ['knownStartupWorkerPoolErrors', 'unexpectedErrors']:
        diagnostics[key].extend(dict(file=str(path.relative_to(root)), **entry) for entry in result[key])
assert not diagnostics['unexpectedErrors'], diagnostics
window = read(folder / 'window-monitor.json')
assert window['foreground_samples'] == window['restored_samples'] == 0, window
report = dict(branch=args.branch, scenes=len(names), sceneNames=names, handChecks=len(checks), cleanupChecks=len(cleanup), heldChecks=len(holds),
              captureBuild=expected, recordings=recordings, log=diagnostics,
              window=window, scope='Targeted VFX capture, including same-version relay when selected; no full singleplayer/multiplayer matrix or third-party mod coexistence testing')
(folder / 'runtime-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k: v for k, v in report.items() if k != 'recordings'}, ensure_ascii=False))
