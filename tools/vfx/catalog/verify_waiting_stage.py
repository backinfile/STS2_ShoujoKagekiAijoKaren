"""Audit the persisted MCP observations and game-only audio/video evidence."""
import argparse
import hashlib
import json
import pathlib
import re
import subprocess
import sys

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=pathlib.Path, required=True)
parser.add_argument('--lifecycle-output', type=pathlib.Path, required=True)
parser.add_argument('--media-output', type=pathlib.Path, help='Delivery capture root when recording was repeated with a newer MCP')
parser.add_argument('--showcase-only', action='store_true', help='Validate a visual revision using genuine casts, actual gains and lifecycle records')
parser.add_argument('--audio-check', action='store_true', help='Also inspect dedicated cues, fast repeats and interrupted playback')
args = parser.parse_args()
root = args.output.resolve()
repo = pathlib.Path(__file__).resolve().parents[3]
sys.path.insert(0, str(repo / 'tools/vfx/six-card'))
from audit_log import audit
read = lambda p: json.loads(p.read_text(encoding='utf-8'))
results = {}
for branch in ('stable', 'beta'):
    folder = root / branch
    cases = sorted(folder.glob('waiting-stage*/*-waiting.json'))
    assert len(cases) >= (7 if args.showcase_only else 20), (branch, len(cases))
    for path in cases:
        state = read(path)
        assert state['live'] <= 1 and state['back'] <= 1 and state['hud'] <= 1, (path, state)
        for node in state['casts']:
            assert node['anchorError'] < .1 and node['hudCorrect'] and node['belowDigits'], (path, node)
            assert node['countMatches'], node
            assert node['peakDeliveries'] <= 8 and node['peakBursts'] <= 12, (path, node)
            assert node['hexHeadError'] < .1 and node['hexCanvasError'] < .1, node
            assert node['ringError'] < .1 and abs(node['ringWidth'] - 240) < .1 and node['peakStage'] <= .61 and not node['feedbackEarly'], (path, node)
            if node['firstFeedbackAge'] >= 0: assert node['firstFeedbackAge'] >= .16, (path, node)
            if node['casts'] > 0: assert node['peakCast'] > .9, (path, node)
            if node['confirmations'] > 0: assert node['peakTrigger'] > .9, (path, node)
    for path in folder.glob('waiting-stage*/*hand.json'):
        hand = read(path)
        if 'message' in hand: hand = json.loads(hand['message'])
        assert hand['modelCount'] == hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons'], (path, hand)
    normal = folder / 'waiting-stage'
    quiet = read(normal / 'cast-quiet-waiting.json')['casts'][0]
    assert quiet['peakStage'] > .5 and .399 < quiet['stageNow'] < .401 and quiet['minBuild'] < .1 and quiet['maxBuild'] > .99 and quiet['partialFrames'] > 4 and quiet['peakCreationHeads'] == 1 and quiet['headsNow'] == 1, quiet
    assert quiet['hexSidesSeen'] == 63, quiet
    assert read(normal / 'trigger-two-waiting.json')['casts'][0]['energy'] == 3
    assert all(read(normal / 'remove-waiting.json')[k] == 0 for k in ('live', 'back', 'hud'))
    state_poses = {}
    pose_cases = [('occupied-idle',False,1), ('empty-idle',True,0),
                                ('stack-empty-idle',True,0), ('refilled-idle',False,1),
                                ('second-empty-idle',True,0)]
    if args.showcase_only:
        pose_cases += [('three-empty-idle',True,0), ('three-occupied-idle',False,1), ('third-empty-idle',True,0), ('reduced-empty-idle',True,0)]
    for name, empty, count in pose_cases:
        node = read(normal / (name + '-waiting.json'))['casts'][0]
        assert node['pileEmpty'] == empty and node['pileCount'] == count and abs(node['opening'] - int(empty)) < .01, node
        assert abs(node['radius'] - 62) < .1 and abs(node['shaderOpening'] - int(empty)) < .01 and node['buildNow'] > .99 and abs(node['stageNow'] - (.22 if empty else .40)) < .001, node
        assert node['headsNow'] == node['powerLayers'] and node['countMatches'], node
        assert all(abs(x - (.72 if empty else 1.15)) < .01 for x in node['headScales']), node
        assert all(abs(x - (.28 if empty else 1)) < .01 for x in node['headStrengths']), node
        state_poses[name] = dict(empty=empty, count=count, opening=node['opening'], changes=node['stateChanges'], radius=node['radius'], build=node['buildNow'], strength=node['stageNow'], layers=node['powerLayers'], lights=node['headsNow'], lightScale=node['headScales'], lightStrength=node['headStrengths'])
    assert len(read(normal / 'before-refill-waiting.json')['casts'][0]['audioEvents']) == len(read(normal / 'refilled-idle-waiting.json')['casts'][0]['audioEvents'])
    if not args.showcase_only:
        for folder_name, name, empty, count in [('waiting-stage','nonfinal-still-waiting',False,1),
            ('waiting-stage','quiet-restore-occupied',False,1),
            ('waiting-stage-infinite','infinite',False,10), ('waiting-stage-void','void-empty-idle',True,0)]:
            node = read(folder / folder_name / (name + '-waiting.json'))['casts'][-1]
            assert node['pileEmpty'] == empty and node['pileCount'] == count and abs(node['opening'] - int(empty)) < .01, node
        infinite_node = read(folder / 'waiting-stage-infinite/infinite-waiting.json')['casts'][0]
        assert infinite_node['stateChanges'] == read(folder / 'waiting-stage-infinite/infinite-before-waiting.json')['casts'][0]['stateChanges']
    if args.showcase_only:
        fast = read(folder / 'waiting-stage-hex-fast/fast-created-waiting.json')['casts'][0]
        assert fast['hexSidesSeen'] == 63 and fast['hexHeadError'] < .1 and fast['hexCanvasError'] < .1, fast
        assert read(folder / 'waiting-stage-hex-fast/fast-gain-waiting.json')['casts'][0]['confirmations'] == 1
        assert not read(normal / 'hex-ui-hidden-waiting.json')['casts'][-1]['uiVisible']
        assert read(normal / 'hex-ui-restored-waiting.json')['casts'][-1]['uiVisible']
    audio_summary = None
    if args.audio_check:
        audio_node = read(normal / 'second-empty-idle-waiting.json')['casts'][0]
        cues = audio_node['audioEvents']
        expected_cues = [(.94, .28)]  # Only cast audio remains; gains are silent.
        assert len(cues) == 2, cues
        for duration, volume in expected_cues:
            assert sum(abs(c['duration'] - duration) < .002 and abs(c['volume'] - volume) < .002 for c in cues) == 2, cues
        assert all(c['bus'] == 'SFX' and abs(c['pitch'] - 1) < .002 for c in cues), cues
        fast_audio = read(folder / 'waiting-stage-audio/audio-rapid-waiting.json')['casts'][0]
        assert fast_audio['confirmations'] == fast_audio['energy'] == 8 and fast_audio['peakSounds'] <= 4, fast_audio
        assert len(fast_audio['audioEvents']) == 1, fast_audio
        assert all(c['bus'] == 'SFX' and abs(c['pitch'] - 1.6) < .002 for c in fast_audio['audioEvents']), fast_audio
        lifecycle = folder / 'waiting-stage-lifecycle'
        for name in ('interrupted', 'instant-switch', 'power-removal'):
            assert all(read(lifecycle / (name + '-observer.json'))[k] == 0 for k in ('live', 'back', 'hud', 'liveSounds'))
        assert not read(lifecycle / 'restored-observer.json')['casts'][-1]['audioEvents']
        assert read(lifecycle / 'normal-switch-observer.json')['live'] == 1
        audio_summary = dict(normalEvents=cues, peakNormalVoices=audio_node['peakSounds'],
                             fastConfirmations=fast_audio['confirmations'], peakFastVoices=fast_audio['peakSounds'],
                             interruptAndSpeedSwitch='pass')
    rapid = None
    if not args.showcase_only:
        assert read(folder / 'waiting-stage-fast/mode-final-waiting.json')['casts'][0]['confirmations'] == 1
        assert read(folder / 'waiting-stage-instant/mode-final-waiting.json')['live'] == 0
        assert read(folder / 'waiting-stage-infinite/infinite-waiting.json')['casts'][0]['confirmations'] == 0
        rapid = read(folder / 'waiting-stage-rapid/rapid-settled-waiting.json')['casts'][0]
        assert rapid['confirmations'] == rapid['energy'] == 8 and rapid['peakDeliveries'] > 1
        assert read(folder / 'waiting-stage-void/void-final-waiting.json')['casts'][0]['confirmations'] == 1
        assert all(read(folder / 'waiting-stage-void/next-room-waiting.json')[k] == 0 for k in ('live', 'back', 'hud'))
    log_paths = [folder / 'godot.log', folder / 'pre-tail-godot.log', folder / 'tail-godot.log', args.lifecycle_output.resolve() / branch / 'godot.log', (args.media_output or args.output).resolve() / branch / 'godot.log']
    logs = dict(knownStartupWorkerPoolErrors=[], unexpectedErrors=[])
    for log_path in dict.fromkeys(log_paths):
        if not log_path.exists(): continue
        report = audit(log_path.read_text(encoding='utf-8', errors='replace'))
        for key in logs: logs[key] += [dict(item, file=log_path.name) for item in report[key]]
    assert not logs['unexpectedErrors'], logs
    media = (args.media_output or args.output).resolve() / branch
    media_normal = media / 'waiting-stage'
    rec = read(media_normal / 'recording.json')
    capture_audit = read(media / 'capture-audit/capture-check.json') if args.media_output else None
    if args.media_output:
        assert rec.get('encoder') in ('h264_nvenc','libx264') and 'frames_dropped' in rec, rec
    package = repo / 'artifacts/ShoujoKagekiAijoKaren'
    expected = dict(assemblySha256=hashlib.sha256((package / 'lib' / f'ShoujoKagekiAijoKaren.{branch.title()}.dll').read_bytes()).hexdigest(),
                    packSha256=hashlib.sha256((package / 'ShoujoKagekiAijoKaren.pck').read_bytes()).hexdigest())
    assert rec['captureBuild'] == expected and rec['audio'] and rec['audio_source'] == 'game_process'
    assert rec['capture_fps'] >= 20, rec
    audio = subprocess.run(['ffmpeg', '-hide_banner', '-i', str(media_normal / 'game.mp4'), '-vn', '-af', 'volumedetect', '-f', 'null', '-'], capture_output=True, text=True, check=True).stderr
    mean = float(re.search(r'mean_volume: ([\-\d.]+) dB', audio).group(1))
    assert mean > -70, mean
    monitor = read(media / 'window-monitor.json')
    assert monitor['foreground_samples'] == monitor['restored_samples'] == 0, monitor
    if not args.showcase_only or (args.lifecycle_output.resolve() / branch / 'waiting-stage-lifecycle').is_dir():
        lifecycle_folder = args.lifecycle_output.resolve() / branch / 'waiting-stage-lifecycle'
        for name in ('interrupted', 'instant-switch', 'power-removal'):
            cleanup = read(lifecycle_folder / (name + '-observer.json'))
            assert all(cleanup[k] == 0 for k in ('live', 'back', 'hud', 'liveSounds')), cleanup
        assert read(lifecycle_folder / 'interrupted-observer.json')['casts'][0]['confirmations'] == 1
        assert read(lifecycle_folder / 'restored-observer.json')['casts'][-1]['casts'] == 0
        assert read(lifecycle_folder / 'normal-switch-observer.json')['live'] == 1
    creation = folder / 'waiting-stage-creation'
    partial = read(creation / 'creation-interrupted-observer.json')
    assert all(partial[k] == 0 for k in ('live','back','hud','liveSounds')), partial
    assert .25 < partial['casts'][0]['maxBuild'] < .85 and partial['casts'][0]['minBuild'] < .1, partial
    restored_creation = read(creation / 'creation-restored-observer.json')['casts'][-1]
    assert restored_creation['creationStarts'] == 0 and restored_creation['minBuild'] > .99 and restored_creation['peakCreationHeads'] == 0, restored_creation
    results[branch] = dict(captureAudit=capture_audit, recordingEncoder=rec.get('encoder'), statePoses=state_poses, observations=len(cases), captureFps=rec['capture_fps'], meanAudioDb=mean, rapid=rapid,
                           lifecycle='interruption, speed-switch and removal pass' if args.audio_check else ('creation, gain interruption, speed-switch and removal pass' if args.showcase_only else 'pass'), logs=logs, provenance=expected,
                           scope='visual and audio revision' if args.audio_check else ('visual revision' if args.showcase_only else 'targeted mechanics and lifecycle'),
                           audio=audio_summary)
(root / 'verification.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
print(json.dumps(results, indent=2))
