import copy
import json
from pathlib import Path
import unittest
from combat import candidates
from outcomes import incoming_damage, outcome, simple_effect


class OutcomeTests(unittest.TestCase):
    def setUp(self):
        self.state = json.loads(Path(__file__).with_name('example-en.json').read_text(encoding='utf-8'))
        self.state['battle']['enemies'][0]['intents'][0]['label'] = '8'

    def test_block_prevents_false_lethal(self):
        option = candidates(self.state)[0]
        self.assertTrue(outcome(option, self.state)['lethal_by_numbers'])
        self.state['battle']['enemies'][0]['block'] = 2
        self.assertFalse(outcome(option, self.state)['lethal_by_numbers'])

    def test_block_marginal_value_not_total_block(self):
        self.state['player']['block'] = 7
        result = outcome(candidates(self.state)[1], self.state)
        self.assertEqual(result['extra_damage_prevented_by_numbers'], 1)
        self.assertEqual(result['excess_block_by_numbers'], 4)
        self.state['player']['block'] = 8
        self.assertEqual(outcome(candidates(self.state)[1], self.state)['extra_damage_prevented_by_numbers'], 0)

    def test_multihit_intent_and_unknown_are_not_guessed(self):
        intent = self.state['battle']['enemies'][0]['intents'][0]
        intent['label'] = '3×4'
        self.assertEqual(incoming_damage(self.state), 12)
        intent['label'] = '?'
        self.assertIsNone(incoming_damage(self.state))

    def test_multiple_enemies_sum_and_dead_enemy_ignored(self):
        other = copy.deepcopy(self.state['battle']['enemies'][0])
        self.state['battle']['enemies'].append(other)
        self.assertEqual(incoming_damage(self.state), 16)
        other['hp'] = 0
        self.assertEqual(incoming_damage(self.state), 8)

    def test_conditional_multihit_and_x_cost_remain_unknown(self):
        for text in ['Deal 6 damage twice.', 'If you have Block, deal 9 damage.', 'Deal 4 damage. Gain 2 Strength.']:
            self.assertEqual(simple_effect({'description': text}), {})
        option = candidates(self.state)[0]
        option['detail']['cost'] = 'X'
        self.assertNotIn('energy_after_cost', outcome(option, self.state))

    def test_draw_does_not_invent_card_identity(self):
        option = candidates(self.state)[0]
        option['detail'].update(cost='0', description='Deal 5 damage. Draw 1 card.')
        result = outcome(option, self.state)
        self.assertEqual(result['energy_after_cost'], 1)
        self.assertEqual(result['drawn_card_identities'], 'unknown')

    def test_power_flag_does_not_claim_certain_kill(self):
        self.state['battle']['enemies'][0]['status'] = [{'name': 'Invincible'}]
        result = outcome(candidates(self.state)[0], self.state)
        self.assertTrue(result['target_has_powers'])
        self.assertIn('not simulated', result['basis'])


if __name__ == '__main__':
    unittest.main()
