"""Desktop-hosted first-use checks for a selected Mod package; this is not an ARM64 device test."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import shutil
import socket
import subprocess
import time
import urllib.request

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--source-env", type=Path, required=True, help="Read-only dev environment containing game/ and user/")
parser.add_argument("--branch", choices=["stable", "beta"], default="stable")
parser.add_argument("--port", type=int, default=15847)
parser.add_argument("--mode",choices=["cold","preload","draw"],default="cold")
parser.add_argument("--renderer",choices=["opengl3","vulkan"],default="opengl3")
parser.add_argument("--effect",choices=["bridge","shine","promise","all"],default="bridge")
parser.add_argument("--passes",type=int,default=4)
parser.add_argument("--package",type=Path,help="Final Mod folder; defaults to the AnyCPU Android package")
args = parser.parse_args()
assert args.passes>=2
project = Path(__file__).resolve().parents[3]
run = project / "artifacts/vfx-first-use-20261022/runs" / (args.branch + "-" + time.strftime("%Y%m%d-%H%M%S"))
run.mkdir(parents=True, exist_ok=False)
game = run / "game"
game.mkdir()
source = args.source_env.resolve() / "game"
package = args.package.resolve() if args.package else project / "artifacts/android/ShoujoKagekiAijoKaren"
base = "http://127.0.0.1:" + str(args.port) + "/api/v1"
results = []
with socket.socket() as sock:
    if sock.connect_ex(("127.0.0.1", args.port)) == 0:
        raise RuntimeError("Test MCP port is occupied")

def static_file(src, dest):
    dest.parent.mkdir(parents=True, exist_ok=True)
    try:
        os.link(src, dest)
    except OSError:
        shutil.copy2(src, dest)

# Read-only static game files may share disk blocks; mods and all user data are copies.
for src in source.iterdir():
    if src.is_file():
        static_file(src, game / src.name)
for src in (source / "data_sts2_windows_x86_64").rglob("*"):
    if src.is_file():
        static_file(src, game / src.relative_to(source))
mods = game / "mods"
mods.mkdir()
shutil.copytree(source / "mods/BaseLib", mods / "BaseLib")
shutil.copytree(package, mods / "ShoujoKagekiAijoKaren")
for name in ["KarenSTS2MCP.dll", "KarenSTS2MCP.json"]:
    shutil.copy2(source / "mods" / name, mods / name)
(mods / "KarenSTS2MCP.conf").write_text(json.dumps(dict(port=args.port, muted=True, fullscreen=False,
    window_width=1280, window_height=720, skip_tutorial=True, skip_first_prompt=True)), encoding="utf-8")
variant = args.branch.capitalize()
for plugin, csproj in [("KarenFirstUseProbe", "tools/vfx/first-use/Probe.csproj")]:
    out = run / "fixtures" / plugin
    subprocess.run(["dotnet", "build", str(project / csproj), "--nologo", "-v:q", "-clp:ErrorsOnly",
        "-p:Variant=" + variant, "-p:Game=" + str(source),
        "-p:KarenAssembly=" + str(package / "lib" / ("ShoujoKagekiAijoKaren." + variant + ".dll")),
        "-o", str(out)], cwd=project, check=True)
    target = mods / plugin
    target.mkdir()
    shutil.copy2(out / (plugin + ".dll"), target / (plugin + ".dll"))
    (target / (plugin + ".json")).write_text(json.dumps(dict(id=plugin, name=plugin, version="1.0.0",
        has_dll=True, has_pck=False, affects_gameplay=True, dependencies=[dict(id="ShoujoKagekiAijoKaren")])), encoding="utf-8")
profile = run / "user/AppData/Roaming/SlayTheSpire2/default/8622"
profile.mkdir(parents=True)
shutil.copy2(args.source_env / "user/AppData/Roaming/SlayTheSpire2/default/1/settings.save", profile / "settings.save")
env = os.environ.copy()
env["KAREN_ALL_VFX_RESULT"]=str(run/"all-result.json")
env["KAREN_VFX_PROBE_EFFECT"]=args.effect
env["KAREN_VFX_PROBE_MODE"]=args.mode
env["APPDATA"] = str(run / "user/AppData/Roaming")
env["LOCALAPPDATA"] = str(run / "user/AppData/Local")
Path(env["LOCALAPPDATA"]).mkdir(parents=True)
log = run / "godot.log"
startup = subprocess.STARTUPINFO()
startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
startup.wShowWindow = 0
import sys
sys.path.insert(0,str(project/"tools/vfx/six-card"))
from isolated_desktop import TestDesktop
desktop=TestDesktop()
process = desktop.launch([str(game / "SlayTheSpire2.exe"), "--rendering-driver", args.renderer, *(["--rendering-method", "gl_compatibility"] if args.renderer=="opengl3" else []), "--force-steam=off",
    "--clientId=8622", "--log-file", str(log)], cwd=game, env=env)
print("START", args.branch, "pid=" + str(process.pid), "run=" + str(run), flush=True)

def request(endpoint, body=None):
    data = None if body is None else json.dumps(body).encode()
    req = urllib.request.Request(base + endpoint, data=data, headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=15) as response:
        return json.loads(response.read())

def action(**body):
    result = request("/singleplayer", body)
    if result.get("status") != "ok":
        raise RuntimeError(str(body) + ": " + str(result))
    return result

def wait_for(test, label, seconds=45):
    last = None
    until = time.monotonic() + seconds
    while time.monotonic() < until:
        if process.poll() is not None:
            raise RuntimeError("Game exited: " + str(process.returncode))
        try:
            last = request("/singleplayer?format=json")
            if test(last):
                return last
        except (OSError, ValueError):
            pass
        time.sleep(.2)
    raise RuntimeError("Timeout " + label + ": " + str(last))

def screenshot(name):
    with urllib.request.urlopen(base + "/screenshot", timeout=20) as response:
        (run / (name + ".png")).write_bytes(response.read())

def fixture(case, command, status_command):
    screenshot(case + "-before")
    action(action="run_command", command=command)
    until = time.monotonic() + 40
    while time.monotonic() < until:
        status = action(action="run_command", command=status_command).get("message", "")
        if status.startswith(("pass:", "fail:")):
            if not status.startswith("pass:"):
                raise RuntimeError(status)
            time.sleep(1.5)
            hand = action(action="run_command", command="karen_check_hand")
            check = json.loads(hand["message"])
            if check["modelCount"] != check["visualCount"] or check["missing"] or check["unexpected"] or check["unexpectedPileIcons"] or any(not b["valid"] for b in check["burnOverlays"]):
                raise RuntimeError("Hand model/visual mismatch: " + str(check))
            screenshot(case + "-after")
            results.append(dict(case=case, passed=True, status=status, hand=check))
            print("PASS", case, flush=True)
            return
        time.sleep(.2)
    raise RuntimeError("Fixture timed out: " + case)

try:
    until = time.monotonic() + 50
    while time.monotonic() < until:
        if log.exists() and "[Karen STS2 MCP]" in log.read_text(encoding="utf-8", errors="replace"):
            break
        if process.poll() is not None:
            raise RuntimeError("Game exited before MCP startup")
        time.sleep(.25)
    else:
        raise RuntimeError("Karen STS2 MCP did not load")
    wait_for(lambda s: s.get("menu_screen") == "main" and "singleplayer" in s.get("options", []), "main menu")
    request("/settings", dict(muted=True, fullscreen=False, window_width=1280, window_height=720))
    wait_for(lambda s: s.get("menu_screen") == "main" and "singleplayer" in s.get("options", []), "main menu after display setup")
    action(action="menu_select", option="singleplayer")
    state = wait_for(lambda s: s.get("menu_screen") in ["singleplayer", "character_select"], "character menu")
    if state["menu_screen"] == "singleplayer":
        action(action="menu_select", option="standard")
    wait_for(lambda s: s.get("menu_screen") == "character_select", "character selection")
    action(action="menu_select", option="KAREN")
    screenshot("character")
    action(action="menu_select", option="embark")
    state = wait_for(lambda s: s.get("menu_screen") == "tutorial_prompt" or s.get("state_type") == "map", "map startup")
    if state.get("menu_screen") == "tutorial_prompt":
        action(action="menu_select", option="no")
    wait_for(lambda s: s.get("state_type") == "map", "map")
    action(action="choose_map_node", index=0)
    wait_for(lambda s: s.get("state_type") == "monster" and s.get("battle", {}).get("is_play_phase"), "combat")
    time.sleep(4)
    if args.effect=="all":
        action(action="run_command",command="karen_all_vfx_first_use run")
        until=time.monotonic()+300
        while time.monotonic()<until:
            file=run/"all-result.json"
            raw=file.read_text(encoding="utf-8") if file.exists() else "running"
            if raw.startswith("{"):
                measured=json.loads(raw)
                (run/"measurements.json").write_text(json.dumps(measured,indent=2),encoding="utf-8")
                assert measured["status"]=="done",measured
                hitches=[]
                for case in measured["samples"]:
                    first=case["passes"][0];warm=sorted(x["maxMs"] for x in case["passes"][1:])[0]
                    if first["maxMs"]>33.3 and first["maxMs"]>warm*1.5:hitches.append(dict(effect=case["effect"],firstMs=first["maxMs"],warmMs=warm))
                print(json.dumps(dict(warmup={k:v for k,v in measured.items() if k!="samples"},cases=len(measured["samples"]),hitches=hitches)),flush=True)
                results.append(dict(case="all_visual_families",passed=not hitches,hitches=hitches))
                break
            time.sleep(1)
        else:raise RuntimeError("All visual measurement timed out")
    elif args.effect!="bridge":
        def common(operation):return action(action="run_command",command="karen_common_first_use "+operation+" "+args.effect)
        def common_result():
            for _ in range(120):
                raw=action(action="run_command",command="karen_common_first_use_status")["message"]
                if raw.startswith("{"):
                    value=json.loads(raw);assert value["status"]!="error",value
                    return value
                time.sleep(.1)
            raise RuntimeError("Common VFX timed out")
        def hand(name):
            check=json.loads(action(action="run_command",command="karen_check_hand")["message"])
            (run/(name+"-hand.json")).write_text(json.dumps(check,indent=2),encoding="utf-8")
            assert check["modelCount"]==check["visualCount"] and not check["missing"] and not check["unexpected"] and not check["unexpectedPileIcons"],check
        common("prepare");time.sleep(4);prepared=common_result()
        (run/"prepared.json").write_text(json.dumps(prepared,indent=2),encoding="utf-8")
        measured=[]
        for number in range(args.passes):
            screenshot(str(number)+"-before");hand(str(number)+"-before")
            common("run");time.sleep(2.5);result=common_result()
            assert result["status"]=="done" and result["drawn"]>20,result
            result["pass"]=number;measured.append(result)
            screenshot(str(number)+"-after");hand(str(number)+"-after")
            common("reset");time.sleep(1.2);common_result()
            screenshot(str(number)+"-reset");hand(str(number)+"-reset")
        (run/"measurements.json").write_text(json.dumps(dict(effect=args.effect,mode=args.mode,renderer=args.renderer,samples=measured),indent=2),encoding="utf-8")
        common("dump");time.sleep(.5);contours=common_result()
        (run/"contours.json").write_text(json.dumps(contours,indent=2),encoding="utf-8")
        if args.branch=="stable":
            reference=json.loads((Path(__file__).parent/"contour-reference.json").read_text(encoding="utf-8"))["frames"]
            actual={x["path"]:{"points":len(x["points"]),"sha256":hashlib.sha256(json.dumps(x["points"],separators=(",",":")).encode()).hexdigest()} for x in contours["frames"]}
            assert actual==reference,{"contour_regression":actual,"expected":reference}
            print("PASS: five native card frame contours unchanged",flush=True)
        warm=sorted(x["maxMs"] for x in measured[1:])[len(measured[1:])//2]
        hit=measured[0]["maxMs"]>33.3 and measured[0]["maxMs"]>warm*1.5
        print(json.dumps(dict(effect=args.effect,firstMaxMs=measured[0]["maxMs"],warmMaxMs=warm,samples=[dict(syncMs=x["syncMs"],maxMs=x["maxMs"],cpu=x["cpu"]) for x in measured],firstUseHitch=hit)),flush=True)
        results.append(dict(case="first_use_"+args.effect,passed=not hit))
    else:
        time.sleep(4)
        action(action="run_command",command="karen_first_use "+args.mode)
        time.sleep(12)
        for _ in range(150):
            value=action(action="run_command",command="karen_first_use_status")["message"]
            if value.startswith("{"):
                measured=json.loads(value)
                (run/"measurements.json").write_text(json.dumps(measured,indent=2),encoding="utf-8")
                assert measured["status"]=="done",measured
                assert all(x["drawn"]>30 for x in measured["samples"]),"Rendering did not advance"
                first_hitch=False
                for effect in ["bridge"]:
                    group=[x for x in measured["samples"] if x["effect"]==effect]
                    warm=sorted(x["maxMs"] for x in group[1:])[1]
                    cold=group[0]
                    hit=cold["maxMs"]>33.3 and cold["maxMs"]>warm*1.5
                    first_hitch|=hit
                    print(json.dumps(dict(effect=effect,firstMaxMs=round(cold["maxMs"],2),warmMedianMaxMs=round(warm,2),firstBuildMs=round(cold["buildMs"],2),firstReadyMs=round(cold["readyMs"],2),warmReadyMs=[round(x["readyMs"],2) for x in group[1:]],mode=args.mode,warmBuildMs=[round(x["buildMs"],2) for x in group[1:]],firstUseHitch=hit)),flush=True)
                results.append(dict(case="first_use_hitch",passed=not first_hitch))
                break
            time.sleep(.25)
        else:raise RuntimeError("Measurement timed out")
except Exception as error:
    results.append(dict(case="runner", passed=False, error=str(error)))
    print("FAIL", error, flush=True)
finally:
    report = dict(platform="Windows x64 host / selected Mod package", android_arm64_tested=False,
        branch=args.branch,effect=args.effect,renderer=args.renderer,measurement_protocol=2 if args.effect=="all" else 1,
        package_sha256={str(f.relative_to(mods / "ShoujoKagekiAijoKaren")):hashlib.sha256(f.read_bytes()).hexdigest() for f in (mods / "ShoujoKagekiAijoKaren").rglob("*") if f.is_file()},
        results=results)
    (run / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    if process.poll() is None:
        process.terminate()
        process.wait(timeout=15)
    desktop.close()
    print("REPORT", run / "report.json", flush=True)
raise SystemExit(0 if results and all(r["passed"] for r in results) else 1)
