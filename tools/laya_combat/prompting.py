"""Readable, bounded decision inputs; no simulated or invented card outcomes."""
import json

from pathlib import Path

STRATEGY = Path(__file__).with_name('strategy.txt').read_text(encoding='utf-8').strip()
def effects(items):
    return '; '.join(f"{x.get('name', '?')}({x.get('amount', x.get('counter', ''))}):{x.get('description', '')}"
                    for x in items) or 'none'


def action_label(option, state):
    action, card = option['action'], option['detail']
    kind = action['action']
    if kind == 'end_turn':
        return 'End turn'
    if kind == 'combat_confirm_selection':
        return 'Confirm current selection'
    if kind == 'combat_select_card':
        return f"Select hand {action['card_index']}: {card.get('name', '?')}"
    enemies = state.get('battle', {}).get('enemies', [])
    target_enemy = next((e for e in enemies if e['entity_id'] == action.get('target')), None)
    target = target_enemy['entity_id'] if target_enemy else None
    verb = f"Use potion {action['slot']}" if kind == 'use_potion' else f"Play hand {action['card_index']}"
    return f"{verb}: {card.get('name', '?')}" + (f" on {target}" if target else '')


def enemy_label(enemy, enemies):
    if sum(e['name'] == enemy['name'] for e in enemies) > 1:
        return f"{enemy['name']}({enemy['entity_id']})"
    return enemy['name']


def battle_text(state, options=None):
    player, battle = state.get('player', {}), state.get('battle', {})
    lines = [f"Round {battle.get('round', '?')}. Player HP {player.get('hp')}/{player.get('max_hp')}, "
             f"block {player.get('block', 0)}, remaining energy {player.get('energy', '?')}.",
             'Player powers: ' + effects(player.get('status', []))]
    for enemy in battle.get('enemies', []):
        lines.append(f"Enemy {enemy['name']}({enemy['entity_id']}): HP {enemy['hp']}/{enemy.get('max_hp')}, block {enemy.get('block', 0)}; "
                     f"Intent: {'; '.join(i.get('description') or i.get('label', '?') for i in enemy.get('intents', [])) }; "
                     f"Powers: {effects(enemy.get('status', []))}")
    selection = state.get('hand_select')
    if selection:
        lines.append('Selection requirement: ' + selection.get('prompt', '') +
                     '; Already selected (separate container): ' + ', '.join(c.get('name', '?') for c in selection.get('selected_cards', [])))
    cards = selection.get('cards', []) if selection else player.get('hand', [])
    keywords = {}
    card_groups = {}
    for card in cards:
        signature = json.dumps({k: v for k, v in card.items() if k != 'index'}, sort_keys=True)
        card_groups.setdefault(signature, []).append(card)
    for group in card_groups.values():
        card = group[0]
        indices = ','.join(str(c['index']) for c in group)
        lines.append(f"Hand {indices} {card.get('name')}, cost {card.get('cost', '?')}, "
                     f"{'unplayable' if card.get('can_play') is False else 'available'}: {card.get('description', '')}")
        if card.get('star_cost') is not None:
            lines.append(f"Hand {indices} star cost: {card['star_cost']}")
        for keyword in card.get('keywords', []):
            keywords[keyword['name']] = keyword.get('description', '')
        extra = {k: v for k, v in card.items() if k not in {
            'index', 'id', 'name', 'cost', 'star_cost', 'can_play', 'description', 'keywords',
            'rarity', 'type', 'is_upgraded', 'target_type', 'unplayable_reason'}}
        if extra:
            lines.append('Card extension state: ' + json.dumps(extra, ensure_ascii=False))
    lines.append('Relics: ' + effects(player.get('relics', [])))
    for relic in player.get('relics', []):
        for keyword in relic.get('keywords', []):
            keywords[keyword['name']] = keyword.get('description', '')
    for potion in player.get('potions', []):
        lines.append(f"Potion {potion['slot']} {potion.get('name')}: {potion.get('description', '')}")
    status_sources = player.get('status', []) + player.get('potions', [])
    for enemy in battle.get('enemies', []):
        status_sources += enemy.get('status', [])
    for item in status_sources:
        for keyword in item.get('keywords', []):
            keywords[keyword['name']] = keyword.get('description', '')
    for name, description in keywords.items():
        lines.append(f"Keyword {name}: {description}")
    # Preserve unknown player extensions (e.g. a future MCP promise-pile export).
    known = {'character', 'hp', 'max_hp', 'block', 'energy', 'max_energy', 'hand', 'status',
             'relics', 'potions', 'max_potion_slots', 'gold', 'deck', 'draw_pile', 'discard_pile', 'exhaust_pile'}
    extra = {k: v for k, v in player.items() if k not in known}
    if extra:
        lines.append('Other combat resources: ' + json.dumps(extra, ensure_ascii=False, separators=(',', ':')))
    if options:
        from outcomes import outcome_text
        lines.append('Estimates use displayed numbers only; triggers are not simulated. Outcomes are not guaranteed.')
        summaries = {}
        for option in options:
            summary = outcome_text(option, state)
            if summary:
                action = option['action']
                enemies = battle.get('enemies', [])
                target = next((e for e in enemies if e['entity_id'] == action.get('target')), None)
                reference = ('Hand ' + str(action['card_index']) +
                             ((' on ' + str(target['entity_id'])) if target else '')
                             if action['action'] == 'play_card' else action_label(option, state))
                summaries.setdefault(summary, []).append(reference)
        for summary, references in summaries.items():
            lines.append('; '.join(references) + ': ' + summary)
    lines.append(STRATEGY)
    return '\n'.join(lines)


def build_question(options, state, reverse=False):
    order = list(range(len(options)))
    if reverse:
        order.reverse()
    return {'type': 'choice', 'instructions': 'Which next action best wins this combat while minimizing HP loss?',
            'criteria': {action_label(options[i], state): None for i in order}}
