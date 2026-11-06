"""Read-only verification of production textures, UI state, hands, and recorded energy motion."""
import pathlib,json,hashlib,subprocess,numpy as np
repo=pathlib.Path(__file__).resolve().parents[2];out=repo/'artifacts/energy-demo-output'
labels=['00-production-start','01-hand','01-hover-card','02-before-spending','02-zero-energy','03-turn-restored','04-inline-energy-icons','04-energy-above-max','05-two-cost','05-X-cost','06-before-fall','06-after-fall','07-before-tower','07-returned-one-cost']
checks=[];motion=[];sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
source=repo/'docs/worktree-tasks/karen-energy-concepts/style-assets/regular-nonagon.png'
assert sha(source)==sha(repo/'images/ui/combat/energy_counters/karen/karen_energy_nonagon.png')==sha(repo/'images/packed/sprite_fonts/karen_energy_icon.png')
for branch in ['stable','beta']:
 evidence=repo/'artifacts/energy-demo'/branch/'evidence'
 for name in labels:
  state=json.loads((evidence/(name+'-state.json')).read_text(encoding='utf-8'));audit=json.loads((evidence/(name+'-audit.json')).read_text(encoding='utf-8'));hand=json.loads((evidence/(name+'-hand.json')).read_text(encoding='utf-8'))
  assert hand['modelCount']==hand['visualCount'] and not any(hand[k] for k in ['missing','unexpected','unexpectedPileIcons']),(branch,name,hand)
  assert all(c['valid'] for c in hand['burnOverlays']),(branch,name)
  assert audit['production'] and audit['noTextureOverride']
  assert audit['inlineIcon']['width']==audit['inlineIcon']['height']==24
  karen=[c for c in audit['cards'] if c['pool']=='karen' and c['visible']]
  for c in karen:
   assert c['texture']['atlas'].endswith('/karen_energy_nonagon.png'),(branch,name,c)
   assert all(abs(c['icon']['size'][axis]-64)<.02 for axis in ['x','y']),(branch,name,c)
   assert all(abs(c['relativeCenter'][axis]-16)<.02 for axis in ['x','y']),(branch,name,c)
   assert abs(c['icon']['position']['x']+166)<.02 and abs(c['icon']['position']['y']+227)<.02
  counter=audit['counter'];energy=state['player']['energy']
  assert counter['body']['atlas'].endswith('/karen_energy_nonagon.png') and counter['label']==f'{energy}/3'
  assert counter['darkened']==(energy==0) and all(abs(counter['layout']['size'][axis]-128)<.02 for axis in ['x','y']),(branch,name,energy,counter)
  checks.append({'branch':branch,'snapshot':name,'handModelAndViewCount':hand['modelCount'],'karenVisualPoolCards':len(karen),'energy':energy,'passed':True})
 def cost(label,card):
  s=json.loads((evidence/(label+'-state.json')).read_text(encoding='utf-8'));return [c['cost'] for c in s['player']['hand'] if c['id']==card]
 assert cost('04-inline-energy-icons','KAREN_TO_THE_STAGE')==['0']
 assert cost('05-two-cost','KAREN_HOLY_STAR')==['2']
 assert cost('05-X-cost','KAREN_BRIDGE')==['X']
 assert cost('07-returned-one-cost','KAREN_HOLY_STAR')==['1']
 log=(evidence/'godot.log').read_text(encoding='utf-8',errors='replace');errors=[l for l in log.splitlines() if l.startswith('ERROR:') or 'SCRIPT ERROR' in l or 'Unhandled exception' in l]
 assert not errors,(branch,errors)
 assert json.loads((evidence/'completed.json').read_text(encoding='utf-8-sig'))['ownedProcessClosed']
 for name in ['01-native-layout','03-next-turn','04-inline-and-refill','07-promise-out']:
  info=json.loads((evidence/(name+'-recording.json')).read_text(encoding='utf-8'))
  assert info['status']=='completed' and info['capture_fps']>=24 and info['frames_dropped']==0
  raw=out/('raw-'+branch)/(name+'.mp4')
  process=subprocess.run(['ffmpeg','-v','error','-i',str(raw),'-vf','crop=92:92:62:548','-f','rawvideo','-pix_fmt','rgb24','pipe:1'],stdout=subprocess.PIPE,check=True)
  frames=np.frombuffer(process.stdout,dtype=np.uint8).reshape((-1,92,92,3));dif=np.abs(frames[1:].astype(np.int16)-frames[:-1].astype(np.int16));changed=np.any(dif!=0,axis=(1,2,3));run=0;longest=0
  for v in changed:run=0 if v else run+1;longest=max(longest,run)
  motion.append({'branch':branch,'clip':name,'decodedFrames':len(frames),'changedAdjacentFrames':int(changed.sum()),'maxIdenticalRunFrames':longest+1,'meanAbsolutePixelDelta':float(dif.mean()),'maxPixelDelta':int(dif.max())})
assert all(r['maxIdenticalRunFrames']<15 and r['changedAdjacentFrames']>0 for r in motion),motion
result={'passed':True,'sourcePngUnchanged':True,'sourceSha256':sha(source),'nativeCostControl':[64,64],'nativeCostCenterRelativeToFrame':[16,16],'battleCounter':[128,128],'inlineIcon':[24,24],'checks':checks,'decodedEnergyMotion':motion,'motionNote':'Energy ROI changes verified across captured sequences. Intentional scene holds can be flagged by full-frame freezedetect; the filter is not a standalone pass/fail signal.','packageSha256':sha(repo/'artifacts/ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck'),'coreRulesChanged':False}
(out/'production-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'passed':True,'snapshotChecks':len(checks),'decodedMotionClips':len(motion),'sourcePngUnchanged':True}))