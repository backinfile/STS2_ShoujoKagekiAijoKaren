"""Audit all boundary attempts, screenshots, logs and final package provenance."""
import argparse,hashlib,json,pathlib,sys
from PIL import Image,ImageDraw
p=argparse.ArgumentParser();p.add_argument('--output',type=pathlib.Path,required=True);p.add_argument('--assert-complete',action='store_true');p.add_argument('--assert-functional',action='store_true');a=p.parse_args()
repo=pathlib.Path(__file__).resolve().parents[3];root=a.output.resolve()
sys.path.insert(0,str(repo/'tools/vfx/six-card'))
from audit_log import audit
review=root/'evidence-review';review.mkdir(exist_ok=True)
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
plan=read(repo/'docs/worktree-tasks/vfx-boundaries-20261022/test-plan.json')
adapt=read(repo/'docs/worktree-tasks/vfx-boundaries-20261022/adaptation-plan.json')
enemy=read(repo/'docs/worktree-tasks/vfx-boundaries-20261022/enemy-plan.json')
expected={r['name'] for r in enemy}|{r['name'] for r in plan}|{r['name'] for r in adapt['states']}|{'actor-'+c.lower() for c in adapt['actors']}
summary={'scope':'Targeted single-player VFX boundaries and actor adaptation; no full multiplayer matrix.','branches':{},'imageManifest':[]}
for branch in ['stable','beta']:
    package=repo/'artifacts/ShoujoKagekiAijoKaren'
    build=dict(assemblySha256=sha(package/'lib'/f'ShoujoKagekiAijoKaren.{branch.title()}.dll'),packSha256=sha(package/'ShoujoKagekiAijoKaren.pck'))
    attempts=[];latest={};hands=[];hand_errors=[];logs=[];images=[];monitors=[]
    for folder in sorted(root.glob('round*-'+branch),key=lambda f:int(f.name.split('-')[0][5:]))+[root/branch]:
        if not folder.exists():continue
        for filename in ['boundary-results.json','adaptation-results.json','enemy-results.json']:
            result=folder/filename
            if not result.exists():continue
            try:rows=read(result)
            except ValueError:continue
            for row in rows:
                row=dict(row,source=str(folder.relative_to(root)))
                row['provenanceMatches']=row.get('build')==build
                attempts.append(row);latest[row['name']]=row
        for path in sorted(folder.glob('*/*hand.json')):
            report=read(path)
            if 'message' in report:report=json.loads(report['message'])
            if 'modelCount' not in report:continue
            valid=report['modelCount']==report['visualCount'] and not report['missing'] and not report['unexpected'] and not report['unexpectedPileIcons']
            hands.append(str(path.relative_to(root)))
            if not valid:hand_errors.append(dict(file=str(path.relative_to(root)),report=report))
        log=folder/'godot.log'
        if log.exists():
            contents=log.read_text(encoding='utf-8',errors='replace');raw=audit(contents);lines=contents.splitlines();fixture=[];production=[]
            for error in raw['unexpectedErrors']:
                tail=[]
                for line in lines[error['line']:error['line']+40]:
                    if line.startswith(('[INFO]','[WARN]','ERROR:','[ERROR]')):break
                    tail.append(line)
                block='\n'.join(tail)
                # Only the known test watcher null-tower stack is separated. Any other error still fails.
                if error['message'] in ('ERROR: System.InvalidOperationException: Sequence contains no elements','ERROR: Exception thrown while trying to log another exception...') and 'System.InvalidOperationException: Sequence contains no elements' in (error['message']+'\n'+block) and 'IntegrationWatch.get_Tower()' in block:
                    fixture.append(dict(error,category='test observer sampled during explicit fixture tower rebuild',stack=block))
                else:
                    category='unclassified'
                    if 'Attempt to disconnect a nonexistent connection' in error['message'] and 'SNCreatureVisuals._ExitTree' in block:category='VFX-02: mod entrance signal cleanup'
                    elif error['message']=='ERROR: Invalid Task ID' and 'wait_for_task_completion (core/object/worker_thread_pool.cpp:' in block:category='ENV-01: native worker-pool diagnostic; origin not isolated'
                    elif folder.name=='round2-stable' and any(t in block for t in ('EventSynchronizer.GetEventForPlayer','NRunMusicController.UpdateTrack','NTopBarHp.LerpAtNeow')):category='fixture: opening event replaced before initialization completed'
                    elif folder.name in ('round3-stable','round4-stable','round5-stable') and 'Error deleting path' in error['message'] and 'RunSaveManager.DeleteCurrentRun' in block:category='fixture: run ended while opening reward UI remained pending'
                    production.append(dict(error,stack=block,category=category))
            logs.append(dict(file=str(log.relative_to(root)),sha256=sha(log),knownStartupWorkerPoolErrors=raw['knownStartupWorkerPoolErrors'],fixtureObserverErrors=fixture,unexpectedErrors=production))
        monitor=folder/'window-monitor.json'
        if monitor.exists():monitors.append(dict(file=str(monitor.relative_to(root)),**read(monitor)))
        for case in sorted(p for p in folder.iterdir() if p.is_dir() and p.name.startswith(('boundary-','state-','actor-','enemy-'))):
            images.extend(sorted(case.glob('*.png')))
    actor_errors=[]
    for name,row in latest.items():
        if not name.startswith('actor-') or row['result']!='passed':continue
        actor=row.get('actor',{})
        valid=actor.get('character')==name.removeprefix('actor-').upper() and actor.get('hasShinePileButton') is False and actor.get('maxFootError',99)<.2 and actor.get('footFrames',0)>0 and actor.get('liveDepletes')==0 and {'NKarenBurnVfx','NKarenFormVfx'}<=set(actor.get('ownerVfx',[]))
        if not valid:actor_errors.append(dict(name=name,actor=actor))
    report=dict(actorErrors=actor_errors,build=build,expected=len(expected),passed=sum(r['result']=='passed' and r['provenanceMatches'] for r in latest.values()),
                failed=[r for r in latest.values() if r['result']!='passed' or not r['provenanceMatches']],
                pending=sorted(expected-set(latest)),latest=latest,attempts=attempts,handChecks=len(hands),handErrors=hand_errors,logs=logs,monitors=monitors)
    summary['branches'][branch]=report
    for offset in range(0,len(images),30):
        batch=images[offset:offset+30];rows=(len(batch)+4)//5
        sheet=Image.new('RGB',(1600,rows*208),(23,24,28));draw=ImageDraw.Draw(sheet);mapping=[]
        for i,path in enumerate(batch):
            x,y=(i%5)*320,(i//5)*208
            im=Image.open(path).convert('RGB');im.thumbnail((320,180));sheet.paste(im,(x,y+28))
            draw.text((x+4,y+2),path.parent.name[:48],fill='white');draw.text((x+4,y+14),path.stem[:48],fill=(180,200,220))
            mapping.append(dict(file=str(path.relative_to(root)),sha256=sha(path),column=i%5,row=i//5))
        dest=review/f'{branch}-all-{offset//30+1:02}.jpg';sheet.save(dest,quality=86)
        summary['imageManifest'].append(dict(sheet=str(dest.relative_to(root)),images=mapping))
execution_complete=all(not r['pending'] for r in summary['branches'].values())
complete=all(not r['pending'] and not r['failed'] and not r['handErrors'] and not r['actorErrors'] and not any(l['unexpectedErrors'] for l in r['logs']) and all(m['foreground_samples']==m['restored_samples']==0 for m in r['monitors']) for r in summary['branches'].values())
reviewed_file=review/'visual-reviewed.json'
reviewed={i['sha256'] for i in read(reviewed_file)['images']} if reviewed_file.exists() else set()
summary['unreviewedImages']=[i for sheet in summary['imageManifest'] for i in sheet['images'] if i['sha256'] not in reviewed]
summary['visualReviewComplete']=not summary['unreviewedImages']
summary['executionComplete']=execution_complete
summary['functionalCasesPassed']=all(not r['pending'] and not r['failed'] and not r['handErrors'] and not r['actorErrors'] for r in summary['branches'].values())
summary['automatedChecksPassed']=complete
summary['openFindings']=['VFX-01: deep Bridge additive light overexposure','VFX-02: Karen exit disconnects an absent entrance animation signal','ENV-01: beta menu worker-pool diagnostic; origin not isolated; see findings.md']
summary['complete']=complete and not summary['openFindings']
(review/'verification.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({b:{k:r[k] for k in ['expected','passed','pending','handChecks']}|{'failed':[f['name'] for f in r['failed']],'logErrors':sum(len(l['unexpectedErrors']) for l in r['logs'])} for b,r in summary['branches'].items()},ensure_ascii=False))
print('Contact sheets:',len(summary['imageManifest']),'complete:',complete)
if a.assert_complete:assert summary['complete'],'Inspect verification.json; incomplete/failed checks cannot count as passed'

if a.assert_functional:assert summary['functionalCasesPassed'],'Some cases are missing, failing or lack matching production build provenance'
