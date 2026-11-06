# Executed inside record.py; native plays plus small regression probes.
if want('compatibility-regressions'):
    stage('compatibility-regressions', seed=False)
    checks = {}
    checks['audio'] = json.loads(cmd('vfx_review audio')['message'])
    checks['cardCache'] = json.loads(cmd('vfx_review card-cache')['message'])
    save('regressions', checks)
    # Keep all observed failures together, so a first failure does not mask another.
    failures = []
    for row in checks['audio']:
        if any(abs(row[key] - .4) > .001 for key in ('direct', 'ordinary', 'loop')):
            failures.append(('double-volume-scaling', row))
        assert row['buses'] == ['SFX'] * 3, row
    if not all(checks['cardCache'].values()):
        failures.append(('pooled-delivery-target', checks['cardCache']))
    time.sleep(.3)
    check_hand('regression-hand')
    snap(case/'regression-after')
    assert not failures, failures
    record_start()
    prep('KAREN_TICKET'); clip_cast('KAREN_TICKET', '兼容回归｜门票及真实回合生成牌')
    next_turn('ticket-return')
    effects_idle('finished')
    finish()
