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
parser.add_argument('--full-catalog', action='store_true')
args = parser.parse_args()
repo = pathlib.Path(__file__).resolve().parents[3]
sys.path.insert(0, str(repo / 'tools/vfx/six-card'))
from audit_log import audit
from assemble import CURRENT_ENTRIES

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
    assert report['stages'] == report['states'] == 1, (path, report)
    assert abs(report['eventWeight'] - .72) < .002 and abs(report['otherOwnerWeight'] - .72) < .002, (path, report)
    if path.parent.name.startswith('newstack'):
        assert abs(report['ambient'] - .4176) < .002, (path, report)
    holds.append(str(path.relative_to(folder)))

package = repo / 'artifacts/ShoujoKagekiAijoKaren'
expected = dict(assemblySha256=hashlib.sha256((package / 'lib' / f'ShoujoKagekiAijoKaren.{args.branch.title()}.dll').read_bytes()).hexdigest(),
                packSha256=hashlib.sha256((package / 'ShoujoKagekiAijoKaren.pck').read_bytes()).hexdigest())
recordings = []
names = [n for n, _ in CURRENT_ENTRIES] if args.full_catalog else ['starlight-act2', 'newstack-normal', 'newstack-void', 'stage-lifecycle']
for name in names:
    scene = folder / name
    rec = read(scene / 'recording.json')
    assert rec['status'] == 'completed' and rec['audio_source'] == 'game_process' and rec['audio'], scene
    assert rec['captureBuild'] == expected, (scene, 'Not the final package')
    assert rec['capture_fps'] >= 20, (scene, rec['capture_fps'])
    assert (scene / 'recording-before.png').is_file() and (scene / 'recording-after.png').is_file(), scene
    if name != 'lastword':
        final = read(scene / 'final-state.json')
        assert final['identitiesMatch'] and final['flightModelMismatchFrames'] == 0 and final['anchorError'] < .1, (scene, final)
    if name in ['starlight-act2', 'newstack-normal', 'newstack-void']:
        observed = read(scene / 'stage-probe.json')
        hovers = [c for c in observed['casts'] if c['kind'] == 'Act2Hover' and c['ownedFrames'] > 0]
        assert len(hovers) == 1 and hovers[0]['playIndices'] == [0, 1, 2], (scene, observed)
        assert hovers[0]['backCorrect'], (scene, observed)
    recordings.append(dict(name=name, captureFps=rec['capture_fps'], repeatedFrames=rec['frames_repeated']))

logs = [folder / 'godot.log', *sorted(root.glob(args.branch + '-run-*-godot.log'))]
diagnostics = dict(knownStartupWorkerPoolErrors=[], unexpectedErrors=[], files=[])
for path in logs:
    result = audit(path.read_text(encoding='utf-8', errors='replace'))
    diagnostics['files'].append(str(path.relative_to(root)))
    for key in ['knownStartupWorkerPoolErrors', 'unexpectedErrors']:
        diagnostics[key].extend(dict(file=str(path.relative_to(root)), **entry) for entry in result[key])
assert not diagnostics['unexpectedErrors'], diagnostics
report = dict(branch=args.branch, scenes=len(names), handChecks=len(checks), cleanupChecks=len(cleanup), heldChecks=len(holds),
              captureBuild=expected, recordings=recordings, log=diagnostics,
              scope='Targeted singleplayer VFX; no full singleplayer/multiplayer matrix; team-footlight card injected into singleplayer only')
(folder / 'runtime-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k: v for k, v in report.items() if k != 'recordings'}, ensure_ascii=False))
