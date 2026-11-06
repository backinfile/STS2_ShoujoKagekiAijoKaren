"""Validate the final VFX package and its exact runtime evidence before delivery.

Runs missing/stale desktop cases automatically. A release APK additionally needs
matching real ARM64 device evidence; a PC pass cannot upgrade it to released.
"""
from pathlib import Path
import argparse,hashlib,json,subprocess,sys,time,zipfile
ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).parent

def main():
 p=argparse.ArgumentParser(description=__doc__)
 p.add_argument('--source-dev',type=Path,required=True,help='Read-only parent of stable/ and beta/')
 p.add_argument('--target',choices=['apk','workshop'],required=True)
 p.add_argument('--apk',type=Path)
 p.add_argument('--package',type=Path,help='Final Mod directory; defaults to Android or desktop build by target')
 p.add_argument('--device-report',type=Path)
 p.add_argument('--matrix-report',type=Path)
 p.add_argument('--output',type=Path,default=ROOT/'artifacts/vfx-release-check.json')
 a=p.parse_args()
 subprocess.run([sys.executable,str(HERE/'catalog.py'),'--check'],cwd=ROOT,check=True)
 package=a.package.resolve() if a.package else ROOT/('artifacts/android/ShoujoKagekiAijoKaren' if a.target=='apk' else 'artifacts/ShoujoKagekiAijoKaren')
 hashes={str(f.relative_to(package)):hashlib.sha256(f.read_bytes()).hexdigest() for f in package.rglob('*') if f.is_file()}
 assert hashes,'Build the final Mod package first'
 report=dict(target=a.target,package_sha256=hashes,cases=[],pending=[],failures=[])
 for branch in ['stable','beta']:
  for effect in ['all','bridge','shine','promise']:
   evidence=None;invocation_succeeded=True
   for file in sorted((ROOT/'artifacts/vfx-first-use-20261022/runs').glob(branch+'-*/report.json'),reverse=True):
    r=json.loads(file.read_text(encoding='utf-8-sig'))
    if not r.get('effect_filter') and r.get('effect')==effect and r.get('renderer')=='opengl3' and r.get('package_sha256')==hashes and r.get('measurement_protocol',1)==(3 if effect=='all' else 1):
     evidence=file;break
   if evidence is None:
    completed=subprocess.run([sys.executable,str(HERE/'run.py'),'--source-env',str(a.source_dev/branch),'--branch',branch,'--port',str(15849 if branch=='stable' else 15850),'--effect',effect,'--package',str(package)],cwd=ROOT)
    invocation_succeeded=completed.returncode==0
    latest=sorted((ROOT/'artifacts/vfx-first-use-20261022/runs').glob(branch+'-*/report.json'),reverse=True)
    assert latest,'Missing runtime report'
    evidence=latest[0]
   r=json.loads(evidence.read_text(encoding='utf-8-sig'))
   passed=invocation_succeeded and not r.get('effect_filter') and bool(r.get('results')) and all(x['passed'] for x in r['results']) and r.get('package_sha256')==hashes and r.get('effect')==effect and r.get('branch')==branch and r.get('renderer')=='opengl3' and r.get('measurement_protocol',1)==(3 if effect=='all' else 1)
   log=(evidence.parent/'godot.log').read_text(encoding='utf-8',errors='replace')
   # Never silently discard startup errors; publish the exact lines for review.
   errors=[line for line in log.splitlines() if line.startswith('ERROR:') or '[KarenVfxWarmup] failed' in line or '[EXCEPTION]' in line]
   entry=dict(branch=branch,effect=effect,passed=passed,evidence=evidence.relative_to(ROOT).as_posix(),log_errors=errors)
   report['cases'].append(entry)
   if not passed:report['failures'].append(branch+'/'+effect)
   if errors:report['pending'].append('Review logged errors: '+branch+'/'+effect)
 if a.target=='apk':
  assert a.apk and a.apk.is_file(),'--apk must identify the final signed candidate'
  subprocess.run([sys.executable,str(ROOT/'tools/android-standalone/test.py'),'--apk',str(a.apk)],cwd=ROOT,check=True)
  report['apk_sha256']=hashlib.sha256(a.apk.read_bytes()).hexdigest()
  with zipfile.ZipFile(a.apk) as archive:
   for name,digest in hashes.items():
    asset='assets/direct/files/mods/ShoujoKagekiAijoKaren/'+name.replace('\\','/')
    assert hashlib.sha256(archive.read(asset)).hexdigest()==digest,'APK contains a different Mod: '+name
   mod_entries=[n for n in archive.namelist() if n.startswith('assets/direct/files/mods/')]
   assert all(n.split('/')[4] in ('ShoujoKagekiAijoKaren','BaseLib') for n in mod_entries),'Test Mod leaked into APK'
  report['embedded_mod_matches_tested_package']=True
  required={'fresh_install','upgrade_keeps_save','cold_first_vfx','background_resume'}
  device=json.loads(a.device_report.read_text(encoding='utf-8-sig')) if a.device_report else {}
  if not(device.get('apk_sha256')==report['apk_sha256'] and device.get('platform')=='Android ARM64' and all(device.get('checks',{}).get(k) is True for k in required)):
   report['pending'].append('Android ARM64 device checks for this exact APK')
 else:
  matrix=json.loads(a.matrix_report.read_text(encoding='utf-8-sig')) if a.matrix_report else {}
  if not(matrix.get('package_sha256')==hashes and matrix.get('full_dual_version_matrix_passed') is True):
   report['pending'].append('Full stable/beta singleplayer and same-version multiplayer matrix for this exact package')
 report['status']='failed' if report['failures'] else 'pending' if report['pending'] else 'passed'
 a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
 print(json.dumps({k:v for k,v in report.items() if k not in ('package_sha256','cases')},ensure_ascii=False,indent=2))
 return 1 if report['failures'] else 2 if report['pending'] else 0
if __name__=='__main__':sys.exit(main())
