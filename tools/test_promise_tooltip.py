"""Check live Promise pile titles after upgrade/transform through KarenSTS2MCP."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import socket
import sys
import time
from urllib.request import ProxyHandler, Request, build_opener

from mcp_hand_audit import wait_for_hand
from test_card_exhaust_cleanup import log_diagnostics

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools/vfx/six-card"))
from isolated_desktop import TestDesktop


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--branch", choices=["stable", "beta"], required=True)
    parser.add_argument("--package", type=Path, default=ROOT / "artifacts/ShoujoKagekiAijoKaren")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=False)
    source = ROOT / "artifacts/dev-game" / args.branch
    game = out / "game"
    port = 18881 if args.branch == "stable" else 18882
    with socket.socket() as check:
        assert check.connect_ex(("127.0.0.1", port)) != 0, "Test port is occupied"
    for folder, dirs, files in os.walk(source / "game"):
        dirs[:] = [d for d in dirs if d != "mods"]
        target = game / Path(folder).relative_to(source / "game")
        target.mkdir(parents=True, exist_ok=True)
        for name in files:
            if name != "override.cfg":
                os.link(Path(folder) / name, target / name)
    mods = game / "mods"
    mods.mkdir()
    shutil.copytree(source / "game/mods/BaseLib", mods / "BaseLib")
    shutil.copytree(args.package, mods / "ShoujoKagekiAijoKaren")
    for name in ["KarenSTS2MCP.dll", "KarenSTS2MCP.json"]:
        shutil.copy2(source / "game/mods" / name, mods / name)
    (mods / "KarenSTS2MCP.conf").write_text(json.dumps(dict(
        port=port, fullscreen=False, muted=True, silent_test=True,
        skip_tutorial=True, skip_first_prompt=True, window_width=1280, window_height=720)), encoding="utf-8")
    (game / "override.cfg").write_text("[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\n", encoding="utf-8")
    env = os.environ.copy()
    env["APPDATA"] = str(out / "user/AppData/Roaming")
    env["LOCALAPPDATA"] = str(out / "user/AppData/Local")
    client_id = 8873
    profile = Path(env["APPDATA"]) / f"SlayTheSpire2/default/{client_id}"
    profile.mkdir(parents=True)
    Path(env["LOCALAPPDATA"]).mkdir(parents=True)
    settings = json.loads((source / "user/AppData/Roaming/SlayTheSpire2/default/1/settings.save").read_text(encoding="utf-8-sig"))
    settings.update(language="zhs", fullscreen=False, limit_fps_in_background=False, volume_master=0)
    (profile / "settings.save").write_text(json.dumps(settings), encoding="utf-8")
    opener = build_opener(ProxyHandler({}))
    base = f"http://127.0.0.1:{port}/api/v1/"
    report = dict(branch=args.branch, checks=[], hand_audits=[], passed=False)

    def api(endpoint="singleplayer?format=json", body=None):
        request = Request(base + endpoint, data=None if body is None else json.dumps(body).encode(),
                          headers={"Content-Type": "application/json"})
        with opener.open(request, timeout=15) as response:
            return response.read() if endpoint == "screenshot" else json.load(response)

    def act(action, **values):
        body = dict(action=action, **values)
        response = api("singleplayer", body)
        with (out / "actions.jsonl").open("a", encoding="utf-8") as stream:
            stream.write(json.dumps([body, response], ensure_ascii=False) + "\n")
        assert response.get("status") == "ok", response
        # MCP schedules UI clicks on the next frame; wait before reading reindexed selections.
        time.sleep(.4)
        return response

    def cmd(text):
        return act("run_command", command=text)

    def wait(predicate, timeout=40):
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            state = api()
            if predicate(state):
                return state
            time.sleep(.2)
        raise TimeoutError("Unexpected MCP state: " + str(state)[:800])

    def capture(name):
        state = api()
        (out / (name + ".json")).write_text(json.dumps(state, ensure_ascii=False, indent=2), encoding="utf-8")
        (out / (name + ".png")).write_bytes(api("screenshot"))
        return state

    def audit(name):
        result = wait_for_hand(lambda body: api() if body is None else api("singleplayer", body))
        assert result["kind"] == "hand", result
        report["hand_audits"].append(dict(name=name, audit=result["audit"]))

    def play(card_id):
        state = wait(lambda s: any(c["id"] == card_id and c["can_play"] for c in s.get("player", {}).get("hand", [])))
        card = next(c for c in state["player"]["hand"] if c["id"] == card_id and c["can_play"])
        act("play_card", card_index=card["index"])
        time.sleep(1)

    def check_titles(stage, expected):
        state = capture(stage)
        power = next(p for p in state["player"]["status"] if p["id"] == "KAREN_PROMISE_PILE_POWER")
        actual = next(k["description"] for k in power["keywords"] if k["name"] == "约定牌堆内容：")
        passed = sorted(actual.splitlines()) == sorted(expected)
        report["checks"].append(dict(stage=stage, expected=expected, actual=actual, passed=passed))
        print(json.dumps(report["checks"][-1], ensure_ascii=False), flush=True)

    desktop = TestDesktop()
    log = out / "godot.log"
    try:
        process = desktop.launch([game / "SlayTheSpire2.exe", "--rendering-driver", "vulkan", "--force-steam=off",
                                  f"--clientId={client_id}", "--log-file", log], game, env)
        deadline = time.monotonic() + 45
        while not (log.exists() and "[Karen STS2 MCP]" in log.read_text(encoding="utf-8", errors="replace")):
            assert process.poll() is None and time.monotonic() < deadline, "MCP failed to start"
            time.sleep(.2)
        wait(lambda s: s.get("menu_screen") == "main" and "singleplayer" in s.get("options", []))
        act("menu_select", option="singleplayer")
        state = wait(lambda s: s.get("menu_screen") in ["singleplayer", "character_select"])
        if state.get("menu_screen") == "singleplayer":
            act("menu_select", option="standard")
        act("menu_select", option="KAREN")
        act("menu_select", option="embark")
        state = wait(lambda s: s.get("menu_screen") == "tutorial_prompt" or s.get("state_type") == "map")
        if state.get("menu_screen") == "tutorial_prompt":
            act("menu_select", option="no")
        wait(lambda s: s.get("state_type") == "map")
        cmd("fight NIBBITS_WEAK")
        wait(lambda s: s.get("battle", {}).get("is_play_phase"))
        time.sleep(1)
        while api()["player"]["hand"]:
            cmd("remove_card " + api()["player"]["hand"][0]["id"] + " Hand")
            time.sleep(.2)
        cmd("energy 99")
        for card in ["KAREN_SMALL_SNACK", "KAREN_DEFEND", "KAREN_FALL"]:
            cmd("card " + card + " Hand")
        wait(lambda s: len(s["player"]["hand"]) == 3)
        capture("before-store")
        play("KAREN_FALL")
        for card_id in ["KAREN_SMALL_SNACK"]:
            state = wait(lambda s: s.get("state_type") == "hand_select")
            card = next(c for c in state["hand_select"]["cards"] if c["id"] == card_id)
            act("combat_select_card", card_index=card["index"])
        if api().get("hand_select", {}).get("can_confirm"):
            act("combat_confirm_selection")
        audit("stored")
        check_titles("after-store", ["Banana什锦"])
        cmd("card APOTHEOSIS Hand")
        play("APOTHEOSIS")
        audit("upgraded-in-pile")
        check_titles("after-upgrade-in-pile", ["Banana什锦+"])
        before_round = api()["battle"]["round"]
        act("end_turn")
        wait(lambda s: s.get("battle", {}).get("round", 0) > before_round and s["battle"]["is_play_phase"])
        time.sleep(1)
        audit("transformed-in-pile")
        check_titles("after-transform-in-pile", ["banana晚餐+"])
        cmd("card KAREN_TOWER_OF_PROMISE Hand")
        capture("before-recall")
        play("KAREN_TOWER_OF_PROMISE")
        audit("recalled")
        state = capture("after-recall")
        banana = next(c for c in state["player"]["hand"] if c["id"] == "KAREN_BANANA")
        assert banana["name"] == "banana晚餐+" and banana["is_upgraded"], banana
        strength = lambda s: next((p["amount"] for p in s["player"]["status"] if p["id"] == "STRENGTH_POWER"), 0)
        before_strength = strength(state)
        play("KAREN_BANANA")
        audit("banana-played")
        state = capture("after-banana")
        report["checks"].append(dict(stage="banana-strength", gained=strength(state) - before_strength,
                                     passed=strength(state) - before_strength == 10))
        report["diagnostics"] = log_diagnostics(log.read_text(encoding="utf-8", errors="replace"))
        report["passed"] = all(c["passed"] for c in report["checks"]) and not report["diagnostics"]["other_errors"]
        assert report["passed"], dict(failed_checks=[c for c in report["checks"] if not c["passed"]],
                                     diagnostics=report["diagnostics"])
    finally:
        desktop.close()
        report["package_sha256"] = {p.relative_to(args.package).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
                                    for p in args.package.rglob("*") if p.is_file()}
        (out / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(dict(branch=args.branch, passed=report["passed"], checks=len(report["checks"])), ensure_ascii=False))


if __name__ == "__main__":
    main()
