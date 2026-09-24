"""Advertised single-player actions, without deciding which option is best."""
import json
from pathlib import Path

RUN_STRATEGY = Path(__file__).with_name('run-strategy.txt').read_text(encoding='utf-8').strip()
COMBAT = {'monster', 'elite', 'boss', 'hand_select'}


class Flow:
    def __init__(self, character='IRONCLAD', start_run=False, potions=True):
        self.character = character
        self.start_run = start_run
        self.potions = potions
        self.selected = False
        self.selection_pending = False
        self.started = False

    def observe(self, state):
        if state.get('run') and state['state_type'] != 'game_over':
            self.started = True
        if state.get('state_type') not in {'card_select', 'bundle_select'}:
            self.selection_pending = False

    def sent(self, action):
        if action == {'action': 'menu_select', 'option': self.character}:
            self.selected = True
        if action.get('action') in {'select_card', 'select_bundle'}:
            self.selection_pending = True
        elif action.get('action') in {'confirm_selection', 'confirm_bundle_selection',
                                      'cancel_selection', 'cancel_bundle_selection'}:
            self.selection_pending = False

    def candidates(self, state):
        from combat import candidates
        kind = state['state_type']
        if kind in COMBAT:
            return candidates(state, self.potions)
        result = []

        def add(action, detail, **params):
            result.append({'action': dict(action=action, **params), 'detail': detail})

        if kind == 'game_over':
            if self.start_run and not self.started:
                add('menu_select', 'Return to main menu to start the requested run', option='main_menu')
        elif kind == 'menu':
            advertised = {o if isinstance(o, str) else o['name'] for o in state.get('options', [])
                          if isinstance(o, str) or o.get('enabled', True)}
            screen = state.get('menu_screen')
            desired = None
            if screen == 'main':
                desired = 'continue' if 'continue' in advertised else ('singleplayer' if self.start_run else None)
            elif screen == 'singleplayer' and self.start_run:
                desired = 'standard'
            elif screen == 'character_select' and self.start_run:
                desired = 'confirm' if self.selected else self.character
            elif screen == 'tutorial_prompt':
                desired = 'no'
            if desired in advertised:
                add('menu_select', 'Follow requested single-player setup: ' + desired, option=desired)
        elif kind == 'map':
            for item in state['map']['next_options']:
                add('choose_map_node', item, index=item['index'])
        elif kind == 'event':
            data = state['event']
            if data.get('in_dialogue'):
                add('advance_dialogue', 'Continue event dialogue')
            else:
                for item in data.get('options', []):
                    if not item.get('is_locked', False):
                        add('choose_event_option', item, index=item['index'])
        elif kind == 'rewards':
            data = state['rewards']
            for item in data.get('items', []):
                full = len(state['player'].get('potions', [])) >= state['player'].get('max_potion_slots', 3)
                if item.get('type') != 'potion' or not full:
                    add('claim_reward', item, index=item['index'])
            if data.get('can_proceed'):
                add('proceed', 'Leave rewards; unclaimed rewards will be skipped')
        elif kind == 'card_reward':
            data = state[kind]
            for item in data.get('cards', []):
                add('select_card_reward', item, card_index=item['index'])
            if data.get('can_skip'):
                add('skip_card_reward', 'Skip adding a card')
        elif kind in {'shop', 'fake_merchant'}:
            data = state['shop'] if kind == 'shop' else state[kind]['shop']
            if not data.get('error'):
                for item in data.get('items', []):
                    full = len(state['player'].get('potions', [])) >= state['player'].get('max_potion_slots', 3)
                    if item.get('is_stocked') and item.get('can_afford') and not (item.get('category') == 'potion' and full):
                        add('shop_purchase', item, index=item['index'])
                # MCP reads the underlying disabled button while inventory is open.
                # Its proceed action closes that inventory before clicking the button.
                add('proceed', 'Close shop inventory and leave shop')
        elif kind == 'rest_site':
            data = state[kind]
            for item in data.get('options', []):
                if item.get('is_enabled'):
                    add('choose_rest_option', item, index=item['index'])
            if data.get('can_proceed'):
                add('proceed', 'Leave rest site')
        elif kind in {'treasure', 'relic_select'}:
            data = state[kind]
            for item in data.get('relics', []):
                add('claim_treasure_relic' if kind == 'treasure' else 'select_relic', item, index=item['index'])
            if kind == 'treasure' and data.get('can_proceed'):
                add('proceed', 'Leave treasure room')
            if kind == 'relic_select' and data.get('can_skip'):
                add('skip_relic_selection', 'Skip relic')
        elif kind in {'card_select', 'bundle_select'}:
            data = state[kind]
            bundle = kind == 'bundle_select'
            selection_ready = bool(data.get('preview_showing') or self.selection_pending)
            if not selection_ready:
                for item in data.get('bundles' if bundle else 'cards', []):
                    if item.get('is_selected') or item.get('selected') or item.get('is_enabled') is False:
                        continue
                    add('select_bundle' if bundle else 'select_card', item, index=item['index'])
            # MCP can see an enabled top-level confirm control before a card is selected
            # (notably NDeckEnchantSelectScreen). A visible preview is the documented
            # indication that the required selection is complete and awaiting confirm.
            if data.get('can_confirm') and selection_ready:
                add('confirm_bundle_selection' if bundle else 'confirm_selection', 'Confirm selected items')
            if data.get('can_cancel') or data.get('can_skip'):
                add('cancel_bundle_selection' if bundle else 'cancel_selection', 'Cancel preview or skip selection')
        elif kind == 'crystal_sphere':
            data = state[kind]
            for tool in ('big', 'small'):
                if data.get('can_use_' + tool + '_tool') and data.get('tool') != tool:
                    add('crystal_sphere_set_tool', 'Use ' + tool + ' divination tool', tool=tool)
            for cell in data.get('clickable_cells', []):
                add('crystal_sphere_click_cell', cell, x=cell['x'], y=cell['y'])
            if data.get('can_proceed'):
                add('crystal_sphere_proceed', 'Finish divination')
        return result


def label(option):
    return json.dumps(option['action'], ensure_ascii=False, separators=(',', ':'))


def decision_input(state, options):
    # Only send gameplay fields, not arbitrary top-level account/profile metadata.
    fields = {'state_type', 'run', 'player', 'map', 'event', 'rewards', 'card_reward',
              'shop', 'fake_merchant', 'rest_site', 'treasure', 'card_select',
              'bundle_select', 'relic_select', 'crystal_sphere'}
    payload = RUN_STRATEGY + '\nCurrent game state:\n' + json.dumps(
        {k: v for k, v in state.items() if k in fields}, ensure_ascii=False, separators=(',', ':'))
    criteria = {label(o): o['detail'] for o in options}
    question = {'type': 'choice', 'instructions': 'Which available action best improves the chance of completing this run?',
                'criteria': criteria}
    reverse = dict(question, criteria=dict(reversed(list(criteria.items()))))
    return payload, {'forward': question, 'reverse': reverse}, {label(o): o for o in options}
