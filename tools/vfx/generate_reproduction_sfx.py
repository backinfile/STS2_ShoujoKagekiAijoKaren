"""Original paper/rail motion and a warm stage bell, using no recorded samples."""
import math
import pathlib
import random
import struct
import wave

ROOT = pathlib.Path(__file__).resolve().parents[2]
RATE = 44100


def cue(name, length, peak, entrance):
    rng = random.Random(23017)
    filtered = 0.0
    samples = []
    for i in range(round(RATE * length)):
        t = i / RATE
        filtered = filtered * 0.82 + rng.uniform(-1, 1) * 0.18
        envelope = min(t / 0.09, 1) * min((length - t) / 0.24, 1)
        flutter = (0.5 + 0.5 * math.sin(math.tau * (17 * t + 8 * t * t))) ** 7
        rail = math.sin(math.tau * (105 * t + 25 * t * t)) * 0.035
        tone_age = t - (1.12 / 3 if entrance else 0.21)
        bell = 0
        if tone_age > 0:
            for hz, gain in ((660, 0.20), (990, 0.08), (1327, 0.055)):
                bell += math.sin(math.tau * hz * tone_age) * gain * math.exp(-tone_age * 6)
            bell *= min(tone_age / 0.003, 1)
        samples.append(envelope * (filtered * (0.17 + flutter * 0.48) + rail + bell))
    maximum = max(abs(v) for v in samples)
    data = [round(v / maximum * peak * 32767) for v in samples]
    with wave.open(str(ROOT / "ShoujoKagekiAijoKaren/audio" / name), "wb") as output:
        output.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        output.writeframes(struct.pack("<" + "h" * len(data), *data))


cue("karen_reproduction.wav", 2.75 / 3, 0.53, True)
cue("karen_reproduction_feed.wav", 0.34, 0.38, False)
