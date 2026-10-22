"""Red/green regressions through real MCP plays and native death/menu teardown."""
from PIL import Image

assert not any(r['result']!='passed' for r in results), 'Base fixture failed; do not run fix assertions on an invalid fixture'

fix_results=[]
def fix_save():
    (out/'fix-results.json').write_text(json.dumps(fix_results,ensure_ascii=False,indent=2),encoding='utf-8')
def fix_result(name,passed,**data):
    row=dict(name=name,result='passed' if passed else 'failed',build=capture_build,**data)
    fix_results.append(row);fix_save();print(branch,name,row['result'],flush=True)

case=out/'fix-native-start';case.mkdir(exist_ok=True)
cmd('entrance_probe config on');cmd('entrance_probe watch')
start_character('KAREN')
entrance=json.loads(cmd('entrance_probe status')['message']);save('entrance',entrance)
fix_result('entrance-native-entry',entrance['poseFrames']>0 and entrance['casts']>0,observed=entrance)
interrupted=json.loads(cmd('vfx_fix interrupt')['message']);save('interrupted',interrupted)
fix_result('entrance-native-interrupt',all(interrupted.values()),observed=interrupted)
time.sleep(1.5)

for depth in [1,8]:
    fresh_adaptation('fix-bridge-'+str(depth))
    cmd('boundary seed Promise '+','.join(['KAREN_BRIDGE']*(depth-1)+['KAREN_WHY']))
    seed('Hand',['KAREN_DEFEND','KAREN_STRIKE']);add('KAREN_BRIDGE');cmd('boundary energy 0')
    cmd('bridge_vfx watch');cmd('boundary watch');cmd('vfx_fix watch');snap(case/'before');check_hand('before-hand')
    before=len(probe()['finished']);play('KAREN_BRIDGE')
    choice=wait_state(lambda s:s['state_type']=='hand_select',40)
    time.sleep(1.1);held=json.loads(cmd('vfx_fix status')['message']);save('held-render',held);snap(case/'held')
    image=Image.open(case/'held.png').convert('RGB');region=image.crop((540,420,760,525))
    white=sum(all(v>245 for v in px) for px in region.get_flattened_data())/(region.width*region.height)
    assert held['coordinators']==1 and held['count']==depth and min(held['ages'])>.7,held
    assert len(set(held['materialIds']))==depth,held
    fix_result('bridge-budget-'+str(depth),held['light']<=1.01 and held['atmosphere']<=1.01 and held['light']>.85 and white<.10,observed=held,whiteFraction=white)
    for _ in range(2):
        current=state()
        if current['state_type']!='hand_select' or not current['hand_select']['cards']:break
        post(action='combat_select_card',card_index=current['hand_select']['cards'][0]['index'])
    if state()['state_type']=='hand_select':post(action='combat_confirm_selection')
    done=wait_done('KAREN_BRIDGE',before);settle('after');snap(case/'after');check_hand('after-hand')
    assert done['activeBridges']==0 and not done['play'],done
    rendered=json.loads(cmd('vfx_fix status')['message']);save('rendered-lifetime',rendered)
    fix_result('bridge-lifetime-budget-'+str(depth),rendered['frames']>0 and rendered['peakLight']<=1.01 and rendered['peakAtmosphere']<=1.01 and rendered['minHeldLight']>.85,observed=rendered)
    fix_result('bridge-cleanup-'+str(depth),rendered['coordinators']==0 and len(done['started'])==len(done['finished']) and probe()['maxStages']>=depth)

# Re-run the original unobscured eight-stage symptom (no selection dimmer).
fresh_adaptation('fix-bridge-8-open')
cmd('boundary seed Promise '+','.join(['KAREN_BRIDGE']*7+['KAREN_DEFEND']))
add('KAREN_BRIDGE');cmd('boundary energy 0');cmd('bridge_vfx watch');cmd('boundary watch');cmd('vfx_fix watch')
snap(case/'before');check_hand('before-hand');before=len(probe()['finished']);play('KAREN_BRIDGE')
done=wait_done('KAREN_BRIDGE',before);settle('after');snap(case/'after');check_hand('after-hand')
rendered=json.loads(cmd('vfx_fix status')['message']);save('rendered-lifetime',rendered)
assert done['maxStages']>=8 and (case/'peak-depth.png').is_file(),done
region=Image.open(case/'peak-depth.png').convert('RGB').crop((540,420,760,525))
white=sum(all(v>245 for v in px) for px in region.get_flattened_data())/(region.width*region.height)
fix_result('bridge-original-eight-stage',rendered['peakLight']<=1.01 and rendered['peakAtmosphere']<=1.01 and white<.10 and rendered['minHeldLight']>.85,observed=rendered,whiteFraction=white)

# Exercise normal teardown, a pre-disconnected callback, then a fresh real Karen run.
for mode in ['native','detached']:
    case=out/('fix-exit-'+mode);case.mkdir(exist_ok=True)
    if mode=='detached':cmd('vfx_fix detach')
    snap(case/'before');cmd('promise_integration off')
    logpath=dev/'godot.log';offset=len(logpath.read_text(encoding='utf-8',errors='replace'))
    cmd('die');wait_state(lambda s:s['state_type']=='game_over',45);time.sleep(1);snap(case/'game-over')
    post(action='menu_select',option='main_menu')
    wait_state(lambda s:s.get('menu_screen')=='main' and 'singleplayer' in s.get('options',[]));time.sleep(3);snap(case/'menu')
    fragment=logpath.read_text(encoding='utf-8',errors='replace')[offset:]
    (case/'exit.log').write_text(fragment,encoding='utf-8')
    errors=[line for line in fragment.splitlines() if 'Attempt to disconnect a nonexistent connection' in line]
    fix_result('entrance-exit-'+mode,not errors,errors=errors)
    start_character('KAREN')
    report=json.loads(cmd('entrance_probe status')['message']);save('reentry',report)
    check_hand('reentry-hand');snap(case/'reentry')
    fix_result('entrance-reentry-'+mode,report['poseFrames']>0 and report['casts']>0,observed=report)
# Compatibility check for the changed room-wide budget: a native foreign actor,
# persistent actor effects, and the Act 2 surrounded encounter.
case=out/'fix-foreign-pincer';case.mkdir(exist_ok=True)
start_character('IRONCLAD');finish_combat();cmd('fight KAISER_CRAB_BOSS')
wait_state(lambda s:s.get('battle',{}).get('is_play_phase') and len(s['battle']['enemies'])==2)
time.sleep(1.5);cmd('promise_integration off');trim();cmd('showcase clear');cmd('showcase rebuild-tower')
cmd('promise_integration watch');cmd('boundary watch');cmd('actor_state watch')
actual_cast('KAREN_BURN','burn');actual_cast('KAREN_FORM','form')
# Burn upgrades inner bridges: each reserves two, so thirteen produce eight full stages.
cmd('boundary seed Promise '+','.join(['KAREN_BRIDGE']*13+['KAREN_DEFEND']))
add('KAREN_BRIDGE');cmd('boundary energy 0');cmd('bridge_vfx watch');cmd('boundary watch');cmd('vfx_fix watch')
snap(case/'before');check_hand('before-hand');before=len(probe()['finished']);play('KAREN_BRIDGE')
done=wait_done('KAREN_BRIDGE',before,timeout=60);settle('after');snap(case/'after');check_hand('after-hand')
rendered=json.loads(cmd('vfx_fix status')['message']);save('rendered-lifetime',rendered)
fix_result('bridge-foreign-surrounded-stack',rendered['character']=='IRONCLAD' and done['maxStages']>=8 and rendered['peakLight']<=1.01 and rendered['peakAtmosphere']<=1.01 and done['activeBridges']==0 and not done['play'],observed=rendered)
assert all(r['result']=='passed' for r in fix_results),[(r['name'],r['result']) for r in fix_results]
