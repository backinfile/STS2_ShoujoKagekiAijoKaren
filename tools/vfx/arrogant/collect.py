"""Collect the latest completed targeted runs; preserve earlier attempts separately."""
import argparse
import json
import pathlib

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=pathlib.Path, required=True)
root = parser.parse_args().output.resolve()
summary = {}
for branch in ['stable', 'beta']:
    folder = root / branch
    def read(path):
        return json.loads((folder / path).read_text(encoding='utf-8-sig'))
    log = (root / 'isolated' / branch / 'godot.log').read_text(encoding='utf-8-sig')
    assert '[Karen STS2 MCP]' in log
    actions = (folder / 'actions.jsonl').read_text(encoding='utf-8').splitlines()
    start = max(i for i, line in enumerate(actions) if json.loads(line)[0] == dict(action='menu_select', option='continue'))
    (folder / 'final-actions.jsonl').write_text('\n'.join(actions[start:]) + '\n', encoding='utf-8')
    summary[branch] = {
        'normal': read('arrogant-normal/arrogant-results.json'),
        'fast': read('arrogant-fast/arrogant-results.json'),
        'instant': read('arrogant-instant/arrogant-results.json'),
        'repeat': read('arrogant-repeat/arrogant-results.json'),
        'combat_end': read('arrogant-end-cleanup/after-combat-end.json'),
        'relic_rewards': sum(item['type'] == 'relic' for item in read('arrogant-normal/reward-state.json')['rewards']['items']),
        'errors': log.count('ERROR:') + log.count('System.NullReferenceException'),
        'monitor': read('window-monitor.json'),
        'recordings': {scene: read(scene + '/recording.json') for scene in ['arrogant-normal', 'arrogant-fast']}
    }
(root / 'test-summary.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({b: {'errors': value['errors'], 'relic_rewards': value['relic_rewards'],
                       'icon_max_error': max(c['destinationError'] for c in value['normal']['casts'] + value['fast']['casts'])}
                  for b, value in summary.items()}, ensure_ascii=False))
