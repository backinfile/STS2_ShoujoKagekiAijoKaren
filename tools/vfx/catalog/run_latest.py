"""Resume the current showcase without overwriting completed captures from this package."""
import argparse, datetime, hashlib, json, pathlib, shutil, subprocess, sys
from assemble import LATEST_ENTRIES

p=argparse.ArgumentParser()
p.add_argument('branch',choices=['stable','beta'])
p.add_argument('--output',type=pathlib.Path,required=True)
p.add_argument('--port',type=int,required=True)
a=p.parse_args()
repo=pathlib.Path(__file__).resolve().parents[3]
root=a.output.resolve()
package=repo/'artifacts/ShoujoKagekiAijoKaren'
expected=dict(assemblySha256=hashlib.sha256((package/'lib'/f'ShoujoKagekiAijoKaren.{a.branch.title()}.dll').read_bytes()).hexdigest(),
              packSha256=hashlib.sha256((package/'ShoujoKagekiAijoKaren.pck').read_bytes()).hexdigest())
pending=[]
for name,title in LATEST_ENTRIES:
    if name=='relay-transfer': continue  # Recorded by the real two-process relay harness.
    folder=root/a.branch/name
    try:
        rec=json.loads((folder/'recording.json').read_text(encoding='utf-8'))
        complete=rec['status']=='completed' and rec['captureBuild']==expected and (folder/'game.mp4').stat().st_size>0 and (folder/'markers.json').is_file()
    except (OSError,ValueError,KeyError): complete=False
    if not complete: pending.append(name)
local_total=len(LATEST_ENTRIES)-1
plan=dict(branch=a.branch,package=expected,total=len(LATEST_ENTRIES),pending=pending,deferred=['relay-transfer'],
          entries=[dict(name=n,title=t) for n,t in LATEST_ENTRIES])
(root/f'plan-{a.branch}.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'{local_total-len(pending)}/{local_total} local scenes already recorded; {len(pending)} remaining; relay recorded separately',flush=True)
if not pending: sys.exit(0)
tag=datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
try:
    subprocess.run([sys.executable,'-u',str(pathlib.Path(__file__).with_name('record.py')),a.branch,
                    '--output',str(root),'--port',str(a.port),'--scenes',','.join(pending)],check=True)
finally:
    for name in ('godot.log','window-monitor.json','silent-settings.json'):
        source=root/a.branch/name
        if source.exists(): shutil.copy2(source,root/f'{a.branch}-run-{tag}-{name}')
