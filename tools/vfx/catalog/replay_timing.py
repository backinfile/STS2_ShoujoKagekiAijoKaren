# Executed by record.py. Observe native replay completion without treating a
# transient model/node gap during Draw as a settled-hand failure.
stage('replay-timing', seed=False)
for _ in range(9): cmd('card KAREN_DEFEND Draw'); time.sleep(.08)
prep('KAREN_STARLIGHT02_CARD'); prep('KAREN_DROP_FUEL')
record_start()
clip_cast('KAREN_STARLIGHT02_CARD', '连续重播时序诊断：第二幕')
marking('投下燃料：逐步记录实际结算和手牌节点')
started_at = time.monotonic()
play('KAREN_DROP_FUEL')
timeline = []
for _ in range(100):
    current = state()
    hand = api('/singleplayer', dict(action='run_command', command='karen_check_hand'))
    history = json.loads(cmd('bridge_vfx status')['message'])
    timeline.append(dict(time=time.monotonic()-started_at, energy=current['player']['energy'],
        hand=json.loads(hand['message']), started=history['started'], finished=history['finished']))
    if current['player']['energy'] == 14 and any(e['id']=='KAREN_DROP_FUEL' for e in history['finished']):
        break
    time.sleep(.1)
else: raise AssertionError(('Replay did not finish', timeline[-1]))
save('replay-timeline', timeline)
settle('complete')
assert state()['player']['energy'] == 14
finish()
