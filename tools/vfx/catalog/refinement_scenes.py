"""Targeted native-card recordings for the accepted October 20 VFX choices."""
def refinement_observe():
    return json.loads(cmd('vfx_refinement status')['message'])


def refinement_play(card):
    before = sum(c['id'] == card for c in json.loads(cmd('bridge_vfx status')['message'])['finished'])
    play(card)
    for _ in range(200):
        history = json.loads(cmd('bridge_vfx status')['message'])
        if sum(c['id'] == card for c in history['finished']) > before:
            return
        time.sleep(.1)
    raise AssertionError(('Native card play did not finish', card))


def refinement_finish():
    save('refinement-final', refinement_observe())
    finish()


if 'refinement-burn' in wanted:
    stage('refinement-burn', seed=False)
    prep('KAREN_BURN'); refinement_play('KAREN_BURN'); time.sleep(.8)
    cmd('showcase seed KAREN_SHINE_STRIKE 2'); time.sleep(.8)
    prep('KAREN_TOWER_OF_PROMISE'); refinement_play('KAREN_TOWER_OF_PROMISE'); settle('burn-drawn')
    cmd('promise_integration shine1'); time.sleep(.6)
    cards = [c for c in state()['player']['hand'] if c['id'] == 'KAREN_SHINE_STRIKE']
    assert len(cards) == 2, cards
    record_start(); marking('燃烧手牌｜采用 10/18 细化：火花与烟雾减弱，减少顶部发射'); time.sleep(3)
    marking('悬停临界闪耀牌｜燃烧与最后一点闪耀共同显示')
    post(action='hover_card', card_index=cards[0]['index']); time.sleep(3)
    held = refinement_observe(); save('refinement-observation', held)
    assert len(held['hand']) == 2 and all(c['burn'] and abs(c['sparkAlpha'] - .72) < .001
        and abs(c['sparkScale'] - .72) < .001 and abs(c['smokeAlpha'] - .30) < .001 for c in held['hand']), held
    post(action='unhover_card'); time.sleep(1.2); refinement_finish()

if 'refinement-position' in wanted:
    stage('refinement-position', seed=False)
    for card in ['KAREN_POSITION0', 'KAREN_SHINE_STRIKE', 'KAREN_STRIKE', 'KAREN_DEFEND']:
        prep(card)
    cmd('promise_integration shine1'); cmd('pzero watch')
    record_start(); marking('Position 0｜施放暖光与次数脚灯，恢复卡顶 T 标记')
    clip_cast('KAREN_POSITION0', 'Position 0｜施放暖光与次数脚灯')
    cast_state = json.loads(cmd('pzero status')['message']); save('position-cast', cast_state)
    assert cast_state['live'] == cast_state['remaining'] == 1 and cast_state['ready'], cast_state
    for card_id in ['KAREN_DEFEND', 'KAREN_SHINE_STRIKE', 'KAREN_STRIKE']:
        card = next(c for c in state()['player']['hand'] if c['id'] == card_id)
        marking('技能牌｜不显示卡顶标记' if card_id == 'KAREN_DEFEND' else '悬停攻击牌｜T 固定在卡框顶部中央，不向上偏移')
        post(action='hover_card', card_index=card['index']); time.sleep(1.6)
        hovered = json.loads(cmd('pzero status')['message']); save('position-hover-' + card_id, hovered)
        expected_marker = int(card_id != 'KAREN_DEFEND')
        assert hovered['cardLights'] == expected_marker and hovered['hover'] == bool(expected_marker), hovered
        assert sum(c['position'] for c in refinement_observe()['hand']) == expected_marker
        if expected_marker:
            post(action='hover_card', card_index=card['index']); time.sleep(.3)
            repeated = json.loads(cmd('pzero status')['message']); save('position-repeat-hover-' + card_id, repeated)
            assert repeated['hover'] and repeated['cardLights'] == 1 and repeated['nodes'][0]['claimStarts'] == 1, repeated
        post(action='unhover_card'); time.sleep(.3)
        released = json.loads(cmd('pzero status')['message']); save('position-unhover-' + card_id, released)
        assert not released['hover'] and released['cardLights'] == 0, released
    clip_cast('KAREN_STRIKE', '第一次攻击｜确认格挡与冷光，剩余次数脚灯熄灭')
    spent = json.loads(cmd('pzero status')['message']); save('position-spent', spent)
    assert spent['confirmations'] == 1 and spent['maxLights'] > 0 and spent['block'] > 0
    assert spent['remaining'] == 0 and not spent['ready'] and not spent['hover'] and spent['cardLights'] == 0, spent
    card = next(c for c in state()['player']['hand'] if c['id'] == 'KAREN_SHINE_STRIKE')
    post(action='hover_card', card_index=card['index']); time.sleep(.5)
    exhausted = json.loads(cmd('pzero status')['message']); save('position-exhausted-hover', exhausted)
    assert not exhausted['hover'] and exhausted['cardLights'] == 0, exhausted
    post(action='unhover_card'); marking('下一回合｜次数脚灯重新就绪，攻击牌恢复固定卡顶 T 标记')
    before = state()['battle']['round']; post(action='end_turn')
    for _ in range(240):
        current = state()
        if current.get('battle', {}).get('round', 0) > before and current['battle']['is_play_phase']:
            break
        time.sleep(.15)
    else:
        raise AssertionError(('Next turn did not start', current))
    settle('position-next-turn'); prep('KAREN_STRIKE')
    card = next(c for c in state()['player']['hand'] if c['id'] == 'KAREN_STRIKE')
    post(action='hover_card', card_index=card['index']); time.sleep(1.5)
    rearmed = json.loads(cmd('pzero status')['message']); save('position-rearmed', rearmed)
    assert rearmed['ready'] and rearmed['hover'] and rearmed['remaining'] == rearmed['cardLights'] == 1, rearmed
    marking('悬停时移除能力｜脚灯、卡顶标记与悬停状态一并清理')
    cmd('pzero remove'); time.sleep(.8)
    removed = json.loads(cmd('pzero status')['message']); save('position-removed', removed)
    assert removed['live'] == removed['back'] == removed['cardLights'] == 0 and not removed['hover'], removed
    post(action='unhover_card'); refinement_finish()

if 'refinement-why' in wanted:
    stage('refinement-why', seed=False)
    for _ in range(4): prep('KAREN_DEFEND')
    prep('KAREN_WHY'); record_start(); marking('Why｜保留入场冲击，随后降低暖色角部亮度')
    play('KAREN_WHY')
    for _ in range(100):
        if state()['state_type'] == 'hand_select': break
        time.sleep(.1)
    else:
        raise AssertionError('Why selection did not open')
    save('why-early', refinement_observe()); time.sleep(2)
    held = refinement_observe(); save('refinement-observation', held)
    assert held['why']['active'] and held['why']['cornerDecayShader'] and held['why']['age'] > 1.7, held
    marking('持续选牌｜暖色角部退到低亮度，保留流光与原生按钮'); time.sleep(4)
    selection = state()['hand_select']; marking('选择并确认｜边缘退出，按真实结果移牌')
    for card in selection['cards'][:2]: post(action='combat_select_card', card_index=card['index'])
    if state()['state_type'] == 'hand_select': post(action='combat_confirm_selection')
    settle('why-selected'); assert refinement_observe()['why'] is None
    refinement_finish()

if 'refinement-lastword' in wanted:
    stage('refinement-lastword', seed=False); cmd('showcase clear'); trim()
    record_start(); marking('最后的台词｜保留 main 10/20 的均匀发射、T 字漂移与余辉')
    prep('KAREN_LAST_WORD'); time.sleep(4)
    idle = refinement_observe(); save('lastword-idle', idle)
    assert len(idle['lastWord']) == 1 and idle['lastWord'][0]['particles'] > 32, idle
    card = state()['player']['hand'][0]; marking('原生悬停｜保留现有舞台追光与随机大小，不播放终幕影片')
    post(action='hover_card', card_index=card['index']); time.sleep(3)
    save('lastword-hover', refinement_observe()); post(action='unhover_card'); time.sleep(1.5)
    refinement_finish()
