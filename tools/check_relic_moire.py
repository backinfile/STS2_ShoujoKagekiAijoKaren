"""Run local Mi-Ripple on native-resolution relic masters without regeneration.

Candidates and all tool evidence remain separate until visually reviewed.
Use a Python environment containing mi_ripple and Pillow.
"""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image
from mi_ripple.common import load_rgb
from mi_ripple.pipeline import run
from mi_ripple.verify import verify, verify_board

ROOT = Path(__file__).resolve().parents[1]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('destination', type=Path)
    parser.add_argument('--names', nargs='+')
    parser.add_argument('--gentle-on-reject', action='store_true',
                        help='Verify a 25%% notch correction if full correction damages structure')
    args = parser.parse_args()
    sources = sorted(args.source.glob('*.png'))
    if args.names:
        sources = [p for p in sources if p.stem in args.names]
    args.destination.mkdir(parents=True, exist_ok=True)
    review_path = args.destination / 'moire-review.json'
    previous = json.loads(review_path.read_text(encoding='utf-8')) if review_path.exists() else {}
    rows = {row['name']: row for row in previous.get('assets', [])}
    for source in sources:
        dest = args.destination / source.stem
        sidecar = dest / (source.stem + '_restored.json')
        result = json.loads(sidecar.read_text(encoding='utf-8')) if sidecar.exists() else None
        if result is None or result['source_sha256'] != sha(source):
            result = run(source, dest, allow_regen=False, max_regen=0)
        row = dict(name=source.stem, source=str(source), source_sha256=sha(source),
                   outcome=result['outcome'], actions=[s['action'] for s in result['steps']],
                   source_copy=result['final_is_source_copy'], report=str(sidecar),
                   candidate=None, acceptance='pending visual inspection')
        if result.get('final'):
            original = Image.open(source).convert('RGBA')
            filtered = Image.open(result['final']).convert('RGBA')
            assert original.size == filtered.size, (source, 'dimensions changed')
            if args.gentle_on_reject and result['outcome'] == 'delivered_with_verify_failure':
                damage = json.loads((dest / (source.stem + '_verify.json')).read_text(encoding='utf-8'))
                before = load_rgb(source)
                output = np.clip(np.rint(before * 255 * .75 +
                                        np.asarray(filtered.convert('RGB'), dtype=float) * .25),
                                 0, 255).astype('uint8')
                after = output.astype(float) / 255
                flats = [w['box'] for w in damage['flat_windows']]
                structures = [w['box'] for w in damage['structure_windows']]
                verification = verify(before, after, flats, structures)
                board = dest / (source.stem + '_gentle_verify_board.png')
                verify_board(before, after, verification, board, '25% notch correction')
                verification.update(route='25% of deterministic notch correction', board=str(board))
                gentle_report = dest / (source.stem + '_gentle_verify.json')
                gentle_report.write_text(json.dumps(verification, indent=2), encoding='utf-8')
                row.update(gentle_report=str(gentle_report), gentle_passed=verification['passed'])
                if verification['passed']:
                    filtered = Image.fromarray(output).convert('RGBA')
                    row.update(outcome='delivered_gentle_verified', notch_strength=.25)
            # Mi-Ripple operates on RGB. Keep the exact original alpha, including
            # silhouette edges, interior holes and almost-transparent pixels.
            filtered.putalpha(original.getchannel('A'))
            candidate = dest / (source.stem + '_candidate-rgba.png')
            filtered.save(candidate)
            assert original.getchannel('A').tobytes() == filtered.getchannel('A').tobytes()
            row.update(candidate=str(candidate), candidate_sha256=sha(candidate),
                       alpha_unchanged=True, dimensions=list(original.size),
                       pixels_unchanged=filtered.tobytes() == original.tobytes())
        rows[source.stem] = row
        review_path.write_text(
            json.dumps(dict(tool='Mi-Ripple 0.1.0', native_resolution=True,
                            regeneration=False, assets=list(rows.values())), ensure_ascii=False, indent=2),
            encoding='utf-8')
        print(json.dumps(row, ensure_ascii=False), flush=True)


if __name__ == '__main__':
    main()
