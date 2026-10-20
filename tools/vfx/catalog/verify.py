"""Check final media, source provenance, hand checks, chapters and audio."""
import argparse
import hashlib
import json
import pathlib
import re
import subprocess
from assemble import ENTRIES, CURRENT_ENTRIES, LATEST_ENTRIES, probe, call

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=pathlib.Path, required=True)
parser.add_argument('--branch', default='stable', choices=['stable', 'beta'])
parser.add_argument('--catalog', choices=['legacy', 'current', 'latest'], default='legacy')
args = parser.parse_args()
root = args.output.resolve()
entries = {'legacy':ENTRIES,'current':CURRENT_ENTRIES,'latest':LATEST_ENTRIES}[args.catalog]
date = {'legacy':'2026-10-12','current':'2026-10-14','latest':'2026-10-20'}[args.catalog]
movie = root / f'Karen-VFX-catalog-{date}.mp4'
manifest = json.loads((root / 'manifest.json').read_text(encoding='utf-8'))
data = probe(movie)
assert len(manifest) == len(entries) == len(data['chapters'])
assert [e['name'] for e in manifest] == [e[0] for e in entries]
video = next(s for s in data['streams'] if s['codec_type'] == 'video')
audio = next(s for s in data['streams'] if s['codec_type'] == 'audio')
assert (video['width'], video['height'], video['r_frame_rate']) == (1280, 816, '30/1')
assert audio['codec_name'] == 'aac' and audio['channels'] == 2
assert abs(float(data['format']['duration']) - manifest[-1]['end']) < .2
sources = []
for entry, chapter in zip(manifest, data['chapters']):
    source = pathlib.Path(entry['source'])
    assert hashlib.sha256(source.read_bytes()).hexdigest() == entry['sha256']
    assert abs(float(chapter['start_time']) - entry['start']) < .002
    assert chapter['tags']['title'].startswith(f"{entry['index']:02} ")
    log = call(['ffmpeg', '-hide_banner', '-threads', '2', '-i', str(source), '-vf', 'freezedetect=n=-50dB:d=1', '-af', 'volumedetect', '-f', 'null', 'NUL'], root / 'edit' / f"{entry['index']:02}-av.log")
    peak = float(re.search(r'max_volume: (-?[\d.]+) dB', log).group(1))
    assert peak > -60, (source, peak)
    sources.append(dict(index=entry['index'], peakDb=peak, captureFps=entry['captureFps'], repeatedFrames=entry['repeatedFrames'], freezeStarts=re.findall(r'freeze_start: ([\d.]+)', log)))
hand_checks = 0
for entry in manifest:
    folder = pathlib.Path(entry['source']).parent
    for path in folder.glob('*hand.json'):
        hand = json.loads(json.loads(path.read_text(encoding='utf-8'))['message'])
        assert hand['modelCount'] == hand['visualCount']
        assert not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons'], path
        hand_checks += 1
for path in (root / args.branch).rglob('window-monitor.json'):
    window = json.loads(path.read_text(encoding='utf-8'))
    assert window['foreground_samples'] == window['restored_samples'] == 0, path
log = call(['ffmpeg', '-hide_banner', '-threads', '2', '-i', str(movie), '-af', 'volumedetect', '-vn', '-f', 'null', 'NUL'], root / 'edit' / 'final-audio.log')
peak = float(re.search(r'max_volume: (-?[\d.]+) dB', log).group(1))
assert -60 < peak < 0
decode_errors = call(['ffmpeg', '-hide_banner', '-v', 'error', '-xerror', '-threads', '2', '-i', str(movie),
                     '-fps_mode', 'passthrough', '-enc_time_base', '-1', '-f', 'null', 'NUL'], root / 'edit' / 'final-decode.log')
assert not decode_errors.strip(), decode_errors
report = dict(sections=len(entries), duration=float(data['format']['duration']), resolution='1280x816', fps=30, handChecks=hand_checks, audioPeakDb=peak, fullDecode=True, sources=sources, sha256=hashlib.sha256(movie.read_bytes()).hexdigest())
(root / 'verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k: v for k, v in report.items() if k != 'sources'}, ensure_ascii=False))
