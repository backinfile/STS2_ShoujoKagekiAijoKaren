"""Original short glass/bell accents; deterministic synthesis, no sampled music/voice."""
import math
from pathlib import Path
import random
import struct
import wave

root = Path(__file__).resolve().parents[3] / 'ShoujoKagekiAijoKaren/audio'
rate = 44100

def render(name, length, notes):
    rng = random.Random(99)
    samples = []
    for i in range(round(length * rate)):
        t = i / rate
        value = 0
        for at, frequency, volume, decay in notes:
            q = t - at
            if q < 0:
                continue
            env = min(1, q / .008) * math.exp(-q / decay)
            value += volume * env * (math.sin(math.tau * frequency * q)
                + .23 * math.sin(math.tau * frequency * 2.003 * q)
                + .08 * math.sin(math.tau * frequency * 4.01 * q))
        value += (rng.random() - .5) * .012 * math.sin(math.pi * min(t / length, 1)) ** 2
        value *= min(1, (length - t) / .05)
        samples.append(value)
    peak = max(abs(x) for x in samples)
    with wave.open(str(root / name), 'wb') as out:
        out.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
        out.writeframes(b''.join(struct.pack('<h', round(x / peak * .65 * 32767)) for x in samples))

render('karen_star_gather.wav', .78, [(0, 523.25, .2, .24), (.11, 783.99, .2, .25), (.24, 1046.5, .19, .26), (.38, 1567.98, .1, .22)])
render('karen_star_step.wav', .22, [(0, 1318.51, .18, .055), (.008, 1975.53, .07, .07)])
render('karen_star_resolve.wav', .75, [(0, 1046.5, .2, .22), (.035, 1318.51, .17, .24), (.07, 1567.98, .13, .29), (.09, 2093, .07, .25)])
