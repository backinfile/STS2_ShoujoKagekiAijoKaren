# Executed by record.py, explicitly selected with --scenes jealousy.
def jealousy_status():
    return json.loads(cmd('jealousy_probe status')['message'])


def jealousy_result(label, hp, damage, rewards, kinds):
    current = state()
    assert hp - current['battle']['enemies'][0]['hp'] == damage, current
    observed = jealousy_status()
    save(label + '-jealousy', observed)
    assert observed['relicRewards'] == rewards, observed
    assert observed['live'] == 0, observed
    assert [c['kind'] for c in observed['casts']] == kinds, observed
    for cast in observed['casts']:
        assert cast['exited'] and cast['maxAnchorError'] < .1 and cast['frames'] > 0, cast
        if cast['kind'] == 'sweep':
            assert cast['travel'] > 0 and cast['confirmed'] > 0, cast
            assert abs(cast['confirmedAt'] - .42) < .08, cast
    save(label + '-rules', dict(damage=damage, relicRewards=rewards, powers=current['player']['status']))
    return observed


if 'jealousy' in wanted:
    stage('jealousy', seed=False)
    prep('KAREN_JEALOUSY'); prep('KAREN_JEALOUSY')
    cmd('jealousy_probe upgrade-last'); cmd('jealousy_probe deplete-last'); time.sleep(.3)
    cmd('jealousy_probe watch'); record_start()
    hp = state()['battle']['enemies'][0]['hp']
    clip_cast('KAREN_JEALOUSY', '嫉妒｜青绿弧光、金色星扣与真实命中')
    jealousy_result('ordinary', hp, 15, 0, ['sweep', 'impact'])
    hp = state()['battle']['enemies'][0]['hp']
    clip_cast('KAREN_JEALOUSY', '嫉妒＋｜闪耀耗尽后确认两份遗物奖励')
    report = jealousy_result('upgraded', hp, 20, 2, ['sweep', 'impact', 'sweep', 'impact', 'reward'])
    assert report['casts'][-1]['rewardCount'] == 2, report
    finish()


if 'jealousy-checks' in wanted:
    # Targeted cases are kept out of the normal-speed presentation clip.
    for mode in ('fast', 'instant', 'pose'):
        cmd('promise_integration off'); fight(); trim()
        case = out / ('jealousy-' + mode); case.mkdir(exist_ok=True)
        cmd('promise_integration watch')
        cmd('stage_probe speed ' + ('normal' if mode == 'pose' else mode))
        if mode == 'pose': cmd('jealousy_probe pose'); time.sleep(.5)
        prep('KAREN_JEALOUSY'); cmd('jealousy_probe deplete-last'); cmd('jealousy_probe watch')
        hp = state()['battle']['enemies'][0]['hp']; snap(case / 'before')
        play('KAREN_JEALOUSY'); settle('after')
        report = jealousy_result(mode, hp, 15, 1, [] if mode == 'instant' else ['sweep', 'impact', 'reward'])
        if mode != 'instant': assert report['casts'][-1]['rewardCount'] == 1, report
        if mode == 'pose': assert report['casts'][0]['unit'] < .7, report


if 'jealousy-lethal' in wanted:
    stage('jealousy-lethal', seed=False)
    cmd('stage_probe speed normal'); prep('KAREN_JEALOUSY'); cmd('jealousy_probe lethal')
    cmd('promise_integration off'); cmd('jealousy_probe watch')
    snap(case / 'before'); play('KAREN_JEALOUSY')
    for _ in range(100):
        if state()['state_type'] == 'rewards': break
        time.sleep(.1)
    else: raise AssertionError(state())
    time.sleep(.7); snap(case / 'after')
    observed = jealousy_status(); save('lethal-jealousy', observed)
    assert observed['live'] == 0 and len(observed['casts']) == 2 and all(c['exited'] for c in observed['casts']), observed
    fight(); time.sleep(.5)
    observed = jealousy_status(); save('next-room-jealousy', observed)
    assert observed['live'] == 0, observed
    check_hand('next-room-hand'); snap(case / 'next-room')
