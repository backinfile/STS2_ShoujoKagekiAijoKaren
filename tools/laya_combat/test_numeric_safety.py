import copy, json, unittest
from pathlib import Path
from combat import candidates
from outcomes import outcome

class NumericSafetyTests(unittest.TestCase):
    def state(self):
        return json.loads(Path(__file__).with_name('example-en.json').read_text(encoding='utf-8'))

    def test_target_power_does_not_publish_numeric_lethal_as_fact(self):
        s = self.state()
        s['battle']['enemies'][0]['status'] = [{'name': 'Invincible'}]
        result = outcome(candidates(s)[0], s)
        self.assertNotIn('lethal_by_numbers', result)
        self.assertIn('uncertainty', result)

    def test_multihit_consumes_block_across_hits(self):
        s = self.state()
        s['player']['hand'][0]['description'] = 'Deal 3 damage 2 times.'
        s['battle']['enemies'][0]['block'] = 4
        result = outcome(candidates(s)[0], s)
        self.assertEqual(result['hp_damage_before_triggers'], 2)
        self.assertFalse(result['lethal_by_numbers'])

    def test_player_power_or_relic_disables_exact_outcome(self):
        for key in ('status', 'relics'):
            s = self.state()
            s['player'][key] = [{'name': 'Unknown modifier'}]
            result = outcome(candidates(s)[0], s)
            self.assertNotIn('lethal_by_numbers', result)
            self.assertIn('uncertainty', result)

if __name__ == '__main__':
    unittest.main()
