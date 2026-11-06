"""Check F1 onset against the visible burst in 1280x720, 30 fps MCP fuel captures.

Run after catalog/record.py --scenes fuel,replay for stable and beta. This
uses real video and process audio, not model timestamps or playback logs.
"""
from pathlib import Path
import argparse
import json
import subprocess

import numpy as np


parser = argparse.ArgumentParser()
parser.add_argument('recordings', type=Path)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
repo = Path(__file__).resolve().parents[3]
rate = 16000


def audio(path):
    raw = subprocess.check_output(['ffmpeg', '-v', 'error', '-i', str(path), '-vn',
        '-af', 'highpass=f=80,lowpass=f=6500', '-ac', '1', '-ar', str(rate),
        '-f', 'f32le', 'pipe:1'])
    return np.frombuffer(raw, dtype='<f4').astype(float)


template = audio(repo / 'ShoujoKagekiAijoKaren/audio/karen_fuel_ignite.wav')[:4000]
results = []
for branch in ('stable', 'beta'):
    folder = args.recordings / branch / 'fuel'
    capture = json.loads((folder / 'recording.json').read_text(encoding='utf-8'))
    assert (capture['width'], capture['height'], capture['fps']) == (1280, 720, 30)
    assert capture['status'] == 'completed' and capture['audio_source'] == 'game_process'
    video = folder / 'game.mp4'
    raw = subprocess.check_output(['ffmpeg', '-v', 'error', '-i', str(video),
        '-an', '-vf', 'crop=240:310:520:140', '-f', 'rawvideo', '-pix_fmt', 'rgb24', 'pipe:1'])
    pixels = np.frombuffer(raw, dtype=np.uint8).reshape(-1, 310, 240, 3).astype(int)
    r, g, b = pixels[..., 0], pixels[..., 1], pixels[..., 2]
    # Cyan played-card outline disappears at the visible burst. The orange
    # charge flash and dispersed particles do not satisfy this color mask.
    counts = ((g > 150) & (b > 140) & (g > r + 40) & (b > r + 30)).sum(axis=(1, 2))
    edges = np.diff((counts > 300).astype(int), prepend=0)
    starts, ends = np.flatnonzero(edges == 1), np.flatnonzero(edges == -1)
    assert len(starts) == len(ends) == 1, (branch, starts, ends)
    assert ends[0] - starts[0] >= 5, 'No stable visible card before burst'
    burst = float(ends[0] / 30)

    recorded = audio(video)
    size = 1 << (len(recorded) + len(template) - 1).bit_length()
    cross = np.fft.irfft(np.fft.rfft(recorded, size) * np.conj(np.fft.rfft(template, size)), size)
    cross = cross[:len(recorded) - len(template) + 1]
    energy = np.concatenate(([0], np.cumsum(recorded * recorded)))
    norm = np.sqrt(np.maximum(energy[len(template):] - energy[:-len(template)], 1e-14) * np.sum(template * template))
    scores = cross / norm
    index = int(np.argmax(scores))
    assert scores[index] >= .45, 'No reliable F1 match in the recorded audio'
    onset = index / rate
    offset = onset - burst
    result = dict(branch=branch, visualBurst=burst, audioOnset=onset,
        correlation=float(scores[index]), audioMinusVisualMs=round(offset * 1000, 2),
        toleranceMs=67, passed=abs(offset) <= .067)
    results.append(result)
    print(json.dumps(result), flush=True)

args.output.parent.mkdir(parents=True, exist_ok=True)
args.output.write_text(json.dumps(results, indent=2), encoding='utf-8')
assert all(x['passed'] for x in results), 'F1 attack is not aligned with the visible burst (67 ms tolerance)'
