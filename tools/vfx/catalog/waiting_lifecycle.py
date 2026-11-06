# Interrupt a genuine gain while its light flow, short particles and audio still exist.
stage('waiting-stage-lifecycle', seed=False)
cmd('stage_probe speed normal'); cmd('waiting_probe watch')
prep('KAREN_STAGE_IS_WAITING'); play('KAREN_STAGE_IS_WAITING'); time.sleep(2)
cmd('waiting_probe fill 1'); settle('seeded')
cmd('waiting_probe cancel-on-confirm'); prep('KAREN_PROMISE_DRAW')
before = state()['player']['energy']; play('KAREN_PROMISE_DRAW'); settle('interrupted')
assert state()['player']['energy'] == before + 1
def lifecycle_status(label):
    value = json.loads(cmd('waiting_probe status')['message']); save(label, value); return value
report = lifecycle_status('interrupted-observer')
assert report['casts'][0]['confirmations'] == 1 and report['casts'][0]['exited'], report
assert all(report[k] == 0 for k in ('live', 'back', 'hud', 'liveSounds')), report
cmd('waiting_probe restore'); time.sleep(.3)
restored = lifecycle_status('restored-observer')['casts'][-1]
assert restored['casts'] == 0 and restored['pileEmpty'] and restored['opening'] > .99 and not restored['audioEvents'], restored
cmd('stage_probe speed instant'); time.sleep(.3)
report = lifecycle_status('instant-switch-observer')
assert all(report[k] == 0 for k in ('live', 'back', 'hud', 'liveSounds')), report
cmd('stage_probe speed normal'); cmd('waiting_probe restore'); time.sleep(.3)
assert lifecycle_status('normal-switch-observer')['live'] == 1
cmd('waiting_probe remove'); time.sleep(.3)
report = lifecycle_status('power-removal-observer')
assert all(report[k] == 0 for k in ('live', 'back', 'hud', 'liveSounds')), report
check_hand('final-hand'); snap(case/'final'); print(branch, 'lifecycle passed', flush=True)

# Interrupt the actual ring while its contour is still being traced.
stage('waiting-stage-creation', seed=False)
cmd('stage_probe speed normal'); cmd('waiting_probe watch'); cmd('waiting_probe cancel-on-build')
prep('KAREN_STAGE_IS_WAITING'); play('KAREN_STAGE_IS_WAITING'); time.sleep(3)
partial = lifecycle_status('creation-interrupted-observer')
assert all(partial[k] == 0 for k in ('live', 'back', 'hud', 'liveSounds')), partial
node = partial['casts'][0]
assert node['exited'] and node['creationStarts'] == 1 and node['minBuild'] < .1 and .25 < node['maxBuild'] < .85, node
cmd('waiting_probe restore'); time.sleep(.4)
restored = lifecycle_status('creation-restored-observer')['casts'][-1]
assert restored['creationStarts'] == 0 and restored['minBuild'] > .99 and restored['peakCreationHeads'] == 0 and not restored['audioEvents'], restored
cmd('waiting_probe remove'); time.sleep(.3)
assert all(lifecycle_status('creation-removed-observer')[k] == 0 for k in ('live', 'back', 'hud', 'liveSounds'))
check_hand('creation-final-hand'); snap(case/'creation-final'); print(branch, 'creation interruption passed', flush=True)
