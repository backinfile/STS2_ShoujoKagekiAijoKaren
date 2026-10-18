"""Verify and retain concise evidence for the selected C implementation."""
import hashlib,json,shutil
from pathlib import Path
project=Path(__file__).resolve().parents[3]
root=project/'artifacts/unforgettable-persistent-20261018'
dest=project/'docs/worktree-tasks/unforgettable-vfx-concepts-20261018/implementation'
required={'ordinary','stack','last','upgrade','fast','instant','pose','replay','lethal','turn','room'}
summary={'scope':'targeted singleplayer VFX validation; multiplayer/full matrix not run','versions':{},'asset':{}}
asset=project/'images/vfx/unforgettable/lingering-star.png'
summary['asset']={'file':asset.relative_to(project).as_posix(),'sha256':hashlib.sha256(asset.read_bytes()).hexdigest(),'mode':'built-in image_gen; original RGBA copied unchanged'}
for branch,version in [('stable','v0.107.1'),('beta','v0.111.0')]:
    source=root/branch/'checks'
    results=json.loads((source/'results.json').read_text())
    lookup={r['case']:r for r in results}
    assert required<=lookup.keys() and all(lookup[n]['passed'] for n in required|{'showcase'})
    for name in required:
        row=lookup[name];ret=row['retained'];clean=row['clean']
        count=3 if name=='stack' else 2 if name=='replay' else 1
        assert ret['live']==1 and ret['retained']==count and ret['flashes']==0
        assert clean['live']==0 and all(r['Exited'] for r in clean['roots'])
        assert all(r['MaxDrift']<.02 for r in ret['roots'])
        # ShinePileManager restores an exhausted card to its maximum in the shine pile.
        expected=[3] if name in {'last','upgrade','replay'} else [2,2,2] if name=='stack' else [2]
        assert ret['shines']==expected,(branch,name,ret['shines'])
    hand_files=[p for name in required|{'showcase'} for p in (source/name).glob('*-hand.json')]
    for path in hand_files:
        h=json.loads(path.read_text())
        assert h['modelCount']==h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'],path
    media_path=dest/('media-'+branch+'.json')
    media=json.loads(media_path.read_text())
    relative_raw=(source/'showcase/game.mp4').relative_to(project).as_posix()
    media['raw']['format']['filename']=relative_raw
    media['recording']['output_path']=relative_raw
    media_path.write_text(json.dumps(media,ensure_ascii=False,indent=2),encoding='utf-8')
    assert media['recording']['audio_source']=='game_process' and media['sourcePeakDb']>-70
    assert media['recording'].get('encoder') and media['recording'].get('capture_fps',0)>20
    output=dest/'evidence'/branch;output.mkdir(parents=True,exist_ok=True)
    for name,stage,label in [('ordinary','before','ordinary-before'),('ordinary','retained','ordinary-retained'),('stack','retained','stack-retained'),('lethal','retained','lethal-retained'),('turn','retained','turn-retained'),('room','finished','room-clean')]:
        shutil.copy2(source/name/(stage+'.png'),output/(label+'.png'))
    (output/'results.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
    build=json.loads((source/'build.json').read_text())
    for name,digest in build.items():
        assert hashlib.sha256((project/'artifacts/ShoujoKagekiAijoKaren'/name).read_bytes()).hexdigest()==digest
    summary['versions'][branch]=dict(version=version,cases={n:'passed' for n in sorted(required)},showcase='passed',handChecks=len(hand_files),maxAnchorDrift=max(r['MaxDrift'] for n in required for r in lookup[n]['retained']['roots']),allCombatGroupsCleared=True,build=build,videoSeconds=media['recording']['video_duration_seconds'],captureFps=media['recording']['capture_fps'],framesRepeated=media['recording']['frames_repeated'],framesDropped=media['recording']['frames_dropped'],audioPeakDb=media['sourcePeakDb'],previewGainDb=media['previewGainDb'])
visual=root/'stable/visual/ordinary'
for name in ['hit','fading','retained']:
    shutil.copy2(visual/(name+'.png'),dest/('stable-'+name+'.png'))
summary['testInfrastructureNote']='First stable batch passed ordinary/stack then encountered MCP timeout during transition; raw log kept as artifacts/unforgettable-persistent-20261018/stable/checks/attempt1-timeout.log. Remaining checks were rerun separately with reward-screen settling before the next encounter and all passed. Timeout cause not claimed resolved in production.'
summary['limits']=['No full multiplayer matrix','No save/reload restoration test for historical cosmetic scars','No third-party mod compatibility matrix']
(dest/'verification.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(summary,ensure_ascii=False,indent=2))
