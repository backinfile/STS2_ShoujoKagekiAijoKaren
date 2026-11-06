"""Executed inside the existing isolated MCP recorder. Native game/model actions only."""
def lw_status(name=None):
    result=json.loads(cmd('lastword_stage status')['message'])
    if name:save(name+'-probe',result)
    return result

def lw_count():
    return len(lw_status()['current']['nodes'])

def lw_expect(name,count,delay=1.0):
    time.sleep(delay)
    result=lw_status(name)
    assert len(result['current']['nodes'])==(0 if baseline else count),(name,result)
    assert len(result['current']['tNodes'])==count,(name,'T cue',result)
    if count:assert result['current']['tNodes'][0]['particles']>0,(name,'Missing T particles',result)
    check_hand(name+'-hand')
    snap(case/name)
    return result

def lw_remove(card,pile):
    cmd(('remove_card ' if pile=='Hand' else 'lastword_stage remove ')+card+' '+pile)
    time.sleep(.25)

if wanted & {'lastword-stage','lastword-stage-before','lastword-motion-before'}:
    baseline='lastword-stage-before' in wanted
    motion_baseline='lastword-motion-before' in wanted
    stage('lastword-motion-before' if motion_baseline else 'lastword-stage-before' if baseline else 'lastword-stage',seed=True)
    cmd('promise_integration off')
    cmd('showcase clear');trim()
    prep('KAREN_LAST_WORD');prep('KAREN_DEFEND')
    cmd('lastword_stage watch')
    lw_expect('blocked-before',0)
    record_start()
    marking('最后的台词｜条件尚未满足')
    time.sleep(1.0)
    marking('顶部扇形追光｜条件满足，依次亮起')
    lw_remove('KAREN_DEFEND','Hand')
    time.sleep(2.0)
    lw_status('motion-idle-start')
    marking('常态｜错相扫动、呼吸与上浮光尘')
    time.sleep(3.0)
    lw_status('motion-idle-end')
    card=next(c for c in state()['player']['hand'] if c['id']=='KAREN_LAST_WORD')
    marking('原生悬停｜灯束平滑收拢、提亮并加快流动')
    post(action='hover_card',card_index=card['index'])
    time.sleep(.12)
    lw_status('motion-hover-enter')
    time.sleep(.5)
    lw_status('motion-hover-ready')
    time.sleep(4.0)
    lw_status('motion-hover-held')
    cmd('lastword_stage refresh')
    time.sleep(.3)
    lw_status('motion-hover-refreshed')
    marking('移开｜平滑恢复常态，T 字随卡牌收回')
    post(action='unhover_card')
    time.sleep(.12)
    lw_status('motion-hover-exit')
    time.sleep(1.0)
    lw_status('motion-idle-restored')
    time.sleep(1.2)
    marking('连续悬停/移开｜无叠加、无重播')
    for _ in range(3):
        post(action='hover_card',card_index=card['index']);time.sleep(.22)
        post(action='unhover_card');time.sleep(.22)
    time.sleep(.7)
    lw_status('motion-rapid-restored')
    record_stop()
    ready_result=lw_expect('ready-after-recording',1)
    archive=case/'ready-recording'
    archive.mkdir(exist_ok=True)
    for path in list(case.iterdir()):
        if path.is_file() and (path.name=='game.mp4' or path.name.startswith('recording') or path.name=='markers.json'):
            shutil.copy2(path,archive/path.name)
    assert ready_result['peak']==(0 if baseline else 1),ready_result
    assert ready_result['peakTNodes']==1 and ready_result['peakTParticles']>0,ready_result
    if not baseline and not motion_baseline:
        post(action='hover_card',card_index=card['index'])
        hovered=lw_expect('hover-screenshot',1,.7)
        assert hovered['current']['nodes'][0]['hover']>.99,hovered
        post(action='unhover_card')
        restored=lw_expect('hover-return-screenshot',1,.7)
        assert restored['current']['nodes'][0]['hover']<.01,restored
        assert ready_result['peakChildren']==1,ready_result
        for pile in ['Draw','Discard','Hand']:
            snap(case/('before-block-'+pile))
            cmd('card KAREN_DEFEND '+pile)
            lw_expect('blocked-'+pile,0)
            snap(case/('before-restore-'+pile))
            lw_remove('KAREN_DEFEND',pile)
            lw_expect('restored-'+pile,1)

        # Two Last Words cannot both satisfy the condition; removing one retargets safely.
        snap(case/'before-duplicate')
        prep('KAREN_LAST_WORD')
        lw_expect('duplicate-blocked',0)
        lw_remove('KAREN_LAST_WORD','Hand')
        lw_expect('duplicate-restored',1)
        original_nodes=lw_status()['current']['nodes']
        cmd('lastword_stage refresh')
        stable=lw_expect('repeated-native-refresh',1)
        assert stable['current']['nodes'][0]['instance']==original_nodes[0]['instance'],stable

        # Pooled/replaced view must not keep a stale screen overlay alive.
        snap(case/'before-remove')
        lw_remove('KAREN_LAST_WORD','Hand')
        lw_expect('removed-lastword',0)
        snap(case/'before-recreate')
        prep('KAREN_LAST_WORD')
        lw_expect('recreated-lastword',1)

        api('/settings',dict(fullscreen=False,window_width=1024,window_height=768))
        lw_expect('resized-4x3',1)
        api('/settings',dict(fullscreen=False,window_width=1280,window_height=720))
        lw_expect('resized-16x9',1)

        # Record the real play and unchanged film; wait for rewards rather than a fixed duration.
        snap(case/'before-native-play')
        check_hand('before-native-play-hand')
        record_start()
        marking('最后的台词｜追光常驻')
        time.sleep(1.2)
        marking('真实出牌｜追光收回，保留原终幕演出')
        play('KAREN_LAST_WORD')
        for _ in range(160):
            s=state()
            if s['state_type']=='rewards':break
            time.sleep(.15)
        else:raise AssertionError(('No reward screen',s))
        time.sleep(1.0)
        record_stop()
        # Preserve the first readiness recording separately from the native-play recording.
        lw_expect('rewards-cleanup',0)
        print('LASTWORD_STAGE_MAIN_PASSED',branch,flush=True)

if 'lastword-stage-void' in wanted:
    baseline=False
    stage('lastword-stage-void',void=True,seed=True)
    cmd('promise_integration off')
    cmd('showcase clear');trim()
    prep('KAREN_LAST_WORD')
    cmd('card KAREN_DEFEND Draw')
    cmd('lastword_stage watch')
    lw_expect('void-draw-allowed',1)
    snap(case/'before-discard-block')
    cmd('card KAREN_DEFEND Discard')
    lw_expect('void-discard-blocked',0)
    lw_remove('KAREN_DEFEND','Discard')
    lw_expect('void-restored',1)
    # Changing combat rooms clears the old GlobalUi overlay even while the model survives.
    snap(case/'before-room-change')
    cmd('fight WATERFALL_GIANT_BOSS')
    settle_history=[]
    for _ in range(120):
        current=state()
        hand_result=api('/singleplayer',dict(action='run_command',command='karen_check_hand'))
        hand=json.loads(hand_result['message'])
        settled=current.get('battle',{}).get('is_play_phase') and hand['modelCount']>=5 and not any(c['id']=='KAREN_LAST_WORD' for c in current.get('player',{}).get('hand',[])) and hand['modelCount']==hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons']
        settle_history.append(dict(time=time.monotonic(),hand=hand,playPhase=current.get('battle',{}).get('is_play_phase')))
        if settled:break
        time.sleep(.1)
    else:raise AssertionError(('New combat hand did not settle',settle_history))
    save('new-room-settle',settle_history)
    lw_expect('new-room-cleanup',0,0.2)
    print('LASTWORD_STAGE_VOID_PASSED',branch,flush=True)

if 'lastword-stage-debug' in wanted:
    baseline=False
    stage('lastword-stage-debug',seed=True)
    cmd('promise_integration off')
    cmd('showcase clear');trim()
    prep('KAREN_LAST_WORD')
    cmd('lastword_stage watch')
    lw_expect('debug-ready',1,1.0)
    print(json.dumps(lw_status()['current'],ensure_ascii=False),flush=True)
