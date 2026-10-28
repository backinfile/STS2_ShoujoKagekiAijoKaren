"""Targeted quality checks and optional actual-game recordings in a fresh isolated desktop."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import sys
import time
import urllib.request

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("branch", choices=["stable", "beta"])
parser.add_argument("--output", type=Path, required=True)
parser.add_argument("--record", action="store_true")
args = parser.parse_args()
repo = Path(__file__).resolve().parents[3]
out = args.output.resolve()
out.mkdir(parents=True, exist_ok=False)
source = repo / "artifacts/dev-game" / args.branch
game = out / "game"
port = 18747 if args.branch == "stable" else 18748
with socket.socket() as check:
    assert check.connect_ex(("127.0.0.1", port)) != 0, "Fixture port occupied"


def link(src, dst):
    try:
        os.link(src, dst)
    except OSError:
        shutil.copy2(src, dst)
    return dst


shutil.copytree(source / "game", game, copy_function=link,
                ignore=lambda folder, names: ["mods", "override.cfg"] if Path(folder) == source / "game" else [])
mods = game / "mods"
mods.mkdir()
shutil.copytree(source / "game/mods/BaseLib", mods / "BaseLib")
shutil.copytree(repo / "artifacts/ShoujoKagekiAijoKaren", mods / "ShoujoKagekiAijoKaren")
for ext in ["dll", "json"]:
    shutil.copy2(source / ("game/mods/KarenSTS2MCP." + ext), mods)
(mods / "KarenSTS2MCP.conf").write_text(json.dumps(dict(port=port, fullscreen=False, muted=False,
    silent_test=True, skip_tutorial=True, skip_first_prompt=True, window_width=1280, window_height=720)), encoding="utf-8")
for name, project in [("KarenQualityProbe", "tools/vfx/quality/QualityProbe.csproj"),
                      ("KarenBackgroundTestGuard", "tools/vfx/catalog/guard/Guard.csproj")]:
    target = out / "fixtures" / name
    subprocess.run(["dotnet", "build", str(repo / project), "--nologo", "-v:q", "-clp:ErrorsOnly",
                    "-p:Variant=" + args.branch.title(), "-p:Game=" + str(source / "game"), "-o", str(target)], check=True, cwd=repo)
    shutil.copy2(target / (name + ".dll"), mods)
    (mods / (name + ".json")).write_text(json.dumps(dict(id=name, name=name, version="1.0.0",
        has_dll=True, has_pck=False, affects_gameplay=False)), encoding="utf-8")
profile = out / "user/AppData/Roaming/SlayTheSpire2/default/8321"
profile.mkdir(parents=True)
settings_source = source / "user/AppData/Roaming/SlayTheSpire2/default/1/settings.save"
if settings_source.exists():
    settings = json.loads(settings_source.read_text(encoding="utf-8-sig"))
    settings.update(mute_in_background=False, limit_fps_in_background=False)
    (profile / "settings.save").write_text(json.dumps(settings), encoding="utf-8")
(game / "override.cfg").write_text("[display]\nwindow/size/mode=1\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n", encoding="utf-8")
sys.path.insert(0, str(repo / "tools/vfx/six-card"))
from isolated_desktop import TestDesktop
desktop = TestDesktop()
env = os.environ.copy()
env["APPDATA"] = str(out / "user/AppData/Roaming")
env["LOCALAPPDATA"] = str(out / "user/AppData/Local")
env["STS2_MCP_FFMPEG"] = shutil.which("ffmpeg") or "ffmpeg"
log = out / "godot.log"
base = f"http://127.0.0.1:{port}/api/v1"
report = dict(branch=args.branch, cases=[], screenshots=[], recorded=args.record)
package = mods / "ShoujoKagekiAijoKaren"
report["package"] = {str(p.relative_to(package)): hashlib.sha256(p.read_bytes()).hexdigest()
                     for p in package.rglob("*") if p.is_file()}
recording = False


def api(path, data=None):
    req = urllib.request.Request(base + path, data=None if data is None else json.dumps(data).encode(),
                                 headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=30) as response:
        return json.load(response)


def state():
    return api("/singleplayer?format=json")


def action(**kw):
    result = api("/singleplayer", kw)
    with (out / "actions.jsonl").open("a", encoding="utf-8") as f:
        f.write(json.dumps([kw, result], ensure_ascii=False) + "\n")
    assert result.get("status") == "ok", (kw, result)
    return result


def cmd(command):
    return action(action="run_command", command=command)["message"]


def quality(operation="status"):
    return json.loads(cmd("quality_probe " + operation))


def wait(test, label, seconds=45):
    until = time.monotonic() + seconds
    last = None
    while time.monotonic() < until:
        try:
            last = state()
            if test(last):
                return last
        except (OSError, ValueError):
            pass
        time.sleep(.15)
    raise RuntimeError("Timeout " + label + ": " + str(last))


def snap(name):
    with urllib.request.urlopen(base + "/screenshot", timeout=30) as response:
        (out / (name + ".png")).write_bytes(response.read())
    report["screenshots"].append(name + ".png")


def hand(name):
    last = None
    for _ in range(50):
        last = json.loads(cmd("karen_check_hand"))
        if last["modelCount"] == last["visualCount"] and not last["missing"] and not last["unexpected"] and not last["unexpectedPileIcons"]:
            (out / (name + "-hand.json")).write_text(json.dumps(last, indent=2), encoding="utf-8")
            return last
        time.sleep(.1)
    raise AssertionError(last)


def add(card):
    cmd("card " + card + " Hand")
    time.sleep(.4)


def play(card):
    s = state()
    actual = next(c for c in s["player"]["hand"] if c["id"] == card)
    kw = dict(action="play_card", card_index=actual["index"])
    if actual["target_type"] == "AnyEnemy":
        kw["target"] = s["battle"]["enemies"][0]["entity_id"]
    return action(**kw)


def record_start():
    global recording
    api("/recording/start", dict(fps=30, max_duration_seconds=60, width=1280, height=720, audio=True, encoder="libx264"))
    recording = True


def record_stop(level):
    global recording
    api("/recording/stop", {})
    for _ in range(150):
        result = api("/recording")
        if result["status"] in ["completed", "failed"]:
            break
        time.sleep(.2)
    assert result["status"] == "completed", result
    shutil.copy2(result["output_path"], out / (level + ".mp4"))
    recording = False
    (out / (level + "-recording.json")).write_text(json.dumps(result, indent=2), encoding="utf-8")


try:
    process = desktop.launch([str(game / "SlayTheSpire2.exe"), "--rendering-driver", "vulkan",
        "--force-steam=off", "--clientId=8321", "--log-file", str(log)], cwd=game, env=env)
    until = time.monotonic() + 60
    while time.monotonic() < until:
        if log.exists() and "[Karen STS2 MCP]" in log.read_text(encoding="utf-8", errors="replace"):
            break
        if process.poll() is not None:
            raise RuntimeError("Fixture exited before MCP startup")
        time.sleep(.2)
    else:
        raise RuntimeError("MCP load timeout")
    wait(lambda s: s.get("menu_screen") == "main" and "singleplayer" in s.get("options", []), "main menu")
    action(action="menu_select", option="singleplayer")
    s = wait(lambda s: s.get("menu_screen") in ["singleplayer", "character_select"], "character menu")
    if s["menu_screen"] == "singleplayer":
        action(action="menu_select", option="standard")
    wait(lambda s: s.get("menu_screen") == "character_select", "character select")
    action(action="menu_select", option="KAREN")
    action(action="menu_select", option="embark")
    s = wait(lambda s: s.get("menu_screen") == "tutorial_prompt" or s.get("state_type") == "map", "map")
    if s.get("menu_screen") == "tutorial_prompt":
        action(action="menu_select", option="no")
    wait(lambda s: s.get("state_type") == "map", "map")
    action(action="choose_map_node", index=0)
    wait(lambda s: s.get("battle", {}).get("is_play_phase"), "combat")
    time.sleep(2)
    report["budgets"] = quality("check")
    print("PASS", args.branch, "budget geometry, labels and invalid-level fallback", flush=True)
    for level in ["Full", "Balanced", "Low"]:
        quality("set " + level)
        snap(level + "-before-fixture")
        cmd("fight WATERFALL_GIANT_BOSS")
        wait(lambda s: s.get("battle", {}).get("is_play_phase") and
             any(e.get("entity_id", "").startswith("WATERFALL") for e in s.get("battle", {}).get("enemies", [])), "fixture combat")
        time.sleep(.7)
        cmd("energy 99")
        for card in list(state()["player"]["hand"]):
            cmd("remove_card " + card["id"] + " Hand")
            time.sleep(.08)
        for card in ["KAREN_NONON", "KAREN_PASSION", "KAREN_SHINE_STRIKE", "KAREN_DEFEND"]:
            add(card)
        cmd("quality_probe prepare")
        time.sleep(1.2)
        hand(level + "-prepared")
        snap(level + "-prepared")
        item = dict(level=level, idle=quality(), markers=[])
        assert item["idle"]["warningVisible"] >= 1, item
        assert (item["idle"]["ambientVisible"] == 0) == (level == "Low"), item
        if args.record:
            record_start()
        started = time.monotonic()

        def mark(title):
            item["markers"].append(dict(time=time.monotonic() - started, title=title))

        mark("卡面微光、耗尽预告与约定之塔")
        time.sleep(1.3)
        quality("reset")
        mark("NONNON · 大范围碎屑与刀光")
        before = state()["battle"]["enemies"][0]["hp"]
        play("KAREN_NONON")
        for _ in range(50):
            q = quality()
            if q["nononParticles"] > 0:
                break
            time.sleep(.04)
        item["nonon"] = q
        expected = {"Full": 1280, "Balanced": 640, "Low": 321}[level]
        assert q["nononParticles"] == expected, q
        time.sleep(.7)
        snap(level + "-nonon")
        time.sleep(2.2)
        item["nononAfter"] = quality()
        item["damage"] = before - state()["battle"]["enemies"][0]["hp"]
        assert item["damage"] == 12, item
        mark("激情 · 卡面星尘与闪耀耗尽")
        play("KAREN_PASSION")
        time.sleep(3.2)
        hand(level + "-passion")
        mark("闪耀打击 · 耗尽与牌堆落点")
        snap(level + "-before-deplete")
        play("KAREN_SHINE_STRIKE")
        time.sleep(2.4)
        hand(level + "-deplete")
        snap(level + "-after-deplete")
        mark("过去与未来、觉醒形态 · 常驻装饰")
        for card in ["KAREN_PAST_AND_FUTURE", "KAREN_FORM"]:
            add(card)
            play(card)
            time.sleep(1.3)
        time.sleep(1.5)
        snap(level + "-ambient")
        # Toggle both ways in a living scene; models and hand stay in place.
        quality("set " + ("Full" if level == "Low" else "Low"))
        time.sleep(.25)
        quality("set " + level)
        time.sleep(.4)
        hand(level + "-toggle")
        mark("最后的台词 · 演出预览")
        item["video"] = quality("video")
        assert item["video"]["played"] and item["video"]["waitSeconds"] == 6.7, item
        if level == "Low":
            assert item["video"]["players"] == 0, item
        time.sleep(2.4)
        snap(level + "-video")
        time.sleep(4.6)
        if args.record:
            record_stop(level)
        item["final"] = quality()
        assert "NKarenNononVfx" not in item["final"]["effects"], item
        assert "NKarenLastWordVideoVfx" not in item["final"]["effects"], item
        report["cases"].append(item)
        (out / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
        print("PASS", args.branch, level, "particles", expected, "damage", item["damage"], "hand/toggle/cleanup", flush=True)
    log_text = log.read_text(encoding="utf-8", errors="replace")
    errors = [line for line in log_text.splitlines() if line.startswith("ERROR:") or "[EXCEPTION]" in line or "[KarenVfxWarmup] failed" in line]
    assert not errors, errors
    report["passed"] = True
except Exception as error:
    report["passed"] = False
    report["error"] = repr(error)
    raise
finally:
    if recording:
        try:
            api("/recording/stop", {})
        except Exception:
            pass
    desktop.close()
    (out / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
