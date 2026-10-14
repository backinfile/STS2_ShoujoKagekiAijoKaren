"""Verify the seven requested revisions against real-game evidence and separate clips."""
import argparse
import hashlib
import json
import re
from pathlib import Path
from assemble import call, probe

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
root = args.output.resolve()
names = ['relic-pick', 'relic-crime', 'starlight-act2', 'ticket', 'dream', 'nonon', 'shine-play']
repo = Path(__file__).resolve().parents[3]
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
read = lambda path: json.loads(path.read_text(encoding='utf-8'))
pack_sha = sha(repo / 'artifacts/ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck')
report = dict(versions={}, clips=[])
for branch in ['stable', 'beta']:
    dll_sha = sha(repo / f'artifacts/ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.{branch.title()}.dll')
    results = {}
    for name in names:
        folder = root / branch / name
        capture = read(folder / 'recording.json')
        assert capture['status'] == 'completed' and capture['audio'] and capture['audio_source'] == 'game_process'
        assert capture['captureBuild'] == dict(assemblySha256=dll_sha, packSha256=pack_sha)
        assert capture['capture_fps'] >= 20, capture
        hands = 0
        for path in folder.glob('*hand.json'):
            hand = json.loads(read(path)['message'])
            assert hand['modelCount'] == hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons'], path
            hands += 1
        assert (folder / 'recording-before.png').exists() and (folder / 'recording-after.png').exists()
        result = read(folder / 'revision-probe.json')
        stage = read(folder / 'stage-probe.json')
        assert result['liveAbsorbs'] == stage['live'] == stage['allLive'] == stage['backLive'] == stage['hudLive'] == 0
        assert result['missingPlayFrames'] == result['falseWarningFrames'] == 0, (branch, name, result)
        if name in ['relic-pick', 'relic-crime']:
            destination = 'Energy' if name == 'relic-pick' else 'DrawPile'
            assert len(result['absorbs']) == 3
            assert all(s['destination'] == destination and s['landings'] == 6 and s['landingError'] < .1 and s['canvasError'] < .1 for s in result['absorbs'])
        if name == 'dream':
            assert result['dreamCast'] == 0 and result['dreamTrigger'] == 1
            assert 'KAREN_WE_KOWN_TOKEN' in read(folder / 'final-state.json')['promise']
        if name == 'starlight-act2':
            assert result['castLayerFrames'] > 0 and result['playWarningFrames'] > 0
            assert all(c['backCorrect'] for c in stage['casts'])
            hover = next(c for c in stage['casts'] if c['kind'] == 'Act2Hover')
            assert hover['playIndices'] == [0, 1, 2] and hover['depletionHeldFrames'] > 0
        if name == 'ticket':
            assert any(c['kind'] == 'TicketCast' and c['confirmed'] for c in stage['casts'])
            assert any(c['kind'] == 'TicketReturn' and c['cards'] == 1 for c in stage['casts'])
        if name == 'shine-play':
            assert result['handWarningFrames'] > 0 and result['playWarningFrames'] > 0
        results[name] = dict(handChecks=hands, captureFps=capture['capture_fps'], probe=result)
    window = read(root / branch / 'window-monitor.json')
    assert window['foreground_samples'] == window['restored_samples'] == 0
    log = (root / branch / 'godot.log').read_text(encoding='utf-8')
    errors = [line for line in log.splitlines() if line.startswith('ERROR:') or 'Exception:' in line]
    if branch == 'stable':
        assert not errors, errors
    else:
        assert all(line == 'ERROR: Invalid Task ID' for line in errors), errors
        assert max(log.find(line) for line in errors or ['']) < log.index("Preloading 'Common' Complete:")
    report['versions'][branch] = dict(scenes=results, window=window, startupErrors=errors)

for name in names:
    folder = root / 'clips' / name
    movie = folder / f'Karen-{name}.mp4'
    data = probe(movie)
    video = next(s for s in data['streams'] if s['codec_type'] == 'video')
    audio = next(s for s in data['streams'] if s['codec_type'] == 'audio')
    assert (video['width'], video['height'], video['r_frame_rate']) == (1280, 816, '30/1')
    assert audio['codec_name'] == 'aac' and audio['channels'] == 2 and len(data['chapters']) == 1
    manifest = read(folder / 'manifest.json')
    assert len(manifest) == 1 and manifest[0]['name'] == name
    assert sha(Path(manifest[0]['source'])) == manifest[0]['sha256']
    log = call(['ffmpeg', '-hide_banner', '-threads', '2', '-i', str(movie), '-vn', '-af', 'volumedetect', '-f', 'null', 'NUL'], folder / 'audio-check.log')
    peak = float(re.search(r'max_volume: (-?[\d.]+) dB', log).group(1))
    assert -60 < peak < 0, peak
    errors = call(['ffmpeg', '-v', 'error', '-xerror', '-threads', '2', '-i', str(movie), '-fps_mode', 'passthrough', '-enc_time_base', '-1', '-f', 'null', 'NUL'], folder / 'decode-check.log')
    assert not errors.strip(), errors
    report['clips'].append(dict(name=name, path=str(movie), duration=float(data['format']['duration']), audioPeakDb=peak, fullDecode=True, sha256=sha(movie)))
(root / 'verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(dict(versions={b: sum(s['handChecks'] for s in r['scenes'].values()) for b, r in report['versions'].items()}, clips=report['clips']), ensure_ascii=False))
