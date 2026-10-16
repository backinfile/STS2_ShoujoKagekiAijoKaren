"""Original airy lift and glass crossing cue; no sampled third-party audio."""
from pathlib import Path
import wave
import numpy as np

root = Path(__file__).resolve().parents[3] / 'ShoujoKagekiAijoKaren/audio'
rate = 44100
rng = np.random.default_rng(29617)

def air(length):
    n = rng.normal(0, 1, length)
    spectrum = np.fft.rfft(n)
    frequency = np.fft.rfftfreq(length, 1 / rate)
    spectrum *= np.exp(-((frequency - 2400) / 2100) ** 2) * (1 - np.exp(-frequency / 500))
    out = np.fft.irfft(spectrum, n=length)
    return out / max(np.std(out), .001)

def save(name, signal):
    signal *= .57 / max(np.max(np.abs(signal)), .001)
    with wave.open(str(root / name), 'wb') as stream:
        stream.setnchannels(2); stream.setsampwidth(2); stream.setframerate(rate)
        stream.writeframes(np.asarray(np.clip(signal, -1, 1) * 32767, dtype='<i2').tobytes())

lift_duration = .24
t = np.arange(int(rate * lift_duration)) / rate
envelope = np.sin(np.pi * t / lift_duration) ** 1.7
breath = air(len(t)) * .21 * envelope
phase = 2 * np.pi * (780 * t + 1850 * t ** 2)
glass = (np.sin(phase) + .32 * np.sin(phase * 1.501)) * envelope * .06
pan = np.linspace(.2, .8, len(t))
save('karen_fate_lift.wav', np.stack([(breath + glass) * np.sqrt(1 - pan), (np.roll(breath, 123) + glass) * np.sqrt(pan)], axis=1))

cross_duration = .42
t = np.arange(int(rate * cross_duration)) / rate
bell = np.zeros_like(t)
for frequency, amplitude, decay in [(1046.5, .46, 6), (1568, .25, 8), (2093, .15, 11), (2810, .07, 15), (4186, .05, 18)]:
    bell += np.sin(2 * np.pi * frequency * t + .12) * amplitude * np.exp(-decay * t)
attack = 1 - np.exp(-t * 700)
bell *= attack * (1 - (t / cross_duration) ** 2)
breath = air(len(t)) * .045 * np.exp(-t * 12) * attack
left = bell + breath
right = bell * .88 + np.roll(bell, 207) * .10 + np.roll(breath, 81)
save('karen_fate_cross.wav', np.stack([left, right], axis=1))
