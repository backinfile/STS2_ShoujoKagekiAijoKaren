"""Verify gather-triggered icon feedback, speed modes and hover/retrigger recovery."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys
from assemble import probe, call

repo=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(repo/'tools/vfx/six-card'))
from audit_log import audit

parser=argparse.ArgumentParser()
parser.add_argument('--output',type=Path,required=True)
root=parser.parse_args().output.resolve()
read=lambda path:json.loads(path.read_text(encoding='utf-8'))
sha=lambda path:hashlib.sha256(path.read_bytes()).hexdigest()
names=['shine-motion','shine-speeds','shine-arrival-control','shine-count-control']
report={'versions':{},'clips':[]}
for branch in ['stable','beta']:
    scenes={};hands=0
    for name in names:
        folder=root/branch/name
        capture=read(folder/'recording.json');motion=read(folder/'shine-motion.json');revision=read(folder/'revision-probe.json')
        assert capture['status']=='completed' and capture['audio_source'] in ('game_process','engine_mix') and capture['capture_fps']>=20,capture
        assert capture['captureBuild']['assemblySha256']==sha(repo/f'artifacts/ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.{branch.title()}.dll')
        assert capture['captureBuild']['packSha256']==sha(repo/'artifacts/ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck')
        assert motion['liveDepletes']==motion['liveArrivals']==0 and motion['pileVisualRestoreError']<.001,motion
        assert all(abs(s-1)<.001 for s in motion['pileIconScale']),motion
        assert abs(motion['pileIconRotation'])<.001,motion
        assert revision['missingPlayFrames']==revision['falseWarningFrames']==0,revision
        counter=motion['counter']
        assert counter['displayed']==counter['target']==counter['model']==len(read(folder/'final-state.json')['shine']),counter
        assert counter['pending']==counter['earlyFrames']==0 and not counter['animating'] and abs(counter['scale']-1)<.001,counter
        expected={'shine-motion':['KAREN_SHINE_STRIKE','KAREN_NONON'],
                  'shine-speeds':['KAREN_SHINE_STRIKE','KAREN_SHINE_STRIKE','KAREN_DROP_FUEL'],
                  'shine-arrival-control':[],'shine-count-control':[]}[name]
        assert len(motion['depletes'])==len(expected)
        if name=='shine-count-control':
            assert read(folder/'final-state.json')['shine']==['KAREN_SHINE_STRIKE']*4
            burst=read(folder/'count-burst.json')['counter'];settled=read(folder/'count-burst-settled.json')['counter']
            assert burst['displayed']<burst['model'] and settled['displayed']==settled['model'],burst
            changes=settled['changes']
            steps=[(a,b) for a,b in zip(changes,changes[1:]) if b['count']>a['count']]
            assert len(steps)==3 and all(b['count']==a['count']+1 for a,b in steps),changes
            assert all(b[1]['time']-a[1]['time']>=.15 for a,b in zip(steps,steps[1:])),steps
            for filename,n in [('count-cleared',0),('count-snapshot',3),('count-cancelled',4)]:
                c=read(folder/f'{filename}.json')['counter']
                assert c['displayed']==c['model']==n and c['pending']==0 and not c['animating'],c
            held=read(folder/'count-held.json')['counter']
            assert held['displayed']==3 and held['model']==4 and held['pending']==1,held
        else:
            assert read(folder/'final-state.json')['shine']==read(folder/'initial-state.json')['shine']+expected
        assert sum(a['count'] for a in motion['arrivals'])==(3 if name=='shine-arrival-control' else len(expected)),motion
        if expected:assert counter['heldFrames']>0,counter
        for d in motion['depletes']:
            assert d['released'] and not d['hasFlight'] and d['duration']==0 and not d['faceReappeared'],d
            assert 0<=d['completionAge']-d['gather']<.10,d
        for a in motion['arrivals']:
            assert 1.12<a['maxScale']<1.24 and .87<a['minScale']<1,a
            assert -.29<a['minRotation']<-.18 and .12<a['maxRotation']<.22,a
        if name=='shine-speeds':
            assert all(abs(a['duration']-t)<.001 for a,t in zip(motion['arrivals'],[.46,.20,.46]))
            assert motion['depletes'][-1]['fuel']
        if name=='shine-arrival-control':
            hover=read(folder/'hover-arrivals.json')
            assert len(hover['arrivals'])==1 and hover['arrivals'][0]['count']==3,hover
            assert hover['liveArrivals']==0 and hover['pileVisualRestoreError']<.001,hover
            assert all(abs(s-1.1)<.001 for s in hover['pileIconScale']),hover
            assert abs(hover['pileIconRotation']-3.14159265)<.001,hover
        for path in folder.glob('*hand.json'):
            hand=json.loads(read(path)['message'])
            assert hand['modelCount']==hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons'],path
            hands+=1
        assert all((folder/f'recording-{stage}.png').exists() for stage in ['before','after'])
        scenes[name]={'captureFps':capture['capture_fps'],'motion':motion}
    log=(root/branch/'godot.log').read_text(encoding='utf-8');errors=audit(log)
    assert '[Karen STS2 MCP]' in log and not errors['unexpectedErrors'],errors
    preload=next(i for i,line in enumerate(log.splitlines(),1) if "Preloading 'Common' Complete:" in line)
    assert all(e['line']<preload for e in errors['knownStartupWorkerPoolErrors'])
    window=read(root/branch/'window-monitor.json')
    assert window['foreground_samples']==window['restored_samples']==0,window
    extra={}
    if (root/f'{branch}-flight-session.log').exists():
        earlier=(root/f'{branch}-flight-session.log').read_text(encoding='utf-8');earlier_errors=audit(earlier)
        assert not earlier_errors['unexpectedErrors'],earlier_errors
        earlier_preload=next(i for i,line in enumerate(earlier.splitlines(),1) if "Preloading 'Common' Complete:" in line)
        assert all(e['line']<earlier_preload for e in earlier_errors['knownStartupWorkerPoolErrors'])
        earlier_window=read(root/f'{branch}-flight-window.json')
        assert earlier_window['foreground_samples']==earlier_window['restored_samples']==0,earlier_window
        extra={'errors':earlier_errors,'window':earlier_window}
    report['versions'][branch]={'scenes':scenes,'handChecks':hands,'errors':errors,'window':window,'additionalFlightSession':extra}
for name in names[:2]:
    folder=root/'clips'/name;movie=folder/f'Karen-{name}.mp4';info=probe(movie)
    video=next(s for s in info['streams'] if s['codec_type']=='video')
    assert (video['width'],video['height'],video['r_frame_rate'])==(1280,816,'30/1')
    assert any(s['codec_type']=='audio' and s['codec_name']=='aac' for s in info['streams'])
    log=call(['ffmpeg','-hide_banner','-i',str(movie),'-vn','-af','volumedetect','-f','null','NUL'],folder/'audio-check.log')
    peak=float(re.search(r'max_volume: (-?[\d.]+) dB',log).group(1));assert -60<peak<0
    errors=call(['ffmpeg','-v','error','-xerror','-threads','2','-i',str(movie),'-fps_mode','passthrough','-enc_time_base','-1','-f','null','NUL'],folder/'decode-check.log')
    assert not errors.strip(),errors
    report['clips'].append({'name':name,'duration':float(info['format']['duration']),'audioPeakDb':peak,'sha256':sha(movie)})
(root/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'handChecks':{b:v['handChecks'] for b,v in report['versions'].items()},'clips':report['clips']},ensure_ascii=False))
