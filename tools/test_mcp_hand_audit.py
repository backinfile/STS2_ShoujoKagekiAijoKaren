"""Regression cases minimized from the A1 floor 21 / floor 39 MCP traces."""
import json
import unittest
from unittest.mock import patch
from mcp_hand_audit import wait_for_hand

HAND = {"state_type": "monster", "battle": {"is_play_phase": True}}
GOOD = {"status": "ok", "message": json.dumps(dict(modelCount=5, visualCount=5, missing=[], unexpected=[]))}
BAD = {"status": "error", "message": json.dumps(dict(modelCount=0, visualCount=5, missing=[], unexpected=["KAREN_FALL"]))}


class Replay:
    def __init__(self, states, audits):
        self.states = iter(states)
        self.audits = iter(audits)
        self.state = states[-1]
        self.audit = audits[-1] if audits else None
        self.posts = []
    def __call__(self, body):
        if body is None:
            self.state = next(self.states, self.state)
            return self.state
        self.posts.append(body)
        self.audit = next(self.audits, self.audit)
        return self.audit


class WaitTests(unittest.TestCase):
    def run_wait(self, replay):
        now = [0.0]
        def sleep(seconds): now[0] += seconds
        with patch("mcp_hand_audit.time.monotonic", side_effect=lambda: now[0]), patch("mcp_hand_audit.time.sleep", side_effect=sleep):
            return wait_for_hand(replay, timeout=1, interval=.1)
    def test_selection_opens_after_initial_play_phase(self):
        r = Replay([HAND, {"state_type": "hand_select"}], [{"status": "error", "message": "Finish card selection before checking the hand."}])
        self.assertEqual(self.run_wait(r)["kind"], "selection")
    def test_combat_ends_between_state_read_and_hand_audit(self):
        r = Replay([HAND, {"state_type": "monster"}, {"state_type": "rewards"}], [BAD])
        self.assertEqual(self.run_wait(r)["kind"], "left_combat")
        self.assertEqual(len(r.posts), 1)
    def test_empty_battle_during_exit_is_repolled(self):
        r = Replay([{"state_type": "monster", "battle": None}, {"state_type": "rewards"}], [])
        self.assertEqual(self.run_wait(r)["kind"], "left_combat")
        self.assertFalse(r.posts)
    def test_mismatch_recovers_after_visuals_finish(self):
        r = Replay([HAND], [BAD, GOOD, GOOD])
        self.assertEqual(self.run_wait(r)["kind"], "hand")
        self.assertEqual(len(r.posts), 3)
    def test_busy_action_queue_does_not_count_as_a_hand_failure(self):
        r = Replay([HAND], [{"status": "error", "message": "Wait for queued actions to finish before checking the hand."}, GOOD, GOOD])
        self.assertEqual(self.run_wait(r)["kind"], "hand")
    def test_persistent_ghost_card_still_fails(self):
        with self.assertRaisesRegex(TimeoutError, "KAREN_FALL"):
            self.run_wait(Replay([HAND], [BAD]))
    def test_overlay_is_not_reported_as_combat_exit(self):
        with self.assertRaisesRegex(RuntimeError, "overlay"):
            self.run_wait(Replay([{ "state_type": "overlay" }], []))
    def test_unknown_errors_are_not_hidden(self):
        with self.assertRaisesRegex(RuntimeError, "Unknown command"):
            self.run_wait(Replay([HAND], [{"status": "error", "error": "Unknown command"}]))


if __name__ == "__main__": unittest.main()
