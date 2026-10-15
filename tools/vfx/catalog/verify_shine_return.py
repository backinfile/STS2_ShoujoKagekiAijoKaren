"""Check actual surviving-Shine returns, depletion separation, ownership, and recorded media."""
import argparse
import hashlib
import json
import pathlib
import re
import sys
from assemble import probe, call

repo=pathlib.Path(__file__).resolve().parents[3]
sys.path.insert(0,str(repo/'tools/vfx/six-card'))
from audit_log import audit

parser=argparse.ArgumentParser()
parser.add_argument('--output',type=pathlib.Path,required=True)
root=parser.parse_args().output.resolve()
read=lambda p:json.loads(p.read_text(encoding='utf-8'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
report={'versions':{},'clips':[]}
names=['shine-return','shine-return-modes','starlight-act2']
for branch in ['stable','beta']:
    scenes={};hands=0
    for name in names:
        folder=root/branch/name
        capture=read(folder/'recording.json');motion=read(folder/'shine-motion.json')
        assert capture['status']=='completed' and capture['audio_source']=='game_process' and capture['capture_fps']>=20,capture
        assert capture['captureBuild']['assemblySha256']==sha(repo/f'artifacts/ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.{branch.title()}.dll')
        assert capture['captureBuild']['packSha256']==sha(repo/'artifacts/ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck')
        assert motion['liveReturns']==motion['liveDepletes']==0,motion
        returns=motion['returns'];depletes=motion['depletes']
        expected={'shine-return':['KAREN_SHINE_STRIKE','KAREN_NONON'],'shine-return-modes':['KAREN_SHINE_STRIKE'],'starlight-act2':[]}[name]
        assert [r['card'] for r in returns]==expected,returns
        for r in returns:
            assert r['released'] and r['frames']>10 and r['landingError']<.1,r
            assert r['destination']=='Discard' and r['remainingShine']>0,r
            assert r['gather']<.15 and r['duration']<.37,r
        if name=='shine-return':
            assert [r['remainingShine'] for r in returns]==[1,2],returns
            assert all(abs(r['duration']-.36)<.001 for r in returns)
        if name=='shine-return-modes':
            assert abs(returns[0]['duration']-.26)<.001 and returns[0]['remainingShine']==1
            # The fixture also plays Fuel, uses Instant and discards an unplayed Shine card.
            # Those must not create this replacement return flourish.
            assert len(read(folder/'return-assertions.json')['returns'])==1
        assert len(depletes)==(0 if name=='shine-return-modes' else 1)
        assert all(d['released'] and d['landingError']<.1 and not d['faceReappeared'] for d in depletes)
        initial=read(folder/'initial-state.json')['shine'];final=read(folder/'final-state.json')['shine']
        assert final==initial+(['KAREN_SHINE_STRIKE'] if depletes else []),(name,initial,final)
        revision=read(folder/'revision-probe.json')
        assert revision['missingPlayFrames']==revision['falseWarningFrames']==0,revision
        stage=read(folder/'stage-probe.json')
        assert all(stage[k]==0 for k in ['live','allLive','backLive','hudLive'])
        if name=='starlight-act2':
            hover=next(c for c in stage['casts'] if c['kind']=='Act2Hover')
            assert hover['playIndices']==[0,1,2] and hover['depletionHeldFrames']>0
        for path in folder.glob('*hand.json'):
            hand=json.loads(read(path)['message'])
            assert hand['modelCount']==hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons'],path
            hands+=1
        assert all((folder/f'recording-{s}.png').exists() for s in ['before','after'])
        scenes[name]=dict(captureFps=capture['capture_fps'],returns=returns,depletes=depletes,warning=revision)
    window=read(root/branch/'window-monitor.json')
    assert window['foreground_samples']==window['restored_samples']==0,window
    log=(root/branch/'godot.log').read_text(encoding='utf-8')
    assert '[Karen STS2 MCP]' in log
    errors=audit(log);assert not errors['unexpectedErrors'],errors
    report['versions'][branch]=dict(handChecks=hands,scenes=scenes,errors=errors,window=window)
for name in names:
    folder=root/'clips'/name;movie=folder/f'Karen-{name}.mp4';info=probe(movie)
    video=next(s for s in info['streams'] if s['codec_type']=='video')
    assert (video['width'],video['height'],video['r_frame_rate'])==(1280,816,'30/1')
    assert any(s['codec_type']=='audio' and s['codec_name']=='aac' for s in info['streams'])
    log=call(['ffmpeg','-hide_banner','-i',str(movie),'-vn','-af','volumedetect','-f','null','NUL'],folder/'audio-check.log')
    peak=float(re.search(r'max_volume: (-?[\d.]+) dB',log).group(1));assert -60<peak<0
    errors=call(['ffmpeg','-v','error','-xerror','-threads','2','-i',str(movie),'-fps_mode','passthrough','-enc_time_base','-1','-f','null','NUL'],folder/'decode-check.log')
    assert not errors.strip(),errors
    report['clips'].append(dict(name=name,duration=float(info['format']['duration']),sha256=sha(movie),audioPeakDb=peak))
(root/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(handChecks={b:v['handChecks'] for b,v in report['versions'].items()},clips=report['clips']),ensure_ascii=False))
