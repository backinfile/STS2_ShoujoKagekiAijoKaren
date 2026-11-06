"""Native enemy layout, pincer facing and live summon adaptation through MCP."""
import traceback
exec(compile((main/'tools/vfx/catalog/attack_observer.py').read_text(encoding='utf-8-sig'), 'attack_observer.py', 'exec'))

enemy_specs=[
    dict(name='enemy-pincer',encounter='KAISER_CRAB_BOSS',pincer=True,kill_side='left',title='帝王蟹：左右交替、多段攻击、左侧死亡后自动朝右'),
    dict(name='enemy-pincer-void',encounter='KAISER_CRAB_BOSS',pincer=True,void=True,kill_side='right',title='帝王蟹：空虚/燃烧/觉醒、嵌套塔桥、右侧死亡后自动朝左'),
    dict(name='enemy-slimes',encounter='SLIMES_WEAK',title='小体型群怪的目标、命中与移牌表现'),
    dict(name='enemy-ovicopter',encounter='OVICOPTER_NORMAL',summons=True,title='飞行怪、偏移镜头及原生新增召唤目标'),
    dict(name='enemy-knights',encounter='KNIGHTS_ELITE',title='三骑士多目标与护盾，非默认镜头比例'),
]
enemy_results=[]

def geometry():return actor_status()['geometry']

def living_enemies():return [e for e in geometry() if e['id']!='PLAYER' and e['hp']>0 and not e['dead']]

def targeted_cast(card,combat_id,label):
    cmd('boundary energy 20')
    if card not in [c['id'] for c in state()['player']['hand']]:add(card)
    current=state();target=next(e for e in current['battle']['enemies'] if e['combat_id']==combat_id)
    hand=next(c for c in current['player']['hand'] if c['id']==card)
    snap(case/(label+'-before'));check_hand(label+'-before-hand');before=len(probe()['finished'])
    watch_attack(card,label);post(action='play_card',card_index=hand['index'],target=target['entity_id'])
    time.sleep(.15);snap(case/(label+'-during'))
    time.sleep(.3);snap(case/(label+'-active'))
    report=wait_done(card,before);time.sleep(1.2);snap(case/(label+'-after'));check_hand(label+'-after-hand')
    own=[c for c in report['finished'][before:] if c['id']==card]
    assert own and all(c['target']==combat_id for c in own),(combat_id,own)
    save(label+'-native',report);verify_attack(card,label);save(label+'-actor',actor_status())
    return report

def enemy_fresh(item):
    global case
    case=out/item['name'];case.mkdir(exist_ok=True)
    if not state().get('battle') or actor_status()['character']!='KAREN':start_character('KAREN')
    cmd('promise_integration off');cmd('showcase reset-demo-relics');finish_combat()
    cmd('fight '+item['encounter'])
    wait_state(lambda s:s.get('battle',{}).get('is_play_phase'),35);time.sleep(args.setup_wait)
    trim();cmd('showcase clear');cmd('stage_probe promise 0');cmd('showcase rebuild-tower');time.sleep(.5)
    cmd('promise_integration watch');cmd('actor_state watch');cmd('boundary watch');cmd('bridge_vfx watch');cmd('stage_probe watch');cmd('pzero watch')
    actual_cast('KAREN_POSITION0','position0')
    if item.get('void'):
        for card in ['KAREN_VOID','KAREN_BURN','KAREN_FORM']:actual_cast(card,card.lower())
    save('initial-geometry',actor_status());snap(case/'layout-before')

if 'boundary-enemies' in wanted:
    for item in enemy_specs:
        row=dict(name=item['name'],title=item['title'],result='failed',build=capture_build)
        try:
            enemy_fresh(item)
            enemies=living_enemies();player=next(e for e in geometry() if e['id']=='PLAYER')
            if item.get('pincer'):
                left=next(e for e in enemies if e['x']<player['x']);right=next(e for e in enemies if e['x']>player['x'])
                assert {left['id'],right['id']}=={'CRUSHER','ROCKET'},enemies
                for index,(side,target,card) in enumerate([
                    ('left',left,'KAREN_SHINE_TOGETHER'),('right',right,'KAREN_SHINE_STRIKE_BARRAGE'),
                    ('left',left,'KAREN_SHINE_STRIKE_BARRAGE'),('right',right,'KAREN_SHINE_TOGETHER')]):
                    targeted_cast(card,target['combatId'],str(index)+'-'+side)
                    actor=actor_status();assert actor['facing']==side.capitalize(),actor
                    assert (actor['bodyScaleX']<0)==(side=='left'),actor
                # Automatic targets are chosen by native play resolution; their
                # exact combat IDs and all direction changes remain in history.
                cmd('boundary seed '+('Draw' if item.get('void') else 'Promise')+' KAREN_BRIDGE,KAREN_SHINE_TOGETHER,KAREN_DEFEND')
                time.sleep(.6);add('KAREN_BRIDGE');cmd('boundary energy 0');before=len(probe()['finished']);snap(case/'nested-before');play('KAREN_BRIDGE');wait_done('KAREN_BRIDGE',before);settle('nested-after')
                victim=left if item['kill_side']=='left' else right
                survivor=right if item['kill_side']=='left' else left
                cmd('boundary weakenEnemy '+str(victim['combatId']));time.sleep(.3)
                targeted_cast('KAREN_BASEBALL',victim['combatId'],'one-side-lethal')
                wait_state(lambda s:all(e['combat_id']!=victim['combatId'] or e['hp']<=0 for e in s['battle']['enemies']))
                survivor_side='Right' if item['kill_side']=='left' else 'Left'
                actor=actor_status();assert actor['facing']==survivor_side,actor
                assert (actor['bodyScaleX']<0)==(survivor_side=='Left'),actor
                targeted_cast('KAREN_SHINE_STRIKE_BARRAGE',survivor['combatId'],'remaining-side')
                row['sides']=dict(left=left,right=right,survivor=survivor_side)
            else:
                if item.get('summons'):
                    cmd('block 999');round_before=state()['battle']['round'];post(action='end_turn')
                    wait_state(lambda s:s.get('battle',{}).get('round',0)>round_before and s['battle']['is_play_phase'],45)
                    enemies=living_enemies();save('summoned-geometry',actor_status());snap(case/'summoned')
                    assert len(enemies)>1,enemies
                for index,enemy in enumerate(enemies):
                    if index>=3:break
                    targeted_cast('KAREN_SHINE_STRIKE_BARRAGE',enemy['combatId'],'target-'+str(index))
                cmd('boundary seed Promise KAREN_BRIDGE,KAREN_DEFEND,KAREN_DEFEND');time.sleep(.6)
                add('KAREN_BRIDGE');cmd('boundary energy 0');before=len(probe()['finished']);snap(case/'nested-before');play('KAREN_BRIDGE');wait_done('KAREN_BRIDGE',before);settle('nested-after')
                row['enemyGeometry']=enemies
            actor=validate_actor('final');snap(case/'layout-after');assert actor['footFrames']>0,actor
            row.update(result='passed',actor=actor)
        except Exception as exc:
            row.update(error=str(exc),traceback=traceback.format_exc())
            try:snap(case/'failure');save('failure-state',state());save('failure-actor',actor_status())
            except Exception:pass
        enemy_results.append(row)
        (out/'enemy-results.json').write_text(json.dumps(enemy_results,ensure_ascii=False,indent=2),encoding='utf-8')
        print(branch,row['name'],row['result'],row.get('error',''),flush=True)
