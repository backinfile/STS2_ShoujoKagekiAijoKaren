"""Original, brief rising confirmation for Passion buff acquisition; no source samples."""
import math
from pathlib import Path
import random
import struct
import wave

RATE = 44100
DEST = Path(__file__).resolve().parents[3] / 'ShoujoKagekiAijoKaren/audio'
rng = random.Random(1927)
samples = []
for i in range(round(RATE * .32)):
    t = i / RATE
    attack = min(1, t / .008)
    tones = sum(math.sin(math.tau * hz * t) * level for hz, level in [(1046.5, .44), (1568, .20), (2093, .08)])
    air = rng.uniform(-1, 1) * .04 * math.exp(-t * 50)
    envelope = attack * math.exp(-t * 12) * min(1, (.32 - t) / .045)
    samples.append(envelope * (tones + air))
gain = .62 * 32767 / max(abs(x) for x in samples)
with wave.open(str(DEST / 'karen_passion_gain.wav'), 'wb') as audio:
    audio.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
    audio.writeframes(struct.pack('<' + 'h' * len(samples), *(round(x * gain) for x in samples)))
