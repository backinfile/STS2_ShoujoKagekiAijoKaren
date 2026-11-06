"""Assert the NONNON particle regression at the recorded native-play seam."""
import argparse
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('status', type=Path)
parser.add_argument('--quality', choices=['Full', 'Low', 'Off'], default='Full')
parser.add_argument('--speed', choices=['normal', 'fast', 'instant'], default='normal')
args = parser.parse_args()
status = json.loads(args.status.read_text(encoding='utf-8'))
assert 'maxManualParticles' in status, 'Re-record with the manual particle observer'
count = status['maxManualParticles']
enabled = args.quality != 'Off' and args.speed != 'instant'
assert (count > 0) == enabled, f'NONNON particles: {count}; expected enabled={enabled}'
assert status['spawned'].get('NKarenNononVfx', 0) == int(enabled)
assert count <= (60 if args.quality == 'Low' else 232), f'Particle budget exceeded: {count}'
print(f'NONNON {args.quality}/{args.speed}: manual particles={count}, passed')
