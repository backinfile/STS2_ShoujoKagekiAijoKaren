"""Archive compact runtime evidence and inspect audio, capture health, hashes, and logs."""
import argparse,gzip,hashlib,json,re,shutil,subprocess
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--run',default='checks');parser.add_argument('--expected-cases',type=int,default=10);args=parser.parse_args()
project=Path(__file__).resolve().parents[3];root=project/'artifacts/shine-barrage';delivery=project/'docs/worktree-tasks/shine-barrage-vfx-20261017';evidence=delivery/('evidence' if args.run=='checks' else 'evidence-'+args.run);evidence.mkdir(exist_ok=True);summary={}
for branch in ['stable','beta']:
    checks=root/branch/args.run;results=json.loads((checks/'results.json').read_text(encoding='utf-8'));assert len(results)==args.expected_cases and all(r['passed'] for r in results)
    saved=evidence/branch;saved.mkdir(exist_ok=True);shutil.copy2(checks/'results.json',saved/'results.json');shutil.copy2(checks/'build.json',saved/'build.json');(saved/'godot.log.gz').write_bytes(gzip.compress((checks/'godot.log').read_bytes(),mtime=0))
    nodes=0;max_error=0;penetration=[]
    for result in results:
        case=checks/result['case'];data=json.loads((case/'probe.json').read_text(encoding='utf-8'));assert data['live']==data['childLive']==0
        penetration.extend(e['maxBeyondSilhouette'] for e in data['effects'] if e['impact'] and result['case']!='lethal' and 'maxBeyondSilhouette' in e);nodes+=len(data['effects']);max_error=max(max_error,max((e['maxAnchorError'] for e in data['effects']),default=0))
        dest=saved/result['case'];dest.mkdir(exist_ok=True)
        for name in ['probe.json','before-hand.json','after-hand.json']:
            if (case/name).is_file():shutil.copy2(case/name,dest/name)
        if result['case'] in ['normal','last','pose','lethal']:
            for name in ['before.png','after.png']:shutil.copy2(case/name,dest/name)
    videos=[]
    for name in ['normal','upgrade','replay']:
        case=checks/name;record=json.loads((case/'recording.json').read_text(encoding='utf-8'));assert record['audio'] and record['audio_source']=='game_process' and record['encoder']=='h264_nvenc'
        assert record['capture_fps']>24 and record['frames_repeated']/record['frames_written']<.15,record
        volume=subprocess.run(['ffmpeg','-hide_banner','-i',str(case/'game.mp4'),'-af','volumedetect','-vn','-sn','-f','null','NUL'],capture_output=True,text=True).stderr
        peak=float(re.search(r'max_volume: ([\-\d.]+)',volume)[1]);mean=float(re.search(r'mean_volume: ([\-\d.]+)',volume)[1]);assert -70<mean and peak<0,(branch,name,mean,peak)
        videos.append(dict(case=name,capture_fps=record['capture_fps'],repeated=record['frames_repeated'],written=record['frames_written'],encoder=record['encoder'],mean_db=mean,peak_db=peak));shutil.copy2(case/'recording.json',saved/name/'recording.json')
    # The first GPU-readback capture pass is separate from all deliverable recordings.
    contact=root/branch/('visual' if args.run=='checks' else args.run+'-visual')/'normal/contact.png';assert contact.is_file();shutil.copy2(contact,saved/'contact.png')
    log=(checks/'godot.log').read_text(encoding='utf-8',errors='replace');errors=[line for line in log.splitlines() if re.search(r'ERROR|Exception|Invalid Task ID|Shader compilation failed',line)]
    common_done=log.index("Preloading 'Common' Complete:")
    startup=[line for line in log[:common_done].splitlines() if line=='ERROR: Invalid Task ID']
    unexpected=errors.copy()
    for line in startup:unexpected.remove(line)
    build=json.loads((checks/'build.json').read_text(encoding='utf-8'))
    for name,digest in build.items():assert hashlib.sha256((project/'artifacts/ShoujoKagekiAijoKaren'/name).read_bytes()).hexdigest()==digest
    summary[branch]=dict(passed=len(results),observed_nodes=nodes,max_anchor_error=max_error,unexpectedErrors=unexpected,startupDiagnostics=startup,minPenetrationBeyondSilhouette=min(penetration) if penetration else None,recordings=videos)
assert not any(v['unexpectedErrors'] for v in summary.values()),summary
(delivery/('validation.json' if args.run=='checks' else 'validation-'+args.run+'.json')).write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(summary,ensure_ascii=False,indent=2))
