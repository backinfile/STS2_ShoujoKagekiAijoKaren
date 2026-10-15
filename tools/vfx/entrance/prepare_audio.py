"""Calibrate the existing stage-light recording without adding any sound layers.

Requires FFmpeg and NumPy. Run from any directory; output is deterministic PCM.
"""
import hashlib
from pathlib import Path
import subprocess
import wave

import numpy as np

audio = Path(__file__).resolve().parents[3] / 'ShoujoKagekiAijoKaren/audio'
source = audio / 'karen_stage_light.mp3'
assert hashlib.sha256(source.read_bytes()).hexdigest() == 'fc4fc0424d4243d0ba993c856efc1e4bded3f77e6db8b7c6e19ffeacde74a70f'
filter_ = ('atrim=start=0.06:end=1.10,asetpts=PTS-STARTPTS,'
           'acompressor=threshold=0.002:ratio=20:attack=0.01:release=0.1:detection=peak:link=maximum')
decoded = subprocess.run(['ffmpeg', '-v', 'error', '-i', str(source), '-af', filter_,
                          '-ar', '44100', '-ac', '2', '-f', 'f32le', '-'],
                         capture_output=True, check=True)
samples = np.frombuffer(decoded.stdout, dtype='<f4').reshape(-1, 2).astype(float)
samples *= 10 ** (-1.5 / 20) / np.max(np.abs(samples))
samples[:220] *= np.linspace(0, 1, 220)[:, None]
samples[-1764:] *= np.linspace(1, 0, 1764)[:, None]
target = audio / 'karen_entrance_stage_light.wav'
with wave.open(str(target), 'wb') as output:
    output.setnchannels(2)
    output.setsampwidth(2)
    output.setframerate(44100)
    output.writeframes((samples * 32767).astype('<i2').tobytes())
print(f'{target.name}: {len(samples)/44100:.6f}s, peak -1.5 dBFS, '
      f'RMS {20*np.log10(np.sqrt(np.mean(samples*samples))):.2f} dBFS; '
      f'{hashlib.sha256(target.read_bytes()).hexdigest()}')
