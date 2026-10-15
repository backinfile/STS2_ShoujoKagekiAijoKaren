"""Original satin flutter and warm wax-stamp cues, synthesized without source audio."""
import math
from pathlib import Path
import random
import struct
import wave

RATE = 44100
DEST = Path(__file__).resolve().parents[3] / 'ShoujoKagekiAijoKaren/audio'
rng = random.Random(1927)


def save(name, samples, peak):
    gain = peak * 32767 / max(abs(x) for x in samples)
    with wave.open(str(DEST / name), 'wb') as audio:
        audio.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        audio.writeframes(struct.pack('<' + 'h' * len(samples), *(round(x * gain) for x in samples)))


silk = []
filtered = previous = 0
for i in range(round(RATE * .67)):
    t = i / RATE
    previous = filtered
    filtered = .87 * filtered + .13 * rng.uniform(-1, 1)
    flutter = .65 + .35 * math.sin(math.tau * (13 * t + 9 * t * t))
    envelope = min(1, t / .07) * max(0, 1 - t / .67) ** 1.7
    silk.append(envelope * flutter * (filtered - previous * .70))
save('karen_passion_silk.wav', silk, .55)

stamp = []
for i in range(round(RATE * .57)):
    t = i / RATE
    attack = min(1, t / .002)
    impact = (.65 * math.sin(math.tau * 136 * t) + rng.uniform(-1, 1) * .13) * math.exp(-t * 48)
    chime = sum(math.sin(math.tau * hz * t) * amplitude for hz, amplitude in [(784, .18), (1176, .10), (1568, .055)])
    stamp.append(attack * (impact + chime * math.exp(-t * 9)) * min(1, (.57 - t) / .06))
save('karen_passion_stamp.wav', stamp, .70)
