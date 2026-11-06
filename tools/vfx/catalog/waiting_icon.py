"""Real casts/draws: patrol heads must follow the visible energy texture alpha edge."""
stage('waiting-icon', seed=False)
cmd('waiting_probe watch'); cmd('waiting_probe fill 1'); settle('seeded')
prep('KAREN_STAGE_IS_WAITING'); prep('KAREN_PROMISE_DRAW')
snap(case/'before-cast')
record_start(); marking('沿真实能量图标边缘创建光轨')
play('KAREN_STAGE_IS_WAITING'); time.sleep(5)
occupied = json.loads(cmd('waiting_probe status')['message']); save('occupied', occupied)
finish()
snap(case/'occupied-icon')
assert occupied['casts'][0]['iconEdgeError'] < 2, occupied['casts'][0]
before = state()['player']['energy']
play('KAREN_PROMISE_DRAW'); settle('after-empty')
assert state()['player']['energy'] == before + 1
for layers in (2, 3):
    prep('KAREN_STAGE_IS_WAITING'); play('KAREN_STAGE_IS_WAITING'); time.sleep(1.8)
    save('layers-' + str(layers), json.loads(cmd('waiting_probe status')['message']))
cmd('waiting_probe ui-pose'); time.sleep(2)
save('counter-pose', json.loads(cmd('waiting_probe status')['message']))
snap(case/'transformed-icon'); check_hand('transformed-hand')
cmd('waiting_probe icon-pose'); time.sleep(2)
save('face-pose', json.loads(cmd('waiting_probe status')['message']))
snap(case/'face-transformed-icon')
cmd('waiting_probe icon-reset')
cmd('waiting_probe ui-reset'); time.sleep(.5)
report = json.loads(cmd('waiting_probe status')['message']); save('icon-result', report)
assert report['casts'][0]['confirmations'] == 1 and report['casts'][0]['headsNow'] == 3, report
assert report['casts'][0]['anchorError'] < .1 and report['casts'][0]['ringError'] < .1, report
assert report['casts'][0]['countMatches'] and report['casts'][0]['belowDigits'], report
assert not report['casts'][0]['ringVisible'], report
assert report['casts'][0]['unexpectedGlints'] == 0 and report['casts'][0]['fadedGlints'] > 0, report
cmd('waiting_probe remove'); time.sleep(.3)
removed = json.loads(cmd('waiting_probe status')['message']); save('removed', removed)
assert removed['live'] == removed['hud'] == removed['liveSounds'] == 0, removed
assert report['casts'][0]['iconEdgeError'] < 2, report['casts'][0]

for mode in (() if 'waiting-icon-preview' in wanted else ('fast', 'instant')):
    stage('waiting-icon-' + mode, seed=False)
    cmd('stage_probe speed ' + mode); cmd('waiting_probe watch')
    cmd('waiting_probe fill 1'); settle('seeded')
    prep('KAREN_STAGE_IS_WAITING'); prep('KAREN_PROMISE_DRAW')
    snap(case/'before-cast'); play('KAREN_STAGE_IS_WAITING'); time.sleep(3)
    before = state()['player']['energy']
    snap(case/'before-empty'); play('KAREN_PROMISE_DRAW'); settle('after-empty')
    assert state()['player']['energy'] == before + 1
    result = json.loads(cmd('waiting_probe status')['message']); save('icon-result', result)
    assert result['live'] == (1 if mode == 'fast' else 0), result
    if mode == 'fast':
        node = result['casts'][0]
        assert node['iconEdgeError'] < 2 and node['confirmations'] == 1 and node['countMatches'], node
        assert not node['ringVisible'], node
        assert node['anchorError'] < .1 and node['ringError'] < .1, node
    cmd('waiting_probe remove'); time.sleep(.3)
    result = json.loads(cmd('waiting_probe status')['message']); save('removed', result)
    assert result['live'] == result['hud'] == result['liveSounds'] == 0, result
cmd('stage_probe speed normal')
