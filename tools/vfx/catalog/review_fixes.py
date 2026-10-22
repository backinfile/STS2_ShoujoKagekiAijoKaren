"""Audit targeted fixes without treating ENV-01 as a resolved defect."""
import argparse,hashlib,json,pathlib,sys
from PIL import Image,ImageDraw
p=argparse.ArgumentParser();p.add_argument('--output',type=pathlib.Path,required=True);args=p.parse_args()
repo=pathlib.Path(__file__).resolve().parents[3];root=args.output.resolve();review=root/'review';review.mkdir(exist_ok=True)
sys.path.insert(0,str(repo/'tools/vfx/six-card'))
from audit_log import audit
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
source_names=['run_boundaries.py','fix_scenes.py','VfxFixProbe.cs','adaptation_scenes.py','boundary_scenes.py','review_fixes.py']
result={'sourceHashes':{name:sha(repo/'tools/vfx/catalog'/name) for name in source_names},'scope':'Only VFX-01/VFX-02 and targeted regressions; ENV-01 remains unresolved.','branches':{},'images':[]}
for branch in ['stable','beta']:
    folder=root/branch
    expected={'boundary-empty','boundary-chain2','boundary-void-chain','boundary-replay','boundary-fast','boundary-instant','boundary-lethal','boundary-next-clean'}
    boundaries=read(folder/'boundary-results.json');fixes=read(folder/'fix-results.json')
    assert {r['name'] for r in boundaries}==expected,boundaries
    assert len(fixes)==14,fixes
    assert all(r['result']=='passed' for r in boundaries+fixes),[(r['name'],r['result']) for r in boundaries+fixes]
    build={'assemblySha256':sha(repo/'artifacts/ShoujoKagekiAijoKaren/lib'/f'ShoujoKagekiAijoKaren.{branch.title()}.dll'),'packSha256':sha(repo/'artifacts/ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck')}
    assert all(r['build']==build for r in boundaries+fixes)
    hands=[]
    for path in sorted(folder.glob('*/*hand.json')):
        raw=read(path);h=json.loads(raw['message'])
        assert h['modelCount']==h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'],(path,h)
        hands.append(str(path.relative_to(root)))
    monitor=read(folder/'window-monitor.json');assert monitor['foreground_samples']==monitor['restored_samples']==0,monitor
    log=(folder/'godot.log').read_text(encoding='utf-8',errors='replace');errors=audit(log)
    assert 'Attempt to disconnect a nonexistent connection' not in log,errors
    # Permit only the separately reported, unresolved worker-pool diagnostic.
    others=[e for e in errors['unexpectedErrors'] if e['message']!='ERROR: Invalid Task ID']
    assert not others,others
    for error in errors['unexpectedErrors']:
        assert 'wait_for_task_completion (core/object/worker_thread_pool.cpp:' in '\n'.join(log.splitlines()[error['line']:error['line']+32]),error
    result['branches'][branch]={'boundaries':boundaries,'fixes':fixes,'build':build,'handChecks':len(hands),'handFiles':hands,'monitor':monitor,'logAudit':errors,'logSha256':sha(folder/'godot.log')}
    images=sorted(folder.glob('*/*.png'))
    for path in images:result['images'].append({'file':str(path.relative_to(root)),'sha256':sha(path)})
    for start in range(0,len(images),48):
        batch=images[start:start+48]
        rows=(len(batch)+3)//4;sheet=Image.new('RGB',(1280,rows*207),(18,18,18));draw=ImageDraw.Draw(sheet)
        for i,path in enumerate(batch):
            shot=Image.open(path).convert('RGB');shot.thumbnail((320,180));x=(i%4)*320;y=(i//4)*207
            sheet.paste(shot,(x,y));draw.text((x+3,y+182),str(path.relative_to(folder))[:48],fill='white')
        sheet.save(review/f'{branch}-{start//48+1}.jpg',quality=86)
(review/'results.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'branches':{b:{'checks':len(r['boundaries'])+len(r['fixes']),'hands':r['handChecks'],'remainingEnvironmentErrors':len(r['logAudit']['unexpectedErrors'])} for b,r in result['branches'].items()},'images':len(result['images'])},ensure_ascii=False))
