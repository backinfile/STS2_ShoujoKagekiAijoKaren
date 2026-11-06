"""Collate portable empty-pile evidence; optionally build screenshot contact sheets."""
import argparse,hashlib,json,os,re
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--sheets-only',action='store_true');args=parser.parse_args()
root=Path(__file__).resolve().parents[2];base=root/'artifacts/review-boundaries/after'
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def relative(path):return path.relative_to(root).as_posix()
if args.sheets_only:
    from PIL import Image,ImageDraw,ImageFont
    font=ImageFont.truetype(str(Path(os.environ['WINDIR'])/'Fonts/msyh.ttc'),18)
    for folder in sorted(base.glob('*-empty-*')):
        if not (folder/'results.json').exists():continue
        rows=json.loads((folder/'results.json').read_text(encoding='utf-8'));out=folder/'sheets';out.mkdir(exist_ok=True)
        for page in range((len(rows)+4)//5):
            group=rows[page*5:page*5+5];target=out/f'{page+1:02d}.jpg'
            if target.exists():
                with Image.open(target) as previous:
                    if previous.height==386*len(group):continue
            sheet=Image.new('RGB',(1280,386*len(group)),(18,22,30));draw=ImageDraw.Draw(sheet)
            for row,case in enumerate(group):
                for col,side in enumerate(['before','after']):
                    path=folder/(case['case']+'-'+side+'.png')
                    if not path.exists():path=folder/(case['case']+'-failure.png')
                    im=Image.open(path).convert('RGB');im.thumbnail((640,360));sheet.paste(im,(640*col,386*row+26));draw.text((640*col+5,386*row+3),case['case']+' / '+side,font=font,fill='white')
            sheet.save(target,quality=92)
        print(folder.name,len(rows),'cases;',len(list(out.glob('*.jpg'))),'sheets')
    raise SystemExit(0)
from emptiness import cases
expected={x['name'] for x in cases()};report=dict(date='2026-10-29',scope='empty-pile targeted singleplayer',scenariosPerVersion=len(expected),runs=[])
for branch in ['stable','beta']:
    selected={};attempts=[];logs=[];fixtures=[]
    for suffix in ['empty-matrix','empty-edge','empty-timing','empty-lethal']:
        folder=base/(branch+'-'+suffix)
        if not (folder/'results.json').exists():continue
        rows=json.loads((folder/'results.json').read_text(encoding='utf-8'))
        attempts.append(dict(path=relative(folder/'results.json'),sha256=digest(folder/'results.json'),passed=sum(x['result']=='pass' for x in rows),failures=[dict(case=x['case'],error=x.get('error')) for x in rows if x['result']!='pass']))
        for row in rows:selected[row['case']]=(folder,row)
        lines=(folder/'godot.log').read_text(encoding='utf-8',errors='replace').splitlines()
        errors=[dict(line=i+1,message=x.strip()) for i,x in enumerate(lines) if re.search(r'ERROR:|Exception',x)]
        common_end=next((i+1 for i,x in enumerate(lines) if "Preloading 'Common' Complete:" in x),0)
        for error in errors:error['phase']='startup' if error['line']<common_end else 'runtime'
        assert all(e['message']=='ERROR: Invalid Task ID' and e['phase']=='startup' for e in errors),errors
        order=[]
        if suffix=='empty-lethal':
            for i,line in enumerate(lines):
                if 'pile_empty op draw-win' not in line:continue
                empty=next(j for j in range(i+1,len(lines)) if 'triggered OnPromisePileEmpty' in lines[j])
                end=next(j for j in range(i+1,len(lines)) if '[PromisePile] AfterCombatEnd Patch' in lines[j])
                assert empty<end
                order.append(dict(commandLine=i+1,emptyTriggerLine=empty+1,combatCleanupLine=end+1))
        logs.append(dict(path=relative(folder/'godot.log'),sha256=digest(folder/'godot.log'),errors=errors,lethalEventOrder=order))
        variant=branch.title();impl=folder/f'game/mods/ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.{variant}.dll';built=root/f'artifacts/fixed/{branch}/ShoujoKagekiAijoKaren.{variant}.dll';assert digest(impl)==digest(built)
        fixture=folder/'game/mods/KarenBoundaryReview/KarenBoundaryReview.dll';fixtures.append(dict(path=relative(fixture),sha256=digest(fixture)))
    assert set(selected)==expected,(branch,expected-set(selected),set(selected)-expected)
    output=[];audits=0;ended=0
    for name in [x['name'] for x in cases()]:
        folder,row=selected[name];assert row['result']=='pass',(branch,name,row.get('error'))
        checks=[]
        for step in row['steps']:
            assert all(step['actual'].get(k)==v for k,v in step['expected'].items()),(name,step)
            checks.append(dict(operation=step['operation'],expected=step['expected'],actual={k:step['actual'][k] for k in step['expected']}))
        assert all(row['after'].get(k)==v for k,v in row['steps'][-1]['expected'].items()),(name,'final state',row['after'])
        hand=[]
        for key in ['handBefore','handAfter']:
            h=row[key]
            if h['status']=='not_applicable':ended+=1;hand.append(h);continue
            a=json.loads(h['message']);assert a['modelCount']==a['visualCount'] and not a['missing'] and not a['unexpected'] and not a['unexpectedPileIcons'],(name,a)
            assert all(x['valid'] for x in a.get('burnOverlays',[])),(name,a)
            audits+=1;hand.append(dict(status='ok',modelCount=a['modelCount'],visualCount=a['visualCount']))
        screenshots=[]
        for side in ['before','after']:
            p=folder/(name+'-'+side+'.png');assert p.is_file();screenshots.append(dict(path=relative(p),sha256=digest(p)))
        output.append(dict(case=name,result='pass',setup=row['setup'],checks=checks,handAudit=hand,screenshots=screenshots))
    report['runs'].append(dict(branch=branch,passed=len(output),handAuditsPassed=audits,combatEndedAudits=ended,attempts=attempts,logs=logs,fixtures=fixtures,cases=output))
# One case per line keeps the evidence compact and individually reviewable.
path=root/'docs/worktree-tasks/review-promise-rewards-20261029/empty-evidence.json'
head={k:v for k,v in report.items() if k!='runs'}
body=json.dumps(head,ensure_ascii=False,indent=2)[:-2]+',\n  "runs": [\n'
for ri,run in enumerate(report['runs']):
    metadata={k:v for k,v in run.items() if k!='cases'}
    body+='    '+json.dumps(metadata,ensure_ascii=False,separators=(',',':'))[:-1]+',"cases":[\n'
    body+=',\n'.join('      '+json.dumps(row,ensure_ascii=False,separators=(',',':')) for row in run['cases'])
    body+='\n    ]}'+(',' if ri+1<len(report['runs']) else '')+'\n'
body+='  ]\n}\n'
assert json.loads(body)==report
path.write_text(body,encoding='utf-8')
print(json.dumps([{k:r[k] for k in ['branch','passed','handAuditsPassed','combatEndedAudits']} for r in report['runs']],ensure_ascii=False))