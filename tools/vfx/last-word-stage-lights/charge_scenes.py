"""Real native hover and play; passive production VFX observation only."""
before='lastword-charge-before' in wanted
regular='lastword-regular' in wanted
stage('lastword-regular' if regular else ('lastword-charge-before' if before else 'lastword-charge'),seed=True)
cmd('promise_integration off');cmd('showcase clear');trim()
prep('KAREN_LAST_WORD');prep('KAREN_DEFEND')
def charge_status(name):
    value=json.loads(cmd('lastword_stage status')['message']);save(name+'-probe',value);return value
def charge_snap(name):
    snap(case/name);check_hand(name+'-hand')
def charge_nodes(value,number):
    assert len(value['current']['nodes'])==number and len(value['current']['tNodes'])==number,value['current']
cmd('lastword_stage watch')
charge_nodes(charge_status('blocked'),0);charge_snap('blocked')
charge_snap('before-remove-defend')
cmd('remove_card KAREN_DEFEND Hand');time.sleep(1.5)
charge_nodes(charge_status('idle'),1);charge_snap('idle')
card=next(c for c in state()['player']['hand'] if c['id']=='KAREN_LAST_WORD')
record_start();marking('常态 T 字与舞台追光');time.sleep(1.1)
marking('Hover｜T 字保持普通状态的飘散' if regular else 'Hover｜牌边均匀分布，轻微内聚与呼吸')
post(action='hover_card',card_index=card['index']);time.sleep(2.0)
charge_status('hover-first');time.sleep(2.4)
charge_status('hover-held')
marking('移开｜平滑恢复常态')
post(action='unhover_card');time.sleep(1.5)
charge_status('unhover-restored')
record_stop()
ready=case/'ready-recording';ready.mkdir(exist_ok=True)
for path in list(case.iterdir()):
    if path.is_file() and (path.name=='game.mp4' or path.name.startswith('recording') or path.name=='markers.json'):
        shutil.copy2(path,ready/path.name)
post(action='hover_card',card_index=card['index']);time.sleep(1.8)
hover=charge_status('hover-screenshot');charge_snap('hover-screenshot')
if regular:
    assert hover['current']['tNodes'][0]['charge']==0,hover['current']
    assert hover['current']['nodes'][0]['hover']>.99,hover['current']
elif not before:
    charge_t=hover['current']['tNodes'][0]
    assert charge_t['charge']>.99 and all(n>0 for n in charge_t['bins']) and charge_t['distributionCv']<.45,charge_t
post(action='unhover_card');time.sleep(1.5)
charge_nodes(charge_status('unhover-screenshot'),1);charge_snap('unhover-screenshot')
cmd('lastword_stage watch')
charge_snap('before-play')
record_start();marking('最后的台词｜真实出牌');time.sleep(1.0)
play('KAREN_LAST_WORD')
for _ in range(160):
    current=state()
    if current['state_type']=='rewards':break
    time.sleep(.12)
else:raise AssertionError(('No rewards',current))
time.sleep(.9)
record_stop()
result=charge_status('postplay')
assert not result['current']['attack'] and not result['current']['nodes'] and not result['current']['tNodes'],result['current']
charge_snap('rewards-cleanup')
print('LASTWORD_CHARGE_PASSED',branch,'before' if before else 'after',flush=True)
