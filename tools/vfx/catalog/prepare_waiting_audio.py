"""Render original light-curtain cues and an audition; no recorded audio is used in new assets."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import wave

import numpy as np

ROOT = Path(__file__).resolve().parents[3]
RATE = 44100
parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
out = args.output.resolve()
out.mkdir(parents=True, exist_ok=True)
assets = ROOT / 'ShoujoKagekiAijoKaren/audio'


def db(value):
    return float(20 * np.log10(max(float(value), 1e-12)))


def decode(path):
    data = subprocess.check_output(['ffmpeg', '-v', 'error', '-i', str(path),
                                    '-ar', str(RATE), '-ac', '2', '-f', 'f32le', '-'])
    return np.frombuffer(data, dtype='<f4').reshape(-1, 2).astype(float)


def describe(path, samples, gain):
    return dict(file=path.name, duration=len(samples) / RATE, peakDb=db(abs(samples).max()),
                rmsDb=db(np.sqrt(np.mean(samples ** 2))), gain=gain,
                preBusPeakDb=db(abs(samples).max() * gain),
                preBusRmsDb=db(np.sqrt(np.mean(samples ** 2)) * gain),
                clippedSamples=int(np.count_nonzero(abs(samples) >= 1)),
                sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def delay(signal, seconds):
    count = round(seconds * RATE)
    return np.concatenate([np.zeros(count), signal])[:len(signal)]


def air(length, seed):
    rng = np.random.default_rng(seed)
    frequencies = np.fft.rfftfreq(length, 1 / RATE)
    # Silk-like air: no sub-bass, restrained hiss, softened octave balance.
    shape = (1 - np.exp(-(frequencies / 520) ** 4)) * np.exp(-(frequencies / 5100) ** 4)
    shape /= np.sqrt(np.maximum(frequencies, 180))
    noise = np.fft.irfft(np.fft.rfft(rng.standard_normal(length)) * shape, n=length)
    return noise / np.sqrt(np.mean(noise ** 2))


def glint(t, start, hz, decay):
    age = np.maximum(0, t - start)
    envelope = (1 - np.exp(-age / .009)) * np.exp(-age * decay)
    result = np.zeros_like(t)
    for ratio, weight in ((1, 1), (2.006, .20), (2.73, .065), (3.92, .018)):
        result += weight * np.sin(2 * np.pi * hz * ratio * age) * np.exp(-age * decay * (ratio - 1) * .22)
    return result * envelope


def save(name, mono, peak_db, gain):
    # Very short, quiet early reflections give space without a long reverb tail.
    stereo = np.column_stack([
        mono + .10 * delay(mono, .027) + .035 * delay(mono, .073),
        mono + .085 * delay(mono, .039) + .035 * delay(mono, .061)])
    fade = min(round(.035 * RATE), len(stereo) // 5)
    stereo[:round(.006 * RATE)] *= np.linspace(0, 1, round(.006 * RATE))[:, None]
    stereo[-fade:] *= np.linspace(1, 0, fade)[:, None]
    stereo *= 10 ** (peak_db / 20) / np.max(abs(stereo))
    stereo[0] = stereo[-1] = 0
    path = assets / name
    with wave.open(str(path), 'wb') as stream:
        stream.setparams((2, 2, RATE, 0, 'NONE', 'not compressed'))
        stream.writeframes(np.round(stereo * 32767).astype('<i2').tobytes())
    return describe(path, decode(path), gain)


new = []
t = np.arange(round(.94 * RATE)) / RATE
envelope = (1 - np.exp(-t / .065)) * np.exp(-((t - .23) / .22) ** 2)
rise = .095 * air(len(t), 16101) * envelope
rise += .12 * glint(t, .16, 1046.5, 6.8) + .025 * glint(t, .29, 1570.8, 9.0)
rise += .017 * np.sin(2 * np.pi * (690 * t + 170 * t * t)) * envelope
new.append(save('karen_waiting_curtain_rise.wav', rise, -6.5, .40))

t = np.arange(round(.38 * RATE)) / RATE
envelope = (1 - np.exp(-t / .018)) * np.exp(-t * 12)
pulse = .065 * air(len(t), 16102) * envelope + .12 * glint(t, .018, 1046.5, 13.5)
new.append(save('karen_waiting_curtain_pulse.wav', pulse, -8.0, .34))

t = np.arange(round(.22 * RATE)) / RATE
arrival = .15 * glint(t, 0, 1569.8, 22) + .025 * glint(t, .006, 2093, 27)
new.append(save('karen_waiting_energy_arrive.wav', arrival, -10.0, .26))

old = [describe(assets / file, decode(assets / file), gain) for file, gain in (
    ('karen_entrance_stage_light.wav', .24), ('karen_stage_light.mp3', .13),
    ('karen_story_energy.wav', .14))]
report = dict(source='Original deterministic NumPy synthesis; no recorded samples', rate=RATE,
              channels=2, format='PCM S16LE', baseline=old, cues=new)
(out / 'audio-design.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
audition = np.concatenate([part for cue in new for part in (
    decode(assets / cue['file']) * cue['gain'], np.zeros((round(.38 * RATE), 2)))])
with wave.open(str(out / 'new-cues-preview.wav'), 'wb') as stream:
    stream.setparams((2, 2, RATE, 0, 'NONE', 'not compressed'))
    stream.writeframes(np.round(audition * 32767).astype('<i2').tobytes())
print(json.dumps(report, indent=2))
