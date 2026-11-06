"""Verify persistent card light, warning separation, native returns and actual MCP captures."""
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
parser.add_argument('--scenes',default='shine-ambient,shine-ambient-modes,starlight-act2')
args=parser.parse_args();root=args.output.resolve()
read=lambda p:json.loads(p.read_text(encoding='utf-8'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
names=args.scenes.split(',')
assert names and all(n in ['shine-ambient','shine-ambient-modes','starlight-act2'] for n in names),names
report={'versions':{},'clips':[]}
for branch in ['stable','beta']:
    assembly=repo/f'artifacts/ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.{branch.title()}.dll'
    binary=assembly.read_bytes()
    assert b'NKarenShineAmbientVfx' in binary
    assert all(name not in binary for name in [b'NKarenShineReturnVfx',b'ReturnSourcePatch']), 'Retired return code remains'
    scenes={};hands=0
    for name in names:
        folder=root/branch/name
        capture=read(folder/'recording.json');revision=read(folder/'revision-probe.json');motion=read(folder/'shine-motion.json')
        assert capture['status']=='completed' and capture['audio_source']=='game_process' and capture['capture_fps']>=20,capture
        assert capture['captureBuild']['assemblySha256']==sha(assembly)
        assert capture['captureBuild']['packSha256']==sha(repo/'artifacts/ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck')
        assert all(revision[k]==0 for k in ['missingAmbientFrames','ambientOverlapFrames','ambientLeakFrames','missingPlayFrames','falseWarningFrames']),revision
        assert revision['maxAmbientSparks']<=14
        if name.startswith('shine-ambient'):
            assert revision['handAmbientFrames']>100 and revision['playAmbientFrames']>0,revision
            assert revision['maxAmbientSparks']>=8,revision
        if name=='shine-ambient':
            assert motion['detachedSparkFrames']>0 and revision['handWarningFrames']>0
            border=revision['border']
            assert border['ambientSamples']>20 and border['warningSamples']>20,border
            assert border['edgeMisses']==0 and border['maxEdgeErrorPixels']<=1,border
            assert border['minScale']<border['maxScale'],border
            assert abs(border['ambientOpacity']-.65)<.001,border
        assert motion['liveDepletes']==0
        assert len(motion['depletes'])==(0 if name=='shine-ambient-modes' else 1)
        assert all(d['released'] and d['landingError']<.1 and not d['faceReappeared'] for d in motion['depletes'])
        initial=read(folder/'initial-state.json')['shine'];final=read(folder/'final-state.json')['shine']
        assert final==initial+(['KAREN_SHINE_STRIKE'] if motion['depletes'] else []),(name,initial,final)
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
        scenes[name]=dict(captureFps=capture['capture_fps'],cardLight=revision,motion=motion)
    window=read(root/branch/'window-monitor.json')
    assert window['foreground_samples']==window['restored_samples']==0,window
    log=(root/branch/'godot.log').read_text(encoding='utf-8')
    assert '[Karen STS2 MCP]' in log
    errors=audit(log);assert not errors['unexpectedErrors'],errors
    preload=next(i for i,line in enumerate(log.splitlines(),1) if "Preloading 'Common' Complete:" in line)
    assert all(e['line']<preload for e in errors['knownStartupWorkerPoolErrors'])
    report['versions'][branch]=dict(handChecks=hands,scenes=scenes,errors=errors,window=window,retiredReturnCodePresent=False)
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
