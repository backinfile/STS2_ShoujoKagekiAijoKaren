"""Verify the final package matches all recorded tests and decoded media."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
root = args.output.resolve()
package = root.parent / 'ShoujoKagekiAijoKaren'
expected = {'ordinary', 'last', 'upgrade', 'fast', 'instant', 'pose', 'replay', 'lethal'}
summary = {}
for branch in ['stable', 'beta']:
    out = root / branch
    results = json.loads((out / 'results.json').read_text(encoding='utf-8'))
    sha = hashlib.sha256((package / f'lib/ShoujoKagekiAijoKaren.{branch.title()}.dll').read_bytes()).hexdigest()
    assert {r['case'] for r in results} == expected
    assert all(r['passed'] and r['assemblySha256'] == sha for r in results), 'Stale result from another build'
    assert all(r['metrics']['backLive']==0 for r in results)
    assert all(r['metrics']['particleLive']==0 for r in results)
    build = json.loads((out / 'build.json').read_text(encoding='utf-8'))
    assert build['pckSha256'] == hashlib.sha256((package / 'ShoujoKagekiAijoKaren.pck').read_bytes()).hexdigest()
    log = (out / 'godot.log').read_text(encoding='utf-8', errors='replace')
    boundary = log.index('DevConsole: memory_probe speed normal')
    def errors(text):
        return [line for line in text.splitlines() if 'ERROR:' in line or '[ERROR]' in line or 'Exception:' in line]
    startup_errors, combat_errors = errors(log[:boundary]), errors(log[boundary:])
    assert not combat_errors, combat_errors[:10]
    # Retain and report the observed native preload error; never call this a clean log.
    if startup_errors:
        assert branch == 'beta' and startup_errors == ['ERROR: Invalid Task ID'], startup_errors
        error_at = log.index(startup_errors[0])
        assert log.index("Preloading 'Common' assets") < error_at < log.index("Preloading 'Common' Complete"), error_at
    effects = [e for r in results for e in r['metrics']['effects']]
    recordings = {}
    for name in ['ordinary', 'last']:
        rec = json.loads((out / name / 'recording.json').read_text(encoding='utf-8'))
        assert rec['status'] == 'completed' and rec['audio_source'] == 'game_process'
        # Decode action samples as a supplement to manual motion review.
        # Changing frame hashes alone are not a visual-quality verdict.
        video = out / name / 'game.mp4'
        hashes = subprocess.run(['ffmpeg', '-v', 'error', '-ss', '0.8', '-t', '1.5', '-i', str(video),
            '-an', '-f', 'framemd5', '-'], capture_output=True, text=True, check=True).stdout
        unique = len({line.split(',')[-1].strip() for line in hashes.splitlines() if line and not line.startswith('#')})
        assert unique > 15, ('Frozen action capture', name, unique)
        recordings[name] = dict(duration=rec['video_duration_seconds'], captureFps=rec['capture_fps'], uniqueActionFrames=unique)
    final = root / 'preview' / branch / f'Unforgettable-Shine-{branch}.mp4'
    volume = subprocess.run(['ffmpeg', '-hide_banner', '-i', str(final), '-vn', '-af', 'volumedetect', '-f', 'null', 'NUL'], capture_output=True, text=True).stderr
    peak = float(re.search(r'max_volume: ([-\d.]+) dB', volume)[1])
    assert -70 < peak < 0, peak
    stages = root / branch / 'visual-stages/last'
    visual_build = json.loads((stages.parent/'build.json').read_text(encoding='utf-8'))
    assert visual_build['assemblySha256'] == sha and visual_build['pckSha256'] == build['pckSha256']
    stage_files = {}
    for stage in ['01-focus','02-cut','03-shine','04-remain']:
        file = stages/(stage+'.png')
        assert file.stat().st_size > 50000
        stage_files[stage] = dict(sha256=hashlib.sha256(file.read_bytes()).hexdigest(),
            observed=json.loads((stages/(stage+'.json')).read_text(encoding='utf-8')))
    comparison = root / 'preview' / branch / 'reference-vs-game.png'
    assert comparison.is_file()
    summary[branch] = dict(assemblySha256=sha, pckSha256=build['pckSha256'], cases=sorted(expected),
        passed=True, handChecks=len(expected)*2, effects=len(effects), freed=sum(e['Exited'] for e in effects),
        particleEmitters=sum(e['particleEmitters'] for e in effects),
        rewardVfx=sum(e['Kind']=='Reward' for e in effects),
        maxAnchorError=max(e['MaxAnchorError'] for e in effects), startupLogErrors=startup_errors, combatLogErrors=len(combat_errors),
        recordings=recordings, previewPeakDb=peak, preview=str(final.relative_to(root)),
        visualStages=stage_files, comparison=str(comparison.relative_to(root)))
summary['scope'] = 'Targeted single-card singleplayer checks; full matrix, multiplayer and third-party coexistence not run.'
summary['design'] = 'Unmodified ImageGen transparent diagonal blade sprite matching the new reference. Isotropic rendering, hit pivot and 32 rim samples measured from its alpha. Four sparse native particle layers (20 slots). All card-specific VFX sound calls and exclusive WAV removed; exhaust-reward VFX remains removed.'
summary['visualScope'] = 'Age-triggered viewport captures are a separate pass; functional timing checks and final recordings do not perform mid-animation GPU readback. Frame hashes and functional tests do not establish visual fidelity; inspect the comparison images and videos.'
source = (root.parents[1] / 'src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenUnforgottenShineVfx.cs').read_text(encoding='utf-8-sig')
assert not re.search(r'Sound\(|KarenAudioManager|AudioStream', source)
assert not (root.parents[1] / 'ShoujoKagekiAijoKaren/audio/karen_unforgotten_cut.wav').exists()
summary['customSoundCalls'] = 0
summary['spriteSha256'] = hashlib.sha256((root.parents[1] / 'images/vfx/unforgettable/slash-body.png').read_bytes()).hexdigest()
(root / 'summary.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(summary, ensure_ascii=False, indent=2))
