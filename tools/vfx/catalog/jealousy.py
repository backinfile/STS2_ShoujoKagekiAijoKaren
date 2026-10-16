# Executed by record.py, explicitly selected with --scenes jealousy.
def jealousy_status():
    return json.loads(cmd('jealousy_probe status')['message'])


def jealousy_result(label, hp, damage, rewards, kinds):
    current = state()
    assert hp - current['battle']['enemies'][0]['hp'] == damage, current
    observed = jealousy_status()
    save(label + '-jealousy', observed)
    assert observed['relicRewards'] == rewards, observed
    assert observed['live'] == 0 and observed['actorOffsetError'] < .001 and observed['modelPositionError'] < .001, observed
    assert observed['actorZError'] == 0, observed
    assert all(observed[k] < .001 for k in ('maxActorOffsetError', 'maxModelPositionError', 'maxVisualPositionError')), observed
    assert observed['maxActorZError'] == 0, observed
    assert [c['kind'] for c in observed['casts']] == kinds, observed
    for cast in observed['casts']:
        assert cast['exited'] and cast['maxAnchorError'] < .1 and cast['frames'] > 0, cast
        if cast['kind'] == 'sweep' and label != 'cancel':
            assert cast['preHitFrames'] > 0 and cast['confirmed'] > 0, cast
            assert abs(cast['confirmedAt'] - .5) < .08, cast
        assert cast['ghosts'] == 0, cast
    save(label + '-rules', dict(damage=damage, relicRewards=rewards, powers=current['player']['status']))
    return observed


if 'jealousy' in wanted:
    stage('jealousy', seed=False)
    prep('KAREN_JEALOUSY'); prep('KAREN_JEALOUSY')
    cmd('jealousy_probe upgrade-last'); time.sleep(.3)
    cmd('jealousy_probe watch'); record_start()
    hp = state()['battle']['enemies'][0]['hp']
    clip_cast('KAREN_JEALOUSY', '嫉妒｜角色原位、方片掠击、命中碎闪')
    jealousy_result('ordinary', hp, 15, 0, ['sweep', 'impact'])
    hp = state()['battle']['enemies'][0]['hp']
    clip_cast('KAREN_JEALOUSY', '嫉妒＋｜同一方片攻击，保留20点伤害')
    report = jealousy_result('upgraded', hp, 20, 0, ['sweep', 'impact', 'sweep', 'impact'])
    finish()


if 'jealousy-checks' in wanted:
    # Targeted cases are kept out of the normal-speed presentation clip.
    for mode in ('fast', 'instant', 'pose', 'cancel'):
        cmd('promise_integration off'); fight(); trim()
        case = out / ('jealousy-' + mode); case.mkdir(exist_ok=True)
        cmd('promise_integration watch')
        cmd('stage_probe speed ' + ('normal' if mode in ('pose', 'cancel') else mode))
        if mode == 'pose': cmd('jealousy_probe pose'); time.sleep(.5)
        prep('KAREN_JEALOUSY'); cmd('jealousy_probe deplete-last'); cmd('jealousy_probe watch')
        hp = state()['battle']['enemies'][0]['hp']; snap(case / 'before')
        play('KAREN_JEALOUSY')
        if mode == 'cancel': time.sleep(.3); cmd('jealousy_probe cancel')
        settle('after')
        report = jealousy_result(mode, hp, 15, 1, [] if mode == 'instant' else ['sweep', 'impact'])
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
    assert observed['actorOffsetError'] < .001 and observed['modelPositionError'] < .001, observed
    assert observed['actorZError'] == 0, observed
    assert all(observed[k] < .001 for k in ('maxActorOffsetError', 'maxModelPositionError', 'maxVisualPositionError')), observed
    assert observed['maxActorZError'] == 0, observed
    assert observed['live'] == 0 and len(observed['casts']) == 2 and all(c['exited'] for c in observed['casts']), observed
    fight(); time.sleep(.5)
    observed = jealousy_status(); save('next-room-jealousy', observed)
    assert observed['live'] == 0 and observed['actorOffsetError'] < .001 and observed['modelPositionError'] < .001, observed
    check_hand('next-room-hand'); snap(case / 'next-room')
