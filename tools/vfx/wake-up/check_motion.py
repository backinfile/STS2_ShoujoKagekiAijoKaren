"""Check visible curtain movement in a native recording while the grid stays open.

The right battlefield crop excludes cards, hover outlines and the HUD. Blur removes
small sparkles: those alone must not satisfy a full-screen cloth-motion check.
This checks motion only; likeness to the concept still needs visual review.
"""
import argparse
from io import BytesIO
import json
from pathlib import Path
import subprocess

import numpy as np
from PIL import Image, ImageFilter


def compare_frames(images):
    frames = [np.asarray(frame.convert('RGB').resize((1280, 720))
                         .crop((760, 120, 1240, 570))
                         .filter(ImageFilter.GaussianBlur(5)), dtype=float) for frame in images]
    difference = np.max(np.abs(frames[1] - frames[0]), axis=2)
    return dict(changedArea=float(np.mean(difference > 10)),
                meanChange=float(difference.mean()),
                passed=bool(np.mean(difference > 10) >= .10 and difference.mean() >= 5))


def measure(video, start, end):
    images = []
    for moment in (start, end):
        data = subprocess.check_output([
            'ffmpeg', '-v', 'error', '-ss', str(moment), '-i', str(video),
            '-frames:v', '1', '-f', 'image2pipe', '-vcodec', 'png', '-',
        ])
        images.append(Image.open(BytesIO(data)))
    return dict(compare_frames(images), start=start, end=end)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('video', type=Path)
    parser.add_argument('--start', type=float, default=1.2)
    parser.add_argument('--end', type=float, default=2.2)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    result = measure(args.video, args.start, args.end)
    if args.output:
        args.output.write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(('PASS' if result['passed'] else 'FAIL') +
          f" cloth motion: changed area {result['changedArea']:.1%} (need 10%), " +
          f"mean change {result['meanChange']:.2f} (need 5)")
    raise SystemExit(0 if result['passed'] else 1)
