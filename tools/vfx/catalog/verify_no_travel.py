"""Check real depletion traces: fixed gather, immediate release, preserved icon feedback."""
import argparse
import json
import math
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--branch', choices=['stable', 'beta'], required=True)
parser.add_argument('--scenes', default='shine-motion,shine-speeds,starlight-act2')
args = parser.parse_args()
root = args.output.resolve() / args.branch
read = lambda p: json.loads(p.read_text(encoding='utf-8'))
results = []
for name in args.scenes.split(','):
    folder = root / name
    motion = read(folder / 'shine-motion.json')
    assert motion['depletes'], (name, 'No real depletion observed')
    assert motion['liveDepletes'] == motion['liveArrivals'] == 0, name
    counter = motion['counter']
    assert counter['earlyFrames'] == counter['pending'] == 0, (name, counter)
    assert counter['displayed'] == counter['target'] == counter['model'], (name, counter)
    assert motion['pileVisualRestoreError'] < .001, name
    for d in motion['depletes']:
        samples = d['samples']
        assert d['duration'] == 0 and d.get('hasFlight') is False, (name, 'Middle flight remains', d['duration'])
        assert samples and max(math.hypot(s['x']-samples[0]['x'], s['y']-samples[0]['y']) for s in samples) < .1, name
        assert d['released'] and not d['faceReappeared'], (name, d)
        # Instant can expose only one intermediate frame before QueueFree.
        # Its final 0.1 scale is set and released in the same process tick.
        assert d['gatherDisplacement'] < .1 and d['minCardScale'] < (.6 if d['gather'] <= .07 else .25), (name, d)
        assert 0 <= d['completionAge'] - d['gather'] < .10, (name, 'Hidden wait or early release', d)
    assert sum(a['count'] for a in motion['arrivals']) == len(motion['depletes']), (name, motion['arrivals'])
    assert all(a['maxScale'] > 1.05 and a['minScale'] < .99 for a in motion['arrivals']), (name, 'Icon feedback missing')
    revision = read(folder / 'revision-probe.json')
    assert revision['missingPlayFrames'] == revision['falseWarningFrames'] == 0, name
    for path in folder.glob('*hand.json'):
        hand = read(path)
        if 'message' in hand:
            hand = json.loads(hand['message'])
        assert hand['modelCount'] == hand['visualCount'] and not any(hand[k] for k in ['missing', 'unexpected', 'unexpectedPileIcons']), path
    assert all((folder / f'recording-{phase}.png').exists() for phase in ['before', 'after']), name
    results.append(dict(scene=name, depletions=len(motion['depletes']), gathers=[d['gather'] for d in motion['depletes']],
                        maxReleaseDelay=max(d['completionAge']-d['gather'] for d in motion['depletes'])))
report = dict(branch=args.branch, scenes=results)
(root / 'no-travel-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(report, ensure_ascii=False))
