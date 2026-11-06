"""Conservative arithmetic from displayed rules; not a combat simulator."""
import re


def incoming_damage(state):
    total = 0
    for enemy in state.get('battle', {}).get('enemies', []):
        if enemy.get('hp', 0) <= 0:
            continue
        intents = enemy.get('intents', [])
        if not intents:
            return None
        for intent in intents:
            kind = intent.get('type', '')
            if kind == 'Attack':
                match = re.fullmatch(r'(\d+)(?:\s*[x×]\s*(\d+))?', str(intent.get('label', '')).strip())
                if not match:
                    return None
                total += int(match[1]) * int(match[2] or 1)
            elif kind not in {'Buff', 'Debuff', 'Defend', 'Sleep', 'Stun', 'Escape', 'Unknown'}:
                return None
            elif kind == 'Unknown':
                return None
    return total


def simple_effect(card):
    text = card.get('description', '').strip()
    attack = re.fullmatch(r'Deal (\d+) damage(?: (\d+) times)?\.(?:\s+Draw (\d+) cards?\.)?', text, re.I)
    block = re.fullmatch(r'Gain (\d+) Block\.', text, re.I)
    if attack:
        return {'damage': int(attack[1]), 'hits': int(attack[2] or 1), 'draw': int(attack[3] or 0)}
    if block:
        return {'block': int(block[1])}
    return {}


def outcome(option, state):
    action = option['action']
    player = state.get('player', {})
    energy = player.get('energy')
    incoming = incoming_damage(state)
    current_block = player.get('block', 0)
    result = {'basis': 'displayed_numbers_only; triggers and unknown effects are not simulated; '
                      'card preview may already include attacker modifiers, so do not apply them again'}
    uncertain = bool(player.get('status') or player.get('relics') or
                     any(e.get('status') for e in state.get('battle', {}).get('enemies', [])))
    if uncertain:
        result['uncertainty'] = 'Powers/relics present. Target modifiers, rounding and triggers are not modeled; exact HP outcome unknown.'
    if action['action'] == 'end_turn':
        result['unused_energy'] = energy
        if incoming is not None:
            result['unblocked_intent_damage' if not uncertain else 'baseline_unblocked_intent_damage'] = max(0, incoming - current_block)
        result['playable_cards_remaining'] = sum(c.get('can_play') is True for c in player.get('hand', []))
        return result
    if action['action'] != 'play_card':
        return result
    card = option['detail']
    cost = str(card.get('cost', ''))
    if cost.isdigit() and isinstance(energy, (int, float)):
        result['energy_after_cost'] = energy - int(cost)
    parsed = simple_effect(card)
    if not parsed:
        result['effect'] = 'unknown; read card rules'
        return result
    if 'damage' in parsed:
        enemy = next((e for e in state.get('battle', {}).get('enemies', [])
                      if e.get('entity_id') == action.get('target')), None)
        if enemy:
            result['displayed_damage'] = parsed['damage']
            result['hits'] = parsed['hits']
            hp_damage = max(0, parsed['damage'] * parsed['hits'] - enemy.get('block', 0))
            result['baseline_hp_damage' if uncertain else 'hp_damage_before_triggers'] = hp_damage
            if uncertain:
                result['baseline_lethal_before_unmodeled_effects'] = hp_damage >= enemy['hp']
            if not uncertain:
                result['lethal_by_numbers'] = hp_damage >= enemy['hp']
            # Even a positive numeric test is not proof of a kill under arbitrary mod hooks.
            result['target_has_powers'] = bool(enemy.get('status'))
        if parsed['draw']:
            result['cards_drawn'] = parsed['draw']
            result['drawn_card_identities'] = 'unknown'
    if 'block' in parsed:
        result['displayed_block'] = parsed['block']
        if incoming is not None and not uncertain:
            result['extra_damage_prevented_by_numbers'] = min(parsed['block'], max(0, incoming - current_block))
            result['excess_block_by_numbers'] = parsed['block'] - result['extra_damage_prevented_by_numbers']
    return result


def outcome_text(option, state):
    data = outcome(option, state)
    bits = []
    if 'energy_after_cost' in data:
        bits.append(f"energy left={data['energy_after_cost']}")
    if 'lethal_by_numbers' in data:
        bits.append(f"HP damage={data['hp_damage_before_triggers']}")
        bits.append('numerical LETHAL' if data['lethal_by_numbers'] else 'nonlethal')
        if data['target_has_powers']:
            bits.append('target powers may change outcome')
    if 'extra_damage_prevented_by_numbers' in data:
        bits.append(f"extra damage prevented={data['extra_damage_prevented_by_numbers']}")
        bits.append(f"excess block={data['excess_block_by_numbers']}")
    if 'cards_drawn' in data:
        bits.append(f"draw {data['cards_drawn']} unknown card(s)")
    if 'unused_energy' in data:
        bits.append(f"unused energy={data['unused_energy']}")
        bits.append(f"playable cards left={data['playable_cards_remaining']}")
    if 'unblocked_intent_damage' in data:
        bits.append(f"unblocked intent damage={data['unblocked_intent_damage']}")
    if 'effect' in data:
        bits.append(data['effect'])
    return '; '.join(bits)


def option_hint(option, state):
    """Short factual criteria description; full qualifications remain in the state."""
    data = outcome(option, state)
    if data.get('lethal_by_numbers'):
        return 'Numerical lethal; check triggers.'
    if 'hp_damage_before_triggers' in data:
        return f"HP damage estimate {data['hp_damage_before_triggers']}."
    if 'extra_damage_prevented_by_numbers' in data:
        return f"Extra damage prevented estimate {data['extra_damage_prevented_by_numbers']}."
    if 'unblocked_intent_damage' in data:
        return f"Take {data['unblocked_intent_damage']} intent damage before triggers."
    return None
