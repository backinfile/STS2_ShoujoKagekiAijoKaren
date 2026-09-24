"""Compare Jev decision modes on saved snapshots. Never connects to the game."""
import argparse
import json
from pathlib import Path
import time

from flow import Flow
from jev import JevPolicy


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('states', nargs='+', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    policy = JevPolicy()
    records = []
    try:
        for path in args.states:
            state = json.loads(path.read_text(encoding='utf-8-sig'))
            options = Flow().candidates(state)
            for mode in ('single', 'composite'):
                policy.decision_mode = mode
                start = time.perf_counter()
                chosen, info = policy.decide(state, options)
                row = {'case': path.name, 'mode': mode, 'action': chosen['action'],
                       'seconds': round(time.perf_counter() - start, 3), 'decision': info}
                records.append(row)
                args.output.parent.mkdir(parents=True, exist_ok=True)
                args.output.write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding='utf-8')
                print(json.dumps({k: v for k, v in row.items() if k != 'decision'}), flush=True)
    finally:
        policy.http.close()


if __name__ == '__main__':
    main()
