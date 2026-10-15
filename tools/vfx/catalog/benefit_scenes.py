# Executed inside record.py: every cast uses real cards and native hooks.
def composition():
    return json.loads(cmd('showcase composition')['message'])

def effects_idle(label):
    for _ in range(180):
        report = composition()
        if report['registered'] == report['states'] == report['stages'] == report['benefits'] == report['back'] == report['hud'] == 0:
            assert abs(report['eventWeight'] - 1) < .001, report
            save(label + '-composition', report)
            return report
        time.sleep(.08)
    raise AssertionError(('Presentation did not release all layers', report))

def next_turn(label):
    before = state()['battle']['round']
    post(action='end_turn')
    for _ in range(240):
        current = state()
        if current.get('battle', {}).get('round', 0) > before and current['battle']['is_play_phase']:
            break
        time.sleep(.15)
    else:
        raise AssertionError(('No next turn', current))
    settle(label); effects_idle(label)

if want('encore'):
    stage('encore', seed=False)
    for _ in range(4): prep('KAREN_CONTINUE')
    cmd('upgrade 3'); time.sleep(.4)
    record_start()
    for i in range(4):
        clip_cast('KAREN_CONTINUE', '续演｜原生匕首命中与红金星屑消耗' + (' · 升级' if i == 3 else ''))
    finish()

for name, card, title in [
    ('banana-cake', 'KAREN_BANANA_CAKE', 'Banana蛋糕｜聚光凝聚，实际药水入槽'),
    ('banana-snack', 'KAREN_SMALL_SNACK', 'Banana什锦｜暖雾上升，临时力量确认'),
    ('banana-dinner', 'KAREN_BANANA', 'Banana晚餐｜暖金气流，永久力量确认'),
    ('radiance', 'KAREN_WE_KOWN_TOKEN', '光辉｜从实际卡图散光，交接能量槽'),
    ('shine-team', 'KAREN_SHINE_TOGETHER_ALL', '一起闪耀吧｜队伍脚灯与真实奖励确认（单人注入展示）'),
]:
    if not want(name): continue
    stage(name, seed=False)
    if name == 'banana-cake':
        for potion in list(state()['player'].get('potions', [])):
            post(action='discard_potion', slot=potion['slot'])
    prep(card); record_start(); clip_cast(card, title); effects_idle('complete'); finish()

if want('banana-cake-full'):
    stage('banana-cake-full', seed=False)
    while len(state()['player']['potions']) < state()['player']['max_potion_slots']:
        cmd('potion FLEX_POTION'); time.sleep(.35)
    before = len(state()['player']['potions'])
    prep('KAREN_BANANA_CAKE'); record_start()
    clip_cast('KAREN_BANANA_CAKE', 'Banana蛋糕｜药水槽已满，光雾消散，不显示成功入槽')
    assert len(state()['player']['potions']) == before
    effects_idle('full-slot'); finish()

if want('snack-transform'):
    stage('snack-transform', seed=False); prep('KAREN_SMALL_SNACK'); prep('KAREN_FALL')
    record_start(); marking('Banana什锦｜坠落入约定塔，回合结束变化为晚餐')
    play('KAREN_FALL')
    for _ in range(100):
        current = state()
        if current['state_type'] in ('hand_select', 'card_select'):
            select_first('snack-to-tower'); break
        # Native choice auto-resolves when only one eligible card remains.
        if 'KAREN_SMALL_SNACK' in integration()['promise']:
            settle('snack-to-tower'); break
        time.sleep(.1)
    else:
        raise AssertionError(('Snack did not enter the real promise pile', current))
    assert integration()['promise'] == ['KAREN_SMALL_SNACK'], integration()
    marking('变化交接｜真实卡牌转换与塔星身份更新')
    next_turn('transformed')
    assert integration()['promise'] == ['KAREN_BANANA'], integration()
    finish()

if want('ticket'):
    stage('ticket', seed=False); prep('KAREN_TICKET'); record_start()
    clip_cast('KAREN_TICKET', '门票｜连续透视光路由近至远铺开，能力确认')
    marking('门票回返｜回合开始生成真实约定之塔并交接到手牌')
    next_turn('ticket-return')
    assert any(c['id'] == 'KAREN_TOWER_OF_PROMISE' for c in state()['player']['hand'])
    finish()

if want('dream'):
    stage('dream', seed=False); prep('KAREN_WE_KOWN'); record_start()
    clip_cast('KAREN_WE_KOWN', '两人的梦想｜打出仅应用能力，移除自定义开场特效')
    report=json.loads(cmd('revision_probe status')['message']);assert report['dreamCast']==report['dreamTrigger']==0,report
    marking('梦想兑现｜回合结束生成光辉，真实入塔与星光交接')
    next_turn('dream-return')
    assert 'KAREN_WE_KOWN_TOKEN' in integration()['promise'], integration()
    report=json.loads(cmd('revision_probe status')['message']);assert report['dreamCast']==0 and report['dreamTrigger']==1,report
    finish()

for void in (False, True):
    name = 'duet-void' if void else 'duet-normal'
    if want(name):
        stage(name, void); prep('KAREN_ON_STAGE'); record_start()
        clip_cast('KAREN_ON_STAGE', ('空虚｜' if void else '约定塔｜') + '命运与本能：卡图双星，实际取牌与回能')
        effects_idle('duet'); finish()

if want('starlight-act1'):
    stage('starlight-act1', seed=False)
    for _ in range(3): cmd('card KAREN_DEFEND Draw'); time.sleep(.15)
    prep('KAREN_STARLIGHT_CARD'); prep('KAREN_SHINE_STRIKE'); record_start()
    clip_cast('KAREN_STARLIGHT_CARD', 'Starlight 第一幕｜蓝幕升起与粉星能力确认')
    clip_cast('KAREN_SHINE_STRIKE', '第一幕触发｜实际抽到卡牌上缘的短促掠光')
    effects_idle('act1'); finish()

def stage_hover(label):
    card = next(c for c in state()['player']['hand'] if c['id'] == 'KAREN_SHINE_STRIKE')
    post(action='hover_card', card_index=card['index']); time.sleep(.55)
    report = composition(); save(label + '-held', report)
    assert report['stages'] == report['states'] == 1 and abs(report['eventWeight'] - .72) < .002, report
    assert abs(report['otherOwnerWeight'] - .72) < .002, report
    return report

if want('starlight-act2'):
    stage('starlight-act2', seed=False); prep('KAREN_STARLIGHT02_CARD'); prep('KAREN_SHINE_STRIKE')
    record_start(); clip_cast('KAREN_STARLIGHT02_CARD', 'Starlight 第二幕｜更换独立灯光音效；前景下移与椭圆铺光')
    marking('第二幕预览｜蓝白平行背光，角色与敌人位于光幕前方'); stage_hover('hover')
    time.sleep(1); marking('第二幕结算｜同一背光覆盖三次重播与闪耀耗尽')
    clip_cast('KAREN_SHINE_STRIKE', '重播与耗尽｜背景持有至实际卡牌完整结算')
    effects_idle('act2'); finish()

if want('shine-play'):
    stage('shine-play',seed=False);prep('KAREN_SHINE_STRIKE');cmd('revision_probe shine KAREN_SHINE_STRIKE 1')
    record_start();marking('闪耀临界｜手牌卡缘与星屑提示');time.sleep(1)
    clip_cast('KAREN_SHINE_STRIKE','最后一点闪耀｜提示跟随卡牌进入打出区，持续至耗尽交接')
    report=json.loads(cmd('revision_probe status')['message'])
    assert report['handWarningFrames']>0 and report['playWarningFrames']>0 and report['missingPlayFrames']==0,report
    prep('KAREN_SHINE_STRIKE');cmd('revision_probe shine KAREN_SHINE_STRIKE 2')
    clip_cast('KAREN_SHINE_STRIKE','两点闪耀对照｜本次不会耗尽，打出过程中不误亮临界提示')
    report=json.loads(cmd('revision_probe status')['message']);assert report['falseWarningFrames']==0,report
    finish()

if want('shine-motion'):
    stage('shine-motion',seed=False)
    for card in ['KAREN_SHINE_STRIKE','KAREN_NONON']:
        prep(card);cmd('shine_fuel set '+card+' 1')
    record_start();marking('临界提示｜错拍流光与自由漂浮星屑');time.sleep(1.2)
    card=next(c for c in state()['player']['hand'] if c['id']=='KAREN_SHINE_STRIKE')
    post(action='hover_card',card_index=card['index']);time.sleep(.7)
    marking('移开预览｜余烬保留惯性，亮星柔和跟随卡牌')
    post(action='unhover_card');time.sleep(.8)
    clip_cast('KAREN_SHINE_STRIKE','闪耀打击耗尽｜大范围星屑收拢，短弧快速入堆')
    clip_cast('KAREN_NONON','NONNON 耗尽｜卡牌整体缩小，星屑汇为亮点后加速飞入')
    prep('KAREN_SHINE_STRIKE');cmd('shine_fuel set KAREN_SHINE_STRIKE 2')
    clip_cast('KAREN_SHINE_STRIKE','两点闪耀对照｜本次不耗尽，不误亮临界提示')
    motion=json.loads(cmd('shine_motion status')['message']);save('motion-assertions',motion)
    assert motion['detachedSparkFrames']>0 and motion['maxSparks']<=40,motion
    assert len(motion['depletes'])==2 and motion['liveDepletes']==0,motion
    assert all(s['released'] and s['landingError']<.1 and s['duration']<.43 and s['minCardScale']<.2 for s in motion['depletes']),motion
    report=json.loads(cmd('revision_probe status')['message'])
    assert report['missingPlayFrames']==report['falseWarningFrames']==0 and report['playWarningFrames']>0,report
    finish()

if want('shine-speeds'):
    stage('shine-speeds',seed=False);record_start()
    for speed,card in [('fast','KAREN_SHINE_STRIKE'),('instant','KAREN_SHINE_STRIKE'),('normal','KAREN_DROP_FUEL')]:
        cmd('stage_probe speed '+speed);prep(card);cmd('shine_fuel set '+card+' 1')
        clip_cast(card,speed+'｜耗尽飞行时长与落点；燃料爆散后不重新显示卡面')
    motion=json.loads(cmd('shine_motion status')['message']);save('motion-assertions',motion)
    assert len(motion['depletes'])==3 and motion['liveDepletes']==0,motion
    assert all(s['released'] and s['landingError']<.1 and not s['faceReappeared'] for s in motion['depletes']),motion
    assert all(abs(s['duration']-duration)<.001 for s,duration in zip(motion['depletes'],[.30,.12,.42])),motion
    finish()

for void in (False, True):
    name = 'newstack-void' if void else 'newstack-normal'
    if not want(name): continue
    stage(name, void, seed=False)
    for card in ['KAREN_BURN', 'KAREN_PAST_AND_FUTURE', 'KAREN_FORM', 'KAREN_STAGE_REPRODUCE', 'KAREN_TICKET', 'KAREN_WE_KOWN', 'KAREN_STARLIGHT_CARD', 'KAREN_STARLIGHT02_CARD']:
        cmd('energy 20'); cast(card); effects_idle('setup-' + card)
    trim(); cmd('energy 12')
    for card in ['KAREN_SHINE_STRIKE', 'KAREN_ON_STAGE', 'KAREN_WE_KOWN_TOKEN']:
        prep(card)
    record_start(); marking('新旧叠加｜燃烧＋光环＋觉醒＋再生产＋双幕 Starlight')
    held = stage_hover('stack-hover'); time.sleep(.5)
    assert held['ambient'] < .58 and held['ambient'] >= .40, held
    post(action='unhover_card'); effects_idle('stack-unhover')
    clip_cast('KAREN_SHINE_STRIKE', '闪耀三连｜单个舞台背光，第一幕真实抽牌与耗尽')
    effects_idle('stack-replay'); cmd('energy 12')
    clip_cast('KAREN_ON_STAGE', '命运与本能｜塔星／图标交接，牌缘燃烧与光环同时可见')
    effects_idle('stack-duet')
    clip_cast('KAREN_WE_KOWN_TOKEN', '光辉｜卡面散光与回能，背景装饰让出亮度')
    effects_idle('stack-radiance'); trim()
    marking('回合交接｜梦想生成光辉、门票生成塔牌、觉醒抽牌')
    next_turn('stack-turn'); finish()

if want('stage-lifecycle'):
    stage('stage-lifecycle', seed=False); cast('KAREN_STARLIGHT02_CARD'); effects_idle('setup')
    prep('KAREN_SHINE_STRIKE'); record_start()
    marking('取消预览｜淡出后恢复常驻亮度，不留下背光／HUD 节点')
    stage_hover('cancel'); post(action='unhover_card'); effects_idle('cancelled')
    marking('离开房间｜持有预览期间换场，旧节点与持有权全部清理')
    stage_hover('room-change'); cmd('promise_integration off'); fight(); trim()
    cmd('promise_integration watch'); effects_idle('room-changed')
    marking('Instant｜跳过新特效，原生收益仍正常完成')
    cmd('stage_probe speed instant'); prep('KAREN_TICKET'); play('KAREN_TICKET'); time.sleep(1)
    assert json.loads(cmd('stage_probe status')['message'])['ticket'] == 1
    assert all(c['id'] != 'KAREN_TICKET' for c in state()['player']['hand'])
    effects_idle('instant'); cmd('native_speed normal'); finish()
