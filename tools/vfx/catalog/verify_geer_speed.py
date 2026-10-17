"""Validate targeted real Geer outcomes, presentation lifetime, capture provenance and hand nodes."""
import argparse
import hashlib
import json
from pathlib import Path
import re

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
root = args.output.resolve()

def load(p):
    return json.loads(p.read_text(encoding='utf-8-sig'))

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

report = dict(scope='Targeted single-player checks; no multiplayer or full matrix.', runs=[])
for version, branch in [('before', 'stable'), ('after', 'stable'), ('after', 'beta')]:
    folder = root / version / branch
    build = dict(assemblySha256=sha(root / (version+'-package') / 'lib' / f'ShoujoKagekiAijoKaren.{branch.title()}.dll'),
                 packSha256=sha(root / (version+'-package') / 'ShoujoKagekiAijoKaren.pck'))
    run = dict(version=version, branch=branch, build=build, cases=[], handChecks=0)
    cases = ['normal', 'fast', 'instant', 'switch-cast']
    if version == 'after':
        cases += ['void-normal', 'void-fast', 'void-instant', 'empty', 'stacked', 'deferred', 'interrupt', 'leave']
    for name in cases:
        scene = folder / ('geer-'+name)
        r = load(scene / 'geer-result.json')
        assert r['live'] == r['activeSounds'] == 0, (scene,r)
        assert all(n['exited'] and n['parentCorrect'] and n['frames'] > 0 for n in r['nodes']), scene
        assert all(r[k] < .001 for k in ('offsetError', 'positionError', 'visualError')) and r['zError'] == 0, scene
        if name in ('normal','fast','void-normal','void-fast'):
            assert len(r['nodes']) == 3 and r['power'] == 1 and r['selectionPeak'] == 0, scene
            assert all(.85 < n['duration'] < 1.12 if n['fullScreen'] or n['ownerCast'] else .50 < n['duration'] < .74 for n in r['nodes']), r
        if version == 'after' and name in ('instant','void-instant','deferred'):
            assert r['nodes'] == [], scene
        if name == 'switch-cast':
            assert r['power'] == 1 and len(r['nodes']) == 2 and r['switchAt'] > 0, scene
            delay = max(n['ended'] - r['switchAt'] for n in r['nodes'])
            assert delay < .12 if version == 'after' else delay > .5, r
        if version == 'before' and name == 'instant':
            assert r['selectionPeak'] == 3 and len(r['nodes']) == 3, r
        if name == 'empty':
            assert len(r['nodes']) == 2 and r['selectionAt'] < 0 and r['power'] == 1, r
        if name == 'stacked':
            assert len(r['nodes']) == 6 and r['power'] == 2, r
        plays = None
        if (scene/'geer-native.json').exists():
            native = load(scene/'geer-native.json')
            plays = [e for e in native['finished'] if e['auto'] and e['id'] == 'KAREN_STRIKE']
            assert len(plays) == (0 if name=='empty' else 2 if name=='stacked' else 1), scene
            if name != 'empty':
                before = load(scene/('recording-before.json' if name in ('normal','fast','instant') else 'before.json'))
                after = load(scene/('recording-after.json' if name in ('normal','fast','instant') else 'after.json'))
                assert before['battle']['enemies'][0]['hp'] - after['battle']['enemies'][0]['hp'] == 6 * len(plays), scene
        entry = dict(name=name,nodes=len(r['nodes']),selectionPeak=r['selectionPeak'],power=r['power'],autoplays=None if plays is None else len(plays),
                     live=r['live'],activeSounds=r['activeSounds'],timing=[dict(duration=n['duration'],age=n['age'],instantFrames=n['instantFrames'],instantSoundFrames=n['instantSoundFrames']) for n in r['nodes']])
        if (scene/'recording.json').exists():
            cap=load(scene/'recording.json')
            assert cap['status']=='completed' and cap['audio_source']=='game_process' and cap['captureBuild']==build, scene
            assert cap['frames_dropped']==0 and cap['capture_fps']>25, cap
            entry['capture']=dict(fps=cap['capture_fps'],repeated=cap['frames_repeated'],dropped=cap['frames_dropped'],encoder=cap['encoder'],movieSha256=sha(scene/'game.mp4'))
        run['cases'].append(entry)
    for path in folder.rglob('*hand*.json'):
        h=load(path)
        if isinstance(h,dict) and 'message' in h:
            h=json.loads(h['message'])
        if not isinstance(h,dict) or 'modelCount' not in h: continue
        assert h['modelCount']==h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'], path
        run['handChecks'] += 1
    monitor=load(folder/'window-monitor.json')
    assert monitor['foreground_samples']==monitor['restored_samples']==0, monitor
    run['windowMonitor']=monitor
    lines=(folder/'godot.log').read_text(encoding='utf-8',errors='replace').splitlines()
    assert any('[Karen STS2 MCP]' in line for line in lines), folder
    run['logDiagnostics']=[line for line in lines if re.search(r'ERROR:|SCRIPT ERROR|SHADER ERROR|Exception',line)]
    run['knownStartupDiagnostics']=[]
    unexpected=[]; common_loading=False
    for line in lines:
        if "Preloading 'Common' assets" in line: common_loading=True
        if re.search(r'ERROR:|SCRIPT ERROR|SHADER ERROR|Exception',line):
            if common_loading and line.strip() == 'ERROR: Invalid Task ID':
                run['knownStartupDiagnostics'].append(line)
            else: unexpected.append(line)
        if "Preloading 'Common' Complete" in line: common_loading=False
    assert not unexpected, (folder,unexpected)
    report['runs'].append(run)
(root/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps([dict(version=r['version'],branch=r['branch'],cases=len(r['cases']),handChecks=r['handChecks'],logDiagnostics=len(r['logDiagnostics'])) for r in report['runs']],ensure_ascii=False))
