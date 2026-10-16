# Targeted fixture: genuine stacked casts and actual final-card draws; no synthetic VFX confirmation.
def observed(label):
    value = json.loads(cmd('waiting_probe status')['message'])
    save(label + '-waiting', value)
    for node in value['casts']:
        assert node['anchorError'] < .1 and node['hudCorrect'] and node['belowDigits'], node
        assert node['countMatches'], node
        assert node['peakDeliveries'] <= 8 and node['peakBursts'] <= 12, node
        assert node['hexHeadError'] < .1 and node['hexCanvasError'] < .1, node
        assert node['ringError'] < .1 and abs(node['ringWidth'] - 240) < .1 and node['peakStage'] <= .61 and not node['feedbackEarly'], node
        if node['firstFeedbackAge'] >= 0: assert node['firstFeedbackAge'] >= .16, node
        if node['casts'] > 0: assert node['peakCast'] > .9, node
        if node['confirmations'] > 0: assert node['peakTrigger'] > .9, node
    return value

def pile_pose(label, empty, count=None):
    value = observed(label)
    node = value['casts'][-1]
    assert node['pileEmpty'] == empty, node
    assert abs(node['opening'] - int(empty)) < .01, node
    if count is not None: assert node['pileCount'] == count, node
    assert abs(node['radius'] - 62) < .1 and abs(node['shaderOpening'] - int(empty)) < .01 and node['buildNow'] > .99 and node['uiVisible'] and node['headsNow'] == node['powerLayers'] and node['countMatches'], node
    assert abs(node['stageNow'] - (.22 if empty else .40)) < .001, node
    assert all(abs(x - (.72 if empty else 1.15)) < .01 for x in node['headScales']), node
    assert all(abs(x - (.28 if empty else 1)) < .01 for x in node['headStrengths']), node
    return node

def waiting_cast(upgrade=False):
    prep('KAREN_STAGE_IS_WAITING')
    if upgrade: cmd('stage_probe upgrade KAREN_STAGE_IS_WAITING'); time.sleep(.25)
    play('KAREN_STAGE_IS_WAITING'); time.sleep(3.8)

def drain_real(count, label):
    prep('KAREN_PROMISE_DRAW')
    before = state()['player']['energy']
    play('KAREN_PROMISE_DRAW'); settle(label)
    return state()['player']['energy'] - before

if not wanted.intersection({'waiting-stage-tail', 'waiting-stage-void'}):
    stage('waiting-stage', seed=False)
    cmd('stage_probe speed normal'); cmd('waiting_probe watch')
    prep('KAREN_STAGE_IS_WAITING'); prep('KAREN_PROMISE_DRAW'); prep('KAREN_PROMISE_DRAW')
    cmd('waiting_probe fill 1'); settle('seeded')
    record_start(); marking('打出能力｜单光抵达、沿圆角边缘创建')
    play('KAREN_STAGE_IS_WAITING'); time.sleep(4.6)
    assert observed('cast')['casts'][0]['confirmations'] == 0
    quiet = observed('cast-quiet')['casts'][0]
    assert quiet['peakStage'] > .5 and .399 < quiet['stageNow'] < .401 and quiet['minBuild'] < .1 and quiet['maxBuild'] > .99 and quiet['partialFrames'] > 4 and quiet['peakCreationHeads'] == 1 and quiet['headsNow'] == 1, quiet
    assert quiet['creationStarts'] == 1 and quiet['orbitTravel'] > .5 and quiet['hexSidesSeen'] == 63, quiet
    pile_pose('occupied-idle', False, 1)
    marking('有牌 · 候场｜1 层 · 1 枚放大光点贴边巡游')
    time.sleep(1.8)
    marking('有牌 → 清空｜实际回能，从环绕向槽内收光')
    before = state()['player']['energy']; play('KAREN_PROMISE_DRAW'); settle('trigger-one')
    assert state()['player']['energy'] == before + 1
    assert observed('trigger-one')['casts'][0]['confirmations'] == 1
    pile_pose('empty-idle', True, 0)
    marking('空堆 · 待续｜1 层 · 1 枚光点减弱，数量不变')
    time.sleep(2)
    marking('空堆时叠层｜保留环绕主体，强化扫光')
    waiting_cast(upgrade=True)
    assert observed('stack')['live'] == 1 and observed('stack-count')['casts'][0]['casts'] == 2
    assert observed('stack-keeps-orbit')['casts'][0]['creationStarts'] == 1
    pile_pose('stack-empty-idle', True, 0)
    old_energy = state()['player']['energy']
    old_sounds = len(observed('before-refill')['casts'][0]['audioEvents'])
    marking('空堆 → 补牌｜2 层 · 2 枚光点放大，不重复回能')
    cmd('waiting_probe fill 1'); time.sleep(2.2)
    refilled = pile_pose('refilled-idle', False, 1)
    assert state()['player']['energy'] == old_energy
    assert len(refilled['audioEvents']) == old_sounds, refilled
    marking('再次清空｜环绕收光，实际获得 2 点能量')
    before = state()['player']['energy']; play('KAREN_PROMISE_DRAW'); settle('trigger-two')
    assert state()['player']['energy'] == before + 2
    assert observed('trigger-two')['casts'][0]['confirmations'] == 2
    pile_pose('second-empty-idle', True, 0)
    if 'waiting-stage-preview' in wanted:
        marking('空堆叠至 3 层｜3 枚弱光点')
        waiting_cast()
        three = pile_pose('three-empty-idle', True, 0)
        assert three['powerLayers'] == three['headsNow'] == 3, three
        marking('3 层有牌｜3 枚光点一起放大')
        cmd('waiting_probe fill 1'); time.sleep(2.2)
        three = pile_pose('three-occupied-idle', False, 1)
        assert three['powerLayers'] == three['headsNow'] == 3, three
        marking('3 层清空｜数量不变，光点变弱')
        before = state()['player']['energy']; prep('KAREN_PROMISE_DRAW'); play('KAREN_PROMISE_DRAW'); settle('trigger-three')
        assert state()['player']['energy'] == before + 3
        assert pile_pose('third-empty-idle', True, 0)['headsNow'] == 3
        marking('能力减至 2 层｜光点同步减至 2 枚')
        cmd('waiting_probe reduce'); time.sleep(1.6)
        reduced = pile_pose('reduced-empty-idle', True, 0)
        assert reduced['powerLayers'] == reduced['headsNow'] == 2, reduced
    time.sleep(1.8); finish()

    if 'waiting-stage-preview' in wanted:
        cmd('waiting_probe ui-pose'); time.sleep(.4); snap(case/'hex-ui-transformed'); observed('hex-ui-transformed')
        cmd('waiting_probe ui-hide'); time.sleep(.2); assert not observed('hex-ui-hidden')['casts'][-1]['uiVisible']
        cmd('waiting_probe ui-show'); cmd('waiting_probe ui-reset'); time.sleep(.3); pile_pose('hex-ui-restored', True, 0)
        cmd('waiting_probe remove'); time.sleep(.3)
        assert all(observed('remove')[k] == 0 for k in ('live', 'back', 'hud', 'liveSounds'))
        stage('waiting-stage-hex-fast', seed=False)
        cmd('stage_probe speed fast'); cmd('waiting_probe watch'); cmd('waiting_probe fill 1')
        waiting_cast()
        fast = observed('fast-created')['casts'][0]
        assert fast['hexSidesSeen'] == 63 and fast['minBuild'] < .1 and fast['maxBuild'] > .99, fast
        assert drain_real(1, 'fast-gain') == 1
        assert observed('fast-gain')['casts'][0]['confirmations'] == 1
        cmd('waiting_probe remove'); time.sleep(.3)
        assert all(observed('fast-removed')[k] == 0 for k in ('live', 'back', 'hud', 'liveSounds'))
        cmd('stage_probe speed normal')
    else:
        # No trigger on already empty, on an unrelated gain, or a non-final removal.
        assert drain_real(1, 'already-empty') == 0
        assert observed('already-empty')['casts'][0]['confirmations'] == 2
        cmd('energy 5'); time.sleep(.3)
        assert observed('unrelated-energy')['casts'][0]['confirmations'] == 2
        cmd('waiting_probe fill 3'); settle('three-seeded')
        assert drain_real(1, 'not-final') == 0
        assert observed('not-final')['casts'][0]['confirmations'] == 2
        pile_pose('nonfinal-still-waiting', False, 1)
        assert drain_real(1, 'final') == 2
        assert observed('final')['casts'][0]['confirmations'] == 3

        # Restore while occupied; lifecycle separately covers restoration while empty.
        cmd('waiting_probe fill 1'); settle('before-occupied-restore')
        pile_pose('occupied-before-restore', False, 1)
        # Layer ownership, interruption, quiet rebuild and presentation removal.
        cmd('waiting_probe pose'); time.sleep(.4); snap(case/'transformed')
        pose = observed('transformed'); assert abs(pose['casts'][0]['unit'] - 1) < .01, pose
        cmd('waiting_probe ui-pose'); time.sleep(.4); snap(case/'ui-transformed'); observed('ui-transformed')
        cmd('waiting_probe ui-hide'); time.sleep(.2); assert not observed('ui-hidden')['casts'][-1]['uiVisible']
        cmd('waiting_probe ui-show'); cmd('waiting_probe ui-reset'); time.sleep(.3); pile_pose('ui-restored', False, 1)
        cmd('waiting_probe cancel'); time.sleep(.2)
        assert all(observed('cancel')[k] == 0 for k in ('live', 'back', 'hud'))
        cmd('waiting_probe restore'); time.sleep(.5)
        assert observed('quiet-restore')['casts'][-1]['casts'] == 0
        pile_pose('quiet-restore-occupied', False, 1)
        cmd('waiting_probe remove'); time.sleep(.3)
        assert all(observed('remove')[k] == 0 for k in ('live', 'back', 'hud'))

        for mode in ('fast', 'instant'):
            stage('waiting-stage-' + mode, seed=False)
            cmd('stage_probe speed ' + mode); cmd('waiting_probe watch')
            before = state()['player']['energy']; waiting_cast()
            assert state()['player']['energy'] == before - 2
            assert observed('applied')['live'] == (0 if mode == 'instant' else 1)
            cmd('waiting_probe fill 1'); settle('before-empty')
            assert drain_real(1, 'mode-final') == 1
            node = observed('mode-final')
            assert node['live'] == (0 if mode == 'instant' else 1), node
            if mode == 'fast': assert node['casts'][0]['confirmations'] == 1
            cmd('waiting_probe remove'); time.sleep(.3)
            assert all(observed('removed')[k] == 0 for k in ('live', 'back', 'hud'))

        stage('waiting-stage-infinite', seed=False)
        cmd('stage_probe speed normal'); cmd('waiting_probe watch'); waiting_cast()
        cmd('waiting_probe fill 1'); settle('infinite-seeded')
        before_infinite = pile_pose('infinite-before', False, 1)['stateChanges']
        cast('KAREN_STAGE_REPRODUCE'); before = state()['player']['energy']
        play_card = 'KAREN_PROMISE_DRAW'; prep(play_card); play(play_card); settle('infinite-draw')
        assert state()['player']['energy'] == before
        infinite_node = pile_pose('infinite', False, 10)
        assert infinite_node['confirmations'] == 0 and infinite_node['stateChanges'] == before_infinite, infinite_node


if not wanted.intersection({'waiting-stage-void', 'waiting-stage-preview'}):
    stage('waiting-stage-rapid', seed=False)
    cmd('stage_probe speed fast'); cmd('waiting_probe watch'); waiting_cast()
    before = state()['player']['energy']; cmd('waiting_probe rapid')
    for _ in range(160):
        rapid = json.loads(cmd('waiting_probe status')['message'])
        if rapid['rapidDone'] == 8: break
        time.sleep(.1)
    else: raise AssertionError(rapid)
    settle('rapid-settled'); rapid = observed('rapid-settled')
    assert state()['player']['energy'] == before + 8
    assert rapid['live'] == 1 and rapid['casts'][0]['confirmations'] == 8, rapid
    assert rapid['casts'][0]['peakDeliveries'] > 1, rapid
    cmd('waiting_probe remove'); time.sleep(.3)
    assert all(observed('rapid-removed')[k] == 0 for k in ('live', 'back', 'hud'))


if 'waiting-stage-preview' not in wanted:
    stage('waiting-stage-void', void=True, seed=False)
    cmd('stage_probe speed normal')
    cmd('waiting_probe watch'); waiting_cast()
    # Void redirects promise draws to Draw; retain one actual draw card, then play a real draw attack.
    cmd('waiting_probe void-one'); settle('void-one')
    assert drain_real(1, 'void-final') == 1
    assert observed('void-final')['casts'][0]['confirmations'] == 1
    pile_pose('void-empty-idle', True, 0)
    check_hand('void-hand'); snap(case/'void-ambient')

    # Leaving the room during a cast must take all sibling layers and sounds with it.
    cmd('promise_integration off'); fight(); time.sleep(.7)
    assert all(observed('next-room')[k] == 0 for k in ('live', 'back', 'hud'))
    cmd('stage_probe speed normal')
