"""Deterministic quiet glass/water cues; no external recordings or voice samples."""
from pathlib import Path
import math, random, struct, wave
OUT=Path(__file__).resolve().parents[3]/'ShoujoKagekiAijoKaren/audio'
RATE=44100
for name,length,drops in [('open',1.05,9),('drop',.52,3)]:
    rng=random.Random(417 if name=='open' else 419)
    data=[0.0]*int(RATE*length)
    for k in range(drops):
        start=.025+k*(.065 if name=='open' else .060)
        freq=(720 if name=='open' else 960)+rng.random()*620
        for j in range(int(RATE*.38)):
            at=int(start*RATE)+j
            if at>=len(data):break
            t=j/RATE
            # A short rising liquid resonance, with a soft glass overtone.
            phase=2*math.pi*(freq*t+freq*.72*t*t)
            env=(1-math.exp(-t*210))*math.exp(-t*15)
            data[at]+=env*(math.sin(phase)+.16*math.sin(phase*2.006))*.075
    if name=='open':
        # Low airy water movement; no continuous ambient sound.
        smoothed=0
        for i in range(len(data)):
            t=i/RATE;smoothed=.97*smoothed+.03*rng.uniform(-1,1)
            data[i]+=smoothed*math.sin(math.pi*t/length)**2*.16
    with wave.open(str(OUT/f'karen_aquarium_{name}.wav'),'wb') as wav:
        wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(RATE)
        wav.writeframes(b''.join(struct.pack('<h',round(max(-1,min(1,x))*32767)) for x in data))
