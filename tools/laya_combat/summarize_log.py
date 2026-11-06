"""Read-only summary of a combat JSONL log; never connects to the game."""
import argparse
import json
from collections import Counter
from pathlib import Path

from outcomes import outcome


def summarize(path):
    records = [json.loads(line) for line in path.read_text(encoding='utf-8-sig').splitlines() if line.strip()]
    decisions = [r for r in records if 'chosen' in r]
    if not decisions:
        return {'log': str(path), 'decisions': 0}
    model = [r for r in decisions if r['decision']['method'] != 'only_legal_action']
    executed = []
    pending = None
    for record in records:
        if 'chosen' in record:
            pending = record
        elif 'sent' in record:
            if pending and record['sent'] == pending['chosen']['action'] and record.get('response', {}).get('status') == 'ok':
                executed.append(pending)
            pending = None
        elif 'discarded_decision' in record or 'stopped' in record:
            pending = None
    premature = []
    zero_block = []
    for record in executed:
        state, chosen = record['state'], record['chosen']
        turn = state.get('battle', {}).get('round')
        if chosen['action']['action'] == 'end_turn' and any(c.get('can_play') for c in state['player'].get('hand', [])):
            premature.append(turn)
        numbers = outcome(chosen, state)
        if numbers.get('extra_damage_prevented_by_numbers') == 0:
            zero_block.append(turn)
    return {
        'log': str(path), 'decisions': len(decisions),
        'successful_posts': sum(r.get('response', {}).get('status') == 'ok' for r in records),
        'actions': dict(Counter(r['chosen']['action']['action'] for r in executed)),
        'start_hp': next((r['state']['player']['hp'] for r in decisions if 'hp' in r['state'].get('player', {})), None),
        'last_decision_hp': decisions[-1]['state'].get('player', {}).get('hp'),
        'state_types': dict(Counter(r['state']['state_type'] for r in executed)),
        'battles_finished': [{'floor': r['state'].get('run', {}).get('floor'),
                              'hp': r['state'].get('player', {}).get('hp'),
                              'result': r['state']['state_type']} for r in records if 'battle_finished' in r],
        'last_round': decisions[-1]['state'].get('battle', {}).get('round'),
        'model_decisions': len(model),
        'order_agreement': sum(r['decision'].get('order_agreement') is True for r in model),
        'order_comparisons': sum(r['decision'].get('order_agreement') is not None for r in model),
        'max_tokens': max((max(r['decision'].get('input_tokens', {}).values(), default=0) for r in model), default=0),
        'api_input_tokens': sum((r['decision'].get('usage') or {}).get('input_tokens', 0) for r in model),
        'end_with_playable_cards_rounds': premature,
        'zero_immediate_block_value_rounds': zero_block,
        'strategy_hashes': sorted({r['decision']['strategy_sha256'] for r in model}),
        'methods': sorted({r['decision']['method'] for r in decisions}),
        'note': 'Actions and behavior counts use confirmed POSTs only. Final outcome requires battle_finished or a separate result snapshot. Block values exclude unmodeled triggers.'
    }


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('logs', nargs='+', type=Path)
    args = parser.parse_args()
    print(json.dumps([summarize(path) for path in args.logs], ensure_ascii=False, indent=2))
