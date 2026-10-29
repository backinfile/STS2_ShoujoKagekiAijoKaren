"""Regress Next Stage+ shuffle reentry through KarenSTS2MCP on a private desktop."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import socket
import sys
import time
from urllib.request import ProxyHandler, Request, build_opener

ROOT = Path(__file__).resolve().parents[1]
from mcp_hand_audit import WAIT_MESSAGES

sys.path.insert(0, str(ROOT / "tools/vfx/six-card"))
from isolated_desktop import TestDesktop


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("branch", choices=["stable", "beta"])
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=False)
    dev = ROOT / "artifacts/dev-game" / args.branch
    game = dev / "game"
    config = json.loads((game / "mods/KarenSTS2MCP.conf").read_text(encoding="utf-8-sig"))
    port = config["port"]
    with socket.socket() as check:
        assert check.connect_ex(("127.0.0.1", port)) != 0, "Test port already in use"
    profile_id = int(time.time())
    env = os.environ.copy()
    env["APPDATA"] = str(out / "user/AppData/Roaming")
    env["LOCALAPPDATA"] = str(out / "user/AppData/Local")
    profile = Path(env["APPDATA"]) / f"SlayTheSpire2/default/{profile_id}"
    profile.mkdir(parents=True)
    Path(env["LOCALAPPDATA"]).mkdir(parents=True)
    settings = json.loads((dev / "user/AppData/Roaming/SlayTheSpire2/default/1/settings.save").read_text(encoding="utf-8-sig"))
    settings.update(language="zhs", fullscreen=False, limit_fps_in_background=False)
    (profile / "settings.save").write_text(json.dumps(settings), encoding="utf-8")
    base = f"http://127.0.0.1:{port}/api/v1/"
    opener = build_opener(ProxyHandler({}))
    results = []

    def api(endpoint="singleplayer?format=json", body=None):
        req = Request(base + endpoint, data=None if body is None else json.dumps(body).encode(),
                      headers={"Content-Type": "application/json"})
        with opener.open(req, timeout=15) as response:
            return response.read() if endpoint == "screenshot" else json.load(response)

    def act(action, **values):
        body = dict(action=action, **values)
        response = api("singleplayer", body)
        with (out / "actions.jsonl").open("a", encoding="utf-8") as f:
            f.write(json.dumps([body, response], ensure_ascii=False) + "\n")
        assert response.get("status") == "ok", response
        return response

    def wait(test, timeout=40):
        end = time.monotonic() + timeout
        while time.monotonic() < end:
            state = api()
            if test(state):
                return state
            time.sleep(.2)
        raise TimeoutError(state)

    def command(text):
        return act("run_command", command=text)

    def snap(name):
        (out / (name + ".png")).write_bytes(api("screenshot"))
        (out / (name + ".json")).write_text(json.dumps(api(), ensure_ascii=False, indent=2), encoding="utf-8")

    def audit(name, expected):
        def read_hand():
            end = time.monotonic() + 20
            while time.monotonic() < end:
                response = api("singleplayer", dict(action="run_command", command="karen_check_hand"))
                message = response.get("message", "")
                try:
                    return response, json.loads(message)
                except ValueError:
                    if message not in WAIT_MESSAGES:
                        raise RuntimeError(response)
                time.sleep(.2)
            raise TimeoutError(response)

        # Console tasks can be scheduled after the HTTP response. Wait for the
        # requested model change before comparing the settled visual snapshots.
        wait(lambda s: len(s["player"]["hand"]) == expected, timeout=20)
        response, a = read_hand()
        time.sleep(.5)
        second, b = read_hand()
        end = time.monotonic() + 5
        while a != b and time.monotonic() < end:
            response, a = second, b
            time.sleep(.5)
            second, b = read_hand()
        snap(name)
        result = dict(case=name, expected=expected, audit=b)
        result["pass"] = (response.get("status") == second.get("status") == "ok"
                          and a == b and b["modelCount"] == b["visualCount"] == expected
                          and len(api()["player"]["hand"]) == expected and expected <= 10
                          and not b["missing"] and not b["unexpected"]
                          and not b.get("unexpectedPileIcons"))
        results.append(result)
        print(json.dumps(result), flush=True)

    def clear():
        while api()["player"]["hand"]:
            hand = api()["player"]["hand"]
            command("remove_card " + hand[0]["id"] + " Hand")
            wait(lambda s: len(s["player"]["hand"]) < len(hand))

    def add(card):
        before = len(api()["player"]["hand"])
        command("card " + card + " Hand")
        wait(lambda s: len(s["player"]["hand"]) == before + 1)
        time.sleep(.2)

    def play(card):
        c = next(c for c in api()["player"]["hand"] if c["id"] == card)
        act("play_card", card_index=c["index"])

    desktop = TestDesktop()
    log = out / "godot.log"
    try:
        process = desktop.launch([game / "SlayTheSpire2.exe", "--rendering-driver", "vulkan",
                                  "--force-steam=off", f"--clientId={profile_id}", "--log-file", log], game, env)
        print(f"Started {args.branch} PID={process.pid}", flush=True)
        end = time.monotonic() + 50
        while time.monotonic() < end:
            if log.exists() and "[Karen STS2 MCP]" in log.read_text(encoding="utf-8", errors="replace"):
                break
            assert process.poll() is None, "Game exited before MCP loaded"
            time.sleep(.25)
        else:
            raise TimeoutError("KarenSTS2MCP did not load")
        wait(lambda s: s.get("menu_screen") == "main" and "singleplayer" in s.get("options", []))
        api("settings", dict(muted=True, fullscreen=False, window_width=1280, window_height=720))
        act("menu_select", option="singleplayer")
        state = wait(lambda s: s.get("menu_screen") in ("singleplayer", "character_select"))
        if state["menu_screen"] == "singleplayer":
            act("menu_select", option="standard")
        wait(lambda s: s.get("menu_screen") == "character_select")
        act("menu_select", option="KAREN")
        act("menu_select", option="embark")
        state = wait(lambda s: s.get("menu_screen") == "tutorial_prompt" or s.get("state_type") == "map")
        if state.get("menu_screen") == "tutorial_prompt":
            act("menu_select", option="no")
        wait(lambda s: s.get("state_type") == "map")
        act("choose_map_node", index=0)
        wait(lambda s: s.get("battle", {}).get("is_play_phase"))
        command("energy 99")
        clear()
        command("draw 10")
        time.sleep(2)
        clear()
        audit("empty-control", 0)
        add("KAREN_NEXT_STAGE")
        snap("normal-before")
        play("KAREN_NEXT_STAGE")
        audit("normal-after", 0)
        command("draw 10")
        audit("normal-shuffled", 1)
        clear()
        add("KAREN_NEXT_STAGE")
        command("upgrade 0")
        snap("upgraded-before")
        play("KAREN_NEXT_STAGE")
        audit("upgraded-after", 1)
        for _ in range(9):
            add("KAREN_DEFEND")
        audit("full-hand", 10)
        play("KAREN_NEXT_STAGE")
        audit("full-hand-replayed", 10)
        if args.branch == "beta":
            # Beta moves the complete shuffle batch before hooks. All three
            # cards can be drawn before the deferred animations are constructed.
            clear()
            command("card KAREN_STRIKE Discard")
            command("card KAREN_DEFEND Discard")
            add("KAREN_NEXT_STAGE")
            command("upgrade 0")
            snap("batch-shuffle-before")
            play("KAREN_NEXT_STAGE")
            audit("batch-shuffle-after", 3)
            play("KAREN_DEFEND")
            audit("batch-drawn-card-played", 2)
    finally:
        desktop.close()
        variant = game / f"mods/ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.{args.branch.title()}.dll"
        report = dict(branch=args.branch, dllSha256=hashlib.sha256(variant.read_bytes()).hexdigest(), results=results)
        (out / "results.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    assert results and all(r["pass"] for r in results), "Hand count or visual node mismatch"


if __name__ == "__main__":
    main()
