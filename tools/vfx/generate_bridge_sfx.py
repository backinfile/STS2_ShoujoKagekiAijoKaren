"""Generate original, short stage-mechanism cues; no samples from the TV soundtrack."""
import math
import pathlib
import random
import struct
import wave

ROOT = pathlib.Path(__file__).resolve().parents[2]
RATE = 44100
rng = random.Random(87012)


def save(name, values, peak):
    maximum = max(abs(x) for x in values)
    samples = [round(x / maximum * peak * 32767) for x in values]
    with wave.open(str(ROOT / "ShoujoKagekiAijoKaren/audio" / name), "wb") as audio:
        audio.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        audio.writeframes(struct.pack("<" + "h" * len(samples), *samples))


slide = []
filtered = 0.0
for i in range(round(RATE * 0.65)):
    t = i / RATE
    filtered = filtered * 0.90 + rng.uniform(-1, 1) * 0.10
    envelope = min(t / 0.065, 1) * min(max((0.65 - t) / 0.16, 0), 1)
    roller = math.sin(math.tau * (92 * t + 43 * t * t))
    rattle = (0.5 + 0.5 * math.sin(math.tau * 38 * t)) ** 6
    slide.append(envelope * (0.29 * roller + 0.55 * filtered + 0.1 * rattle * filtered))
save("karen_bridge_slide.wav", slide, 0.58)

lock = []
for i in range(round(RATE * 0.26)):
    t = i / RATE
    attack = min(t / 0.002, 1)
    body = 0.65 * math.sin(math.tau * 115 * t) * math.exp(-t * 30)
    metal = sum(math.sin(math.tau * hz * t) * gain for hz, gain in [(630, .18), (943, .13), (1427, .07)])
    strike = rng.uniform(-1, 1) * math.exp(-t * 140) * 0.18
    lock.append(attack * (body + metal * math.exp(-t * 23) + strike) * min((.26 - t) / .035, 1))
save("karen_bridge_lock.wav", lock, 0.62)
