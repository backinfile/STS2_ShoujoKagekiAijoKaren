# Executed by record.py only for --scenes void-tower-timing.
import math


def on_beam(star, beam):
    # Measure against the rendered ribbon polyline, including how far its
    # head has traveled. An early independent flight to the icon must fail.
    def point(t):
        return ((1-t)**2 * beam['x'] + 2*(1-t)*t * beam['controlX'] + t*t * beam['endX'],
                (1-t)**2 * beam['y'] + 2*(1-t)*t * beam['controlY'] + t*t * beam['endY'])
    nearest = (float('inf'), 0)
    for index in range(64):
        a, b = point(index / 64), point((index + 1) / 64)
        dx, dy = b[0]-a[0], b[1]-a[1]
        u = max(0, min(1, ((star['x']-a[0])*dx + (star['y']-a[1])*dy) / (dx*dx+dy*dy)))
        distance = math.hypot(star['x']-a[0]-u*dx, star['y']-a[1]-u*dy)
        nearest = min(nearest, (distance, (index + u) / 64))
    return nearest


timing_checks = []
for occupied in [True, False]:
    name = 'void-tower-occupied' if occupied else 'void-tower-empty'
    label = '约定牌堆有牌（3 张）' if occupied else '约定牌堆无牌（0 张）'
    stage(name, seed=occupied)
    prep('KAREN_VOID')
    before = integration(); save('before-transfer', before)
    assert len(before['promise']) == (3 if occupied else 0), before
    assert not before['voidMode'], before
    p = json.loads(cmd('void_orbit_check presentation')['message'])
    assert p['towerAlpha'] > .99 and p['towerHit'], p
    cmd('void_orbit_check watch')
    record_start(); marking(label + ' · 原塔完整显示'); time.sleep(1.2)
    marking('世界上最空虚的人 · 卡牌星星随下扫灯光、塔脚光束传入图标')
    play('KAREN_VOID'); time.sleep(3.2)
    # Draw-pile destruction precedes the tower scan. Wait on the actual scan
    # age instead of assuming both effects always fit one wall-clock duration.
    for _ in range(50):
        p = json.loads(cmd('void_orbit_check presentation')['message'])
        if p['voidMode'] and 2.2 <= p['voidAge'] < 10:
            break
        time.sleep(.2)
    else:
        raise AssertionError(p)
    timeline = json.loads(cmd('void_orbit_check timeline')['message'])
    save('tower-timeline', timeline)
    scan = [s for s in timeline if .14 <= s['voidAge'] <= .98]
    fade = [s for s in timeline if .98 < s['voidAge'] < 1.22]
    retired = [s for s in timeline if s['voidAge'] >= 1.22]
    assert len(scan) >= 20 and all(s['towerAlpha'] > .99 for s in scan), scan
    assert len(fade) >= 5 and any(.05 < s['towerAlpha'] < .95 for s in fade), fade
    assert retired and all(s['towerAlpha'] < .01 for s in retired), retired
    assert any(.7 <= s['voidAge'] <= .9 and s['iconAlpha'] < .01 for s in timeline), timeline
    assert any(s['voidAge'] >= 1.3 and s['iconAlpha'] > .99 for s in timeline), timeline
    assert all(a['towerAlpha'] >= b['towerAlpha'] for a, b in zip(timeline, timeline[1:])), timeline
    beam_errors = []
    if occupied:
        waiting = [s for s in timeline if .14 <= s['voidAge'] <= .69]
        riding = [s for s in timeline if .76 <= s['voidAge'] <= .99]
        assert len(waiting) >= 20 and len(riding) >= 12
        for sample in waiting:
            assert len(sample['cardStars']) == 3
            end = sample['beams'][0]
            assert all(math.hypot(star['x']-end['endX'], star['y']-end['endY']) > 100
                       for star in sample['cardStars']), sample
        for sample in riding:
            assert len(sample['cardStars']) == 3
            head = max(0, min(1, (sample['voidAge']-.72)/.29))
            for star in sample['cardStars']:
                distance, progress = on_beam(star, sample['beams'][star['slot'] % 2])
                assert distance < 1 and progress <= head + .02 and star['light'] > .9, (sample, distance, progress)
                beam_errors.append(distance)
    else:
        assert all(not s['cardStars'] for s in timeline)
    p = json.loads(cmd('void_orbit_check presentation')['message']); save('retired-presentation', p)
    assert p['voidMode'] and p['towerAlpha'] < .01 and not p['towerHit'], p
    after = settle('after-transfer')
    assert cmd('void_orbit_check')['status'] == 'ok'
    assert after['draw'] == before['promise'] and not after['promise'], (before, after)
    marking(label + (' · 库存星光环绕抽牌堆图标' if occupied else ' · 塔退场，图标无库存星光'))
    time.sleep(1.2)
    # Save the final MCP screenshot after capture to keep the transition smooth.
    finish(); snap(case / 'after-transfer')
    timing_checks.append(dict(case=name, passed=True, scanFrames=len(scan), fadeFrames=len(fade),
                              retiredFrames=len(retired), firstFadeAge=next(s['voidAge'] for s in timeline if s['towerAlpha'] < .999999),
                              fullyHiddenAge=next(s['voidAge'] for s in timeline if s['towerAlpha'] == 0),
                              promiseBefore=len(before['promise']), drawAfter=len(after['draw']),
                              cardBeamSamples=len(beam_errors), maxCardBeamError=max(beam_errors, default=0)))
(out / 'void-tower-timing-checks.json').write_text(json.dumps(timing_checks, ensure_ascii=False, indent=2), encoding='utf-8')
print('VOID TOWER TIMING', len(timing_checks), 'passed', flush=True)
