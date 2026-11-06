# Executed within the existing isolated MCP recorder, never by the production Mod.
def polish_status():
    return json.loads(cmd('vfx_polish status')['message'])


def polish_done(expected_rewards=None):
    for _ in range(100):
        observed = polish_status()
        if observed['live'] == 0 and observed['back'] == 0:
            break
        time.sleep(.05)
    else:
        raise AssertionError(('VFX did not clear', observed))
    assert all(n['exited'] and n['frames'] > 0 for n in observed['nodes']), observed
    if expected_rewards is not None:
        assert observed['relicRewards'] == expected_rewards and observed['passion'] == expected_rewards, observed
    save('polish-status', observed)
    check_hand('polish-finished-hand')
    return observed


def polish_jealousy_scene():
    stage('polish-jealousy', seed=False)
    prep('KAREN_JEALOUSY'); prep('KAREN_JEALOUSY'); cmd('jealousy_probe upgrade-last')
    cmd('vfx_polish watch'); cmd('jealousy_probe watch')
    hp = state()['battle']['enemies'][0]['hp']
    record_start()
    marking('嫉妒｜普通攻击，原位命中与向玩家回飘'); play('KAREN_JEALOUSY'); time.sleep(2.8)
    marking('嫉妒＋｜相同运动方向，20点实际伤害'); play('KAREN_JEALOUSY'); time.sleep(2.8)
    assert hp - state()['battle']['enemies'][0]['hp'] == 35
    save('jealousy', json.loads(cmd('jealousy_probe status')['message']))
    polish_done(0); finish()


if 'suitability-jealousy-mahiru' in wanted:
    polish_jealousy_scene()


if 'suitability-polish' in wanted:
    polish_jealousy_scene()

    stage('polish-wake', seed=False)
    cmd('wake_probe seed draw 3 base'); time.sleep(2.3)
    cmd('wake_probe watch'); cmd('vfx_polish watch')
    record_start(); marking('唤醒｜展开后停留6秒，观察持有态'); play('KAREN_WAKE_UP')
    for _ in range(100):
        selected_state = state()
        if selected_state.get('card_select', {}).get('screen_type') == 'simple_select': break
        time.sleep(.05)
    else: raise AssertionError(selected_state)
    time.sleep(6)
    choices = selected_state['card_select']['cards']
    marking('选中一张｜短脉冲，继续读取候选牌'); post(action='select_card', index=choices[0]['index']); time.sleep(.7)
    marking('选满两张｜幕布收向真实卡牌'); post(action='select_card', index=choices[1]['index']); time.sleep(2.8)
    assert len(state()['player']['hand']) == 5
    save('wake-probe', json.loads(cmd('wake_probe status')['message']))
    polish_done(); finish()

    for name, card, label in [('arrogant', 'KAREN_ARROGANT', '骄傲｜代价起势与激情Buff确认'),
                               ('relay', 'KAREN_FIGHT_RELAY', '接力耗尽｜通用Buff确认预约奖励')]:
        stage('polish-' + name, seed=False)
        prep(card); prep(card)
        cmd('vfx_polish watch'); record_start()
        for i in range(2):
            if name == 'relay': cmd('vfx_polish deplete ' + card)
            marking(label + ('｜首次' if i == 0 else '｜第二次'))
            play(card); time.sleep(3)
        # Relay is explicitly injected here to isolate its real exhaustion/reward path.
        # This is not a multiplayer-transfer or normal single-player generation test.
        polish_done(2); finish()


if 'suitability-checks' in wanted:
    for speed in ('fast', 'instant'):
        for card in ('KAREN_ARROGANT', 'KAREN_FIGHT_RELAY'):
            stage('polish-check-' + speed + '-' + card.lower(), seed=False)
            cmd('stage_probe speed ' + speed); prep(card)
            if card == 'KAREN_FIGHT_RELAY': cmd('vfx_polish deplete ' + card)
            cmd('vfx_polish watch'); snap(case / 'before')
            play(card); time.sleep(2.2)
            observed = polish_done(1)
            if speed == 'instant': assert observed['nodes'] == [], observed
            snap(case / 'after')
    cmd('stage_probe speed normal')


if 'suitability-wake-upgraded' in wanted:
    stage('polish-wake-upgraded', seed=False)
    cmd('wake_probe seed draw 3 upgrade'); time.sleep(2.3)
    cmd('wake_probe watch'); cmd('vfx_polish watch')
    record_start(); marking('第一段｜先选择牌堆，停留5秒'); play('KAREN_WAKE_UP')
    for _ in range(100):
        s = state()
        if s.get('card_select', {}).get('screen_type') == 'choose': break
        time.sleep(.05)
    else: raise AssertionError(s)
    assert {c['id'] for c in s['card_select']['cards']} == {
        'KAREN_WAKE_UP_DRAW_PILE_OPTION', 'KAREN_WAKE_UP_PROMISE_PILE_OPTION', 'KAREN_WAKE_UP_DISCARD_PILE_OPTION'}, s
    time.sleep(5)
    save('source-choice-state', state())
    first_probe = json.loads(cmd('wake_probe status')['message'])
    save('source-choice-probe', first_probe)
    assert first_probe['showingCurtains'] == 0 and first_probe['live'] == 0
    marking('第二段｜进入所选牌堆选两张牌，停留6秒')
    option = next(c for c in s['card_select']['cards'] if c['id'] == 'KAREN_WAKE_UP_DRAW_PILE_OPTION')
    post(action='select_card', index=option['index'])
    for _ in range(100):
        selected_state = state()
        if selected_state.get('card_select', {}).get('screen_type') == 'simple_select': break
        time.sleep(.05)
    else: raise AssertionError(selected_state)
    time.sleep(6)
    save('card-choice-state', state())
    second_probe = json.loads(cmd('wake_probe status')['message'])
    save('card-choice-probe', second_probe)
    assert second_probe['showingCurtains'] == 1 and second_probe['live'] == 0
    choices = selected_state['card_select']['cards']
    marking('第二段｜选中第一张，短脉冲'); post(action='select_card', index=choices[0]['index']); time.sleep(.7)
    marking('第二段｜选满两张，收束并回手'); post(action='select_card', index=choices[1]['index']); time.sleep(2.8)
    after = state()
    assert len(after['player']['hand']) == 5 and after['player']['exhaust_pile_count'] == 1
    save('wake-probe', json.loads(cmd('wake_probe status')['message']))
    polish_done(); finish()
