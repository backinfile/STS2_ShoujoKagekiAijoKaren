# Executed by record.py only for --scenes void-tower-handoff.
checks = []


def presentation():
    return json.loads(cmd('void_orbit_check presentation')['message'])


def handoff_snap(name):
    snap(case / name)
    if recording:
        with urllib.request.urlopen(base + '/screenshot', timeout=30) as response:
            (case / (name + '.png')).write_bytes(response.read())


def verify_presentation(name, void, burn=False):
    p = presentation(); save(name + '-presentation', p)
    assert p['voidMode'] == void, p
    assert p['towerAlpha'] < .01 if void else p['towerAlpha'] > .99, p
    assert p['towerHit'] == (not void), p
    if void:
        assert p['usingIcon'] and p['layerCount'] == 2, p
    if burn:
        assert p['iconFire'] and p['flameAlpha'] < .01 and p['smokeAlpha'] < .01, p
    checks.append(dict(case=case.name, check=name, passed=True, presentation=p))
    return p


def activate_void(burn=False):
    before = integration(); handoff_snap('before-void')
    marking('世界上最空虚的人 · 扫描后塔与灯光淡出，库存迁入抽牌堆图标')
    play('KAREN_VOID')
    samples = []
    for _ in range(65):
        p = presentation(); samples.append(p)
        if p['voidMode'] and p['voidAge'] > 2.1:
            break
        time.sleep(.06)
    save('handoff-timeline', samples)
    assert any(.01 < p['towerAlpha'] < .99 for p in samples), samples
    assert samples[-1]['towerAlpha'] < .01, samples
    after = settle('after-void'); handoff_snap('after-void')
    assert after['draw'] == before['promise'] and not after['promise'], (before, after)
    verify_presentation('handoff', True, burn)


stage('void-tower-handoff')
for card in ['KAREN_VOID', 'KAREN_WAKE_UP', 'KAREN_TOWER_OF_PROMISE']:
    prep(card)
record_start(); marking('普通模式 · Karen 身后的约定塔与库存灯光')
verify_presentation('normal-before', False); time.sleep(1)
activate_void()
marking('唤醒 · 从图标取出两张牌，身后的塔保持退场')
handoff_snap('before-wakeup'); before = integration(); play('KAREN_WAKE_UP')
for _ in range(80):
    s = state()
    if s['state_type'] == 'card_select':
        break
    time.sleep(.1)
else:
    raise AssertionError(s)
for card in s['card_select']['cards'][:2]:
    post(action='select_card', index=card['index'])
if state()['state_type'] == 'card_select':
    post(action='confirm_selection')
after = settle('after-wakeup'); handoff_snap('after-wakeup')
assert len(after['draw']) == len(before['draw']) - 2, (before, after)
verify_presentation('wakeup', True)
marking('约定之塔 · 拖牌提示改为图标周围的金色灯光')
cmd('tower_recall drag KAREN_TOWER_OF_PROMISE'); time.sleep(1.2)
p = verify_presentation('tower-preview', True)
assert p['profile'] == 'Tower' and p['previewAlpha'] > .99, p
handoff_snap('tower-preview'); cmd('tower_recall cancel'); time.sleep(.6)
marking('约定之塔 · 从图标蓄光取出剩余库存')
handoff_snap('before-recall'); clip_cast('KAREN_TOWER_OF_PROMISE', '约定之塔 · 图标取牌与实际手牌落点')
after = integration(); handoff_snap('after-recall'); assert not after['draw'], after
verify_presentation('empty-draw', True)
cmd('tower_recall click'); time.sleep(.5)
assert presentation()['screen'] is None
checks.append(dict(case=case.name, check='hidden-tower-click', passed=True))
finish()

stage('void-tower-burn')
for card in ['KAREN_BURN', 'KAREN_PAST_AND_FUTURE']:
    cast(card)
for card in ['KAREN_VOID', 'KAREN_PROMISE_DRAW', 'KAREN_BRIDGE']:
    prep(card)
record_start(); marking('燃烧与过去未来 · 普通塔火焰与光带')
verify_presentation('burn-before', False); time.sleep(1)
activate_void(burn=True)
marking('取牌提示 · 图标点亮，原塔与塔火焰不再出现')
cmd('tower_recall drag KAREN_PROMISE_DRAW'); time.sleep(1.2)
p = verify_presentation('take-preview', True, True)
assert p['profile'] == 'Take' and p['previewAlpha'] > .99, p
handoff_snap('take-preview'); cmd('tower_recall cancel'); time.sleep(.5)
handoff_snap('before-take'); clip_cast('KAREN_PROMISE_DRAW', '图标取牌 · 光带与手牌燃烧边框继续联动')
handoff_snap('after-take'); verify_presentation('take', True, True)
marking('塔桥 · 从图标逐张自动出牌与追光')
cmd('bridge_vfx energy2'); handoff_snap('before-bridge'); play('KAREN_BRIDGE'); time.sleep(.8)
for _ in range(160):
    bs = json.loads(cmd('bridge_vfx status')['message'])
    if any(e['id'] == 'KAREN_BRIDGE' and not e['auto'] for e in bs['finished']) and bs['nodeCount'] == 0:
        break
    time.sleep(.15)
else:
    raise AssertionError(bs)
save('bridge-result', bs); settle('after-bridge'); handoff_snap('after-bridge')
assert bs['drawCasts'] > 0 and bs['prematureExitFrames'] == 0 and bs['lightingGapFrames'] == 0, bs
verify_presentation('bridge', True, True)
prep('KAREN_VOID'); cmd('energy 8')
clip_cast('KAREN_VOID', '重复打出空虚 · 塔保持退场')
verify_presentation('repeat-void', True, True)
prep('KAREN_STAGE_REPRODUCE'); clip_cast('KAREN_STAGE_REPRODUCE', '舞台再生产 · 新库存继续进入图标')
verify_presentation('reproduction', True, True)
marking('已有空虚状态重新建立表现 · 图标库存恢复，塔不闪回')
cmd('showcase rebuild-tower'); settle('rebuilt'); handoff_snap('rebuilt')
verify_presentation('rebuilt-in-void', True, True)
finish()

stage('void-tower-late-burn', void=True)
prep('KAREN_BURN'); prep('KAREN_PROMISE_DRAW')
record_start(); marking('先进入空虚 · 塔已消失')
verify_presentation('late-burn-before', True)
handoff_snap('before-late-burn')
clip_cast('KAREN_BURN', '空虚后再点燃 · 只在图标生成火焰')
handoff_snap('after-late-burn'); verify_presentation('late-burn', True, True)
handoff_snap('before-late-burn-take')
clip_cast('KAREN_PROMISE_DRAW', '图标火焰 · 取牌与手牌燃烧边框')
handoff_snap('after-late-burn-take'); verify_presentation('late-burn-take', True, True)
finish()

stage('void-tower-reset')
prep('KAREN_TOWER_OF_PROMISE')
record_start(); marking('下一场普通战斗 · 约定塔重新出现')
verify_presentation('normal-reset', False)
cmd('tower_recall drag KAREN_TOWER_OF_PROMISE'); time.sleep(1.2)
handoff_snap('normal-preview'); verify_presentation('normal-preview', False)
cmd('tower_recall cancel'); time.sleep(.6)
handoff_snap('normal-before-recall')
clip_cast('KAREN_TOWER_OF_PROMISE', '普通模式 · 原塔蓄光取牌保持正常')
handoff_snap('normal-after-recall'); verify_presentation('normal-recall', False)
assert not integration()['promise']
finish()
(out / 'void-tower-checks.json').write_text(json.dumps(checks, ensure_ascii=False, indent=2), encoding='utf-8')
print('VOID TOWER CHECKS', len(checks), 'passed', flush=True)
