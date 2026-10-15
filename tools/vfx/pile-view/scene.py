"""Executed by the isolated catalog recording harness with --scenes pile-view."""
def pile(command):
    return json.loads(cmd('pile_view ' + command)['message'])

def picture(name):
    # Capture the actual viewport even while the native recorder is active.
    with urllib.request.urlopen(base + '/screenshot', timeout=30) as response:
        (case / (name + '.png')).write_bytes(response.read())
    save(name, pile('status'))

def assert_closed(name):
    time.sleep(.12)
    status = pile('status'); save(name, status)
    assert status['living'] == status['decorations'] == 0 and status['screen'] is None, status
    check_hand(name + '-hand')

def opened(kind, label, hold=1.8):
    before = pile('status')
    if recording: marking(label)
    result = pile(kind); save(label + '-opened', result)
    assert result['decorations'] == 1 and result['promise'] == (kind == 'promise'), result
    assert result['backgroundOrder'] < result['order'] < result['gridOrder'], result
    assert result['mouse'] == 'Ignore' and result['focus'] == 'None', result
    time.sleep(.12); picture(label + '-opening')
    time.sleep(hold); picture(label + '-idle')
    status = pile('status')
    assert not status['processing'], status
    for field in ['promiseCards', 'shineCards']:
        assert before[field] == status[field], (field, before, status)
    pile('return'); assert_closed(label + '-closed')
    time.sleep(.4)

if want('pile-view'):
    stage('pile-view', seed=False)
    cmd('pile_view clear-shine')
    picture('empty-before')
    for kind in ['promise', 'shine']:
        opened(kind, 'empty-' + kind, .75)
    for card in ['KAREN_STRIKE', 'KAREN_DEFEND', 'KAREN_FALL']:
        cmd('showcase seed ' + card + ' 2')
    settle('seeded-promise'); picture('seeded-before')
    prep('KAREN_SHINE_STRIKE'); cmd('promise_integration shine1')
    snap(case / 'before-real-depletion'); play('KAREN_SHINE_STRIKE')
    settle('after-real-depletion'); picture('populated-before')
    record_start()
    opened('promise', '约定牌堆-普通')
    opened('shine', '闪耀耗尽牌堆-普通')
    cmd('native_speed fast')
    opened('promise', '约定牌堆-快速', .8)
    opened('shine', '闪耀耗尽牌堆-快速', .8)
    pile('instant')
    opened('promise', '约定牌堆-瞬时', .4)
    opened('shine', '闪耀耗尽牌堆-瞬时', .4)
    cmd('native_speed normal')
    # Closing during entrance and replacement must destroy the owner and all decoration.
    for i in range(8):
        pile('promise' if i % 2 == 0 else 'shine')
        time.sleep(.04)
        pile('close'); assert_closed('rapid-' + str(i))
    pile('promise'); time.sleep(.15); pile('shine'); time.sleep(.8)
    assert pile('status')['living'] == 1
    picture('replacement-shine')
    pile('vanilla'); time.sleep(.3)
    vanilla = pile('status'); save('vanilla-control', vanilla)
    assert vanilla['decorations'] == vanilla['living'] == 0, vanilla
    pile('return'); assert_closed('vanilla-closed')
    finish()
    # These are settings requests only: the hidden-window guard may preserve the
    # actual viewport. Do not count them as measured responsive-layout coverage.
    for width, height in [(1600, 900), (1100, 800)]:
        api('/settings', dict(window_width=width, window_height=height))
        time.sleep(.8)
        opened('promise', 'resize-' + str(width), .75)
    api('/settings', dict(window_width=1280, window_height=720))
    save('results', dict(passed=True, variants=branch,
         checks=['empty', 'populated', 'normal', 'fast', 'instant', 'native-return',
                 'early-close', 'replacement', 'vanilla-control', 'unchanged-piles', 'hand-model-view'],
         not_measured=['actual viewport resize; hidden-window guard preserves display']))
