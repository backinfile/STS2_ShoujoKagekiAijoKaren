"""Retain verification for the jitter-only revision separately from the initial C build."""
import hashlib,json,math,shutil
from pathlib import Path
project=Path(__file__).resolve().parents[3]
root=project/'artifacts/unforgettable-persistent-20261018'
dest=project/'docs/worktree-tasks/unforgettable-vfx-concepts-20261018/implementation/jitter'
dest.mkdir(parents=True,exist_ok=True)
required={'stack','replay','instant-replay','fast-stack','pose','turn'}
summary={'scope':'jitter revision; six targeted singleplayer checks per version','sampling':'presentation-only bounded offset; best of eight nearby candidates; additional rotation within +/-10 degrees; sampled once per hit','versions':{}}
for branch in ['stable','beta']:
    source=root/branch/'jitter';results=json.loads((source/'results.json').read_text())
    lookup={x['case']:x for x in results}
    assert required<=lookup.keys() and all(lookup[n]['passed'] for n in required)
    separated={}
    for name in required:
        row=lookup[name];final=row['retained']
        assert final['live']==1 and final['flashes']==0 and row['clean']['live']==0
        assert all(r['Exited'] for r in row['clean']['roots'])
        for r in final['roots']:
            assert r['MaxDrift']<.02 and r['MaxRotationDrift']<.0001 and r['MaxFlashMismatch']<.02
            assert abs(r['opacity']-.2)<.001
        if final['retained']>1:
            marks=final['roots'][-1]['marks']
            minimum=min(math.dist(x['anchor'],y['anchor']) for i,x in enumerate(marks) for y in marks[i+1:])
            angles=[math.degrees(x['rotation']) for x in marks]
            assert minimum>2 and all(abs(a)<=10.01 for a in angles)
            separated[name]=dict(minCenterDistanceCanvasUnits=minimum,additionalRotationDegrees=angles)
    hands=[path for case in required for path in (source/case).glob('*-hand.json')]
    for path in hands:
        h=json.loads(path.read_text());assert h['modelCount']==h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons']
    build=json.loads((source/'build.json').read_text())
    assert all(hashlib.sha256((project/'artifacts/ShoujoKagekiAijoKaren'/name).read_bytes()).hexdigest()==value for name,value in build.items())
    media=json.loads((dest/('media-'+branch+'.json')).read_text())
    assert media['recording']['audio_source']=='game_process' and media['sourcePeakDb']>-70
    evidence=dest/'evidence'/branch;evidence.mkdir(parents=True,exist_ok=True)
    for case,stage in [('stack','before'),('stack','retained'),('replay','retained'),('turn','retained')]:
        shutil.copy2(source/case/(stage+'.png'),evidence/(case+'-'+stage+'.png'))
    (evidence/'results.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
    summary['versions'][branch]=dict(cases={n:'passed' for n in sorted(required)},separation=separated,handChecks=len(hands),maxPositionDrift=max(r['MaxDrift'] for n in required for r in lookup[n]['retained']['roots']),maxRotationDrift=max(r['MaxRotationDrift'] for n in required for r in lookup[n]['retained']['roots']),maxFlashToScarMismatch=max(r['MaxFlashMismatch'] for n in required for r in lookup[n]['retained']['roots']),build=build,videoSeconds=media['recording']['video_duration_seconds'],captureFps=media['recording']['capture_fps'],audioSourcePeakDb=media['sourcePeakDb'])
summary['limits']='The initial C build has its own earlier 11-case evidence. This revision reruns only these six directly related cases; full multiplayer and save/load history reconstruction not tested.'
(dest/'verification.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(summary,ensure_ascii=False,indent=2))
