# Recent main effects on top of the four older ambient systems, in both pile modes.
def recent_idle(label):
    for _ in range(100):
        report = composition()
        # Position 0 intentionally remains registered while its real Power exists.
        if report['registered'] == 1 and abs(report['eventWeight'] - 1) < .001:
            save(label + '-ready', report)
            return
        time.sleep(.1)
    raise AssertionError(('Transient cue survived', report))


def selection_state(kind):
    for _ in range(100):
        current = state()
        if current['state_type'] == kind:
            return current
        time.sleep(.1)
    raise AssertionError(('Selection not ready', kind, current))


for void in (False, True):
    name = 'recent-stack-void' if void else 'recent-stack-normal'
    if name not in wanted:
        continue
    stage(name, void, seed=False)
    for card in ['KAREN_BURN', 'KAREN_PAST_AND_FUTURE', 'KAREN_FORM', 'KAREN_STAGE_REPRODUCE']:
        cmd('energy 20'); cast(card); effects_idle('setup-' + card)
    cmd('energy 20'); cast('KAREN_POSITION0'); recent_idle('position')
    record_start()
    for card, title in [
        ('KAREN_PASSION', '激情｜全屏粒子、能力图标与闪耀耗尽'),
        ('KAREN_ARROGANT', '骄傲｜易伤与奖励确认'),
        ('KAREN_JEALOUSY', '嫉妒｜接触箔片、Position 0 格挡'),
        ('KAREN_BACK_TO_BACK_CARD', '背靠背｜库存双剑'),
        ('KAREN_SHINE_TOGETHER', '难以忘怀的闪耀｜挥斩与真实命中')]:
        cmd('energy 20'); prep(card); clip_cast(card, title); recent_idle(card)
    record_stop()
    save('attacks-finished', json.loads(cmd('bridge_vfx status')['message']))

    trim()
    for _ in range(4): prep('KAREN_DEFEND')
    prep('KAREN_WHY'); snap(case / 'why-before'); play('KAREN_WHY')
    current = selection_state('hand_select'); time.sleep(.7); snap(case / 'why-selection')
    for card in current['hand_select']['cards'][:2]:
        post(action='combat_select_card', card_index=card['index'])
    if state()['state_type'] == 'hand_select': post(action='combat_confirm_selection')
    settle('why-after'); recent_idle('why')

    trim()
    for _ in range(4): cmd('card KAREN_DEFEND Draw'); time.sleep(.12)
    prep('KAREN_WAKE_UP'); snap(case / 'wakeup-before'); play('KAREN_WAKE_UP')
    current = selection_state('card_select'); time.sleep(.7); snap(case / 'wakeup-selection')
    for card in current['card_select']['cards'][:2]:
        post(action='select_card', index=card['index']); time.sleep(.2)
    if state()['state_type'] == 'card_select': post(action='confirm_selection')
    settle('wakeup-after'); recent_idle('wakeup')

    trim(); cmd('showcase seed KAREN_DEFEND 3'); time.sleep(.8)
    prep('KAREN_STRIKE'); prep('KAREN_DEFEND'); prep('KAREN_EXCHANGE_FATE')
    snap(case / 'exchange-before'); play('KAREN_EXCHANGE_FATE'); time.sleep(.1); snap(case / 'exchange-during')
    # Reproduction can refill ten cards while the silk has already finished.
    # Wait for the actual native card play before taking the final screenshot or leaving.
    for _ in range(300):
        observed = json.loads(cmd('bridge_vfx status')['message'])
        if any(row['id'] == 'KAREN_EXCHANGE_FATE' for row in observed['finished']):
            break
        time.sleep(.1)
    else:
        raise AssertionError(('Exchange play did not finish', observed))
    settle('exchange-after'); recent_idle('exchange')
    save('native-finished', observed)
    cmd('promise_integration off'); fight(); trim(); cmd('promise_integration watch')
    effects_idle('room-exit')
    save('final-state', integration()); check_hand('final-hand'); snap(case / 'next-room')
    print('RECORDED', name, flush=True)
