import copy
import json
from pathlib import Path
import unittest
from combat import candidates, context, execute_if_fresh, Game
from unittest.mock import Mock


class CombatTests(unittest.TestCase):
    def setUp(self):
        self.state = json.loads(Path(__file__).with_name('example.json').read_text(encoding='utf-8'))

    def test_targets_and_unplayable(self):
        self.state['player']['hand'][1]['can_play'] = False
        options = candidates(self.state)
        self.assertEqual([o['action'] for o in options], [
            {'action': 'play_card', 'card_index': 0, 'target': 'TEST_0'}, {'action': 'end_turn'}])

    def test_enemy_turn_and_rewards_never_act(self):
        self.state['battle']['turn'] = 'enemy'
        self.assertEqual(candidates(self.state), [])
        self.state['state_type'] = 'rewards'
        self.assertEqual(candidates(self.state), [])

    def test_stale_state_never_posts(self):
        fresh = copy.deepcopy(self.state)
        fresh['player']['energy'] = 0
        game = Mock(spec=Game)
        game.request.return_value = fresh
        with self.assertRaisesRegex(RuntimeError, 'State changed'):
            execute_if_fresh(game, self.state, {'action': 'end_turn'})
        game.request.assert_called_once_with()

    def test_invalid_action_never_posts(self):
        game = Mock(spec=Game)
        game.request.return_value = self.state
        with self.assertRaisesRegex(RuntimeError, 'no longer available'):
            execute_if_fresh(game, self.state, {'action': 'play_card', 'card_index': 99})
        game.request.assert_called_once_with()

    def test_fresh_action_posts_once(self):
        game = Mock(spec=Game)
        game.request.side_effect = [self.state, {'status': 'ok'}]
        self.assertEqual(execute_if_fresh(game, self.state, {'action': 'end_turn'}), {'status': 'ok'})
        self.assertEqual(game.request.call_count, 2)

    def test_selected_container_indices_do_not_hide_remaining_hand(self):
        self.state.update(state_type='hand_select', hand_select={
            'cards': [{'index': 0}, {'index': 1}], 'selected_cards': [{'index': 0}], 'can_confirm': True})
        self.assertEqual([o['action'] for o in candidates(self.state)], [
            {'action': 'combat_select_card', 'card_index': 0},
            {'action': 'combat_select_card', 'card_index': 1}, {'action': 'combat_confirm_selection'}])

    def test_unknown_target_stops(self):
        self.state['player']['hand'][0]['target_type'] = 'AnyAlly'
        with self.assertRaisesRegex(RuntimeError, 'Unsupported target'):
            candidates(self.state)

    def test_singleplayer_potion_player_target_uses_self(self):
        self.state['player']['potions'] = [{'slot': 1, 'target_type': 'AnyPlayer', 'can_use_in_combat': True}]
        self.assertIn({'action': 'use_potion', 'slot': 1}, [o['action'] for o in candidates(self.state, True)])

    def test_compact_context_keeps_target_rules_and_mod_state(self):
        self.state['player']['hand'][0]['shine_current'] = 0
        self.state['player']['hand'][0]['keywords'] = [{'name': '闪耀', 'description': '规则'}]
        self.state['player']['promise_pile'] = [{'name': '坠落'}]
        compact = context(self.state)
        card = compact['player']['hand'][0]
        self.assertEqual(card['description'], '造成6点伤害。')
        self.assertEqual(card['shine_current'], 0)
        self.assertEqual(card['keywords'][0]['description'], '规则')
        self.assertEqual(compact['battle']['enemies'][0]['entity_id'], 'TEST_0')
        self.assertEqual(compact['player']['promise_pile'], [{'name': '坠落'}])


if __name__ == '__main__':
    unittest.main()
