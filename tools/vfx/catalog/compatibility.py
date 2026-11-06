# Targeted checks for state-dependent transfers after the tower effects were merged.
compat_checks = []

def preview_check(card, expected, label):
    cmd('tower_recall drag ' + card); time.sleep(.5)
    probe = json.loads(cmd('tower_recall status')['message'])
    save(label + '-probe', probe); snap(case / (label + '-preview'))
    compat_checks.append(dict(case=case.name, card=card, expected=expected,
                              actual=probe['computed'], alpha=probe['alpha'],
                              passed=probe['computed'] == expected))
    cmd('tower_recall cancel'); time.sleep(.4)

def compatibility_stage(name, void=False):
    stage(name, void, seed=False)
    cmd('tower_recall ' + ('void' if void else 'prepare'))
    for _ in range(100):
        probe = json.loads(cmd('tower_recall status')['message'])
        if not probe['preparing']:
            break
        time.sleep(.1)
    else:
        raise RuntimeError(probe)
    time.sleep(.7)

compatibility_stage('compat-newday')
prep('KAREN_NEW_DAY'); prep('KAREN_STRIKE')
play('KAREN_NEW_DAY'); settle('newday-applied')
preview_check('KAREN_STRIKE', 'Put', 'next-card')
before = integration()
cmd('tower_recall drag KAREN_STRIKE'); time.sleep(.4); cmd('tower_recall play')
after = settle('strike-transferred')
save('actual-transfer', dict(before=before['promise'], after=after['promise']))
assert len(after['promise']) == len(before['promise']) + 1, after
probe = json.loads(cmd('tower_recall status')['message'])
save('resolution-probe', probe)
compat_checks.append(dict(case=case.name, card='KAREN_STRIKE-resolution',
                          passed=probe['playFrames'] > 0 and probe['playMinimumAlpha'] > .9))

compatibility_stage('compat-newday-take')
prep('KAREN_NEW_DAY'); prep('KAREN_PROMISE_DRAW'); prep('KAREN_DROP_FUEL')
play('KAREN_NEW_DAY'); settle('newday-applied')
preview_check('KAREN_PROMISE_DRAW', 'Both', 'draw-and-return')
preview_check('KAREN_DROP_FUEL', 'None', 'exhaust-negative')
prep('KAREN_SHINE_STRIKE'); cmd('promise_integration shine1')
preview_check('KAREN_SHINE_STRIKE', 'None', 'shine-depletion-negative')
prep('KAREN_FORM'); preview_check('KAREN_FORM', 'None', 'power-negative')
prep('KAREN_STRIKE'); cmd('showcase next-exhaust KAREN_STRIKE')
preview_check('KAREN_STRIKE', 'None', 'next-exhaust-negative')

compatibility_stage('compat-wakeup-normal')
prep('KAREN_WAKE_UP')
preview_check('KAREN_WAKE_UP', 'None', 'normal-negative')
cmd('tower_recall upgrade KAREN_WAKE_UP'); time.sleep(.4)
preview_check('KAREN_WAKE_UP', 'Take', 'normal-upgraded')

compatibility_stage('compat-wakeup-void', void=True)
cmd('showcase seed KAREN_DEFEND 1'); time.sleep(.7); prep('KAREN_WAKE_UP')
preview_check('KAREN_WAKE_UP', 'Take', 'void-draw')
before = integration(); play('KAREN_WAKE_UP'); time.sleep(.4)
s = state()
if s['state_type'] == 'card_select':
    for card in s['card_select']['cards'][:2]:
        post(action='select_card', index=card['index'])
    if state()['state_type'] == 'card_select':
        post(action='confirm_selection')
after = settle('wakeup-drawn')
save('actual-transfer', dict(before=before['draw'], after=after['draw']))
assert len(after['draw']) == len(before['draw']) - 2, after

compatibility_stage('compat-void-drawers', void=True)
for card, expected in [
        ('KAREN_RUN', 'Both'), ('KAREN_OUR_PROMISE', 'Both'),
        ('KAREN_MEET_AGAIN', 'Both'), ('KAREN_DROP_FUEL', 'Take'),
        ('KAREN_READY', 'Take'), ('KAREN_STAR_CRIME', 'Take'),
        ('KAREN_BANANA_MUFFIN', 'Take'), ('KAREN_TO_THE_STAGE', 'None')]:
    prep(card); preview_check(card, expected, card.lower())
    if card == 'KAREN_TO_THE_STAGE':
        cmd('tower_recall upgrade ' + card); time.sleep(.4)
        preview_check(card, 'Take', 'stage-upgraded')
    cmd('tower_recall retire ' + card); time.sleep(.5)
settle('drawers-complete')

(out / 'compatibility-checks.json').write_text(json.dumps(compat_checks, ensure_ascii=False, indent=2), encoding='utf-8')
print('COMPATIBILITY', json.dumps(compat_checks, ensure_ascii=False), flush=True)
assert all(c['passed'] for c in compat_checks), compat_checks
