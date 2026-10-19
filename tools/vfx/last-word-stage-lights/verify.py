"""Verify accepted MCP evidence, lifetime, visible lighting, capture and unchanged card logic."""
import json, pathlib, re, subprocess, hashlib
from PIL import Image, ImageDraw, ImageFont, ImageStat

main=pathlib.Path(__file__).resolve().parents[3]
root=main/'artifacts/last-word-stage-lights'
out=main/'docs/worktree-tasks/last-word-stage-lights-20261019'
out.mkdir(parents=True,exist_ok=True)
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
expected={
 'blocked-before':0,'ready-after-recording':1,
 'blocked-Draw':0,'restored-Draw':1,'blocked-Discard':0,'restored-Discard':1,
 'blocked-Hand':0,'restored-Hand':1,'duplicate-blocked':0,'duplicate-restored':1,
 'repeated-native-refresh':1,'removed-lastword':0,'recreated-lastword':1,
 'resized-4x3':1,'resized-16x9':1,'rewards-cleanup':0}
void_expected={'void-draw-allowed':1,'void-discard-blocked':0,'void-restored':1,'new-room-cleanup':0}
summary={'branches':{},'baseline':{},'unchanged':{}}
pictures=[]
def red_weight(p,left=True):
 im=Image.open(p).convert('RGB');w,h=im.size
 region=im.crop((0 if left else int(w*.88),int(h*.12),int(w*.12) if left else w,int(h*.56)))
 r,g,b=ImageStat.Stat(region).mean
 return r-(g+b)*.5
for branch in ['stable','beta']:
 folder=root/branch
 result={'cases':{},'handChecks':0,'captures':[],'tCue':'original textures, density, scale, motion, color retained'}
 for case_name,states in [('lastword-stage',expected),('lastword-stage-void',void_expected)]:
  case=folder/case_name
  for name,count in states.items():
   d=read(case/(name+'-probe.json'));nodes=d['current']['nodes']
   assert len(nodes)==count,(branch,name,nodes)
   assert len(d['current']['tNodes'])==count,(branch,name,'T cue',d['current']['tNodes'])
   if count:assert d['current']['tNodes'][0]['particles']>0,(branch,name,'No T particles')
   assert d['peakTNodes']<=1,(branch,name,'Duplicate T cue')
   assert d['peak']==1 or count==0,(branch,name,d['peak'])
   assert d['peakChildren']<=1,(branch,name,d['peakChildren'])
   if nodes:
    node=nodes[0];assert node['mouse']=='Ignore',node
    assert node['children']==1,node
    assert 0<=node['opacity']<=1,node
    hud=[float(v) for v in node['uniforms']['hud_rect'].strip('()').split(',')]
    assert 0<hud[3]<node['height']*.2,(branch,name,hud)
   result['cases'][name]=count
  for p in case.glob('*-hand.json'):
   value=read(p)
   if 'message' not in value:continue  # Windows glob also matches pile name '-Hand.json'.
   assert value.get('status')=='ok',(p,value)
   h=json.loads(value['message'])
   assert h['modelCount']==h['visualCount'] and not h['missing'] and not h['unexpected'] and not h['unexpectedPileIcons'],(p,h)
   result['handChecks']+=1
  pictures += [(branch+'/'+case_name+'/'+p.stem,p) for p in sorted(case.glob('*.png'))]
 case=folder/'lastword-stage'
 first=read(case/'ready-after-recording-probe.json')
 refreshed=read(case/'repeated-native-refresh-probe.json')
 before_refresh=read(case/'duplicate-restored-probe.json')
 assert first['instances']==1,first['instances']
 assert before_refresh['current']['nodes'][0]['instance']==refreshed['current']['nodes'][0]['instance']
 assert read(case/'rewards-cleanup.json')['state_type']=='rewards'
 sizes=[read(case/(name+'-probe.json'))['current']['nodes'][0] for name in ['resized-4x3','resized-16x9']]
 result['resizeRequests']={'observedViewports':[[v['width'],v['height']] for v in sizes],
   'aspectCoverageConfirmed': sizes[0]['width']/sizes[0]['height'] != sizes[1]['width']/sizes[1]['height'],
   'note':'The hidden-window guard can retain the native 16:9 viewport; settings requests alone do not prove aspect-ratio coverage.'}
 result['edgeRedIncrease']={}
 for left in [True,False]:
  change=red_weight(case/'ready-after-recording.png',left)-red_weight(case/'blocked-before.png',left)
  assert change>8,(branch,left,change)
  result['edgeRedIncrease']['left' if left else 'right']=round(change,2)
 for recpath in [case/'ready-recording/recording.json',case/'recording.json']:
  rec=read(recpath)
  assert rec['status']=='completed' and rec['audio'] and rec['audio_source']=='game_process',rec
  assert rec['frames_dropped']==0,(recpath,rec['frames_dropped'])
  result['captures'].append({k:rec.get(k) for k in ['encoder','capture_fps','frames_captured','frames_repeated','frames_dropped','video_duration_seconds','captureBuild']})
 monitor=read(folder/'window-monitor.json')
 assert monitor['foreground_samples']==0 and monitor['restored_samples']==0,monitor
 result['windowMonitor']=monitor
 audits=[]
 for p in (folder/'capture-audit').glob('*.json'):
  value=read(p)
  if 'timelineDriftSeconds' in value:
   assert abs(value['timelineDriftSeconds'])<.05 and value['longestUnchangedClockSeconds']<.2,value
   audits.append(value)
 assert audits,(folder,'No clock audit')
 result['captureAudit']=audits
 log=(folder/'godot.log').read_text(encoding='utf-8',errors='replace')
 diagnostics=[{'line':i+1,'text':line} for i,line in enumerate(log.splitlines()) if re.search(r'\bERROR\b|Exception|Invalid Task ID',line)]
 result['logDiagnostics']=diagnostics
 (out/('log-diagnostics-'+branch+'.json')).write_text(json.dumps(diagnostics,ensure_ascii=False,indent=2),encoding='utf-8')
 summary['branches'][branch]=result
baseline=root/'before/stable/lastword-stage-before'
summary['baseline']['recording']={k:read(baseline/'ready-recording/recording.json').get(k) for k in ['capture_fps','frames_captured','frames_dropped','captureBuild']}
assert summary['baseline']['recording']['frames_dropped']==0
pictures += [('before/'+p.stem,p) for p in sorted(baseline.glob('*.png'))]
for path in [
 'src/Core/Models/Cards/neutral/KarenLastWord.cs',
 'src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenLastWordVideoVfx.cs',
 'src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenLastWordVfx.cs',
 'ShoujoKagekiAijoKaren/video/last_word.ogv']:
 before=subprocess.check_output(['git','show','84ab49fca7128e9b0338669eea0f66163a7f6cb6:'+path],cwd=main)
 after=(main/path).read_bytes()
 if path.endswith('.cs'):before=before.replace(b'\r\n',b'\n');after=after.replace(b'\r\n',b'\n')
 assert before==after,path
 summary['unchanged'][path]=hashlib.sha256(after).hexdigest()
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
for page in range((len(pictures)+15)//16):
 group=pictures[page*16:(page+1)*16]
 contact=Image.new('RGB',(1600,4*251),(20,22,30));draw=ImageDraw.Draw(contact)
 for j,(label,path) in enumerate(group):
  image=Image.open(path).convert('RGB');image.thumbnail((392,220))
  x=(j%4)*400+4;y=(j//4)*251+25
  contact.paste(image,(x,y));draw.text((x,y-21),label[-50:],font=font,fill=(245,245,248))
 contact.save(out/('mcp-contact-%02d.jpg'%(page+1)),quality=86)
summary['screenshots']=len(pictures)
(out/'verification.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({b:{'states':len(v['cases']),'hands':v['handChecks'],'edgeRedIncrease':v['edgeRedIncrease'],'diagnostics':v['logDiagnostics']} for b,v in summary['branches'].items()},ensure_ascii=False,indent=2))
print('MCP screenshots:',len(pictures))
