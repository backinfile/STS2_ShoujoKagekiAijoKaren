"""Verify recorded gameplay invariants, capture provenance and the A/B deliverable."""
import argparse
import hashlib
import json
import re
from pathlib import Path
from assemble import call, probe
from compare import SCENES, read


def gameplay(state):
    player = state['player']
    return dict(energy=player['energy'], hp=player['hp'], block=player['block'],
        hand=[(c['id'], c['is_upgraded']) if 'is_upgraded' in c else c['id'] for c in player['hand']],
        piles=[player[k] for k in ('draw_pile_count','discard_pile_count','exhaust_pile_count')],
        enemies=[dict(hp=e['hp'], block=e['block'], powers=sorted((p['id'],p['amount']) for p in e['status']))
                 for e in state['battle']['enemies']],
        powers=sorted((p['id'],p['amount']) for p in player['status']))


def verify(root):
    root = root.resolve()
    report = dict(captures=[], handChecks=0, integrationChecks=0, stableGameplayMatches=[], logs=[])
    for phase, branch in [('before','stable'),('after','stable'),('after','beta'),('fast','stable'),('fast','beta')]:
        folder = root/phase/branch
        scenes = ['guilt','replay','nonon'] if phase == 'fast' else [s[0] for s in SCENES]
        monitor = read(folder/'window-monitor.json')
        assert monitor['foreground_samples'] == monitor['restored_samples'] == 0, folder
        log = (folder/'godot.log').read_text(encoding='utf-8-sig')
        lines = log.splitlines()
        preload_end = next(i for i, line in enumerate(lines) if "Preloading 'Common' Complete:" in line)
        errors = [dict(line=i+1, text=line, beforeCommonPreload=i < preload_end)
                  for i, line in enumerate(lines) if '[ERROR]' in line or line.startswith(('ERROR:', 'SCRIPT ERROR:'))]
        # Preload Invalid Task ID is retained as evidence, never silently classified as a pass.
        report['logs'].append(dict(phase=phase,branch=branch,errorLines=errors))
        assert all(e['beforeCommonPreload'] and e['text'] == 'ERROR: Invalid Task ID' for e in errors), errors
        for scene in scenes:
            case = folder/scene
            rec = read(case/'recording.json')
            assert rec['status'] == 'completed' and rec['audio_source'] == 'game_process', case
            assert rec['width'] == 1280 and rec['height'] == 720, case
            for path in case.glob('*hand.json'):
                hand = json.loads(read(path)['message'])
                assert hand['modelCount'] == hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons'], path
                report['handChecks'] += 1
            for path in case.glob('*state.json'):
                s = read(path)
                if 'identitiesMatch' not in s: continue
                assert s['identitiesMatch'] and s['anchorError'] < .1 and s['flightModelMismatchFrames'] == 0, path
                assert s['entryCount'] == s['flightCount'] == 0, path
                report['integrationChecks'] += 1
            assert (case/'recording-before.png').is_file() and (case/'recording-after.png').is_file()
            final = read(case/'recording-after.json')
            initial = read(case/'recording-before.json')
            if scene == 'guilt': assert initial['battle']['enemies'][0]['hp'] - final['battle']['enemies'][0]['hp'] == 12
            if scene == 'nonon':
                assert initial['battle']['enemies'][0]['hp'] - final['battle']['enemies'][0]['hp'] == 28
                assert next(p['amount'] for p in final['battle']['enemies'][0]['status'] if p['id'] == 'STRENGTH_POWER') == -28
            if scene == 'replay':
                assert final['player']['energy'] == 14
                assert 'KAREN_DROP_FUEL' in read(case/'final-state.json')['shine']
            if scene == 'fuel': assert final['player']['energy'] == 4 and len(final['player']['hand']) == 2
            report['captures'].append(dict(phase=phase,branch=branch,scene=scene,
                fps=rec['capture_fps'],repeatedFrames=rec['frames_repeated'],frames=rec['frames_written'],
                build=rec['captureBuild']))
    for scene, _, _ in SCENES:
        before = root/'before/stable'/scene
        after = root/'after/stable'/scene
        assert gameplay(read(before/'recording-after.json')) == gameplay(read(after/'recording-after.json')), scene
        for pile in ('hand','draw','discard','shine','promise'):
            assert read(before/'final-state.json')[pile] == read(after/'final-state.json')[pile], (scene,pile)
        report['stableGameplayMatches'].append(scene)
    movie = root/'Karen-VFX-before-after-2026-10-13.mp4'
    media = probe(movie)
    video = next(s for s in media['streams'] if s['codec_type']=='video')
    assert (video['width'],video['height'],video['r_frame_rate']) == (1920,720,'30/1')
    manifest = read(root/'comparison-manifest.json')
    assert len(media['chapters']) == len(manifest) == 18
    for entry in manifest:
        for source, expected in zip(entry['sources'],entry['sourceHashes']):
            assert hashlib.sha256(Path(source).read_bytes()).hexdigest() == expected
        assert entry['builds'][0]['assemblySha256'] != entry['builds'][1]['assemblySha256']
    log = call(['ffmpeg','-hide_banner','-threads','2','-i',str(movie),'-af','volumedetect','-f','null','NUL'],root/'comparison-edit/verify-media.log')
    peak = float(re.search(r'max_volume: (-?[\d.]+) dB',log).group(1))
    assert -60 < peak < 0
    report['media'] = dict(duration=float(media['format']['duration']),resolution='1920x720',fps=30,
        chapters=18,audioPeakDb=peak,sha256=hashlib.sha256(movie.read_bytes()).hexdigest())
    (root/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in report.items() if k not in ('captures','logs')},ensure_ascii=False))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--output',type=Path,required=True)
    verify(parser.parse_args().output)
