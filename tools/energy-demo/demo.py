"""Drive the shipped energy UI through KarenSTS2MCP. No texture override or image edits."""
import argparse,json,pathlib,time,urllib.request,hashlib,sys
sys.stdout.reconfigure(encoding='utf-8')
p=argparse.ArgumentParser();p.add_argument('branch',choices=['stable','beta']);p.add_argument('--phase',choices=['setup','overview','cycle','gain','costs','fall','tower','status'],default='status');p.add_argument('--record',action='store_true');args=p.parse_args()
repo=pathlib.Path(__file__).resolve().parents[2];root=repo/'artifacts/energy-demo'/args.branch;out=root/'evidence';out.mkdir(exist_ok=True)
port=15637 if args.branch=='stable' else 15638;base=f'http://127.0.0.1:{port}/api/v1';recording=False
log=(root/'godot.log').read_text(encoding='utf-8',errors='replace')
assert '[Karen STS2 MCP]' in log and '[KarenEnergyDemoAudit] Loaded.' in log,'MCP and read-only audit must load before API use'
def api(endpoint='/singleplayer?format=json',data=None,binary=False):
 req=urllib.request.Request(base+endpoint,data=None if data is None else json.dumps(data).encode('utf-8'),headers={'Content-Type':'application/json'})
 with urllib.request.urlopen(req,timeout=30) as response:raw=response.read()
 return raw if binary else json.loads(raw)
def state():return api()
def save(name,data):
 (out/(name+'.json')).write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def act(**data):
 value=api('/singleplayer',data)
 with (out/'actions.jsonl').open('a',encoding='utf-8') as f:f.write(json.dumps({'time':time.time(),'action':data,'result':value},ensure_ascii=False)+'\n')
 assert value.get('status')=='ok',(data,value)
 return value
def cmd(command):return act(action='run_command',command=command)
def wait(test,label):
 for _ in range(120):
  current=state()
  if test(current):return current
  time.sleep(.25)
 raise RuntimeError('Timed out: '+label)
def hand_card(card_id,last=False):
 cards=[c for c in state()['player']['hand'] if c['id']==card_id]
 assert cards,(card_id,state())
 return cards[-1 if last else 0]
def play(card_id,target=None,last=False):
 card=hand_card(card_id,last);data={'action':'play_card','card_index':card['index']}
 if target is not None:data['target']=state()['battle']['enemies'][target]['entity_id']
 return act(**data)
def hover(card_id,last=False):return act(action='hover_card',card_index=hand_card(card_id,last)['index'])
def snap(name):
 assert not recording,'Do not stall active capture with PNG readback'
 time.sleep(.65);current=state();save(name+'-state',current)
 probe=json.loads(cmd('karen_energy_audit')['message']);save(name+'-audit',probe)
 hand=json.loads(cmd('karen_check_hand')['message']);save(name+'-hand',hand)
 assert hand['modelCount']==hand['visualCount'] and not any(hand[k] for k in ('missing','unexpected','unexpectedPileIcons')),(name,hand)
 for card in probe['cards']:
  if card['pool']=='karen' and card['visible']:
   assert card['texture']['atlas'].endswith('/karen_energy_nonagon.png'),card
   assert abs(card['icon']['size']['x']-64)<.01 and abs(card['icon']['size']['y']-64)<.01,card
   assert abs(card['relativeCenter']['x']-16)<.01 and abs(card['relativeCenter']['y']-16)<.01,card
 assert probe['inlineIcon']['width']==probe['inlineIcon']['height']==24,probe['inlineIcon']
 assert probe['counter']['body']['atlas'].endswith('/karen_energy_nonagon.png'),probe['counter']
 (out/(name+'.png')).write_bytes(api('/screenshot',binary=True))
 print(json.dumps({'snapshot':name,'branch':args.branch,'hand':len(current['player']['hand']),'energy':current['player']['energy'],'counter':probe['counter']['label'],'inlineSize':[24,24]},ensure_ascii=False),flush=True)
def start(name):
 global recording
 if not args.record:return
 result=api('/recording/start',{'fps':30,'max_duration_seconds':90,'width':1280,'height':720,'audio':False,'encoder':'auto'});save(name+'-record-start',result);recording=True
 time.sleep(.4)
def stop(name):
 global recording
 if not recording:return
 result=api('/recording/stop',{})
 for _ in range(100):
  if result.get('status') in ('completed','error','failed'):break
  time.sleep(.2);result=api('/recording')
 save(name+'-recording',result);recording=False
 assert result.get('status')=='completed',result
 print(json.dumps({'clip':name,'seconds':result['video_duration_seconds'],'captureFps':result.get('capture_fps'),'repeated':result.get('frames_repeated'),'dropped':result.get('frames_dropped')},ensure_ascii=False),flush=True)
def combat():return wait(lambda s:s.get('state_type')=='monster' and s.get('battle',{}).get('is_play_phase'),'combat')
try:
 if args.phase=='setup':
  current=state()
  if current.get('menu_screen')=='main':
   if 'continue' in current['options']:act(action='menu_select',option='continue')
   else:
    act(action='menu_select',option='singleplayer');current=wait(lambda s:s.get('menu_screen') in ('singleplayer','character_select'),'character selection')
    if current['menu_screen']=='singleplayer':act(action='menu_select',option='standard');wait(lambda s:s.get('menu_screen')=='character_select','character selection')
    act(action='menu_select',option='KAREN');act(action='menu_select',option='embark')
    current=wait(lambda s:s.get('menu_screen')=='tutorial_prompt' or s.get('state_type')=='map','map')
    if current.get('menu_screen')=='tutorial_prompt':act(action='menu_select',option='no')
    wait(lambda s:s.get('state_type')=='map','map');act(action='choose_map_node',index=0)
  combat();snap('00-production-start')
 elif args.phase=='overview':
  combat();act(action='unhover_card');snap('01-hand');start('01-native-layout');time.sleep(2)
  target=next((c['id'] for c in state()['player']['hand'] if c['id']=='KAREN_STRIKE'),state()['player']['hand'][0]['id']);hover(target);time.sleep(4);stop('01-native-layout');snap('01-hover-card');act(action='unhover_card')
 elif args.phase=='cycle':
  combat();assert state()['player']['energy']==3
  snap('02-before-spending');start('02-spend-to-zero')
  for energy in (2,1,0):
   cards=state()['player']['hand'];chosen=next((c for c in cards if c['id']=='KAREN_DEFEND'),None) or next(c for c in cards if c['id'] in ('KAREN_STRIKE','KAREN_SHINE_STRIKE'))
   play(chosen['id'],0 if 'STRIKE' in chosen['id'] else None);wait(lambda s:s.get('player',{}).get('energy')==energy,'energy '+str(energy));time.sleep(2)
  time.sleep(2);stop('02-spend-to-zero');snap('02-zero-energy');start('03-next-turn');act(action='end_turn');wait(lambda s:s.get('battle',{}).get('is_play_phase') and s.get('player',{}).get('energy')==3,'next turn refill');time.sleep(3);stop('03-next-turn');snap('03-turn-restored')
 elif args.phase=='gain':
  combat();cmd('card KAREN_TO_THE_STAGE Hand');wait(lambda s:any(c['id']=='KAREN_TO_THE_STAGE' for c in s.get('player',{}).get('hand',[])),'energy card');hover('KAREN_TO_THE_STAGE');snap('04-inline-energy-icons');start('04-inline-and-refill');time.sleep(4);act(action='unhover_card');before=state()['player']['energy'];play('KAREN_TO_THE_STAGE');wait(lambda s:s.get('player',{}).get('energy')==before+3,'gain 3 energy');time.sleep(4);stop('04-inline-and-refill');snap('04-energy-above-max')
 elif args.phase=='costs':
  combat();cmd('card KAREN_HOLY_STAR Hand');cmd('card KAREN_BRIDGE Hand');wait(lambda s:any(c['id']=='KAREN_BRIDGE' for c in s.get('player',{}).get('hand',[])),'X card');hover('KAREN_HOLY_STAR');snap('05-two-cost');start('05-two-and-X-cost');time.sleep(3);hover('KAREN_BRIDGE');time.sleep(3);stop('05-two-and-X-cost');snap('05-X-cost');act(action='unhover_card')
 elif args.phase=='fall':
  combat();cmd('card KAREN_FALL Hand');wait(lambda s:any(c['id']=='KAREN_FALL' for c in s.get('player',{}).get('hand',[])),'fall fixture');snap('06-before-fall');start('06-promise-in');play('KAREN_FALL',last=True);selection=wait(lambda s:s.get('state_type')=='hand_select','fall selection');card=next(c for c in selection['hand_select']['cards'] if c['id']=='KAREN_HOLY_STAR');act(action='combat_select_card',card_index=card['index']);current=state()
  if current.get('state_type')=='hand_select':act(action='combat_confirm_selection')
  combat();time.sleep(3);stop('06-promise-in');snap('06-after-fall')
 elif args.phase=='tower':
  combat();cmd('card KAREN_TOWER_OF_PROMISE Hand');wait(lambda s:any(c['id']=='KAREN_TOWER_OF_PROMISE' for c in s.get('player',{}).get('hand',[])),'tower fixture');snap('07-before-tower');start('07-promise-out');play('KAREN_TOWER_OF_PROMISE');wait(lambda s:any(c['id']=='KAREN_HOLY_STAR' for c in s.get('player',{}).get('hand',[])),'return from promise');combat();time.sleep(3);hover('KAREN_HOLY_STAR');time.sleep(3);stop('07-promise-out');snap('07-returned-one-cost')
 elif args.phase=='status':
  save('current-state',state());save('current-audit',json.loads(cmd('karen_energy_audit')['message']));print(json.dumps(state(),ensure_ascii=False),flush=True)
finally:
 if recording:stop(args.phase+'-interrupted')