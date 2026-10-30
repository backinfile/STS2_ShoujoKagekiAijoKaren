"""Verify recorded B03 native rules, quality gates, visual ownership and package provenance."""
import argparse,functools,hashlib,json,pathlib,re,subprocess
p=argparse.ArgumentParser();p.add_argument('--root',type=pathlib.Path,required=True);p.add_argument('--report',type=pathlib.Path,required=True);p.add_argument('--semantics',action='store_true');p.add_argument('--directions',action='store_true');p.add_argument('--batch-label');p.add_argument('--cards',default='');a=p.parse_args()
project=pathlib.Path(__file__).resolve().parents[3];root=a.root.resolve()
def load(path):return json.loads(path.read_text(encoding='utf-8-sig'))
@functools.lru_cache(None)
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
cards=load(pathlib.Path(__file__).with_name('cards.json'))
if a.cards:
 wanted=set(a.cards.split(','));assert wanted<={r['id'] for r in cards};cards=[r for r in cards if r['id'] in wanted]
ids={r['id'] for r in cards}
base={r['id']:r['base_damage'] for r in cards};base['KAREN_SPIN']=7;base['KAREN_NONON']=12
rounds={'KAREN_DEBUT':3,'KAREN_HOLY_STAR':2,'KAREN_CONFRONT':3,'KAREN_COUNTER':3}
all_cases=[];counts={};indexes={}
for phase in ('before','after'):
 for env in ('stable','beta'):
  rows=load(root/phase/env/'results.json');counts[phase+'_'+env]=len(rows)
  assert {r['card'] for r in rows if r['mode']=='full' and r['card'] in ids}==ids
  indexes[phase,env]={(r['mode'],r['card']):r for r in rows}
  package=root/'packages'/('before' if phase=='before' else ('final' if a.semantics else 'final-v3'))
  for r in rows:
   card=r['card'];mode=r['mode'];folder=root/phase/env/(mode+'-'+card.lower());video=folder/'game.mp4';rec=load(folder/'recording.json');status=load(folder/'status.json');before=load(folder/'before.json');after=load(folder/'after.json')
   assert rec['status']=='completed' and rec['audio'] and rec['audio_source'] in ('engine_mix','game_process')
   assert rec['frames_written']>30 and rec['frames_captured']>30
   info=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_streams','-of','json',str(video)]))
   assert any(s['codec_type']=='video' and (s['width'],s['height'])==(1280,720) for s in info['streams'])
   assert any(s['codec_type']=='audio' for s in info['streams'])
   sound=subprocess.run(['ffmpeg','-hide_banner','-i',str(video),'-vn','-af','volumedetect','-f','null','-'],capture_output=True).stderr.decode('utf-8',errors='replace');volume=re.search(r'mean_volume: ([\w.+-]+) dB',sound);assert volume and float(volume[1])>-75
   for name,wanted in r['build'].items():assert sha(package/name)==wanted,(folder,name)
   for position in ('before','after'):
    assert (folder/(position+'.png')).is_file();hand=json.loads(load(folder/(position+'-hand.json'))['message'])
    assert hand['modelCount']==hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons']
   expected=base[card]
   if mode=='upgrade':expected={'KAREN_SWORD_UP':7,'KAREN_DEBUT':12,'KAREN_CONFRONT':16}[card]
   if mode=='growth':expected={'KAREN_CONTINUE02':126,'KAREN_SPIN':122}[card]
   scope=next(c for c in before['player']['hand'] if c['id']==card)['target_type']
   if scope=='AllEnemies':expected*=len(before['battle']['enemies'])
   if mode=='lethal':expected=1
   if mode=='replay':expected*=3
   assert r['damage']==expected and r['block']==(expected if card=='KAREN_CONSCIOUSNESS' else 0),(folder,r['damage'],r['block'],expected)
   assert status['hand']==0 and not status['effects'] and not status.get('poseActive',False),folder
   assert status['bodyZ']==load(folder/'before-status.json')['bodyZ'] and status['bodyRelative']==load(folder/'before-status.json')['bodyRelative']
   if card in ('KAREN_COURAGE_STRIKE','KAREN_LANDING'):assert status['promise']==1
   if card=='KAREN_CHARGE_STRIKE':
    power={p['id']:p['amount'] for p in after['battle']['enemies'][0]['status']};amount=9 if mode=='replay' else 3
    assert power.get('STRENGTH_POWER')==-amount and power.get('KAREN_CHARGE_STRIKE_STRENGTH_DOWN_POWER')==amount
   if card=='KAREN_SWORD_UP':
    power={p['id']:p['amount'] for p in after['battle']['enemies'][0]['status']};amount=2 if mode=='upgrade' else 1
    assert power.get('WEAK_POWER')==amount and power.get('VULNERABLE_POWER')==amount
   if phase=='after':
    baseline=indexes['before',env].get((mode,card)) or indexes['before',env].get(('full',card))
    if mode in ('full','low','off','fast','instant','mirror','bounds','regression','cancel'):assert baseline and (r['damage'],r['block'])==(baseline['damage'],baseline['block'])
    enabled=r['quality']!='Off' and r['speed']!='instant'
    assert status['maxAttackOffset']<=12 and 0<=status['maxProfileGrowth']<=1
    if enabled:
     assert status['spawned'].get('NKarenStageAttackVfx')==(3 if mode=='replay' else 1)
     if mode!='cancel':assert status['poseFrames']>0 and status['maxAttackOffset']>1
     hit_count=rounds.get(card,1)+(1 if mode=='upgrade' and card in ('KAREN_DEBUT','KAREN_CONFRONT') else 0)
     targets=len(before['battle']['enemies']) if next(c for c in before['player']['hand'] if c['id']==card)['target_type']=='AllEnemies' else 1
     if mode!='cancel':
      assert status['totalContacts']==hit_count*targets*(3 if mode=='replay' else 1),(folder,status['totalContacts'])
      if a.semantics and card in ids and scope=='AllEnemies':
       assert status['totalGroupWaves']==hit_count*(3 if mode=='replay' else 1),(folder,status['totalGroupWaves'])
       assert status['maxGroupSpan']>status['maxTargetWidth']*1.15,(folder,status['maxGroupSpan'])
       if a.directions:
        assert len(status['groupDirections'])==hit_count
        for path in status['groupDirections']:
         assert path['dx']*path['side']>0,(folder,path)
         if card!='KAREN_LANDING':assert path['dy']*(-1 if path['round']%2==0 else 1)>0,(folder,path)
     if card=='KAREN_CONSCIOUSNESS':assert status['returnCueFrames']>0
     if mode=='lethal':assert status['fatalCueFrames']>0 and any(p['id']=='KAREN_SHINE_CARD_REWARD_POWER' and p['amount']==1 for p in status['powers'])
     if mode=='growth':assert status['maxProfileGrowth']==1
     if card=='KAREN_NONON':assert status['maxManualParticles']==220 and status['spawned'].get('NKarenNononVfx')==1
    else:assert not status['spawned'] and status['maxAttackOffset']==0
    assert all(status['exited'].get(name,0)>=number for name,number in status['spawned'].items())
   all_cases.append(dict(phase=phase,environment=env,mode=mode,card=card,quality=r['quality'],speed=r['speed'],damage=r['damage'],block=r['block'],passed=True,video=video.relative_to(project).as_posix(),sha256=sha(video),build=r['build'],status=status,recording={k:rec[k] for k in ('frames_written','frames_captured','frames_repeated','frames_dropped','capture_fps','encoder','audio_source')},audio_mean_db=float(volume[1])))
def engine_observations(root,project):
 observations=[]
 for phase in ('before','after'):
  for env in ('stable','beta'):
   path=root/phase/env/'godot.log';lines=path.read_text(encoding='utf-8',errors='replace').splitlines()

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
for name,wanted in sources.items():assert hashlib.sha256(((project/name).read_text(encoding='utf-8-sig').rstrip()+'\n').encode()).hexdigest()==wanted,name
if a.report.exists():assert load(a.report)['source_sha256']==sources
result=dict(date='2026-10-30',batch=a.batch_label or ('B03 swing direction correction' if a.directions else 'B03 target-scope redesign' if a.semantics else 'B03 ordinary attacks'),baseline=manifest['baseline'],status='targeted verified; visual review and merge pending',counts=counts,cases=all_cases,source_sha256=sources,engine_observations=engine_observations(root,project),untested=['full singleplayer/multiplayer matrix','full first-use/frame-time suite','Android'])
a.report.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(dict(counts=counts,verified_recordings=len(all_cases),source_files=len(sources))))
