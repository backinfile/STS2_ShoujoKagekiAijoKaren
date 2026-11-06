"""Verify B02 recordings and native outcomes, with no game actions."""
import argparse,hashlib,json,pathlib,re,subprocess,sys
p=argparse.ArgumentParser();p.add_argument('--root',type=pathlib.Path,required=True);p.add_argument('--report',type=pathlib.Path);p.add_argument('--redesign',action='store_true');p.add_argument('--stationary',action='store_true');p.add_argument('--nonon-particles',action='store_true');a=p.parse_args()
project=pathlib.Path(__file__).resolve().parents[3];root=a.root.resolve()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def load(p):return json.loads(p.read_text(encoding='utf-8-sig'))
cards={'KAREN_SHINE_TOGETHER':30,'KAREN_NONON':12,'KAREN_SHINE_STRIKE_BARRAGE':12,'KAREN_BACK_TO_BACK_CARD':32,'KAREN_BASEBALL':24,'KAREN_JEALOUSY':15,'KAREN_CARRYING_GUILT':12}
if a.nonon_particles:cards={'KAREN_NONON':12}
retention={'RETAIN_HAND_POWER','KAREN_RETAIN_ENERGY_POWER','BLUR_POWER','KAREN_RETAIN_TMP_STRENGTH_POWER'}
all_cases=[];counts={};indexes={}
for phase in ('before','after'):
 for env in ('stable','beta'):
  cases=load(root/phase/env/'results.json');counts[phase+'_'+env]=len(cases)
  assert {r['card'] for r in cases if r['mode']=='full'}==set(cards)
  indexes[phase,env]={(r['mode'],r['card']):r for r in cases}
  package=root/'packages'/('before' if phase=='before' else 'final-v2' if a.redesign else 'final')
  for r in cases:
   folder=root/phase/env/(r['mode']+'-'+r['card'].lower());video=folder/'game.mp4';record=load(folder/'recording.json');status=load(folder/'status.json')
   assert record['status']=='completed' and record['audio'] and record['audio_source'] in ('engine_mix','game_process')
   assert record['frames_written']>30 and record['frames_captured']>30
   info=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_streams','-show_format','-of','json',str(video)]))
   assert any(s['codec_type']=='audio' for s in info['streams'])
   assert any(s['codec_type']=='video' and s['width']==1280 and s['height']==720 for s in info['streams'])
   audio=subprocess.run(['ffmpeg','-hide_banner','-i',str(video),'-vn','-af','volumedetect','-f','null','-'],capture_output=True).stderr.decode('utf-8',errors='replace')
   match=re.search(r'mean_volume: ([\w.+-]+) dB',audio);assert match and float(match[1])>-75,(video,'audio')
   for name,value in r['build'].items():assert sha(package/name)==value,(folder,name)
   for name in ('before','after'):
    assert (folder/(name+'.png')).is_file()
    hand=json.loads(load(folder/(name+'-hand.json'))['message'])
    assert hand['modelCount']==hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons']
   expected=8 if r['mode']=='stress' else 120 if r['mode']=='stock' else 15 if r['mode']=='upgrade' else 36 if r['mode'] in ('area','replay') else cards[r['card']]
   assert r['damage']==expected and r['block']==0 and status['hand']==0,(folder,r['damage'],expected)
   assert not [e for e in status['effects'] if e['type']!='NKarenUnforgottenShineVfx']
   if r['card']=='KAREN_BASEBALL':
    powers={p['id']:p['amount'] for p in status['powers']};assert all(powers.get(k)==2 for k in retention)
   if a.nonon_particles:
    for enemy in load(folder/'after.json')['battle']['enemies']:
     powers={p['id']:p['amount'] for p in enemy['status']}
     assert powers.get('STRENGTH_POWER')==-12 and powers.get('KAREN_NONON_STRENGTH_DOWN_POWER')==12,(folder,powers)
    if phase=='before':assert not status['spawned'].get('NKarenNononVfx',0),(folder,'baseline particle layer already present')
   if phase=='after':
    if a.nonon_particles:subprocess.run([sys.executable,str(pathlib.Path(__file__).with_name('check_nonon_particles.py')),str(folder/'status.json'),'--quality',r['quality'],'--speed',r['speed']],check=True)
    base=indexes['before',env].get((r['mode'],r['card']))
    if base is None and r['mode'] not in ('upgrade','area','replay','stock','stress'):base=indexes['before',env]['full',r['card']]
    if base is not None and r['mode'] not in ('budget','budget_low'):assert r['damage']==base['damage'] and r['block']==base['block']
    if (a.redesign or a.stationary) and r['quality']!='Off' and r['speed']!='instant':
     assert status['spawned'].get('NKarenStageAttackVfx')==(3 if r['mode']=='replay' else 1)
     assert not status['poseActive'] and status['bodyZ']==load(folder/'before-status.json')['bodyZ']
     if r['mode']!='cancel':assert status['poseFrames']>0 and status['maxAttackOffset']>(1 if a.stationary else 5)
    if a.stationary:
     assert status['maxAttackOffset']<=18,(folder,status['maxAttackOffset'])
     assert not status['poseActive'] and status['bodyRelative']==load(folder/'before-status.json')['bodyRelative']
    if r['mode'] in ('budget','budget_low'):
     assert status['scars']==(6 if r['mode']=='budget_low' else 12) and status['reuseStable']
    if r['mode']=='stress':assert status['maxScars']==4
    if r['quality']=='Off' or r['speed']=='instant':assert not status['spawned']
   rec={k:record[k] for k in ('frames_written','frames_captured','frames_repeated','frames_dropped','capture_fps','encoder','audio_source')}
   all_cases.append(dict(phase=phase,environment=env,mode=r['mode'],card=r['card'],quality=r['quality'],speed=r['speed'],damage=r['damage'],block=r['block'],passed=True,
     video=video.relative_to(project).as_posix(),sha256=sha(video),build=r['build'],status=status,recording=rec,audio_mean_db=float(match[1])))
# Production changes only; fingerprints normalize encoding/newlines, matching the reviewed source.
previous=load(a.report) if a.report and a.report.exists() else None
changed=list(previous['source_sha256']) if previous else subprocess.check_output(['git','diff','--name-only','-z','HEAD'],cwd=project).decode('utf-8').split('\0')
sources={name:hashlib.sha256(((project/name).read_text(encoding='utf-8-sig').rstrip()+'\n').encode('utf-8')).hexdigest() for name in changed if name.startswith(('src/','shaders/','materials/')) and (project/name).is_file()}
if previous:assert sources==previous['source_sha256'],'Production source differs from the recorded package fingerprint'
result=dict(date='2026-10-30',batch='NONNON particle layer restoration' if a.nonon_particles else 'B02 restrained movement' if a.stationary else 'B02 reference redesign' if a.redesign else 'B02',baseline='064432f2cc39536032c1ada8e9b035a520ab4c1a' if a.nonon_particles else '221c92764c51dff65735683259d1fc0495f44a1a' if a.stationary else '28163aab2fc08444e62a581cf1b5d7797ef856e4' if a.redesign else '40fee16ab1a03a75a96723a916a3da86636712c3',status='targeted verified; merge pending',counts=counts,cases=all_cases,source_sha256=sources,
 engine_baseline_issue=dict(message='Invalid Task ID at worker_thread_pool.cpp:418 during beta Common preloading, present before and after; gameplay cases passed',logs=[('artifacts/heavy-batch02-20261030/after/beta/godot.log' if a.redesign else (root/'before/beta/godot.log').relative_to(project).as_posix()),(root/'after/beta/godot.log').relative_to(project).as_posix()]),
 untested=['full multiplayer matrix','full first-use/frame-time suite','Android'])
if a.stationary:
 result['engine_baseline_issue']['message']='Invalid Task ID appears in the historical before beta Common preload log; neither after log contains ERROR or Exception for this revision'
 result['movement_limit']=dict(recording_limit_px=12,offline_limit_px=18,scope='owned pose translation, fixed 1280x720 fixtures')
 result['not_repeated']=['low/off quality, upgrade, area, replay, mirror and cancellation: previous reference-redesign results retained; this revision changes only local amplitude and removes the echo']
if a.nonon_particles:
 result.pop('engine_baseline_issue',None)
 result['not_repeated']=['other cards and broad matrices: changes limited to NONNON particle layer, rendering tail and passive test observer']
if a.redesign and (root/'basic-regression/results.json').exists():
 basic=[]
 for r in load(root/'basic-regression/results.json'):
  folder=root/'after/stable'/(r['mode']+'-'+r['card'].lower());video=folder/'game.mp4'
  expected={'KAREN_STRIKE':(6,0),'KAREN_DEFEND':(0,5),'KAREN_SHINE_STRIKE':(9,0)}[r['card']]
  assert (r['damage'],r['block'])==expected and not r['status']['effects'] and not r['status']['poseActive']
  for name,value in r['build'].items():assert sha(root/'packages/final-v2'/name)==value
  sound=subprocess.run(['ffmpeg','-hide_banner','-i',str(video),'-vn','-af','volumedetect','-f','null','-'],capture_output=True).stderr.decode('utf-8',errors='replace')
  match=re.search(r'mean_volume: ([\w.+-]+) dB',sound);assert match and float(match[1])>-75
  basic.append(dict(card=r['card'],mode=r['mode'],damage=r['damage'],block=r['block'],passed=True,video=video.relative_to(project).as_posix(),sha256=sha(video),audio_mean_db=float(match[1])))
 result['basic_regression']=basic
if a.report:a.report.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(counts=counts,verified_recordings=len(all_cases),source_files=len(sources))))
