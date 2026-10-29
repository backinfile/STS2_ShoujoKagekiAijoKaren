"""Targeted empty-pile transitions; uses the owned MCP runner context."""
import json,time

def cases():
    rows=[]
    def add(name,mode,count,steps,hand=0,inactive=0):
        rows.append(dict(name=name,mode=mode,count=count,hand=hand,inactive=inactive,steps=steps))
    for mode in ['normal','void']:
        def put(name,count,steps,**kw):add(mode+'-'+name,mode,count,steps,**kw)
        put('already-empty',0,[('empty-noops',dict(hits=0,energy=3,active=0))])
        put('partial-then-last',3,[('draw 1',dict(hits=0,active=2)),('draw 2',dict(hits=1,energy=4,active=0,hand=3))])
        put('batch-to-hand',3,[('transfer',dict(hits=1,energy=4,active=0,hand=3))])
        put('discard-all',3,[('discard',dict(hits=1,energy=4,active=0,discard=3))])
        put('discard-partial',3,[('discard-one 1',dict(hits=0,active=2)),('discard-one 2',dict(hits=1,energy=4,active=0,discard=3))])
        put('exhaust-all',3,[('exhaust',dict(hits=1,energy=4,active=0,exhaust=3))])
        put('remove-from-combat',3,[('remove',dict(hits=1,energy=4,active=0))])
        put('move-to-draw',3,[('to-draw',dict(hits=0 if mode=='void' else 1,energy=3 if mode=='void' else 4,active=3 if mode=='void' else 0,draw=3))])
        put('select-all',3,[('select 3',dict(hits=1,energy=4,active=0,hand=3))])
        put('autoplay-all',3,[('autoplay 3',dict(hits=1,energy=4,active=0,discard=3))])
        put('select-and-play-last',1,[('select-play',dict(hits=1,energy=4,active=0,discard=1))])
        put('switch-empty-hand',3,[('switch',dict(hits=1,energy=4,active=0,hand=3))])
        put('switch-with-hand',3,[('switch',dict(hits=1,energy=4,active=2,hand=3))],hand=2)
        put('switch-empty-source',0,[('switch',dict(hits=0,energy=3,active=2,hand=0))],hand=2)
        put('inactive-pile-cleared',3,[('inactive-clear',dict(hits=0,energy=3,active=3))],inactive=2)
        put('duplicate-event-and-unrelated-play',1,[('draw',dict(hits=1,energy=4)),('repeat-hook',dict(hits=1)),('play-hand',dict(hits=1)),('repeat-hook',dict(hits=1,energy=4))])
        for refill in ['refill-native','refill-direct','refill-token','refill-hand']:
            put(refill,1,[('draw',dict(hits=1)),(refill,dict(hits=1,active=1)),('draw',dict(hits=2,energy=5,active=0))])
        put('refill-from-discard',1,[('discard',dict(hits=1)),('refill-discard',dict(hits=1,active=1)),('draw',dict(hits=2,energy=5,active=0))])
        put('async-duplicate-event',1,[('reenter-same',dict(hits=0)),('draw',dict(hits=1,energy=4,active=0))])
        put('async-new-empty-cycle',1,[('reenter-refill',dict(hits=0)),('draw',dict(hits=2,energy=5,active=0,hand=2))])
        put('combat-end-cleanup',3,[('win',dict(hits=0,active=0,ending=True))])
    add('normal-draw-pile-empty-is-inactive','normal',0,[('native-draw',dict(hits=0,energy=3,draw=0,hand=1))],inactive=1)
    add('void-shuffle-second-empty','void',1,[('draw',dict(hits=1)),('discard-hand',dict(hits=1,active=0,discard=1)),('native-draw',dict(hits=2,energy=5,active=0,hand=1))])
    add('normal-to-void-through-card','normal',3,[('cast-void',dict(hits=1,energy=4,active=3,promise=0)),('draw 3',dict(hits=2,energy=5,active=0,hand=3))],inactive=3)
    add('normal-to-infinite-through-card','normal',1,[('draw',dict(hits=1)),('cast-infinite',dict(hits=1,energy=4,active=10)),('draw 3',dict(hits=1,energy=4,active=10,hand=4))])
    for mode in ['infinite','void-infinite']:
        add(mode+'-draw-and-discard',mode,3,[('draw 3',dict(hits=0,energy=3,active=10,hand=3)),('discard',dict(hits=0,energy=3,active=10))])
        add(mode+'-batch-clear-and-refill',mode,3,[('transfer',dict(hits=0,energy=3,hand=10)),('update',dict(hits=0,energy=3,active=10))])
    for mode in ['burn','past','void-burn-past']:
        add(mode+'-last-draw',mode,3,[('draw 3',dict(hits=1,energy=4,active=0,hand=3))])
    for mode in ['normal','void']:
        add(mode+'-select-partial-then-last',mode,3,[('select 1',dict(hits=0,active=2,hand=1)),('select 2',dict(hits=1,energy=4,active=0,hand=3))])
        add(mode+'-reorder-in-place',mode,3,[('reorder',dict(hits=0,energy=3,active=3)),('draw 3',dict(hits=1,energy=4,active=0))])
        add(mode+'-switch-twice',mode,3,[('switch',dict(hits=1,active=2,hand=3)),('switch',dict(hits=2,energy=5,active=3,hand=2))],hand=2)
        add(mode+'-full-hand-draw',mode,1,[('draw',dict(hits=0 if mode=='void' else 1,energy=3 if mode=='void' else 4,active=1 if mode=='void' else 0,hand=10,discard=0 if mode=='void' else 1))],hand=10)
        add(mode+'-selection-overflow',mode,3,[('select 3',dict(hits=1,energy=4,active=0,hand=10,discard=2))],hand=9)
        add(mode+'-stacked-energy-power',mode,1,[('stack-power',dict(hits=0)),('draw',dict(hits=1,energy=6,active=0))])
    for mode in ['normal','void']:
        add(mode+'-natural-turn-draw',mode,3,[('end-turn',dict(hits=1 if mode=='void' else 0,energy=4 if mode=='void' else 3,active=0 if mode=='void' else 3,hand=4 if mode=='void' else 1))])
        add(mode+'-last-card-lethal',mode+'-lethal',1,[('draw-win',dict(hits=1,active=0,ending=True))])
    return rows

def run_matrix(c,results):
    def snapshot():return json.loads(c.cmd('pile_empty snapshot')['message'])
    def settle():
        deadline=time.monotonic()+45;index=0
        while time.monotonic()<deadline:
            value=snapshot()
            if value['status'] in ['ready','finished']:return value
            assert not value['status'].startswith('error:'),value
            s=c.state();kind=s.get('state_type')
            if kind in ['card_select','hand_select']:
                selection=s[kind]
                if selection.get('can_confirm') or selection.get('preview_showing'):
                    c.act('combat_confirm_selection' if kind=='hand_select' else 'confirm_selection')
                elif index<len(selection.get('cards',[])):
                    if kind=='hand_select':c.act('combat_select_card',card_index=selection['cards'][index]['index'])
                    else:c.act('select_card',index=selection['cards'][index]['index'])
                    index+=1
            time.sleep(.2)
        raise RuntimeError('Timeout empty-pile operation: '+str(snapshot()))
    selected=cases()
    if c.case_filter:selected=[x for x in selected if x['name'] in c.case_filter.split(',')]
    assert selected,'No matching empty-pile cases'
    print('EMPTY_MATRIX cases='+str(len(selected)),flush=True)
    for spec in selected:
        name=spec['name'];entry=dict(case=name,setup={k:v for k,v in spec.items() if k!='steps'},steps=[],result='pass')
        try:
            c.cmd('fight NIBBITS_WEAK');c.wait(lambda s:s.get('battle',{}).get('is_play_phase'),'combat ready');time.sleep(.5)
            c.cmd(f"pile_empty setup {spec['mode']} {spec['count']} {spec['hand']} {spec['inactive']}");settle();time.sleep(.6)
            c.capture(name+'-before');entry['before']=snapshot();entry['handBefore']=c.hand(name+'-before')
            for i,(operation,expected) in enumerate(spec['steps']):
                if operation=='end-turn':
                    round_before=c.state()['battle']['round'];c.act('end_turn')
                    c.wait(lambda s:s.get('battle',{}).get('round',0)>round_before and s['battle'].get('is_play_phase'),'next natural player turn')
                    time.sleep(.6);value=snapshot()
                else:
                    c.cmd('pile_empty op '+operation);value=settle()
                entry['steps'].append(dict(operation=operation,expected=expected,actual=value))
                assert all(value.get(k)==v for k,v in expected.items()),(name,operation,expected,value)
            if value.get('ending'):c.wait(lambda s:s.get('state_type') in ['rewards','map'],'combat end UI settled')
            time.sleep(1)
            c.capture(name+'-after');entry['after']=snapshot();entry['handAfter']=c.hand(name+'-after')
            assert all(entry['after'].get(k)==v for k,v in spec['steps'][-1][1].items()),(name,'after settle',spec['steps'][-1][1],entry['after'])
            if 'burn' in spec['mode']:assert all(x['upgraded'] and x['exhaust'] for x in entry['after']['handCards']),entry
        except Exception as e:
            entry['result']='fail';entry['error']=str(e)
            try:c.capture(name+'-failure');entry['after']=snapshot()
            except Exception:pass
        results.append(entry)
        (c.out/'results.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
        print(json.dumps(dict(case=name,result=entry['result'],after=entry.get('after'),error=entry.get('error')),ensure_ascii=False),flush=True)
    assert all(r['result']=='pass' for r in results),'Empty-pile regression failures: '+','.join(r['case'] for r in results if r['result']!='pass')