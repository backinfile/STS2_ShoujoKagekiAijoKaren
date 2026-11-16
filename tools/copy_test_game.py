"""Copy mutable test configuration and hardlink immutable native game files."""
from pathlib import Path
import os
import shutil
import sys

source, target = (Path(value).resolve() for value in sys.argv[1:])
workspace = Path(__file__).resolve().parents[1]
if not target.is_relative_to(workspace / "artifacts/test-matrix") or source == target:
    raise ValueError("Client copy must stay inside test-matrix and differ from source")
for item in source.rglob("*"):
    relative = item.relative_to(source)
    destination = target / relative
    if item.is_dir():
        destination.mkdir(parents=True, exist_ok=True)
    elif item.is_file():
        destination.parent.mkdir(parents=True, exist_ok=True)
        if "mods" in relative.parts or item.name == "override.cfg":
            shutil.copy2(item, destination)
        else:
            if destination.exists() and os.path.samefile(item, destination):
                continue
            if destination.exists():
                destination.unlink()
            os.link(item, destination)
