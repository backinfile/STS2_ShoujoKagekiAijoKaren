"""Wait for the real hand check, selection, or combat exit without discarding audit errors."""
from __future__ import annotations
import argparse
import json
import time
from typing import Callable
from urllib.request import ProxyHandler, Request, build_opener

COMBAT = {"monster", "elite", "boss"}
SELECTION = {"hand_select", "card_select"}
AFTER_COMBAT = {"rewards", "map", "event", "rest_site", "treasure", "shop", "game_over"}
WAIT_MESSAGES = {
    "Finish card selection before checking the hand.",
    "Combat is not in progress.",
    "A local combat hand is required.",
    "Wait for queued actions to finish before checking the hand.",
}


def wait_for_hand(request: Callable, timeout: float = 20, interval: float = .2) -> dict:
    """request(None) reads state; request(body) posts an action. No gameplay actions are sent.

    Selection is returned to the caller for completion. A numeric mismatch is retried until
    the deadline and then fails; it is never treated as an unavailable/successful check.
    """
    deadline = time.monotonic() + timeout
    last = None
    previous = None
    while True:
        state = request(None)
        kind = state.get("state_type")
        if kind in SELECTION:
            return {"kind": "selection", "state": state}
        if kind not in COMBAT:
            if kind in AFTER_COMBAT or (kind == "menu" and state.get("menu_screen") == "main"):
                return {"kind": "left_combat", "state": state}
            raise RuntimeError("Unexpected MCP state while checking the hand: " + str(kind))
        if (state.get("battle") or {}).get("is_play_phase"):
            response = request({"action": "run_command", "command": "karen_check_hand"})
            message = response.get("message", response.get("error", ""))
            try:
                audit = json.loads(message)
            except (ValueError, TypeError):
                if message not in WAIT_MESSAGES:
                    raise RuntimeError("Hand check failed: " + str(message))
                last = message
                previous = None
            else:
                if not isinstance(audit, dict) or not {"modelCount", "visualCount", "missing", "unexpected"} <= audit.keys():
                    raise RuntimeError("Unrecognized hand audit: " + str(audit))
                valid = (response.get("status") == "ok" and audit["modelCount"] == audit["visualCount"]
                         and not audit["missing"] and not audit["unexpected"]
                         and not audit.get("unexpectedPileIcons")
                         and all(item.get("valid") for item in audit.get("burnOverlays", [])))
                if valid and previous == audit:
                    return {"kind": "hand", "state": state, "audit": audit}
                previous = audit if valid else None
                last = audit
        else:
            previous = None
        if time.monotonic() >= deadline:
            raise TimeoutError("Hand did not settle: " + json.dumps(last, ensure_ascii=False))
        time.sleep(min(interval, max(0, deadline - time.monotonic())))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, required=True)
    parser.add_argument("--timeout", type=float, default=20)
    args = parser.parse_args()
    opener = build_opener(ProxyHandler({}))
    base = f"http://127.0.0.1:{args.port}/api/v1/singleplayer"
    def request(body):
        req = Request(base + ("?format=json" if body is None else ""),
                      data=None if body is None else json.dumps(body).encode(),
                      headers={"Content-Type": "application/json"})
        with opener.open(req, timeout=10) as response:
            return json.load(response)
    print(json.dumps(wait_for_hand(request, args.timeout), ensure_ascii=False))


if __name__ == "__main__":
    main()
