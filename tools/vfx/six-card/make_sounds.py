"""Original, deterministic PCM sweeps/chimes for the seven benefit effects. No sampled audio."""
from pathlib import Path
import math, random, struct, wave

root=Path(__file__).resolve().parents[3]
out=root/'ShoujoKagekiAijoKaren/audio'
rate=44100
for name,duration in [('gather',.48),('bloom',1.35)]:
    rng=random.Random(20261013)
    samples=[];phase=0;filtered=0
    for i in range(round(duration*rate)):
        t=i/rate;u=t/duration
        filtered=filtered*.86+(rng.random()*2-1)*.14
        if name=='gather':
            phase+=(180+1750*u*u)*math.tau/rate
            env=math.sin(math.pi*u)**1.8
            value=(filtered*.7+math.sin(phase)*.035)*env
        else:
            value=filtered*.18*math.exp(-t*13)*min(1,t/.012)
            for j,hz in enumerate([523.25,659.25,783.99,1046.5]):
                elapsed=t-j*.026
                if elapsed<0:continue
                env=min(1,elapsed/.008)*math.exp(-elapsed*(3.2+j*.4))
                value+=math.sin(math.tau*hz*elapsed)*env*.105
                value+=math.sin(math.tau*hz*2.76*elapsed)*env*.018
        samples.append(value)
    maximum=max(abs(x) for x in samples)
    pcm=b''.join(struct.pack('<h',round(x/maximum*.6*32767)) for x in samples)
    with wave.open(str(out/f'karen_benefit_{name}.wav'),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes(pcm)
    print(name,len(samples))

# Abstract air and energy cues. All are oscillator/noise synthesis.
for name,duration in [('steam',.9),('energy',.72)]:
    rng=random.Random(20261013);samples=[];filtered=0
    for i in range(round(duration*rate)):
        t=i/rate;u=t/duration;noise=rng.random()*2-1
        filtered=filtered*.83+noise*.17
        if name=='steam':
            value=filtered*math.sin(math.pi*u)**1.3*.45
            value+=math.sin(math.tau*(330*t+35*t*t))*.026*math.sin(math.pi*u)
        else:
            value=filtered*.15*math.sin(math.pi*u)**1.4
            for j,hz in enumerate([784,1175,1568]):
                d=t-j*.065
                if d>0:value+=math.sin(math.tau*hz*d)*math.exp(-d*8)*min(1,d/.008)*.10
        samples.append(value*min(1,t/.002))
    maximum=max(abs(x) for x in samples)
    pcm=b''.join(struct.pack('<h',round(x/maximum*.48*32767)) for x in samples)
    with wave.open(str(out/f'karen_story_{name}.wav'),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes(pcm)
    print(name,len(samples))
