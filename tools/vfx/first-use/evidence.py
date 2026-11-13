from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]

def harness_fingerprint(game):
    game = Path(game)
    files = {"tools/vfx/first-use/" + name: HERE / name for name in
             ["run.py", "release_check.py", "evidence.py", "Probe.cs", "AllProbe.cs", "CommonProbe.cs", "Probe.csproj"]}
    files["tools/vfx/six-card/isolated_desktop.py"] = ROOT / "tools/vfx/six-card/isolated_desktop.py"
    files["tools/Directory.Build.targets"] = ROOT / "tools/Directory.Build.targets"
    files.update({"game/release_info.json": game / "release_info.json",
                  "game/sts2.dll": game / "data_sts2_windows_x86_64/sts2.dll",
                  "game/KarenSTS2MCP.dll": game / "mods/KarenSTS2MCP.dll"})
    hashes = {name: hashlib.sha256(path.read_bytes()).hexdigest() for name, path in files.items()}
    return hashlib.sha256(json.dumps(hashes, sort_keys=True).encode()).hexdigest()
