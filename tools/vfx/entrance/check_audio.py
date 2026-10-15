"""Verify the actual lamp sample is recognizable and above the surrounding recording mix."""
import argparse
import json
from pathlib import Path
import subprocess
import numpy as np

parser = argparse.ArgumentParser()
parser.add_argument('recording', type=Path)
parser.add_argument('--source', type=Path, default=Path('ShoujoKagekiAijoKaren/audio/karen_entrance_stage_light.wav'))
parser.add_argument('--output', type=Path)
args = parser.parse_args()

def decode(path):
    result = subprocess.run(['ffmpeg', '-v', 'error', '-i', str(path), '-ac', '1', '-ar', '16000',
                             '-f', 'f32le', '-'], capture_output=True, check=True)
    samples = np.frombuffer(result.stdout, dtype='<f4').astype(float)
    return samples - samples.mean()

source, recording = decode(args.source), decode(args.recording)
assert len(recording) >= len(source) + 2 and np.sum(source ** 2) > 1e-12
best = None
# Native playback and Windows process capture resample on separate clocks.
# A 50 ppm discrepancy shifts noise transients by almost one sample/second
# at 16 kHz. Fit only +/-100 ppm (0.1 ms/s), keeping the level thresholds.
for time_scale in np.linspace(.9999, 1.0001, 9):
    reference = np.interp(np.arange(round(len(source)*time_scale))/time_scale, np.arange(len(source)), source)
    n = len(reference)
    size = 1 << ((len(recording) + n - 1).bit_length())
    correlation = np.fft.irfft(np.fft.rfft(recording, size) * np.conj(np.fft.rfft(reference, size)), size)[:len(recording)-n+1]
    sums = np.cumsum(np.r_[0, recording ** 2])
    energy = sums[n:] - sums[:-n]
    scores = correlation / np.sqrt(np.sum(reference ** 2) * np.maximum(energy, 1e-18))
    i = int(np.argmax(scores))
    if best is None or scores[i] > best[0]:
        best = (float(scores[i]), i, reference, float(correlation[i]/np.sum(reference**2)), float(time_scale))
score, i, reference, gain, time_scale = best
fitted = reference * gain
residual = recording[i:i+len(reference)] - fitted
result = dict(score=score, startSeconds=i/16000, sampleGain=gain, referenceTimeScale=time_scale,
              matchedPeakDbfs=float(20*np.log10(max(np.max(np.abs(fitted)), 1e-12))),
              signalToResidualDb=float(10*np.log10(max(np.sum(fitted**2),1e-18)/max(np.sum(residual**2),1e-18))))
if args.output:
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2), encoding='utf-8')
print(json.dumps(result))
assert result['score'] >= .65, 'Lamp sample is not reliably verified; unrelated game audio is insufficient evidence'
assert result['signalToResidualDb'] >= 0, 'Lamp sample is below the surrounding mix'
