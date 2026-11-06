"""Run inside the existing isolated MCP recording harness with --scenes arrogant."""
def arrogant_status():
    return json.loads(cmd('arrogant_probe status')['message'])

def assert_arrogant(name, expected_casts, vulnerable, passion):
    report = arrogant_status()
    for _ in range(70):
        if report['live'] == report['back'] == 0: break
        time.sleep(.1); report = arrogant_status()
    save(name, report)
    assert report['live'] == report['back'] == 0, report
    assert report['vulnerable'] == vulnerable and report['passion'] == passion, report
    assert len(report['casts']) == expected_casts, report
    for cast in report['casts']:
        assert cast['cost'] and cast['reward'] and cast['frames'] > 2, report
        assert cast['arrivalFrames'] > 0 and cast['destinationError'] < 1, report
    return report

stage('arrogant-normal', seed=False)
prep('KAREN_ARROGANT'); prep('KAREN_ARROGANT')
cards = [c for c in state()['player']['hand'] if c['id'] == 'KAREN_ARROGANT']
cmd('upgrade ' + str(cards[-1]['index'])); time.sleep(.5)
cmd('arrogant_probe watch')
record_start()
clip_cast('KAREN_ARROGANT', '骄傲｜金扣脱离、易伤生效、登记战后遗物')
clip_cast('KAREN_ARROGANT', '骄傲＋｜相同演出，易伤减少为一层')
assert_arrogant('arrogant-results', 2, 3, 2)
marking('战斗结束｜查看已登记的两份额外遗物奖励')
cmd('promise_integration off')
cmd('kill all')
for _ in range(120):
    current = state()
    if current['state_type'] == 'rewards': break
    time.sleep(.1)
else: raise AssertionError(current)
assert sum(r['type'] == 'relic' for r in current['rewards']['items']) == 2, current
save('reward-state', current); time.sleep(1.6)
save('after-combat', arrogant_status()); record_stop()
print('RECORDED arrogant-normal', flush=True)

stage('arrogant-fast', seed=False)
prep('KAREN_ARROGANT')
cmd('stage_probe speed fast'); cmd('arrogant_probe watch')
record_start(); clip_cast('KAREN_ARROGANT', '快速模式｜金扣演出按游戏速度缩短')
assert_arrogant('arrogant-results', 1, 2, 1); finish()

stage('arrogant-instant', seed=False)
prep('KAREN_ARROGANT')
cmd('stage_probe speed instant'); cmd('arrogant_probe watch')
play('KAREN_ARROGANT'); time.sleep(1)
assert_arrogant('arrogant-results', 0, 2, 1); snap(case/'instant-after'); check_hand('instant-hand')

stage('arrogant-repeat', seed=False)
cmd('stage_probe speed normal')
for _ in range(2): prep('KAREN_ARROGANT')
cmd('arrogant_probe watch')
play('KAREN_ARROGANT'); time.sleep(.55); play('KAREN_ARROGANT')
for _ in range(120):
    report = arrogant_status()
    if report['passion'] == 2 and report['live'] == 0: break
    time.sleep(.1)
save('arrogant-results', report)
assert report['live'] == report['back'] == 0 and report['vulnerable'] == 4 and report['passion'] == 2, report
assert len(report['casts']) == 2 and all(c['cost'] and c['reward'] for c in report['casts']), report
snap(case/'repeat-after'); check_hand('repeat-hand')

stage('arrogant-end-cleanup', seed=False)
prep('KAREN_ARROGANT'); cmd('arrogant_probe watch')
play('KAREN_ARROGANT')
for _ in range(100):
    before = arrogant_status()
    if before['casts'] and before['casts'][0]['reward']: break
    time.sleep(.035)
save('before-combat-end', before)
assert before['live'] == 1 and before['casts'][0]['reward'], before
cmd('promise_integration off')
cmd('kill all'); time.sleep(2)
after = arrogant_status(); save('after-combat-end', after)
assert after['live'] == after['back'] == 0, after
cmd('stage_probe speed normal')
