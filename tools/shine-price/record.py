"""Record real native plays through KarenSTS2MCP; no desktop interaction."""
import argparse,hashlib,json,os,shutil,socket,subprocess,time,urllib.request
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--output',default='artifacts/shine-price-recording/capture');parser.add_argument('--cases',default='normal,upgrade');args=parser.parse_args()
scenarios={
 'normal':dict(energy=3,hits=6,cost=3,per_hit=5,plays=1),
 'upgrade':dict(energy=3,hits=6,cost=3,per_hit=6,plays=1),
 'blocked':dict(energy=3,hits=0,cost=0,per_hit=5,plays=0,caption='双星 · 遗物不足无法打出',detail='X = 3 · 可禁用 2 个遗物 · 尝试两次均拒绝，不耗能、不禁用、不攻击'),
 'replay':dict(energy=2,hits=8,cost=4,per_hit=5,plays=2,caption='双星 · 额外重播一次（测试插件）',detail='X = 2 · 4 个遗物 → 两次各禁用 2 个 · 共 8 次命中，40 点伤害'),
 'native-blocked':dict(energy=3,hits=0,cost=0,per_hit=5,plays=0,caption='双星 · 原生重放 1 · 遗物不足无法打出',detail='X = 3 · 仅 2 个遗物 · 原生拒绝两次，不耗能、不禁用、不攻击'),
 'native-replay':dict(energy=2,hits=8,cost=4,per_hit=5,plays=2,caption='双星 · 原生重放 1 · 完整执行两次',detail='卡面原生重放关键字 · X = 2 · 4 个遗物 · 8 次命中，40 点伤害'),
 'native-replay-short':dict(energy=2,hits=4,cost=2,per_hit=5,plays=2,caption='双星 · 原生重放 1 · 第二次遗物不足',detail='卡面原生重放关键字 · X = 2 · 3 个遗物 · 第二次跳过，共 20 点伤害'),
 'replay-short':dict(energy=2,hits=4,cost=2,per_hit=5,plays=2,caption='双星 · 额外重播时遗物不足（测试插件）',detail='X = 2 · 3 个遗物 → 第一次禁用 2 个，剩 1 个 · 第二次跳过，共 20 点伤害')
}
selected=args.cases.split(',');assert selected and all(mode in scenarios for mode in selected),selected
root=Path(__file__).resolve().parents[2];dev=root/'artifacts/dev-game/stable';game=dev/'game'
out=root/args.output;out.mkdir(parents=True,exist_ok=True)
port=15657;base=f'http://127.0.0.1:{port}/api/v1';opener=urllib.request.build_opener(urllib.request.ProxyHandler({}))
process=None;recording=False;backups={};clips=[]

def save(path,data):path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def api(path,data=None,binary=False):
 req=urllib.request.Request(base+path,data=None if data is None else json.dumps(data).encode(),headers={'Content-Type':'application/json'})
 with opener.open(req,timeout=30) as response:content=response.read()
 return content if binary else json.loads(content)
def state():return api('/singleplayer?format=json')
def action(action,**args):
 result=api('/singleplayer',dict(action=action,**args));assert result.get('status')=='ok',result
 with (out/'actions.jsonl').open('a',encoding='utf-8') as f:f.write(json.dumps([action,args,result],ensure_ascii=False)+'\n')
 return result
def cmd(command):return action('run_command',command=command)
def probe():return json.loads(cmd('shine_price_probe status')['message'])
def wait(test,timeout=40):
 deadline=time.monotonic()+timeout
 while time.monotonic()<deadline:
  assert process.poll() is None,'Owned recorder game exited'
  try:
   s=state()
   if s.get('prompt_id'):
    options=[v['name'] if isinstance(v,dict) else v for v in s.get('options',[])];api('/ui/prompt',dict(option=next((v for v in ['no','advance','proceed'] if v in options),options[0]),prompt_id=s['prompt_id']))
   elif test(s):return s
  except (OSError,ValueError):pass
  time.sleep(.15)
 raise TimeoutError('MCP state timeout')
def resolved():
 deadline=time.monotonic()+35
 while time.monotonic()<deadline:
  p=probe()
  if p['pile']=='Discard' and p['attacks']:return p
  time.sleep(.12)
 raise TimeoutError('Card resolution')
def snap(case,label):
 save(case/(label+'.json'),state());(case/(label+'.png')).write_bytes(api('/screenshot',binary=True))
 hand=json.loads(cmd('karen_check_hand')['message']);save(case/(label+'-hand.json'),hand)
 assert hand['modelCount']==hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons'],hand
def seed(mode,energy):
 cmd('shine_price_probe seed '+mode)
 deadline=time.monotonic()+20
 while time.monotonic()<deadline:
  p=probe()
  assert not p['state'].startswith('failed'),p
  if p['state']=='ready':break
  time.sleep(.1)
 else:raise TimeoutError('Seed')
 cmd('shine_price_probe present');cmd('energy '+str(energy));wait(lambda s:s.get('player',{}).get('energy')==energy);time.sleep(1)
def play():
 s=state();card=next(c for c in s['player']['hand'] if c['id']=='KAREN_SHINE_PRICE')
 action('play_card',card_index=card['index'],target=s['battle']['enemies'][0]['entity_id'])
def back(path):backups[path]=path.read_bytes() if path.exists() else None
def main():
 global process,recording
 with socket.socket() as s:assert s.connect_ex(('127.0.0.1',port))!=0,'Owned test port occupied'
 mods=game/'mods';shutil.copytree(root/'artifacts/ShoujoKagekiAijoKaren',mods/'ShoujoKagekiAijoKaren',dirs_exist_ok=True);mcp=mods/'KarenSTS2MCP.dll';config=mods/'KarenSTS2MCP.conf';override=game/'override.cfg'
 for p in [mcp,config,override]:back(p)
 shutil.copy2(root/'artifacts/shine-price-recording/mcp-build/KarenSTS2MCP.dll',mcp)
 shutil.copy2(root/'artifacts/shine-price/probe/stable/KarenShinePriceProbe.dll',mods)
 data=json.loads(config.read_text(encoding='utf-8-sig'));data.update(muted=False,silent_test=True,fullscreen=False,window_width=1280,window_height=720);save(config,data)
 override.write_text('[display]\nwindow/size/mode=1\nwindow/size/no_focus=true\n',encoding='utf-8')
 settings=dev/'user/AppData/Roaming/SlayTheSpire2/default/1/settings.save';back(settings)
 data=json.loads(settings.read_text(encoding='utf-8-sig'));data.update(volume_master=.5,volume_sfx=.85,volume_bgm=.15);save(settings,data)
 # Background mute is stored in profile prefs, not in settings.save.
 for prefs in (dev/'user/AppData/Roaming/SlayTheSpire2/default/1').rglob('prefs.save'):
  back(prefs);data=json.loads(prefs.read_text(encoding='utf-8-sig'));data['mute_in_background']=False;save(prefs,data)
 env=os.environ.copy();env.update(APPDATA=str(dev/'user/AppData/Roaming'),LOCALAPPDATA=str(dev/'user/AppData/Local'),STS2_MCP_FFMPEG=shutil.which('ffmpeg') or '')
 startup=subprocess.STARTUPINFO();startup.dwFlags=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
 log=dev/'godot.log';launch=(out/'launch.log').open('w',encoding='utf-8')
 # Clear this owned log before launch so an earlier readiness marker cannot win the startup race.
 log.write_text('',encoding='utf-8')
 process=subprocess.Popen([str(game/'SlayTheSpire2.exe'),'--rendering-driver','vulkan','--force-steam=off','--log-file',str(log)],cwd=game,env=env,startupinfo=startup,stdout=launch,stderr=subprocess.STDOUT)
 deadline=time.monotonic()+120
 while time.monotonic()<deadline:
  assert process.poll() is None,'Owned game failed to launch'
  text=log.read_text(encoding='utf-8',errors='replace') if log.exists() else ''
  if '[Karen STS2 MCP]' in text and "Preloading 'Common' Complete:" in text:break
  time.sleep(.25)
 else:raise TimeoutError('MCP log readiness')
 settings_state=api('/settings');save(out/'settings.json',settings_state)
 assert settings_state.get('silent_test') and not settings_state.get('silent_test_error') and settings_state.get('minimized_startup_guard'),settings_state
 time.sleep(1.5);s=state()
 if s.get('menu_screen')=='main' and 'continue' in s.get('options',[]):action('menu_select',option='continue');s=wait(lambda s:s.get('player'))
 if not s.get('player'):
  if s.get('menu_screen')=='main':action('menu_select',option='singleplayer')
  s=wait(lambda s:s.get('menu_screen') in ['singleplayer','character_select'])
  if s.get('menu_screen')=='singleplayer':action('menu_select',option='standard')
  action('menu_select',option='KAREN');action('menu_select',option='embark')
  s=wait(lambda s:s.get('state_type')=='map' or s.get('menu_screen')=='tutorial_prompt')
  if s.get('menu_screen')=='tutorial_prompt':action('menu_select',option='no')
  wait(lambda s:s.get('state_type')=='map')
 api('/settings',dict(muted=False))
 cmd('fight WATERFALL_GIANT_BOSS');wait(lambda s:s.get('battle',{}).get('is_play_phase'));time.sleep(1)
 seed('normal',1);play();resolved();time.sleep(1.2)
 for mode in selected:
  spec=scenarios[mode];per_hit=spec['per_hit'];cost=spec['cost'];hits=spec['hits'];damage=per_hit*hits
  case=out/mode;case.mkdir(exist_ok=True);seed(mode,spec['energy'])
  if mode.startswith('native-'):
   before=probe();assert before['nativeBaseReplayCount']==1 and before['nativeReplayCount']==1 and before['modExtraReplayCount']==0,before
   assert before['nativeReplayKeyword'] in before['description'],before
  action('hover_card',card_index=0);time.sleep(.7);snap(case,'before');save(case/'probe-before.json',probe())
  action('unhover_card');cmd('shine_price_probe inspect');time.sleep(.8);snap(case,'inspect');cmd('shine_price_probe close-inspect');time.sleep(.8);action('hover_card',card_index=0);time.sleep(.6)
  start=api('/recording/start',dict(fps=30,max_duration_seconds=20,width=1280,height=720,audio=True,encoder='auto'));recording=True
  assert start.get('audio_source')=='engine_mix',start
  began=time.monotonic();time.sleep(2.6);action('unhover_card');time.sleep(.5);play_at=time.monotonic()-began
  if mode in ['blocked','native-blocked']:
   cmd('shine_price_probe try-blocked');time.sleep(1.3);cmd('shine_price_probe try-blocked');time.sleep(1.3)
  else:play();resolved()
  resolved_at=time.monotonic()-began;time.sleep(2.2)
  api('/recording/stop',{});deadline=time.monotonic()+50
  while time.monotonic()<deadline:
   meta=api('/recording')
   if meta['status'] in ['completed','failed']:break
   time.sleep(.2)
  assert meta['status']=='completed' and meta['audio'] and meta['audio_source']=='engine_mix',meta
  recording=False;save(case/'recording.json',meta)
  source=Path(meta['output_path']);assert source.suffix=='.mp4' and source.name.startswith('sts2-');shutil.copy2(source,case/'game.mp4')
  after=probe();save(case/'probe-after.json',after);snap(case,'after')
  assert 'gather' not in after['visualEvents'] and after['activeVfx']==0 and after['visualEvents'].count('gold')==hits//2 and after['visualEvents'].count('pink')==hits//2 and after['visualEvents'].count('contact')==hits,after
  assert len(after['pairs'])==cost and all(p['fromRelic'] and 0 < p['distance'] <= 220.1 for p in after['pairs']),after
  assert after['locks']==cost and after['targetHp']==100-damage and sum(x['hits'] for x in after['attacks'])==hits and after['energy']==(spec['energy'] if mode in ['blocked','native-blocked'] else 0),after
  assert after['plays']==spec['plays'],after
  if mode.startswith('native-'):assert after['nativeBaseReplayCount']==1 and after['modExtraReplayCount']==0,after
  if mode in ['blocked','native-blocked']:assert after['nativeBlockedAttempts']==2 and after['pile']=='Hand' and not after['visualEvents'] and not after['playable'],after
  else:assert after['pile']=='Discard',after
  if mode in ['replay-short','native-replay-short']:assert after['eligible']==1 and len(after['attacks'])==1,after
  if mode in ['replay','native-replay']:assert after['eligible']==0 and len(after['attacks'])==2,after
  summary=dict(mode=mode,native_replay_count=after['nativeReplayCount'],mod_extra_replay_count=after['modExtraReplayCount'],native_keyword=after['nativeReplayKeyword'],caption=spec.get('caption'),detail=spec.get('detail'),plays=after['plays'],native_blocked_attempts=after['nativeBlockedAttempts'],pairs=after['pairs'],relics_disabled=cost,hits=hits,damage=damage,per_hit=per_hit,visual_events=after['visualEvents'],play_at=play_at,resolved_at=resolved_at,recording={k:meta.get(k) for k in ['video_duration_seconds','capture_fps','frames_captured','frames_repeated','frames_dropped','frames_written','encoder','audio_source','audio']})
  clips.append(summary);save(out/'clips.json',clips);print(mode,'RECORDED',summary,flush=True)
 save(out/'source-hashes.json',{'mod_dll':hashlib.sha256((mods/'ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.Stable.dll').read_bytes()).hexdigest(),'pck':hashlib.sha256((mods/'ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck').read_bytes()).hexdigest(),'mcp_dll':hashlib.sha256(mcp.read_bytes()).hexdigest(),'mcp_source_commit':'b8535120fbcdfb4fe2bf1ae915a410d611ee4067'})
try:main()
finally:
 if recording:
  try:
   api('/recording/stop',{})
   deadline=time.monotonic()+50
   while time.monotonic()<deadline:
    if api('/recording')['status'] in ['completed','failed']:break
    time.sleep(.2)
  except Exception:pass
 if process and process.poll() is None:process.terminate();process.wait(timeout=15)
 for path,content in backups.items():
  if content is None:path.unlink(missing_ok=True)
  else:path.write_bytes(content)
 log=dev/'godot.log'
 if log.exists():shutil.copy2(log,out/'godot.log')
