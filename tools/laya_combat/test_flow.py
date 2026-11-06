import unittest
import tempfile
from pathlib import Path
from unittest.mock import Mock, patch

from combat import Game, execute_if_fresh
from flow import Flow, decision_input


class FlowTests(unittest.TestCase):
    def actions(self, kind, data, player=None):
        state = {'state_type': kind, kind: data, 'player': player or {'potions': [], 'max_potion_slots': 3}}
        return [o['action'] for o in Flow().candidates(state)]

    def test_menu_never_abandons_save_or_opens_settings(self):
        f = Flow(start_run=True)
        state = {'state_type': 'menu', 'menu_screen': 'main',
                 'options': ['continue', 'abandon_run', 'settings', 'quit', 'singleplayer']}
        self.assertEqual(f.candidates(state)[0]['action']['option'], 'continue')

    def test_character_selection_then_confirmation(self):
        f = Flow(start_run=True)
        state = {'state_type': 'menu', 'menu_screen': 'character_select', 'options': ['IRONCLAD', 'confirm']}
        action = f.candidates(state)[0]['action']
        self.assertEqual(action['option'], 'IRONCLAD')
        f.sent(action)
        self.assertEqual(f.candidates(state)[0]['action']['option'], 'confirm')

    def test_game_over_does_not_restart_finished_run(self):
        f = Flow(start_run=True)
        state = {'state_type': 'game_over'}
        self.assertEqual(len(f.candidates(state)), 1)
        f.observe({'state_type': 'map', 'run': {'floor': 1}})
        self.assertEqual(f.candidates(state), [])

    def test_map_and_event_use_advertised_indices(self):
        self.assertEqual(self.actions('map', {'next_options': [{'index': 7}]}),
                         [{'action': 'choose_map_node', 'index': 7}])
        self.assertEqual(self.actions('event', {'options': [{'index': 0, 'is_locked': True}, {'index': 2}]}),
                         [{'action': 'choose_event_option', 'index': 2}])
        self.assertEqual(self.actions('event', {'in_dialogue': True}), [{'action': 'advance_dialogue'}])

    def test_shop_only_available_and_affordable(self):
        items = [{'index': 0, 'is_stocked': True, 'can_afford': False},
                 {'index': 1, 'is_stocked': False, 'can_afford': True},
                 {'index': 2, 'is_stocked': True, 'can_afford': True}]
        self.assertEqual(self.actions('shop', {'items': items, 'can_proceed': True}),
                         [{'action': 'shop_purchase', 'index': 2}, {'action': 'proceed'}])

    def test_full_potions_do_not_claim_unusable_reward(self):
        self.assertEqual(self.actions('rewards', {'items': [{'index': 0, 'type': 'potion'}], 'can_proceed': True},
                                      {'potions': [{}], 'max_potion_slots': 1}), [{'action': 'proceed'}])

    def test_shop_inventory_hides_button_but_proceed_closes_inventory(self):
        data = {'items': [{'index': 0, 'is_stocked': True, 'can_afford': False}], 'can_proceed': False}
        self.assertEqual(self.actions('shop', data), [{'action': 'proceed'}])
        self.assertEqual(self.actions('fake_merchant', {'shop': data}), [{'action': 'proceed'}])

    def test_shop_loading_error_does_not_offer_exit(self):
        self.assertEqual(self.actions('shop', {'items': [], 'can_proceed': False, 'error': 'Inventory not ready'}), [])

    def test_card_reward_skip_and_actual_indices(self):
        self.assertEqual(self.actions('card_reward', {'cards': [{'index': 4}], 'can_skip': True}),
                         [{'action': 'select_card_reward', 'card_index': 4}, {'action': 'skip_card_reward'}])

    def test_preview_confirm_does_not_reselect_cards(self):
        self.assertEqual(self.actions('card_select', {'cards': [{'index': 0}], 'preview_showing': True,
                                                       'can_confirm': True, 'can_cancel': False}),
                         [{'action': 'confirm_selection'}])

    def test_enabled_confirm_without_completed_preview_is_not_offered(self):
        data = {'cards': [{'index': 0}], 'preview_showing': False,
                'can_confirm': True, 'can_cancel': False}
        flow = Flow()
        state = {'state_type': 'card_select', 'card_select': data,
                 'player': {'potions': [], 'max_potion_slots': 3}}
        actions = [o['action'] for o in flow.candidates(state)]
        self.assertEqual(actions, [{'action': 'select_card', 'index': 0}])
        flow.sent(actions[0])
        self.assertEqual([o['action'] for o in flow.candidates(state)],
                         [{'action': 'confirm_selection'}])
        flow.sent({'action': 'confirm_selection'})
        self.assertFalse(flow.selection_pending)

    def test_rest_and_treasure_respect_flags(self):
        self.assertEqual(self.actions('rest_site', {'options': [{'index': 0, 'is_enabled': False},
                                                               {'index': 3, 'is_enabled': True}]}),
                         [{'action': 'choose_rest_option', 'index': 3}])
        self.assertEqual(self.actions('treasure', {'message': 'Opening chest...'}), [])

    def test_noncombat_stale_action_never_posts(self):
        state = {'state_type': 'map', 'map': {'next_options': [{'index': 0}]}}
        game = Mock(spec=Game)
        game.request.return_value = {'state_type': 'map', 'map': {'next_options': [{'index': 1}]}}
        with self.assertRaisesRegex(RuntimeError, 'State changed'):
            execute_if_fresh(game, state, {'action': 'choose_map_node', 'index': 0}, candidate_fn=Flow().candidates)
        game.request.assert_called_once_with()

    def test_run_prompt_preserves_deck_and_excludes_account_metadata(self):
        state = {'state_type': 'card_reward', 'card_reward': {'cards': [{'index': 0}]},
                 'player': {'deck': [{'name': 'Test card'}]}, 'account_secret': 'must-not-send'}
        options = Flow().candidates(state)
        payload, questions, labels = decision_input(state, options)
        self.assertIn('Test card', payload)
        self.assertNotIn('must-not-send', payload)
        self.assertEqual(set(labels), set(questions['forward']['criteria']))

    def test_disabled_menu_during_embark_waits_for_event(self):
        from combat import main
        transition = {'state_type': 'menu', 'menu_screen': 'character_select', 'options': []}
        event = {'state_type': 'event', 'run': {'floor': 1}, 'event': {'options': [{'index': 0}]}}
        game = Mock(spec=Game)
        game.request.side_effect = [transition, event, event, {'status': 'ok'}, event]
        choice = Flow().candidates(event)[0]
        policy = Mock()
        policy.decide.return_value = (choice, {'confidence': None, 'method': 'only_legal_action'})
        with tempfile.TemporaryDirectory() as folder:
            argv = ['combat.py', '--execute', '--loop', '--max-actions', '1', '--log', str(Path(folder) / 'test.jsonl')]
            with patch('sys.argv', argv), patch('combat.Game', return_value=game), \
                 patch('jev.JevPolicy', return_value=policy), patch('combat.time.sleep') as sleep:
                main()
            self.assertEqual([c.args[0] for c in game.request.call_args_list if c.args],
                             [{'action': 'choose_event_option', 'index': 0}])
            sleep.assert_any_call(0.5)

    def test_changed_state_discards_old_decision_and_recollects(self):
        from combat import main
        first = {'state_type': 'map', 'run': {'floor': 1}, 'map': {'next_options': [{'index': 0}]}}
        fresh = {'state_type': 'map', 'run': {'floor': 1}, 'map': {'next_options': [{'index': 3}]}}
        game = Mock(spec=Game)
        game.request.side_effect = [first, fresh, fresh, fresh, {'status': 'ok'}, fresh]
        policy = Mock()
        policy.decide.side_effect = lambda state, options: (options[0], {'confidence': None, 'method': 'only_legal_action'})
        with tempfile.TemporaryDirectory() as folder:
            log = Path(folder) / 'test.jsonl'
            argv = ['combat.py', '--execute', '--loop', '--max-actions', '2', '--log', str(log)]
            with patch('sys.argv', argv), patch('combat.Game', return_value=game), \
                 patch('jev.JevPolicy', return_value=policy), patch('combat.time.sleep'):
                main()
            self.assertEqual([c.args[0] for c in game.request.call_args_list if c.args],
                             [{'action': 'choose_map_node', 'index': 3}])
            self.assertIn('discarded_decision', log.read_text(encoding='utf-8'))


if __name__ == '__main__':
    unittest.main()
