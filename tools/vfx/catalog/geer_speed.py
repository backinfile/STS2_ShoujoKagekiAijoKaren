# Executed by the isolated catalog recorder. No production rule timing is replaced.
def geer_observed():
    return json.loads(cmd('geer_speed status')['message'])


def geer_wait(predicate, timeout=18):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        observed = geer_observed()
        if predicate(observed): return observed
        time.sleep(.06)
    raise AssertionError(('Geer observation timeout', observed, state()))


def geer_fixture(name, mode='normal', *, void=False, casts=1, empty=False):
    cmd('stage_probe speed normal')
    stage(name, void=void, seed=False)
    cmd(f'geer_speed seed {casts}' + (' empty' if empty else ''))
    for _ in range(100):
        if sum(c['id'] == 'KAREN_GEER' for c in state()['player']['hand']) == casts: break
        time.sleep(.08)
    else: raise AssertionError(state())
    time.sleep(1.8)
    cmd('energy 8'); cmd('bridge_vfx watch'); cmd('promise_integration watch')
    cmd('stage_probe speed ' + mode); cmd('geer_speed watch')


def geer_finish(*, expected_power=1, expected_plays=1):
    for _ in range(120):
        native = json.loads(cmd('bridge_vfx status')['message'])
        played = [e for e in native['finished'] if e['auto'] and e['id'] == 'KAREN_STRIKE']
        if len(played) == expected_plays and not state().get('card_select'): break
        time.sleep(.10)
    else: raise AssertionError(('Native auto-play incomplete', native, state()))
    # Native CardPlay finishes before the discard flight. Inspect settled hand visuals after its tail.
    time.sleep(1.1)
    observed = geer_wait(lambda s: s['live'] == s['activeSounds'] == 0)
    assert observed['power'] == expected_power and len(played) == expected_plays, observed
    assert all(n['exited'] and n['parentCorrect'] and n['frames'] > 0 for n in observed['nodes']), observed
    assert observed['offsetError'] < .001 and observed['positionError'] < .001 and observed['visualError'] < .001 and observed['zError'] == 0, observed
    save('geer-result', observed); save('geer-native', native)
    check_hand('geer-finished-hand')
    return observed


def geer_choose_once(label, *, expect_instant=False):
    for _ in range(160):
        current = state()
        if current.get('card_select', {}).get('screen_type') == 'simple_select': break
        time.sleep(.06)
    else: raise AssertionError(('Native Geer selection missing', current))
    save(label + '-selection-state', current)
    observed = geer_observed(); save(label + '-selection-geer', observed)
    if expect_instant: assert observed['live'] == observed['activeSounds'] == 0, observed
    choices = current['card_select']['cards']
    assert len(choices) >= 2 and all(c['id'] == 'KAREN_STRIKE' for c in choices), current
    time.sleep(.75)
    if recording: marking('选定真实卡牌｜移入Play并自动打出')
    post(action='select_card', index=choices[0]['index'])


is_geer_after = 'geer-speed-after' in wanted
if wanted & {'geer-speed-baseline', 'geer-speed-after'}:
    for mode in ('normal', 'fast', 'instant'):
        geer_fixture('geer-' + mode, mode)
        hp = state()['battle']['enemies'][0]['hp']
        record_start(); marking(mode.upper() + '｜真实打出命运的齿轮')
        play('KAREN_GEER'); geer_wait(lambda s: s['power'] == 1)
        time.sleep(.12 if mode == 'instant' else .24)
        save('cast-settled-geer', geer_observed())
        cmd('geer_speed phase'); marking(mode.upper() + '｜下一回合触发与原生选牌')
        post(action='end_turn')
        geer_choose_once(mode, expect_instant=is_geer_after and mode == 'instant')
        observed = geer_finish()
        assert hp - state()['battle']['enemies'][0]['hp'] == 6
        if is_geer_after and mode == 'instant': assert observed['nodes'] == [], observed
        if mode in ('normal', 'fast'):
            assert len(observed['nodes']) == 3, observed
            assert sum(n['fullScreen'] for n in observed['nodes']) == 1
        finish()

    geer_fixture('geer-switch-cast')
    cmd('geer_speed switch 0.30')
    record_start(); marking('播放中切换瞬时｜0.30秒后更改原生偏好')
    play('KAREN_GEER'); observed = geer_wait(lambda s: s['power'] == 1 and s['live'] == 0)
    assert observed['switchAt'] > 0 and len(observed['nodes']) == 2, observed
    if is_geer_after:
        assert all(n['ended'] - observed['switchAt'] < .12 for n in observed['nodes']), observed
        assert observed['activeSounds'] == 0
    save('geer-result', observed); finish()


if 'geer-speed-checks' in wanted:
    for mode in ('normal', 'fast', 'instant'):
        geer_fixture('geer-void-' + mode, mode, void=True)
        hp = state()['battle']['enemies'][0]['hp']
        snap(case / 'before'); check_hand('before-hand')
        play('KAREN_GEER'); geer_wait(lambda s: s['power'] == 1)
        cmd('geer_speed phase'); post(action='end_turn')
        geer_choose_once('void', expect_instant=mode == 'instant')
        observed = geer_finish()
        assert hp - state()['battle']['enemies'][0]['hp'] == 6
        if mode == 'instant': assert observed['nodes'] == [], observed
        snap(case / 'after'); check_hand('after-hand')

    geer_fixture('geer-empty', empty=True)
    snap(case / 'before'); play('KAREN_GEER'); geer_wait(lambda s: s['power'] == 1)
    cmd('geer_speed phase'); post(action='end_turn')
    for _ in range(160):
        current = state()
        if current.get('battle', {}).get('is_play_phase') and current.get('battle', {}).get('round', 0) >= 2: break
        time.sleep(.08)
    else:
        # MCP versions expose the current round on different state paths; history is definitive.
        current = state(); assert current.get('battle', {}).get('is_play_phase') and not current.get('card_select'), current
    time.sleep(.5)
    observed = geer_finish(expected_plays=0)
    assert len(observed['nodes']) == 2 and observed['selectionAt'] < 0, observed
    snap(case / 'after'); check_hand('after-hand')

    geer_fixture('geer-stacked', casts=2)
    hp = state()['battle']['enemies'][0]['hp']
    snap(case / 'before')
    play('KAREN_GEER'); geer_wait(lambda s: s['power'] == 1)
    play('KAREN_GEER'); geer_wait(lambda s: s['power'] == 2)
    cmd('geer_speed phase'); post(action='end_turn')
    geer_choose_once('stack-1')
    for _ in range(100):
        native = json.loads(cmd('bridge_vfx status')['message'])
        if any(e['auto'] for e in native['finished']): break
        time.sleep(.08)
    geer_choose_once('stack-2')
    observed = geer_finish(expected_power=2, expected_plays=2)
    assert hp - state()['battle']['enemies'][0]['hp'] == 12 and len(observed['nodes']) == 6, observed
    snap(case / 'after'); check_hand('after-hand')

    geer_fixture('geer-deferred')
    snap(case / 'before'); cmd('geer_speed deferred'); time.sleep(.2)
    observed = geer_observed(); save('geer-result', observed)
    assert observed['nodes'] == [] and observed['activeSounds'] == 0, observed
    time.sleep(1.1); snap(case / 'after'); check_hand('after-hand')

    geer_fixture('geer-interrupt')
    snap(case / 'before'); play('KAREN_GEER'); geer_wait(lambda s: s['live'] == 2)
    cmd('geer_speed cancel'); observed = geer_wait(lambda s: s['live'] == 0 and s['power'] == 1)
    save('geer-result', observed); assert observed['activeSounds'] == 0
    snap(case / 'after'); check_hand('after-hand')

    geer_fixture('geer-leave')
    snap(case / 'before'); play('KAREN_GEER'); geer_wait(lambda s: s['live'] == 2)
    cmd('fight WATERFALL_GIANT_BOSS'); time.sleep(2.5)
    observed = geer_observed(); save('geer-result', observed)
    assert observed['live'] == observed['activeSounds'] == 0 and all(n['exited'] for n in observed['nodes']), observed
    snap(case / 'after'); check_hand('after-hand')

cmd('stage_probe speed normal')
