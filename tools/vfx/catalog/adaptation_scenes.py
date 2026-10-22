"""Actual actor/state compatibility, executed in the isolated record.py context."""
import traceback

adapt_results=[]

def adapt_save():
    (out/'adaptation-results.json').write_text(json.dumps(adapt_results,ensure_ascii=False,indent=2),encoding='utf-8')

def actor_status():return json.loads(cmd('actor_state status')['message'])

exec(compile((main/'tools/vfx/catalog/attack_observer.py').read_text(encoding='utf-8-sig'), 'attack_observer.py', 'exec'))

def actual_cast(card,label,selection=None):
    cmd('boundary energy 20')
    if card not in [c['id'] for c in state()['player']['hand']]:add(card)
    snap(case/(label+'-before'));check_hand(label+'-before-hand')
    watch_attack(card,label);before=len(probe()['finished']);play(card)
    time.sleep(.12);snap(case/(label+'-during'))
    report=wait_done(card,before,selection=selection)
    time.sleep(2.2);snap(case/(label+'-after'));check_hand(label+'-after-hand')
    save(label+'-native',report);verify_attack(card,label)
    assert len(report['started'])==len(report['finished']),report
    assert not report['play'],report
    return report

def fresh_adaptation(name,void=False):
    stage(name,void,seed=False)
    cmd('promise_integration off');trim();cmd('showcase clear');cmd('stage_probe promise 0')
    cmd('showcase rebuild-tower');time.sleep(.4);cmd('promise_integration watch')
    cmd('actor_state watch');cmd('boundary watch');cmd('pzero watch')

def validate_actor(label):
    settle(label)
    status=actor_status();save(label+'-actor',status)
    assert not status['dead'] and status['actorVisible'] and status['bodyVisible'],status
    assert status['maxFootError']<.2,status
    native=probe();assert not native['play'] and native['activeBridges']==0,native
    comp=json.loads(cmd('showcase composition')['message']);save(label+'-composition',comp)
    assert all(comp[k]==0 for k in ('states','stages','benefits','back','hud')),comp
    owner=json.loads(cmd('pzero status')['message']);save(label+'-position0',owner)
    assert owner['ownership'] and (owner['amount']<=0 or owner['live']==1),owner
    return status

state_specs=[
    dict(name='state-debuff-hurt',title='虚弱/脆弱/易伤，原生受击与攻击姿态',setup=['KAREN_POSITION0'],debuff=True),
    dict(name='state-small-flip-stack',title='燃烧/光带/觉醒/Position 0，半尺寸翻转',setup=['KAREN_BURN','KAREN_PAST_AND_FUTURE','KAREN_FORM','KAREN_POSITION0'],transforms=['small','flip']),
    dict(name='state-void-large-offset',title='空虚/燃烧/觉醒，放大位移旋转',void=True,setup=['KAREN_BURN','KAREN_FORM','KAREN_POSITION0'],transforms=['large','offset','rotate']),
    dict(name='state-reproduce-stretch',title='再生产，非均匀缩放与燃料收益',setup=['KAREN_STAGE_REPRODUCE'],transforms=['stretch']),
    dict(name='state-temporary-material',title='临时替换人物材质，受击后恢复呼吸与新演出',setup=['KAREN_FORM','KAREN_POSITION0'],material=True),
    dict(name='state-death-revive',title='死亡表现/原生治疗复活，以及蜥蜴尾巴濒死恢复',setup=['KAREN_BURN','KAREN_FORM','KAREN_POSITION0'],revive=True),
]
if 'boundary-states' in wanted or any(n.startswith('boundary-state-') for n in wanted):
    # The first supplementary pass had only three draw cards: native turn draw
    # legitimately emptied the deck. Re-run the positive-draw condition with
    # eight actual cards; retain that failed precondition as a separate attempt.
    previous=out/'boundary-results.json'
    retry_cue=previous.exists() and any(r['name']=='boundary-cue-turn' and r['result']!='passed' for r in json.loads(previous.read_text(encoding='utf-8')))
    if retry_cue:
        row=dict(name='boundary-cue-turn',title='跨回合三能力，充足抽牌来源复测',result='failed',build=capture_build)
        try:
            fresh_adaptation('boundary-cue-turn-rerun')
            cmd('boundary seed Promise KAREN_TICKET,KAREN_WE_KOWN,KAREN_STARLIGHT_CARD')
            cmd('boundary seed Draw '+','.join(['KAREN_DEFEND']*8));time.sleep(.7)
            add('KAREN_BRIDGE');cmd('boundary energy 2');snap(case/'before');before=len(probe()['finished']);play('KAREN_BRIDGE');wait_done('KAREN_BRIDGE',before)
            number=state()['battle']['round'];post(action='end_turn')
            for _ in range(200):
                now=state()
                if now.get('battle',{}).get('round',0)>number and now['battle']['is_play_phase']:break
                time.sleep(.1)
            else:raise AssertionError('Turn did not finish')
            settle('return');actual_cast('KAREN_TOWER_OF_PROMISE','tower');actual_cast('KAREN_SHINE_TOGETHER','shine')
            cues=json.loads(cmd('stage_probe status')['message']);save('cues',cues)
            assert any(c['kind']=='TicketReturn' and c['cards']>0 for c in cues['casts']),cues
            assert any(c['kind']=='Act1Draw' and c['cards']>0 for c in cues['casts']),cues
            assert any(c['kind']=='DreamTrigger' and c['cards']>0 for c in cues['benefits']),cues
            validate_actor('final');snap(case/'after');row['result']='passed'
        except Exception as exc:row.update(error=str(exc),traceback=traceback.format_exc())
        adapt_results.append(row);adapt_save();print(branch,'cue-turn-rerun',row['result'],row.get('error',''),flush=True)
    for item in state_specs:
        if 'boundary-states' not in wanted and 'boundary-'+item['name'] not in wanted:continue
        row=dict(name=item['name'],title=item['title'],result='failed',build=capture_build)
        try:
            fresh_adaptation(item['name'],item.get('void',False))
            for i,card in enumerate(item['setup']):actual_cast(card,'setup-'+str(i))
            for transform in item.get('transforms',[]):cmd('actor_state '+transform)
            time.sleep(.6);snap(case/'state-before');save('state-before',actor_status())
            if item.get('debuff'):
                cmd('actor_state debuff');time.sleep(.3);save('native-debuffs',actor_status())
                cmd('damage 1 0');time.sleep(.15);snap(case/'hurt')
            if item.get('material'):
                cmd('actor_state temporary');time.sleep(.2);before_material=actor_status();save('temporary-before',before_material)
                cmd('damage 1 0');actual_cast('KAREN_TICKET','temporary-ticket')
                assert not actor_status()['materialRestored'],actor_status()
                cmd('actor_state restore');time.sleep(.5)
                a=actor_status();time.sleep(.5);b=actor_status();save('restored-animation',[a,b])
                assert a['materialRestored'] and b['idlePhase']!=a['idlePhase'],[a,b]
            if item.get('revive'):
                for delay,label in [(.15,'interrupted'),(.9,'settled')]:
                    cmd('actor_state death');time.sleep(delay);snap(case/('death-'+label));dead=actor_status();save('death-'+label,dead)
                    assert dead['dead'] and dead['corpseVisible'] and dead['spriteVisible']==(delay<.55),dead
                    cmd('actor_state revive');time.sleep(1.2);snap(case/('revived-'+label));alive=actor_status();save('revived-'+label,alive)
                    assert not alive['dead'] and alive['spriteVisible'] and alive['materialRestored'] and not alive['corpseVisible'] and not alive['curtainVisible'],alive
                cmd('relic LIZARD_TAIL');cmd('damage 9999 0');time.sleep(2)
                alive=actor_status();save('native-tail-revive',alive);snap(case/'native-tail-revive')
                assert not alive['dead'] and alive['hp']>0 and alive['materialRestored'],alive
            if 'KAREN_STAGE_REPRODUCE' in item['setup']:
                actual_cast('KAREN_DROP_FUEL','reproduce-fuel')
            else:
                cmd('boundary seed '+('Draw' if item.get('void') else 'Promise')+' KAREN_BRIDGE,KAREN_DEFEND,KAREN_DEFEND');time.sleep(.6)
                add('KAREN_BRIDGE');cmd('boundary energy 0');snap(case/'nested-before');before=len(probe()['finished']);play('KAREN_BRIDGE');wait_done('KAREN_BRIDGE',before)
                settle('nested-after')
            actual_cast('KAREN_PASSION','passion')
            actual_cast('KAREN_SHINE_TOGETHER','attack')
            result=validate_actor('adapted');snap(case/'state-after')
            if 'KAREN_POSITION0' in item['setup']:assert result['footFrames']>0,result
            cmd('actor_state restore');time.sleep(.5);validate_actor('restored');snap(case/'restored')
            row.update(result='passed',actor=result)
        except Exception as exc:
            row.update(error=str(exc),traceback=traceback.format_exc())
            try:snap(case/'failure');save('failure-actor',actor_status())
            except Exception:pass
        adapt_results.append(row);adapt_save();print(branch,item['name'],row['result'],row.get('error',''),flush=True)


def wait_state(predicate,seconds=30):
    until=time.monotonic()+seconds
    while time.monotonic()<until:
        current=state()
        if predicate(current):return current
        if current.get('menu_screen')=='tutorial_prompt':post(action='menu_select',option='no')
        time.sleep(.15)
    raise AssertionError(('Menu/actor transition timed out',current))

def start_character(character):
    # Only the disposable copied profile is affected. Remove native timeline
    # unlock gates so repeated real runs remain available to the MCP.
    current=state()
    if current.get('player'):
        cmd('unlock epochs');time.sleep(.6)
    if current.get('player'):
        if current.get('battle'):
            cmd('promise_integration off')
        snap(case/'previous-run-before-death');cmd('die');wait_state(lambda s:s['state_type']=='game_over',45)
        time.sleep(1.5);snap(case/'previous-run-ended')
        cleaned=json.loads(cmd('actor_state global')['message']);save('previous-run-cleanup',cleaned)
        assert cleaned['registered']==cleaned['states']==0,cleaned
        post(action='menu_select',option='main_menu')
    wait_state(lambda s:s.get('menu_screen')=='main' and 'singleplayer' in s.get('options',[]))
    time.sleep(2) # Native menu preloading must finish before a new run.
    post(action='menu_select',option='singleplayer')
    current=wait_state(lambda s:s.get('menu_screen') in ('singleplayer','character_select'))
    if current.get('menu_screen')=='singleplayer':
        post(action='menu_select',option='standard');wait_state(lambda s:s.get('menu_screen')=='character_select')
    save('character-options',state())
    post(action='menu_select',option=character);post(action='menu_select',option='embark')
    wait_state(lambda s:s.get('state_type')=='map' or (s.get('state_type')=='event' and (s.get('event',{}).get('in_dialogue') or s.get('event',{}).get('options'))),45)
    time.sleep(2)
    # Finish the actual opening event before replacing its room. A populated player
    # object alone is too early: EventSynchronizer may still be initializing.
    for opening_step in range(24):
        current=state();save('opening-'+str(opening_step),current)
        if current['state_type']=='map':break
        event=current.get('event',{})
        if current['state_type']=='bundle_select':
            if not current['bundle_select']['preview_showing']:post(action='select_bundle',index=0)
            else:post(action='confirm_bundle_selection')
        elif current['state_type']=='rewards' and current.get('rewards',{}).get('can_proceed'):
            post(action='proceed')
        elif current['state_type']=='card_select':
            cards=current['card_select']['cards'];assert cards,current
            post(action='select_card',index=cards[0]['index']);time.sleep(.3)
            if state()['state_type']=='card_select' and state()['card_select'].get('can_confirm'):post(action='confirm_selection')
        elif event.get('in_dialogue'):post(action='advance_dialogue')
        elif event.get('options'):
            options=[o for o in event['options'] if not o.get('is_locked') and not o.get('was_chosen')]
            option=next((o for o in options if o.get('is_proceed')),None) or next((o for o in options if any(word in o.get('description','') for word in ('金币','最大生命','药水栏位'))),options[0])
            post(action='choose_event_option',index=option['index'])
        else:time.sleep(.5);continue
        time.sleep(1)
    else:raise AssertionError(('Opening event did not reach map',state()))
    time.sleep(1)
    # The opening event is now complete; enter the native controlled encounter.
    if 'boundary-fixes' in wanted:cmd('entrance_probe watch')
    _original_fight();trim();cmd('showcase clear');cmd('boundary watch');cmd('actor_state watch')
    report=actor_status();save('actual-character',report);assert report['character']==character,report

if 'boundary-actors' in wanted or any(n.startswith('boundary-actor-') for n in wanted):
    for character in ['IRONCLAD','SILENT','DEFECT','REGENT','NECROBINDER']:
        if 'boundary-actors' not in wanted and 'boundary-actor-'+character.lower() not in wanted:continue
        case=out/('actor-'+character.lower());case.mkdir(exist_ok=True)
        row=dict(name=case.name,title='原版 '+character+' 实际持牌/出牌',result='failed',build=capture_build,steps=[])
        try:
            start_character(character)
            # Bootstrap the pile through an actual Fall, before any forced tower reconstruction.
            add('KAREN_DEFEND');add('KAREN_STRIKE');add('KAREN_FALL');cmd('boundary energy 20');snap(case/'fall-before');play('KAREN_FALL')
            choice=wait_state(lambda s:s['state_type']=='hand_select');post(action='combat_select_card',card_index=choice['hand_select']['cards'][0]['index'])
            if state()['state_type']=='hand_select':post(action='combat_confirm_selection')
            wait_done('KAREN_FALL',0);time.sleep(.8);snap(case/'fall-after');check_hand('fall-hand')
            cmd('promise_integration watch');settle('pile-created-by-play')
            cmd('bridge_vfx watch');cmd('stage_probe watch');cmd('pzero watch')
            for card in ['KAREN_TOWER_OF_PROMISE','KAREN_POSITION0','KAREN_TICKET','KAREN_STARLIGHT02_CARD','KAREN_SHINE_TOGETHER','KAREN_SHINE_STRIKE_BARRAGE','KAREN_BASEBALL']:
                actual_cast(card,card.lower());row['steps'].append(card)
            cmd('boundary seed Promise KAREN_BRIDGE,KAREN_DEFEND,KAREN_DEFEND');time.sleep(.6)
            add('KAREN_BRIDGE');cmd('boundary energy 0');snap(case/'bridge-before');before=len(probe()['finished']);play('KAREN_BRIDGE');wait_done('KAREN_BRIDGE',before);settle('bridge-after')
            row['steps'].append('nested-bridge')
            actual_cast('KAREN_EXCHANGE_FATE','exchange')
            actual_cast('KAREN_VOID','void')
            for card in ['KAREN_BURN','KAREN_FORM','KAREN_PASSION']:
                actual_cast(card,card.lower());row['steps'].append(card)
            cmd('showcase seed KAREN_DEFEND 3');time.sleep(.6);add('KAREN_BRIDGE');cmd('boundary energy 0')
            snap(case/'void-bridge-before');before=len(probe()['finished']);play('KAREN_BRIDGE');wait_done('KAREN_BRIDGE',before);settle('void-bridge-after')
            result=validate_actor('final');snap(case/'final')
            assert result['character']==character and result['footFrames']>0,result
            shine=probe();save('final-shine',shine)
            assert len(shine['shine'])>=1 and result['liveDepletes']==0,(shine,result)
            row.update(result='passed',actor=result)
        except Exception as exc:
            row.update(error=str(exc),traceback=traceback.format_exc())
            try:snap(case/'failure');save('failure-state',state())
            except Exception:pass
        adapt_results.append(row);adapt_save();print(branch,row['name'],row['result'],row.get('error',''),flush=True)
