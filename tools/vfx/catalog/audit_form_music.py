"""Locate the Form track in real capture audio using normalized sample matching."""
import argparse
import json
from pathlib import Path
import subprocess

import numpy as np
from assemble import LATEST_ENTRIES

p = argparse.ArgumentParser()
p.add_argument('--output', type=Path, required=True)
p.add_argument('--report', type=Path, required=True)
p.add_argument('--movie', type=Path, help='Audit the assembled movie using its manifest chapter ranges')
p.add_argument('--assert-clean', action='store_true')
a = p.parse_args()
repo = Path(__file__).resolve().parents[3]

def decode(path):
    r = subprocess.run(['ffmpeg', '-v', 'error', '-i', str(path), '-ac', '1',
                        '-ar', '8000', '-f', 'f32le', '-'], capture_output=True, check=True)
    data = np.frombuffer(r.stdout, dtype='<f4').astype(float)
    return data - data.mean()

reference = decode(repo / 'ShoujoKagekiAijoKaren/audio/Karen_form.MP3')
n = 16000
size = 1 << ((len(reference) + n - 1).bit_length())
reference_fft = np.fft.rfft(reference, size)
energy = np.cumsum(np.r_[0, reference ** 2])
energy = energy[n:] - energy[:-n]
# These scenes intentionally apply Form and retain its combat-long replacement.
allowed = {'normal-modes', 'normal-form-turn', 'allstack-normal', 'void-modes',
           'void-form-turn', 'void-fuel-stack', 'allstack-void', 'newstack-normal', 'newstack-void',
           'form-music-lifecycle'}
results = []
catalog = {name for name, _ in LATEST_ENTRIES}
if a.movie:
    master = decode(a.movie)
    manifest = json.loads((a.output / 'manifest.json').read_text(encoding='utf-8'))
    captures = ((e['name'], master[round(e['start']*8000):round(e['end']*8000)]) for e in manifest)
else:
    captures = ((path.parent.name, decode(path)) for path in sorted((a.output / 'stable').glob('*/game.mp4'))
                if path.parent.name in catalog)
for name, samples in captures:
    matches = []
    for start in sorted(set(round(t) for t in np.linspace(0, max(0, len(samples)-n), 5))):
        window = samples[start:start+n]
        if len(window) < n:
            continue
        corr = np.fft.irfft(reference_fft * np.conj(np.fft.rfft(window, size)), size)[:len(energy)]
        scores = corr / np.sqrt(np.maximum(energy * np.sum(window ** 2), 1e-24))
        peak = int(np.argmax(scores))
        matches.append(dict(clipTime=start/8000, referenceTime=peak/8000, score=float(scores[peak])))
    score = max((m['score'] for m in matches), default=0)
    item = dict(scene=name, expectedForm=name in allowed,
                detected=score >= .55, score=score, matches=matches)
    results.append(item)
    print(f"{item['scene']}: {score:.3f} {'FORM' if item['detected'] else 'clear'}", flush=True)
unexpected = [r['scene'] for r in results if r['detected'] and not r['expectedForm']]
missing = [r['scene'] for r in results if r['expectedForm'] and not r['detected']]
a.report.parent.mkdir(parents=True, exist_ok=True)
a.report.write_text(json.dumps(dict(unexpected=unexpected, missingExpected=missing, scenes=results), indent=2), encoding='utf-8')
print('UNEXPECTED', unexpected)
print('MISSING EXPECTED', missing)
if a.assert_clean:
    assert not unexpected, unexpected
    assert not missing, missing
