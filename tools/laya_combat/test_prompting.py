import copy
import json
from pathlib import Path
import unittest
from unittest.mock import patch

from combat import Policy, candidates
from prompting import action_label, battle_text, build_question


class PromptTests(unittest.TestCase):
    def setUp(self):
        self.state = json.loads(Path(__file__).with_name('example.json').read_text(encoding='utf-8'))

    def test_same_name_enemy_targets_remain_distinct(self):
        enemy = copy.deepcopy(self.state['battle']['enemies'][0])
        enemy['entity_id'] = 'TEST_1'
        self.state['battle']['enemies'].append(enemy)
        options = candidates(self.state)
        labels = [action_label(o, self.state) for o in options]
        self.assertEqual(len(labels), len(set(labels)))
        self.assertIn('TEST_0', battle_text(self.state))
        self.assertIn('TEST_1', battle_text(self.state))

    def test_reverse_reorders_semantics_without_remapping_actions(self):
        options = candidates(self.state)
        forward = build_question(options, self.state)['criteria']
        backward = build_question(options, self.state, True)['criteria']
        self.assertEqual(list(forward), list(reversed(backward)))
        self.assertTrue(all('a0' not in label for label in forward))

    def test_selection_prompt_and_extension_resources_are_visible(self):
        self.state['hand_select'] = {'prompt': '选择一张牌消耗', 'cards': self.state['player']['hand'],
                                     'selected_cards': [{'index': 1, 'name': '测试防御'}]}
        self.state['player']['promise_pile'] = [{'name': '自定义牌'}]
        self.state['player']['hand'][0]['shine_current'] = 2
        text = battle_text(self.state)
        for expected in ['选择一张牌消耗', 'Already selected (separate container): 测试防御', '自定义牌', 'shine_current', 'Fixed combat strategy']:
            self.assertIn(expected, text)

    def test_english_game_state_produces_english_prompt(self):
        state = json.loads(Path(__file__).with_name('example-en.json').read_text(encoding='utf-8'))
        text = battle_text(state)
        question = build_question(candidates(state), state)
        self.assertTrue(text.isascii())
        self.assertTrue(json.dumps(question, ensure_ascii=False).isascii())
        self.assertIn('remaining energy 1', text)
        self.assertIn('Seek a safe lethal sequence', text)

    def test_ensemble_maps_probabilities_by_label_not_position(self):
        options = candidates(self.state)
        labels = [action_label(o, self.state) for o in options]
        answers = {
            'forward': {'choice': labels[0], 'probabilities': dict(zip(labels, [.6, .3, .1]))},
            'reverse': {'choice': labels[1], 'probabilities': dict(zip(reversed(labels), [.1, .5, .4]))}}
        policy = Policy.__new__(Policy)
        policy.agent = object()
        with patch.object(policy, 'predict', return_value=({'answers': answers}, {'forward': 300, 'reverse': 300})):
            chosen, info = policy.decide(self.state, options)
        self.assertEqual(chosen['action'], options[0]['action'])
        self.assertFalse(info['order_agreement'])
        self.assertAlmostEqual(info['probabilities'][labels[0]], .5)

    def test_single_action_skips_model(self):
        policy = Policy.__new__(Policy)
        selected, info = policy.decide(self.state, [{'action': {'action': 'end_turn'}}])
        self.assertEqual(selected['action']['action'], 'end_turn')
        self.assertEqual(info['method'], 'only_legal_action')

    def test_duplicate_rules_merge_but_actions_and_distinct_state_remain(self):
        state = json.loads(Path(__file__).with_name('example-en.json').read_text(encoding='utf-8'))
        duplicate = copy.deepcopy(state['player']['hand'][0])
        duplicate['index'] = 2
        state['player']['hand'].append(duplicate)
        text = battle_text(state, candidates(state))
        self.assertIn('Hand 0,2 ', text)
        self.assertEqual(len(build_question(candidates(state), state)['criteria']), len(candidates(state)))
        duplicate['shine_current'] = 1
        text = battle_text(state)
        self.assertNotIn('Hand 0,2 ', text)
        self.assertIn('shine_current', text)

    def test_invalid_probabilities_stop_instead_of_executing(self):
        options = candidates(self.state)
        labels = [action_label(o, self.state) for o in options]
        answer = {'choice': labels[0], 'probabilities': dict(zip(labels, [float('nan'), .5, .5]))}
        policy = Policy.__new__(Policy)
        policy.agent = object()
        with patch.object(policy, 'predict', return_value=({'answers': {'forward': answer, 'reverse': answer}}, {})):
            with self.assertRaisesRegex(RuntimeError, 'Invalid model probabilities'):
                policy.decide(self.state, options)


if __name__ == '__main__':
    unittest.main()
