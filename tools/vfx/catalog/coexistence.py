# Executed inside record.py. Keep the failing checks together for the red/green report.
stage('coexistence-regressions', seed=False)
effects_idle('before')
checks = {'composition': json.loads(cmd('vfx_coexist composition')['message']),
          'powerAnchor': json.loads(cmd('vfx_coexist power-anchor')['message'])}
cmd('vfx_coexist music')
for _ in range(100):
    music = json.loads(cmd('vfx_coexist status')['message'])
    if not music['running']:
        break
    time.sleep(.1)
else:
    raise AssertionError(('Music regression did not finish', music))
checks['music'] = music['checks']
assert len(checks['composition']) == 11 and len(checks['music']) == 9 and len(checks['powerAnchor']) == 10, checks
save('regressions', checks)
snap(case / 'regression-after')
check_hand('regression-hand')
failures = [(group, key) for group, values in checks.items() for key, passed in values.items() if not passed]
assert not failures, failures
record_start()
prep('KAREN_FORM'); clip_cast('KAREN_FORM', '共存回归｜觉醒常驻与独立音乐')
prep('KAREN_STARLIGHT02_CARD'); clip_cast('KAREN_STARLIGHT02_CARD', '共存回归｜第二幕与常驻层')
prep('KAREN_SHINE_TOGETHER'); clip_cast('KAREN_SHINE_TOGETHER', '共存回归｜难以忘怀的闪耀、真实命中与耗尽')
effects_idle('finished')
finish()
