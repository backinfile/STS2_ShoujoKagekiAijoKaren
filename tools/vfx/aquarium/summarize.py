"""Validate observed results and archive review evidence; never substitutes for viewing screenshots/video."""
import argparse, gzip, hashlib, json, re, shutil, subprocess
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--run',default='transient');parser.add_argument('--cases',default='normal,stack,void,fast,instant,empty,cancel,switch,remove,room');args=parser.parse_args()
ROOT=Path(__file__).resolve().parents[3];raw=ROOT/'artifacts/aquarium';out=ROOT/'docs/worktree-tasks/aquarium-vfx-20261017/evidence'/args.run;out.mkdir(parents=True,exist_ok=True)
def save(p,v):p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
expected=set(args.cases.split(','))
summary={'scope':'targeted singleplayer, not full or multiplayer','cases':{},'recordings':[],'logAudit':[],'handChecks':0}
for branch in ['stable','beta']:
    package=ROOT/'artifacts/ShoujoKagekiAijoKaren'
    hashes={name:hashlib.sha256((package/name).read_bytes()).hexdigest() for name in ['ShoujoKagekiAijoKaren.pck',f'lib/ShoujoKagekiAijoKaren.{branch.title()}.dll']}
    for kind in ['visual','recordings']:
        run=args.run+'-'+kind
        folder=raw/branch/run
        assert json.loads((folder/'build.json').read_text(encoding='utf-8'))==hashes,'Stale tested package'
        results=json.loads((folder/'results.json').read_text(encoding='utf-8'))
        if kind=='visual':assert {r['case'] for r in results}==expected
        assert all(r['passed'] for r in results)
        summary['cases'][branch+'/'+run]=[r['case'] for r in results]
        dest=out/branch/run;dest.mkdir(parents=True,exist_ok=True)
        for filename in ['results.json','build.json','actions.jsonl']:shutil.copy2(folder/filename,dest/filename)
        log=(folder/'godot.log').read_text(encoding='utf-8',errors='replace')
        bad=[(i,line) for i,line in enumerate(log.splitlines(),1) if re.search(r'ERROR:|Exception|SHADER ERROR',line)]
        common=next(i for i,line in enumerate(log.splitlines(),1) if "Preloading 'Common' Complete:" in line)
        for line_no,line in bad:
            assert branch=='beta' and line=='ERROR: Invalid Task ID' and line_no<common,(branch,run,line_no,line)
        summary['logAudit'].append(dict(branch=branch,run=run,startupInvalidTaskIds=len(bad),unexpectedErrors=0))
        (dest/'godot.log.gz').write_bytes(gzip.compress((folder/'godot.log').read_bytes(),mtime=0))
        for result in results:
            case=folder/result['case'];d=dest/result['case'];d.mkdir(exist_ok=True)
            for file in case.glob('*.json'):shutil.copy2(file,d/file.name)
            if kind=='visual':
                for file in case.glob('*.png'):shutil.copy2(file,d/file.name)
            for file in case.glob('*hand.json'):
                h=json.loads(file.read_text(encoding='utf-8'));assert h['modelCount']==h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'] and all(b['valid'] for b in h['burnOverlays']),file
                summary['handChecks']+=1
            if (case/'recording.json').exists():
                rec=json.loads((case/'recording.json').read_text(encoding='utf-8'))
                assert rec['status']=='completed' and rec['audio'] and rec['audio_source']=='game_process' and rec['encoder']=='h264_nvenc'
                assert rec['capture_fps']>24 and rec['frames_dropped']==0,rec
                probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_streams','-show_format','-of','json',str(case/'game.mp4')]))
                audio=subprocess.run(['ffmpeg','-hide_banner','-i',str(case/'game.mp4'),'-vn','-af','volumedetect','-f','null','-'],capture_output=True,text=True).stderr
                mean=float(re.search(r'mean_volume: ([\d.-]+) dB',audio)[1]);peak=float(re.search(r'max_volume: ([\d.-]+) dB',audio)[1]);assert mean>-60 and peak>-40
                stats=dict(branch=branch,case=result['case'],captureFps=rec['capture_fps'],repeated=rec['frames_repeated'],dropped=rec['frames_dropped'],seconds=rec['video_duration_seconds'],audioMeanDb=mean,audioPeakDb=peak)
                summary['recordings'].append(stats);save(d/'media-audit.json',dict(stats=stats,probe=probe))
    for file in (raw/('qa-'+args.run)/branch).glob('*.jpg'):
        d=out/'review'/branch;d.mkdir(parents=True,exist_ok=True);shutil.copy2(file,d/file.name)
review=raw/('video-review-'+args.run)
for file in review.glob('*.jpg'):
    dest=out/'video-review';dest.mkdir(exist_ok=True);shutil.copy2(file,dest/file.name)
save(out/'summary.json',summary)
print(json.dumps(summary,ensure_ascii=False,indent=2))
