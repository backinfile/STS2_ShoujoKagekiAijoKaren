"""Regress native exhaust cleanup during forced room replacement via KarenSTS2MCP."""
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

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools/vfx/six-card"))
from isolated_desktop import TestDesktop
from mcp_hand_audit import wait_for_hand


def log_diagnostics(text):
    lines = text.splitlines()
    common_end = next((i for i, line in enumerate(lines) if "Preloading 'Common' Complete:" in line), -1)
    startup, errors = [], []
    for i, line in enumerate(lines):
        if not line.startswith(("[ERROR]", "ERROR:", "SCRIPT ERROR:")):
            continue
        entry = dict(line=i + 1, text=line)
        if i < common_end and line == "ERROR: Invalid Task ID" and any(
                "core/object/worker_thread_pool.cpp:418" in following for following in lines[i + 1:i + 3]):
            startup.append(entry)
        else:
            errors.append(entry)
    return dict(startup_worker_pool_errors=startup, other_errors=errors)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--branch", choices=["stable", "beta"], default="beta")
    parser.add_argument("--package", type=Path, default=ROOT / "artifacts/ShoujoKagekiAijoKaren")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--delay", type=float, default=1.0)
    args = parser.parse_args()
    assert 0 < args.delay < 2, "Replace the room during the native delayed-cleanup window"
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=False)
    source = ROOT / "artifacts/dev-game" / args.branch
    game = out / "game"
    port = 18879 if args.branch == "stable" else 18880
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
    shutil.copytree(args.package.parent / "ShoujoKagekiAijoCore", mods / "ShoujoKagekiAijoCore")
    for name in ["KarenSTS2MCP.dll", "KarenSTS2MCP.json"]:
        shutil.copy2(source / "game/mods" / name, mods / name)
    (mods / "KarenSTS2MCP.conf").write_text(json.dumps(dict(
        port=port, fullscreen=False, muted=True, silent_test=True,
        skip_tutorial=True, skip_first_prompt=True, window_width=1280, window_height=720)), encoding="utf-8")
    (game / "override.cfg").write_text("[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\n", encoding="utf-8")
    env = os.environ.copy()
    env["APPDATA"] = str(out / "user/AppData/Roaming")
    env["LOCALAPPDATA"] = str(out / "user/AppData/Local")
    profile = Path(env["APPDATA"]) / "SlayTheSpire2/default/8870"
    profile.mkdir(parents=True)
    Path(env["LOCALAPPDATA"]).mkdir(parents=True)
    mod_configs = Path(env["APPDATA"]) / "SlayTheSpire2/mod_configs"
    mod_configs.mkdir(parents=True)
    (mod_configs / "ShoujoKagekiAijoCore.cfg").write_text(json.dumps({
        "EnableDataUpload": "False", "EnableErrorUpload": "False"
    }), encoding="utf-8")
    settings = json.loads((source / "user/AppData/Roaming/SlayTheSpire2/default/1/settings.save").read_text(encoding="utf-8-sig"))
    settings.update(language="zhs", fullscreen=False, limit_fps_in_background=False)
    (profile / "settings.save").write_text(json.dumps(settings), encoding="utf-8")
    opener = build_opener(ProxyHandler({}))
    base = f"http://127.0.0.1:{port}/api/v1/"
    def api(endpoint="singleplayer?format=json", body=None):
        request = Request(base + endpoint, data=None if body is None else json.dumps(body).encode(),
                          headers={"Content-Type": "application/json"})
        with opener.open(request, timeout=10) as response:
            return response.read() if endpoint == "screenshot" else json.load(response)
    def act(action, **values):
        response = api("singleplayer", dict(action=action, **values))
        assert response.get("status") == "ok", response
        return response
    def cmd(text): return act("run_command", command=text)
    def wait(predicate, timeout=40):
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            try:
                state = api()
                if predicate(state): return state
            except (OSError, TimeoutError): pass
            time.sleep(.2)
        raise TimeoutError("MCP state did not reach the expected exhaust/room phase")
    def capture(name):
        (out / (name + ".png")).write_bytes(api("screenshot"))
        (out / (name + ".json")).write_text(json.dumps(api(), ensure_ascii=False, indent=2), encoding="utf-8")
    def audit():
        result = wait_for_hand(lambda body: api() if body is None else api("singleplayer", body))
        assert result["kind"] == "hand", result
        return result["audit"]
    def play_adrenaline():
        cmd("card ADRENALINE Hand")
        state = wait(lambda s: any(c["id"] == "ADRENALINE" for c in s.get("player", {}).get("hand", [])))
        act("play_card", card_index=next(c["index"] for c in state["player"]["hand"] if c["id"] == "ADRENALINE"))
        wait(lambda s: not any(c["id"] == "ADRENALINE" for c in s.get("player", {}).get("hand", [])))
    log = out / "godot.log"
    desktop = TestDesktop()
    report = dict(branch=args.branch, card="ADRENALINE", delay=args.delay, checks=[])
    try:
        process = desktop.launch([game / "SlayTheSpire2.exe", "--rendering-driver", "vulkan", "--force-steam=off",
                                  "--clientId=8870", "--log-file", log], game, env)
        deadline = time.monotonic() + 45
        while not (log.exists() and "[Karen STS2 MCP]" in log.read_text(encoding="utf-8", errors="replace")):
            assert process.poll() is None and time.monotonic() < deadline, "MCP failed to start"
            time.sleep(.2)
        wait(lambda s: s.get("menu_screen") == "main" and "singleplayer" in s.get("options", []))
        act("menu_select", option="singleplayer")
        state = wait(lambda s: s.get("menu_screen") in ["singleplayer", "character_select"])
        if state.get("menu_screen") == "singleplayer": act("menu_select", option="standard")
        act("menu_select", option="KAREN")
        act("menu_select", option="embark")
        state = wait(lambda s: s.get("menu_screen") == "tutorial_prompt" or s.get("state_type") == "map")
        if state.get("menu_screen") == "tutorial_prompt": act("menu_select", option="no")
        wait(lambda s: s.get("state_type") == "map")
        cmd("fight NIBBITS_WEAK")
        wait(lambda s: s.get("battle", {}).get("is_play_phase"))
        time.sleep(1)
        capture("before-exhaust")
        play_adrenaline()
        time.sleep(args.delay)
        capture("before-replacement")
        cmd("fight NIBBITS_WEAK")
        wait(lambda s: s.get("battle", {}).get("is_play_phase"))
        time.sleep(1.5)
        capture("after-replacement")
        report["checks"].append(dict(case="exhaust-then-forced-room", hand=audit()))
        play_adrenaline()
        time.sleep(3)
        capture("normal-exhaust-finished")
        report["checks"].append(dict(case="normal-exhaust-completion", hand=audit()))
        text = log.read_text(encoding="utf-8", errors="replace")
        report["duplicate_pool_returns"] = text.count("but it's already been freed!")
        report["pool_membership_errors"] = text.count("but it's not part of the pool!")
        report["diagnostics"] = log_diagnostics(text)
        report["passed"] = (report["duplicate_pool_returns"] == report["pool_membership_errors"] == 0
                            and not report["diagnostics"]["other_errors"])
        print(json.dumps(report, ensure_ascii=False), flush=True)
        assert report["passed"], "Native exhaust returned an NCard to its pool twice"
    finally:
        desktop.close()
        report["package_sha256"] = {p.relative_to(args.package).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
                                    for p in args.package.rglob("*") if p.is_file()}
        (out / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
