"""Check B04 native results, finite visual ownership, real recordings and provenance."""
import argparse,functools,hashlib,json,pathlib,re,subprocess
p=argparse.ArgumentParser();p.add_argument('--root',type=pathlib.Path,required=True);p.add_argument('--report',type=pathlib.Path,required=True);a=p.parse_args()
project=pathlib.Path(__file__).resolve().parents[3];root=a.root.resolve()
def load(p):return json.loads(p.read_text(encoding='utf-8-sig'))
@functools.lru_cache(None)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
cards=load(pathlib.Path(__file__).with_name('cards.json'));ids={c['id'] for c in cards}
all_cases=[];counts={};baseline={}
for phase in ('before','after'):
 for env in ('stable','beta'):
  rows=load(root/phase/env/'results.json');counts[phase+'_'+env]=len(rows)
  assert {r['card'] for r in rows if r['mode']=='full'}==ids
  if phase=='after':
   required={'full':ids,'low':ids if env=='stable' else {'KAREN_REWRITE','KAREN_DROP_FUEL','KAREN_STARLIGHT02_CARD'},
    'replay':{'KAREN_CONTINUE','KAREN_DROP_FUEL'},'cancel':{'KAREN_REWRITE','KAREN_STAR'}}
   if env=='stable':required.update(off={'KAREN_CONTINUE','KAREN_REWRITE','KAREN_DROP_FUEL'},fast={'KAREN_REWRITE','KAREN_STAR','KAREN_DROP_FUEL'},instant={'KAREN_STAR','KAREN_BRIDGE','KAREN_STARLIGHT02_CARD'},stress={'KAREN_REWRITE','KAREN_STAR'},upgrade={'KAREN_CONTINUE','KAREN_DROP_FUEL'})
   for mode,wanted in required.items():assert {r['card'] for r in rows if r['mode']==mode}==wanted,(env,mode)
  package=root/'packages'/phase
  for r in rows:
   mode,card=r['mode'],r['card'];folder=root/phase/env/(mode+'-'+card.lower())
   before,after,status=[load(folder/(n+'.json')) for n in ('before','after','status')]
   rec=load(folder/'recording.json');video=folder/'game.mp4'
   assert r['passed'] and rec['status']=='completed' and rec['audio'] and rec['audio_source']=='engine_mix'
   media=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_streams','-of','json',str(video)]))['streams']
   assert any(x['codec_type']=='video' and x['width']==1280 and x['height']==720 for x in media)
   assert any(x['codec_type']=='audio' for x in media)
   volume=subprocess.run(['ffmpeg','-hide_banner','-i',str(video),'-af','volumedetect','-f','null','-'],capture_output=True,text=True).stderr
   m=re.search(r'mean_volume: ([-.0-9]+) dB',volume);assert m and float(m[1])>-75,(video,'audio')
   for name,wanted in r['build'].items():assert sha(package/name)==wanted,(folder,name)
   for name in ('before-hand','after-hand'):
    hand=json.loads(load(folder/(name+'.json'))['message'])
    assert hand['modelCount']==hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons']
   assert not status['effects'] and not status['poseActive'] and status['chain']['playPile']==0
   assert status['maxAttackOffset']<=10
   assert all(status['exited'].get(k,0)>=v for k,v in status['spawned'].items())
   plays=status['chain']['plays'];n=lambda id:sum(x['id']==id for x in plays)
   if card=='KAREN_CONTINUE':assert r['damage']==(6 if mode=='upgrade' else 4)*(3 if mode=='replay' else 1)
   if card=='KAREN_REWRITE':
    expected=11 if mode=='stress' else 7
    assert n(card)==expected and r['damage']==expected*10
    if phase=='after' and r['quality']!='Off' and r['speed']!='instant' and mode!='cancel':assert status['spawned']['NKarenStageAttackVfx']==1 and status['totalContacts']==expected
   if card=='KAREN_STAR':
    assert n('KAREN_SHINE_STRIKE')==(9 if mode=='stress' else 5) and status['chain']['extraRewards']==1 and status['chain']['sourceDepleted']
    if phase=='after':assert status['chain']['maxPulses']<=(2 if r['quality']=='Low' else 4)
   if card=='KAREN_BRIDGE':assert n('KAREN_STRIKE')==3 and status['promise']==0
   if card=='KAREN_GEER':
    assert n('KAREN_STRIKE')==1 and status['promise']==1
    assert any(p['id']=='KAREN_GEER_POWER' and p['amount']==1 for p in status['powers'])
   if card=='KAREN_DROP_FUEL':
    count=(3 if mode=='upgrade' else 2)*(3 if mode=='replay' else 1)
    assert status['hand']==count and after['player']['energy']-before['player']['energy']==count
   if card=='KAREN_STARLIGHT02_CARD':
    assert n('KAREN_SHINE_STRIKE')==3 and status['chain']['sourceDepleted']
    assert any(p['id']=='KAREN_STARLIGHT02_POWER' and p['amount']==2 for p in status['powers'])
   if mode=='cancel':
    if card=='KAREN_REWRITE':assert status['totalContacts']<7 and status['spawned'].get('NKarenStageAttackVfx')==1
    if card=='KAREN_STAR':assert status['chain']['stars'] and all(c['played']<5 and not c['reward'] for c in status['chain']['stars'])
   assert status['chain'].get('starBackLive',0)==0
   if r['quality']=='Off' or r['speed']=='instant':assert not status['spawned']
   if phase=='before':baseline[env,card]=r
   elif mode in ('full','low','off','fast','instant'):
    original=baseline[env,card]
    assert r['damage']==original['damage'] and plays==original['status']['chain']['plays'],(env,mode,card)
   all_cases.append(dict(phase=phase,environment=env,mode=mode,card=card,damage=r['damage'],passed=True,
    video=video.relative_to(project).as_posix(),sha256=sha(video),audio_mean_db=float(m[1]),build=r['build'],status=status))
def engine_observations(root,project):
 observations=[]
 for phase in ('before','after'):
  for env in ('stable','beta'):
   for path in sorted((root/phase/env).glob('godot*.log')):
    lines=path.read_text(encoding='utf-8',errors='replace').splitlines()

    for i,line in enumerate(lines):
     if line.startswith('ERROR:'):
      assert line=='ERROR: Invalid Task ID' and i+1<len(lines) and 'worker_thread_pool.cpp:418' in lines[i+1],(path.relative_to(project).as_posix(),i+1,line)
      starts=[(j,re.search(r"Preloading '(Common|Combat Room)' assets",v)) for j,v in enumerate(lines[:i])]
      starts=[(j,m[1]) for j,m in starts if m];assert starts,(path.name,i+1)
      begin,stage=starts[-1]
      completed=next(j for j in range(begin+1,len(lines)) if f"Preloading '{stage}' Complete:" in lines[j])
      assert begin<i<completed,(path.name,i+1,stage)
      observations.append(dict(phase=phase,environment=env,log=path.relative_to(project).as_posix(),line=i+1,message=f'Invalid Task ID during native {stage} preloading; preloading subsequently completed',historical_reference='docs/testing/heavy-attack-redesign-2026-10-30.md'))
     assert 'Exception' not in line and not ('Shader' in line and 'error' in line.lower()),(path.name,i+1)
 return observations

manifest=load(root/'source-at-build.json');sources=manifest['source_sha256']
for name,wanted in sources.items():assert sha(project/name)==wanted,name
result=dict(date='2026-10-31',batch='B04',baseline=manifest['baseline'],status='targeted verified; visual review and merge pending',counts=counts,cases=all_cases,source_sha256=sources,engine_observations=engine_observations(root,project),untested=['full singleplayer/multiplayer matrix','full first-use/frame-time suite','Android'])
a.report.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(dict(counts=counts,recordings=len(all_cases),sources=len(sources))))
