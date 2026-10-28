"""Keep a test game's private desktop alive until the game exits."""
import argparse
import json
import os
from pathlib import Path
import time

from isolated_desktop import TestDesktop

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--game", type=Path, required=True)
parser.add_argument("--pid-file", type=Path, required=True)
parser.add_argument("arguments", nargs=argparse.REMAINDER)
args = parser.parse_args()
arguments = args.arguments[1:] if args.arguments[:1] == ["--"] else args.arguments
desktop = TestDesktop()
try:
    process = desktop.launch([str(args.game / "SlayTheSpire2.exe"), *arguments], cwd=args.game, env=os.environ.copy())
    pending = args.pid_file.with_suffix(".tmp")
    pending.write_text(json.dumps(dict(pid=process.pid, desktop=desktop.name, isolated=True)), encoding="utf-8")
    pending.replace(args.pid_file)
    while process.poll() is None:
        time.sleep(0.2)
finally:
    desktop.close()
