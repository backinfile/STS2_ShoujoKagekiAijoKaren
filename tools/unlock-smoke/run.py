"""Desktop-hosted smoke for the AnyCPU Android Mod; this is not an ARM64 device test."""
from pathlib import Path
import argparse
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

args = parser.parse_args()
project = Path(__file__).resolve().parents[2]
run = project / "artifacts/unlock-smoke" / (args.branch + "-" + time.strftime("%Y%m%d-%H%M%S"))
run.mkdir(parents=True, exist_ok=False)
game = run / "game"
game.mkdir()
source = args.source_env.resolve() / "game"
package = project / "artifacts/android/ShoujoKagekiAijoKaren"
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
for plugin, csproj in [("KarenUnlockSmoke", "tools/unlock-smoke/UnlockSmoke.csproj")]:
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
process = desktop.launch([str(game / "SlayTheSpire2.exe"), "--rendering-driver", "opengl3", "--rendering-method", "gl_compatibility", "--force-steam=off",
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
    def probe_result(applying):
        path=run/"user/AppData/Roaming/SlayTheSpire2/unlock-smoke-result.json"
        for _ in range(150):
            if path.exists():
                try:
                    raw=json.loads(path.read_text(encoding="utf-8"));assert raw["success"],raw
                    measured=json.loads(raw["message"])
                    if measured["applying"]==applying:return dict(message=raw["message"])
                except json.JSONDecodeError:pass
            time.sleep(.1)
        raise RuntimeError("Auto test did not finish")
    applied=probe_result(True)
    print("APPLY",applied["message"],flush=True)
    report=json.loads(applied["message"]);assert report["passed"]
    results.append(dict(case="unlock_button_and_profile",passed=True,report=report))
    screenshot("unlock-after")
    process.terminate();process.wait(timeout=15)
    process=desktop.launch([str(game/"SlayTheSpire2.exe"),"--rendering-driver","opengl3","--rendering-method","gl_compatibility","--force-steam=off","--clientId=8622","--log-file",str(run/"restart.log")],cwd=game,env=env)
    time.sleep(2)
    wait_for(lambda s:s.get("menu_screen")=="main" and "singleplayer" in s.get("options",[]),"restart main menu")
    verified=probe_result(False)
    print("RESTART",verified["message"],flush=True)
    report=json.loads(verified["message"]);assert report["passed"]
    results.append(dict(case="unlock_persists_after_restart",passed=True,report=report))
except Exception as error:
    results.append(dict(case="runner", passed=False, error=str(error)))
    print("FAIL", error, flush=True)
finally:
    report = dict(platform="Windows x64 host / AnyCPU Android package", android_arm64_tested=False, results=results)
    (run / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    if process.poll() is None:
        process.terminate()
        process.wait(timeout=15)
    desktop.close()
    print("REPORT", run / "report.json", flush=True)
raise SystemExit(0 if results and all(r["passed"] for r in results) else 1)
