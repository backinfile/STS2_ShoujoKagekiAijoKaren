"""Check final-build evidence, native hand nodes, cleanup, logs and recorded media."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import struct
import sys

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--package', type=Path, help='Exact package associated with archived recordings')
args = parser.parse_args()
root = args.output.resolve()
repo = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(repo / 'tools/vfx/six-card'))
from audit_log import audit

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

report = {}
for branch in ['stable', 'beta']:
    folder = root / branch / 'pile-view'
    results = read(folder / 'results.json')
    assert results['passed'], results
    capture = read(folder / 'recording.json')
    package = (args.package or repo / 'artifacts/ShoujoKagekiAijoKaren').resolve()
    assert capture['captureBuild']['assemblySha256'] == digest(package / 'lib' / f'ShoujoKagekiAijoKaren.{branch.title()}.dll')
    assert capture['captureBuild']['packSha256'] == digest(package / 'ShoujoKagekiAijoKaren.pck')
    assert capture['status'] == 'completed' and capture['audio_source'] == 'game_process', capture
    for speed, duration in [('普通', .64), ('快速', .32), ('瞬时', 0)]:
        for name in ['约定牌堆', '闪耀耗尽牌堆']:
            label = name + '-' + speed
            opening, idle = (read(folder / (label + suffix + '.json')) for suffix in ['-opened', '-idle'])
            assert abs(opening['duration'] - duration) < .001, opening
            assert not idle['processing'] and idle['living'] == idle['decorations'] == 1, idle
            assert idle['backgroundOrder'] < idle['order'] < idle['gridOrder'], idle
            assert idle['mouse'] == 'Ignore' and idle['focus'] == 'None', idle
            assert len(idle['cards']) == (6 if name == '约定牌堆' else 1), idle
            assert idle['cards'] == idle['visibleCards'], idle
            assert (folder / (label + '-opening.png')).exists()
    closed = list(folder.glob('*-closed.json')) + list(folder.glob('rapid-?.json'))
    for path in closed:
        status = read(path)
        assert status['living'] == status['decorations'] == 0 and status['screen'] is None, (path, status)
    hands = list(folder.glob('*hand.json'))
    for path in hands:
        hand = json.loads(read(path)['message'])
        assert hand['modelCount'] == hand['visualCount'] and not hand['missing'] and not hand['unexpected'] and not hand['unexpectedPileIcons'], (path, hand)
    assert read(folder / 'vanilla-control.json')['decorations'] == 0
    requested_sizes = []
    for width, height in [(1600, 900), (1100, 800)]:
        image = folder / f'resize-{width}-idle.png'
        requested_sizes.append(dict(requested=[width, height], screenshot=list(struct.unpack('>II', image.read_bytes()[16:24]))))
    log = (root / branch / 'godot.log').read_text(encoding='utf-8')
    errors = audit(log)
    assert '[Karen STS2 MCP]' in log and not errors['unexpectedErrors'], errors
    monitor = read(root / branch / 'window-monitor.json')
    assert monitor['foreground_samples'] == monitor['restored_samples'] == 0, monitor
    media = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_streams', '-show_format', '-of', 'json', str(folder / 'game.mp4')], text=True))
    video = next(stream for stream in media['streams'] if stream['codec_type'] == 'video')
    assert (video['width'], video['height']) == (1280, 720)
    assert any(stream['codec_type'] == 'audio' for stream in media['streams'])
    report[branch] = dict(checks=[c for c in results['checks'] if c != 'resize'], hand_checks=len(hands), cleanup_checks=len(closed),
                          not_measured=['Actual viewport resize: the hidden-window guard preserves the display'],
                          window_setting_requests=requested_sizes,
                          capture=capture, errors=errors, video_sha256=digest(folder / 'game.mp4'))
(root / 'verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({branch: dict(passed=True, hand_checks=row['hand_checks'], cleanup_checks=row['cleanup_checks'],
       capture_fps=row['capture']['capture_fps'], known_startup_diagnostics=len(row['errors']['knownStartupWorkerPoolErrors']))
       for branch, row in report.items()}, ensure_ascii=False, indent=2))
