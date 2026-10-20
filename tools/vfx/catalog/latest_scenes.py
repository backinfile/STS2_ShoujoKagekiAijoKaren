"""Current card and battle presentations, executed inside the isolated MCP recorder."""

def await_selection(kind, screen_type=None):
    for _ in range(120):
        current = state()
        if current['state_type'] == kind and (screen_type is None or current.get(kind,{}).get('screen_type')==screen_type):
            return current
        time.sleep(.1)
    raise AssertionError(('Selection did not open', kind, current))


def turn_without_idle(label):
    before = state()['battle']['round']
    post(action='end_turn')
    for _ in range(240):
        current = state()
        if current.get('battle', {}).get('round', 0) > before and current['battle']['is_play_phase']:
            return settle(label)
        time.sleep(.15)
    raise AssertionError(('Next turn did not begin', current))


if 'battle-entrance' in wanted:
    stage('battle-entrance', seed=False)
    cmd('promise_integration off')
    cmd('entrance_probe watch')
    record_start(); marking('进入新战斗｜Karen 披风、舞台灯光与约定塔揭幕')
    fight(); time.sleep(3)
    marking('待机｜呼吸与披风摆动，能量球前后粒子')
    time.sleep(3)
    cmd('promise_integration watch'); settle('entrance-finished')
    entrance = json.loads(cmd('entrance_probe status')['message'])
    save('entrance-probe', entrance)
    assert entrance['casts'] == 1 and entrance['soundCues'] == 0, entrance
    assert entrance['poseFrames'] > 0 and entrance['towerPartialFrames'] > 0, entrance
    assert entrance['live'] == entrance['backLive'] == entrance['posesLive'] == 0, entrance
    finish()

if 'polish-jealousy' in wanted:
    exec(compile((main/'tools/vfx/catalog/suitability_polish.py').read_text(encoding='utf-8'), 'suitability_polish.py', 'exec'))
    polish_jealousy_scene()

if 'back-to-back' in wanted:
    stage('back-to-back')
    prep('KAREN_BACK_TO_BACK_CARD'); prep('KAREN_BACK_TO_BACK_CARD')
    cmd('stage_probe upgrade KAREN_BACK_TO_BACK_CARD')
    record_start()
    clip_cast('KAREN_BACK_TO_BACK_CARD', '背靠背＋｜红蓝双剑接近、交击与接触碎片')
    clip_cast('KAREN_BACK_TO_BACK_CARD', '背靠背｜约定牌堆库存决定表现强度')
    finish()

if 'unforgettable' in wanted:
    stage('unforgettable', seed=False)
    cmd('scar_probe seed stack'); time.sleep(1.4); cmd('scar_probe watch')
    record_start()
    for index in range(3):
        clip_cast('KAREN_SHINE_TOGETHER', f'第 {index+1} 次命中｜暖金星切淡出，旧星痕留在原位')
    marking('战斗留痕｜多次星切错落保留，共享低亮度')
    time.sleep(3)
    save('scar-probe', json.loads(cmd('scar_probe status')['message']))
    finish()

if 'shine-barrage' in wanted:
    stage('shine-barrage', seed=False)
    record_start()
    for mode, label in [('normal','普通版'), ('upgrade','升级版'), ('last','最后一点闪耀')]:
        cmd('barrage_probe seed '+mode); time.sleep(1); cmd('barrage_probe watch')
        clip_cast('KAREN_SHINE_STRIKE_BARRAGE', label+'｜整体起势、逐击突刺、背侧出口碎光')
        save('barrage-'+mode, json.loads(cmd('barrage_probe status')['message']))
    finish()

if 'position-zero' in wanted:
    stage('position-zero', seed=False)
    prep('KAREN_POSITION0'); prep('KAREN_STRIKE'); prep('KAREN_STRIKE')
    cmd('pzero watch'); record_start()
    clip_cast('KAREN_POSITION0', 'Position 0｜施放暖光与次数脚灯')
    card=next(c for c in state()['player']['hand'] if c['id']=='KAREN_STRIKE')
    marking('悬停攻击牌｜脚灯增强，T 标记固定在卡框顶部中央，不向上偏移')
    post(action='hover_card', card_index=card['index']); time.sleep(2)
    hovered = json.loads(cmd('pzero status')['message']); save('position-hover', hovered)
    assert hovered['hover'] and hovered['cardLights'] == 1, hovered
    post(action='unhover_card')
    clip_cast('KAREN_STRIKE', '第一次攻击｜确认格挡，冷光亮起后次数熄灭')
    marking('下一回合｜脚灯按真实回合重新就绪')
    turn_without_idle('position-next-turn'); time.sleep(1)
    save('position-probe', json.loads(cmd('pzero status')['message']))
    finish()

if 'passion' in wanted:
    stage('passion', seed=False)
    prep('KAREN_PASSION'); prep('KAREN_PASSION')
    record_start()
    clip_cast('KAREN_PASSION', '激情｜全屏光粒与真实能力获得确认')
    clip_cast('KAREN_PASSION', '再次施放｜受控粒子叠加与能力图标反馈')
    finish()

for name, card, label in [('arrogant-show','KAREN_ARROGANT','骄傲｜易伤、激情与额外遗物奖励登记'),
                           ('relay-reward','KAREN_FIGHT_RELAY','歌唱吧舞蹈吧互相争斗吧｜本地耗尽奖励演示')]:
    if name not in wanted: continue
    stage(name, seed=False)
    prep(card); cmd('vfx_polish watch')
    if name=='relay-reward': cmd('vfx_polish deplete '+card)
    record_start(); clip_cast(card, label)
    save('reward-probe', json.loads(cmd('vfx_polish status')['message']))
    finish()

if 'star' in wanted:
    stage('star', seed=False)
    cmd('star_probe setup 3 discard'); time.sleep(1.5)
    record_start()
    clip_cast('KAREN_STAR', '唯一闪耀牌｜星光聚集，真实打出三次后耗尽并确认奖励')
    save('star-probe', json.loads(cmd('star_probe status')['message']))
    finish()

if 'why' in wanted:
    stage('why', seed=False)
    for _ in range(4): prep('KAREN_DEFEND')
    prep('KAREN_WHY'); record_start()
    marking('Why｜进入原生手牌选择，边缘流光保持')
    play('KAREN_WHY'); selected=await_selection('hand_select'); time.sleep(2)
    for card in selected['hand_select']['cards'][:2]:
        marking('选择卡牌｜边缘流光与原生选择反馈')
        post(action='combat_select_card', card_index=card['index']); time.sleep(.6)
    if state()['state_type']=='hand_select': post(action='combat_confirm_selection')
    settle('why-selected'); finish()

for upgraded in (False, True):
    name='wake-upgraded' if upgraded else 'wake-up'
    if name not in wanted: continue
    stage(name, seed=False)
    cmd('wake_probe seed draw 3 '+('upgrade' if upgraded else 'base')); time.sleep(2)
    record_start(); marking('唤醒｜打开原生选牌界面')
    play('KAREN_WAKE_UP'); selected=await_selection('card_select'); time.sleep(1.5)
    if upgraded:
        marking('唤醒＋第一步｜选择抽牌堆，随后展开幕帘')
        option=next(c for c in selected['card_select']['cards'] if c['id']=='KAREN_WAKE_UP_DRAW_PILE_OPTION')
        post(action='select_card', index=option['index']); time.sleep(.6)
        selected=await_selection('card_select','simple_select')
    marking('幕帘持有态｜卡牌与原生按钮保持清晰')
    time.sleep(2)
    for card in selected['card_select']['cards'][:2]:
        marking('选择与归手｜选中星光、幕布回收与真实卡牌尾迹')
        post(action='select_card', index=card['index']); time.sleep(.7)
    if state()['state_type']=='card_select': post(action='confirm_selection')
    settle('wake-selected'); finish()

if 'aquarium' in wanted:
    stage('aquarium', seed=False)
    cmd('aquarium_probe seed normal'); time.sleep(2)
    record_start()
    clip_cast('KAREN_AQUARIUM_CARD', '水族馆｜施放、流水光与气泡')
    marking('回合开始｜原生选牌界面与持续特效触发')
    before=state()['battle']['round']; post(action='end_turn')
    selected=await_selection('hand_select'); time.sleep(2)
    card=selected['hand_select']['cards'][0]
    marking('选择手牌放入约定牌堆｜气泡汇聚、真实卡牌交接')
    post(action='combat_select_card', card_index=card['index']); time.sleep(.4)
    if state()['state_type']=='hand_select': post(action='combat_confirm_selection')
    settle('aquarium-next-turn')
    save('aquarium-probe', json.loads(cmd('aquarium_probe status')['message']))
    finish()

if 'waiting-show' in wanted:
    stage('waiting-show', seed=False)
    cmd('waiting_probe fill 1'); time.sleep(1)
    prep('KAREN_STAGE_IS_WAITING'); prep('KAREN_PROMISE_DRAW')
    record_start()
    clip_cast('KAREN_STAGE_IS_WAITING', '舞台正在等待着｜图标沿边巡光与真实能力层数')
    time.sleep(2)
    clip_cast('KAREN_PROMISE_DRAW', '约定牌堆清空｜光点收弱，实际回能与确认反馈')
    prep('KAREN_STAGE_IS_WAITING')
    clip_cast('KAREN_STAGE_IS_WAITING', '空堆叠层｜巡游光点同步增加')
    save('waiting-probe', json.loads(cmd('waiting_probe status')['message']))
    finish()
