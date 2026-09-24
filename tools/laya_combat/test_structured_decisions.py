import copy
import json
import os
from pathlib import Path
import unittest
from unittest.mock import patch

from combat import candidates
from decision_input import build_input, questions_for, DIMENSIONS
from jev import JevPolicy


class StructuredTests(unittest.TestCase):
    def setUp(self):
        self.state = json.loads(Path(__file__).with_name('example-en.json').read_text(encoding='utf-8'))
        self.options = candidates(self.state)
        with patch.dict(os.environ, {'TYPESAFE_API_KEY': 'unit-test-secret'}):
            self.policy = JevPolicy()

    def response(self):
        answers = {}
        labels = list(build_input(self.state, self.options)[1])
        for d in DIMENSIONS:
            answers[d] = {'type': 'choice', 'choice': labels[1],
                          'probabilities': {name: int(i == 1) for i, name in enumerate(labels)}}
        return {'answers': answers, 'model': 'jev-1.13.0'}

    def test_composite_batch_maps_shuffled_answers_and_does_not_claim_confidence(self):
        response = self.response()
        response['answers'] = dict(reversed(list(response['answers'].items())))
        with patch.object(self.policy, 'predict', return_value=(response, {})) as call:
            selected, info = self.policy.decide(self.state, self.options)
        self.assertEqual(selected, self.options[1])
        self.assertIsNone(info['confidence'])
        self.assertEqual(info['utility_scores'][next(k for k in info['utility_scores'] if 'Defend' in k)], 1)
        call.assert_called_once()
        self.assertIsInstance(call.call_args.args[0], dict)
        self.assertEqual(len(call.call_args.args[1]), 3)

    def test_invalid_choice_or_distribution_is_rejected(self):
        for field, value in [('choice', 'invented'), ('probabilities', {'invented': 1})]:
            response = self.response()
            response['answers']['survival'][field] = value
            with patch.object(self.policy, 'predict', return_value=(response, {})):
                with self.assertRaises(RuntimeError):
                    self.policy.decide(self.state, self.options)

    def test_deck_grouping_preserves_upgrades_and_unknown_fields_without_mutating_state(self):
        self.state['player']['deck'] = [dict(self.state['player']['hand'][0])] * 2
        self.state['player']['deck'].append(dict(self.state['player']['hand'][0], is_upgraded=True))
        self.state['account_secret'] = 'not-game-data'
        original = copy.deepcopy(self.state)
        payload, _, _ = build_input(self.state, self.options)
        groups = payload['game']['player']['deck_summary']
        self.assertEqual(groups['count'], 3)
        self.assertEqual([x['count'] for x in groups['cards']], [2, 1])
        self.assertNotIn('account_secret', payload['game'])
        self.assertEqual(self.state, original)

    def test_missing_deck_is_explicit_and_snapshot_is_not_reused(self):
        payload, _, _ = build_input(self.state, self.options)
        self.assertIn('not supplied', payload['game']['player']['deck_knowledge'])
        self.state['player']['hp'] = 3
        fresh, _, _ = build_input(self.state, self.options)
        self.assertEqual(fresh['game']['player']['hp'], 3)
        self.assertEqual(payload['game']['player']['hp'], 20)

    def test_oversized_state_stops_before_api(self):
        self.state['player']['mod_rules'] = 'x' * 64000
        with patch.object(self.policy, 'predict') as call:
            with self.assertRaisesRegex(RuntimeError, 'budget'):
                self.policy.decide(self.state, self.options)
        call.assert_not_called()

    def test_single_choice_uses_one_question(self):
        payload, _, strategy = build_input(self.state, self.options)
        questions = questions_for(payload, strategy, 'single')
        self.assertEqual(len(questions), 1)
        self.assertEqual(questions['forward']['type'], 'choice')


if __name__ == '__main__':
    unittest.main()
