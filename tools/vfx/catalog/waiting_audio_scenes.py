# The normal showcase already checks both casts and two real gains. Stress short audio separately.
stage('waiting-stage-audio', seed=False)
cmd('stage_probe speed fast'); cmd('waiting_probe watch'); waiting_cast()
before = state()['player']['energy']; cmd('waiting_probe rapid')
for _ in range(160):
    rapid = json.loads(cmd('waiting_probe status')['message'])
    if rapid['rapidDone'] == 8: break
    time.sleep(.1)
else: raise AssertionError(rapid)
settle('audio-rapid')
rapid = observed('audio-rapid')
assert state()['player']['energy'] == before + 8
assert rapid['casts'][0]['confirmations'] == 8 and rapid['casts'][0]['peakSounds'] <= 4, rapid
assert all(abs(cue['pitch'] - 1.6) < .01 and cue['bus'] == 'SFX' for cue in rapid['casts'][0]['audioEvents']), rapid
cmd('waiting_probe remove'); time.sleep(.3)
assert all(observed('audio-removed')[key] == 0 for key in ('live', 'back', 'hud', 'liveSounds'))
cmd('stage_probe speed normal')
