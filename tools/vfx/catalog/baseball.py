# Executed by record.py, explicitly selected with --scenes baseball.
RETENTION = {'RETAIN_HAND_POWER', 'KAREN_RETAIN_ENERGY_POWER', 'BLUR_POWER', 'KAREN_RETAIN_TMP_STRENGTH_POWER'}

def baseball_result(label, before_hp, damage, stacks, visual_count=1):
    current = state()
    assert before_hp - current['battle']['enemies'][0]['hp'] == damage, current
    powers = {p['id']: p['amount'] for p in current['player']['status']}
    assert all(powers.get(p) == stacks for p in RETENTION), powers
    observed = json.loads(cmd('baseball_probe status')['message'])
    save(label + '-baseball', observed)
    assert observed['live'] == 0, observed
    assert len(observed['casts']) == visual_count * 2, observed
    for cast in observed['casts']:
        assert cast['exited'] and cast['maxAnchorError'] < .1 and cast['frames'] > 0, cast
        if not cast['impact']:
            assert cast['windup'] > 0 and cast['flight'] > 0 and cast['confirmed'] > 0, cast
            assert abs(cast['confirmedAt'] - .35) < .075, cast
        else:
            assert .42 <= cast['finalAge'] <= .51, cast
    save(label + '-rules', dict(damage=damage, powers=powers))

stage('baseball', seed=False)
prep('KAREN_BASEBALL'); prep('KAREN_BASEBALL')
cmd('stage_probe upgrade KAREN_BASEBALL'); time.sleep(.3)
# Upgrade helper chooses the first matching card; show it first, then the ordinary copy.
cmd('baseball_probe watch'); record_start()
hp = state()['battle']['enemies'][0]['hp']
clip_cast('KAREN_BASEBALL', '棒球＋｜蓄势、缝线球飞行、真实命中')
baseball_result('upgraded', hp, 32, 2)
hp = state()['battle']['enemies'][0]['hp']
clip_cast('KAREN_BASEBALL', '棒球｜同款演出，保留原有 24 点伤害')
baseball_result('ordinary', hp, 24, 4, visual_count=2)
finish()

# Targeted checks stay out of the normal-trigger preview video.
for mode in ('fast', 'instant', 'pose'):
    stage('baseball-' + mode, seed=False)
    cmd('stage_probe speed ' + ('normal' if mode == 'pose' else mode))
    if mode == 'pose': cmd('baseball_probe pose'); time.sleep(.5)
    prep('KAREN_BASEBALL'); cmd('baseball_probe watch')
    hp = state()['battle']['enemies'][0]['hp']; snap(case / 'before')
    play('KAREN_BASEBALL'); settle('after')
    baseball_result(mode, hp, 24, 2, visual_count=0 if mode == 'instant' else 1)
    if mode == 'pose':
        probe = json.loads(cmd('baseball_probe status')['message'])
        assert probe['casts'][0]['unit'] < .7, probe

stage('baseball-lethal', seed=False)
cmd('stage_probe speed normal'); prep('KAREN_BASEBALL'); cmd('baseball_probe lethal')
# The transfer observer expects a live tower; combat end intentionally removes it.
cmd('promise_integration off')
cmd('baseball_probe watch'); snap(case / 'before'); play('KAREN_BASEBALL')
for _ in range(100):
    if state()['state_type'] == 'rewards': break
    time.sleep(.1)
else: raise AssertionError(state())
time.sleep(.7); snap(case / 'after')
observed = json.loads(cmd('baseball_probe status')['message']); save('lethal-baseball', observed)
assert observed['live'] == 0 and len(observed['casts']) == 2 and all(c['exited'] for c in observed['casts']), observed
cmd('stage_probe speed normal')
# A new room also has no surviving flight/impact nodes.
fight(); time.sleep(.5)
observed = json.loads(cmd('baseball_probe status')['message']); save('next-room-baseball', observed)
assert observed['live'] == 0, observed
check_hand('next-room-hand'); snap(case / 'next-room')
