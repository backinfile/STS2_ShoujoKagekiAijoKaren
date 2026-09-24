"""Jev game runner for STS2 MCP. Python 3.11+."""
import argparse
import hashlib
import json
import math
from pathlib import Path
import sys
import time
from urllib.request import Request, build_opener, ProxyHandler


def dump(value):
    return json.dumps(value, ensure_ascii=False, separators=(",", ":"), sort_keys=True)


def fingerprint(state):
    return hashlib.sha256(dump(state).encode("utf-8")).hexdigest()


class Game:
    def __init__(self, port):
        self.url = f"http://127.0.0.1:{port}/api/v1/singleplayer"
        self.http = build_opener(ProxyHandler({}))

    def request(self, action=None):
        data = None if action is None else dump(action).encode("utf-8")
        request = Request(self.url + ("?format=json" if data is None else ""),
                          data=data, headers={"Content-Type": "application/json"})
        # Never retry POST: a timeout may occur AFTER the game accepted the action.
        with self.http.open(request, timeout=10) as response:
            result = json.load(response)
        if not isinstance(result, dict) or result.get("status") == "error":
            raise RuntimeError(f"MCP error: {result}")
        if action is not None and result.get("status") != "ok":
            raise RuntimeError(f"Unconfirmed action; stopped: {result}")
        return result


def candidates(state, potions=False):
    kind = state.get("state_type")
    result = []
    if kind == "hand_select":
        selection = state["hand_select"]
        # MCP indexes remaining ActiveHolders and SelectedHandCardContainer separately.
        for card in selection.get("cards", []):
            result.append({"action": {"action": "combat_select_card", "card_index": card["index"]},
                           "detail": card})
        if selection.get("can_confirm") is True:
            result.append({"action": {"action": "combat_confirm_selection"}, "detail": "Confirm selection"})
        return result
    battle = state.get("battle", {})
    if kind not in ("monster", "elite", "boss") or battle.get("turn") != "player" or battle.get("is_play_phase") is not True:
        return []
    enemies = [e for e in battle.get("enemies", []) if e.get("hp", 0) > 0 and e.get("entity_id")]

    def add(item, action):
        target_type = item.get("target_type")
        if target_type == "AnyEnemy":
            for enemy in enemies:
                result.append({"action": dict(action, target=enemy["entity_id"]), "detail": item})
        elif target_type in ("None", "Self", "AllEnemies") or (action["action"] == "use_potion" and target_type in ("AnyPlayer", "AnyAlly")):
            result.append({"action": action, "detail": item})
        else:
            # Do not silently end a turn because a mod target type is unsupported.
            raise RuntimeError(f"Unsupported target type {target_type!r}: {item.get('name')}; act manually.")

    for card in state.get("player", {}).get("hand", []):
        if card.get("can_play") is True:
            add(card, {"action": "play_card", "card_index": card["index"]})
    if potions:
        for potion in state.get("player", {}).get("potions", []):
            if potion.get("can_use_in_combat") is True:
                add(potion, {"action": "use_potion", "slot": potion["slot"]})
    result.append({"action": {"action": "end_turn"}, "detail": "End turn; enemies act next."})
    return result


def context(state):
    # Drop pile contents (not their counts) to reduce context length, retain mod fields.
    player = {k: v for k, v in state.get("player", {}).items()
              if k not in ("draw_pile", "discard_pile", "exhaust_pile", "deck")}
    def compact(value):
        if isinstance(value, list):
            return [compact(item) for item in value]
        if isinstance(value, dict):
            return {k: compact(v) for k, v in value.items()
                    if k not in ("id", "combat_id", "rarity", "is_upgraded", "max_energy",
                                 "max_potion_slots", "gold", "title", "label")
                    and v is not None and v != []}
        return value
    return compact({"player": player, "battle": state.get("battle"),
                    "selection": state.get("hand_select"), "run": state.get("run")})


class Policy:
    method = 'jev_choice_two_orders_v6_flow'

    def predict(self, payload, questions):
        raise NotImplementedError('Use JevPolicy')

    def decide(self, state, options):
        if len(options) == 1:
            return options[0], {"method": "only_legal_action", "confidence": None}
        from prompting import STRATEGY, action_label, battle_text, build_question
        from flow import COMBAT, RUN_STRATEGY, decision_input
        strategy = STRATEGY
        if state.get('state_type') in COMBAT:
            payload = battle_text(state, options)
            questions = {'forward': build_question(options, state),
                         'reverse': build_question(options, state, True)}
            labels = {action_label(option, state): option for option in options}
        else:
            strategy = RUN_STRATEGY
            payload, questions, labels = decision_input(state, options)
        if len(labels) != len(options):
            raise RuntimeError('Ambiguous action labels; no decision made')
        result, lengths = self.predict(payload, questions)
        answers = result['answers']
        if not isinstance(answers, dict) or set(answers) != set(questions):
            raise RuntimeError('Invalid model probabilities: missing or unexpected question answers')
        probabilities = {label: 0.0 for label in labels}
        for answer in answers.values():
            scores = answer.get('probabilities', {})
            if set(scores) != set(labels) or answer.get('choice') not in labels:
                raise RuntimeError('Invalid model choices or probability labels')
            if any(not math.isfinite(float(v)) or not 0 <= float(v) <= 1 for v in scores.values()):
                raise RuntimeError('Invalid model probabilities')
            total = sum(scores.values())
            if abs(total - 1) > 0.01:
                raise RuntimeError('Model probabilities do not sum to one')
            for label, value in scores.items():
                probabilities[label] += value / total / len(answers)
        best = max(probabilities, key=probabilities.get)
        ordered = sorted(probabilities.values(), reverse=True)
        entropy = -sum(p * math.log(p) for p in probabilities.values() if p > 0)
        confidence = max(0.0, min(1.0, 1 - entropy / math.log(len(options))))
        return labels[best], {'method': self.method,
                             'confidence': round(confidence, 4),
                             'order_agreement': len({a['choice'] for a in answers.values()}) == 1,
                             'margin': round(ordered[0] - ordered[1], 4),
                             'probabilities': probabilities, 'answers': answers,
                             'input_tokens': lengths, 'prompt': payload, 'questions': questions,
                             'resolved_model': result.get('model'), 'usage': result.get('usage'),
                             'api_timing': result.get('_timing'),
                             'strategy_sha256': hashlib.sha256(strategy.encode('utf-8')).hexdigest()}


class StateChanged(RuntimeError):
    pass


def execute_if_fresh(game, original, action, potions=False, candidate_fn=None):
    fresh = game.request()
    if fingerprint(fresh) != fingerprint(original):
        raise StateChanged("State changed during inference; no action sent. Recollect state.")
    if action not in [c["action"] for c in (candidate_fn(fresh) if candidate_fn else candidates(fresh, potions))]:
        raise RuntimeError("Action is no longer available; no action sent.")
    return game.request(action)


def main():
    from flow import Flow, COMBAT
    from jev import JevPolicy
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--execute', action='store_true')
    parser.add_argument('--loop', action='store_true', help='Continue across rooms and combat')
    parser.add_argument('--start-run', action='store_true', help='Start requested character if no saved run; continue existing save otherwise')
    parser.add_argument('--character', default='IRONCLAD')
    parser.add_argument('--no-potions', action='store_true')
    parser.add_argument('--state-file', type=Path, help='Saved state; calls Jev but never the game')
    parser.add_argument('--inspect', action='store_true', help='List legal candidates without API calls')
    parser.add_argument('--model', default='jev-1.13.0')
    parser.add_argument('--decision-mode', choices=['single', 'composite', 'two-orders'], default='composite')
    parser.add_argument('--port', type=int, default=15526)
    parser.add_argument('--min-confidence', type=float, default=0.0)
    parser.add_argument('--max-actions', type=int, default=200)
    parser.add_argument('--max-battles', type=int, default=0, help='Stop after this many completed battles; 0 means no battle limit')
    parser.add_argument('--log', type=Path, default=Path(__file__).parent / 'logs/jev-decisions.jsonl')
    args = parser.parse_args()
    if args.state_file and args.execute or args.loop and not args.execute:
        parser.error('--state-file cannot execute; --loop requires --execute')
    if not 0 <= args.min_confidence <= 1 or args.max_actions < 1 or args.max_battles < 0:
        parser.error('Invalid confidence or action/battle limit')
    if args.decision_mode == 'composite' and args.min_confidence > 0:
        parser.error('Composite utility has no calibrated confidence; use single mode for --min-confidence')
    game = Game(args.port)
    flow = Flow(args.character, args.start_run, not args.no_potions)
    policy = None
    last_sent = None
    wait_until = time.monotonic() + 30
    in_battle = False
    completed = 0
    stale_count = 0
    args.log.parent.mkdir(parents=True, exist_ok=True)

    def log(record):
        with args.log.open('a', encoding='utf-8') as stream:
            stream.write(dump(dict(time=time.time(), **record)) + '\n')

    def stop(reason, state):
        log({'stopped': reason, 'state': state, 'completed_battles': completed})
        print(reason, flush=True)

    for _ in range(args.max_actions):
        while True:
            state = json.loads(args.state_file.read_text(encoding='utf-8-sig')) if args.state_file else game.request()
            flow.observe(state)
            kind = state['state_type']
            if kind in COMBAT:
                in_battle = True
            elif in_battle and kind in {'rewards', 'map', 'game_over'}:
                completed += 1
                in_battle = False
                log({'battle_finished': completed, 'state': state})
            if kind == 'game_over' and flow.started:
                stop('Run ended.', state)
                return
            if args.max_battles and completed >= args.max_battles:
                stop('Battle limit reached.', state)
                return
            options = flow.candidates(state)
            identity = fingerprint({'state': state, 'actions': [o['action'] for o in options]})
            if options and identity != last_sent:
                break
            blocked_menu = kind == 'menu' and state.get('menu_screen') not in {'main', 'singleplayer', 'character_select', 'tutorial_prompt'}
            if not args.loop or (not options and (kind in {'overlay', 'game_over'} or blocked_menu)):
                stop('No supported action: ' + kind, state)
                return
            if time.monotonic() > wait_until:
                stop('No actionable transition for 30s; stopped without repeating POST.', state)
                return
            time.sleep(0.5)
        if args.inspect:
            print(json.dumps(options, ensure_ascii=False, indent=2))
            return
        if policy is None:
            policy = JevPolicy(args.model, decision_mode=args.decision_mode)
        try:
            decision_started = time.perf_counter()
            chosen, decision = policy.decide(state, options)
            decision['decision_seconds'] = round(time.perf_counter() - decision_started, 4)
            log({'state': state, 'chosen': chosen, 'decision': decision, 'provider': 'jev',
                 'model': args.model, 'execute_requested': args.execute})
            print(json.dumps({'state_type': kind, 'floor': state.get('run', {}).get('floor'),
                              'round': state.get('battle', {}).get('round'),
                              'hp': state.get('player', {}).get('hp'), 'action': chosen['action'],
                              'confidence': decision['confidence'],
                              'decision_seconds': decision['decision_seconds']}, ensure_ascii=False), flush=True)
            if not args.execute:
                return
            if decision['confidence'] is not None and decision['confidence'] < args.min_confidence:
                stop('Below confidence threshold; no action sent.', state)
                return
            post_started = time.perf_counter()
            response = execute_if_fresh(game, state, chosen['action'], candidate_fn=flow.candidates)
            log({'sent': chosen['action'], 'response': response,
                 'verify_post_seconds': round(time.perf_counter() - post_started, 4)})
            flow.sent(chosen['action'])
            stale_count = 0
        except StateChanged:
            stale_count += 1
            log({'discarded_decision': 'State changed before POST', 'consecutive_stale': stale_count})
            if stale_count >= 3:
                stop('State changed during three consecutive decisions; stopped.', game.request())
                return
            last_sent = None
            wait_until = time.monotonic() + 30
            continue
        except Exception:
            log({'stopped': 'Decision or execution error; inspect console. No automatic retry.', 'state': state})
            raise
        last_sent = identity
        if not args.loop:
            return
        wait_until = time.monotonic() + 30
        time.sleep(1.0)
    stop('Action limit reached.', game.request() if not args.state_file else state)


if __name__ == '__main__':
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, 'reconfigure'):
            stream.reconfigure(encoding='utf-8')
    try:
        main()
    except KeyboardInterrupt:
        print('Stopped.')
    except Exception as exc:
        print(f'Stopped: {exc}', file=sys.stderr)
        sys.exit(1)
