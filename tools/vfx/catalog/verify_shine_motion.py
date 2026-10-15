"""Targeted runtime and media checks for natural Shine motion and scene-floor lighting."""
import argparse
import hashlib
import json
import math
import re
from pathlib import Path
from assemble import probe, call

parser=argparse.ArgumentParser()
parser.add_argument('--output',type=Path,required=True)
args=parser.parse_args();root=args.output.resolve();repo=Path(__file__).resolve().parents[3]
read=lambda p:json.loads(p.read_text(encoding='utf-8'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
report={'versions':{},'clips':[]}
names=['shine-motion','shine-speeds','starlight-act2']

def check_flight_speed(d):
    buckets=[[],[],[]];path=early=peak=0
    cutoff=d['gather']+d['duration']*.30
    for a,b in zip(d['samples'],d['samples'][1:]):
        dt=b['age']-a['age']
        if dt<=0:continue
        distance=math.hypot(b['x']-a['x'],b['y']-a['y'])
        path+=distance;peak=max(peak,distance/dt)
        early+=distance*max(0,min(1,(cutoff-a['age'])/dt))
        if a['age']<d['gather'] or b['age']>d['gather']+d['duration']:continue
        t=((a['age']+b['age'])/2-d['gather'])/d['duration']
        speed=distance/dt
        if 0<t<.24:buckets[0].append(speed)
        if .5<t<.72:buckets[1].append(speed)
        if .9<t<1:buckets[2].append(speed)
    assert all(buckets),d
    averages=[sum(b)/len(b) for b in buckets]
    assert averages[1]>averages[0]*6 and averages[1]>averages[2]*4,averages
    ratio=peak/(path/d['duration']);early_share=early/path
    assert ratio>2.6 and early_share<.05,(ratio,early_share)
    return dict(launch=averages[0],surge=averages[1],catch=averages[2],peakToAverage=ratio,first30PercentDistance=early_share)

for branch in ['stable','beta']:
    branch_report={};hands=0
    for name in names:
        folder=root/branch/name;motion=read(folder/'shine-motion.json');revision=read(folder/'revision-probe.json')
        capture=read(folder/'recording.json')
        assert capture['status']=='completed' and capture['audio_source']=='game_process' and capture['capture_fps']>=20
        assert capture['captureBuild']['assemblySha256']==sha(repo/f'artifacts/ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.{branch.title()}.dll')
        assert capture['captureBuild']['packSha256']==sha(repo/'artifacts/ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck')
        assert motion['liveDepletes']==revision['missingPlayFrames']==revision['falseWarningFrames']==0,(branch,name,revision)
        assert motion['depletes'] and all(d['released'] and d['landingError']<.1 and not d['faceReappeared'] for d in motion['depletes']),motion
        assert motion['maxSparks']<=40
        initial=read(folder/'initial-state.json')['shine'];final=read(folder/'final-state.json')['shine']
        expected={'shine-motion':['KAREN_SHINE_STRIKE','KAREN_NONON'],
                  'shine-speeds':['KAREN_SHINE_STRIKE','KAREN_SHINE_STRIKE','KAREN_DROP_FUEL'],
                  'starlight-act2':['KAREN_SHINE_STRIKE']}[name]
        assert final==initial+expected,(branch,name,initial,final)
        for d in motion['depletes']:
            assert d['duration']<=.55 and d['gather']<=.341 and d['minCardScale']<.2
        speed_checks=[check_flight_speed(d) for d in motion['depletes'] if d['duration']>=.3]
        if name=='shine-motion':
            assert len(motion['depletes'])==2 and motion['detachedSparkFrames']>0
            assert revision['playWarningFrames']>0
        if name=='shine-speeds':
            assert len(motion['depletes'])==3
            assert all(abs(d['duration']-v)<.001 for d,v in zip(motion['depletes'],[.38,.12,.54]))
            assert motion['depletes'][-1]['fuel']
        if name=='starlight-act2':
            assert motion['floorFrames']>0 and motion['maxFloorY']<motion['viewportHeight']*.8
            # The foreground plane projects another 36 pixels down at 720p.
            assert motion['maxFloorOffset']<motion['viewportHeight']*.10
            stage=read(folder/'stage-probe.json')
            assert stage['live']==stage['allLive']==stage['backLive']==stage['hudLive']==0
            hover=next(s for s in stage['casts'] if s['kind']=='Act2Hover')
            assert hover['playIndices']==[0,1,2] and hover['depletionHeldFrames']>0
        for path in folder.glob('*hand.json'):
            hand=json.loads(read(path)['message'])
            assert hand['modelCount']==hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons'],path
            hands+=1
        assert (folder/'recording-before.png').exists() and (folder/'recording-after.png').exists()
        branch_report[name]=dict(motion=motion,warning=revision,captureFps=capture['capture_fps'],speedChecks=speed_checks)
    window=read(root/branch/'window-monitor.json')
    assert window['foreground_samples']==window['restored_samples']==0,window
    log=(root/branch/'godot.log').read_text(encoding='utf-8');errors=[l for l in log.splitlines() if l.startswith('ERROR:') or 'Exception:' in l]
    if branch=='stable':assert not errors,errors
    else:
        assert all(l=='ERROR: Invalid Task ID' for l in errors),errors
        after_preload=log[log.index("Preloading 'Common' Complete:"):]
        assert not any(l.startswith('ERROR:') or 'Exception:' in l for l in after_preload.splitlines())
    report['versions'][branch]=dict(scenes=branch_report,handChecks=hands,window=window,startupErrors=errors)
for name in ['shine-motion','starlight-act2']:
    folder=root/'clips'/name;movie=folder/f'Karen-{name}.mp4';info=probe(movie)
    video=next(s for s in info['streams'] if s['codec_type']=='video')
    assert (video['width'],video['height'],video['r_frame_rate'])==(1280,816,'30/1')
    assert any(s['codec_type']=='audio' for s in info['streams'])
    log=call(['ffmpeg','-hide_banner','-i',str(movie),'-vn','-af','volumedetect','-f','null','NUL'],folder/'audio-check.log')
    peak=float(re.search(r'max_volume: (-?[\d.]+) dB',log).group(1));assert -60<peak<0
    error=call(['ffmpeg','-v','error','-xerror','-threads','2','-i',str(movie),'-fps_mode','passthrough','-enc_time_base','-1','-f','null','NUL'],folder/'decode-check.log')
    assert not error.strip(),error
    report['clips'].append(dict(name=name,duration=float(info['format']['duration']),audioPeakDb=peak,sha256=sha(movie),fullDecode=True))
(root/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(handChecks={b:v['handChecks'] for b,v in report['versions'].items()},clips=report['clips']),ensure_ascii=False))
