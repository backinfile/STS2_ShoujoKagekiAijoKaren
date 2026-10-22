"""Targeted sequential boundaries, executed by record.py in copied game environments."""
from collections import Counter
import traceback

specs = [
    dict(name='boundary-empty', title='空约定，X=0', source=[], energy=0, started=1, casts=1, spots=0),
    dict(name='boundary-chain2', title='塔桥打出塔桥再打出塔桥', source=['KAREN_BRIDGE','KAREN_BRIDGE','KAREN_DEFEND'], energy=0, started=4, casts=3, spots=3),
    dict(name='boundary-reserved', title='外层预留三张，内层只取未预留的牌', source=['KAREN_BRIDGE','KAREN_DEFEND','KAREN_STRIKE','KAREN_DEFEND','KAREN_DEFEND'], energy=2, started=5, casts=2, spots=4),
    dict(name='boundary-deep', title='八层塔桥调用栈', source=['KAREN_BRIDGE']*7+['KAREN_DEFEND'], energy=0, started=9, casts=8, spots=8),
    dict(name='boundary-upgrade', title='升级塔桥，零能量，内层塔桥', source=['KAREN_BRIDGE','KAREN_DEFEND','KAREN_STRIKE','KAREN_DEFEND'], energy=0, upgrade=True, started=4, casts=2, spots=3),
    dict(name='boundary-void-chain', title='空虚从抽牌堆预留的三层塔桥', void=True, source=['KAREN_BRIDGE','KAREN_BRIDGE','KAREN_DEFEND'], energy=0, started=4, casts=3, spots=3),
    dict(name='boundary-void-empty', title='空虚抽弃牌堆皆空', void=True, source=[], energy=0, started=1, casts=1, spots=0),
    dict(name='boundary-void-shuffle', title='空虚抽牌堆空，从弃牌堆洗牌', void=True, source=[], discard=['KAREN_DEFEND','KAREN_STRIKE','KAREN_DEFEND'], energy=2, started=4, casts=1, spots=3),
    dict(name='boundary-switch-mode', title='塔桥自动打出空虚，再自动打出塔桥', source=['KAREN_VOID','KAREN_BRIDGE','KAREN_DEFEND','KAREN_STRIKE','KAREN_DEFEND'], energy=2, started=5, casts=2, spots=4),
    dict(name='boundary-recall', title='内层约定之塔取回剩余牌，外层继续预留牌', source=['KAREN_TOWER_OF_PROMISE','KAREN_DEFEND','KAREN_STRIKE','KAREN_DEFEND','KAREN_DEFEND'], energy=2, started=4, casts=1, spots=3),
    dict(name='boundary-exchange', title='塔桥自动命运交换，预留出牌与整手交换交错', source=['KAREN_EXCHANGE_FATE','KAREN_DEFEND','KAREN_STRIKE','KAREN_DEFEND'], hand=['KAREN_DEFEND','KAREN_STRIKE'], energy=2, started=4, casts=1, spots=3),
    dict(name='boundary-selection', title='自动 Why 选牌期间保留塔桥舞台', source=['KAREN_WHY','KAREN_DEFEND','KAREN_STRIKE'], hand=['KAREN_DEFEND']*4, energy=2, selection='hand_select', started=4, casts=1, spots=3),
    dict(name='boundary-replay', title='同一张塔桥真实额外重播两次', source=['KAREN_DEFEND']*6, energy=0, replay=2, started=6, casts=3, spots=3),
    dict(name='boundary-fast', title='Fast 深层塔桥', source=['KAREN_BRIDGE']*3+['KAREN_DEFEND'], energy=0, speed='fast', started=5, casts=4, spots=4),
    dict(name='boundary-instant', title='Instant 深层塔桥，规则继续而表现跳过', source=['KAREN_BRIDGE']*3+['KAREN_DEFEND'], energy=0, speed='instant', started=5, casts=0, spots=0),
    dict(name='boundary-full-recall', title='满手牌塔桥自动取回，容量溢出留在约定', source=['KAREN_TOWER_OF_PROMISE']+['KAREN_DEFEND']*4, hand=['KAREN_DEFEND']*9, energy=0, started=2, casts=1, spots=1, hand_count=10, promise_count=3),
    dict(name='boundary-act2-shine', title='第二幕下塔桥自动打出闪耀攻击并耗尽', setup=['KAREN_STARLIGHT02_CARD'], source=['KAREN_SHINE_TOGETHER','KAREN_DEFEND'], energy=0, started=4, casts=1, spots=1, shine_delta=1),
    dict(name='boundary-stage-stack', title='燃烧、光带、觉醒叠加塔桥嵌套', setup=['KAREN_BURN','KAREN_PAST_AND_FUTURE','KAREN_FORM'], source=['KAREN_BRIDGE','KAREN_DEFEND','KAREN_STRIKE','KAREN_DEFEND'], energy=0, casts=2),
    dict(name='boundary-reproduce', title='再生产十张续演和塔桥的真实耗尽补充', setup=['KAREN_STAGE_REPRODUCE'], source=[], energy=2, casts=1, spots=3),
    dict(name='boundary-new-cues', title='塔桥自动门票、梦想、第一幕，三种短演出串联', source=['KAREN_TICKET','KAREN_WE_KOWN','KAREN_STARLIGHT_CARD','KAREN_DEFEND'], energy=3, casts=1),
    dict(name='boundary-cue-turn', title='自动三种能力后跨回合生成、取塔和第一幕抽牌', source=['KAREN_TICKET','KAREN_WE_KOWN','KAREN_STARLIGHT_CARD'], draw=['KAREN_DEFEND']*8, energy=2, followup=True, started=6, casts=1, spots=3),
    dict(name='boundary-multi-shine', title='第二幕下连续多张不同闪耀牌的耗尽反馈', setup=['KAREN_STARLIGHT02_CARD'], source=['KAREN_SHINE_TOGETHER','KAREN_PASSION','KAREN_ARROGANT'], energy=2, casts=1, spots=3, shine_delta=2),
    dict(name='boundary-run-newday', title='自动行动和新的一天改变结果牌堆', source=['KAREN_RUN','KAREN_NEW_DAY','KAREN_STRIKE','KAREN_DEFEND'], draw=['KAREN_DEFEND']*3, energy=2, started=4, casts=1, spots=3),
    dict(name='boundary-fuel-replay', title='双幕叠加下自动燃料三连播、抽牌回能与耗尽', setup=['KAREN_STARLIGHT_CARD','KAREN_STARLIGHT02_CARD'], source=['KAREN_DROP_FUEL','KAREN_DEFEND','KAREN_STRIKE'], draw=['KAREN_DEFEND']*8, energy=2, started=6, casts=1, spots=3, shine_delta=1),
    dict(name='boundary-why-empty', title='自动 Why 无手牌，无可选目标立即收尾', source=['KAREN_WHY','KAREN_DEFEND'], energy=1, started=3, casts=1, spots=2),
    dict(name='boundary-wakeup', title='自动唤醒原生选牌界面和塔桥灯光共存', source=['KAREN_WAKE_UP','KAREN_DEFEND'], draw=['KAREN_DEFEND']*3, energy=1, selection='card_select', started=3, casts=1, spots=2, hand_count=2),
    dict(name='boundary-energy-change', title='燃料中途回能改变内层 X，不改变外层预留数', source=['KAREN_DROP_FUEL','KAREN_BRIDGE']+['KAREN_DEFEND']*4, draw=['KAREN_DEFEND']*2, energy=1, started=6, casts=2, spots=5, promise_count=1),
    dict(name='boundary-lethal', title='塔桥首张攻击致命，后续预留牌与追光清理', source=['KAREN_STRIKE','KAREN_BRIDGE','KAREN_DEFEND'], energy=2, lethal=True),
    dict(name='boundary-next-clean', title='致命结束后的下一场无旧舞台与耗尽票据', source=[], energy=0, started=1, casts=1, spots=0),
]
# Exact IDs validated against ModelDb before a scenario can count as passed.
# A separate result is kept for every scenario, including failed setup/preconditions.
selected = specs if 'boundary-suite' in wanted else [s for s in specs if s['name'] in wanted]
(out/'boundary-plan.json').write_text(json.dumps(selected,ensure_ascii=False,indent=2),encoding='utf-8')
results=[]
_original_fight=fight

def finish_combat():
    if state().get('battle') and state().get('state_type') not in ('rewards','map'):
        cmd('kill all')
        ended_round=-1;ending_turns=0
        for _ in range(200):
            ending=state()
            if ending.get('state_type')=='rewards': break
            # Waterfall Giant's native steam phase delays death through its stun and explosion turns.
            if ending_turns<3 and ending.get('battle',{}).get('is_play_phase') and ending['battle']['round']!=ended_round and any(e['hp']>=100000000 for e in ending['battle']['enemies']):
                snap(case/('boss-delayed-death-before-'+str(ending_turns)));ended_round=ending['battle']['round'];post(action='end_turn');ending_turns+=1
            time.sleep(.1)
        else: raise AssertionError('Native combat end did not reach rewards')
        time.sleep(.8)

def safe_fight():
    finish_combat()
    _original_fight()
fight=safe_fight

def probe(): return json.loads(cmd('boundary status')['message'])
def seed(pile, ids):
    if ids: cmd('boundary seed '+pile+' '+','.join(ids));time.sleep(.8)
def wait_done(root,before=0,timeout=35,selection=False):
    started_at=time.monotonic(); chosen=False; samples=[];shots=0;peak_saved=False
    while time.monotonic()-started_at<timeout:
        current=state()
        if selection and current['state_type']==selection and not chosen:
            time.sleep(.75); snap(case/'selection-held')
            held=json.loads(cmd('bridge_vfx status')['message']);save('selection-held-bridge',held)
            assert held['nodeCount']>0,held
            cards=current[selection]['cards'][:2]
            for card in cards:
                if selection=='hand_select':post(action='combat_select_card',card_index=card['index'])
                else:post(action='select_card',index=card['index'])
            if state()['state_type']==selection:
                if selection=='hand_select':post(action='combat_confirm_selection')
                else:post(action='confirm_selection')
            chosen=True
        observed=probe(); samples.append(dict(elapsed=time.monotonic()-started_at,report=observed,state_type=current['state_type']))
        if observed['maxStages']>=8 and not peak_saved:
            snap(case/'peak-depth');peak_saved=True
        if shots<3 and len(observed['started'])>before+1:
            snap(case/('during-'+str(shots)));shots+=1
        if len(observed['finished'])>before and any(c['id']==root for c in observed['finished'][before:]) and len(observed['finished'])==len(observed['started']) and not observed['play'] and observed['activeBridges']==0:
            save('timeline',samples);return observed
        if current['state_type']=='rewards':
            save('timeline',samples);return observed
        time.sleep(.15)
    save('timeline',samples)
    raise AssertionError(('Resolution timed out',samples[-1]))

for spec in selected:
    started_at=time.monotonic()
    row=dict(name=spec['name'],title=spec['title'],result='failed',build=capture_build)
    try:
        stage(spec['name'],void=spec.get('void',False),seed=False)
        cmd('stage_probe speed '+spec.get('speed','normal'))
        cmd('promise_integration off');trim();cmd('showcase clear');cmd('stage_probe promise 0');cmd('showcase rebuild-tower');time.sleep(.6);cmd('promise_integration watch')
        for card in spec.get('setup',[]):
            cmd('energy 20');add(card);before=len(probe()['finished']);play(card);wait_done(card,before);settle('setup-'+card)
        trim();cmd('showcase clear');time.sleep(.6)
        # Clearing is fixture setup; all tested transitions below use real card plays.
        seed('Draw' if spec.get('void') else 'Promise',spec['source'])
        seed('Draw',spec.get('draw',[]))
        seed('Discard',spec.get('discard',[]))
        seed('Hand',spec.get('hand',[]))
        add('KAREN_BRIDGE')
        if spec.get('upgrade'):cmd('stage_probe upgrade KAREN_BRIDGE')
        if spec.get('replay'):cmd('boundary replay Hand KAREN_BRIDGE '+str(spec['replay']))
        if spec.get('lethal'):cmd('stage_probe weaken')
        cmd('boundary energy '+str(spec['energy']));time.sleep(.6)
        cmd('showcase rebuild-tower');time.sleep(.4);cmd('bridge_vfx watch');cmd('boundary watch');cmd('stage_probe watch');cmd('shine_motion watch')
        settle('before');check_hand('before-hand');snap(case/'before')
        baseline=probe();before=len(baseline['finished']);shine_before=len(baseline['shine'])
        row['baseline_finished']=before;row['shine_before']=shine_before
        if spec.get('lethal'):cmd('promise_integration off')
        play('KAREN_BRIDGE'); observed=wait_done('KAREN_BRIDGE',before,selection=spec.get('selection',False))
        if spec.get('followup'):
            round_before=state()['battle']['round'];post(action='end_turn')
            for _ in range(200):
                current=state()
                if current.get('battle',{}).get('round',0)>round_before and current['battle']['is_play_phase']:break
                time.sleep(.1)
            else:raise AssertionError('Turn-triggered benefits did not complete')
            settle('turn-return');snap(case/'turn-return')
            for card in ['KAREN_TOWER_OF_PROMISE','KAREN_SHINE_TOGETHER']:
                if card not in [c['id'] for c in state()['player']['hand']]:add(card)
                cmd('boundary energy 10');before_follow=len(probe()['finished']);play(card);wait_done(card,before_follow);settle('follow-'+card)
            cues=json.loads(cmd('stage_probe status')['message']);save('followup-cues',cues)
            assert any(c['kind']=='TicketReturn' and c['cards']>0 for c in cues['casts']),cues
            assert any(c['kind']=='Act1Draw' and c['cards']>0 for c in cues['casts']),cues
            assert any(c['kind']=='DreamTrigger' and c['cards']>0 for c in cues['benefits']),cues
        time.sleep(2.5)
        snap(case/'after');check_hand('after-hand')
        observed=probe();save('observed',observed)
        bridge=json.loads(cmd('bridge_vfx status')['message']);save('bridge',bridge)
        save('stage',json.loads(cmd('stage_probe status')['message']))
        motion=json.loads(cmd('shine_motion status')['message']);save('shine-motion',motion)
        played=observed['started'][before:];done=observed['finished'][before:]
        row.update(started=played,finished=done,casts=bridge['casts'],spots=bridge['spots'],maxStages=observed['maxStages'])
        assert observed['activeBridges']==0,observed
        assert not observed['play'],observed['play']
        assert len(played)==len(done),(played,done)
        if not spec.get('lethal'):
            final=settle('settled');save('final-state',final)
            comp=json.loads(cmd('showcase composition')['message']);save('composition',comp)
            assert all(comp[k]==0 for k in ('registered','states','stages','benefits','back','hud')),comp
            assert bridge['prematureExitFrames']==bridge['lightingGapFrames']==0,bridge
            assert observed['duplicateSpotlightFrames']==0,observed
            if spec['name'] in ('boundary-chain2','boundary-void-chain'): assert observed['maxStages']>=3,observed
            if spec['name']=='boundary-deep': assert observed['maxStages']>=8,observed
        for key,actual in [('started',len(played)),('casts',bridge['casts']),('spots',bridge['spots']),('hand_count',len(observed['hand'])),('promise_count',len(observed['promise'])),('shine_delta',len(observed['shine'])-shine_before)]:
            if key in spec: assert actual==spec[key],(key,actual,spec[key])
        assert motion['liveDepletes']==motion['liveArrivals']==motion['counter']['pending']==0,motion
        if spec.get('lethal'): assert state()['state_type']=='rewards',state()
        row['result']='passed'
    except Exception as exc:
        row['error']=str(exc);row['traceback']=traceback.format_exc()
        try:snap(case/'failure');save('failure-state',state());save('failure-probe',probe())
        except Exception:pass
    row['seconds']=round(time.monotonic()-started_at,2)
    results.append(row)
    (out/'boundary-results.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
    print(branch,spec['name'],row['result'],row.get('error',''),flush=True)
    try:cmd('stage_probe speed normal')
    except Exception:pass
cmd('promise_integration off')
print('BOUNDARY RESULTS',Counter(r['result'] for r in results),flush=True)
