"""Check real lethal depletion, mid-gather victory and the following combat."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

repo=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(repo/'tools/vfx/six-card'))
from audit_log import audit

parser=argparse.ArgumentParser()
parser.add_argument('--output',type=Path,required=True)
root=parser.parse_args().output.resolve()
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
report={}
for branch in ['stable','beta']:
    scenes={};hands=0
    for name in ['shine-lethal','shine-end-flight']:
        folder=root/branch/name
        initial=read(folder/'initial-state.json')
        captured=read(folder/'recording.json')
        assert captured['status']=='completed' and captured['capture_fps']>=20,captured
        assert captured['audio_source'] in ('game_process','engine_mix'),captured
        assert captured['captureBuild']['assemblySha256']==sha(repo/f'artifacts/ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.{branch.title()}.dll')
        assert captured['captureBuild']['packSha256']==sha(repo/'artifacts/ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck')
        assert read(folder/'rewards-state.json')['state_type']=='rewards'
        expected=len(initial['shine'])+1
        ended=read(folder/'rewards-counter.json')
        assert len(ended['depletes'])==1 and not ended['depletes'][0]['arrived'] and not ended['arrivals'],ended
        assert ended['counter']['heldFrames']>0,ended
        for stage in ['rewards','next-combat']:
            status=read(folder/f'{stage}-counter.json');c=status['counter']
            assert c['model']==c['displayed']==c['target']==expected,c
            assert c['pending']==0 and not c['animating'] and abs(c['scale']-1)<.001,c
            assert status['liveDepletes']==status['liveArrivals']==0,status
            assert status['pileVisualRestoreError']<.001,status
        if name=='shine-end-flight':
            flying=read(folder/'before-end-flight.json')
            assert flying['counter']['pending']==1 and flying['counter']['model']==expected,flying
            assert flying['counter']['displayed']==expected-1,flying
            d=flying['depletes'][0]
            assert not d['hasFlight'] and 0<d['samples'][-1]['age']<d['gather'],d
        assert read(folder/'next-combat.json')['battle']['is_play_phase']
        for path in folder.glob('*hand.json'):
            h=json.loads(read(path)['message'])
            assert h['modelCount']==h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'],h
            hands+=1
        assert all((folder/f'recording-{s}.png').is_file() for s in ['before','after'])
        scenes[name]={'modelCount':expected,'captureFps':captured['capture_fps'],
                      'rewardCounter':read(folder/'rewards-counter.json')['counter'],
                      'nextCombatCounter':read(folder/'next-combat-counter.json')['counter']}
    log=(root/branch/'godot.log').read_text(encoding='utf-8');errors=audit(log)
    assert '[Karen STS2 MCP]' in log and not errors['unexpectedErrors'],errors
    preload=next(i for i,line in enumerate(log.splitlines(),1) if "Preloading 'Common' Complete:" in line)
    assert all(e['line']<preload for e in errors['knownStartupWorkerPoolErrors']),errors
    window=read(root/branch/'window-monitor.json')
    assert window['foreground_samples']==window['restored_samples']==0,window
    report[branch]={'scenes':scenes,'handChecks':hands,'errors':errors,'window':window}
(root/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({b:{'scenes':len(v['scenes']),'handChecks':v['handChecks']} for b,v in report.items()}))
