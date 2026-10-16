# Executed by record.py, explicitly selected with --scenes jealousy.
def jealousy_status():
    return json.loads(cmd('jealousy_probe status')['message'])


def jealousy_result(label, hp, damage, rewards, kinds):
    # Native replays include their own scheduling gap. Wait for the last real cast's particles.
    for _ in range(80):
        observed = jealousy_status()
        if observed['live'] == 0 and len(observed['casts']) == len(kinds): break
        time.sleep(.05)
    current = state()
    assert hp - current['battle']['enemies'][0]['hp'] == damage, current
    if label == 'replay':
        snap(case / 'visual-finished')
        check_hand('visual-finished-hand')
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
        assert cast['emitters'] == (1 if cast['kind'] == 'sweep' else 2), cast
        assert cast['particleCapacity'] == (12 if cast['kind'] == 'sweep' else 48), cast
        assert cast['emittingFrames'] > 0, cast
    assert observed['distinctParticleMaterials'] == sum(c['emitters'] for c in observed['casts']), observed
    assert observed['maxLiveParticleCapacity'] <= 108, observed
    if label == 'overlap':
        assert observed['maxLiveNodes'] == 2 and observed['maxLiveParticleCapacity'] == 96, observed
    save(label + '-rules', dict(damage=damage, relicRewards=rewards, powers=current['player']['status']))
    print(branch, 'CHECKED jealousy', label, flush=True)
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


if 'jealousy-checks' in wanted or 'jealousy-replay-check' in wanted:
    # Targeted cases are kept out of the normal-speed presentation clip.
    modes = ('fast', 'instant', 'pose', 'cancel', 'cancel-impact', 'replay', 'overlap') if 'jealousy-checks' in wanted else ('replay',)
    for mode in modes:
        cmd('promise_integration off'); fight(); trim()
        case = out / ('jealousy-' + mode); case.mkdir(exist_ok=True)
        cmd('promise_integration watch')
        cmd('stage_probe speed ' + (mode if mode in ('fast', 'instant') else 'normal'))
        if mode == 'pose': cmd('jealousy_probe pose'); time.sleep(.5)
        prep('KAREN_JEALOUSY')
        if mode != 'overlap':
            cmd('jealousy_probe replay-last' if mode == 'replay' else 'jealousy_probe deplete-last')
        cmd('jealousy_probe watch')
        hp = state()['battle']['enemies'][0]['hp']; snap(case / 'before')
        if mode == 'overlap': cmd('jealousy_probe overlap')
        else: play('KAREN_JEALOUSY')
        if mode == 'cancel': time.sleep(.3); cmd('jealousy_probe cancel')
        if mode == 'cancel-impact': time.sleep(.62); cmd('jealousy_probe cancel-all')
        settle('after')
        kinds = [] if mode == 'instant' else ['sweep', 'impact'] * (2 if mode == 'replay' else 1)
        if mode == 'overlap': kinds = ['impact', 'impact']
        damage = 0 if mode == 'overlap' else 30 if mode == 'replay' else 15
        report = jealousy_result(mode, hp, damage, 0 if mode in ('replay', 'overlap') else 1, kinds)
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
