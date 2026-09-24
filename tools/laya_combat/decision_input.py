"""Structured Jev inputs and fixed, card-independent evaluation rubrics."""
import copy
import json

from outcomes import outcome
from prompting import STRATEGY, action_label
from flow import COMBAT, RUN_STRATEGY, label


# Deliberately fixed across characters/cards; these are utility weights, not odds.
DIMENSIONS = {
    'survival': (0.55, 'How well does this action preserve health through the current encounter or next visible threat?',
                 ['Clearly worsens survival', 'Likely sacrifices health without enough protection',
                  'Little effect or uncertain survival impact', 'Meaningfully protects health, including by removing threats',
                  'Decisively prevents defeat or safely ends the threat']),
    'progress': (0.30, 'How much useful progress does this action make toward winning the fight or completing the run?',
                 ['Actively undermines progress', 'Negligible benefit for this state',
                  'Modest or uncertain benefit', 'Strong relevant damage, setup, information, or deck improvement',
                  'Decisive relevant progress']),
    'efficiency': (0.15, 'How efficiently does this action convert its costs into useful benefits in this state?',
                   ['Wastes scarce resources or valuable opportunities', 'Poor return for its cost',
                    'Neutral or uncertain return', 'Good return with little waste',
                    'Exceptional return; preserves valuable follow-up options']),
}


def input_budget(payload, questions):
    """Conservative UTF-8 byte guard, NOT an exact Jev token count."""
    size = lambda value: len(json.dumps(value, ensure_ascii=False, separators=(',', ':')).encode('utf-8'))
    state_bytes = size(payload)
    lengths = [size(q) for q in questions.values()]
    return {'state_plus_longest_question_bytes': state_bytes + max(lengths, default=0),
            'request_input_bytes': state_bytes + size(questions)}


def within_budget(budget):
    # Reserve overhead and use bytes conservatively until a matching tokenizer is available.
    return budget['state_plus_longest_question_bytes'] <= 30000 and budget['request_input_bytes'] <= 60000


def group_cards(cards):
    groups = {}
    for card in cards:
        rule = {k: v for k, v in card.items() if k not in {'index', 'combat_id'}}
        key = json.dumps(rule, sort_keys=True, ensure_ascii=False)
        if key not in groups:
            groups[key] = {'count': 0, 'card': rule}
        groups[key]['count'] += 1
    return list(groups.values())


def build_input(state, options):
    combat = state.get('state_type') in COMBAT
    fields = {'state_type', 'run', 'player', 'battle', 'hand_select', 'map', 'event',
              'rewards', 'card_reward', 'shop', 'fake_merchant', 'rest_site', 'treasure',
              'card_select', 'bundle_select', 'relic_select', 'crystal_sphere',
              'menu_screen'}
    game = copy.deepcopy({k: v for k, v in state.items() if k in fields})
    player = game.get('player', {})
    for pile in ('deck', 'draw_pile', 'discard_pile', 'exhaust_pile'):
        if isinstance(player.get(pile), list):
            cards = player.pop(pile)
            player[pile + '_summary'] = {'count': len(cards), 'cards': group_cards(cards),
                                       'order': 'unordered; does not reveal draw order'}
    if 'deck_summary' not in player:
        player['deck_knowledge'] = 'Full permanent deck not supplied. Combat piles are not the permanent deck.'
    actions = {}
    mapping = {}
    for option in options:
        name = action_label(option, state) if combat else label(option)
        if name in mapping:
            raise RuntimeError('Ambiguous action labels; no decision made')
        mapping[name] = option
        actions[name] = {'action': option['action'], 'rules': option['detail']}
        if combat:
            actions[name]['computed'] = outcome(option, state)
    strategy = STRATEGY if combat else RUN_STRATEGY
    return {'game': game, 'actions': actions, 'fixed_strategy': strategy,
            'knowledge_limits': 'Use supplied facts. Unknown is not zero. Do not invent draws or hidden mechanics. '
                                'Computed values describe only the explicitly stated numeric model; inspect its limitations.'}, mapping, strategy


def questions_for(payload, strategy, mode):
    common = {'strategy': 'Apply state.fixed_strategy; current rules override general priorities.',
              'scope': 'Evaluate taking exactly one action now, then reassessing. '
                       'Follow current rules and selection requirements. Treat unknown effects as uncertain, not absent. '
                       'Read computed facts instead of recalculating them. Do not rely on other answers in this request.'}
    if mode in {'single', 'two-orders'}:
        question = {'type': 'choice', 'instructions': dict(common, question='Which available action best follows the fixed strategy?'),
                    'criteria': {name: {'state_reference': f'actions[{json.dumps(name)}]'} for name in payload['actions']}}
        questions = {'forward': question}
        if mode == 'two-orders':
            questions['reverse'] = dict(question, criteria=dict(reversed(list(question['criteria'].items()))))
        return questions
    questions = {}
    for dimension, (_, question, rubric) in DIMENSIONS.items():
        criteria = {name: {'state_reference': f'actions[{json.dumps(name)}]'}
                    for name in payload['actions']}
        questions[dimension] = {
            'type': 'choice',
            'instructions': dict(common, question=question +
                ' Compare all available actions and choose the best one on only this dimension.',
                evaluation_anchors=rubric),
            'criteria': criteria}
    return questions
