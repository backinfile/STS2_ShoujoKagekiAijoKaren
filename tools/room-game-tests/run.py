"""Targeted native lobby and battle test with the real room relay, isolated profiles and MCP only."""
from pathlib import Path
import argparse,importlib.util,json,os,shutil,socket,subprocess,sys,time,urllib.request
parser=argparse.ArgumentParser();parser.add_argument('branch',choices=['stable','beta']);parser.add_argument('--source-repo',type=Path,required=True);parser.add_argument('--output',type=Path,required=True);parser.add_argument('--mcp-source',type=Path,required=True);parser.add_argument('--endpoint');parser.add_argument('--desktop-host',action='store_true');parser.add_argument('--record',action='store_true');parser.add_argument('--complete-battle',action='store_true');parser.add_argument('--save-check',action='store_true');args=parser.parse_args()
repo=Path(__file__).resolve().parents[2];source=args.source_repo/'artifacts/dev-game'/args.branch
root=args.output.resolve();root.mkdir(parents=True,exist_ok=False)
spec=importlib.util.spec_from_file_location('room_desktop',repo/'tools/vfx/six-card/isolated_desktop.py');desktop_module=importlib.util.module_from_spec(spec);spec.loader.exec_module(desktop_module)
desktop=desktop_module.TestDesktop();games=[];result={'branch':args.branch,'passed':False};relay=None
ports=[19367,19368] if args.branch=='stable' else [19377,19378]
with socket.socket() as probe:probe.bind(('127.0.0.1',0));relay_port=probe.getsockname()[1]
for port in ports:
 with socket.socket() as probe:assert probe.connect_ex(('127.0.0.1',port))!=0,'MCP port occupied'
relay_log=None
if not args.endpoint:
 relay_log=(root/'relay.log').open('w',encoding='utf-8')
 relay=subprocess.Popen([sys.executable,str(repo/'server/karen-rooms/server.py'),'--port',str(relay_port)],stdout=relay_log,stderr=relay_log)
endpoint=args.endpoint or f'ws://127.0.0.1:{relay_port}/rooms'
recording=set()
mcp=root/'mcp'
subprocess.run(['dotnet','build',str(args.mcp_source/'KarenSTS2MCP.csproj'),'-c','Release','-p:STS2GameDir='+str(source/'game'),'-o',str(mcp),'--nologo','-v:q','-clp:ErrorsOnly'],check=True)
def save(name,data):(root/(name+'.json')).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
def link(src,dst):
 try:os.link(src,dst)
 except OSError:shutil.copy2(src,dst)
 return dst
def api(side,path,data=None):
 req=urllib.request.Request(f'http://127.0.0.1:{ports[side]}/api/v1'+path,data=None if data is None else json.dumps(data).encode(),headers={'Content-Type':'application/json'})
 with urllib.request.urlopen(req,timeout=15) as response:return json.load(response)
def state(side,endpoint='singleplayer'):return api(side,'/'+endpoint+'?format=json')
def post(side,endpoint='singleplayer',**data):
 value=api(side,'/'+endpoint,data)
 with (root/'actions.jsonl').open('a',encoding='utf-8') as f:f.write(json.dumps([side,data,value],ensure_ascii=False)+'\n')
 assert value.get('status')=='ok',(side,data,value)
 return value
def wait(side,test,label,endpoint='singleplayer',seconds=65):
 until=time.monotonic()+seconds;last=None
 while time.monotonic()<until:
  if games[side]['process'].poll() is not None:raise RuntimeError('Game exited: '+str(side))
  try:
   last=state(side,endpoint)
   if test(last):return last
   if last.get('menu_screen')=='tutorial_prompt':post(side,action='menu_select',option='no')
  except (OSError,ValueError):pass
  report=games[side]['report']
  if report.exists():
   try:
    check=json.loads(report.read_text(encoding='utf-8'))
    if check.get('error'):raise RuntimeError(check['error'])
   except ValueError:pass
  time.sleep(.25)
 raise RuntimeError(str((side,label,last)))
def screenshot(side,name):
 with urllib.request.urlopen(f'http://127.0.0.1:{ports[side]}/api/v1/screenshot',timeout=20) as response:(root/(name+'.png')).write_bytes(response.read())
def launch(side,code=None):
 dev=root/(('pc-host' if args.desktop_host else 'android-host') if side==0 else 'desktop-client');game=dev/'game'
 shutil.copytree(source/'game',game,copy_function=link,ignore=lambda folder,names:['mods','override.cfg'] if Path(folder)==source/'game' else [])
 mods=game/'mods';mods.mkdir();shutil.copytree(source/'game/mods/BaseLib',mods/'BaseLib')
 package=repo/('artifacts/android' if side==0 and not args.desktop_host else 'artifacts')/'ShoujoKagekiAijoKaren'
 shutil.copytree(package,mods/'ShoujoKagekiAijoKaren')
 shutil.copy2(mcp/'KarenSTS2MCP.dll',mods)
 shutil.copy2(args.mcp_source/'mod_manifest.json',mods/'KarenSTS2MCP.json')
 (mods/'KarenSTS2MCP.conf').write_text(json.dumps(dict(port=ports[side],muted=True,silent_test=True,window_width=1280,window_height=720,skip_tutorial=True,skip_first_prompt=True)),encoding='utf-8')
 fixture=dev/'fixture';subprocess.run(['dotnet','build',str(repo/'tools/room-game-tests/RoomGameProbe.csproj'),'-p:Game='+str(source/'game'),'-p:KarenAssembly='+str(package/'lib'/f'ShoujoKagekiAijoKaren.{args.branch.title()}.dll'),'-o',str(fixture),'--nologo','-v:q','-clp:ErrorsOnly'],check=True)
 shutil.copy2(fixture/'KarenRoomGameProbe.dll',mods)
 (mods/'KarenRoomGameProbe.json').write_text(json.dumps(dict(id='KarenRoomGameProbe',name='KarenRoomGameProbe',version='1.0.0',has_dll=True,has_pck=False,affects_gameplay=False,dependencies=[dict(id='ShoujoKagekiAijoKaren')])),encoding='utf-8')
 profile=70210+side;settings=dev/f'user/AppData/Roaming/SlayTheSpire2/default/{profile}/settings.save';settings.parent.mkdir(parents=True)
 data=json.loads((source/'user/AppData/Roaming/SlayTheSpire2/default/1/settings.save').read_text(encoding='utf-8-sig'));data.update(mute_in_background=False,limit_fps_in_background=False);settings.write_text(json.dumps(data),encoding='utf-8')
 (game/'override.cfg').write_text('[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n',encoding='utf-8')
 env=os.environ.copy();env['APPDATA']=str(dev/'user/AppData/Roaming');env['LOCALAPPDATA']=str(dev/'user/AppData/Local');Path(env['LOCALAPPDATA']).mkdir(parents=True)
 env['KAREN_ROOM_SAVE_CHECK']='1' if args.save_check and side==0 else '';env['KAREN_ROOM_ENDPOINT']=endpoint;env['STS2_MCP_FFMPEG']=shutil.which('ffmpeg') or 'ffmpeg';env['KAREN_ROOM_TEST_CODE_FILE']=str(root/'join-code.txt') if side==1 else ''; env['KAREN_ROOM_TEST_REPORT']=str(dev/'probe.json')
 if code:env['KAREN_ROOM_TEST_CODE']=code
 process=desktop.launch([str(game/'SlayTheSpire2.exe'),'--force-steam=off','--rendering-driver','vulkan',f'--clientId={profile}','--log-file',str(dev/'godot.log')],cwd=game,env=env)
 games.append(dict(process=process,report=dev/'probe.json',dev=dev));print('START',side,process.pid,flush=True)
 until=time.monotonic()+65
 while time.monotonic()<until:
  log=dev/'godot.log'
  if log.exists() and '[Karen STS2 MCP]' in log.read_text(encoding='utf-8',errors='replace'):break
  if process.poll() is not None:raise RuntimeError('Game exited before MCP')
  time.sleep(.25)
 else:raise RuntimeError('MCP did not load')
 wait(side,lambda s:s.get('menu_screen')=='main','main menu')
 api(side,'/settings',dict(muted=True,window_width=1280,window_height=720))
 wait(side,lambda s:games[side]['report'].exists() and json.loads(games[side]['report'].read_text(encoding='utf-8')).get('configDone'),'configuration success')

def begin_record(side,name,seconds=90):
 if not args.record:return
 save(name+'-start',api(side,'/recording/start',dict(fps=30,max_duration_seconds=seconds,width=1280,height=720,audio=False,encoder='auto')))
 recording.add(side);time.sleep(.8)

def finish_record(side,name):
 if side not in recording:return
 api(side,'/recording/stop',{})
 until=time.monotonic()+35
 while time.monotonic()<until:
  rec=api(side,'/recording')
  if rec.get('status') not in ('recording','stopping'):break
  time.sleep(.2)
 save(name+'-recording',rec)
 assert rec['status']=='completed',rec
 shutil.copy2(rec['output_path'],root/(name+'.mp4'))
 recording.remove(side)

def enter(side):
 post(side,action='menu_select',option='multiplayer');wait(side,lambda s:s.get('menu_screen')=='multiplayer','multiplayer menu')
 time.sleep(.8)
 post(side,action='menu_select',option='host' if side==0 else 'join')
 if side==0:
  wait(side,lambda s:s.get('menu_screen')=='character_select','host room lobby')
  wait(side,lambda s:json.loads(games[side]['report'].read_text(encoding='utf-8')).get('code') is not None,'room code')

try:
 launch(0)
 launch(1)
 begin_record(0,'01-create-room')
 enter(0)
 host=json.loads(games[0]['report'].read_text(encoding='utf-8'));assert host['policyDone'] and host['android']==(not args.desktop_host),host
 code=host['code'];assert len(code)==4
 time.sleep(2)
 begin_record(1,'02-join-room')
 enter(1)
 (root/'join-code.txt').write_text(code,encoding='ascii')
 wait(1,lambda s:s.get('lobby',{}).get('player_count')==2,'joined native lobby')
 for side,character in [(0,'KAREN'),(1,'IRONCLAD')]:
  post(side,action='menu_select',option=character)
  wait(side,lambda s:any(p.get('is_local') and p.get('character_id')==character for p in s.get('lobby',{}).get('players',[])),'character choice')
 time.sleep(2)
 finish_record(0,'01-create-room');finish_record(1,'02-join-room')
 for side in range(2):screenshot(side,f'{side}-lobby')
 for side in range(2):post(side,action='menu_select',option='embark')
 for side in range(2):wait(side,lambda s:s.get('state_type')=='map','map','multiplayer')
 for side in range(2):post(side,'multiplayer',action='choose_map_node',index=0)
 for side in range(2):wait(side,lambda s:s.get('battle',{}).get('is_play_phase'),'battle','multiplayer');screenshot(side,f'{side}-combat')
 for side in range(2):
  current=state(side,'multiplayer')
  cards=[c for c in current['player']['hand'] if c.get('can_play',False) and c.get('target_type') in ('Self','AnyEnemy','Enemy') and 'FALL' not in c.get('id','')]
  assert cards,'No legal immediate card in the opening hand'
  cards.sort(key=lambda c:0 if c.get('target_type')=='Self' and 'DEFEND' in c.get('id','') else 1)
  card=cards[0];body=dict(action='play_card',card_index=card['index'])
  if card.get('target_type') in ('AnyEnemy','Enemy'):
   enemies=current.get('enemies',current.get('battle',{}).get('enemies',[]))
   body['target']=next(e['entity_id'] for e in enemies if e.get('hp',e.get('current_hp',1))>0)
  post(side,'multiplayer',**body)
  time.sleep(2)
 for side in range(2):
  until=time.monotonic()+15
  while True:
   response=api(side,'/multiplayer',dict(action='end_turn'))
   if response.get('status')=='ok':break
   if 'while a card is being played' not in response.get('error','') or time.monotonic()>until:raise RuntimeError(response)
   time.sleep(.3)
 for side in range(2):wait(side,lambda s:s.get('battle',{}).get('is_play_phase') and s.get('battle',{}).get('round',0)>=2,'second turn','multiplayer')
 for side in range(2):
  save(f'{side}-final-state',state(side,'multiplayer'));save(f'{side}-probe',json.loads(games[side]['report'].read_text(encoding='utf-8')))
  screenshot(side,f'{side}-after-turn')
  hand=json.loads(post(side,'multiplayer',action='run_command',command='karen_check_hand')['message']);save(f'{side}-hand',hand)
  assert hand['modelCount']==hand['visualCount'] and not hand['missing'] and not hand['unexpected'],hand
 if args.complete_battle:
  begin_record(0,'03-online-battle',seconds=240)
  until=time.monotonic()+210
  while time.monotonic()<until:
   states=[state(side,'multiplayer') for side in range(2)]
   if all(s.get('state_type')=='rewards' for s in states):break
   for side in range(2):
    for _ in range(12):
     current=state(side,'multiplayer')
     if current.get('state_type')=='rewards':break
     if not current.get('battle',{}).get('is_play_phase'):break
     cards=[c for c in current.get('player',{}).get('hand',[]) if c.get('can_play') and c.get('target_type') in ('AnyEnemy','Enemy','Self','None','AllEnemies') and 'FALL' not in c.get('id','')]
     # Attack before block, respecting actual energy and legal targets each time.
     cards.sort(key=lambda c:0 if c.get('type')=='Attack' or 'STRIKE' in c.get('id','') or 'BASH' in c.get('id','') else 1)
     if not cards:break
     card=cards[0];body=dict(action='play_card',card_index=card['index'])
     if card.get('target_type') in ('AnyEnemy','Enemy'):
      enemies=[e for e in current.get('enemies',current.get('battle',{}).get('enemies',[])) if e.get('hp',e.get('current_hp',1))>0]
      if not enemies:break
      body['target']=enemies[0]['entity_id']
     response=api(side,'/multiplayer',body)
     with (root/'actions.jsonl').open('a',encoding='utf-8') as f:f.write(json.dumps([side,body,response],ensure_ascii=False)+'\n')
     if response.get('status')!='ok':time.sleep(.4);continue
     time.sleep(1.6)
    current=state(side,'multiplayer')
    if current.get('battle',{}).get('is_play_phase'):
     for _ in range(30):
      response=api(side,'/multiplayer',dict(action='end_turn'))
      if response.get('status')=='ok' or 'Already submitted' in response.get('error','') or 'Not in combat' in response.get('error',''):break
      time.sleep(.3)
   time.sleep(1)
  else:raise RuntimeError('Complete battle timed out')
  time.sleep(2);finish_record(0,'03-online-battle')
  for side in range(2):save(f'{side}-victory-state',state(side,'multiplayer'));screenshot(side,f'{side}-victory')
  result['battle_completed']=True
 if args.save_check:
  probe=json.loads(games[0]['report'].read_text(encoding='utf-8'));assert probe.get('saveProtectionChecked'),probe
  result['save_protection_checked']=True
 result.update(passed=True,room_code=code,native_battle=True,endpoint=endpoint,desktop_host=args.desktop_host,android_arm64_device_tested=False)
 print('PASS native room lobby and battle',flush=True)
except Exception as error:
 import traceback
 result['error']=str(error);result['traceback']=traceback.format_exc();print(result['traceback'],flush=True)
finally:
 for side in list(recording):
  try:finish_record(side,f'{side}-interrupted')
  except Exception:pass
 save('result',result);desktop.close()
 if relay:relay.terminate();relay.wait(timeout=15)
 if relay_log:relay_log.close()
raise SystemExit(0 if result['passed'] else 1)
