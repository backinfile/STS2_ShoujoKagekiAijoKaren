"""Exercise real Form playback across the same room replacement used by the catalog."""
if 'form-music-lifecycle' in wanted:
    stage('form-music-lifecycle', seed=False)
    prep('KAREN_FORM')
    record_start()
    clip_cast('KAREN_FORM', '觉醒形态｜本场战斗替换 BGM')
    active = json.loads(cmd('showcase music-status')['message'])
    save('music-active', active)
    assert active['hasForm'] and active['currentPlaying'] and active['musicPlayers'] == 1, active
    marking('进入下一场战斗｜应恢复原生 BGM')
    cmd('promise_integration off')
    fight()
    time.sleep(.6)
    ended = json.loads(cmd('showcase music-status')['message'])
    save('music-next-room', ended)
    cmd('promise_integration watch')
    settle('next-room')
    finish()
    assert not ended['hasForm'] and not ended['currentPlaying'] and not ended['fadingPlaying'], ended
    assert ended['musicPlayers'] == 0 and not ended['needsRestore'], ended
    assert ended['nativeMusicEvent'], 'Native music event was not recreated after Form stopped'
    cmd('vfx_coexist music')
    for _ in range(100):
        races = json.loads(cmd('vfx_coexist status')['message'])
        if not races['running']:
            break
        time.sleep(.1)
    save('music-fade-checks', races)
    assert not races['running'] and races['checks'] and all(races['checks'].values()), races
    # Cover the regular combat-end hook as well as the direct room replacement.
    cmd('promise_integration off')
    fight()
    prep('KAREN_FORM')
    play('KAREN_FORM')
    time.sleep(2)
    active = json.loads(cmd('showcase music-status')['message'])
    assert active['hasForm'] and active['currentPlaying'], active
    snap(case/'normal-end-before')
    cmd('kill all')
    for _ in range(100):
        if state()['state_type'] == 'rewards':
            break
        time.sleep(.1)
    else:
        raise AssertionError('Normal combat end did not reach rewards')
    time.sleep(.7)
    ended = json.loads(cmd('showcase music-status')['message'])
    save('music-normal-end', ended)
    snap(case/'normal-end-after')
    assert not ended['currentPlaying'] and not ended['fadingPlaying'], ended
    assert ended['musicPlayers'] == 0 and not ended['needsRestore'], ended
    assert ended['nativeMusicEvent'], ended
