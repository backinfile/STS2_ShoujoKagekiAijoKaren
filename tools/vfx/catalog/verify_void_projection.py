"""Check rendered light in two clear regions of the fixed 1280x720 fixture.

The empty-pile capture is essential: inventory stars cannot stand in for the
two tower-foot beams and the temporary icon-reforming stars there.
"""
import argparse
import json
import pathlib
import subprocess

parser = argparse.ArgumentParser()
source = parser.add_mutually_exclusive_group(required=True)
source.add_argument('--video', type=pathlib.Path)
source.add_argument('--output', type=pathlib.Path)
parser.add_argument('--branch', choices=['stable', 'beta'], default='stable')
args = parser.parse_args()


def light_counts(video, crop):
    x, y, width, height = crop
    raw = subprocess.check_output(['ffmpeg', '-v', 'error', '-i', str(video),
                                   '-vf', f'crop={width}:{height}:{x}:{y}',
                                   '-pix_fmt', 'rgb24', '-f', 'rawvideo', '-'])
    size = width * height * 3
    assert len(raw) % size == 0
    counts = []
    for start in range(0, len(raw), size):
        frame = raw[start:start + size]
        counts.append(sum(min(r, g, b) > 170 and r >= b * .93
                          for r, g, b in zip(frame[0::3], frame[1::3], frame[2::3])))
    return counts


def check(video):
    # Exclude the energy counter, hero HUD, draw icon and count badge.
    beams = light_counts(video, (130, 520, 76, 60))
    reform = light_counts(video, (6, 592, 68, 68))
    result = dict(video=str(video), beamPeak=max(beams), reformPeak=max(reform),
                  beamFrames=sum(n >= 40 for n in beams), reformFrames=sum(n >= 12 for n in reform))
    print(json.dumps(result), flush=True)
    assert result['beamFrames'] >= 3, 'Missing visible tower-foot light transmission'
    assert result['reformFrames'] >= 3, 'Missing visible stars gathering above the icon'
    return result


if args.video:
    check(args.video)
else:
    folder = args.output.resolve() / args.branch
    results = [check(folder / case / 'game.mp4')
               for case in ['void-tower-occupied', 'void-tower-empty']]
    (folder / 'void-projection-pixel-checks.json').write_text(
        json.dumps(results, ensure_ascii=False, indent=2), encoding='utf-8')
